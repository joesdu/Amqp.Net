// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Amqp.Net.Broker.Server.Configuration;
using Amqp.Net.Broker.Server.Connections;
using Amqp.Net.Broker.Server.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amqp.Net.Broker.Server.Transport;

/// <summary>
/// TCP listener for AMQP connections using System.IO.Pipelines.
/// </summary>
public sealed class AmqpListener : IAsyncDisposable
{
    private readonly AmqpServerOptions _options;
    private readonly IAmqpConnectionHandler _connectionHandler;
    private readonly ILogger<AmqpListener> _logger;
    private readonly List<Socket> _listenSockets = [];
    private readonly ConcurrentDictionary<string, AmqpConnectionContext> _connections = new();
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly PipeOptions _inputPipeOptions;
    private readonly PipeOptions _outputPipeOptions;
    
    private CancellationTokenSource? _cts;
    private Task? _acceptLoopTask;
    private long _connectionIdCounter;
    private bool _disposed;

    /// <summary>
    /// Creates a new AMQP listener.
    /// </summary>
    public AmqpListener(
        IOptions<AmqpServerOptions> options,
        IAmqpConnectionHandler connectionHandler,
        ILogger<AmqpListener> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connectionHandler);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _connectionHandler = connectionHandler;
        _logger = logger;
        _connectionSemaphore = new SemaphoreSlim(_options.MaxConnections, _options.MaxConnections);

        // Configure pipe options with memory limits for backpressure
        _inputPipeOptions = new PipeOptions(
            pauseWriterThreshold: _options.InputBufferSize * 2,
            resumeWriterThreshold: _options.InputBufferSize,
            useSynchronizationContext: false);

