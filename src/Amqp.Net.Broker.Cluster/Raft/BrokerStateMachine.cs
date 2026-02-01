// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amqp.Net.Broker.Core.Exchanges;
using Amqp.Net.Broker.Core.Queues;
using DotNext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Amqp.Net.Broker.Cluster.Raft;

/// <summary>
/// Raft state machine for broker metadata (exchanges, queues, bindings).
/// This state machine replicates broker topology across cluster nodes.
/// </summary>
public sealed class BrokerStateMachine : ISupplier<BrokerState>, IAsyncDisposable
{
    /// <summary>
    /// Configuration key for log location.
    /// </summary>
    public const string LogLocation = "BrokerCluster:DataPath";

    private readonly DirectoryInfo _location;

    private readonly ILogger<BrokerStateMachine>? _logger;
    private readonly SemaphoreSlim _snapshotLock = new(1, 1);

    /// <summary>
    /// Creates a new broker state machine.
    /// </summary>
    public BrokerStateMachine(DirectoryInfo location, ILogger<BrokerStateMachine>? logger = null)
    {
        _location = location;
        _logger = logger;
        if (!_location.Exists)
        {
            _location.Create();
        }

        // Try to restore from existing snapshot
        RestoreFromSnapshot();
    }

    /// <summary>
    /// Creates a new broker state machine from path.
    /// </summary>
    public BrokerStateMachine(string location, ILogger<BrokerStateMachine>? logger = null)
        : this(new DirectoryInfo(Path.GetFullPath(Path.Combine(location, "state"))), logger) { }

    /// <summary>
    /// Creates a new broker state machine from configuration.
    /// </summary>
    public BrokerStateMachine(IConfiguration? config, ILogger<BrokerStateMachine>? logger = null)
        : this(config?[LogLocation] ?? "./data/raft", logger)
    {
        ArgumentNullException.ThrowIfNull(config);
    }

    /// <summary>
    /// Gets the current broker state.
    /// </summary>
    public BrokerState State { get; } = new();

    /// <summary>
    /// Gets the last applied log index.
    /// </summary>
    public long LastAppliedIndex { get; private set; }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _snapshotLock.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    BrokerState ISupplier<BrokerState>.Invoke() => State;

    /// <summary>
    /// Applies a command to the state machine.
    /// </summary>
    /// <param name="data">The serialized command data.</param>
    /// <param name="index">The log index.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The applied index.</returns>
    public async ValueTask<long> ApplyAsync(ReadOnlyMemory<byte> data, long index, CancellationToken token)
    {
        if (data.Length == 0)
        {
            _logger?.LogWarning("Received empty log entry");
            return index;
        }
        try
        {
            var command = ClusterCommand.Deserialize(data.Span);
            ApplyCommand(command);
            _logger?.LogDebug("Applied command {CommandType}", command.Type);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to apply log entry");
            throw;
        }
        LastAppliedIndex = index;

        // Create snapshot every 100 log entries
        if (LastAppliedIndex % 100L == 0)
        {
            await CreateSnapshotAsync(LastAppliedIndex, token).ConfigureAwait(false);
        }
        return LastAppliedIndex;
    }

    /// <summary>
    /// Gets the current snapshot data.
    /// </summary>
    public async Task<byte[]> GetSnapshotAsync(CancellationToken token)
    {
        using var stream = new MemoryStream();
        await State.SerializeAsync(stream, token).ConfigureAwait(false);
        return stream.ToArray();
    }

    private void ApplyCommand(ClusterCommand command)
    {
        switch (command)
        {
            case DeclareExchangeCommand declareExchange:
                State.DeclareExchange(declareExchange.Name, declareExchange.ExchangeType, declareExchange.Durable, declareExchange.AutoDelete);
                break;
            case DeleteExchangeCommand deleteExchange:
                State.DeleteExchange(deleteExchange.Name);
                break;
            case DeclareQueueCommand declareQueue:
                State.DeclareQueue(declareQueue.Name, declareQueue.Options.ToQueueOptions());
                break;
            case DeleteQueueCommand deleteQueue:
                State.DeleteQueue(deleteQueue.Name);
                break;
            case BindCommand bind:
                State.Bind(bind.QueueName, bind.ExchangeName, bind.RoutingKey);
                break;
            case UnbindCommand unbind:
                State.Unbind(unbind.QueueName, unbind.ExchangeName, unbind.RoutingKey);
                break;
            default:
                throw new InvalidOperationException($"Unknown command type: {command.GetType().Name}");
        }
    }

