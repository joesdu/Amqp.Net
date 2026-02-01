// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using Amqp.Net.Protocol.Types;

namespace Amqp.Net.Protocol.Messaging;

/// <summary>
/// AMQP Message Header section.
/// Contains delivery-related annotations.
/// </summary>
public sealed class Header
{
    /// <summary>Descriptor code for Header section.</summary>
    public const ulong DescriptorCode = Descriptor.Header;

    /// <summary>Durable message flag.</summary>
    public bool Durable { get; init; }

    /// <summary>Message priority (0-9, default 4).</summary>
    public byte Priority { get; init; } = 4;

    /// <summary>Time-to-live in milliseconds.</summary>
    public uint? Ttl { get; init; }

    /// <summary>First acquirer flag.</summary>
    public bool FirstAcquirer { get; init; }

    /// <summary>Delivery count.</summary>
    public uint DeliveryCount { get; init; }

    /// <summary>Encodes the header section.</summary>
    public int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);

        int fieldCount = GetFieldCount();
        Span<byte> body = stackalloc byte[32];
        int bodySize = EncodeFields(body, fieldCount);

        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;

        return offset;
    }

    private int EncodeFields(Span<byte> buffer, int fieldCount)
    {
        int offset = 0;

        if (fieldCount > 0)
            offset += AmqpEncoder.EncodeBoolean(buffer[offset..], Durable);
        if (fieldCount > 1)
            offset += AmqpEncoder.EncodeUByte(buffer[offset..], Priority);
        if (fieldCount > 2)
        {
            if (Ttl.HasValue)
                offset += AmqpEncoder.EncodeUInt(buffer[offset..], Ttl.Value);
            else
                offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }
        if (fieldCount > 3)
            offset += AmqpEncoder.EncodeBoolean(buffer[offset..], FirstAcquirer);
        if (fieldCount > 4)
            offset += AmqpEncoder.EncodeUInt(buffer[offset..], DeliveryCount);

        return offset;
    }

    private int GetFieldCount()
    {
        if (DeliveryCount != 0) return 5;
        if (FirstAcquirer) return 4;
        if (Ttl.HasValue) return 3;
        if (Priority != 4) return 2;
        if (Durable) return 1;
        return 0;
    }

    /// <summary>Decodes a Header section.</summary>
    public static Header Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        // Skip descriptor (already verified by caller)
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out int headerSize);
        bytesConsumed = headerSize + size;

        int offset = headerSize;
        bool durable = false;
        byte priority = 4;
        uint? ttl = null;
        bool firstAcquirer = false;
        uint deliveryCount = 0;

        if (count > 0)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                durable = AmqpDecoder.DecodeBoolean(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 1)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                priority = AmqpDecoder.DecodeUByte(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 2)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                ttl = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 3)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                firstAcquirer = AmqpDecoder.DecodeBoolean(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 4)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                deliveryCount = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
            offset += consumed;
        }

        return new Header
        {
            Durable = durable,
            Priority = priority,
            Ttl = ttl,
            FirstAcquirer = firstAcquirer,
            DeliveryCount = deliveryCount
        };
    }
}

/// <summary>
/// AMQP Message Properties section.
/// Contains immutable message properties.
/// </summary>
public sealed class Properties
{
    /// <summary>Descriptor code for Properties section.</summary>
    public const ulong DescriptorCode = Descriptor.Properties;

    /// <summary>Message identifier.</summary>
    public object? MessageId { get; init; }

    /// <summary>User identifier.</summary>
    public byte[]? UserId { get; init; }

    /// <summary>Destination address.</summary>
    public string? To { get; init; }

    /// <summary>Subject/routing key.</summary>
    public string? Subject { get; init; }

    /// <summary>Reply-to address.</summary>
    public string? ReplyTo { get; init; }

    /// <summary>Correlation identifier.</summary>
    public object? CorrelationId { get; init; }

    /// <summary>Content type (MIME type).</summary>
    public string? ContentType { get; init; }

