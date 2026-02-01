// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Amqp.Net.Broker.Server.Connections;
using Amqp.Net.Broker.Server.Exceptions;
using Amqp.Net.Broker.Server.Handlers;
using Amqp.Net.Broker.Server.Links;
using Amqp.Net.Broker.Server.Logging;
using Amqp.Net.Broker.Server.Transport;
using Amqp.Net.Protocol.Performatives;
using Amqp.Net.Protocol.Types;
using Microsoft.Extensions.Logging;

namespace Amqp.Net.Broker.Server.Sessions;

/// <summary>
/// Represents an AMQP session within a connection.
/// Manages links and transfer state.
/// </summary>
public sealed class AmqpSession
{
    private readonly AmqpConnection _connection;
    private readonly FrameWriter _frameWriter;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<uint, AmqpLink> _localLinks = new();
    private readonly ConcurrentDictionary<uint, AmqpLink> _remoteLinks = new();
    
    private AmqpSessionState _state = AmqpSessionState.Unmapped;
    private Begin? _remoteBegin;
    private Begin? _localBegin;
    
    // Flow control state
    private uint _nextOutgoingId;
    private uint _nextIncomingId;
    private uint _incomingWindow;
    private uint _outgoingWindow;
    private uint _remoteIncomingWindow;
    private uint _remoteOutgoingWindow;
    private uint _handleMax;

    /// <summary>
    /// Creates a new AMQP session.
    /// </summary>
    internal AmqpSession(
        AmqpConnection connection,
        ushort localChannel,
        FrameWriter frameWriter,
        ILogger logger)
    {
        _connection = connection;
        LocalChannel = localChannel;
        _frameWriter = frameWriter;
        _logger = logger;
        
        // Default window sizes
        _incomingWindow = 2048;
        _outgoingWindow = 2048;
        _handleMax = uint.MaxValue;
        _nextOutgoingId = 0;
    }

    /// <summary>
    /// Gets the local channel number.
    /// </summary>
    public ushort LocalChannel { get; }

    /// <summary>
    /// Gets the remote channel number (after session is mapped).
    /// </summary>
    public ushort? RemoteChannel { get; private set; }

    /// <summary>
    /// Gets the current session state.
    /// </summary>
    public AmqpSessionState State => _state;

    /// <summary>
    /// Gets the remote Begin performative.
    /// </summary>
    public Begin? RemoteBegin => _remoteBegin;

    /// <summary>
    /// Gets the local Begin performative.
    /// </summary>
    public Begin? LocalBegin => _localBegin;

    /// <summary>
    /// Gets the connection this session belongs to.
    /// </summary>
    public AmqpConnection Connection => _connection;

    /// <summary>
    /// Gets the next outgoing transfer ID.
    /// </summary>
    public uint NextOutgoingId => _nextOutgoingId;

    /// <summary>
    /// Gets the next expected incoming transfer ID.
    /// </summary>
    public uint NextIncomingId => _nextIncomingId;

    /// <summary>
    /// Gets the incoming window size.
    /// </summary>
    public uint IncomingWindow => _incomingWindow;

    /// <summary>
    /// Gets the outgoing window size.
    /// </summary>
    public uint OutgoingWindow => _outgoingWindow;

    /// <summary>
    /// Gets the maximum handle number.
    /// </summary>
    public uint HandleMax => _handleMax;

    /// <summary>
    /// Gets all links in this session.
    /// </summary>
    public IEnumerable<AmqpLink> Links => _localLinks.Values;

