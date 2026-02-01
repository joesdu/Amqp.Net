// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Routing;

namespace Amqp.Net.Broker.Core.Exchanges;

/// <summary>
/// Fanout exchange - routes messages to all bound queues regardless of routing key.
/// </summary>
public sealed class FanoutExchange : IExchange
{
    private readonly List<Binding> _bindings = [];
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new fanout exchange.
    /// </summary>
    public FanoutExchange(string name, bool durable = false, bool autoDelete = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Durable = durable;
        AutoDelete = autoDelete;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ExchangeType Type => ExchangeType.Fanout;

    /// <inheritdoc />
    public bool Durable { get; }

    /// <inheritdoc />
    public bool AutoDelete { get; }

    /// <inheritdoc />
    public IReadOnlyList<Binding> Bindings
    {
        get
        {
            lock (_lock)
            {
                return _bindings.ToList();
            }
        }
    }

    /// <inheritdoc />
    public void AddBinding(Binding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        lock (_lock)
        {
            // Check for duplicate (only queue name matters for fanout)
            if (!_bindings.Any(b => b.QueueName == binding.QueueName))
            {
                _bindings.Add(binding);
            }
        }
    }

    /// <inheritdoc />
    public bool RemoveBinding(string queueName, string? routingKey = null)
    {
        lock (_lock)
        {
            // For fanout, routing key is ignored
            var removed = _bindings.RemoveAll(b => b.QueueName == queueName);
            return removed > 0;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Route(string routingKey)
    {
        // Fanout ignores routing key - route to all bound queues
        lock (_lock)
        {
            return _bindings
                   .Select(b => b.QueueName)
                   .Distinct()
                   .ToList();
        }
    }
}
