// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Amqp.Net.Broker.Core.Messages;

namespace Amqp.Net.Broker.Core.Storage;

/// <summary>
/// Thread-safe in-memory message store implementation.
/// </summary>
public sealed class InMemoryMessageStore : IMessageStore
{
    private readonly ConcurrentDictionary<long, StoredMessage> _messages = new();
    private long _nextMessageId;
    private long _totalSizeBytes;

    /// <inheritdoc />
    public long MessageCount => _messages.Count;

    /// <inheritdoc />
    public long TotalSizeBytes => Interlocked.Read(ref _totalSizeBytes);

    /// <inheritdoc />
    public ValueTask<long> StoreAsync(StoredMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var messageId = Interlocked.Increment(ref _nextMessageId);
        var storedMessage = message with { MessageId = messageId };
        if (_messages.TryAdd(messageId, storedMessage))
        {
            Interlocked.Add(ref _totalSizeBytes, storedMessage.Body.Length);
        }
        return ValueTask.FromResult(messageId);
    }

    /// <inheritdoc />
    public ValueTask<StoredMessage?> GetAsync(long messageId, CancellationToken cancellationToken = default)
    {
        _messages.TryGetValue(messageId, out var message);
        return ValueTask.FromResult(message);
    }

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(long messageId, CancellationToken cancellationToken = default)
    {
        if (_messages.TryRemove(messageId, out var message))
        {
            Interlocked.Add(ref _totalSizeBytes, -message.Body.Length);
            return ValueTask.FromResult(true);
        }
        return ValueTask.FromResult(false);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<StoredMessage>> GetByQueueAsync(string queueName, int maxCount, CancellationToken cancellationToken = default)
    {
        var messages = _messages.Values
                                .Where(m => m.QueueName == queueName && !m.IsExpired)
                                .OrderBy(m => m.EnqueuedAt)
                                .Take(maxCount)
                                .ToList();
        return ValueTask.FromResult<IReadOnlyList<StoredMessage>>(messages);
    }

    /// <summary>
    /// Removes all expired messages from the store.
    /// </summary>
    /// <returns>Number of messages removed.</returns>
    public int PurgeExpired()
    {
        var expiredIds = _messages
                         .Where(kvp => kvp.Value.IsExpired)
                         .Select(kvp => kvp.Key)
                         .ToList();
        var removed = 0;
        foreach (var id in expiredIds)
        {
            if (_messages.TryRemove(id, out var message))
            {
                Interlocked.Add(ref _totalSizeBytes, -message.Body.Length);
                removed++;
            }
        }
        return removed;
    }

    /// <summary>
    /// Clears all messages from the store.
    /// </summary>
    public void Clear()
    {
        _messages.Clear();
        Interlocked.Exchange(ref _totalSizeBytes, 0);
    }
}
