// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using Amqp.Net.Broker.Server.Exceptions;
using Amqp.Net.Protocol.Framing;
using Amqp.Net.Protocol.Performatives;

namespace Amqp.Net.Broker.Server.Transport;

/// <summary>
/// Reads AMQP frames from a PipeReader.
/// </summary>
internal sealed class FrameReader
{
    private readonly uint _maxFrameSize;
    private readonly PipeReader _reader;

    /// <summary>
    /// Creates a new frame reader.
    /// </summary>
    public FrameReader(PipeReader reader, uint maxFrameSize)
    {
        _reader = reader;
        _maxFrameSize = maxFrameSize;
    }

    /// <summary>
    /// Reads the next frame from the pipe.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The frame, or null if the connection was closed.</returns>
    public async ValueTask<AmqpFrame?> ReadFrameAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;
            try
            {
                if (TryReadFrame(ref buffer, out var frame))
                {
                    _reader.AdvanceTo(buffer.Start);
                    return frame;
                }
                if (result.IsCompleted)
                {
                    if (buffer.Length > 0)
                    {
                        throw new AmqpConnectionException("Connection closed with incomplete frame data");
                    }
                    return null;
                }
            }
            finally
            {
                _reader.AdvanceTo(buffer.Start, buffer.End);
            }
        }
    }

    private bool TryReadFrame(ref ReadOnlySequence<byte> buffer, out AmqpFrame? frame)
    {
        frame = null;

        // Need at least the frame header
        if (buffer.Length < FrameHeader.Size)
        {
            return false;
        }

        // Read frame header
        Span<byte> headerBytes = stackalloc byte[FrameHeader.Size];
        buffer.Slice(0, FrameHeader.Size).CopyTo(headerBytes);
        var header = FrameHeader.Read(headerBytes);

        // Validate frame size
        if (header.FrameSize > _maxFrameSize)
        {
            throw new AmqpConnectionException($"Frame size {header.FrameSize} exceeds maximum {_maxFrameSize}");
        }
        if (!header.IsValid)
        {
            throw new AmqpConnectionException($"Invalid frame header: {header}");
        }

        // Need the full frame
        if (buffer.Length < header.FrameSize)
        {
            return false;
        }

        // Extract frame body
        var frameSlice = buffer.Slice(0, header.FrameSize);
        IPerformative? performative = null;
        var payload = ReadOnlyMemory<byte>.Empty;
        if (header.BodySize > 0)
        {
            // Read body
            var bodySlice = frameSlice.Slice(header.BodyOffset, header.BodySize);
            var bodyArray = bodySlice.ToArray();

            // Decode performative
            performative = PerformativeDecoder.Decode(bodyArray, out var consumed);

            // Any remaining bytes are payload (for Transfer frames)
            if (consumed < bodyArray.Length)
            {
                payload = new(bodyArray, consumed, bodyArray.Length - consumed);
            }
        }
        frame = new AmqpFrame(header, performative, payload);
        buffer = buffer.Slice(header.FrameSize);
        return true;
    }
}

/// <summary>
/// Represents a decoded AMQP frame.
/// </summary>
/// <param name="Header">The frame header.</param>
/// <param name="Performative">The performative (frame body), or null for empty frames.</param>
/// <param name="Payload">Additional payload data (for Transfer frames).</param>
public readonly record struct AmqpFrame(
    FrameHeader Header,
    IPerformative? Performative,
    ReadOnlyMemory<byte> Payload)
{
    /// <summary>
    /// Returns true if this is an empty frame (heartbeat).
    /// </summary>
    public bool IsEmpty => Header.IsEmpty;

    /// <summary>
    /// Returns true if this is an AMQP frame.
    /// </summary>
    public bool IsAmqp => Header.IsAmqpFrame;

    /// <summary>
    /// Returns true if this is a SASL frame.
    /// </summary>
    public bool IsSasl => Header.IsSaslFrame;

    /// <summary>
    /// Gets the channel number.
    /// </summary>
    public ushort Channel => Header.Channel;
}