    /// <summary>Content encoding.</summary>
    public string? ContentEncoding { get; init; }

    /// <summary>Absolute expiry time.</summary>
    public DateTimeOffset? AbsoluteExpiryTime { get; init; }

    /// <summary>Creation time.</summary>
    public DateTimeOffset? CreationTime { get; init; }

    /// <summary>Group identifier.</summary>
    public string? GroupId { get; init; }

    /// <summary>Group sequence number.</summary>
    public uint? GroupSequence { get; init; }

    /// <summary>Reply-to group identifier.</summary>
    public string? ReplyToGroupId { get; init; }

    /// <summary>Encodes the properties section.</summary>
    public int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);

        int fieldCount = GetFieldCount();
        Span<byte> body = stackalloc byte[1024];
        int bodySize = EncodeFields(body, fieldCount);

        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;

        return offset;
    }

    private int EncodeFields(Span<byte> buffer, int fieldCount)
    {
        int offset = 0;

        // Field 0: message-id
        if (fieldCount > 0)
            offset += EncodeMessageId(buffer[offset..], MessageId);

        // Field 1: user-id
        if (fieldCount > 1)
        {
            if (UserId != null)
                offset += AmqpEncoder.EncodeBinary(buffer[offset..], UserId);
            else
                offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }

        // Field 2: to
        if (fieldCount > 2)
            offset += AmqpEncoder.EncodeString(buffer[offset..], To);

        // Field 3: subject
        if (fieldCount > 3)
            offset += AmqpEncoder.EncodeString(buffer[offset..], Subject);

        // Field 4: reply-to
        if (fieldCount > 4)
            offset += AmqpEncoder.EncodeString(buffer[offset..], ReplyTo);

        // Field 5: correlation-id
        if (fieldCount > 5)
            offset += EncodeMessageId(buffer[offset..], CorrelationId);

        // Field 6: content-type
        if (fieldCount > 6)
            offset += AmqpEncoder.EncodeSymbol(buffer[offset..], ContentType);

        // Field 7: content-encoding
        if (fieldCount > 7)
            offset += AmqpEncoder.EncodeSymbol(buffer[offset..], ContentEncoding);

        // Field 8: absolute-expiry-time
        if (fieldCount > 8)
        {
            if (AbsoluteExpiryTime.HasValue)
                offset += AmqpEncoder.EncodeTimestamp(buffer[offset..], AbsoluteExpiryTime.Value);
            else
                offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }

        // Field 9: creation-time
        if (fieldCount > 9)
        {
            if (CreationTime.HasValue)
                offset += AmqpEncoder.EncodeTimestamp(buffer[offset..], CreationTime.Value);
            else
                offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }

        // Field 10: group-id
        if (fieldCount > 10)
            offset += AmqpEncoder.EncodeString(buffer[offset..], GroupId);

        // Field 11: group-sequence
        if (fieldCount > 11)
        {
            if (GroupSequence.HasValue)
                offset += AmqpEncoder.EncodeUInt(buffer[offset..], GroupSequence.Value);
            else
                offset += AmqpEncoder.EncodeNull(buffer[offset..]);
        }

        // Field 12: reply-to-group-id
        if (fieldCount > 12)
            offset += AmqpEncoder.EncodeString(buffer[offset..], ReplyToGroupId);

        return offset;
    }

    private static int EncodeMessageId(Span<byte> buffer, object? messageId)
    {
        return messageId switch
        {
            null => AmqpEncoder.EncodeNull(buffer),
            string s => AmqpEncoder.EncodeString(buffer, s),
            ulong ul => AmqpEncoder.EncodeULong(buffer, ul),
            Guid g => AmqpEncoder.EncodeUuid(buffer, g),
            byte[] b => AmqpEncoder.EncodeBinary(buffer, b),
            _ => AmqpEncoder.EncodeString(buffer, messageId.ToString())
        };
    }

    private int GetFieldCount()
    {
        if (ReplyToGroupId != null) return 13;
        if (GroupSequence.HasValue) return 12;
        if (GroupId != null) return 11;
        if (CreationTime.HasValue) return 10;
        if (AbsoluteExpiryTime.HasValue) return 9;
        if (ContentEncoding != null) return 8;
        if (ContentType != null) return 7;
        if (CorrelationId != null) return 6;
        if (ReplyTo != null) return 5;
        if (Subject != null) return 4;
        if (To != null) return 3;
        if (UserId != null) return 2;
        if (MessageId != null) return 1;
        return 0;
    }

    /// <summary>Decodes a Properties section.</summary>
    public static Properties Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out int headerSize);
        bytesConsumed = headerSize + size;

        int offset = headerSize;
        var props = new Properties();

        object? messageId = null;
        byte[]? userId = null;
        string? to = null;
        string? subject = null;
        string? replyTo = null;
        object? correlationId = null;
        string? contentType = null;
        string? contentEncoding = null;
        DateTimeOffset? absoluteExpiryTime = null;
        DateTimeOffset? creationTime = null;
        string? groupId = null;
        uint? groupSequence = null;
        string? replyToGroupId = null;

        if (count > 0)
        {
            messageId = AmqpDecoder.DecodeValue(buffer[offset..], out int consumed);
            offset += consumed;
        }

        if (count > 1)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
            {
                var bin = AmqpDecoder.DecodeBinary(buffer[offset..], out consumed);
                userId = bin.ToArray();
            }
            offset += consumed;
        }

        if (count > 2)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                to = AmqpDecoder.DecodeString(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 3)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                subject = AmqpDecoder.DecodeString(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 4)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                replyTo = AmqpDecoder.DecodeString(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 5)
        {
            correlationId = AmqpDecoder.DecodeValue(buffer[offset..], out int consumed);
            offset += consumed;
        }

        if (count > 6)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                contentType = AmqpDecoder.DecodeSymbol(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 7)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                contentEncoding = AmqpDecoder.DecodeSymbol(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 8)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                absoluteExpiryTime = AmqpDecoder.DecodeTimestamp(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 9)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                creationTime = AmqpDecoder.DecodeTimestamp(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 10)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                groupId = AmqpDecoder.DecodeString(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 11)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                groupSequence = AmqpDecoder.DecodeUInt(buffer[offset..], out consumed);
            offset += consumed;
        }

        if (count > 12)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out int consumed))
                replyToGroupId = AmqpDecoder.DecodeString(buffer[offset..], out consumed);
            offset += consumed;
        }

        return new Properties
        {
            MessageId = messageId,
            UserId = userId,
            To = to,
            Subject = subject,
            ReplyTo = replyTo,
            CorrelationId = correlationId,
            ContentType = contentType,
            ContentEncoding = contentEncoding,
            AbsoluteExpiryTime = absoluteExpiryTime,
            CreationTime = creationTime,
            GroupId = groupId,
            GroupSequence = groupSequence,
            ReplyToGroupId = replyToGroupId
        };
    }
}

