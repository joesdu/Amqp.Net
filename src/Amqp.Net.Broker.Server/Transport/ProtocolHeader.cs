// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Amqp.Net.Broker.Server.Transport;

/// <summary>
/// AMQP 1.0 protocol header constants and validation.
/// The protocol header is 8 bytes: "AMQP" + protocol-id + major + minor + revision
/// </summary>
public static class ProtocolHeader
{
    /// <summary>
    /// Size of the protocol header in bytes.
    /// </summary>
    public const int Size = 8;

    /// <summary>
    /// AMQP protocol identifier bytes: "AMQP"
    /// </summary>
    public static ReadOnlySpan<byte> AmqpPrefix => "AMQP"u8;

    /// <summary>
    /// AMQP 1.0 protocol header: AMQP 0 1 0 0
    /// Protocol-id 0 = AMQP
    /// </summary>
    public static ReadOnlySpan<byte> Amqp100 => [0x41, 0x4D, 0x51, 0x50, 0x00, 0x01, 0x00, 0x00];

    /// <summary>
    /// SASL protocol header: AMQP 3 1 0 0
    /// Protocol-id 3 = SASL
    /// </summary>
    public static ReadOnlySpan<byte> Sasl100 => [0x41, 0x4D, 0x51, 0x50, 0x03, 0x01, 0x00, 0x00];

    /// <summary>
    /// TLS protocol header: AMQP 2 1 0 0
    /// Protocol-id 2 = TLS
    /// </summary>
    public static ReadOnlySpan<byte> Tls100 => [0x41, 0x4D, 0x51, 0x50, 0x02, 0x01, 0x00, 0x00];

    /// <summary>
    /// Protocol identifier for plain AMQP.
    /// </summary>
    public const byte ProtocolIdAmqp = 0;

    /// <summary>
    /// Protocol identifier for TLS.
    /// </summary>
    public const byte ProtocolIdTls = 2;

    /// <summary>
    /// Protocol identifier for SASL.
    /// </summary>
    public const byte ProtocolIdSasl = 3;

    /// <summary>
    /// Tries to parse a protocol header from the buffer.
    /// </summary>
    /// <param name="buffer">The buffer to read from.</param>
    /// <param name="protocolId">The protocol identifier (0=AMQP, 2=TLS, 3=SASL).</param>
    /// <param name="version">The protocol version as (major, minor, revision).</param>
    /// <returns>True if the header is valid AMQP, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> buffer, out byte protocolId, out (byte Major, byte Minor, byte Revision) version)
    {
        if (buffer.Length < Size)
        {
            protocolId = 0;
            version = default;
            return false;
        }

        // Check "AMQP" prefix
        if (!buffer[..4].SequenceEqual(AmqpPrefix))
        {
            protocolId = 0;
            version = default;
            return false;
        }

        protocolId = buffer[4];
        version = (buffer[5], buffer[6], buffer[7]);
        return true;
    }

    /// <summary>
    /// Validates that the buffer contains a supported AMQP 1.0 protocol header.
    /// </summary>
    /// <param name="buffer">The buffer to validate.</param>
    /// <returns>The validation result.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ProtocolHeaderResult Validate(ReadOnlySpan<byte> buffer)
    {
        if (!TryParse(buffer, out byte protocolId, out var version))
        {
            return ProtocolHeaderResult.Invalid;
        }

        // Check version is 1.0.0
        if (version.Major != 1 || version.Minor != 0 || version.Revision != 0)
        {
            return ProtocolHeaderResult.UnsupportedVersion;
        }

        return protocolId switch
        {
            ProtocolIdAmqp => ProtocolHeaderResult.Amqp,
            ProtocolIdSasl => ProtocolHeaderResult.Sasl,
            ProtocolIdTls => ProtocolHeaderResult.Tls,
            _ => ProtocolHeaderResult.UnsupportedProtocol
        };
    }

    /// <summary>
    /// Writes the AMQP 1.0 protocol header to the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteAmqp(Span<byte> buffer)
    {
        Amqp100.CopyTo(buffer);
    }

    /// <summary>
    /// Writes the SASL 1.0 protocol header to the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSasl(Span<byte> buffer)
    {
        Sasl100.CopyTo(buffer);
    }

    /// <summary>
    /// Gets the protocol header bytes for the specified protocol ID.
    /// </summary>
    public static ReadOnlySpan<byte> GetHeader(byte protocolId)
    {
        return protocolId switch
        {
            ProtocolIdAmqp => Amqp100,
            ProtocolIdSasl => Sasl100,
            ProtocolIdTls => Tls100,
            _ => throw new ArgumentOutOfRangeException(nameof(protocolId), $"Unknown protocol ID: {protocolId}")
        };
    }
}

/// <summary>
/// Result of protocol header validation.
/// </summary>
public enum ProtocolHeaderResult
{
    /// <summary>
    /// Invalid or malformed header.
    /// </summary>
    Invalid,

    /// <summary>
    /// Valid AMQP 1.0 header.
    /// </summary>
    Amqp,

    /// <summary>
    /// Valid SASL 1.0 header.
    /// </summary>
    Sasl,

    /// <summary>
    /// Valid TLS 1.0 header.
    /// </summary>
    Tls,

    /// <summary>
    /// Unsupported protocol version.
    /// </summary>
    UnsupportedVersion,

    /// <summary>
    /// Unsupported protocol identifier.
    /// </summary>
    UnsupportedProtocol
}
