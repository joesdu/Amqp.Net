// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net.Sockets;
using Amqp.Net.Broker.Server.Configuration;
using Amqp.Net.Broker.Server.Exceptions;
using Amqp.Net.Broker.Server.Handlers;
using Amqp.Net.Broker.Server.Logging;
using Amqp.Net.Broker.Server.Sessions;
using Amqp.Net.Broker.Server.Transport;
using Amqp.Net.Protocol.Performatives;
using Microsoft.Extensions.Logging;

namespace Amqp.Net.Broker.Server.Connections;

/// <summary>
/// Represents an AMQP connection with state machine management.
/// Handles Open/Close negotiation and frame routing.
/// </summary>
public sealed class AmqpConnection : IAsyncDisposable
{
    private readonly TaskCompletionSource _closedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource _connectionCts;
    private readonly AmqpConnectionContext _context;
    private readonly FrameReader _frameReader;
    private readonly ILogger _logger;
    private readonly AmqpServerOptions _options;
    private readonly ConcurrentDictionary<ushort, AmqpSession> _sessions = new();
    private bool _disposed;
    private Timer? _idleTimer;
    private DateTimeOffset _lastActivity;
    private uint _negotiatedIdleTimeout;

    /// <summary>
    /// Creates a new AMQP connection.
    /// </summary>
    internal AmqpConnection(
        AmqpConnectionContext context,
        AmqpServerOptions options,
        ILogger logger,
        IBrokerLinkHandler? linkHandler = null)
    {
        _context = context;
        _options = options;
        _logger = logger;
        LinkHandler = linkHandler;
        _connectionCts = new();
        MaxFrameSize = options.MaxFrameSize;
        _lastActivity = DateTimeOffset.UtcNow;
        _frameReader = new(context.Input, options.MaxFrameSize);
        FrameWriter = new(context.Output, options.MaxFrameSize);
    }

    /// <summary>
    /// Gets the connection ID.
    /// </summary>
    public string ConnectionId => _context.ConnectionId;

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    public ConnectionState State { get; private set; } = ConnectionState.Start;

    /// <summary>
    /// Gets the remote Open performative (after connection is opened).
    /// </summary>
    public Open? RemoteOpen { get; private set; }

    /// <summary>
    /// Gets the local Open performative (after connection is opened).
    /// </summary>
    public Open? LocalOpen { get; private set; }

    /// <summary>
    /// Gets the negotiated maximum frame size.
    /// </summary>
    public uint MaxFrameSize { get; private set; }

    /// <summary>
    /// Gets the negotiated channel maximum.
    /// </summary>
    public ushort ChannelMax { get; private set; }

    /// <summary>
    /// Gets a task that completes when the connection is closed.
    /// </summary>
    public Task Closed => _closedTcs.Task;

    /// <summary>
    /// Gets all sessions on this connection.
    /// </summary>
    public IEnumerable<AmqpSession> Sessions => _sessions.Values;

    /// <summary>
    /// Gets the frame writer for internal use.
    /// </summary>
    internal FrameWriter FrameWriter { get; }

    /// <summary>
    /// Gets the broker link handler for internal use.
    /// </summary>
    internal IBrokerLinkHandler? LinkHandler { get; }