    private void RestoreFromSnapshot()
    {
        var latestSnapshot = _location.EnumerateFiles("snapshot_*.json")
                                      .Select(f =>
                                      {
                                          var fileName = Path.GetFileNameWithoutExtension(f.Name);
                                          if (fileName.StartsWith("snapshot_", StringComparison.Ordinal) &&
                                              long.TryParse(fileName.AsSpan(9), out var index))
                                          {
                                              return (File: f, Index: index);
                                          }
                                          return (File: f, Index: -1L);
                                      })
                                      .Where(x => x.Index >= 0)
                                      .OrderByDescending(x => x.Index)
                                      .FirstOrDefault();
        if (latestSnapshot.File is not null)
        {
            try
            {
                using var stream = latestSnapshot.File.OpenRead();
                State.DeserializeAsync(stream, CancellationToken.None).GetAwaiter().GetResult();
                LastAppliedIndex = latestSnapshot.Index;
                _logger?.LogInformation("Restored state from snapshot at index {Index}: {ExchangeCount} exchanges, {QueueCount} queues",
                    latestSnapshot.Index, State.Exchanges.Count, State.Queues.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to restore from snapshot: {FileName}", latestSnapshot.File.Name);
            }
        }
    }

    private async Task CreateSnapshotAsync(long index, CancellationToken token)
    {
        await _snapshotLock.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var snapshotFile = new FileInfo(Path.Combine(_location.FullName, $"snapshot_{index}.json"));
            await using (var stream = snapshotFile.Create())
            {
                await State.SerializeAsync(stream, token).ConfigureAwait(false);
            }
            _logger?.LogInformation("Created snapshot at index {Index}: {ExchangeCount} exchanges, {QueueCount} queues",
                index, State.Exchanges.Count, State.Queues.Count);

            // Clean up old snapshots (keep last 3)
            var oldSnapshots = _location.EnumerateFiles("snapshot_*.json")
                                        .Select(f =>
                                        {
                                            var fileName = Path.GetFileNameWithoutExtension(f.Name);
                                            if (fileName.StartsWith("snapshot_", StringComparison.Ordinal) &&
                                                long.TryParse(fileName.AsSpan(9), out var idx))
                                            {
                                                return (File: f, Index: idx);
                                            }
                                            return (File: f, Index: -1L);
                                        })
                                        .Where(x => x.Index >= 0)
                                        .OrderByDescending(x => x.Index)
                                        .Skip(3)
                                        .ToList();
            foreach (var (file, _) in oldSnapshots)
            {
                try
                {
                    file.Delete();
                    _logger?.LogDebug("Deleted old snapshot: {FileName}", file.Name);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to delete old snapshot: {FileName}", file.Name);
                }
            }
        }
        finally
        {
            _snapshotLock.Release();
        }
    }
}

/// <summary>
/// Represents the replicated broker state (exchanges, queues, bindings).
/// </summary>
public sealed class BrokerState
{
    private readonly ConcurrentDictionary<string, List<BindingMetadata>> _bindings = new();
    private readonly ConcurrentDictionary<string, ExchangeMetadata> _exchanges = new();
    private readonly ConcurrentDictionary<string, QueueMetadata> _queues = new();

    /// <summary>
    /// Gets all exchanges.
    /// </summary>
    public IReadOnlyDictionary<string, ExchangeMetadata> Exchanges => _exchanges;

    /// <summary>
    /// Gets all queues.
    /// </summary>
    public IReadOnlyDictionary<string, QueueMetadata> Queues => _queues;

    /// <summary>
    /// Gets all bindings grouped by exchange.
    /// </summary>
    public IReadOnlyDictionary<string, List<BindingMetadata>> Bindings => _bindings;

    /// <summary>
    /// Declares an exchange.
    /// </summary>
    public void DeclareExchange(string name, ExchangeType type, bool durable, bool autoDelete)
    {
        _exchanges.TryAdd(name, new()
        {
            Name = name,
            Type = type,
            Durable = durable,
            AutoDelete = autoDelete
        });
    }

