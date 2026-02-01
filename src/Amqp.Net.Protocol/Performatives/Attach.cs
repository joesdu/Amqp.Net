// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Protocol.Types;

namespace Amqp.Net.Protocol.Performatives;

/// <summary>
/// AMQP Attach performative - attaches a link to a session.
/// Descriptor: 0x00000000:0x00000012
/// </summary>
public sealed record Attach : PerformativeBase
{
    /// <inheritdoc />
    public override ulong DescriptorCode => Descriptor.Attach;

    /// <summary>
    /// The link name (mandatory).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The link handle (mandatory).
    /// </summary>
    public required uint Handle { get; init; }

    /// <summary>
    /// The role: false = sender, true = receiver (mandatory).
    /// </summary>
    public required bool Role { get; init; }

    /// <summary>
    /// Sender settle mode (0=unsettled, 1=settled, 2=mixed).
    /// </summary>
    public byte? SndSettleMode { get; init; }

    /// <summary>
    /// Receiver settle mode (0=first, 1=second).
    /// </summary>
    public byte? RcvSettleMode { get; init; }

    /// <summary>
    /// The source terminus.
    /// </summary>
    public Source? Source { get; init; }

    /// <summary>
    /// The target terminus.
    /// </summary>
    public Target? Target { get; init; }

    /// <summary>
    /// Unsettled delivery state map.
    /// </summary>
    public IReadOnlyDictionary<byte[], object?>? Unsettled { get; init; }

    /// <summary>
    /// Incomplete unsettled flag.
    /// </summary>
    public bool IncompleteUnsettled { get; init; }

    /// <summary>
    /// Initial delivery count (sender only).
    /// </summary>
    public uint? InitialDeliveryCount { get; init; }

    /// <summary>
    /// Maximum message size.
    /// </summary>
    public ulong? MaxMessageSize { get; init; }

    /// <summary>
    /// Offered capabilities.
    /// </summary>
    public string[]? OfferedCapabilities { get; init; }

    /// <summary>
    /// Desired capabilities.
    /// </summary>
    public string[]? DesiredCapabilities { get; init; }

    /// <summary>
    /// Link properties.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Properties { get; init; }

    /// <inheritdoc />
    public override int Encode(Span<byte> buffer)
    {
        int offset = 0;

        // Described type constructor
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);

