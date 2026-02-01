// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Amqp.Net.Broker.Server.Connections;
using Amqp.Net.Broker.Server.Exceptions;
using Amqp.Net.Broker.Server.Logging;
using Amqp.Net.Broker.Server.Sessions;
using Amqp.Net.Broker.Server.Transport;
using Amqp.Net.Protocol.Performatives;
using Microsoft.Extensions.Logging;

namespace Amqp.Net.Broker.Server.Links;

/// <summary>
/// Represents an AMQP link within a session.
/// Can be either a sender or receiver link.
/// </summary>
public sealed class AmqpLink
{
    private readonly AmqpSession _session;
    private readonly FrameWriter _frameWriter;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<uint, AmqpDelivery> _unsettledDeliveries = new();

    private LinkState _state = LinkState.Detached;
    private Attach? _remoteAttach;
    private Attach? _localAttach;
    
    // Flow control
    private uint _deliveryCount;
    private uint _linkCredit;
    private bool _drain;

    /// <summary>
    /// Creates a new AMQP link.
    /// </summary>
    internal AmqpLink(
        AmqpSession session,
        string name,
        uint localHandle,
        uint remoteHandle,
        bool remoteRole,
        FrameWriter frameWriter,
        ILogger logger)
    {
        _session = session;
        Name = name;
        LocalHandle = localHandle;
        RemoteHandle = remoteHandle;
        // Our role is opposite of remote's role
        Role = !remoteRole;
        _frameWriter = frameWriter;
        _logger = logger;
    }

    /// <summary>
    /// Gets the link name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the local handle.
    /// </summary>
    public uint LocalHandle { get; }

    /// <summary>
    /// Gets the remote handle.
    /// </summary>
    public uint RemoteHandle { get; }

    /// <summary>
    /// Gets the role: false = sender, true = receiver.
    /// </summary>
    public bool Role { get; }

    /// <summary>
    /// Returns true if this is a sender link.
    /// </summary>
    public bool IsSender => !Role;

    /// <summary>
    /// Returns true if this is a receiver link.
    /// </summary>
    public bool IsReceiver => Role;

    /// <summary>
    /// Gets the current link state.
    /// </summary>
    public LinkState State => _state;

    /// <summary>
    /// Gets the session this link belongs to.
    /// </summary>
    public AmqpSession Session => _session;

    /// <summary>
    /// Gets the remote Attach performative.
    /// </summary>
    public Attach? RemoteAttach => _remoteAttach;

    /// <summary>
    /// Gets the local Attach performative.
    /// </summary>
    public Attach? LocalAttach => _localAttach;

    /// <summary>
    /// Gets the source address.
    /// </summary>
    public string? SourceAddress => _remoteAttach?.Source?.Address ?? _localAttach?.Source?.Address;

    /// <summary>
    /// Gets the target address.
    /// </summary>
    public string? TargetAddress => _remoteAttach?.Target?.Address ?? _localAttach?.Target?.Address;

    /// <summary>
    /// Gets the current delivery count.
    /// </summary>
    public uint DeliveryCount => _deliveryCount;

    /// <summary>
    /// Gets the current link credit.
    /// </summary>
    public uint LinkCredit => _linkCredit;

    /// <summary>
    /// Callback invoked when a message is received on this link.
    /// </summary>
    public Func<AmqpLink, AmqpDelivery, CancellationToken, Task>? OnMessageReceived { get; set; }

