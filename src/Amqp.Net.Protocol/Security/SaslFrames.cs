// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Amqp.Net.Protocol.Types;

namespace Amqp.Net.Protocol.Security;

/// <summary>
/// SASL Mechanisms - advertises available SASL mechanisms.
/// </summary>
public sealed class SaslMechanisms
{
    /// <summary>Descriptor code.</summary>
    public const ulong DescriptorCode = Descriptor.SaslMechanisms;

    /// <summary>Available SASL mechanisms.</summary>
    public required string[] Mechanisms { get; init; }

    /// <summary>Encodes the SASL mechanisms frame.</summary>
    public int Encode(Span<byte> buffer)
    {
        var offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        Span<byte> body = stackalloc byte[256];
        var bodySize = AmqpEncoder.EncodeSymbolArray(body, Mechanisms);
        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, 1);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;
        return offset;
    }

    /// <summary>Decodes SASL mechanisms.</summary>
    public static SaslMechanisms Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out var headerSize);
        bytesConsumed = headerSize + size;
        if (count < 1)
        {
            throw new AmqpDecodeException("SaslMechanisms requires at least 1 field");
        }
        var offset = headerSize;
        var mechanisms = AmqpDecoder.DecodeSymbolArray(buffer[offset..], out _);
        return new()
        {
            Mechanisms = mechanisms ?? []
        };
    }
}

/// <summary>Common SASL mechanism names.</summary>
public static class SaslMechanismNames
{
    /// <summary>Anonymous authentication.</summary>
    public const string Anonymous = "ANONYMOUS";

    /// <summary>Plain username/password authentication.</summary>
    public const string Plain = "PLAIN";

    /// <summary>External authentication (e.g., TLS client cert).</summary>
    public const string External = "EXTERNAL";
}

/// <summary>
/// SASL Init - initiates SASL authentication.
/// </summary>
public sealed class SaslInit
{
    /// <summary>Descriptor code.</summary>
    public const ulong DescriptorCode = Descriptor.SaslInit;

    /// <summary>Selected SASL mechanism.</summary>
    public required string Mechanism { get; init; }

    /// <summary>Initial response data.</summary>
    public byte[]? InitialResponse { get; init; }

    /// <summary>Hostname.</summary>
    public string? Hostname { get; init; }

    /// <summary>Encodes the SASL init frame.</summary>
    public int Encode(Span<byte> buffer)
    {
        var offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        var fieldCount = GetFieldCount();
        Span<byte> body = stackalloc byte[512];
        var bodySize = EncodeFields(body, fieldCount);
        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;
        return offset;
    }

    private int EncodeFields(Span<byte> buffer, int fieldCount)
    {
        var offset = 0;

        // Field 0: mechanism (mandatory)
        offset += AmqpEncoder.EncodeSymbol(buffer[offset..], Mechanism);
        if (fieldCount > 1)
        {
            // Field 1: initial-response
            if (InitialResponse != null)
            {
                offset += AmqpEncoder.EncodeBinary(buffer[offset..], InitialResponse);
            }
            else
            {
                offset += AmqpEncoder.EncodeNull(buffer[offset..]);
            }
        }
        if (fieldCount > 2)
        {
            // Field 2: hostname
            offset += AmqpEncoder.EncodeString(buffer[offset..], Hostname);
        }
        return offset;
    }

    private int GetFieldCount()
    {
        if (Hostname != null)
        {
            return 3;
        }
        if (InitialResponse != null)
        {
            return 2;
        }
        return 1;
    }

    /// <summary>Decodes SASL init.</summary>
    public static SaslInit Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out var headerSize);
        bytesConsumed = headerSize + size;
        if (count < 1)
        {
            throw new AmqpDecodeException("SaslInit requires at least 1 field");
        }
        var offset = headerSize;

        // Field 0: mechanism
        var mechanism = AmqpDecoder.DecodeSymbol(buffer[offset..], out var consumed);
        offset += consumed;
        byte[]? initialResponse = null;
        string? hostname = null;
        if (count > 1)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
            {
                var bin = AmqpDecoder.DecodeBinary(buffer[offset..], out consumed);
                initialResponse = bin.ToArray();
            }
            offset += consumed;
        }
        if (count > 2)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
            {
                hostname = AmqpDecoder.DecodeString(buffer[offset..], out consumed);
            }
            offset += consumed;
        }
        return new()
        {
            Mechanism = mechanism,
            InitialResponse = initialResponse,
            Hostname = hostname
        };
    }

    /// <summary>
    /// Creates a PLAIN authentication init frame.
    /// </summary>
    public static SaslInit CreatePlain(string username, string password, string? hostname = null)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);

        // PLAIN format: \0username\0password
        var response = new byte[1 + username.Length + 1 + password.Length];
        response[0] = 0;
        Encoding.UTF8.GetBytes(username, response.AsSpan(1));
        response[1 + username.Length] = 0;
        Encoding.UTF8.GetBytes(password, response.AsSpan(2 + username.Length));
        return new()
        {
            Mechanism = SaslMechanismNames.Plain,
            InitialResponse = response,
            Hostname = hostname
        };
    }

    /// <summary>
    /// Creates an ANONYMOUS authentication init frame.
    /// </summary>
    public static SaslInit CreateAnonymous(string? hostname = null) =>
        new()
        {
            Mechanism = SaslMechanismNames.Anonymous,
            Hostname = hostname
        };
}

