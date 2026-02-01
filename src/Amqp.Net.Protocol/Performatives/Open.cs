// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Text;
using Amqp.Net.Protocol.Types;

namespace Amqp.Net.Protocol.Performatives;

/// <summary>
/// AMQP Open performative - negotiates connection parameters.
/// Descriptor: 0x00000000:0x00000010
/// </summary>
/// <remarks>
/// Fields:
/// 0. container-id (string, mandatory) - unique container identifier
/// 1. hostname (string) - virtual host name
/// 2. max-frame-size (uint, default 4294967295) - maximum frame size
/// 3. channel-max (ushort, default 65535) - maximum channel number
/// 4. idle-time-out (milliseconds) - idle timeout in milliseconds
/// 5. outgoing-locales (symbol[]) - supported locales for outgoing text
/// 6. incoming-locales (symbol[]) - desired locales for incoming text
/// 7. offered-capabilities (symbol[]) - capabilities offered
/// 8. desired-capabilities (symbol[]) - capabilities desired
/// 9. properties (fields) - connection properties
/// </remarks>
public sealed record Open : PerformativeBase
{
    /// <inheritdoc />
    public override ulong DescriptorCode => Descriptor.Open;

    /// <summary>
    /// The container identifier (mandatory).
    /// </summary>
    public required string ContainerId { get; init; }

    /// <summary>
    /// The virtual host name.
    /// </summary>
    public string? Hostname { get; init; }

    /// <summary>
    /// Maximum frame size (default: uint.MaxValue).
    /// </summary>
    public uint MaxFrameSize { get; init; } = uint.MaxValue;

    /// <summary>
    /// Maximum channel number (default: 65535).
    /// </summary>
    public ushort ChannelMax { get; init; } = ushort.MaxValue;

    /// <summary>
    /// Idle timeout in milliseconds (null = no timeout).
    /// </summary>
    public uint? IdleTimeOut { get; init; }

    /// <summary>
    /// Supported locales for outgoing text.
    /// </summary>
    public string[]? OutgoingLocales { get; init; }

    /// <summary>
    /// Desired locales for incoming text.
    /// </summary>
    public string[]? IncomingLocales { get; init; }

    /// <summary>
    /// Capabilities offered by this peer.
    /// </summary>
    public string[]? OfferedCapabilities { get; init; }

    /// <summary>
    /// Capabilities desired from the remote peer.
    /// </summary>
    public string[]? DesiredCapabilities { get; init; }

    /// <summary>
    /// Connection properties.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Properties { get; init; }

    /// <inheritdoc />
    public override int Encode(Span<byte> buffer)
    {
        var offset = 0;

        // Described type constructor
        buffer[offset++] = FormatCode.Described;

        // Descriptor
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);

        // Calculate field count (trailing nulls can be omitted)
        var fieldCount = GetFieldCount();

        // Encode list body to temporary location to get size
        Span<byte> bodyBuffer = stackalloc byte[GetMaxBodySize()];
        var bodySize = EncodeFields(bodyBuffer, fieldCount);

