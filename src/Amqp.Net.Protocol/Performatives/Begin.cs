// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Protocol.Types;

namespace Amqp.Net.Protocol.Performatives;

/// <summary>
/// AMQP Begin performative - establishes a session.
/// Descriptor: 0x00000000:0x00000011
/// </summary>
/// <remarks>
/// Fields:
/// 0. remote-channel (ushort) - channel number on remote peer
/// 1. next-outgoing-id (transfer-number, mandatory) - next transfer-id to assign
/// 2. incoming-window (uint, mandatory) - max incoming transfers
/// 3. outgoing-window (uint, mandatory) - max outgoing transfers
/// 4. handle-max (handle, default 4294967295) - max handle number
/// 5. offered-capabilities (symbol[]) - capabilities offered
/// 6. desired-capabilities (symbol[]) - capabilities desired
/// 7. properties (fields) - session properties
/// </remarks>
public sealed record Begin : PerformativeBase
{
    /// <inheritdoc />
    public override ulong DescriptorCode => Descriptor.Begin;

    /// <summary>
    /// The channel number on the remote peer (null for initiating peer).
    /// </summary>
    public ushort? RemoteChannel { get; init; }

    /// <summary>
    /// The next transfer-id to assign (mandatory).
    /// </summary>
    public required uint NextOutgoingId { get; init; }

    /// <summary>
    /// Maximum number of incoming transfers (mandatory).
    /// </summary>
    public required uint IncomingWindow { get; init; }

    /// <summary>
    /// Maximum number of outgoing transfers (mandatory).
    /// </summary>
    public required uint OutgoingWindow { get; init; }

    /// <summary>
    /// Maximum handle number (default: uint.MaxValue).
    /// </summary>
    public uint HandleMax { get; init; } = uint.MaxValue;

    /// <summary>
    /// Capabilities offered by this peer.
    /// </summary>
    public string[]? OfferedCapabilities { get; init; }

    /// <summary>
    /// Capabilities desired from the remote peer.
    /// </summary>
    public string[]? DesiredCapabilities { get; init; }

    /// <summary>
    /// Session properties.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Properties { get; init; }

    /// <inheritdoc />
    public override int Encode(Span<byte> buffer)
    {
        int offset = 0;

        // Described type constructor
        buffer[offset++] = FormatCode.Described;

        // Descriptor
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);

        // Calculate field count
        int fieldCount = GetFieldCount();

        // Encode fields
        Span<byte> bodyBuffer = stackalloc byte[256];
        int bodySize = EncodeFields(bodyBuffer, fieldCount);

        // List header
        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);

        // Copy body
        bodyBuffer[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;

        return offset;
    }

    private int EncodeFields(Span<byte> buffer, int fieldCount)
    {
        int offset = 0;

        // Field 0: remote-channel
        if (RemoteChannel.HasValue)
            offset += AmqpEncoder.EncodeUShort(buffer[offset..], RemoteChannel.Value);
        else
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);

        // Field 1: next-outgoing-id (mandatory)
        offset += AmqpEncoder.EncodeUInt(buffer[offset..], NextOutgoingId);

        // Field 2: incoming-window (mandatory)
        offset += AmqpEncoder.EncodeUInt(buffer[offset..], IncomingWindow);

        // Field 3: outgoing-window (mandatory)
        offset += AmqpEncoder.EncodeUInt(buffer[offset..], OutgoingWindow);

        if (fieldCount <= 4) return offset;

        // Field 4: handle-max
        offset += AmqpEncoder.EncodeUInt(buffer[offset..], HandleMax);

        if (fieldCount <= 5) return offset;

        // Field 5: offered-capabilities
        offset += AmqpEncoder.EncodeSymbolArray(buffer[offset..], OfferedCapabilities);

        if (fieldCount <= 6) return offset;

        // Field 6: desired-capabilities
        offset += AmqpEncoder.EncodeSymbolArray(buffer[offset..], DesiredCapabilities);

        if (fieldCount <= 7) return offset;

        // Field 7: properties
        offset += AmqpEncoder.EncodeMap(buffer[offset..], Properties);

        return offset;
    }

    private int GetFieldCount()
    {
        if (Properties != null && Properties.Count > 0) return 8;
        if (DesiredCapabilities != null && DesiredCapabilities.Length > 0) return 7;
        if (OfferedCapabilities != null && OfferedCapabilities.Length > 0) return 6;
        if (HandleMax != uint.MaxValue) return 5;
        return 4; // mandatory fields
    }

    /// <inheritdoc />
    public override int GetEncodedSize()
    {
        return 64; // Estimate
    }

    /// <summary>
    /// Decodes a Begin performative from a buffer.
    /// </summary>
    public static Begin Decode(ReadOnlySpan<byte> buffer, out int listSize, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out int headerSize);
        listSize = size;
        bytesConsumed = headerSize + size;

        if (count < 4)
        {
            throw new AmqpDecodeException("Begin performative requires at least 4 fields");
        }

        int offset = headerSize;

        // Field 0: remote-channel
        ushort? remoteChannel = null;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
        {
            remoteChannel = AmqpDecoder.DecodeUShort(buffer[offset..], out consumed);
        }
        offset += consumed;

        // Field 1: next-outgoing-id
        uint nextOutgoingId = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        offset += consumed;

        // Field 2: incoming-window
        uint incomingWindow = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        offset += consumed;

        // Field 3: outgoing-window
        uint outgoingWindow = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        offset += consumed;

        uint handleMax = uint.MaxValue;
        string[]? offeredCapabilities = null;
        string[]? desiredCapabilities = null;
        IReadOnlyDictionary<string, object?>? properties = null;

        if (count > 4)
        {
            // Field 4: handle-max
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
            {
                handleMax = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
            }
            offset += consumed;
        }

        if (count > 5)
        {
            // Field 5: offered-capabilities
            offeredCapabilities = AmqpDecoder.DecodeSymbolArray(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 6)
        {
            // Field 6: desired-capabilities
            desiredCapabilities = AmqpDecoder.DecodeSymbolArray(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 7)
        {
            // Field 7: properties
            properties = AmqpDecoder.DecodeMap(buffer[offset..], out consumed);
            offset += consumed;
        }

        return new Begin
        {
            RemoteChannel = remoteChannel,
            NextOutgoingId = nextOutgoingId,
            IncomingWindow = incomingWindow,
            OutgoingWindow = outgoingWindow,
            HandleMax = handleMax,
            OfferedCapabilities = offeredCapabilities,
            DesiredCapabilities = desiredCapabilities,
            Properties = properties
        };
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Begin(RemoteChannel={RemoteChannel}, NextOutgoingId={NextOutgoingId}, IncomingWindow={IncomingWindow}, OutgoingWindow={OutgoingWindow}, HandleMax={HandleMax})";
    }
}
