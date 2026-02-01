// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Routing;

namespace Amqp.Net.Broker.Core.Exchanges;

/// <summary>
/// Interface for an exchange.
/// </summary>
public interface IExchange
{
    /// <summary>
    /// Gets the exchange name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the exchange type.
    /// </summary>
    ExchangeType Type { get; }

    /// <summary>
    /// Gets whether the exchange is durable.
    /// </summary>
    bool Durable { get; }

    /// <summary>
    /// Gets whether the exchange is auto-delete.
    /// </summary>
    bool AutoDelete { get; }

    /// <summary>
    /// Gets all bindings for this exchange.
    /// </summary>
    IReadOnlyList<Binding> Bindings { get; }

    /// <summary>
    /// Adds a binding to this exchange.
    /// </summary>
    /// <param name="binding">The binding to add.</param>
    void AddBinding(Binding binding);

    /// <summary>
    /// Removes a binding from this exchange.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="routingKey">The routing key (null = remove all bindings for queue).</param>
    /// <returns>True if any bindings were removed.</returns>
    bool RemoveBinding(string queueName, string? routingKey = null);

    /// <summary>
    /// Routes a message to matching queues.
    /// </summary>
    /// <param name="routingKey">The routing key.</param>
    /// <returns>List of queue names to route to.</returns>
    IReadOnlyList<string> Route(string routingKey);
}
