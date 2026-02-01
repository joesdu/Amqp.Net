// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Amqp.Net.Broker.Core.Exchanges;
using Amqp.Net.Broker.Core.Messages;
using Amqp.Net.Broker.Core.Queues;

namespace Amqp.Net.Broker.Core.Routing;

/// <summary>
/// Central message routing engine that coordinates exchanges and queues.
/// </summary>
public sealed class MessageRouter : IMessageRouter
{
    private readonly ConcurrentDictionary<string, IExchange> _exchanges = new();
    private readonly ConcurrentDictionary<string, IQueue> _queues = new();
    private long _nextMessageId;

    /// <summary>
    /// Creates a new message router with default exchanges.
    /// </summary>
    public MessageRouter()
    {
        // Create default exchanges
        _exchanges.TryAdd("", new DirectExchange("", true)); // Default direct exchange
        _exchanges.TryAdd("amq.direct", new DirectExchange("amq.direct", true));
        _exchanges.TryAdd("amq.topic", new TopicExchange("amq.topic", true));
        _exchanges.TryAdd("amq.fanout", new FanoutExchange("amq.fanout", true));
    }

    /// <inheritdoc />
    public IReadOnlyList<IExchange> Exchanges => _exchanges.Values.ToList();

    /// <inheritdoc />
    public IReadOnlyList<IQueue> Queues => _queues.Values.ToList();

    /// <inheritdoc />
    public async ValueTask<int> RouteAsync(
        string exchangeName,
        string routingKey,
        ReadOnlyMemory<byte> body,
        MessageProperties? properties = null,
        CancellationToken cancellationToken = default)
    {
        if (!_exchanges.TryGetValue(exchangeName, out var exchange))
        {
            return 0;
        }
        var queueNames = exchange.Route(routingKey);
        if (queueNames.Count == 0)
        {
            return 0;
        }
        var routed = 0;
        var messageId = Interlocked.Increment(ref _nextMessageId);
        var now = DateTimeOffset.UtcNow;
        foreach (var queueName in queueNames)
        {
            if (!_queues.TryGetValue(queueName, out var queue))
            {
                continue;
            }
            var message = new StoredMessage
            {
                MessageId = messageId,
                QueueName = queueName,
                Body = body,
                EnqueuedAt = now,
                RoutingKey = routingKey,
                ExchangeName = exchangeName,
                CorrelationId = properties?.CorrelationId,
                MessageIdProperty = properties?.MessageId,
                ReplyTo = properties?.ReplyTo,
                ContentType = properties?.ContentType,
                ExpiresAt = properties?.Expiration is { } expiration ? now + expiration : null,
                Priority = properties?.Priority ?? 0,
                Durable = properties?.Durable ?? false
            };
            if (await queue.EnqueueAsync(message, cancellationToken).ConfigureAwait(false))
            {
                routed++;
            }
        }
        return routed;
    }

    /// <inheritdoc />
    public IExchange DeclareExchange(string name, ExchangeType type, bool durable = false, bool autoDelete = false)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _exchanges.GetOrAdd(name, _ => type switch
        {
            ExchangeType.Direct => new DirectExchange(name, durable, autoDelete),
            ExchangeType.Topic  => new TopicExchange(name, durable, autoDelete),
            ExchangeType.Fanout => new FanoutExchange(name, durable, autoDelete),
            _                   => throw new ArgumentException($"Unsupported exchange type: {type}", nameof(type))
        });
    }

    /// <inheritdoc />
    public bool DeleteExchange(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        // Don't allow deleting default exchanges
        if (name.Length == 0 || name.StartsWith("amq.", StringComparison.Ordinal))
        {
            return false;
        }
        return _exchanges.TryRemove(name, out _);
    }

    /// <inheritdoc />
    public IQueue DeclareQueue(string name, QueueOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var queue = _queues.GetOrAdd(name, _ => new AmqpQueue(name, options));

        // Auto-bind to default exchange with queue name as routing key
        if (_exchanges.TryGetValue("", out var defaultExchange))
        {
            defaultExchange.AddBinding(new()
            {
                QueueName = name,
                ExchangeName = "",
                RoutingKey = name
            });
        }
        return queue;
    }

    /// <inheritdoc />
    public bool DeleteQueue(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_queues.TryRemove(name, out var queue))
        {
            // Remove all bindings to this queue
            foreach (var exchange in _exchanges.Values)
            {
                exchange.RemoveBinding(name);
            }

            // Dispose the queue if it implements IAsyncDisposable
            if (queue is IAsyncDisposable disposable)
            {
                _ = disposable.DisposeAsync().AsTask();
            }
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public void Bind(string queueName, string exchangeName, string routingKey = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(exchangeName);
        if (!_exchanges.TryGetValue(exchangeName, out var exchange))
        {
            throw new InvalidOperationException($"Exchange '{exchangeName}' not found");
        }
        if (!_queues.ContainsKey(queueName))
        {
            throw new InvalidOperationException($"Queue '{queueName}' not found");
        }
        exchange.AddBinding(new()
        {
            QueueName = queueName,
            ExchangeName = exchangeName,
            RoutingKey = routingKey
        });
    }

    /// <inheritdoc />
    public void Unbind(string queueName, string exchangeName, string routingKey = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(exchangeName);
        if (_exchanges.TryGetValue(exchangeName, out var exchange))
        {
            exchange.RemoveBinding(queueName, routingKey);
        }
    }

    /// <inheritdoc />
    public IExchange? GetExchange(string name)
    {
        _exchanges.TryGetValue(name, out var exchange);
        return exchange;
    }

    /// <inheritdoc />
    public IQueue? GetQueue(string name)
    {
        _queues.TryGetValue(name, out var queue);
        return queue;
    }
}
