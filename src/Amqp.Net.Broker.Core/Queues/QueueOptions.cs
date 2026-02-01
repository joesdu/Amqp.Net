// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Broker.Core.Queues;

/// <summary>
/// Configuration options for a queue.
/// </summary>
public sealed record QueueOptions
{
    /// <summary>
    /// Whether the queue survives broker restart.
    /// </summary>
    public bool Durable { get; init; }

    /// <summary>
    /// Whether the queue is exclusive to the declaring connection.
    /// </summary>
    public bool Exclusive { get; init; }

    /// <summary>
    /// Whether the queue is deleted when the last consumer disconnects.
    /// </summary>
    public bool AutoDelete { get; init; }

    /// <summary>
    /// Maximum number of messages in the queue (null = unlimited).
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Maximum total size of messages in bytes (null = unlimited).
    /// </summary>
    public long? MaxLengthBytes { get; init; }

    /// <summary>
    /// Default message TTL (null = messages don't expire).
    /// </summary>
    public TimeSpan? MessageTtl { get; init; }

    /// <summary>
    /// Exchange to route dead-lettered messages to.
    /// </summary>
    public string? DeadLetterExchange { get; init; }

    /// <summary>
    /// Routing key for dead-lettered messages.
    /// </summary>
    public string? DeadLetterRoutingKey { get; init; }

    /// <summary>
    /// Maximum priority level (0-255, default 0 = no priority).
    /// </summary>
    public byte MaxPriority { get; init; }

    /// <summary>
    /// Default options for a transient queue.
    /// </summary>
    public static QueueOptions Default { get; } = new();

    /// <summary>
    /// Options for a durable queue.
    /// </summary>
    public static QueueOptions DurableQueue { get; } = new() { Durable = true };

    /// <summary>
    /// Options for an exclusive, auto-delete queue.
    /// </summary>
    public static QueueOptions TemporaryQueue { get; } = new() { Exclusive = true, AutoDelete = true };
}
