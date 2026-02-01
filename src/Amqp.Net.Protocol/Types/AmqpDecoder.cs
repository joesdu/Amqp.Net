// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace Amqp.Net.Protocol.Types;

/// <summary>
/// High-performance AMQP type decoder using ReadOnlySpan and zero-copy patterns.
/// All methods read in network byte order (big-endian).
/// </summary>
public static class AmqpDecoder
{
    /// <summary>
    /// Peeks at the format code without advancing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte PeekFormatCode(ReadOnlySpan<byte> buffer) => buffer[0];

    /// <summary>
    /// Decodes a null value. Returns true if the value is null.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DecodeNull(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        if (buffer[0] == FormatCode.Null)
        {
            bytesConsumed = 1;
            return true;
        }
        bytesConsumed = 0;
        return false;
    }

    /// <summary>
    /// Decodes a boolean value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DecodeBoolean(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        switch (formatCode)
        {
            case FormatCode.BooleanTrue:
                bytesConsumed = 1;
                return true;
            case FormatCode.BooleanFalse:
                bytesConsumed = 1;
                return false;
            case FormatCode.Boolean:
                bytesConsumed = 2;
                return buffer[1] != 0;
            default:
                throw new AmqpDecodeException($"Expected boolean format code, got 0x{formatCode:X2}");
        }
    }

    /// <summary>
    /// Decodes an unsigned byte (ubyte).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte DecodeUByte(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode != FormatCode.UByte)
        {
            throw new AmqpDecodeException($"Expected ubyte format code 0x{FormatCode.UByte:X2}, got 0x{formatCode:X2}");
        }
        bytesConsumed = 2;
        return buffer[1];
    }

    /// <summary>
    /// Decodes a signed byte.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte DecodeByte(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode != FormatCode.Byte)
        {
            throw new AmqpDecodeException($"Expected byte format code 0x{FormatCode.Byte:X2}, got 0x{formatCode:X2}");
        }
        bytesConsumed = 2;
        return (sbyte)buffer[1];
    }

    /// <summary>
    /// Decodes an unsigned short (ushort).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort DecodeUShort(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode != FormatCode.UShort)
        {
            throw new AmqpDecodeException($"Expected ushort format code 0x{FormatCode.UShort:X2}, got 0x{formatCode:X2}");
        }
        bytesConsumed = 3;
        return BinaryPrimitives.ReadUInt16BigEndian(buffer[1..]);
    }

    /// <summary>
    /// Decodes a signed short.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short DecodeShort(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode != FormatCode.Short)
        {
            throw new AmqpDecodeException($"Expected short format code 0x{FormatCode.Short:X2}, got 0x{formatCode:X2}");
        }
        bytesConsumed = 3;
        return BinaryPrimitives.ReadInt16BigEndian(buffer[1..]);
    }

    /// <summary>
    /// Decodes an unsigned int from any valid encoding.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint DecodeUInt(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        switch (formatCode)
        {
            case FormatCode.UInt0:
                bytesConsumed = 1;
                return 0;
            case FormatCode.SmallUInt:
                bytesConsumed = 2;
                return buffer[1];
            case FormatCode.UInt:
                bytesConsumed = 5;
                return BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]);
            default:
                throw new AmqpDecodeException($"Expected uint format code, got 0x{formatCode:X2}");
        }
    }

    /// <summary>
    /// Decodes a signed int from any valid encoding.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DecodeInt(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        switch (formatCode)
        {
            case FormatCode.SmallInt:
                bytesConsumed = 2;
                return (sbyte)buffer[1];
            case FormatCode.Int:
                bytesConsumed = 5;
                return BinaryPrimitives.ReadInt32BigEndian(buffer[1..]);
            default:
                throw new AmqpDecodeException($"Expected int format code, got 0x{formatCode:X2}");
        }
    }

    /// <summary>
    /// Decodes an unsigned long from any valid encoding.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong DecodeULong(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        switch (formatCode)
        {
            case FormatCode.ULong0:
                bytesConsumed = 1;
                return 0;
            case FormatCode.SmallULong:
                bytesConsumed = 2;
                return buffer[1];
            case FormatCode.ULong:
                bytesConsumed = 9;
                return BinaryPrimitives.ReadUInt64BigEndian(buffer[1..]);
            default:
                throw new AmqpDecodeException($"Expected ulong format code, got 0x{formatCode:X2}");
        }
    }

    /// <summary>
    /// Decodes a signed long from any valid encoding.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long DecodeLong(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        switch (formatCode)
        {
            case FormatCode.SmallLong:
                bytesConsumed = 2;
                return (sbyte)buffer[1];
            case FormatCode.Long:
                bytesConsumed = 9;
                return BinaryPrimitives.ReadInt64BigEndian(buffer[1..]);
            default:
                throw new AmqpDecodeException($"Expected long format code, got 0x{formatCode:X2}");
        }
    }

    /// <summary>
    /// Decodes a float (IEEE 754 binary32).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DecodeFloat(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode != FormatCode.Float)
        {
            throw new AmqpDecodeException($"Expected float format code 0x{FormatCode.Float:X2}, got 0x{formatCode:X2}");
        }
        bytesConsumed = 5;
        return BinaryPrimitives.ReadSingleBigEndian(buffer[1..]);
    }

    /// <summary>
    /// Decodes a double (IEEE 754 binary64).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double DecodeDouble(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode != FormatCode.Double)
        {
            throw new AmqpDecodeException($"Expected double format code 0x{FormatCode.Double:X2}, got 0x{formatCode:X2}");
        }
        bytesConsumed = 9;
        return BinaryPrimitives.ReadDoubleBigEndian(buffer[1..]);
    }

    /// <summary>
    /// Decodes a timestamp as DateTimeOffset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset DecodeTimestamp(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode != FormatCode.Timestamp)
        {
            throw new AmqpDecodeException($"Expected timestamp format code 0x{FormatCode.Timestamp:X2}, got 0x{formatCode:X2}");
        }
        bytesConsumed = 9;
        var milliseconds = BinaryPrimitives.ReadInt64BigEndian(buffer[1..]);
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
    }

    /// <summary>
    /// Decodes a timestamp as raw milliseconds.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long DecodeTimestampMilliseconds(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode != FormatCode.Timestamp)
        {
            throw new AmqpDecodeException($"Expected timestamp format code 0x{FormatCode.Timestamp:X2}, got 0x{formatCode:X2}");
        }
        bytesConsumed = 9;
        return BinaryPrimitives.ReadInt64BigEndian(buffer[1..]);
    }

    /// <summary>
    /// Decodes a UUID.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid DecodeUuid(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode != FormatCode.Uuid)
        {
            throw new AmqpDecodeException($"Expected uuid format code 0x{FormatCode.Uuid:X2}, got 0x{formatCode:X2}");
        }
        bytesConsumed = 17;
        return ReadGuidBigEndian(buffer[1..]);
    }

    /// <summary>
    /// Decodes a UTF-32BE character.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char DecodeChar(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode != FormatCode.Char)
        {
            throw new AmqpDecodeException($"Expected char format code 0x{FormatCode.Char:X2}, got 0x{formatCode:X2}");
        }
        bytesConsumed = 5;
        var codePoint = BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]);
        if (codePoint > char.MaxValue)
        {
            throw new AmqpDecodeException($"Unicode code point 0x{codePoint:X} exceeds char range");
        }
        return (char)codePoint;
    }

    /// <summary>
    /// Decodes binary data, returning a span pointing into the original buffer.
    /// </summary>
    public static ReadOnlySpan<byte> DecodeBinary(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        switch (formatCode)
        {
            case FormatCode.Null:
                bytesConsumed = 1;
                return ReadOnlySpan<byte>.Empty;
            case FormatCode.VBin8:
            {
                int length = buffer[1];
                bytesConsumed = 2 + length;
                return buffer.Slice(2, length);
            }
            case FormatCode.VBin32:
            {
                var length = (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]);
                bytesConsumed = 5 + length;
                return buffer.Slice(5, length);
            }
            default:
                throw new AmqpDecodeException($"Expected binary format code, got 0x{formatCode:X2}");
        }
    }

    /// <summary>
    /// Decodes a UTF-8 string.
    /// </summary>
    public static string DecodeString(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        switch (formatCode)
        {
            case FormatCode.Null:
                bytesConsumed = 1;
                return null!;
            case FormatCode.Str8:
            {
                int length = buffer[1];
                bytesConsumed = 2 + length;
                if (length == 0)
                {
                    return string.Empty;
                }
                return Encoding.UTF8.GetString(buffer.Slice(2, length));
            }
            case FormatCode.Str32:
            {
                var length = (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]);
                bytesConsumed = 5 + length;
                if (length == 0)
                {
                    return string.Empty;
                }
                return Encoding.UTF8.GetString(buffer.Slice(5, length));
            }
            default:
                throw new AmqpDecodeException($"Expected string format code, got 0x{formatCode:X2}");
        }
    }

    /// <summary>
    /// Decodes a symbol (ASCII string).
    /// </summary>
    public static string DecodeSymbol(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        switch (formatCode)
        {
            case FormatCode.Null:
                bytesConsumed = 1;
                return null!;
            case FormatCode.Sym8:
            {
                int length = buffer[1];
                bytesConsumed = 2 + length;
                if (length == 0)
                {
                    return string.Empty;
                }
                return Encoding.ASCII.GetString(buffer.Slice(2, length));
            }
            case FormatCode.Sym32:
            {
                var length = (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]);
                bytesConsumed = 5 + length;
                if (length == 0)
                {
                    return string.Empty;
                }
                return Encoding.ASCII.GetString(buffer.Slice(5, length));
            }
            default:
                throw new AmqpDecodeException($"Expected symbol format code, got 0x{formatCode:X2}");
        }
    }

    /// <summary>
    /// Decodes a list header, returning the size and count.
    /// </summary>
    public static (int Size, int Count) DecodeListHeader(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        switch (formatCode)
        {
            case FormatCode.List0:
                bytesConsumed = 1;
                return (0, 0);
            case FormatCode.List8:
                bytesConsumed = 3;
                return (buffer[1], buffer[2]);
            case FormatCode.List32:
                bytesConsumed = 9;
                return (
                           (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]),
                           (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[5..])
                       );
            default:
                throw new AmqpDecodeException($"Expected list format code, got 0x{formatCode:X2}");
        }
    }

    /// <summary>
    /// Decodes a map header, returning the size and count.
    /// </summary>
    public static (int Size, int Count) DecodeMapHeader(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        switch (formatCode)
        {
            case FormatCode.Map8:
                bytesConsumed = 3;
                return (buffer[1], buffer[2]);
            case FormatCode.Map32:
                bytesConsumed = 9;
                return (
                           (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]),
                           (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[5..])
                       );
            default:
                throw new AmqpDecodeException($"Expected map format code, got 0x{formatCode:X2}");
        }
    }

    /// <summary>
    /// Decodes an array header, returning the size and count.
    /// </summary>
    public static (int Size, int Count) DecodeArrayHeader(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        switch (formatCode)
        {
            case FormatCode.Array8:
                bytesConsumed = 3;
                return (buffer[1], buffer[2]);
            case FormatCode.Array32:
                bytesConsumed = 9;
                return (
                           (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]),
                           (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[5..])
                       );
            default:
                throw new AmqpDecodeException($"Expected array format code, got 0x{formatCode:X2}");
        }
    }

    /// <summary>
    /// Decodes a described type, returning the descriptor code and the offset to the value.
    /// </summary>
    public static ulong DecodeDescriptor(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        if (buffer[0] != FormatCode.Described)
        {
            throw new AmqpDecodeException($"Expected described format code 0x00, got 0x{buffer[0]:X2}");
        }

        // The descriptor follows the 0x00 byte
        var descriptor = DecodeULong(buffer[1..], out var descriptorSize);
        bytesConsumed = 1 + descriptorSize;
        return descriptor;
    }

    /// <summary>
    /// Decodes a map with string/symbol keys and object values.
    /// </summary>
    public static Dictionary<string, object?>? DecodeMap(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode == FormatCode.Null)
        {
            bytesConsumed = 1;
            return null;
        }
        var (size, count) = DecodeMapHeader(buffer, out var headerSize);
        bytesConsumed = headerSize + size;
        if (count == 0)
        {
            return new();
        }
        var map = new Dictionary<string, object?>(count / 2);
        var offset = headerSize;
        var remaining = count;
        while (remaining > 0)
        {
            // Decode key (symbol or string)
            var key = DecodeStringOrSymbol(buffer[offset..], out var keySize);
            offset += keySize;
            remaining--;

            // Decode value
            var value = DecodeValue(buffer[offset..], out var valueSize);
            offset += valueSize;
            remaining--;
            map[key] = value;
        }
        return map;
    }

    /// <summary>
    /// Decodes a symbol array (for capabilities, locales).
    /// </summary>
    public static string[]? DecodeSymbolArray(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode == FormatCode.Null)
        {
            bytesConsumed = 1;
            return null;
        }

        // Single symbol (not array)
        if (formatCode == FormatCode.Sym8 || formatCode == FormatCode.Sym32)
        {
            var single = DecodeSymbol(buffer, out bytesConsumed);
            return [single];
        }

        // Array
        if (formatCode != FormatCode.Array8 && formatCode != FormatCode.Array32)
        {
            throw new AmqpDecodeException($"Expected array or symbol format code, got 0x{formatCode:X2}");
        }
        var (size, count) = DecodeArrayHeader(buffer, out var headerSize);
        bytesConsumed = headerSize + size;
        if (count == 0)
        {
            return [];
        }
        var offset = headerSize;

        // Read element constructor
        var elementCode = buffer[offset++];
        var result = new string[count];
        for (var i = 0; i < count; i++)
        {
            if (elementCode == FormatCode.Sym8)
            {
                int length = buffer[offset++];
                result[i] = length == 0 ? string.Empty : Encoding.ASCII.GetString(buffer.Slice(offset, length));
                offset += length;
            }
            else if (elementCode == FormatCode.Sym32)
            {
                var length = (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[offset..]);
                offset += 4;
                result[i] = length == 0 ? string.Empty : Encoding.ASCII.GetString(buffer.Slice(offset, length));
                offset += length;
            }
            else
            {
                throw new AmqpDecodeException($"Expected symbol element constructor, got 0x{elementCode:X2}");
            }
        }
        return result;
    }

    /// <summary>
    /// Decodes either a string or symbol.
    /// </summary>
    public static string DecodeStringOrSymbol(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        return formatCode switch
        {
            FormatCode.Null                     => DecodeNullString(out bytesConsumed),
            FormatCode.Str8 or FormatCode.Str32 => DecodeString(buffer, out bytesConsumed),
            FormatCode.Sym8 or FormatCode.Sym32 => DecodeSymbol(buffer, out bytesConsumed),
            _                                   => throw new AmqpDecodeException($"Expected string or symbol format code, got 0x{formatCode:X2}")
        };

        static string DecodeNullString(out int consumed)
        {
            consumed = 1;
            return null!;
        }
    }

    /// <summary>
    /// Decodes a generic AMQP value based on its format code.
    /// </summary>
    public static object? DecodeValue(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];

        // Handle described types
        if (formatCode == FormatCode.Described)
        {
            // Skip descriptor and decode value
            _ = DecodeDescriptor(buffer, out var descSize);
            var value = DecodeValue(buffer[descSize..], out var valueSize);
            bytesConsumed = descSize + valueSize;
            return value;
        }
        return formatCode switch
        {
            // Null
            FormatCode.Null => DecodeNullValue(out bytesConsumed),

            // Boolean
            FormatCode.BooleanTrue  => DecodeTrueValue(out bytesConsumed),
            FormatCode.BooleanFalse => DecodeFalseValue(out bytesConsumed),
            FormatCode.Boolean      => DecodeBoxedBoolean(buffer, out bytesConsumed),

            // Integers
            FormatCode.UByte      => DecodeBoxedUByte(buffer, out bytesConsumed),
            FormatCode.Byte       => DecodeBoxedByte(buffer, out bytesConsumed),
            FormatCode.UShort     => DecodeBoxedUShort(buffer, out bytesConsumed),
            FormatCode.Short      => DecodeBoxedShort(buffer, out bytesConsumed),
            FormatCode.UInt0      => DecodeUInt0Value(out bytesConsumed),
            FormatCode.SmallUInt  => DecodeBoxedSmallUInt(buffer, out bytesConsumed),
            FormatCode.UInt       => DecodeBoxedUInt(buffer, out bytesConsumed),
            FormatCode.SmallInt   => DecodeBoxedSmallInt(buffer, out bytesConsumed),
            FormatCode.Int        => DecodeBoxedInt(buffer, out bytesConsumed),
            FormatCode.ULong0     => DecodeULong0Value(out bytesConsumed),
            FormatCode.SmallULong => DecodeBoxedSmallULong(buffer, out bytesConsumed),
            FormatCode.ULong      => DecodeBoxedULong(buffer, out bytesConsumed),
            FormatCode.SmallLong  => DecodeBoxedSmallLong(buffer, out bytesConsumed),
            FormatCode.Long       => DecodeBoxedLong(buffer, out bytesConsumed),

            // Floating point
            FormatCode.Float  => DecodeBoxedFloat(buffer, out bytesConsumed),
            FormatCode.Double => DecodeBoxedDouble(buffer, out bytesConsumed),

            // Other fixed types
            FormatCode.Char      => DecodeBoxedChar(buffer, out bytesConsumed),
            FormatCode.Timestamp => DecodeBoxedTimestamp(buffer, out bytesConsumed),
            FormatCode.Uuid      => DecodeBoxedUuid(buffer, out bytesConsumed),

            // Variable width
            FormatCode.VBin8 or FormatCode.VBin32 => DecodeBinary(buffer, out bytesConsumed).ToArray(),
            FormatCode.Str8 or FormatCode.Str32   => DecodeString(buffer, out bytesConsumed),
            FormatCode.Sym8 or FormatCode.Sym32   => DecodeSymbol(buffer, out bytesConsumed),

            // Compound
            FormatCode.List0 or FormatCode.List8 or FormatCode.List32 => DecodeList(buffer, out bytesConsumed),
            FormatCode.Map8 or FormatCode.Map32                       => DecodeMap(buffer, out bytesConsumed),
            FormatCode.Array8 or FormatCode.Array32                   => DecodeArray(buffer, out bytesConsumed),
            _                                                         => throw new AmqpDecodeException($"Unknown format code 0x{formatCode:X2}")
        };
    }

    /// <summary>
    /// Decodes a list of values.
    /// </summary>
    public static List<object?>? DecodeList(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode == FormatCode.Null)
        {
            bytesConsumed = 1;
            return null;
        }
        if (formatCode == FormatCode.List0)
        {
            bytesConsumed = 1;
            return [];
        }
        var (size, count) = DecodeListHeader(buffer, out var headerSize);
        bytesConsumed = headerSize + size;
        var list = new List<object?>(count);
        var offset = headerSize;
        for (var i = 0; i < count; i++)
        {
            var value = DecodeValue(buffer[offset..], out var valueSize);
            offset += valueSize;
            list.Add(value);
        }
        return list;
    }

    /// <summary>
    /// Decodes an array of values.
    /// </summary>
    public static object[]? DecodeArray(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var formatCode = buffer[0];
        if (formatCode == FormatCode.Null)
        {
            bytesConsumed = 1;
            return null;
        }
        var (size, count) = DecodeArrayHeader(buffer, out var headerSize);
        bytesConsumed = headerSize + size;
        if (count == 0)
        {
            return [];
        }
        var offset = headerSize;

        // Read element constructor
        var elementCode = buffer[offset];
        var result = new object[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = DecodeValue(buffer[offset..], out var valueSize)!;
            offset += valueSize;
        }
        return result;
    }

    // Helper methods for boxed value decoding
    private static object? DecodeNullValue(out int bytesConsumed)
    {
        bytesConsumed = 1;
        return null;
    }

    private static object DecodeTrueValue(out int bytesConsumed)
    {
        bytesConsumed = 1;
        return true;
    }

    private static object DecodeFalseValue(out int bytesConsumed)
    {
        bytesConsumed = 1;
        return false;
    }

    private static object DecodeBoxedBoolean(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 2;
        return buffer[1] != 0;
    }

    private static object DecodeBoxedUByte(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 2;
        return buffer[1];
    }

    private static object DecodeBoxedByte(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 2;
        return (sbyte)buffer[1];
    }

    private static object DecodeBoxedUShort(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 3;
        return BinaryPrimitives.ReadUInt16BigEndian(buffer[1..]);
    }

    private static object DecodeBoxedShort(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 3;
        return BinaryPrimitives.ReadInt16BigEndian(buffer[1..]);
    }

    private static object DecodeUInt0Value(out int bytesConsumed)
    {
        bytesConsumed = 1;
        return 0u;
    }

    private static object DecodeBoxedSmallUInt(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 2;
        return (uint)buffer[1];
    }

    private static object DecodeBoxedUInt(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 5;
        return BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]);
    }

    private static object DecodeBoxedSmallInt(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 2;
        return (int)(sbyte)buffer[1];
    }

    private static object DecodeBoxedInt(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 5;
        return BinaryPrimitives.ReadInt32BigEndian(buffer[1..]);
    }

    private static object DecodeULong0Value(out int bytesConsumed)
    {
        bytesConsumed = 1;
        return 0ul;
    }

    private static object DecodeBoxedSmallULong(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 2;
        return (ulong)buffer[1];
    }

    private static object DecodeBoxedULong(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 9;
        return BinaryPrimitives.ReadUInt64BigEndian(buffer[1..]);
    }

    private static object DecodeBoxedSmallLong(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 2;
        return (long)(sbyte)buffer[1];
    }

    private static object DecodeBoxedLong(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 9;
        return BinaryPrimitives.ReadInt64BigEndian(buffer[1..]);
    }

    private static object DecodeBoxedFloat(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 5;
        return BinaryPrimitives.ReadSingleBigEndian(buffer[1..]);
    }

    private static object DecodeBoxedDouble(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 9;
        return BinaryPrimitives.ReadDoubleBigEndian(buffer[1..]);
    }

    private static object DecodeBoxedChar(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 5;
        return (char)BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]);
    }

    private static object DecodeBoxedTimestamp(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 9;
        return DateTimeOffset.FromUnixTimeMilliseconds(BinaryPrimitives.ReadInt64BigEndian(buffer[1..]));
    }

    private static object DecodeBoxedUuid(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        bytesConsumed = 17;
        return ReadGuidBigEndian(buffer[1..]);
    }

    /// <summary>
    /// Skips over a value without fully decoding it.
    /// </summary>
    public static int SkipValue(ReadOnlySpan<byte> buffer)
    {
        var formatCode = buffer[0];

        // Handle described types
        if (formatCode == FormatCode.Described)
        {
            var descriptorSize = SkipValue(buffer[1..]);
            var valueSize = SkipValue(buffer[(1 + descriptorSize)..]);
            return 1 + descriptorSize + valueSize;
        }
        var category = FormatCode.GetCategory(formatCode);
        return category switch
        {
            FormatCategory.Fixed    => 1 + FormatCode.GetFixedWidth(formatCode),
            FormatCategory.Variable => SkipVariableWidth(buffer),
            FormatCategory.Compound => SkipCompound(buffer),
            FormatCategory.Array    => SkipArray(buffer),
            _                       => throw new AmqpDecodeException($"Unknown format code 0x{formatCode:X2}")
        };
    }

    private static int SkipVariableWidth(ReadOnlySpan<byte> buffer)
    {
        var formatCode = buffer[0];
        var sizeWidth = FormatCode.GetSizeWidth(formatCode);
        var size = sizeWidth == 1
                       ? buffer[1]
                       : (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]);
        return 1 + sizeWidth + size;
    }

    private static int SkipCompound(ReadOnlySpan<byte> buffer)
    {
        var formatCode = buffer[0];
        var sizeWidth = FormatCode.GetSizeWidth(formatCode);
        var size = sizeWidth == 1
                       ? buffer[1]
                       : (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]);

        // Size includes the count field
        return 1 + sizeWidth + size;
    }

    private static int SkipArray(ReadOnlySpan<byte> buffer)
    {
        var formatCode = buffer[0];
        var sizeWidth = FormatCode.GetSizeWidth(formatCode);
        var size = sizeWidth == 1
                       ? buffer[1]
                       : (int)BinaryPrimitives.ReadUInt32BigEndian(buffer[1..]);

        // Size includes count and element constructor
        return 1 + sizeWidth + size;
    }

    /// <summary>
    /// Reads a GUID from big-endian format (RFC 4122).
    /// </summary>
    private static Guid ReadGuidBigEndian(ReadOnlySpan<byte> buffer)
    {
        Span<byte> guidBytes = stackalloc byte[16];

        // Swap time_low (4 bytes) from big-endian to little-endian
        guidBytes[0] = buffer[3];
        guidBytes[1] = buffer[2];
        guidBytes[2] = buffer[1];
        guidBytes[3] = buffer[0];

        // Swap time_mid (2 bytes)
        guidBytes[4] = buffer[5];
        guidBytes[5] = buffer[4];

        // Swap time_hi_and_version (2 bytes)
        guidBytes[6] = buffer[7];
        guidBytes[7] = buffer[6];

        // clock_seq_hi_and_reserved, clock_seq_low, node (8 bytes) - copy as-is
        buffer[8..16].CopyTo(guidBytes[8..]);
        return new(guidBytes);
    }
}

/// <summary>
/// Exception thrown when AMQP decoding fails.
/// </summary>
public class AmqpDecodeException : Exception
{
    public AmqpDecodeException(string message) : base(message) { }
    public AmqpDecodeException(string message, Exception innerException) : base(message, innerException) { }
}
