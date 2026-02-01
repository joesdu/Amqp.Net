// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Amqp.Net.Protocol.Framing;

/// <summary>
/// AMQP 1.0 frame header structure.
/// 8 bytes: SIZE (4) + DOFF (1) + TYPE (1) + CHANNEL (2)
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct FrameHeader
{
    /// <summary>
    /// Size of the frame header in bytes.
    /// </summary>
    public const int Size = 8;

    /// <summary>
    /// Minimum valid frame size (header only).
    /// </summary>
    public const int MinFrameSize = 8;

    /// <summary>
    /// Minimum value for max-frame-size that all peers must accept.
    /// </summary>
    public const int MinMaxFrameSize = 512;

    /// <summary>
    /// Default max-frame-size if not negotiated.
    /// </summary>
    public const uint DefaultMaxFrameSize = uint.MaxValue;

    /// <summary>
    /// AMQP frame type code.
    /// </summary>
    public const byte AmqpFrameType = 0x00;

    /// <summary>
    /// SASL frame type code.
    /// </summary>
    public const byte SaslFrameType = 0x01;

    /// <summary>
    /// Total frame size including header.
    /// </summary>
    public readonly uint FrameSize;

    /// <summary>
    /// Data offset in 4-byte words (minimum 2).
    /// </summary>
    public readonly byte DataOffset;

    /// <summary>
    /// Frame type (0x00 = AMQP, 0x01 = SASL).
    /// </summary>
    public readonly byte FrameType;

    /// <summary>
    /// Channel number (AMQP frames only).
    /// </summary>
    public readonly ushort Channel;

    /// <summary>
    /// Creates a new frame header.
    /// </summary>
    public FrameHeader(uint frameSize, byte dataOffset, byte frameType, ushort channel)
    {
        FrameSize = frameSize;
        DataOffset = dataOffset;
        FrameType = frameType;
        Channel = channel;
    }

    /// <summary>
    /// Gets the offset to the frame body in bytes.
    /// </summary>
    public int BodyOffset => DataOffset * 4;

    /// <summary>
    /// Gets the size of the frame body in bytes.
    /// </summary>
    public int BodySize => (int)FrameSize - BodyOffset;

    /// <summary>
    /// Gets the size of the extended header in bytes.
    /// </summary>
    public int ExtendedHeaderSize => BodyOffset - Size;

    /// <summary>
    /// Returns true if this is an AMQP frame.
    /// </summary>
    public bool IsAmqpFrame => FrameType == AmqpFrameType;

    /// <summary>
    /// Returns true if this is a SASL frame.
    /// </summary>
    public bool IsSaslFrame => FrameType == SaslFrameType;

    /// <summary>
    /// Returns true if this is an empty frame (heartbeat).
    /// </summary>
    public bool IsEmpty => BodySize == 0;

    /// <summary>
    /// Validates the frame header.
    /// </summary>
    public bool IsValid => FrameSize >= MinFrameSize && DataOffset >= 2 && BodyOffset <= FrameSize;

    /// <summary>
    /// Creates an AMQP frame header.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FrameHeader CreateAmqp(uint frameSize, ushort channel, byte dataOffset = 2)
    {
        return new FrameHeader(frameSize, dataOffset, AmqpFrameType, channel);
    }

    /// <summary>
    /// Creates a SASL frame header.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FrameHeader CreateSasl(uint frameSize, byte dataOffset = 2)
    {
        return new FrameHeader(frameSize, dataOffset, SaslFrameType, 0);
    }

    /// <summary>
    /// Creates an empty AMQP frame (heartbeat).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FrameHeader CreateHeartbeat(ushort channel = 0)
    {
        return new FrameHeader(Size, 2, AmqpFrameType, channel);
    }

    /// <summary>
    /// Reads a frame header from a buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FrameHeader Read(ReadOnlySpan<byte> buffer)
    {
        uint frameSize = BinaryPrimitives.ReadUInt32BigEndian(buffer);
        byte dataOffset = buffer[4];
        byte frameType = buffer[5];
        ushort channel = BinaryPrimitives.ReadUInt16BigEndian(buffer[6..]);
        
        return new FrameHeader(frameSize, dataOffset, frameType, channel);
    }

    /// <summary>
    /// Tries to read a frame header from a buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryRead(ReadOnlySpan<byte> buffer, out FrameHeader header)
    {
        if (buffer.Length < Size)
        {
            header = default;
            return false;
        }

        header = Read(buffer);
        return true;
    }

    /// <summary>
    /// Writes the frame header to a buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Span<byte> buffer)
    {
        BinaryPrimitives.WriteUInt32BigEndian(buffer, FrameSize);
        buffer[4] = DataOffset;
        buffer[5] = FrameType;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[6..], Channel);
    }

    /// <summary>
    /// Returns a string representation of the frame header.
    /// </summary>
    public override string ToString()
    {
        string type = FrameType switch
        {
            AmqpFrameType => "AMQP",
            SaslFrameType => "SASL",
            _ => $"0x{FrameType:X2}"
        };
        
        return $"Frame[Size={FrameSize}, DOFF={DataOffset}, Type={type}, Channel={Channel}]";
    }
}
