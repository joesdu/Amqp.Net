// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Protocol.Types;

/// <summary>
/// AMQP 1.0 format codes for primitive type encodings.
/// Based on OASIS AMQP 1.0 specification Part 1: Types.
/// </summary>
public static class FormatCode
{
    // ============================================
    // Fixed Width - Zero octets (0x4X)
    // ============================================

    /// <summary>The null value (0x40)</summary>
    public const byte Null = 0x40;

    /// <summary>Boolean true (0x41)</summary>
    public const byte BooleanTrue = 0x41;

    /// <summary>Boolean false (0x42)</summary>
    public const byte BooleanFalse = 0x42;

    /// <summary>Unsigned int zero (0x43)</summary>
    public const byte UInt0 = 0x43;

    /// <summary>Unsigned long zero (0x44)</summary>
    public const byte ULong0 = 0x44;

    /// <summary>Empty list (0x45)</summary>
    public const byte List0 = 0x45;

    // ============================================
    // Fixed Width - One octet (0x5X)
    // ============================================

    /// <summary>8-bit unsigned integer (0x50)</summary>
    public const byte UByte = 0x50;

    /// <summary>8-bit signed integer (0x51)</summary>
    public const byte Byte = 0x51;

    /// <summary>Unsigned int in range 0-255 (0x52)</summary>
    public const byte SmallUInt = 0x52;

    /// <summary>Unsigned long in range 0-255 (0x53)</summary>
    public const byte SmallULong = 0x53;

    /// <summary>Signed int in range -128 to 127 (0x54)</summary>
    public const byte SmallInt = 0x54;

    /// <summary>Signed long in range -128 to 127 (0x55)</summary>
    public const byte SmallLong = 0x55;

    /// <summary>Boolean with octet value (0x56)</summary>
    public const byte Boolean = 0x56;

    // ============================================
    // Fixed Width - Two octets (0x6X)
    // ============================================

    /// <summary>16-bit unsigned integer (0x60)</summary>
    public const byte UShort = 0x60;

    /// <summary>16-bit signed integer (0x61)</summary>
    public const byte Short = 0x61;

    // ============================================
    // Fixed Width - Four octets (0x7X)
    // ============================================

    /// <summary>32-bit unsigned integer (0x70)</summary>
    public const byte UInt = 0x70;

    /// <summary>32-bit signed integer (0x71)</summary>
    public const byte Int = 0x71;

    /// <summary>IEEE 754 binary32 float (0x72)</summary>
    public const byte Float = 0x72;

    /// <summary>UTF-32BE Unicode character (0x73)</summary>
    public const byte Char = 0x73;

    /// <summary>IEEE 754 decimal32 (0x74)</summary>
    public const byte Decimal32 = 0x74;

    // ============================================
    // Fixed Width - Eight octets (0x8X)
    // ============================================

    /// <summary>64-bit unsigned integer (0x80)</summary>
    public const byte ULong = 0x80;

    /// <summary>64-bit signed integer (0x81)</summary>
    public const byte Long = 0x81;

    /// <summary>IEEE 754 binary64 double (0x82)</summary>
    public const byte Double = 0x82;

    /// <summary>64-bit timestamp (ms since Unix epoch) (0x83)</summary>
    public const byte Timestamp = 0x83;

    /// <summary>IEEE 754 decimal64 (0x84)</summary>
    public const byte Decimal64 = 0x84;

    // ============================================
    // Fixed Width - Sixteen octets (0x9X)
    // ============================================

    /// <summary>IEEE 754 decimal128 (0x94)</summary>
    public const byte Decimal128 = 0x94;

    /// <summary>UUID (RFC 4122 section 4.1.2) (0x98)</summary>
    public const byte Uuid = 0x98;

    // ============================================
    // Variable Width - One octet size (0xAX)
    // ============================================

    /// <summary>Binary data up to 255 bytes (0xA0)</summary>
    public const byte VBin8 = 0xA0;

    /// <summary>UTF-8 string up to 255 bytes (0xA1)</summary>
    public const byte Str8 = 0xA1;

    /// <summary>Symbol (ASCII) up to 255 bytes (0xA3)</summary>
    public const byte Sym8 = 0xA3;

