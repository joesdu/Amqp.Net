// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Protocol.Types;

namespace Amqp.Net.Protocol.Performatives;

/// <summary>
/// AMQP Disposition performative - updates delivery state.
/// Descriptor: 0x00000000:0x00000015
/// </summary>
public sealed record Disposition : PerformativeBase
{
    /// <inheritdoc />
    public override ulong DescriptorCode => Descriptor.Disposition;

    /// <summary>
    /// The role: false = sender, true = receiver (mandatory).
    /// </summary>
    public required bool Role { get; init; }

    /// <summary>
    /// First delivery-id in range (mandatory).
    /// </summary>
    public required uint First { get; init; }

    /// <summary>
    /// Last delivery-id in range (null = same as first).
    /// </summary>
    public uint? Last { get; init; }

    /// <summary>
    /// Settled flag.
    /// </summary>
    public bool Settled { get; init; }

    /// <summary>
    /// Delivery state (accepted, rejected, released, modified).
    /// </summary>
    public DeliveryState? State { get; init; }

    /// <summary>
    /// Batchable hint.
    /// </summary>
    public bool Batchable { get; init; }

    /// <inheritdoc />
    public override int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);

        int fieldCount = GetFieldCount();
        Span<byte> body = stackalloc byte[128];
        int bodySize = EncodeFields(body, fieldCount);

        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;

        return offset;
    }

    private int EncodeFields(Span<byte> buffer, int fieldCount)
    {
        int offset = 0;

        // Field 0: role (mandatory)
        offset += AmqpEncoder.EncodeBoolean(buffer[offset..], Role);

        // Field 1: first (mandatory)
        offset += AmqpEncoder.EncodeUInt(buffer[offset..], First);

        if (fieldCount <= 2) return offset;

        // Field 2: last
        if (Last.HasValue)
            offset += AmqpEncoder.EncodeUInt(buffer[offset..], Last.Value);
        else
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);

        if (fieldCount <= 3) return offset;

        // Field 3: settled
        offset += AmqpEncoder.EncodeBoolean(buffer[offset..], Settled);

        if (fieldCount <= 4) return offset;

        // Field 4: state
        if (State != null)
            offset += State.Encode(buffer[offset..]);
        else
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);

        if (fieldCount <= 5) return offset;

        // Field 5: batchable
        offset += AmqpEncoder.EncodeBoolean(buffer[offset..], Batchable);

        return offset;
    }

    private int GetFieldCount()
    {
        if (Batchable) return 6;
        if (State != null) return 5;
        if (Settled) return 4;
        if (Last.HasValue) return 3;
        return 2;
    }

    /// <inheritdoc />
    public override int GetEncodedSize() => 64;

    /// <summary>
    /// Decodes a Disposition performative.
    /// </summary>
    public static Disposition Decode(ReadOnlySpan<byte> buffer, out int listSize, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out int headerSize);
        listSize = size;
        bytesConsumed = headerSize + size;

        if (count < 2)
            throw new AmqpDecodeException("Disposition requires at least 2 fields");

        int offset = headerSize;

        // Field 0: role
        bool role = AmqpDecoder.DecodeBoolean(buffer[offset..], out int consumed);
        offset += consumed;

        // Field 1: first
        uint first = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        offset += consumed;

        var disposition = new Disposition { Role = role, First = first };

        if (count <= 2) return disposition;

        // Field 2: last
        uint? last = null;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
            last = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        offset += consumed;

        if (count <= 3) return disposition with { Last = last };

        // Field 3: settled
        bool settled = false;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
            settled = AmqpDecoder.DecodeBoolean(buffer[offset..], out consumed);
        offset += consumed;

        if (count <= 4) return disposition with { Last = last, Settled = settled };

        // Field 4: state - skip for now
        consumed = AmqpDecoder.SkipValue(buffer[offset..]);
        offset += consumed;

        return disposition with { Last = last, Settled = settled };
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"Disposition(Role={(Role ? "receiver" : "sender")}, First={First}, Last={Last}, Settled={Settled}, State={State?.GetType().Name})";
}

/// <summary>
/// Base class for delivery states.
/// </summary>
public abstract class DeliveryState
{
    public abstract ulong DescriptorCode { get; }
    public abstract int Encode(Span<byte> buffer);
}

/// <summary>
/// Accepted delivery state.
/// </summary>
public sealed class Accepted : DeliveryState
{
    public static readonly Accepted Instance = new();

    public override ulong DescriptorCode => Descriptor.Accepted;

    public override int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], 0, 0);
        return offset;
    }
}

/// <summary>
/// Rejected delivery state.
/// </summary>
public sealed class Rejected : DeliveryState
{
    public override ulong DescriptorCode => Descriptor.Rejected;

