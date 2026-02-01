// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Protocol.Types;

namespace Amqp.Net.Protocol.Messaging;

/// <summary>
/// Represents a complete AMQP 1.0 message with all sections.
/// </summary>
public sealed class AmqpMessage
{
    /// <summary>Header section (delivery annotations).</summary>
    public Header? Header { get; init; }

    /// <summary>Delivery annotations section.</summary>
    public DeliveryAnnotations? DeliveryAnnotations { get; init; }

    /// <summary>Message annotations section.</summary>
    public MessageAnnotations? MessageAnnotations { get; init; }

    /// <summary>Properties section.</summary>
    public Properties? Properties { get; init; }

    /// <summary>Application properties section.</summary>
    public ApplicationProperties? ApplicationProperties { get; init; }

    /// <summary>Body sections (Data, AmqpValue, or AmqpSequence).</summary>
    public IReadOnlyList<object>? Body { get; init; }

    /// <summary>Footer section.</summary>
    public Footer? Footer { get; init; }

    /// <summary>
    /// Creates a simple message with string body.
    /// </summary>
    public static AmqpMessage Create(string body) => new()
    {
        Body = [new AmqpValue { Value = body }]
    };

    /// <summary>
    /// Creates a simple message with binary body.
    /// </summary>
    public static AmqpMessage Create(byte[] body) => new()
    {
        Body = [new DataSection { Binary = body }]
    };

    /// <summary>
    /// Creates a message with properties and string body.
    /// </summary>
    public static AmqpMessage Create(string body, Properties properties) => new()
    {
        Properties = properties,
        Body = [new AmqpValue { Value = body }]
    };

    /// <summary>
    /// Encodes the message to a buffer.
    /// </summary>
    public int Encode(Span<byte> buffer)
    {
        int offset = 0;

        if (Header != null)
            offset += Header.Encode(buffer[offset..]);

        if (DeliveryAnnotations != null)
            offset += DeliveryAnnotations.Encode(buffer[offset..]);

        if (MessageAnnotations != null)
            offset += MessageAnnotations.Encode(buffer[offset..]);

        if (Properties != null)
            offset += Properties.Encode(buffer[offset..]);

        if (ApplicationProperties != null)
            offset += ApplicationProperties.Encode(buffer[offset..]);

        if (Body != null)
        {
            foreach (var section in Body)
            {
                offset += section switch
                {
                    DataSection data => data.Encode(buffer[offset..]),
                    AmqpValue value => value.Encode(buffer[offset..]),
                    _ => 0
                };
            }
        }

        if (Footer != null)
            offset += Footer.Encode(buffer[offset..]);

        return offset;
    }

    /// <summary>
    /// Decodes a message from a buffer.
    /// </summary>
    public static AmqpMessage Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        int offset = 0;
        Header? header = null;
        DeliveryAnnotations? deliveryAnnotations = null;
        MessageAnnotations? messageAnnotations = null;
        Properties? properties = null;
        ApplicationProperties? applicationProperties = null;
        List<object>? body = null;
        Footer? footer = null;

        while (offset < buffer.Length)
        {
            // Check for described type
            if (buffer[offset] != FormatCode.Described)
                break;

            // Peek at descriptor
            ulong descriptor = AmqpDecoder.DecodeULong(buffer[(offset + 1)..], out int descSize);
            int sectionStart = offset + 1 + descSize;

            switch (descriptor)
            {
                case Descriptor.Header:
                    header = Header.Decode(buffer[sectionStart..], out int headerSize);
                    offset = sectionStart + headerSize;
                    break;

                case Descriptor.DeliveryAnnotations:
                    deliveryAnnotations = DeliveryAnnotations.Decode(buffer[sectionStart..], out int daSize);
                    offset = sectionStart + daSize;
                    break;

                case Descriptor.MessageAnnotations:
                    messageAnnotations = MessageAnnotations.Decode(buffer[sectionStart..], out int maSize);
                    offset = sectionStart + maSize;
                    break;

                case Descriptor.Properties:
                    properties = Properties.Decode(buffer[sectionStart..], out int propsSize);
                    offset = sectionStart + propsSize;
                    break;

                case Descriptor.ApplicationProperties:
                    applicationProperties = ApplicationProperties.Decode(buffer[sectionStart..], out int apSize);
                    offset = sectionStart + apSize;
                    break;

                case Descriptor.Data:
                    body ??= [];
                    var data = DataSection.Decode(buffer[sectionStart..], out int dataSize);
                    body.Add(data);
                    offset = sectionStart + dataSize;
                    break;

                case Descriptor.AmqpValue:
                    body ??= [];
                    var value = AmqpValue.Decode(buffer[sectionStart..], out int valueSize);
                    body.Add(value);
                    offset = sectionStart + valueSize;
                    break;

                case Descriptor.Footer:
                    footer = Footer.Decode(buffer[sectionStart..], out int footerSize);
                    offset = sectionStart + footerSize;
                    break;

                default:
                    // Unknown section, skip it
                    int skipSize = AmqpDecoder.SkipValue(buffer[offset..]);
                    offset += skipSize;
                    break;
            }
        }

        bytesConsumed = offset;
        return new AmqpMessage
        {
            Header = header,
            DeliveryAnnotations = deliveryAnnotations,
            MessageAnnotations = messageAnnotations,
            Properties = properties,
            ApplicationProperties = applicationProperties,
            Body = body,
            Footer = footer
        };
    }

    /// <summary>
    /// Gets the body as a string (if AmqpValue with string).
    /// </summary>
    public string? GetBodyAsString()
    {
        if (Body == null || Body.Count == 0)
            return null;

        return Body[0] switch
        {
            AmqpValue { Value: string s } => s,
            DataSection { Binary: var b } => System.Text.Encoding.UTF8.GetString(b.Span),
            _ => null
        };
    }

    /// <summary>
    /// Gets the body as binary data.
    /// </summary>
    public ReadOnlyMemory<byte>? GetBodyAsBinary()
    {
        if (Body == null || Body.Count == 0)
            return null;

        return Body[0] switch
        {
            DataSection { Binary: var b } => b,
            AmqpValue { Value: byte[] bytes } => bytes,
            AmqpValue { Value: string s } => System.Text.Encoding.UTF8.GetBytes(s),
            _ => null
        };
    }

    /// <summary>
    /// Decodes a message from a buffer (convenience overload).
    /// </summary>
    public static AmqpMessage Decode(ReadOnlySpan<byte> buffer)
    {
        return Decode(buffer, out _);
    }

    /// <summary>
    /// Gets the estimated encoded size of the message.
    /// </summary>
    public int GetEncodedSize()
    {
        int size = 0;
        
        // Estimate header size
        if (Header != null) size += 32;
        if (DeliveryAnnotations != null) size += 128;
        if (MessageAnnotations != null) size += 128;
        if (Properties != null) size += 256;
        if (ApplicationProperties != null) size += 512;
        
        // Body size
        if (Body != null)
        {
            foreach (var section in Body)
            {
                size += section switch
                {
                    DataSection data => 16 + data.Binary.Length,
                    AmqpValue { Value: string s } => 16 + System.Text.Encoding.UTF8.GetByteCount(s),
                    AmqpValue { Value: byte[] b } => 16 + b.Length,
                    _ => 64
                };
            }
        }
        
        if (Footer != null) size += 64;
        
        return Math.Max(size, 64);
    }
}
