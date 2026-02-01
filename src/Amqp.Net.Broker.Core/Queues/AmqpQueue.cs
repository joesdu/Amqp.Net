// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Threading.Channels;
using Amqp.Net.Broker.Core.Messages;

namespace Amqp.Net.Broker.Core.Queues;

/// <summary>
/// In-memory queue implementation using Channel for async producer/consumer pattern.
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - Queue is the correct domain term
public sealed class AmqpQueue : IQueue, IAsyncDisposable
#pragma warning restore CA1711
{
    private readonly Channel<StoredMessage> _channel;
    private readonly ConcurrentDictionary<long, StoredMessage> _unackedMessages = new();
    private readonly ConcurrentDictionary<long, StoredMessage> _allMessages = new();
    private int _consumerCount;
    private long _totalSizeBytes;
    private bool _disposed;

    /// <summary>
    /// Creates a new queue with the specified name and options.
    /// </summary>
    public AmqpQueue(string name, QueueOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Options = options ?? QueueOptions.Default;

        // Configure channel based on options
        var channelOptions = Options.MaxLength.HasValue
            ? new BoundedChannelOptions(Options.MaxLength.Value)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            }
            : (ChannelOptions)new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            };

        _channel = Options.MaxLength.HasValue
            ? Channel.CreateBounded<StoredMessage>((BoundedChannelOptions)channelOptions)
            : Channel.CreateUnbounded<StoredMessage>((UnboundedChannelOptions)channelOptions);
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public QueueOptions Options { get; }

    /// <inheritdoc />
    public int MessageCount => _allMessages.Count;

    /// <inheritdoc />
    public int ConsumerCount => _consumerCount;

    /// <inheritdoc />
    public long TotalSizeBytes => Interlocked.Read(ref _totalSizeBytes);

    /// <inheritdoc />
    public async ValueTask<bool> EnqueueAsync(StoredMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Check size limit
        if (Options.MaxLengthBytes.HasValue && 
            Interlocked.Read(ref _totalSizeBytes) + message.Body.Length > Options.MaxLengthBytes.Value)
        {
            return false;
        }

        // Apply message TTL if configured
        var finalMessage = message;
        if (Options.MessageTtl.HasValue && !message.ExpiresAt.HasValue)
        {
            finalMessage = message with { ExpiresAt = DateTimeOffset.UtcNow + Options.MessageTtl.Value };
        }

        try
        {
            await _channel.Writer.WriteAsync(finalMessage, cancellationToken).ConfigureAwait(false);
            _allMessages.TryAdd(finalMessage.MessageId, finalMessage);
            Interlocked.Add(ref _totalSizeBytes, finalMessage.Body.Length);
            return true;
        }
        catch (ChannelClosedException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async ValueTask<StoredMessage?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_channel.Reader.TryRead(out var message))
                {
                    // Skip expired messages
                    if (message.IsExpired)
                    {
                        _allMessages.TryRemove(message.MessageId, out _);
                        Interlocked.Add(ref _totalSizeBytes, -message.Body.Length);
                        continue;
                    }

                    // Track as unacknowledged
                    var deliveredMessage = message with { DeliveryCount = message.DeliveryCount + 1 };
                    _unackedMessages.TryAdd(deliveredMessage.MessageId, deliveredMessage);
                    
                    return deliveredMessage;
                }
            }
        }
        catch (ChannelClosedException)
        {
            // Queue was closed
        }

        return null;
    }

    /// <inheritdoc />
    public ValueTask<StoredMessage?> PeekAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_channel.Reader.TryPeek(out var message))
        {
            return ValueTask.FromResult<StoredMessage?>(message);
        }

        return ValueTask.FromResult<StoredMessage?>(null);
    }

    /// <inheritdoc />
    public ValueTask<bool> AcknowledgeAsync(long messageId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_unackedMessages.TryRemove(messageId, out var message))
        {
            _allMessages.TryRemove(messageId, out _);
            Interlocked.Add(ref _totalSizeBytes, -message.Body.Length);
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    /// <inheritdoc />
    public async ValueTask<bool> RejectAsync(long messageId, bool requeue, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_unackedMessages.TryRemove(messageId, out var message))
        {
            return false;
        }

        if (requeue && !message.IsExpired)
        {
            // Requeue at the front (by writing back to channel)
            try
            {
                await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                // Queue was closed, message is lost
                _allMessages.TryRemove(messageId, out _);
                Interlocked.Add(ref _totalSizeBytes, -message.Body.Length);
            }
        }
        else
        {
            // Don't requeue - remove from all messages
            _allMessages.TryRemove(messageId, out _);
            Interlocked.Add(ref _totalSizeBytes, -message.Body.Length);
        }

        return true;
    }

    /// <inheritdoc />
    public ValueTask<int> PurgeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        int count = 0;
        while (_channel.Reader.TryRead(out var message))
        {
            _allMessages.TryRemove(message.MessageId, out _);
            Interlocked.Add(ref _totalSizeBytes, -message.Body.Length);
            count++;
        }

        return ValueTask.FromResult(count);
    }

    /// <inheritdoc />
    public void AddConsumer()
    {
        Interlocked.Increment(ref _consumerCount);
    }

    /// <inheritdoc />
    public void RemoveConsumer()
    {
        Interlocked.Decrement(ref _consumerCount);
    }

    /// <summary>
    /// Disposes the queue.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        _channel.Writer.Complete();
        _allMessages.Clear();
        _unackedMessages.Clear();

        return ValueTask.CompletedTask;
    }
}
