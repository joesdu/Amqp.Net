// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Amqp.Net.Broker.Server.Exceptions;
using Amqp.Net.Protocol.Framing;
using Amqp.Net.Protocol.Performatives;

namespace Amqp.Net.Broker.Server.Transport;

/// <summary>
/// Writes AMQP frames to a PipeWriter.
/// </summary>
internal sealed class FrameWriter : IDisposable
{
    private readonly uint _maxFrameSize;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly PipeWriter _writer;
    private bool _disposed;

    /// <summary>
    /// Creates a new frame writer.
    /// </summary>
    public FrameWriter(PipeWriter writer, uint maxFrameSize)
    {
        _writer = writer;
        _maxFrameSize = maxFrameSize;
    }

    /// <summary>
    /// Disposes the frame writer.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _writeLock.Dispose();
    }

    /// <summary>
    /// Writes a performative frame.
    /// </summary>
    public async ValueTask WriteFrameAsync(
        ushort channel,
        IPerformative performative,
        CancellationToken cancellationToken)
    {
        await WriteFrameAsync(channel, performative, ReadOnlyMemory<byte>.Empty, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a performative frame with optional payload.
    /// </summary>
    public async ValueTask WriteFrameAsync(
        ushort channel,
        IPerformative performative,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Encode performative
            var performativeSize = performative.GetEncodedSize();
            var bodySize = performativeSize + payload.Length;
            var frameSize = FrameHeader.Size + bodySize;
            if (frameSize > _maxFrameSize)
            {
                throw new AmqpConnectionException($"Frame size {frameSize} exceeds maximum {_maxFrameSize}");
            }

            // Get buffer from pipe
            var memory = _writer.GetMemory(frameSize);
            var span = memory.Span;

            // Write frame header
            var header = FrameHeader.CreateAmqp((uint)frameSize, channel);
            header.Write(span);

            // Write performative
            var written = performative.Encode(span[FrameHeader.Size..]);

            // Write payload if present
            if (!payload.IsEmpty)
            {
                payload.Span.CopyTo(span[(FrameHeader.Size + written)..]);
            }
            _writer.Advance(frameSize);
            await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Writes an empty frame (heartbeat).
    /// </summary>
    public async ValueTask WriteHeartbeatAsync(ushort channel, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var memory = _writer.GetMemory(FrameHeader.Size);
            var header = FrameHeader.CreateHeartbeat(channel);
            header.Write(memory.Span);
            _writer.Advance(FrameHeader.Size);
            await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Writes raw bytes directly to the pipe (for protocol header).
    /// </summary>
    public async ValueTask WriteRawAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var memory = _writer.GetMemory(data.Length);
            data.Span.CopyTo(memory.Span);
            _writer.Advance(data.Length);
            await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
