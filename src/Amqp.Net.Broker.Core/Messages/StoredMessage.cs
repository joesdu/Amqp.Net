// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Broker.Core.Messages;

/// <summary>
/// Represents a message stored in the broker.
/// </summary>
public sealed record StoredMessage
{
    /// <summary>
    /// Unique identifier for this message within the store.
    /// </summary>
    public required long MessageId { get; init; }

    /// <summary>
    /// The queue this message belongs to.
    /// </summary>
    public required string QueueName { get; init; }

    /// <summary>
    /// The message body.
    /// </summary>
    public required ReadOnlyMemory<byte> Body { get; init; }

    /// <summary>
    /// When the message was enqueued.
    /// </summary>
    public required DateTimeOffset EnqueuedAt { get; init; }

    /// <summary>
    /// The routing key used to route this message.
    /// </summary>
    public string? RoutingKey { get; init; }

    /// <summary>
    /// The exchange this message was published to.
    /// </summary>
    public string? ExchangeName { get; init; }

    /// <summary>
    /// Application-level correlation ID.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Application-level message ID.
    /// </summary>
    public string? MessageIdProperty { get; init; }

    /// <summary>
    /// Reply-to address for request/response patterns.
    /// </summary>
    public string? ReplyTo { get; init; }

    /// <summary>
    /// Content type of the message body.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// When the message expires (null = never).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Message priority (0-9, higher = more important).
    /// </summary>
    public byte Priority { get; init; }

    /// <summary>
    /// Whether the message should survive broker restart.
    /// </summary>
    public bool Durable { get; init; }

    /// <summary>
    /// Number of times this message has been delivered.
    /// </summary>
    public int DeliveryCount { get; init; }

    /// <summary>
    /// Returns true if the message has expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;
}