        // List header
        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);

        // Copy body
        bodyBuffer[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;
        return offset;
    }

    private int EncodeFields(Span<byte> buffer, int fieldCount)
    {
        var offset = 0;

        // Field 0: container-id (mandatory)
        offset += AmqpEncoder.EncodeString(buffer[offset..], ContainerId);
        if (fieldCount <= 1)
        {
            return offset;
        }

        // Field 1: hostname
        if (Hostname != null)
        {
            offset += AmqpEncoder.EncodeString(buffer[offset..], Hostname);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        if (fieldCount <= 2)
        {
            return offset;
        }

        // Field 2: max-frame-size
        offset += AmqpEncoder.EncodeUInt(buffer[offset..], MaxFrameSize);
        if (fieldCount <= 3)
        {
            return offset;
        }

        // Field 3: channel-max
        offset += AmqpEncoder.EncodeUShort(buffer[offset..], ChannelMax);
        if (fieldCount <= 4)
        {
            return offset;
        }

        // Field 4: idle-time-out
        if (IdleTimeOut.HasValue)
        {
            offset += AmqpEncoder.EncodeUInt(buffer[offset..], IdleTimeOut.Value);
        }
        else
        {
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        if (fieldCount <= 5)
        {
            return offset;
        }

        // Field 5: outgoing-locales
        offset += EncodeSymbolArray(buffer[offset..], OutgoingLocales);
        if (fieldCount <= 6)
        {
            return offset;
        }

        // Field 6: incoming-locales
        offset += EncodeSymbolArray(buffer[offset..], IncomingLocales);
        if (fieldCount <= 7)
        {
            return offset;
        }

        // Field 7: offered-capabilities
        offset += EncodeSymbolArray(buffer[offset..], OfferedCapabilities);
        if (fieldCount <= 8)
        {
            return offset;
        }

        // Field 8: desired-capabilities
        offset += EncodeSymbolArray(buffer[offset..], DesiredCapabilities);
        if (fieldCount <= 9)
        {
            return offset;
        }

        // Field 9: properties
        offset += EncodeProperties(buffer[offset..], Properties);
        return offset;
    }

    private static int EncodeSymbolArray(Span<byte> buffer, string[]? values)
    {
        if (values == null || values.Length == 0)
        {
            return AmqpEncoder.EncodeNull(buffer);
        }
        if (values.Length == 1)
        {
            return AmqpEncoder.EncodeSymbol(buffer, values[0]);
        }

        // Calculate array body size
        var bodySize = 0;
        foreach (var value in values)
        {
            bodySize += AmqpEncoder.GetEncodedSymbolSize(value.AsSpan()) - 1; // Subtract format code
        }
        var offset = 0;

        // Array header
        offset += AmqpEncoder.EncodeArrayHeader(buffer[offset..], bodySize + 1, values.Length);

        // Element constructor (sym8 or sym32)
        var useSmall = values.All(v => v.Length <= 255);
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

    private static int EncodeProperties(Span<byte> buffer, IReadOnlyDictionary<string, object?>? properties) => AmqpEncoder.EncodeMap(buffer, properties);

    private int GetFieldCount()
    {
        // Count non-null trailing fields
        if (Properties != null && Properties.Count > 0)
        {
            return 10;
        }
        if (DesiredCapabilities != null && DesiredCapabilities.Length > 0)
        {
            return 9;
        }
        if (OfferedCapabilities != null && OfferedCapabilities.Length > 0)
        {
            return 8;
        }
        if (IncomingLocales != null && IncomingLocales.Length > 0)
        {
            return 7;
        }
        if (OutgoingLocales != null && OutgoingLocales.Length > 0)
        {
            return 6;
        }
        if (IdleTimeOut.HasValue)
        {
            return 5;
        }
        if (ChannelMax != ushort.MaxValue)
        {
            return 4;
        }
        if (MaxFrameSize != uint.MaxValue)
        {
            return 3;
        }
        if (Hostname != null)
        {
            return 2;
        }
        return 1; // container-id is mandatory
    }

    private int GetMaxBodySize()
    {
        // Estimate maximum body size
        var size = 0;
        size += AmqpEncoder.GetEncodedStringSize(ContainerId.AsSpan());
        size += Hostname != null ? AmqpEncoder.GetEncodedStringSize(Hostname.AsSpan()) : 1;
        size += 5; // max-frame-size
        size += 3; // channel-max
        size += IdleTimeOut.HasValue ? 5 : 1;
        size += 256;  // locales estimate
        size += 256;  // capabilities estimate
        size += 1024; // properties estimate
        return size;
    }

    /// <inheritdoc />
    public override int GetEncodedSize() =>
        // This is an estimate - actual encoding may be smaller
        GetDescriptorSize(DescriptorCode) + GetListHeaderSize(GetMaxBodySize(), GetFieldCount()) + GetMaxBodySize();

    /// <summary>
    /// Decodes an Open performative from a buffer.
    /// </summary>
    public static Open Decode(ReadOnlySpan<byte> buffer, out int listSize, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out var headerSize);
        listSize = size;
        var offset = headerSize;
        bytesConsumed = headerSize + size;
        if (count == 0)
        {
            throw new AmqpDecodeException("Open performative requires at least container-id field");
        }

        // Field 0: container-id (mandatory)
        var containerId = AmqpDecoder.DecodeString(buffer[offset..], out var consumed);
        offset += consumed;
        string? hostname = null;
        var maxFrameSize = uint.MaxValue;
        var channelMax = ushort.MaxValue;
        uint? idleTimeOut = null;
        string[]? outgoingLocales = null;
        string[]? incomingLocales = null;
        string[]? offeredCapabilities = null;
        string[]? desiredCapabilities = null;
        IReadOnlyDictionary<string, object?>? properties = null;
        if (count > 1)
        {
            // Field 1: hostname
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
            {
                hostname = AmqpDecoder.DecodeString(buffer[offset..], out consumed);
            }
            offset += consumed;
        }
        if (count > 2)
        {
            // Field 2: max-frame-size
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
            {
                maxFrameSize = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
            }
            offset += consumed;
        }
        if (count > 3)
        {
            // Field 3: channel-max
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
            {
                channelMax = AmqpDecoder.DecodeUShort(buffer[offset..], out consumed);
            }
            offset += consumed;
        }
        if (count > 4)
        {
            // Field 4: idle-time-out
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
            {
                idleTimeOut = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
            }
            offset += consumed;
        }
        if (count > 5)
        {
            // Field 5: outgoing-locales
            outgoingLocales = AmqpDecoder.DecodeSymbolArray(buffer[offset..], out consumed);
            offset += consumed;
        }
        if (count > 6)
        {
            // Field 6: incoming-locales
            incomingLocales = AmqpDecoder.DecodeSymbolArray(buffer[offset..], out consumed);
            offset += consumed;
        }
        if (count > 7)
        {
            // Field 7: offered-capabilities
            offeredCapabilities = AmqpDecoder.DecodeSymbolArray(buffer[offset..], out consumed);
            offset += consumed;
        }
        if (count > 8)
        {
            // Field 8: desired-capabilities
            desiredCapabilities = AmqpDecoder.DecodeSymbolArray(buffer[offset..], out consumed);
            offset += consumed;
        }
        if (count > 9)
        {
            // Field 9: properties
            properties = AmqpDecoder.DecodeMap(buffer[offset..], out consumed);
            offset += consumed;
        }
        return new()
        {
            ContainerId = containerId,
            Hostname = hostname,
            MaxFrameSize = maxFrameSize,
            ChannelMax = channelMax,
            IdleTimeOut = idleTimeOut,
            OutgoingLocales = outgoingLocales,
            IncomingLocales = incomingLocales,
            OfferedCapabilities = offeredCapabilities,
            DesiredCapabilities = desiredCapabilities,
            Properties = properties
        };
    }

    /// <inheritdoc />
    public override string ToString() => $"Open(ContainerId={ContainerId}, Hostname={Hostname}, MaxFrameSize={MaxFrameSize}, ChannelMax={ChannelMax}, IdleTimeOut={IdleTimeOut})";
}
