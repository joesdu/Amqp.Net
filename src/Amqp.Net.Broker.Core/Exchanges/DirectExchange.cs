// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Routing;

namespace Amqp.Net.Broker.Core.Exchanges;

/// <summary>
/// Direct exchange - routes messages to queues with exact routing key match.
/// </summary>
public sealed class DirectExchange : IExchange
{
    private readonly List<Binding> _bindings = [];
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new direct exchange.
    /// </summary>
    public DirectExchange(string name, bool durable = false, bool autoDelete = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Durable = durable;
        AutoDelete = autoDelete;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ExchangeType Type => ExchangeType.Direct;

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
            // Check for duplicate
            if (!_bindings.Any(b => b.QueueName == binding.QueueName && b.RoutingKey == binding.RoutingKey))
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
            int removed = routingKey == null
                ? _bindings.RemoveAll(b => b.QueueName == queueName)
                : _bindings.RemoveAll(b => b.QueueName == queueName && b.RoutingKey == routingKey);

            return removed > 0;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Route(string routingKey)
    {
        lock (_lock)
        {
            return _bindings
                .Where(b => b.RoutingKey == routingKey)
                .Select(b => b.QueueName)
                .Distinct()
                .ToList();
        }
    }
}
