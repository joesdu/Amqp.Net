// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Messages;

namespace Amqp.Net.Broker.Core.Queues;

/// <summary>
/// Interface for a message queue.
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - Queue is the correct domain term
public interface IQueue
#pragma warning restore CA1711
{
    /// <summary>
    /// Gets the queue name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the queue options.
    /// </summary>
    QueueOptions Options { get; }

    /// <summary>
    /// Gets the number of messages in the queue.
    /// </summary>
    int MessageCount { get; }

    /// <summary>
    /// Gets the number of consumers attached to this queue.
    /// </summary>
    int ConsumerCount { get; }

    /// <summary>
    /// Gets the total size of messages in bytes.
    /// </summary>
    long TotalSizeBytes { get; }

    /// <summary>
    /// Enqueues a message.
    /// </summary>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if enqueued, false if queue is full.</returns>
    ValueTask<bool> EnqueueAsync(StoredMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues the next message.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message, or null if queue is empty.</returns>
    ValueTask<StoredMessage?> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Peeks at the next message without removing it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message, or null if queue is empty.</returns>
    ValueTask<StoredMessage?> PeekAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges a message, removing it from the queue.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if acknowledged, false if not found.</returns>
    ValueTask<bool> AcknowledgeAsync(long messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a message, optionally requeuing it.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="requeue">Whether to requeue the message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if rejected, false if not found.</returns>
    ValueTask<bool> RejectAsync(long messageId, bool requeue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges all messages from the queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of messages purged.</returns>
    ValueTask<int> PurgeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a consumer.
    /// </summary>
    void AddConsumer();

    /// <summary>
    /// Unregisters a consumer.
    /// </summary>
    void RemoveConsumer();
}