        int fieldCount = GetFieldCount();
        Span<byte> bodyBuffer = stackalloc byte[1024];
        int bodySize = EncodeFields(bodyBuffer, fieldCount);

        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);
        bodyBuffer[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;

        return offset;
    }

    private int EncodeFields(Span<byte> buffer, int fieldCount)
    {
        int offset = 0;

        // Field 0: name (mandatory)
        offset += AmqpEncoder.EncodeString(buffer[offset..], Name);

        // Field 1: handle (mandatory)
        offset += AmqpEncoder.EncodeUInt(buffer[offset..], Handle);

        // Field 2: role (mandatory)
        offset += AmqpEncoder.EncodeBoolean(buffer[offset..], Role);

        if (fieldCount <= 3) return offset;

        // Field 3: snd-settle-mode
        if (SndSettleMode.HasValue)
            offset += AmqpEncoder.EncodeUByte(buffer[offset..], SndSettleMode.Value);
        else
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);

        if (fieldCount <= 4) return offset;

        // Field 4: rcv-settle-mode
        if (RcvSettleMode.HasValue)
            offset += AmqpEncoder.EncodeUByte(buffer[offset..], RcvSettleMode.Value);
        else
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);

        if (fieldCount <= 5) return offset;

        // Field 5: source
        if (Source != null)
            offset += Source.Encode(buffer[offset..]);
        else
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);

        if (fieldCount <= 6) return offset;

        // Field 6: target
        if (Target != null)
            offset += Target.Encode(buffer[offset..]);
        else
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);

        if (fieldCount <= 7) return offset;

        // Field 7: unsettled
        if (Unsettled != null && Unsettled.Count > 0)
            offset += EncodeUnsettledMap(buffer[offset..], Unsettled);
        else
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);

        if (fieldCount <= 8) return offset;

        // Field 8: incomplete-unsettled
        offset += AmqpEncoder.EncodeBoolean(buffer[offset..], IncompleteUnsettled);

        if (fieldCount <= 9) return offset;

        // Field 9: initial-delivery-count
        if (InitialDeliveryCount.HasValue)
            offset += AmqpEncoder.EncodeUInt(buffer[offset..], InitialDeliveryCount.Value);
        else
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);

        if (fieldCount <= 10) return offset;

        // Field 10: max-message-size
        if (MaxMessageSize.HasValue)
            offset += AmqpEncoder.EncodeULong(buffer[offset..], MaxMessageSize.Value);
        else
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);

        // Remaining fields as null
        for (int i = 11; i < fieldCount; i++)
            offset += AmqpEncoder.EncodeNull(buffer[offset..]);

        return offset;
    }

    private int GetFieldCount()
    {
        if (Properties != null) return 14;
        if (DesiredCapabilities != null) return 13;
        if (OfferedCapabilities != null) return 12;
        if (MaxMessageSize.HasValue) return 11;
        if (InitialDeliveryCount.HasValue) return 10;
        if (IncompleteUnsettled) return 9;
        if (Unsettled != null) return 8;
        if (Target != null) return 7;
        if (Source != null) return 6;
        if (RcvSettleMode.HasValue) return 5;
        if (SndSettleMode.HasValue) return 4;
        return 3;
    }

    /// <inheritdoc />
    public override int GetEncodedSize() => 1024;

    /// <summary>
    /// Encodes the unsettled map (delivery-tag -> delivery-state).
    /// </summary>
    private static int EncodeUnsettledMap(Span<byte> buffer, IReadOnlyDictionary<byte[], object?> unsettled)
    {
        if (unsettled.Count == 0)
            return AmqpEncoder.EncodeNull(buffer);
        
        // Calculate body size first
        Span<byte> bodyBuffer = stackalloc byte[2048];
        int bodySize = 0;
        int pairCount = 0;
        
        foreach (var kvp in unsettled)
        {
            // Key: delivery-tag (binary)
            bodySize += AmqpEncoder.EncodeBinary(bodyBuffer[bodySize..], kvp.Key);
            // Value: delivery-state
            if (kvp.Value is DeliveryState state)
                bodySize += state.Encode(bodyBuffer[bodySize..]);
            else
                bodySize += AmqpEncoder.EncodeNull(bodyBuffer[bodySize..]);
            pairCount++;
        }
        
        int offset = 0;
        offset += AmqpEncoder.EncodeMapHeader(buffer[offset..], bodySize, pairCount * 2);
        bodyBuffer[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;
        
        return offset;
    }

    /// <summary>
    /// Decodes an Attach performative.
    /// </summary>
    public static Attach Decode(ReadOnlySpan<byte> buffer, out int listSize, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out int headerSize);
        listSize = size;
        bytesConsumed = headerSize + size;

        if (count < 3)
            throw new AmqpDecodeException("Attach requires at least 3 fields");

        int offset = headerSize;

        // Field 0: name
        string name = AmqpDecoder.DecodeString(buffer[offset..], out int consumed);
        offset += consumed;

        // Field 1: handle
        uint handle = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
        offset += consumed;

        // Field 2: role
        bool role = AmqpDecoder.DecodeBoolean(buffer[offset..], out consumed);
        offset += consumed;

        var attach = new Attach { Name = name, Handle = handle, Role = role };

        if (count <= 3) return attach;

        // Field 3: snd-settle-mode
        byte? sndSettleMode = null;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
        {
            sndSettleMode = AmqpDecoder.DecodeUByte(buffer[offset..], out consumed);
        }
        offset += consumed;

        if (count <= 4) return attach with { SndSettleMode = sndSettleMode };

        // Field 4: rcv-settle-mode
        byte? rcvSettleMode = null;
        if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
        {
            rcvSettleMode = AmqpDecoder.DecodeUByte(buffer[offset..], out consumed);
        }
        offset += consumed;

        // Skip remaining fields for now
        return attach with { SndSettleMode = sndSettleMode, RcvSettleMode = rcvSettleMode };
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"Attach(Name={Name}, Handle={Handle}, Role={(Role ? "receiver" : "sender")})";
}

/// <summary>
/// Source terminus for a link.
/// </summary>
public sealed class Source
{
    public string? Address { get; init; }
    public uint? Durable { get; init; }
    public string? ExpiryPolicy { get; init; }
    public uint? Timeout { get; init; }
    public bool Dynamic { get; init; }
    public IReadOnlyDictionary<string, object?>? DynamicNodeProperties { get; init; }
    public string? DistributionMode { get; init; }
    public IReadOnlyDictionary<string, object?>? Filter { get; init; }
    public object? DefaultOutcome { get; init; }
    public string[]? Outcomes { get; init; }
    public string[]? Capabilities { get; init; }

    public int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], Descriptor.Source);

        // Minimal encoding - just address
        int fieldCount = Address != null ? 1 : 0;
        Span<byte> body = stackalloc byte[256];
        int bodySize = 0;

        if (Address != null)
            bodySize += AmqpEncoder.EncodeString(body[bodySize..], Address);

        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;

        return offset;
    }
}

/// <summary>
/// Target terminus for a link.
/// </summary>
public sealed class Target
{
    public string? Address { get; init; }
    public uint? Durable { get; init; }
    public string? ExpiryPolicy { get; init; }
    public uint? Timeout { get; init; }
    public bool Dynamic { get; init; }
    public IReadOnlyDictionary<string, object?>? DynamicNodeProperties { get; init; }
    public string[]? Capabilities { get; init; }

    public int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], Descriptor.Target);

        int fieldCount = Address != null ? 1 : 0;
        Span<byte> body = stackalloc byte[256];
        int bodySize = 0;

        if (Address != null)
            bodySize += AmqpEncoder.EncodeString(body[bodySize..], Address);

        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;

        return offset;
    }
}