/// <summary>
/// AMQP Application Properties section.
/// Contains application-specific key-value pairs.
/// </summary>
public sealed class ApplicationProperties
{
    /// <summary>Descriptor code for ApplicationProperties section.</summary>
    public const ulong DescriptorCode = Descriptor.ApplicationProperties;

    /// <summary>The application properties map.</summary>
    public IReadOnlyDictionary<string, object?>? Map { get; init; }

    /// <summary>Encodes the application properties section.</summary>
    public int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        offset += AmqpEncoder.EncodeMap(buffer[offset..], Map);
        return offset;
    }

    /// <summary>Decodes an ApplicationProperties section.</summary>
    public static ApplicationProperties Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var map = AmqpDecoder.DecodeMap(buffer, out bytesConsumed);
        return new ApplicationProperties { Map = map };
    }
}

/// <summary>
/// AMQP Data section - contains opaque binary data.
/// </summary>
public sealed class DataSection
{
    /// <summary>Descriptor code for Data section.</summary>
    public const ulong DescriptorCode = Descriptor.Data;

    /// <summary>The binary data.</summary>
    public ReadOnlyMemory<byte> Binary { get; init; }

    /// <summary>Encodes the data section.</summary>
    public int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        offset += AmqpEncoder.EncodeBinary(buffer[offset..], Binary.Span);
        return offset;
    }

    /// <summary>Decodes a Data section.</summary>
    public static DataSection Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var binary = AmqpDecoder.DecodeBinary(buffer, out bytesConsumed);
        return new DataSection { Binary = binary.ToArray() };
    }
}