    // ============================================
    // Variable Width - Four octet size (0xBX)
    // ============================================

    /// <summary>Binary data up to 2^32-1 bytes (0xB0)</summary>
    public const byte VBin32 = 0xB0;

    /// <summary>UTF-8 string up to 2^32-1 bytes (0xB1)</summary>
    public const byte Str32 = 0xB1;

    /// <summary>Symbol (ASCII) up to 2^32-1 bytes (0xB3)</summary>
    public const byte Sym32 = 0xB3;

    // ============================================
    // Compound - One octet size/count (0xCX)
    // ============================================

    /// <summary>List with 8-bit size and count (0xC0)</summary>
    public const byte List8 = 0xC0;

    /// <summary>Map with 8-bit size and count (0xC1)</summary>
    public const byte Map8 = 0xC1;

    // ============================================
    // Compound - Four octet size/count (0xDX)
    // ============================================

    /// <summary>List with 32-bit size and count (0xD0)</summary>
    public const byte List32 = 0xD0;

    /// <summary>Map with 32-bit size and count (0xD1)</summary>
    public const byte Map32 = 0xD1;

    // ============================================
    // Array - One octet size/count (0xEX)
    // ============================================

    /// <summary>Array with 8-bit size and count (0xE0)</summary>
    public const byte Array8 = 0xE0;

    // ============================================
    // Array - Four octet size/count (0xFX)
    // ============================================

    /// <summary>Array with 32-bit size and count (0xF0)</summary>
    public const byte Array32 = 0xF0;

    // ============================================
    // Special
    // ============================================

    /// <summary>Described type constructor (0x00)</summary>
    public const byte Described = 0x00;

    /// <summary>
    /// Gets the category of a format code.
    /// </summary>
    public static FormatCategory GetCategory(byte formatCode)
    {
        return (formatCode >> 4) switch
        {
            0x4                              => FormatCategory.Fixed,    // Zero width
            0x5                              => FormatCategory.Fixed,    // One byte
            0x6                              => FormatCategory.Fixed,    // Two bytes
            0x7                              => FormatCategory.Fixed,    // Four bytes
            0x8                              => FormatCategory.Fixed,    // Eight bytes
            0x9                              => FormatCategory.Fixed,    // Sixteen bytes
            0xA                              => FormatCategory.Variable, // One byte size
            0xB                              => FormatCategory.Variable, // Four byte size
            0xC                              => FormatCategory.Compound, // One byte size/count
            0xD                              => FormatCategory.Compound, // Four byte size/count
            0xE                              => FormatCategory.Array,    // One byte size/count
            0xF                              => FormatCategory.Array,    // Four byte size/count
            0x0 when formatCode == Described => FormatCategory.Described,
            _                                => FormatCategory.Unknown
        };
    }

    /// <summary>
    /// Gets the fixed width in bytes for a format code, or -1 if not fixed width.
    /// </summary>
    public static int GetFixedWidth(byte formatCode)
    {
        return (formatCode >> 4) switch
        {
            0x4 => 0,
            0x5 => 1,
            0x6 => 2,
            0x7 => 4,
            0x8 => 8,
            0x9 => 16,
            _   => -1
        };
    }

    /// <summary>
    /// Gets the size prefix width for variable/compound/array format codes.
    /// </summary>
    public static int GetSizeWidth(byte formatCode)
    {
        return (formatCode >> 4) switch
        {
            0xA or 0xC or 0xE => 1,
            0xB or 0xD or 0xF => 4,
            _                 => 0
        };
    }
}

/// <summary>
/// Categories of AMQP format codes.
/// </summary>
public enum FormatCategory
{
    /// <summary>Unknown format code</summary>
    Unknown = 0,

    /// <summary>Fixed width encoding (0-16 bytes)</summary>
    Fixed = 1,

    /// <summary>Variable width encoding (size prefix + data)</summary>
    Variable = 2,

    /// <summary>Compound encoding (size + count + items)</summary>
    Compound = 3,

    /// <summary>Array encoding (size + count + constructor + elements)</summary>
    Array = 4,

    /// <summary>Described type (descriptor + value)</summary>
    Described = 5
}