    /// <summary>
    /// Handles an Attach performative from the remote peer.
    /// </summary>
    internal async Task HandleAttachAsync(Attach attach, CancellationToken cancellationToken)
    {
        _remoteAttach = attach;

        Log.LinkAttachProcessed(_logger, _session.Connection.ConnectionId, _session.LocalChannel, 
            Name, LocalHandle, attach.Role ? "receiver" : "sender");

        // Create our Attach response
        _localAttach = new Attach
        {
            Name = Name,
            Handle = LocalHandle,
            Role = Role,
            SndSettleMode = attach.SndSettleMode,
            RcvSettleMode = attach.RcvSettleMode,
            Source = attach.Source,
            Target = attach.Target,
            InitialDeliveryCount = IsSender ? 0u : null
        };

        await _frameWriter.WriteFrameAsync(_session.LocalChannel, _localAttach, cancellationToken)
            .ConfigureAwait(false);

        _state = LinkState.Attached;
        Log.LinkAttached(_logger, _session.Connection.ConnectionId, _session.LocalChannel, Name, LocalHandle);

        // If we're a receiver, grant initial credit
        if (IsReceiver)
        {
            await GrantCreditAsync(100, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles a Detach performative from the remote peer.
    /// </summary>
    internal async Task HandleDetachAsync(Detach detach, CancellationToken cancellationToken)
    {
        Log.LinkDetachProcessed(_logger, _session.Connection.ConnectionId, _session.LocalChannel, 
            Name, LocalHandle, detach.Closed, detach.Error?.Condition);

        if (_state == LinkState.DetachSent)
        {
            // We already sent Detach
            _state = LinkState.Detached;
        }
        else
        {
            // Send Detach response
            _state = LinkState.DetachReceived;

            var response = new Detach
            {
                Handle = LocalHandle,
                Closed = detach.Closed
            };

            await _frameWriter.WriteFrameAsync(_session.LocalChannel, response, cancellationToken)
                .ConfigureAwait(false);

            _state = LinkState.Detached;
        }

        Log.LinkDetached(_logger, _session.Connection.ConnectionId, _session.LocalChannel, Name, LocalHandle);
    }

    /// <summary>
    /// Handles a Flow performative for this link.
    /// </summary>
    internal Task HandleFlowAsync(Flow flow, CancellationToken cancellationToken)
    {
        if (flow.DeliveryCount.HasValue)
        {
            _deliveryCount = flow.DeliveryCount.Value;
        }

        if (flow.LinkCredit.HasValue)
        {
            _linkCredit = flow.LinkCredit.Value;
        }

        _drain = flow.Drain;

        Log.LinkFlowUpdated(_logger, _session.Connection.ConnectionId, _session.LocalChannel, 
            Name, _deliveryCount, _linkCredit, _drain);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles a Transfer performative for this link.
    /// </summary>
    internal async Task HandleTransferAsync(Transfer transfer, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        if (!IsReceiver)
        {
            Log.UnexpectedTransferOnSender(_logger, _session.Connection.ConnectionId, _session.LocalChannel, Name);
            return;
        }

        var settled = transfer.Settled ?? false;
        var delivery = new AmqpDelivery
        {
            DeliveryId = transfer.DeliveryId ?? 0,
            DeliveryTag = transfer.DeliveryTag,
            MessageFormat = transfer.MessageFormat ?? 0,
            Settled = settled,
            More = transfer.More,
            Payload = payload
        };

        if (!settled)
        {
            _unsettledDeliveries.TryAdd(delivery.DeliveryId, delivery);
        }

        _linkCredit--;
        _deliveryCount++;

        Log.TransferProcessed(_logger, _session.Connection.ConnectionId, _session.LocalChannel, 
            Name, delivery.DeliveryId, payload.Length);

        // Notify handlers
        if (OnMessageReceived != null)
        {
            await OnMessageReceived(this, delivery, cancellationToken).ConfigureAwait(false);
        }

        // Auto-replenish credit if running low
        if (_linkCredit < 50)
        {
            await GrantCreditAsync(100, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles a Disposition performative affecting this link's deliveries.
    /// </summary>
    internal void HandleDisposition(Disposition disposition)
    {
        for (uint id = disposition.First; id <= (disposition.Last ?? disposition.First); id++)
        {
            if (_unsettledDeliveries.TryRemove(id, out var delivery))
            {
                delivery.Settled = true;
                delivery.DeliveryState = disposition.State;

                Log.DeliverySettled(_logger, _session.Connection.ConnectionId, _session.LocalChannel, 
                    Name, id, disposition.State?.GetType().Name ?? "null");
            }
        }
    }

    /// <summary>
    /// Grants credit to the sender.
    /// </summary>
    public async Task GrantCreditAsync(uint credit, CancellationToken cancellationToken = default)
    {
        if (!IsReceiver)
        {
            throw new InvalidOperationException("Only receiver links can grant credit");
        }

        _linkCredit += credit;

        var flow = new Flow
        {
            NextIncomingId = _session.NextIncomingId,
            IncomingWindow = _session.IncomingWindow,
            NextOutgoingId = _session.NextOutgoingId,
            OutgoingWindow = _session.OutgoingWindow,
            Handle = LocalHandle,
            DeliveryCount = _deliveryCount,
            LinkCredit = _linkCredit
        };

        await _frameWriter.WriteFrameAsync(_session.LocalChannel, flow, cancellationToken)
            .ConfigureAwait(false);

        Log.CreditGranted(_logger, _session.Connection.ConnectionId, _session.LocalChannel, Name, credit, _linkCredit);
    }

    /// <summary>
    /// Settles a delivery.
    /// </summary>
    public async Task SettleAsync(uint deliveryId, DeliveryState? state, CancellationToken cancellationToken = default)
    {
        var disposition = new Disposition
        {
            Role = Role,
            First = deliveryId,
            Last = deliveryId,
            Settled = true,
            State = state
        };

        await _frameWriter.WriteFrameAsync(_session.LocalChannel, disposition, cancellationToken)
            .ConfigureAwait(false);

        _unsettledDeliveries.TryRemove(deliveryId, out _);
    }

    /// <summary>
    /// Detaches the link.
    /// </summary>
    public async Task DetachAsync(bool closed = true, string? errorCondition = null, string? errorDescription = null, CancellationToken cancellationToken = default)
    {
        if (_state.IsTerminal() || _state == LinkState.DetachSent || _state == LinkState.DetachReceived)
        {
            return;
        }

        _state = LinkState.DetachSent;

        var detach = new Detach
        {
            Handle = LocalHandle,
            Closed = closed,
            Error = errorCondition != null ? new AmqpError
            {
                Condition = errorCondition,
                Description = errorDescription
            } : null
        };

        await _frameWriter.WriteFrameAsync(_session.LocalChannel, detach, cancellationToken)
            .ConfigureAwait(false);

        Log.LinkDetachSent(_logger, _session.Connection.ConnectionId, _session.LocalChannel, Name, LocalHandle);
    }

    /// <summary>
    /// Called when the session ends.
    /// </summary>
    internal void OnSessionEnded()
    {
        _state = LinkState.Detached;
        _unsettledDeliveries.Clear();
    }
}