    /// <summary>
    /// Handles a Begin performative from the remote peer.
    /// </summary>
    internal async Task HandleBeginAsync(Begin begin, CancellationToken cancellationToken)
    {
        if (_state != AmqpSessionState.Unmapped && _state != AmqpSessionState.BeginSent)
        {
            throw new AmqpConnectionException($"Received Begin in invalid session state: {_state}");
        }

        _remoteBegin = begin;
        RemoteChannel = begin.RemoteChannel;
        
        // Store remote flow control parameters
        _nextIncomingId = begin.NextOutgoingId;
        _remoteIncomingWindow = begin.IncomingWindow;
        _remoteOutgoingWindow = begin.OutgoingWindow;
        
        if (begin.HandleMax < _handleMax)
        {
            _handleMax = begin.HandleMax;
        }

        Log.SessionBeginProcessed(_logger, _connection.ConnectionId, LocalChannel, 
            begin.NextOutgoingId, begin.IncomingWindow, begin.OutgoingWindow);

        // Send our Begin response
        _localBegin = new Begin
        {
            RemoteChannel = LocalChannel,
            NextOutgoingId = _nextOutgoingId,
            IncomingWindow = _incomingWindow,
            OutgoingWindow = _outgoingWindow,
            HandleMax = _handleMax
        };

        await _frameWriter.WriteFrameAsync(LocalChannel, _localBegin, cancellationToken)
            .ConfigureAwait(false);

        _state = AmqpSessionState.Mapped;
        Log.SessionMapped(_logger, _connection.ConnectionId, LocalChannel, RemoteChannel);
    }

    /// <summary>
    /// Handles an End performative from the remote peer.
    /// </summary>
    internal async Task HandleEndAsync(End end, CancellationToken cancellationToken)
    {
        Log.SessionEndProcessed(_logger, _connection.ConnectionId, LocalChannel, end.Error?.Condition);

        if (_state == AmqpSessionState.EndSent)
        {
            // We already sent End, just transition to Discarding
            _state = AmqpSessionState.Discarding;
        }
        else
        {
            // Send End response
            _state = AmqpSessionState.EndReceived;
            
            var response = new End();
            await _frameWriter.WriteFrameAsync(LocalChannel, response, cancellationToken)
                .ConfigureAwait(false);
            
            _state = AmqpSessionState.Discarding;
        }

        // Close all links
        foreach (var link in _localLinks.Values)
        {
            link.OnSessionEnded();
        }
        _localLinks.Clear();
        _remoteLinks.Clear();

        Log.SessionEnded(_logger, _connection.ConnectionId, LocalChannel);
    }

    /// <summary>
    /// Handles an Attach performative from the remote peer.
    /// </summary>
    internal async Task HandleAttachAsync(Attach attach, CancellationToken cancellationToken)
    {
        if (!_state.IsOperational())
        {
            throw new AmqpConnectionException($"Received Attach in invalid session state: {_state}");
        }

        // Find or create link
        if (!_remoteLinks.TryGetValue(attach.Handle, out var link))
        {
            // New link from remote
            var localHandle = AllocateHandle();
            link = new AmqpLink(this, attach.Name, localHandle, attach.Handle, attach.Role, _frameWriter, _logger);
            _localLinks.TryAdd(localHandle, link);
            _remoteLinks.TryAdd(attach.Handle, link);
        }

        await link.HandleAttachAsync(attach, cancellationToken).ConfigureAwait(false);

        // Notify broker link handler
        _connection.LinkHandler?.OnLinkAttached(link);
    }

    /// <summary>
    /// Handles a Detach performative from the remote peer.
    /// </summary>
    internal async Task HandleDetachAsync(Detach detach, CancellationToken cancellationToken)
    {
        if (!_remoteLinks.TryGetValue(detach.Handle, out var link))
        {
            Log.LinkNotFound(_logger, _connection.ConnectionId, LocalChannel, detach.Handle);
            return;
        }

        // Notify broker link handler before detaching
        _connection.LinkHandler?.OnLinkDetached(link);

        await link.HandleDetachAsync(detach, cancellationToken).ConfigureAwait(false);

        // Remove link if closed
        if (link.State == LinkState.Detached)
        {
            _localLinks.TryRemove(link.LocalHandle, out _);
            _remoteLinks.TryRemove(link.RemoteHandle, out _);
        }
    }

