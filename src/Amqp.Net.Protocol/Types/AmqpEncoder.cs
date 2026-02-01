// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace Amqp.Net.Protocol.Types;

/// <summary>
/// High-performance AMQP type encoder using Span and zero-copy patterns.
/// All methods write in network byte order (big-endian).
/// </summary>
public static class AmqpEncoder
{
    /// <summary>
    /// Encodes a null value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeNull(Span<byte> buffer)
    {
        buffer[0] = FormatCode.Null;
        return 1;
    }

    /// <summary>
    /// Encodes a boolean value using the most compact representation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeBoolean(Span<byte> buffer, bool value)
    {
        buffer[0] = value ? FormatCode.BooleanTrue : FormatCode.BooleanFalse;
        return 1;
    }

    /// <summary>
    /// Encodes an unsigned byte (ubyte).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeUByte(Span<byte> buffer, byte value)
    {
        buffer[0] = FormatCode.UByte;
        buffer[1] = value;
        return 2;
    }

    /// <summary>
    /// Encodes a signed byte.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeByte(Span<byte> buffer, sbyte value)
    {
        buffer[0] = FormatCode.Byte;
        buffer[1] = (byte)value;
        return 2;
    }

    /// <summary>
    /// Encodes an unsigned short (ushort) in network byte order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeUShort(Span<byte> buffer, ushort value)
    {
        buffer[0] = FormatCode.UShort;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[1..], value);
        return 3;
    }

    /// <summary>
    /// Encodes a signed short in network byte order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeShort(Span<byte> buffer, short value)
    {
        buffer[0] = FormatCode.Short;
        BinaryPrimitives.WriteInt16BigEndian(buffer[1..], value);
        return 3;
    }

    /// <summary>
    /// Encodes an unsigned int using the most compact representation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeUInt(Span<byte> buffer, uint value)
    {
        if (value == 0)
        {
            buffer[0] = FormatCode.UInt0;
            return 1;
        }
        if (value <= 255)
        {
            buffer[0] = FormatCode.SmallUInt;
            buffer[1] = (byte)value;
            return 2;
        }
        buffer[0] = FormatCode.UInt;
        BinaryPrimitives.WriteUInt32BigEndian(buffer[1..], value);
        return 5;
    }

    /// <summary>
    /// Encodes a signed int using the most compact representation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeInt(Span<byte> buffer, int value)
    {
        if (value is >= -128 and <= 127)
        {
            buffer[0] = FormatCode.SmallInt;
            buffer[1] = (byte)value;
            return 2;
        }
        buffer[0] = FormatCode.Int;
        BinaryPrimitives.WriteInt32BigEndian(buffer[1..], value);
        return 5;
    }

    /// <summary>
    /// Encodes an unsigned long using the most compact representation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeULong(Span<byte> buffer, ulong value)
    {
        if (value == 0)
        {
            buffer[0] = FormatCode.ULong0;
            return 1;
        }
        if (value <= 255)
        {
            buffer[0] = FormatCode.SmallULong;
            buffer[1] = (byte)value;
            return 2;
        }
        buffer[0] = FormatCode.ULong;
        BinaryPrimitives.WriteUInt64BigEndian(buffer[1..], value);
        return 9;
    }

    /// <summary>
    /// Encodes a signed long using the most compact representation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeLong(Span<byte> buffer, long value)
    {
        if (value is >= -128 and <= 127)
        {
            buffer[0] = FormatCode.SmallLong;
            buffer[1] = (byte)value;
            return 2;
        }
        buffer[0] = FormatCode.Long;
        BinaryPrimitives.WriteInt64BigEndian(buffer[1..], value);
        return 9;
    }

    /// <summary>
    /// Encodes a float (IEEE 754 binary32).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeFloat(Span<byte> buffer, float value)
    {
        buffer[0] = FormatCode.Float;
        BinaryPrimitives.WriteSingleBigEndian(buffer[1..], value);
        return 5;
    }

    /// <summary>
    /// Encodes a double (IEEE 754 binary64).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeDouble(Span<byte> buffer, double value)
    {
        buffer[0] = FormatCode.Double;
        BinaryPrimitives.WriteDoubleBigEndian(buffer[1..], value);
        return 9;
    }

    /// <summary>
    /// Encodes a timestamp (milliseconds since Unix epoch).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeTimestamp(Span<byte> buffer, DateTimeOffset value)
    {
        buffer[0] = FormatCode.Timestamp;
        BinaryPrimitives.WriteInt64BigEndian(buffer[1..], value.ToUnixTimeMilliseconds());
        return 9;
    }

    /// <summary>
    /// Encodes a timestamp from raw milliseconds.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeTimestamp(Span<byte> buffer, long milliseconds)
    {
        buffer[0] = FormatCode.Timestamp;
        BinaryPrimitives.WriteInt64BigEndian(buffer[1..], milliseconds);
        return 9;
    }

    /// <summary>
    /// Encodes a UUID.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeUuid(Span<byte> buffer, Guid value)
    {
        buffer[0] = FormatCode.Uuid;
        WriteGuidBigEndian(buffer[1..], value);
        return 17;
    }

    /// <summary>
    /// Encodes a UTF-32BE character.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeChar(Span<byte> buffer, char value)
    {
        buffer[0] = FormatCode.Char;
        BinaryPrimitives.WriteUInt32BigEndian(buffer[1..], value);
        return 5;
    }

    /// <summary>
    /// Encodes binary data using the most compact representation.
    /// </summary>
    public static int EncodeBinary(Span<byte> buffer, ReadOnlySpan<byte> value)
    {
        if (value.Length <= 255)
        {
            buffer[0] = FormatCode.VBin8;
            buffer[1] = (byte)value.Length;
            value.CopyTo(buffer[2..]);
            return 2 + value.Length;
        }
        buffer[0] = FormatCode.VBin32;
        BinaryPrimitives.WriteUInt32BigEndian(buffer[1..], (uint)value.Length);
        value.CopyTo(buffer[5..]);
        return 5 + value.Length;
    }

    /// <summary>
    /// Encodes a UTF-8 string using the most compact representation.
    /// </summary>
    public static int EncodeString(Span<byte> buffer, ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            buffer[0] = FormatCode.Str8;
            buffer[1] = 0;
            return 2;
        }

        // Calculate UTF-8 byte count
        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount <= 255)
        {
            buffer[0] = FormatCode.Str8;
            buffer[1] = (byte)byteCount;
            Encoding.UTF8.GetBytes(value, buffer[2..]);
            return 2 + byteCount;
        }
        buffer[0] = FormatCode.Str32;
        BinaryPrimitives.WriteUInt32BigEndian(buffer[1..], (uint)byteCount);
        Encoding.UTF8.GetBytes(value, buffer[5..]);
        return 5 + byteCount;
    }

    /// <summary>
    /// Encodes a string from a string object.
    /// </summary>
    public static int EncodeString(Span<byte> buffer, string? value)
    {
        if (value is null)
        {
            return EncodeNull(buffer);
        }
        return EncodeString(buffer, value.AsSpan());
    }

    /// <summary>
    /// Encodes a symbol (ASCII string) using the most compact representation.
    /// </summary>
    public static int EncodeSymbol(Span<byte> buffer, ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            buffer[0] = FormatCode.Sym8;
            buffer[1] = 0;
            return 2;
        }

        // Symbols are ASCII, so byte count equals char count
        var byteCount = value.Length;
        if (byteCount <= 255)
        {
            buffer[0] = FormatCode.Sym8;
            buffer[1] = (byte)byteCount;
            Encoding.ASCII.GetBytes(value, buffer[2..]);
            return 2 + byteCount;
        }
        buffer[0] = FormatCode.Sym32;
        BinaryPrimitives.WriteUInt32BigEndian(buffer[1..], (uint)byteCount);
        Encoding.ASCII.GetBytes(value, buffer[5..]);
        return 5 + byteCount;
    }

    /// <summary>
    /// Encodes a symbol from a string object.
    /// </summary>
    public static int EncodeSymbol(Span<byte> buffer, string? value)
    {
        if (value is null)
        {
            return EncodeNull(buffer);
        }
        return EncodeSymbol(buffer, value.AsSpan());
    }

    /// <summary>
    /// Encodes a described type constructor (0x00) followed by the descriptor.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeDescriptor(Span<byte> buffer, ulong descriptorCode)
    {
        buffer[0] = FormatCode.Described;
        return 1 + EncodeULong(buffer[1..], descriptorCode);
    }

    /// <summary>
    /// Encodes a described type constructor with a symbolic descriptor.
    /// </summary>
    public static int EncodeDescriptor(Span<byte> buffer, string descriptorName)
    {
        buffer[0] = FormatCode.Described;
        return 1 + EncodeSymbol(buffer[1..], descriptorName);
    }

    /// <summary>
    /// Writes the list header (format code + size + count).
    /// Returns the offset where list items should be written.
    /// </summary>
    public static int EncodeListHeader(Span<byte> buffer, int size, int count)
    {
        if (count == 0)
        {
            buffer[0] = FormatCode.List0;
            return 1;
        }
        if (size <= 255 && count <= 255)
        {
            buffer[0] = FormatCode.List8;
            buffer[1] = (byte)size;
            buffer[2] = (byte)count;
            return 3;
        }
        buffer[0] = FormatCode.List32;
        BinaryPrimitives.WriteUInt32BigEndian(buffer[1..], (uint)size);
        BinaryPrimitives.WriteUInt32BigEndian(buffer[5..], (uint)count);
        return 9;
    }

    /// <summary>
    /// Writes the map header (format code + size + count).
    /// Count is the number of key-value pairs * 2.
    /// </summary>
    public static int EncodeMapHeader(Span<byte> buffer, int size, int count)
    {
        if (size <= 255 && count <= 255)
        {
            buffer[0] = FormatCode.Map8;
            buffer[1] = (byte)size;
            buffer[2] = (byte)count;
            return 3;
        }
        buffer[0] = FormatCode.Map32;
        BinaryPrimitives.WriteUInt32BigEndian(buffer[1..], (uint)size);
        BinaryPrimitives.WriteUInt32BigEndian(buffer[5..], (uint)count);
        return 9;
    }

    /// <summary>
    /// Writes the array header (format code + size + count).
    /// </summary>
    public static int EncodeArrayHeader(Span<byte> buffer, int size, int count)
    {
        if (size <= 255 && count <= 255)
        {
            buffer[0] = FormatCode.Array8;
            buffer[1] = (byte)size;
            buffer[2] = (byte)count;
            return 3;
        }
        buffer[0] = FormatCode.Array32;
        BinaryPrimitives.WriteUInt32BigEndian(buffer[1..], (uint)size);
        BinaryPrimitives.WriteUInt32BigEndian(buffer[5..], (uint)count);
        return 9;
    }

    /// <summary>
    /// Calculates the encoded size of a string without actually encoding it.
    /// </summary>
    public static int GetEncodedStringSize(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return 2; // format code + zero length
        }
        var byteCount = Encoding.UTF8.GetByteCount(value);
        return byteCount <= 255 ? 2 + byteCount : 5 + byteCount;
    }

    /// <summary>
    /// Calculates the encoded size of a symbol without actually encoding it.
    /// </summary>
    public static int GetEncodedSymbolSize(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return 2;
        }
        return value.Length <= 255 ? 2 + value.Length : 5 + value.Length;
    }

    /// <summary>
    /// Calculates the encoded size of binary data without actually encoding it.
    /// </summary>
    public static int GetEncodedBinarySize(int length) => length <= 255 ? 2 + length : 5 + length;

    /// <summary>
    /// Encodes a map with string keys and object values.
    /// Used for properties, annotations, and other AMQP maps.
    /// </summary>
    public static int EncodeMap(Span<byte> buffer, IReadOnlyDictionary<string, object?>? map)
    {
        if (map == null || map.Count == 0)
        {
            return EncodeNull(buffer);
        }

        // First pass: calculate body size
        Span<byte> bodyBuffer = stackalloc byte[4096];
        var bodySize = 0;
        var pairCount = 0;
        foreach (var kvp in map)
        {
            // Encode key as symbol
            bodySize += EncodeSymbol(bodyBuffer[bodySize..], kvp.Key);
            // Encode value
            bodySize += EncodeValue(bodyBuffer[bodySize..], kvp.Value);
            pairCount++;
        }
        var offset = 0;
        // Map header (count is number of items, which is pairs * 2)
        offset += EncodeMapHeader(buffer[offset..], bodySize, pairCount * 2);
        // Copy body
        bodyBuffer[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;
        return offset;
    }

    /// <summary>
    /// Encodes a map with symbol keys (for annotations).
    /// </summary>
    public static int EncodeSymbolMap(Span<byte> buffer, IReadOnlyDictionary<string, object?>? map) => EncodeMap(buffer, map);

    /// <summary>
    /// Encodes a symbol array (for capabilities, locales).
    /// </summary>
    public static int EncodeSymbolArray(Span<byte> buffer, string[]? values)
    {
        if (values == null || values.Length == 0)
        {
            return EncodeNull(buffer);
        }
        if (values.Length == 1)
        {
            return EncodeSymbol(buffer, values[0]);
        }

        // Calculate array body size
        var bodySize = 0;
        var useSmall = true;
        foreach (var value in values)
        {
            if (value.Length > 255)
            {
                useSmall = false;
            }
            bodySize += useSmall ? 1 + value.Length : 4 + value.Length;
        }
        var offset = 0;

        // Array header (size includes constructor byte)
        offset += EncodeArrayHeader(buffer[offset..], bodySize + 1, values.Length);

        // Element constructor
        buffer[offset++] = useSmall ? FormatCode.Sym8 : FormatCode.Sym32;

        // Elements (without individual format codes)
        foreach (var value in values)
        {
            if (useSmall)
            {
                buffer[offset++] = (byte)value.Length;
                Encoding.ASCII.GetBytes(value, buffer[offset..]);
                offset += value.Length;
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer[offset..], (uint)value.Length);
                offset += 4;
                Encoding.ASCII.GetBytes(value, buffer[offset..]);
                offset += value.Length;
            }
        }
        return offset;
    }

    /// <summary>
    /// Encodes a generic AMQP value based on its runtime type.
    /// </summary>
    public static int EncodeValue(Span<byte> buffer, object? value)
    {
        return value switch
        {
            null                                     => EncodeNull(buffer),
            bool b                                   => EncodeBoolean(buffer, b),
            byte ub                                  => EncodeUByte(buffer, ub),
            sbyte sb                                 => EncodeByte(buffer, sb),
            ushort us                                => EncodeUShort(buffer, us),
            short s                                  => EncodeShort(buffer, s),
            uint ui                                  => EncodeUInt(buffer, ui),
            int i                                    => EncodeInt(buffer, i),
            ulong ul                                 => EncodeULong(buffer, ul),
            long l                                   => EncodeLong(buffer, l),
            float f                                  => EncodeFloat(buffer, f),
            double d                                 => EncodeDouble(buffer, d),
            char c                                   => EncodeChar(buffer, c),
            Guid g                                   => EncodeUuid(buffer, g),
            DateTimeOffset dto                       => EncodeTimestamp(buffer, dto),
            DateTime dt                              => EncodeTimestamp(buffer, new DateTimeOffset(dt)),
            string str                               => EncodeString(buffer, str),
            byte[] bytes                             => EncodeBinary(buffer, bytes),
            ReadOnlyMemory<byte> mem                 => EncodeBinary(buffer, mem.Span),
            IReadOnlyDictionary<string, object?> map => EncodeMap(buffer, map),
            string[] symbols                         => EncodeSymbolArray(buffer, symbols),
            IReadOnlyList<object?> list              => EncodeList(buffer, list),
            _                                        => throw new ArgumentException($"Cannot encode value of type {value.GetType().Name}")
        };
    }

    /// <summary>
    /// Encodes a list of values.
    /// </summary>
    public static int EncodeList(Span<byte> buffer, IReadOnlyList<object?>? list)
    {
        if (list == null || list.Count == 0)
        {
            buffer[0] = FormatCode.List0;
            return 1;
        }

        // First pass: encode to temp buffer
        Span<byte> bodyBuffer = stackalloc byte[4096];
        var bodySize = 0;
        foreach (var item in list)
        {
            bodySize += EncodeValue(bodyBuffer[bodySize..], item);
        }
        var offset = 0;
        offset += EncodeListHeader(buffer[offset..], bodySize, list.Count);
        bodyBuffer[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;
        return offset;
    }

    /// <summary>
    /// Calculates the encoded size of a value without encoding it.
    /// </summary>
    public static int GetEncodedValueSize(object? value)
    {
        return value switch
        {
            null   => 1,
            bool   => 1,
            byte   => 2,
            sbyte  => 2,
            ushort => 3,
            short  => 3,
            uint ui => ui == 0
                           ? 1
                           : ui <= 255
                               ? 2
                               : 5,
            int i => i is >= -128 and <= 127 ? 2 : 5,
            ulong ul => ul == 0
                            ? 1
                            : ul <= 255
                                ? 2
                                : 9,
            long l         => l is >= -128 and <= 127 ? 2 : 9,
            float          => 5,
            double         => 9,
            char           => 5,
            Guid           => 17,
            DateTimeOffset => 9,
            DateTime       => 9,
            string str     => GetEncodedStringSize(str.AsSpan()),
            byte[] bytes   => GetEncodedBinarySize(bytes.Length),
            _              => throw new ArgumentException($"Cannot calculate size for type {value.GetType().Name}")
        };
    }

    /// <summary>
    /// Writes a GUID in big-endian format (RFC 4122).
    /// </summary>
    private static void WriteGuidBigEndian(Span<byte> buffer, Guid value)
    {
        // Get the raw bytes
        Span<byte> guidBytes = stackalloc byte[16];
        value.TryWriteBytes(guidBytes);

        // .NET stores GUIDs in little-endian for the first 3 components
        // RFC 4122 requires big-endian, so we need to swap
        // time_low (4 bytes)
        buffer[0] = guidBytes[3];
        buffer[1] = guidBytes[2];
        buffer[2] = guidBytes[1];
        buffer[3] = guidBytes[0];

        // time_mid (2 bytes)
        buffer[4] = guidBytes[5];
        buffer[5] = guidBytes[4];

        // time_hi_and_version (2 bytes)
        buffer[6] = guidBytes[7];
        buffer[7] = guidBytes[6];

        // clock_seq_hi_and_reserved, clock_seq_low, node (8 bytes) - already in correct order
        guidBytes[8..16].CopyTo(buffer[8..]);
    }
}
