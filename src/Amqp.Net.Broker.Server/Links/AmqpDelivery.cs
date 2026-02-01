// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Broker.Server.Links;

/// <summary>
/// Represents a message delivery on a link.
/// </summary>
public sealed class AmqpDelivery
{
    /// <summary>
    /// The delivery ID.
    /// </summary>
    public uint DeliveryId { get; init; }

    /// <summary>
    /// The delivery tag.
    /// </summary>
    public ReadOnlyMemory<byte> DeliveryTag { get; init; }

    /// <summary>
    /// The message format.
    /// </summary>
    public uint MessageFormat { get; init; }

    /// <summary>
    /// Whether the delivery is settled.
    /// </summary>
    public bool Settled { get; set; }

    /// <summary>
    /// Whether more frames are coming for this delivery.
    /// </summary>
    public bool More { get; init; }

    /// <summary>
    /// The message payload.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; init; }

    /// <summary>
    /// The delivery state (after disposition).
    /// </summary>
    public object? DeliveryState { get; set; }
}
