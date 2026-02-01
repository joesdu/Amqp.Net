// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace Amqp.Net.Broker.Server.Connections;

/// <summary>
/// Represents the context for an AMQP connection, including transport and state.
/// </summary>
public sealed class AmqpConnectionContext : IAsyncDisposable
{
    private readonly Socket _socket;
    private readonly NetworkStream _stream;
    private bool _disposed;

    /// <summary>
    /// Creates a new connection context from an accepted socket.
    /// </summary>
    internal AmqpConnectionContext(
        Socket socket,
        string connectionId,
        PipeReader input,
        PipeWriter output,
        byte protocolId)
    {
        _socket = socket;
        _stream = new NetworkStream(socket, ownsSocket: false);
        
        ConnectionId = connectionId;
        Input = input;
        Output = output;
        ProtocolId = protocolId;
        LocalEndPoint = socket.LocalEndPoint as IPEndPoint;
        RemoteEndPoint = socket.RemoteEndPoint as IPEndPoint;
        ConnectedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Unique identifier for this connection.
    /// </summary>
    public string ConnectionId { get; }

    /// <summary>
    /// The pipe reader for incoming data.
    /// </summary>
    public PipeReader Input { get; }

    /// <summary>
    /// The pipe writer for outgoing data.
    /// </summary>
    public PipeWriter Output { get; }

    /// <summary>
    /// The protocol ID from the initial handshake (0=AMQP, 3=SASL).
    /// </summary>
    public byte ProtocolId { get; }

    /// <summary>
    /// The local endpoint of the connection.
    /// </summary>
    public IPEndPoint? LocalEndPoint { get; }

    /// <summary>
    /// The remote endpoint of the connection.
    /// </summary>
    public IPEndPoint? RemoteEndPoint { get; }

    /// <summary>
    /// When the connection was established.
    /// </summary>
    public DateTimeOffset ConnectedAt { get; }

    /// <summary>
    /// The underlying network stream for direct access if needed.
    /// </summary>
    public Stream Transport => _stream;

    /// <summary>
    /// Custom properties associated with this connection.
    /// </summary>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    /// <summary>
    /// Aborts the connection immediately.
    /// </summary>
    public void Abort()
    {
        try
        {
            _socket.Close(0);
        }
        catch (SocketException)
        {
            // Expected during abort - socket may already be closed
        }
        catch (ObjectDisposedException)
        {
            // Socket already disposed
        }
    }

    /// <summary>
    /// Disposes the connection context and releases resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            await Input.CompleteAsync().ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // Pipe already completed
        }

        try
        {
            await Output.CompleteAsync().ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // Pipe already completed
        }

        try
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
        catch (IOException)
        {
            // Stream already closed or error during close
        }

        try
        {
            _socket.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Socket already disposed
        }
    }

    /// <summary>
    /// Creates a connection context from an accepted socket with pipelines.
    /// </summary>
    internal static AmqpConnectionContext Create(
        Socket socket,
        string connectionId,
        byte protocolId,
        PipeOptions? inputOptions = null,
        PipeOptions? outputOptions = null)
    {
        var stream = new NetworkStream(socket, ownsSocket: false);
        
        var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));
        var writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

        return new AmqpConnectionContext(socket, connectionId, reader, writer, protocolId);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Connection[{ConnectionId}] {RemoteEndPoint} -> {LocalEndPoint}";
    }
}
