// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Cluster.Raft;
using Amqp.Net.Broker.Core.Exchanges;
using Amqp.Net.Broker.Core.Messages;
using Amqp.Net.Broker.Core.Queues;
using Amqp.Net.Broker.Core.Routing;
using DotNext.IO;
using DotNext.IO.Log;
using DotNext.Net.Cluster.Consensus.Raft;
using Microsoft.Extensions.Logging;

namespace Amqp.Net.Broker.Cluster;

/// <summary>
/// A clustered message router that replicates topology changes through Raft consensus.
/// </summary>
public sealed class ClusteredMessageRouter : IMessageRouter
{
    private readonly IRaftCluster _cluster;
    private readonly IMessageRouter _localRouter;
    private readonly BrokerStateMachine _stateMachine;
    private readonly ILogger<ClusteredMessageRouter> _logger;

    /// <summary>
    /// Creates a new clustered message router.
    /// </summary>
    public ClusteredMessageRouter(
        IRaftCluster cluster,
        IMessageRouter localRouter,
        BrokerStateMachine stateMachine,
        ILogger<ClusteredMessageRouter> logger)
    {
        _cluster = cluster;
        _localRouter = localRouter;
        _stateMachine = stateMachine;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<IExchange> Exchanges => _localRouter.Exchanges;

    /// <inheritdoc />
    public IReadOnlyList<IQueue> Queues => _localRouter.Queues;

    /// <inheritdoc />
    public ValueTask<int> RouteAsync(
        string exchangeName,
        string routingKey,
        ReadOnlyMemory<byte> body,
        MessageProperties? properties = null,
        CancellationToken cancellationToken = default)
    {
        // Message routing is local - messages are not replicated through Raft
        // Only topology (exchanges, queues, bindings) is replicated
        return _localRouter.RouteAsync(exchangeName, routingKey, body, properties, cancellationToken);
    }

    /// <inheritdoc />
    public IExchange DeclareExchange(string name, ExchangeType type, bool durable = false, bool autoDelete = false)
    {
        // Replicate through Raft if we're the leader
        var command = new DeclareExchangeCommand
        {
            Name = name,
            ExchangeType = type,
            Durable = durable,
            AutoDelete = autoDelete
        };

        ReplicateCommand(command);

        return _localRouter.DeclareExchange(name, type, durable, autoDelete);
    }

    /// <inheritdoc />
    public bool DeleteExchange(string name)
    {
        var command = new DeleteExchangeCommand { Name = name };
        ReplicateCommand(command);

        return _localRouter.DeleteExchange(name);
    }

    /// <inheritdoc />
    public IQueue DeclareQueue(string name, QueueOptions? options = null)
    {
        var command = new DeclareQueueCommand
        {
            Name = name,
            Options = QueueOptionsDto.FromQueueOptions(options ?? QueueOptions.Default)
        };

        ReplicateCommand(command);

        return _localRouter.DeclareQueue(name, options);
    }

    /// <inheritdoc />
    public bool DeleteQueue(string name)
    {
        var command = new DeleteQueueCommand { Name = name };
        ReplicateCommand(command);

        return _localRouter.DeleteQueue(name);
    }

    /// <inheritdoc />
    public void Bind(string queueName, string exchangeName, string routingKey = "")
    {
        var command = new BindCommand
        {
            QueueName = queueName,
            ExchangeName = exchangeName,
            RoutingKey = routingKey
        };

        ReplicateCommand(command);

        _localRouter.Bind(queueName, exchangeName, routingKey);
    }

    /// <inheritdoc />
    public void Unbind(string queueName, string exchangeName, string routingKey = "")
    {
        var command = new UnbindCommand
        {
            QueueName = queueName,
            ExchangeName = exchangeName,
            RoutingKey = routingKey
        };

        ReplicateCommand(command);

        _localRouter.Unbind(queueName, exchangeName, routingKey);
    }

    /// <inheritdoc />
    public IExchange? GetExchange(string name) => _localRouter.GetExchange(name);

    /// <inheritdoc />
    public IQueue? GetQueue(string name) => _localRouter.GetQueue(name);

    private void ReplicateCommand(ClusterCommand command)
    {
        // Check if we're the leader
        var leader = _cluster.Leader;
        if (leader is null || _cluster.LeadershipToken.IsCancellationRequested)
        {
            _logger.LogDebug("Not leader, skipping replication for {CommandType}", command.Type);
            return;
        }

        // We are the leader, replicate the command
        var data = command.Serialize();
        var entry = new BinaryLogEntry(data, _cluster.Term);

        try
        {
            // Fire and forget - the command will be applied when committed
            _ = _cluster.ReplicateAsync(entry, CancellationToken.None).AsTask();
            _logger.LogDebug("Replicated command {CommandType}", command.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to replicate command {CommandType}", command.Type);
            throw;
        }
    }
}

/// <summary>
/// A simple binary log entry for Raft replication.
/// </summary>
internal readonly struct BinaryLogEntry : IRaftLogEntry
{
    private readonly byte[] _data;

    public BinaryLogEntry(byte[] data, long term)
    {
        _data = data;
        Term = term;
        Timestamp = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset Timestamp { get; }

    public long Term { get; }

    public int? CommandId => null;

    bool ILogEntry.IsSnapshot => false;

    bool IDataTransferObject.IsReusable => true;

    long? IDataTransferObject.Length => _data.Length;

    public ValueTask WriteToAsync<TWriter>(TWriter writer, CancellationToken token) where TWriter : IAsyncBinaryWriter
        => writer.Invoke(new ReadOnlyMemory<byte>(_data), token);
}
