// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Protocol.Types;

namespace Amqp.Net.Protocol.Performatives;

/// <summary>
/// AMQP Flow performative - updates flow state.
/// Descriptor: 0x00000000:0x00000013
/// </summary>
public sealed record Flow : PerformativeBase
{
    /// <inheritdoc />
    public override ulong DescriptorCode => Descriptor.Flow;

    /// <summary>
    /// Next expected incoming transfer-id.
    /// </summary>
    public uint? NextIncomingId { get; init; }

    /// <summary>
    /// Incoming window size (mandatory).
    /// </summary>
    public required uint IncomingWindow { get; init; }

    /// <summary>
    /// Next outgoing transfer-id (mandatory).
    /// </summary>
    public required uint NextOutgoingId { get; init; }

    /// <summary>
    /// Outgoing window size (mandatory).
    /// </summary>
    public required uint OutgoingWindow { get; init; }

    /// <summary>
    /// Link handle (null for session-level flow).
    /// </summary>
    public uint? Handle { get; init; }

    /// <summary>
    /// Delivery count at sender.
    /// </summary>
    public uint? DeliveryCount { get; init; }

    /// <summary>
    /// Link credit granted to sender.
    /// </summary>
    public uint? LinkCredit { get; init; }

    /// <summary>
    /// Messages available at sender.
    /// </summary>
    public uint? Available { get; init; }

    /// <summary>
    /// Drain mode flag.
    /// </summary>
    public bool Drain { get; init; }

    /// <summary>
    /// Echo flow state back.
    /// </summary>
    public bool Echo { get; init; }

    /// <summary>
    /// Link properties.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Properties { get; init; }

    /// <inheritdoc />
    public override int Encode(Span<byte> buffer)
    {
        var offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        var fieldCount = GetFieldCount();
        Span<byte> body = stackalloc byte[128];
        var bodySize = EncodeFields(body, fieldCount);
        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;
        return offset;
    }

    private int EncodeFields(Span<byte> buffer, int fieldCount)
    {
        var offset = 0;

        // Field 0: next-incoming-id
        if (NextIncomingId.HasValue)
        {
            offset += AmqpEncoder.EncodeUInt(buffer[offset..], NextIncomingId.Value);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }

        // Field 1: incoming-window (mandatory)
        offset += AmqpEncoder.EncodeUInt(buffer[offset..], IncomingWindow);

        // Field 2: next-outgoing-id (mandatory)
        offset += AmqpEncoder.EncodeUInt(buffer[offset..], NextOutgoingId);

        // Field 3: outgoing-window (mandatory)
        offset += AmqpEncoder.EncodeUInt(buffer[offset..], OutgoingWindow);
        if (fieldCount <= 4)
        {
            return offset;
        }

        // Field 4: handle
        if (Handle.HasValue)
        {
            offset += AmqpEncoder.EncodeUInt(buffer[offset..], Handle.Value);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        if (fieldCount <= 5)
        {
            return offset;
        }

        // Field 5: delivery-count
        if (DeliveryCount.HasValue)
        {
            offset += AmqpEncoder.EncodeUInt(buffer[offset..], DeliveryCount.Value);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        if (fieldCount <= 6)
        {
            return offset;
        }

        // Field 6: link-credit
        if (LinkCredit.HasValue)
        {
            offset += AmqpEncoder.EncodeUInt(buffer[offset..], LinkCredit.Value);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        if (fieldCount <= 7)
        {
            return offset;
        }

        // Field 7: available
        if (Available.HasValue)
        {
            offset += AmqpEncoder.EncodeUInt(buffer[offset..], Available.Value);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        if (fieldCount <= 8)
        {
            return offset;
        }

        // Field 8: drain
        offset += AmqpEncoder.EncodeBoolean(buffer[offset..], Drain);
        if (fieldCount <= 9)
        {
            return offset;
        }

        // Field 9: echo
        offset += AmqpEncoder.EncodeBoolean(buffer[offset..], Echo);
        if (fieldCount <= 10)
        {
            return offset;
        }

        // Field 10: properties
        offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        return offset;
    }

    private int GetFieldCount()
    {
        if (Properties != null)
        {
            return 11;
        }
        if (Echo)
        {
            return 10;
        }
        if (Drain)
        {
            return 9;
        }
        if (Available.HasValue)
        {
            return 8;
        }
        if (LinkCredit.HasValue)
        {
            return 7;
        }
        if (DeliveryCount.HasValue)
        {
            return 6;
        }
        if (Handle.HasValue)
        {
            return 5;
        }
        return 4;
    }

    /// <inheritdoc />
    public override int GetEncodedSize() => 64;

    /// <summary>
    /// Decodes a Flow performative.
    /// </summary>
    public static Flow Decode(ReadOnlySpan<byte> buffer, out int listSize, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out var headerSize);
        listSize = size;
        bytesConsumed = headerSize + size;
        if (count < 4)
        {
            throw new AmqpDecodeException("Flow requires at least 4 fields");
        }
        var offset = headerSize;

        // Field 0: next-incoming-id
        uint? nextIncomingId = null;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out var consumed))
        {
            nextIncomingId = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        }
        offset += consumed;

        // Field 1: incoming-window
        var incomingWindow = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        offset += consumed;

        // Field 2: next-outgoing-id
        var nextOutgoingId = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        offset += consumed;

        // Field 3: outgoing-window
        var outgoingWindow = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        offset += consumed;
        var flow = new Flow
        {
            NextIncomingId = nextIncomingId,
            IncomingWindow = incomingWindow,
            NextOutgoingId = nextOutgoingId,
            OutgoingWindow = outgoingWindow
        };
        if (count <= 4)
        {
            return flow;
        }

        // Field 4: handle
        uint? handle = null;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
        {
            handle = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        }
        offset += consumed;
        if (count <= 5)
        {
            return flow with { Handle = handle };
        }

        // Field 5: delivery-count
        uint? deliveryCount = null;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
        {
            deliveryCount = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        }
        offset += consumed;
        if (count <= 6)
        {
            return flow with { Handle = handle, DeliveryCount = deliveryCount };
        }

        // Field 6: link-credit
        uint? linkCredit = null;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
        {
            linkCredit = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        }
        offset += consumed;
        return flow with { Handle = handle, DeliveryCount = deliveryCount, LinkCredit = linkCredit };
    }

    /// <inheritdoc />
    public override string ToString() => $"Flow(Handle={Handle}, DeliveryCount={DeliveryCount}, LinkCredit={LinkCredit}, Drain={Drain})";
}
