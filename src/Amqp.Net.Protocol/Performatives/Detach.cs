// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Protocol.Types;

namespace Amqp.Net.Protocol.Performatives;

/// <summary>
/// AMQP Detach performative - detaches a link from a session.
/// Descriptor: 0x00000000:0x00000016
/// </summary>
public sealed record Detach : PerformativeBase
{
    /// <inheritdoc />
    public override ulong DescriptorCode => Descriptor.Detach;

    /// <summary>
    /// The link handle (mandatory).
    /// </summary>
    public required uint Handle { get; init; }

    /// <summary>
    /// Close the link (destroy terminus).
    /// </summary>
    public bool Closed { get; init; }

    /// <summary>
    /// Error causing the detach.
    /// </summary>
    public AmqpError? Error { get; init; }

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

        // Field 1: closed
        offset += AmqpEncoder.EncodeBoolean(buffer[offset..], Closed);
        if (fieldCount <= 2)
        {
            return offset;
        }

        // Field 2: error
        if (Error != null)
        {
            offset += Error.Encode(buffer[offset..]);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        return offset;
    }

    private int GetFieldCount()
    {
        if (Error != null)
        {
            return 3;
        }
        if (Closed)
        {
            return 2;
        }
        return 1;
    }

    /// <inheritdoc />
    public override int GetEncodedSize() => 64;

    /// <summary>
    /// Decodes a Detach performative.
    /// </summary>
    public static Detach Decode(ReadOnlySpan<byte> buffer, out int listSize, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out var headerSize);
        listSize = size;
        bytesConsumed = headerSize + size;
        if (count < 1)
        {
            throw new AmqpDecodeException("Detach requires at least 1 field");
        }
        var offset = headerSize;

        // Field 0: handle
        var handle = AmqpDecoder.DecodeUInt(buffer[offset..], out var consumed);
        offset += consumed;
        var detach = new Detach { Handle = handle };
        if (count <= 1)
        {
            return detach;
        }

        // Field 1: closed
        var closed = false;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
        {
            closed = AmqpDecoder.DecodeBoolean(buffer[offset..], out consumed);
        }
        offset += consumed;
        if (count <= 2)
        {
            return detach with { Closed = closed };
        }

        // Field 2: error
        AmqpError? error = null;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
        {
            error = AmqpError.Decode(buffer[offset..], out consumed);
        }
        offset += consumed;
        return detach with { Closed = closed, Error = error };
    }

    /// <inheritdoc />
    public override string ToString() => $"Detach(Handle={Handle}, Closed={Closed}, Error={Error})";
}

/// <summary>
/// AMQP End performative - ends a session.
/// Descriptor: 0x00000000:0x00000017
/// </summary>
public sealed record End : PerformativeBase
{
    /// <inheritdoc />
    public override ulong DescriptorCode => Descriptor.End;

    /// <summary>
    /// Error causing the end.
    /// </summary>
    public AmqpError? Error { get; init; }

    /// <inheritdoc />
    public override int Encode(Span<byte> buffer)
    {
        var offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        if (Error != null)
        {
            Span<byte> body = stackalloc byte[256];
            var bodySize = Error.Encode(body);
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

    /// <inheritdoc />
    public override int GetEncodedSize() => 32;

    /// <summary>
    /// Decodes an End performative.
    /// </summary>
    public static End Decode(ReadOnlySpan<byte> buffer, out int listSize, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out var headerSize);
        listSize = size;
        bytesConsumed = headerSize + size;
        AmqpError? error = null;
        if (count >= 1)
        {
            var offset = headerSize;
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out var consumed))
            {
                error = AmqpError.Decode(buffer[offset..], out consumed);
            }
        }
        return new() { Error = error };
    }

    /// <inheritdoc />
    public override string ToString() => $"End(Error={Error})";
}

/// <summary>
/// AMQP Close performative - closes a connection.
/// Descriptor: 0x00000000:0x00000018
/// </summary>
public sealed record Close : PerformativeBase
{
    /// <inheritdoc />
    public override ulong DescriptorCode => Descriptor.Close;

    /// <summary>
    /// Error causing the close.
    /// </summary>
    public AmqpError? Error { get; init; }

    /// <inheritdoc />
    public override int Encode(Span<byte> buffer)
    {
        var offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        if (Error != null)
        {
            Span<byte> body = stackalloc byte[256];
            var bodySize = Error.Encode(body);
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

    /// <inheritdoc />
    public override int GetEncodedSize() => 32;

    /// <summary>
    /// Decodes a Close performative.
    /// </summary>
    public static Close Decode(ReadOnlySpan<byte> buffer, out int listSize, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out var headerSize);
        listSize = size;
        bytesConsumed = headerSize + size;
        AmqpError? error = null;
        if (count >= 1)
        {
            var offset = headerSize;
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out var consumed))
            {
                error = AmqpError.Decode(buffer[offset..], out consumed);
            }
        }
        return new() { Error = error };
    }

    /// <inheritdoc />
    public override string ToString() => $"Close(Error={Error})";
}
