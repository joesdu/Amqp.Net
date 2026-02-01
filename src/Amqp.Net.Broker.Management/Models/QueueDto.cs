// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Queues;

namespace Amqp.Net.Broker.Management.Models;

/// <summary>
/// Response model for a queue.
/// </summary>
public sealed record QueueDto
{
    /// <summary>
    /// The queue name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Number of messages in the queue.
    /// </summary>
    public int MessageCount { get; init; }

    /// <summary>
    /// Number of consumers attached to this queue.
    /// </summary>
    public int ConsumerCount { get; init; }

    /// <summary>
    /// Total size of messages in bytes.
    /// </summary>
    public long TotalSizeBytes { get; init; }

    /// <summary>
    /// Whether the queue is durable.
    /// </summary>
    public bool Durable { get; init; }

    /// <summary>
    /// Whether the queue is exclusive.
    /// </summary>
    public bool Exclusive { get; init; }

    /// <summary>
    /// Whether the queue is auto-delete.
    /// </summary>
    public bool AutoDelete { get; init; }

    /// <summary>
    /// Maximum number of messages (null = unlimited).
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Maximum total size in bytes (null = unlimited).
    /// </summary>
    public long? MaxLengthBytes { get; init; }

    /// <summary>
    /// Message TTL in milliseconds (null = no expiration).
    /// </summary>
    public long? MessageTtlMs { get; init; }

    /// <summary>
    /// Dead letter exchange name.
    /// </summary>
    public string? DeadLetterExchange { get; init; }

    /// <summary>
    /// Dead letter routing key.
    /// </summary>
    public string? DeadLetterRoutingKey { get; init; }

    /// <summary>
    /// Creates a DTO from a queue.
    /// </summary>
    public static QueueDto FromQueue(IQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);
        return new()
        {
            Name = queue.Name,
            MessageCount = queue.MessageCount,
            ConsumerCount = queue.ConsumerCount,
            TotalSizeBytes = queue.TotalSizeBytes,
            Durable = queue.Options.Durable,
            Exclusive = queue.Options.Exclusive,
            AutoDelete = queue.Options.AutoDelete,
            MaxLength = queue.Options.MaxLength,
            MaxLengthBytes = queue.Options.MaxLengthBytes,
            MessageTtlMs = queue.Options.MessageTtl?.TotalMilliseconds is { } ms ? (long)ms : null,
            DeadLetterExchange = queue.Options.DeadLetterExchange,
            DeadLetterRoutingKey = queue.Options.DeadLetterRoutingKey
        };
    }
}

/// <summary>
/// Request model for creating a queue.
/// </summary>
public sealed record CreateQueueRequest
{
    /// <summary>
    /// The queue name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether the queue is durable.
    /// </summary>
    public bool Durable { get; init; }

    /// <summary>
    /// Whether the queue is exclusive.
    /// </summary>
    public bool Exclusive { get; init; }

    /// <summary>
    /// Whether the queue is auto-delete.
    /// </summary>
    public bool AutoDelete { get; init; }

    /// <summary>
    /// Maximum number of messages (null = unlimited).
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Maximum total size in bytes (null = unlimited).
    /// </summary>
    public long? MaxLengthBytes { get; init; }

    /// <summary>
    /// Message TTL in milliseconds (null = no expiration).
    /// </summary>
    public long? MessageTtlMs { get; init; }

    /// <summary>
    /// Dead letter exchange name.
    /// </summary>
    public string? DeadLetterExchange { get; init; }

    /// <summary>
    /// Dead letter routing key.
    /// </summary>
    public string? DeadLetterRoutingKey { get; init; }

    /// <summary>
    /// Converts to QueueOptions.
    /// </summary>
    public QueueOptions ToQueueOptions() =>
        new()
        {
            Durable = Durable,
            Exclusive = Exclusive,
            AutoDelete = AutoDelete,
            MaxLength = MaxLength,
            MaxLengthBytes = MaxLengthBytes,
            MessageTtl = MessageTtlMs.HasValue ? TimeSpan.FromMilliseconds(MessageTtlMs.Value) : null,
            DeadLetterExchange = DeadLetterExchange,
            DeadLetterRoutingKey = DeadLetterRoutingKey
        };
}

/// <summary>
/// Response model for purge operation.
/// </summary>
public sealed record PurgeQueueResponse
{
    /// <summary>
    /// Number of messages purged.
    /// </summary>
    public int MessagesPurged { get; init; }
}
