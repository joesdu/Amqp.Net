// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Protocol.Types;

// DeliveryState is defined in Disposition.cs
namespace Amqp.Net.Protocol.Performatives;

/// <summary>
/// AMQP Transfer performative - transfers message data.
/// Descriptor: 0x00000000:0x00000014
/// </summary>
public sealed record Transfer : PerformativeBase
{
    /// <inheritdoc />
    public override ulong DescriptorCode => Descriptor.Transfer;

    /// <summary>
    /// Link handle (mandatory).
    /// </summary>
    public required uint Handle { get; init; }

    /// <summary>
    /// Delivery identifier.
    /// </summary>
    public uint? DeliveryId { get; init; }

    /// <summary>
    /// Delivery tag.
    /// </summary>
    public byte[]? DeliveryTag { get; init; }

    /// <summary>
    /// Message format (0 = AMQP message).
    /// </summary>
    public uint? MessageFormat { get; init; }

    /// <summary>
    /// Pre-settled delivery.
    /// </summary>
    public bool? Settled { get; init; }

    /// <summary>
    /// More frames follow for this delivery.
    /// </summary>
    public bool More { get; init; }

    /// <summary>
    /// Receiver settle mode override.
    /// </summary>
    public byte? RcvSettleMode { get; init; }

    /// <summary>
    /// Delivery state.
    /// </summary>
    public object? State { get; init; }

    /// <summary>
    /// Resume delivery.
    /// </summary>
    public bool Resume { get; init; }

    /// <summary>
    /// Abort delivery.
    /// </summary>
    public bool Aborted { get; init; }

    /// <summary>
    /// Batchable hint.
    /// </summary>
    public bool Batchable { get; init; }

    /// <inheritdoc />
    public override int Encode(Span<byte> buffer)
    {
        var offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        var fieldCount = GetFieldCount();
        Span<byte> body = stackalloc byte[256];
        var bodySize = EncodeFields(body, fieldCount);
        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;
        return offset;
    }

    private int EncodeFields(Span<byte> buffer, int fieldCount)
    {
        var offset = 0;

        // Field 0: handle (mandatory)
        offset += AmqpEncoder.EncodeUInt(buffer[offset..], Handle);
        if (fieldCount <= 1)
        {
            return offset;
        }

        // Field 1: delivery-id
        if (DeliveryId.HasValue)
        {
            offset += AmqpEncoder.EncodeUInt(buffer[offset..], DeliveryId.Value);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        if (fieldCount <= 2)
        {
            return offset;
        }

        // Field 2: delivery-tag
        if (DeliveryTag != null)
        {
            offset += AmqpEncoder.EncodeBinary(buffer[offset..], DeliveryTag);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        if (fieldCount <= 3)
        {
            return offset;
        }

        // Field 3: message-format
        if (MessageFormat.HasValue)
        {
            offset += AmqpEncoder.EncodeUInt(buffer[offset..], MessageFormat.Value);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        if (fieldCount <= 4)
        {
            return offset;
        }

        // Field 4: settled
        if (Settled.HasValue)
        {
            offset += AmqpEncoder.EncodeBoolean(buffer[offset..], Settled.Value);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        if (fieldCount <= 5)
        {
            return offset;
        }

        // Field 5: more
        offset += AmqpEncoder.EncodeBoolean(buffer[offset..], More);
        if (fieldCount <= 6)
        {
            return offset;
        }

        // Field 6: rcv-settle-mode
        if (RcvSettleMode.HasValue)
        {
            offset += AmqpEncoder.EncodeUByte(buffer[offset..], RcvSettleMode.Value);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        if (fieldCount <= 7)
        {
            return offset;
        }

        // Field 7: state
        if (State is DeliveryState deliveryState)
        {
            offset += deliveryState.Encode(buffer[offset..]);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        if (fieldCount <= 8)
        {
            return offset;
        }

        // Field 8: resume
        offset += AmqpEncoder.EncodeBoolean(buffer[offset..], Resume);
        if (fieldCount <= 9)
        {
            return offset;
        }

        // Field 9: aborted
        offset += AmqpEncoder.EncodeBoolean(buffer[offset..], Aborted);
        if (fieldCount <= 10)
        {
            return offset;
        }

        // Field 10: batchable
        offset += AmqpEncoder.EncodeBoolean(buffer[offset..], Batchable);
        return offset;
    }

    private int GetFieldCount()
    {
        if (Batchable)
        {
            return 11;
        }
        if (Aborted)
        {
            return 10;
        }
        if (Resume)
        {
            return 9;
        }
        if (State != null)
        {
            return 8;
        }
        if (RcvSettleMode.HasValue)
        {
            return 7;
        }
        if (More)
        {
            return 6;
        }
        if (Settled.HasValue)
        {
            return 5;
        }
        if (MessageFormat.HasValue)
        {
            return 4;
        }
        if (DeliveryTag != null)
        {
            return 3;
        }
        if (DeliveryId.HasValue)
        {
            return 2;
        }
        return 1;
    }

    /// <inheritdoc />
    public override int GetEncodedSize() => 128;

    /// <summary>
    /// Decodes a Transfer performative.
    /// </summary>
    public static Transfer Decode(ReadOnlySpan<byte> buffer, out int listSize, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out var headerSize);
        listSize = size;
        bytesConsumed = headerSize + size;
        if (count < 1)
        {
            throw new AmqpDecodeException("Transfer requires at least 1 field");
        }
        var offset = headerSize;

        // Field 0: handle (mandatory)
        var handle = AmqpDecoder.DecodeUInt(buffer[offset..], out var consumed);
        offset += consumed;
        var transfer = new Transfer { Handle = handle };
        if (count <= 1)
        {
            return transfer;
        }

        // Field 1: delivery-id
        uint? deliveryId = null;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
        {
            deliveryId = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        }
        offset += consumed;
        if (count <= 2)
        {
            return transfer with { DeliveryId = deliveryId };
        }

        // Field 2: delivery-tag
        byte[]? deliveryTag = null;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
        {
            var tagSpan = AmqpDecoder.DecodeBinary(buffer[offset..], out consumed);
            deliveryTag = tagSpan.ToArray();
        }
        offset += consumed;
        if (count <= 3)
        {
            return transfer with { DeliveryId = deliveryId, DeliveryTag = deliveryTag };
        }

        // Field 3: message-format
        uint? messageFormat = null;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
        {
            messageFormat = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        }
        offset += consumed;
        if (count <= 4)
        {
            return transfer with { DeliveryId = deliveryId, DeliveryTag = deliveryTag, MessageFormat = messageFormat };
        }

        // Field 4: settled
        bool? settled = null;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
        {
            settled = AmqpDecoder.DecodeBoolean(buffer[offset..], out consumed);
        }
        offset += consumed;
        if (count <= 5)
        {
            return transfer with { DeliveryId = deliveryId, DeliveryTag = deliveryTag, MessageFormat = messageFormat, Settled = settled };
        }

        // Field 5: more
        var more = false;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
        {
            more = AmqpDecoder.DecodeBoolean(buffer[offset..], out consumed);
        }
        offset += consumed;
        return transfer with { DeliveryId = deliveryId, DeliveryTag = deliveryTag, MessageFormat = messageFormat, Settled = settled, More = more };
    }

    /// <inheritdoc />
    public override string ToString() => $"Transfer(Handle={Handle}, DeliveryId={DeliveryId}, Settled={Settled}, More={More})";
}
