// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Broker.Core.Delivery;

/// <summary>
/// Interface for tracking in-flight message deliveries.
/// </summary>
public interface IDeliveryTracker
{
    /// <summary>
    /// Gets the count of unsettled deliveries.
    /// </summary>
    int UnsettledCount { get; }

    /// <summary>
    /// Gets the next delivery ID.
    /// </summary>
    /// <returns>A unique delivery ID.</returns>
    uint NextDeliveryId();

    /// <summary>
    /// Tracks a delivery.
    /// </summary>
    /// <param name="deliveryId">The delivery ID.</param>
    /// <param name="messageId">The message ID.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="linkName">The link name.</param>
    void Track(uint deliveryId, long messageId, string queueName, string linkName);

    /// <summary>
    /// Gets delivery information.
    /// </summary>
    /// <param name="deliveryId">The delivery ID.</param>
    /// <returns>The delivery info, or null if not found.</returns>
    DeliveryInfo? GetDelivery(uint deliveryId);

    /// <summary>
    /// Settles a delivery, removing it from tracking.
    /// </summary>
    /// <param name="deliveryId">The delivery ID.</param>
    /// <returns>True if settled, false if not found.</returns>
    bool Settle(uint deliveryId);

    /// <summary>
    /// Gets all unsettled deliveries older than the specified time.
    /// </summary>
    /// <param name="olderThan">The age threshold.</param>
    /// <returns>List of delivery IDs.</returns>
    IReadOnlyList<uint> GetUnsettledDeliveries(TimeSpan olderThan);

    /// <summary>
    /// Gets all unsettled deliveries for a specific link.
    /// </summary>
    /// <param name="linkName">The link name.</param>
    /// <returns>List of delivery IDs.</returns>
    IReadOnlyList<uint> GetUnsettledDeliveriesForLink(string linkName);
}

/// <summary>
/// Information about a tracked delivery.
/// </summary>
public sealed record DeliveryInfo
{
    /// <summary>
    /// The delivery ID.
    /// </summary>
    public required uint DeliveryId { get; init; }

    /// <summary>
    /// The message ID.
    /// </summary>
    public required long MessageId { get; init; }

    /// <summary>
    /// The queue name.
    /// </summary>
    public required string QueueName { get; init; }

    /// <summary>
    /// The link name.
    /// </summary>
    public required string LinkName { get; init; }

    /// <summary>
    /// When the delivery was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