        _outputPipeOptions = new PipeOptions(
            pauseWriterThreshold: _options.OutputBufferSize * 2,
            resumeWriterThreshold: _options.OutputBufferSize,
            useSynchronizationContext: false);
    }

    /// <summary>
    /// Gets the number of active connections.
    /// </summary>
    public int ConnectionCount => _connections.Count;

    /// <summary>
    /// Gets all active connection IDs.
    /// </summary>
    public IEnumerable<string> ConnectionIds => _connections.Keys;

    /// <summary>
    /// Starts listening for connections.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_cts != null)
        {
            throw new InvalidOperationException("Listener is already started.");
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        foreach (var endpoint in _options.ListenEndpoints)
        {
            var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            // Socket options for performance
            socket.NoDelay = true;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            try
            {
                socket.Bind(endpoint);
                socket.Listen(_options.Backlog);
                _listenSockets.Add(socket);
                
                Log.ListenerStarted(_logger, endpoint);
            }
            catch (SocketException ex)
            {
                Log.BindFailed(_logger, ex, endpoint);
                socket.Dispose();
                throw;
            }
        }

        // Start accept loop for each socket
        _acceptLoopTask = Task.WhenAll(_listenSockets.Select(s => AcceptLoopAsync(s, _cts.Token)));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops listening and closes all connections.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts == null)
        {
            return;
        }

        Log.ListenerStopping(_logger);

        // Signal cancellation
        await _cts.CancelAsync().ConfigureAwait(false);

        // Close all listen sockets to unblock Accept calls
        foreach (var socket in _listenSockets)
        {
            try
            {
                socket.Close();
            }
            catch (SocketException)
            {
                // Socket may already be closed
            }
            catch (ObjectDisposedException)
            {
                // Socket already disposed
            }
        }

        // Wait for accept loop to complete
        if (_acceptLoopTask != null)
        {
            try
            {
                await _acceptLoopTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (TimeoutException ex)
            {
                Log.AcceptLoopWaitError(_logger, ex);
            }
        }

        // Close all active connections
        var closeTasks = _connections.Values.Select(CloseConnectionInternalAsync);
        await Task.WhenAll(closeTasks).ConfigureAwait(false);

        _connections.Clear();
        _listenSockets.Clear();
        
        _cts.Dispose();
        _cts = null;

        Log.ListenerStopped(_logger);
    }

    private async Task AcceptLoopAsync(Socket listenSocket, CancellationToken cancellationToken)
    {
        var endpoint = listenSocket.LocalEndPoint;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait for connection slot
                await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                Socket clientSocket;
                try
                {
                    clientSocket = await listenSocket.AcceptAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (SocketException)
                {
                    _connectionSemaphore.Release();
                    throw;
                }
                catch (OperationCanceledException)
                {
                    _connectionSemaphore.Release();
                    throw;
                }

                // Handle connection in background
                _ = HandleConnectionAsync(clientSocket, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
            {
                // Socket was closed, exit loop
                break;
            }
            catch (ObjectDisposedException)
            {
                // Socket was disposed, exit loop
                break;
            }
            catch (SocketException ex)
            {
                Log.AcceptError(_logger, ex, endpoint);
                
                // Brief delay before retrying to avoid tight loop on persistent errors
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleConnectionAsync(Socket socket, CancellationToken cancellationToken)
    {
        var connectionId = GenerateConnectionId();
        var remoteEndpoint = socket.RemoteEndPoint;
        
        Log.ConnectionAccepted(_logger, connectionId, remoteEndpoint);

        AmqpConnectionContext? context = null;
        
        try
        {
            // Configure socket
            socket.NoDelay = true;

            // Read and validate protocol header
            var headerResult = await ReadProtocolHeaderAsync(socket, cancellationToken).ConfigureAwait(false);
            
            if (headerResult == null)
            {
                Log.InvalidProtocolHeader(_logger, connectionId, remoteEndpoint);
                socket.Close();
                return;
            }

            var (protocolId, shouldRespond) = headerResult.Value;

            // Send protocol header response if needed
            if (shouldRespond)
            {
                await SendProtocolHeaderAsync(socket, protocolId, cancellationToken).ConfigureAwait(false);
            }

            // Create connection context
            context = AmqpConnectionContext.Create(
                socket,
                connectionId,
                protocolId,
                _inputPipeOptions,
                _outputPipeOptions);

            _connections.TryAdd(connectionId, context);

            var protocol = protocolId == ProtocolHeader.ProtocolIdSasl ? "SASL" : "AMQP";
            Log.ConnectionEstablished(_logger, connectionId, remoteEndpoint, protocol);

            // Hand off to connection handler
            await _connectionHandler.HandleConnectionAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Log.ConnectionCancelled(_logger, connectionId);
        }
        catch (SocketException ex)
        {
            Log.ConnectionError(_logger, ex, connectionId);
        }
        catch (IOException ex)
        {
            Log.ConnectionError(_logger, ex, connectionId);
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
            
            if (context != null)
            {
                await context.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                socket.Dispose();
            }

            _connectionSemaphore.Release();
            
            Log.ConnectionClosed(_logger, connectionId);
        }
    }

    private async Task<(byte ProtocolId, bool ShouldRespond)?> ReadProtocolHeaderAsync(
        Socket socket, 
        CancellationToken cancellationToken)
    {
        var buffer = new byte[ProtocolHeader.Size];
        var received = 0;

        // Read exactly 8 bytes for protocol header
        while (received < ProtocolHeader.Size)
        {
            var bytesRead = await socket.ReceiveAsync(
                buffer.AsMemory(received, ProtocolHeader.Size - received),
                SocketFlags.None,
                cancellationToken).ConfigureAwait(false);

            if (bytesRead == 0)
            {
                // Connection closed
                return null;
            }

            received += bytesRead;
        }

        var result = ProtocolHeader.Validate(buffer);

        switch (result)
        {
            case ProtocolHeaderResult.Amqp:
                return (ProtocolHeader.ProtocolIdAmqp, true);
            
            case ProtocolHeaderResult.Sasl:
                return (ProtocolHeader.ProtocolIdSasl, true);
            
            case ProtocolHeaderResult.Tls:
                Log.TlsNotSupported(_logger);
                // Send back AMQP header to indicate we don't support TLS
                await socket.SendAsync(ProtocolHeader.Amqp100.ToArray(), SocketFlags.None, cancellationToken).ConfigureAwait(false);
                return null;
            
            case ProtocolHeaderResult.UnsupportedVersion:
                Log.UnsupportedVersion(_logger);
                // Send back our supported version
                await socket.SendAsync(ProtocolHeader.Amqp100.ToArray(), SocketFlags.None, cancellationToken).ConfigureAwait(false);
                return null;
            
            default:
                Log.InvalidHeader(_logger, BitConverter.ToString(buffer));
                return null;
        }
    }

    private static async Task SendProtocolHeaderAsync(Socket socket, byte protocolId, CancellationToken cancellationToken)
    {
        var header = ProtocolHeader.GetHeader(protocolId).ToArray();
        await socket.SendAsync(header, SocketFlags.None, cancellationToken).ConfigureAwait(false);
    }

    private async Task CloseConnectionInternalAsync(AmqpConnectionContext context)
    {
        try
        {
            await context.DisposeAsync().ConfigureAwait(false);
        }
        catch (SocketException ex)
        {
            Log.CloseConnectionError(_logger, ex, context.ConnectionId);
        }
        catch (IOException ex)
        {
            Log.CloseConnectionError(_logger, ex, context.ConnectionId);
        }
    }

    private string GenerateConnectionId()
    {
        var id = Interlocked.Increment(ref _connectionIdCounter);
        return $"conn-{id:X8}";
    }

    /// <summary>
    /// Gets a connection by ID.
    /// </summary>
    public AmqpConnectionContext? GetConnection(string connectionId)
    {
        _connections.TryGetValue(connectionId, out var context);
        return context;
    }

    /// <summary>
    /// Closes a specific connection.
    /// </summary>
    public async Task CloseConnectionAsync(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var context))
        {
            return;
        }

        // Use await using to ensure disposal on all paths
        await using (context.ConfigureAwait(false))
        {
            // Context will be disposed when exiting this block
        }
    }

    /// <summary>
    /// Disposes the listener and all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await StopAsync().ConfigureAwait(false);
        
        _connectionSemaphore.Dispose();

        foreach (var socket in _listenSockets)
        {
            socket.Dispose();
        }
    }
}
