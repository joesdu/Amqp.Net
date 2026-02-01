// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Messages;

namespace Amqp.Net.Broker.Core.Storage;

/// <summary>
/// Interface for message persistence.
/// </summary>
public interface IMessageStore
{
    /// <summary>
    /// Gets the total number of stored messages.
    /// </summary>
    long MessageCount { get; }

    /// <summary>
    /// Gets the total size of stored messages in bytes.
    /// </summary>
    long TotalSizeBytes { get; }

    /// <summary>
    /// Stores a message and returns its assigned ID.
    /// </summary>
    /// <param name="message">The message to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The assigned message ID.</returns>
    ValueTask<long> StoreAsync(StoredMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a message by ID.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message, or null if not found.</returns>
    ValueTask<StoredMessage?> GetAsync(long messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a message by ID.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message was deleted, false if not found.</returns>
    ValueTask<bool> DeleteAsync(long messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages for a specific queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="maxCount">Maximum number of messages to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of messages.</returns>
    ValueTask<IReadOnlyList<StoredMessage>> GetByQueueAsync(string queueName, int maxCount, CancellationToken cancellationToken = default);
}