/// <summary>
/// SASL Challenge - server challenge during authentication.
/// </summary>
public sealed class SaslChallenge
{
    /// <summary>Descriptor code.</summary>
    public const ulong DescriptorCode = Descriptor.SaslChallenge;

    /// <summary>Challenge data.</summary>
    public required byte[] Challenge { get; init; }

    /// <summary>Encodes the SASL challenge frame.</summary>
    public int Encode(Span<byte> buffer)
    {
        var offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        Span<byte> body = stackalloc byte[256];
        var bodySize = AmqpEncoder.EncodeBinary(body, Challenge);
        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, 1);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;
        return offset;
    }

    /// <summary>Decodes SASL challenge.</summary>
    public static SaslChallenge Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out var headerSize);
        bytesConsumed = headerSize + size;
        if (count < 1)
        {
            throw new AmqpDecodeException("SaslChallenge requires at least 1 field");
        }
        var offset = headerSize;
        var challenge = AmqpDecoder.DecodeBinary(buffer[offset..], out _);
        return new()
        {
            Challenge = challenge.ToArray()
        };
    }
}

/// <summary>
/// SASL Response - client response to server challenge.
/// </summary>
public sealed class SaslResponse
{
    /// <summary>Descriptor code.</summary>
    public const ulong DescriptorCode = Descriptor.SaslResponse;

    /// <summary>Response data.</summary>
    public required byte[] Response { get; init; }

    /// <summary>Encodes the SASL response frame.</summary>
    public int Encode(Span<byte> buffer)
    {
        var offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        Span<byte> body = stackalloc byte[256];
        var bodySize = AmqpEncoder.EncodeBinary(body, Response);
        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, 1);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;
        return offset;
    }

    /// <summary>Decodes SASL response.</summary>
    public static SaslResponse Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out var headerSize);
        bytesConsumed = headerSize + size;
        if (count < 1)
        {
            throw new AmqpDecodeException("SaslResponse requires at least 1 field");
        }
        var offset = headerSize;
        var response = AmqpDecoder.DecodeBinary(buffer[offset..], out _);
        return new()
        {
            Response = response.ToArray()
        };
    }
}

/// <summary>
/// SASL Outcome - authentication result.
/// </summary>
public sealed class SaslOutcome
{
    /// <summary>Descriptor code.</summary>
    public const ulong DescriptorCode = Descriptor.SaslOutcome;

    /// <summary>Authentication result code.</summary>
    public required SaslCode Code { get; init; }

    /// <summary>Additional data from server.</summary>
    public byte[]? AdditionalData { get; init; }

    /// <summary>Returns true if authentication succeeded.</summary>
    public bool IsSuccess => Code == SaslCode.Ok;

    /// <summary>Encodes the SASL outcome frame.</summary>
    public int Encode(Span<byte> buffer)
    {
        var offset = 0;
        buffer[offset++] = FormatCode.Described;
        offset += AmqpEncoder.EncodeULong(buffer[offset..], DescriptorCode);
        var fieldCount = AdditionalData != null ? 2 : 1;
        Span<byte> body = stackalloc byte[256];
        var bodySize = 0;
        bodySize += AmqpEncoder.EncodeUByte(body[bodySize..], (byte)Code);
        if (AdditionalData != null)
        {
            bodySize += AmqpEncoder.EncodeBinary(body[bodySize..], AdditionalData);
        }
        offset += AmqpEncoder.EncodeListHeader(buffer[offset..], bodySize, fieldCount);
        body[..bodySize].CopyTo(buffer[offset..]);
        offset += bodySize;
        return offset;
    }

    /// <summary>Decodes SASL outcome.</summary>
    public static SaslOutcome Decode(ReadOnlySpan<byte> buffer, out int bytesConsumed)
    {
        var (size, count) = AmqpDecoder.DecodeListHeader(buffer, out var headerSize);
        bytesConsumed = headerSize + size;
        if (count < 1)
        {
            throw new AmqpDecodeException("SaslOutcome requires at least 1 field");
        }
        var offset = headerSize;

        // Field 0: code
        var code = AmqpDecoder.DecodeUByte(buffer[offset..], out var consumed);
        offset += consumed;
        byte[]? additionalData = null;
        if (count > 1)
        {
            if (!AmqpDecoder.DecodeNull(buffer[offset..], out consumed))
            {
                var bin = AmqpDecoder.DecodeBinary(buffer[offset..], out consumed);
                additionalData = bin.ToArray();
            }
            offset += consumed;
        }
        return new()
        {
            Code = (SaslCode)code,
            AdditionalData = additionalData
        };
    }
}

/// <summary>
/// SASL outcome codes.
/// </summary>
#pragma warning disable CA1028 // Enum storage should be Int32 - AMQP spec requires byte
public enum SaslCode : byte
#pragma warning restore CA1028
{
    /// <summary>Authentication succeeded.</summary>
    Ok = 0,

    /// <summary>Authentication failed.</summary>
    Auth = 1,

    /// <summary>System error.</summary>
    Sys = 2,

    /// <summary>System error (permanent).</summary>
    SysPerm = 3,

    /// <summary>System error (temporary).</summary>
    SysTemp = 4
}