    /// <summary>
    /// Handles a Flow performative from the remote peer.
    /// </summary>
    internal Task HandleFlowAsync(Flow flow, CancellationToken cancellationToken)
    {
        // Update session-level flow control
        if (flow.NextIncomingId.HasValue)
        {
            _remoteIncomingWindow = flow.NextIncomingId.Value + flow.IncomingWindow - _nextOutgoingId;
        }
        
        _remoteOutgoingWindow = flow.OutgoingWindow;

        // If handle is specified, route to link
        if (flow.Handle.HasValue && _remoteLinks.TryGetValue(flow.Handle.Value, out var link))
        {
            return link.HandleFlowAsync(flow, cancellationToken);
        }

        Log.SessionFlowUpdated(_logger, _connection.ConnectionId, LocalChannel, 
            _remoteIncomingWindow, _remoteOutgoingWindow);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles a Transfer performative from the remote peer.
    /// </summary>
    internal Task HandleTransferAsync(Transfer transfer, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        // Update incoming window
        _nextIncomingId++;
        _incomingWindow--;

        if (!_remoteLinks.TryGetValue(transfer.Handle, out var link))
        {
            Log.LinkNotFound(_logger, _connection.ConnectionId, LocalChannel, transfer.Handle);
            return Task.CompletedTask;
        }

        return link.HandleTransferAsync(transfer, payload, cancellationToken);
    }

    /// <summary>
    /// Handles a Disposition performative from the remote peer.
    /// </summary>
    internal Task HandleDispositionAsync(Disposition disposition, CancellationToken cancellationToken)
    {
        // Route to all affected links
        foreach (var link in _localLinks.Values)
        {
            if (link.Role == disposition.Role)
            {
                link.HandleDisposition(disposition);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Ends the session gracefully.
    /// </summary>
    public async Task EndAsync(string? errorCondition = null, string? errorDescription = null, CancellationToken cancellationToken = default)
    {
        if (_state.IsTerminal() || _state == AmqpSessionState.EndSent || _state == AmqpSessionState.EndReceived)
        {
            return;
        }

        _state = AmqpSessionState.EndSent;

        var end = new End
        {
            Error = errorCondition != null ? new AmqpError
            {
                Condition = errorCondition,
                Description = errorDescription
            } : null
        };

        await _frameWriter.WriteFrameAsync(LocalChannel, end, cancellationToken).ConfigureAwait(false);
        Log.SessionEndSent(_logger, _connection.ConnectionId, LocalChannel);
    }

    /// <summary>
    /// Gets a link by local handle.
    /// </summary>
    public AmqpLink? GetLink(uint handle)
    {
        _localLinks.TryGetValue(handle, out var link);
        return link;
    }

    private uint AllocateHandle()
    {
        for (uint i = 0; i <= _handleMax; i++)
        {
            if (!_localLinks.ContainsKey(i))
            {
                return i;
            }
        }
        throw new AmqpConnectionException("No available handles");
    }

    /// <summary>
    /// Sends a flow frame to update the remote peer on our window.
    /// </summary>
    internal async Task SendFlowAsync(CancellationToken cancellationToken)
    {
        var flow = new Flow
        {
            NextIncomingId = _nextIncomingId,
            IncomingWindow = _incomingWindow,
            NextOutgoingId = _nextOutgoingId,
            OutgoingWindow = _outgoingWindow
        };

        await _frameWriter.WriteFrameAsync(LocalChannel, flow, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a Transfer frame with payload to deliver a message to the client.
    /// </summary>
    /// <param name="handle">The link handle.</param>
    /// <param name="deliveryId">The delivery ID.</param>
    /// <param name="deliveryTag">The delivery tag.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="messageId">The message ID for tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SendTransferAsync(
        uint handle,
        uint deliveryId,
        byte[] deliveryTag,
        ReadOnlyMemory<byte> payload,
        long messageId,
        CancellationToken cancellationToken)
    {
        var transfer = new Transfer
        {
            Handle = handle,
            DeliveryId = deliveryId,
            DeliveryTag = deliveryTag,
            MessageFormat = 0,
            Settled = false,
            More = false
        };

        await _frameWriter.WriteFrameAsync(LocalChannel, transfer, payload, cancellationToken)
            .ConfigureAwait(false);

        _nextOutgoingId++;
        if (_outgoingWindow > 0)
            _outgoingWindow--;

        Log.TransferSent(_logger, _connection.ConnectionId, LocalChannel, handle, deliveryId, payload.Length);
    }
}