    /// <summary>
    /// Deletes an exchange.
    /// </summary>
    public bool DeleteExchange(string name)
    {
        if (_exchanges.TryRemove(name, out _))
        {
            _bindings.TryRemove(name, out _);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Declares a queue.
    /// </summary>
    public void DeclareQueue(string name, QueueOptions options)
    {
        _queues.TryAdd(name, new()
        {
            Name = name,
            Options = options
        });
    }

    /// <summary>
    /// Deletes a queue.
    /// </summary>
    public bool DeleteQueue(string name)
    {
        if (_queues.TryRemove(name, out _))
        {
            // Remove all bindings to this queue
            foreach (var exchangeBindings in _bindings.Values)
            {
                exchangeBindings.RemoveAll(b => b.QueueName == name);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Binds a queue to an exchange.
    /// </summary>
    public void Bind(string queueName, string exchangeName, string routingKey)
    {
        var bindings = _bindings.GetOrAdd(exchangeName, _ => []);
        var binding = new BindingMetadata
        {
            QueueName = queueName,
            ExchangeName = exchangeName,
            RoutingKey = routingKey
        };
        lock (bindings)
        {
            if (!bindings.Any(b => b.QueueName == queueName && b.RoutingKey == routingKey))
            {
                bindings.Add(binding);
            }
        }
    }

    /// <summary>
    /// Unbinds a queue from an exchange.
    /// </summary>
    public void Unbind(string queueName, string exchangeName, string routingKey)
    {
        if (_bindings.TryGetValue(exchangeName, out var bindings))
        {
            lock (bindings)
            {
                bindings.RemoveAll(b => b.QueueName == queueName && b.RoutingKey == routingKey);
            }
        }
    }

    /// <summary>
    /// Serializes the state to a stream.
    /// </summary>
    public async Task SerializeAsync(Stream stream, CancellationToken token)
    {
        var snapshot = new BrokerStateSnapshot
        {
            Exchanges = [.. _exchanges.Values],
            Queues = [.. _queues.Values],
            Bindings = [.. _bindings.SelectMany(kvp => kvp.Value)]
        };
        await JsonSerializer.SerializeAsync(stream, snapshot, BrokerStateSnapshotContext.Default.BrokerStateSnapshot, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Deserializes the state from a stream.
    /// </summary>
    public async Task DeserializeAsync(Stream stream, CancellationToken token)
    {
        var snapshot = await JsonSerializer.DeserializeAsync(stream, BrokerStateSnapshotContext.Default.BrokerStateSnapshot, token).ConfigureAwait(false);
        if (snapshot is null)
        {
            return;
        }
        _exchanges.Clear();
        _queues.Clear();
        _bindings.Clear();
        foreach (var exchange in snapshot.Exchanges)
        {
            _exchanges.TryAdd(exchange.Name, exchange);
        }
        foreach (var queue in snapshot.Queues)
        {
            _queues.TryAdd(queue.Name, queue);
        }
        foreach (var binding in snapshot.Bindings)
        {
            var bindings = _bindings.GetOrAdd(binding.ExchangeName, _ => []);
            bindings.Add(binding);
        }
    }
}

/// <summary>
/// Exchange metadata for replication.
/// </summary>
public sealed record ExchangeMetadata
{
    /// <summary>
    /// Exchange name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Exchange type.
    /// </summary>
    public ExchangeType Type { get; init; }

    /// <summary>
    /// Whether the exchange is durable.
    /// </summary>
    public bool Durable { get; init; }

    /// <summary>
    /// Whether the exchange is auto-delete.
    /// </summary>
    public bool AutoDelete { get; init; }
}

/// <summary>
/// Queue metadata for replication.
/// </summary>
public sealed record QueueMetadata
{
    /// <summary>
    /// Queue name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Queue options.
    /// </summary>
    public QueueOptions Options { get; init; } = QueueOptions.Default;
}

/// <summary>
/// Binding metadata for replication.
/// </summary>
public sealed record BindingMetadata
{
    /// <summary>
    /// Queue name.
    /// </summary>
    public required string QueueName { get; init; }

    /// <summary>
    /// Exchange name.
    /// </summary>
    public required string ExchangeName { get; init; }

    /// <summary>
    /// Routing key.
    /// </summary>
    public string RoutingKey { get; init; } = "";
}

/// <summary>
/// Snapshot of broker state for serialization.
/// </summary>
internal sealed record BrokerStateSnapshot
{
    public List<ExchangeMetadata> Exchanges { get; init; } = [];

    public List<QueueMetadata> Queues { get; init; } = [];

    public List<BindingMetadata> Bindings { get; init; } = [];
}

/// <summary>
/// JSON serialization context for broker state snapshot.
/// </summary>
[JsonSerializable(typeof(BrokerStateSnapshot))]
[JsonSerializable(typeof(ExchangeMetadata))]
[JsonSerializable(typeof(QueueMetadata))]
[JsonSerializable(typeof(BindingMetadata))]
[JsonSerializable(typeof(QueueOptions))]
internal sealed partial class BrokerStateSnapshotContext : JsonSerializerContext { }
