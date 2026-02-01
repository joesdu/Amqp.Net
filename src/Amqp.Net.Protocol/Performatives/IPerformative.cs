// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Protocol.Types;

namespace Amqp.Net.Protocol.Performatives;

/// <summary>
/// Base interface for all AMQP performatives (frame bodies).
/// </summary>
public interface IPerformative
{
    /// <summary>
    /// Gets the descriptor code for this performative.
    /// </summary>
    ulong DescriptorCode { get; }

    /// <summary>
    /// Encodes this performative to a buffer.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    /// <returns>The number of bytes written.</returns>
    int Encode(Span<byte> buffer);

    /// <summary>
    /// Calculates the encoded size of this performative.
    /// </summary>
    /// <returns>The size in bytes.</returns>
    int GetEncodedSize();
}

/// <summary>
/// Base class for performatives with common encoding logic.
/// </summary>
public abstract record PerformativeBase : IPerformative
{
    /// <inheritdoc />
    public abstract ulong DescriptorCode { get; }

    /// <inheritdoc />
    public abstract int Encode(Span<byte> buffer);

    /// <inheritdoc />
    public abstract int GetEncodedSize();

    /// <summary>
    /// Encodes the descriptor and list header for a performative.
    /// </summary>
    static protected int EncodeListHeader(Span<byte> buffer, ulong descriptor, int bodySize, int fieldCount)
    {
        var offset = 0;

        // Described type constructor (0x00)
        buffer[offset++] = FormatCode.Described;

        // Descriptor (ulong)
        offset += AmqpEncoder.EncodeULong(buffer[offset..], descriptor);

        // List header
        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);
        return offset;
    }

    /// <summary>
    /// Calculates the size of the descriptor encoding.
    /// </summary>
    static protected int GetDescriptorSize(ulong descriptor)
    {
        // 0x00 (described) + ulong encoding
        if (descriptor == 0)
        {
            return 2; // 0x00 + 0x44 (ulong0)
        }
        if (descriptor <= 255)
        {
            return 3; // 0x00 + 0x53 + byte
        }
        return 10; // 0x00 + 0x80 + 8 bytes
    }

    /// <summary>
    /// Calculates the size of the list header encoding.
    /// </summary>
    static protected int GetListHeaderSize(int bodySize, int fieldCount)
    {
        if (fieldCount == 0)
        {
            return 1; // list0
        }
        if (bodySize <= 255 && fieldCount <= 255)
        {
            return 3; // list8
        }
        return 9; // list32
    }
}

/// <summary>
/// Factory for decoding performatives from wire format.
/// </summary>
public static class PerformativeDecoder
{
    /// <summary>
    /// Decodes a performative from a buffer.
    /// </summary>
    public static IPerformative Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        // Check for described type
        if (buffer[0] != FormatCode.Described)
        {
            throw new AmqpDecodeException($"Expected described type (0x00), got 0x{buffer[0]:X2}");
        }

        // Decode descriptor
        var descriptor = AmqpDecoder.DecodeULong(buffer[1..], out var descriptorSize);
        var offset = 1 + descriptorSize;

        // Decode based on descriptor
        IPerformative performative = descriptor switch
        {
            Descriptor.Open        => Open.Decode(buffer[offset..], out var openSize, out bytesConsumed),
            Descriptor.Begin       => Begin.Decode(buffer[offset..], out var beginSize, out bytesConsumed),
            Descriptor.Attach      => Attach.Decode(buffer[offset..], out var attachSize, out bytesConsumed),
            Descriptor.Flow        => Flow.Decode(buffer[offset..], out var flowSize, out bytesConsumed),
            Descriptor.Transfer    => Transfer.Decode(buffer[offset..], out var transferSize, out bytesConsumed),
            Descriptor.Disposition => Disposition.Decode(buffer[offset..], out var dispSize, out bytesConsumed),
            Descriptor.Detach      => Detach.Decode(buffer[offset..], out var detachSize, out bytesConsumed),
            Descriptor.End         => End.Decode(buffer[offset..], out var endSize, out bytesConsumed),
            Descriptor.Close       => Close.Decode(buffer[offset..], out var closeSize, out bytesConsumed),
            _                      => throw new AmqpDecodeException($"Unknown performative descriptor: 0x{descriptor:X16}")
        };
        bytesConsumed += offset;
        return performative;
    }

    /// <summary>
    /// Peeks at the descriptor code without fully decoding.
    /// </summary>
    public static ulong PeekDescriptor(ReadOnlySpan<byte> buffer)
    {
        if (buffer[0] != FormatCode.Described)
        {
            throw new AmqpDecodeException($"Expected described type (0x00), got 0x{buffer[0]:X2}");
        }
        return AmqpDecoder.DecodeULong(buffer[1..], out _);
    }
}