    /// <summary>
    /// Disposes the connection and releases resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        await _connectionCts.CancelAsync().ConfigureAwait(false);
        _connectionCts.Dispose();
        if (_idleTimer != null)
        {
            await _idleTimer.DisposeAsync().ConfigureAwait(false);
        }
        FrameWriter.Dispose();
    }

    /// <summary>
    /// Gets a session by channel.
    /// </summary>
    public AmqpSession? GetSession(ushort channel)
    {
        _sessions.TryGetValue(channel, out var session);
        return session;
    }

    /// <summary>
    /// Runs the connection, processing frames until closed.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _connectionCts.Token);
        var token = linkedCts.Token;
        try
        {
            // Protocol header already exchanged by listener
            State = ConnectionState.HeaderExchanged;
            Log.ConnectionStateChanged(_logger, ConnectionId, State);

            // Main frame processing loop
            while (!token.IsCancellationRequested && !State.IsTerminal())
            {
                var frame = await _frameReader.ReadFrameAsync(token).ConfigureAwait(false);
                if (frame == null)
                {
                    // Connection closed by remote
                    Log.ConnectionClosedByRemote(_logger, ConnectionId);
                    break;
                }
                _lastActivity = DateTimeOffset.UtcNow;
                await ProcessFrameAsync(frame.Value, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Log.ConnectionCancelled(_logger, ConnectionId);
        }
        catch (AmqpConnectionException ex)
        {
            Log.ConnectionProtocolError(_logger, ex, ConnectionId, ex.Message);
            await TryCloseWithErrorAsync("amqp:connection:framing-error", ex.Message, token)
                .ConfigureAwait(false);
        }
        catch (SocketException ex)
        {
            Log.ConnectionUnexpectedError(_logger, ex, ConnectionId);
            await TryCloseWithErrorAsync("amqp:internal-error", "Socket error", token)
                .ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            Log.ConnectionUnexpectedError(_logger, ex, ConnectionId);
            await TryCloseWithErrorAsync("amqp:internal-error", "I/O error", token)
                .ConfigureAwait(false);
        }
        finally
        {
            State = ConnectionState.End;
            if (_idleTimer != null)
            {
                await _idleTimer.DisposeAsync().ConfigureAwait(false);
            }
            _closedTcs.TrySetResult();
            Log.ConnectionEnded(_logger, ConnectionId);
        }
    }

    private async Task ProcessFrameAsync(AmqpFrame frame, CancellationToken cancellationToken)
    {
        // Handle empty frames (heartbeats)
        if (frame.IsEmpty)
        {
            Log.HeartbeatReceived(_logger, ConnectionId);
            return;
        }
        if (frame.Performative == null)
        {
            throw new AmqpConnectionException("Non-empty frame without performative");
        }

        // Route based on performative type
        switch (frame.Performative)
        {
            case Open open:
                await HandleOpenAsync(open, cancellationToken).ConfigureAwait(false);
                break;
            case Close close:
                await HandleCloseAsync(close, cancellationToken).ConfigureAwait(false);
                break;
            case Begin begin:
                await HandleBeginAsync(frame.Channel, begin, cancellationToken).ConfigureAwait(false);
                break;
            case End end:
                await HandleEndAsync(frame.Channel, end, cancellationToken).ConfigureAwait(false);
                break;
            case Attach attach:
                await HandleAttachAsync(frame.Channel, attach, cancellationToken).ConfigureAwait(false);
                break;
            case Detach detach:
                await HandleDetachAsync(frame.Channel, detach, cancellationToken).ConfigureAwait(false);
                break;
            case Flow flow:
                await HandleFlowAsync(frame.Channel, flow, cancellationToken).ConfigureAwait(false);
                break;
            case Transfer transfer:
                await HandleTransferAsync(frame.Channel, transfer, frame.Payload, cancellationToken)
                    .ConfigureAwait(false);
                break;
            case Disposition disposition:
                await HandleDispositionAsync(frame.Channel, disposition, cancellationToken)
                    .ConfigureAwait(false);
                break;
            default:
                throw new AmqpConnectionException($"Unknown performative type: {frame.Performative.GetType().Name}");
        }
    }

    private async Task HandleOpenAsync(Open open, CancellationToken cancellationToken)
    {
        if (State != ConnectionState.HeaderExchanged && State != ConnectionState.OpenSent)
        {
            throw new AmqpConnectionException($"Received Open in invalid state: {State}");
        }
        RemoteOpen = open;
        Log.OpenReceived(_logger, ConnectionId, open.ContainerId, open.MaxFrameSize, open.ChannelMax);

        // Negotiate parameters
        MaxFrameSize = Math.Min(_options.MaxFrameSize, open.MaxFrameSize);
        ChannelMax = Math.Min((ushort)65535, open.ChannelMax);

        // Negotiate idle timeout (use minimum of both, 0 means disabled)
        var localTimeout = _options.IdleTimeoutMs;
        var remoteTimeout = open.IdleTimeOut ?? 0;
        if (localTimeout == 0)
        {
            _negotiatedIdleTimeout = remoteTimeout;
        }
        else if (remoteTimeout == 0)
        {
            _negotiatedIdleTimeout = localTimeout;
        }
        else
        {
            _negotiatedIdleTimeout = Math.Min(localTimeout, remoteTimeout);
        }

        // Send our Open
        LocalOpen = new()
        {
            ContainerId = _options.ContainerId,
            MaxFrameSize = _options.MaxFrameSize,
            ChannelMax = 65535,
            IdleTimeOut = _options.IdleTimeoutMs > 0 ? _options.IdleTimeoutMs : null,
            OfferedCapabilities = null,
            DesiredCapabilities = null,
            Properties = null
        };
        await FrameWriter.WriteFrameAsync(0, LocalOpen, cancellationToken).ConfigureAwait(false);
        Log.OpenSent(_logger, ConnectionId, LocalOpen.ContainerId);
        State = ConnectionState.Opened;
        Log.ConnectionStateChanged(_logger, ConnectionId, State);

        // Start idle timer if negotiated
        if (_negotiatedIdleTimeout > 0)
        {
            StartIdleTimer();
        }
        Log.ConnectionOpened(_logger, ConnectionId, MaxFrameSize, ChannelMax, _negotiatedIdleTimeout);
    }

    private async Task HandleCloseAsync(Close close, CancellationToken cancellationToken)
    {
        Log.CloseReceived(_logger, ConnectionId, close.Error?.Condition);
        if (State == ConnectionState.ClosePipe)
        {
            // We already sent Close, just transition to End
            State = ConnectionState.End;
        }
        else
        {
            // Send Close response
            State = ConnectionState.CloseReceived;
            var response = new Close();
            await FrameWriter.WriteFrameAsync(0, response, cancellationToken).ConfigureAwait(false);
            Log.CloseSent(_logger, ConnectionId);
            State = ConnectionState.End;
        }
        Log.ConnectionStateChanged(_logger, ConnectionId, State);
    }

    private async Task HandleBeginAsync(ushort channel, Begin begin, CancellationToken cancellationToken)
    {
        Log.SessionBeginReceived(_logger, ConnectionId, channel, begin.NextOutgoingId);
        if (State != ConnectionState.Opened)
        {
            throw new AmqpConnectionException($"Received Begin in invalid connection state: {State}");
        }

        // Create new session
        var session = new AmqpSession(this, channel, FrameWriter, _logger);
        if (!_sessions.TryAdd(channel, session))
        {
            throw new AmqpConnectionException($"Session already exists on channel {channel}");
        }
        await session.HandleBeginAsync(begin, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleEndAsync(ushort channel, End end, CancellationToken cancellationToken)
    {
        Log.SessionEndReceived(_logger, ConnectionId, channel);
        if (!_sessions.TryGetValue(channel, out var session))
        {
            Log.SessionNotFound(_logger, ConnectionId, channel);
            return;
        }
        await session.HandleEndAsync(end, cancellationToken).ConfigureAwait(false);

        // Remove session if ended
        if (session.State.IsTerminal())
        {
            _sessions.TryRemove(channel, out _);
        }
    }

    private async Task HandleAttachAsync(ushort channel, Attach attach, CancellationToken cancellationToken)
    {
        Log.LinkAttachReceived(_logger, ConnectionId, channel, attach.Name, attach.Role);
        if (!_sessions.TryGetValue(channel, out var session))
        {
            Log.SessionNotFound(_logger, ConnectionId, channel);
            return;
        }
        await session.HandleAttachAsync(attach, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleDetachAsync(ushort channel, Detach detach, CancellationToken cancellationToken)
    {
        Log.LinkDetachReceived(_logger, ConnectionId, channel, detach.Handle);
        if (!_sessions.TryGetValue(channel, out var session))
        {
            Log.SessionNotFound(_logger, ConnectionId, channel);
            return;
        }
        await session.HandleDetachAsync(detach, cancellationToken).ConfigureAwait(false);
    }

    private Task HandleFlowAsync(ushort channel, Flow flow, CancellationToken cancellationToken)
    {
        Log.FlowReceived(_logger, ConnectionId, channel, flow.NextIncomingId, flow.IncomingWindow);
        if (!_sessions.TryGetValue(channel, out var session))
        {
            Log.SessionNotFound(_logger, ConnectionId, channel);
            return Task.CompletedTask;
        }
        return session.HandleFlowAsync(flow, cancellationToken);
    }

    private Task HandleTransferAsync(ushort channel, Transfer transfer, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        Log.TransferReceived(_logger, ConnectionId, channel, transfer.DeliveryId, payload.Length);
        if (!_sessions.TryGetValue(channel, out var session))
        {
            Log.SessionNotFound(_logger, ConnectionId, channel);
            return Task.CompletedTask;
        }
        return session.HandleTransferAsync(transfer, payload, cancellationToken);
    }

    private Task HandleDispositionAsync(ushort channel, Disposition disposition, CancellationToken cancellationToken)
    {
        Log.DispositionReceived(_logger, ConnectionId, channel, disposition.First, disposition.Last);
        if (!_sessions.TryGetValue(channel, out var session))
        {
            Log.SessionNotFound(_logger, ConnectionId, channel);
            return Task.CompletedTask;
        }
        return session.HandleDispositionAsync(disposition, cancellationToken);
    }

    private void StartIdleTimer()
    {
        // Send heartbeat at half the idle timeout to keep connection alive
        var interval = TimeSpan.FromMilliseconds(_negotiatedIdleTimeout / 2);
        _idleTimer = new(OnIdleTimerTick, null, interval, interval);
    }

    private async void OnIdleTimerTick(object? state)
    {
        try
        {
            if (State != ConnectionState.Opened)
            {
                return;
            }
            var elapsed = DateTimeOffset.UtcNow - _lastActivity;
            if (elapsed.TotalMilliseconds > _negotiatedIdleTimeout)
            {
                // Remote hasn't sent anything, close connection
                Log.IdleTimeoutExpired(_logger, ConnectionId, _negotiatedIdleTimeout);
                await CloseAsync("amqp:resource-limit-exceeded", "Idle timeout expired").ConfigureAwait(false);
                return;
            }

            // Send heartbeat
            await FrameWriter.WriteHeartbeatAsync(0, _connectionCts.Token).ConfigureAwait(false);
            Log.HeartbeatSent(_logger, ConnectionId);
        }
        catch (OperationCanceledException)
        {
            // Connection is closing, ignore
        }
        catch (ObjectDisposedException)
        {
            // Writer disposed, ignore
        }
        catch (SocketException ex)
        {
            Log.HeartbeatError(_logger, ex, ConnectionId);
        }
        catch (IOException ex)
        {
            Log.HeartbeatError(_logger, ex, ConnectionId);
        }
    }

    /// <summary>
    /// Closes the connection gracefully.
    /// </summary>
    public Task CloseAsync(string? errorCondition = null, string? errorDescription = null) => CloseInternalAsync(errorCondition, errorDescription, _connectionCts.Token);

    private async Task CloseInternalAsync(string? errorCondition, string? errorDescription, CancellationToken cancellationToken)
    {
        if (State.IsTerminal() || State.IsClosing())
        {
            return;
        }
        State = ConnectionState.ClosePipe;
        Log.ConnectionStateChanged(_logger, ConnectionId, State);
        var close = new Close
        {
            Error = errorCondition != null
                        ? new AmqpError
                        {
                            Condition = errorCondition,
                            Description = errorDescription
                        }
                        : null
        };
        await FrameWriter.WriteFrameAsync(0, close, cancellationToken).ConfigureAwait(false);
        Log.CloseSent(_logger, ConnectionId);
    }

    private async Task TryCloseWithErrorAsync(string condition, string description, CancellationToken cancellationToken)
    {
        try
        {
            await CloseInternalAsync(condition, description, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignore - connection is closing
        }
        catch (ObjectDisposedException)
        {
            // Ignore - already disposed
        }
        catch (SocketException)
        {
            // Ignore - socket error during close
        }
        catch (IOException)
        {
            // Ignore - I/O error during close
        }
    }

    /// <summary>
    /// Aborts the connection immediately.
    /// </summary>
    public void Abort()
    {
        _connectionCts.Cancel();
        _context.Abort();
    }
}
