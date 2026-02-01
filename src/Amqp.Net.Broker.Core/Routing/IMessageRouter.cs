// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Exchanges;
using Amqp.Net.Broker.Core.Queues;

namespace Amqp.Net.Broker.Core.Routing;

/// <summary>
/// Interface for the message router.
/// </summary>
public interface IMessageRouter
{
    /// <summary>
    /// Gets all exchanges.
    /// </summary>
    IReadOnlyList<IExchange> Exchanges { get; }

    /// <summary>
    /// Gets all queues.
    /// </summary>
    IReadOnlyList<IQueue> Queues { get; }

    /// <summary>
    /// Routes a message through an exchange to matching queues.
    /// </summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="routingKey">The routing key.</param>
    /// <param name="body">The message body.</param>
    /// <param name="properties">Optional message properties.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of queues the message was routed to.</returns>
    ValueTask<int> RouteAsync(
        string exchangeName,
        string routingKey,
        ReadOnlyMemory<byte> body,
        MessageProperties? properties = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Declares an exchange.
    /// </summary>
    /// <param name="name">The exchange name.</param>
    /// <param name="type">The exchange type.</param>
    /// <param name="durable">Whether the exchange is durable.</param>
    /// <param name="autoDelete">Whether the exchange is auto-delete.</param>
    /// <returns>The declared exchange.</returns>
    IExchange DeclareExchange(string name, ExchangeType type, bool durable = false, bool autoDelete = false);

    /// <summary>
    /// Deletes an exchange.
    /// </summary>
    /// <param name="name">The exchange name.</param>
    /// <returns>True if deleted, false if not found.</returns>
    bool DeleteExchange(string name);

    /// <summary>
    /// Declares a queue.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <param name="options">Queue options.</param>
    /// <returns>The declared queue.</returns>
    IQueue DeclareQueue(string name, QueueOptions? options = null);

    /// <summary>
    /// Deletes a queue.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>True if deleted, false if not found.</returns>
    bool DeleteQueue(string name);

    /// <summary>
    /// Binds a queue to an exchange.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="routingKey">The routing key.</param>
    void Bind(string queueName, string exchangeName, string routingKey = "");

    /// <summary>
    /// Unbinds a queue from an exchange.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="routingKey">The routing key.</param>
    void Unbind(string queueName, string exchangeName, string routingKey = "");

    /// <summary>
    /// Gets an exchange by name.
    /// </summary>
    /// <param name="name">The exchange name.</param>
    /// <returns>The exchange, or null if not found.</returns>
    IExchange? GetExchange(string name);

    /// <summary>
    /// Gets a queue by name.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The queue, or null if not found.</returns>
    IQueue? GetQueue(string name);
}

/// <summary>
/// Optional message properties for routing.
/// </summary>
public sealed record MessageProperties
{
    /// <summary>
    /// Application-level correlation ID.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Application-level message ID.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Reply-to address.
    /// </summary>
    public string? ReplyTo { get; init; }

    /// <summary>
    /// Content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Message expiration time.
    /// </summary>
    public TimeSpan? Expiration { get; init; }

    /// <summary>
    /// Message priority (0-9).
    /// </summary>
    public byte Priority { get; init; }

    /// <summary>
    /// Whether the message is durable.
    /// </summary>
    public bool Durable { get; init; }
}
