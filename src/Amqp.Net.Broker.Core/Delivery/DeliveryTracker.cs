// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Amqp.Net.Broker.Core.Delivery;

/// <summary>
/// Thread-safe implementation for tracking in-flight message deliveries.
/// </summary>
public sealed class DeliveryTracker : IDeliveryTracker
{
    private readonly ConcurrentDictionary<uint, DeliveryInfo> _deliveries = new();
    private uint _nextDeliveryId;

    /// <inheritdoc />
    public int UnsettledCount => _deliveries.Count;

    /// <inheritdoc />
    public uint NextDeliveryId() => Interlocked.Increment(ref _nextDeliveryId);

    /// <inheritdoc />
    public void Track(uint deliveryId, long messageId, string queueName, string linkName)
    {
        var info = new DeliveryInfo
        {
            DeliveryId = deliveryId,
            MessageId = messageId,
            QueueName = queueName,
            LinkName = linkName,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _deliveries.TryAdd(deliveryId, info);
    }

    /// <inheritdoc />
    public DeliveryInfo? GetDelivery(uint deliveryId)
    {
        _deliveries.TryGetValue(deliveryId, out var info);
        return info;
    }

    /// <inheritdoc />
    public bool Settle(uint deliveryId) => _deliveries.TryRemove(deliveryId, out _);

    /// <inheritdoc />
    public IReadOnlyList<uint> GetUnsettledDeliveries(TimeSpan olderThan)
    {
        var threshold = DateTimeOffset.UtcNow - olderThan;
        return _deliveries
               .Where(kvp => kvp.Value.CreatedAt < threshold)
               .Select(kvp => kvp.Key)
               .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<uint> GetUnsettledDeliveriesForLink(string linkName)
    {
        return _deliveries
               .Where(kvp => kvp.Value.LinkName == linkName)
               .Select(kvp => kvp.Key)
               .ToList();
    }

    /// <summary>
    /// Settles all deliveries for a specific link.
    /// </summary>
    /// <param name="linkName">The link name.</param>
    /// <returns>Number of deliveries settled.</returns>
    public int SettleAllForLink(string linkName)
    {
        var deliveryIds = GetUnsettledDeliveriesForLink(linkName);
        var count = 0;
        foreach (var id in deliveryIds)
        {
            if (_deliveries.TryRemove(id, out _))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Clears all tracked deliveries.
    /// </summary>
    public void Clear()
    {
        _deliveries.Clear();
    }
}