/// <summary>
/// AMQP Value section - contains a single AMQP value.
/// </summary>
public sealed class AmqpValue
{
    /// <summary>Descriptor code for AmqpValue section.</summary>
    public const ulong DescriptorCode = Descriptor.AmqpValue;

    /// <summary>The value.</summary>
    public object? Value { get; init; }

    /// <summary>Encodes the value section.</summary>
    public int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        offset += AmqpEncoder.EncodeValue(buffer[offset..], Value);
        return offset;
    }

    /// <summary>Decodes an AmqpValue section.</summary>
    public static AmqpValue Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var value = AmqpDecoder.DecodeValue(buffer, out bytesConsumed);
        return new AmqpValue { Value = value };
    }
}

/// <summary>
/// AMQP Message Annotations section.
/// Contains broker-specific annotations.
/// </summary>
public sealed class MessageAnnotations
{
    /// <summary>Descriptor code for MessageAnnotations section.</summary>
    public const ulong DescriptorCode = Descriptor.MessageAnnotations;

    /// <summary>The annotations map (symbol keys).</summary>
    public IReadOnlyDictionary<string, object?>? Map { get; init; }

    /// <summary>Encodes the message annotations section.</summary>
    public int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        offset += AmqpEncoder.EncodeMap(buffer[offset..], Map);
        return offset;
    }

    /// <summary>Decodes a MessageAnnotations section.</summary>
    public static MessageAnnotations Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var map = AmqpDecoder.DecodeMap(buffer, out bytesConsumed);
        return new MessageAnnotations { Map = map };
    }
}

/// <summary>
/// AMQP Delivery Annotations section.
/// Contains delivery-specific annotations.
/// </summary>
public sealed class DeliveryAnnotations
{
    /// <summary>Descriptor code for DeliveryAnnotations section.</summary>
    public const ulong DescriptorCode = Descriptor.DeliveryAnnotations;

    /// <summary>The annotations map (symbol keys).</summary>
    public IReadOnlyDictionary<string, object?>? Map { get; init; }

    /// <summary>Encodes the delivery annotations section.</summary>
    public int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        offset += AmqpEncoder.EncodeMap(buffer[offset..], Map);
        return offset;
    }

    /// <summary>Decodes a DeliveryAnnotations section.</summary>
    public static DeliveryAnnotations Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var map = AmqpDecoder.DecodeMap(buffer, out bytesConsumed);
        return new DeliveryAnnotations { Map = map };
    }
}

/// <summary>
/// AMQP Footer section.
/// Contains message trailer annotations.
/// </summary>
public sealed class Footer
{
    /// <summary>Descriptor code for Footer section.</summary>
    public const ulong DescriptorCode = Descriptor.Footer;

    /// <summary>The footer map.</summary>
    public IReadOnlyDictionary<string, object?>? Map { get; init; }

    /// <summary>Encodes the footer section.</summary>
    public int Encode(Span<byte> buffer)
    {
        int offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        offset += AmqpEncoder.EncodeMap(buffer[offset..], Map);
        return offset;
    }

    /// <summary>Decodes a Footer section.</summary>
    public static Footer Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var map = AmqpDecoder.DecodeMap(buffer, out bytesConsumed);
        return new Footer { Map = map };
    }
}