    public AmqpError? Error { get; init; }

    public override int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);

        if (Error != null)
        {
            Span<byte> body = stackalloc byte[256];
            int bodySize = Error.Encode(body);
            offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, 1);
            body[..bodySize].CopyTo(buffer[offset..]);
            offset += bodySize;
        }
        else
        {
            offset += AmqpEncoder.EncodeListHeader(buffer[offset..], 0, 0);
        }

        return offset;
    }
}

/// <summary>
/// Released delivery state.
/// </summary>
public sealed class Released : DeliveryState
{
    public static readonly Released Instance = new();

    public override ulong DescriptorCode => Descriptor.Released;

    public override int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], 0, 0);
        return offset;
    }
}

/// <summary>
/// Modified delivery state.
/// </summary>
public sealed class Modified : DeliveryState
{
    public override ulong DescriptorCode => Descriptor.Modified;

    public bool DeliveryFailed { get; init; }
    public bool UndeliverableHere { get; init; }
    public IReadOnlyDictionary<string, object?>? MessageAnnotations { get; init; }

    public override int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);

        int fieldCount = GetFieldCount();
        Span<byte> body = stackalloc byte[64];
        int bodySize = 0;

        if (fieldCount > 0)
            bodySize += AmqpEncoder.EncodeBoolean(body[bodySize..], DeliveryFailed);
        if (fieldCount > 1)
            bodySize += AmqpEncoder.EncodeBoolean(body[bodySize..], UndeliverableHere);
        if (fieldCount > 2)
        {
            if (MessageAnnotations != null)
                bodySize += AmqpEncoder.EncodeMap(body[bodySize..], MessageAnnotations);
            else
                bodySize += AmqpEncoder.EncodeNull(body[bodySize..]);
        }

        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;

        return offset;
    }

    private int GetFieldCount()
    {
        if (MessageAnnotations != null) return 3;
        if (UndeliverableHere) return 2;
        if (DeliveryFailed) return 1;
        return 0;
    }
}

/// <summary>
/// AMQP Error composite type.
/// </summary>
public sealed class AmqpError
{
    public required string Condition { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, object?>? Info { get; init; }

    public int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], Descriptor.Error);

        int fieldCount = GetFieldCount();
        Span<byte> body = stackalloc byte[512];
        int bodySize = 0;

        // Field 0: condition (mandatory)
        bodySize += AmqpEncoder.EncodeSymbol(body[bodySize..], Condition);

        if (fieldCount > 1)
        {
            // Field 1: description
            if (Description != null)
                bodySize += AmqpEncoder.EncodeString(body[bodySize..], Description);
            else
                bodySize += AmqpEncoder.EncodeNull(body[bodySize..]);
        }

        if (fieldCount > 2)
        {
            // Field 2: info
            if (Info != null)
                bodySize += AmqpEncoder.EncodeMap(body[bodySize..], Info);
            else
                bodySize += AmqpEncoder.EncodeNull(body[bodySize..]);
        }

        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;

        return offset;
    }

    private int GetFieldCount()
    {
        if (Info != null) return 3;
        if (Description != null) return 2;
        return 1;
    }

    public override string ToString() => $"Error({Condition}: {Description})";

    /// <summary>
    /// Decodes an AmqpError from the buffer.
    /// </summary>
    public static AmqpError Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        int offset = 0;
        
        // Skip described type constructor (0x00)
        if (buffer[offset] != FormatCode.Described)
            throw new AmqpDecodeException($"Expected described type for AmqpError, got 0x{buffer[offset]:X2}");
        offset++;
        
        // Skip descriptor (ulong)
        AmqpDecoder.DecodeULong(buffer[offset..], out int consumed);
        offset += consumed;
        
        // Decode list header
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer[offset..], out int headerSize);
        offset += headerSize;
        
        if (count < 1)
            throw new AmqpDecodeException("AmqpError requires at least condition field");
        
        // Field 0: condition (symbol, mandatory)
        string condition = AmqpDecoder.DecodeSymbol(buffer[offset..], out consumed);
        offset += consumed;
        
        string? description = null;
        if (count >= 2)
        {
            // Field 1: description (string, optional)
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
            {
                description = AmqpDecoder.DecodeString(buffer[offset..], out consumed);
            }
            offset += consumed;
        }
        
        // Skip info field if present (field 2)
        if (count >= 3)
        {
            consumed = AmqpDecoder.SkipValue(buffer[offset..]);
            offset += consumed;
        }
        
        bytesConsumed = offset;
        return new AmqpError { Condition = condition, Description = description };
    }
}
