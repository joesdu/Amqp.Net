// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Amqp.Net.Broker.Server.Connections;
using Microsoft.Extensions.Logging;

namespace Amqp.Net.Broker.Server.Logging;

/// <summary>
/// High-performance logging for AMQP server components.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "AMQP listener started on {Endpoint}")]
    public static partial void ListenerStarted(ILogger logger, EndPoint? endpoint);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to bind to {Endpoint}")]
    public static partial void BindFailed(ILogger logger, Exception exception, EndPoint? endpoint);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping AMQP listener...")]
    public static partial void ListenerStopping(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "AMQP listener stopped")]
    public static partial void ListenerStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error waiting for accept loop to complete")]
    public static partial void AcceptLoopWaitError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error accepting connection on {Endpoint}")]
    public static partial void AcceptError(ILogger logger, Exception exception, EndPoint? endpoint);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} accepted from {RemoteEndpoint}")]
    public static partial void ConnectionAccepted(ILogger logger, string connectionId, EndPoint? remoteEndpoint);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection {ConnectionId} from {RemoteEndpoint}: Invalid protocol header")]
    public static partial void InvalidProtocolHeader(ILogger logger, string connectionId, EndPoint? remoteEndpoint);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connection {ConnectionId} established from {RemoteEndpoint}, Protocol: {Protocol}")]
    public static partial void ConnectionEstablished(ILogger logger, string connectionId, EndPoint? remoteEndpoint, string protocol);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} cancelled")]
    public static partial void ConnectionCancelled(ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error handling connection {ConnectionId}")]
    public static partial void ConnectionError(ILogger logger, Exception exception, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} closed")]
    public static partial void ConnectionClosed(ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "TLS protocol requested but not supported")]
    public static partial void TlsNotSupported(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unsupported AMQP version")]
    public static partial void UnsupportedVersion(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid protocol header: {Header}")]
    public static partial void InvalidHeader(ILogger logger, string header);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error closing connection {ConnectionId}")]
    public static partial void CloseConnectionError(ILogger logger, Exception exception, string connectionId);

    // Connection state machine logging
    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} state changed to {State}")]
    public static partial void ConnectionStateChanged(ILogger logger, string connectionId, ConnectionState state);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} closed by remote")]
    public static partial void ConnectionClosedByRemote(ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection {ConnectionId} protocol error: {ErrorMessage}")]
    public static partial void ConnectionProtocolError(ILogger logger, Exception exception, string connectionId, string errorMessage);

    [LoggerMessage(Level = LogLevel.Error, Message = "Connection {ConnectionId} unexpected error")]
    public static partial void ConnectionUnexpectedError(ILogger logger, Exception exception, string connectionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} ended")]
    public static partial void ConnectionEnded(ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Connection {ConnectionId} heartbeat received")]
    public static partial void HeartbeatReceived(ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Connection {ConnectionId} heartbeat sent")]
    public static partial void HeartbeatSent(ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection {ConnectionId} heartbeat error")]
    public static partial void HeartbeatError(ILogger logger, Exception exception, string connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection {ConnectionId} idle timeout expired after {TimeoutMs}ms")]
    public static partial void IdleTimeoutExpired(ILogger logger, string connectionId, uint timeoutMs);

    // Open/Close performatives
    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} received Open from {ContainerId}, MaxFrameSize={MaxFrameSize}, ChannelMax={ChannelMax}")]
    public static partial void OpenReceived(ILogger logger, string connectionId, string containerId, uint maxFrameSize, ushort channelMax);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} sent Open as {ContainerId}")]
    public static partial void OpenSent(ILogger logger, string connectionId, string containerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connection {ConnectionId} opened: MaxFrameSize={MaxFrameSize}, ChannelMax={ChannelMax}, IdleTimeout={IdleTimeout}ms")]
    public static partial void ConnectionOpened(ILogger logger, string connectionId, uint maxFrameSize, ushort channelMax, uint idleTimeout);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} received Close, Error={ErrorCondition}")]
    public static partial void CloseReceived(ILogger logger, string connectionId, string? errorCondition);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} sent Close")]
    public static partial void CloseSent(ILogger logger, string connectionId);

    // Session performatives
    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} channel {Channel} received Begin, NextOutgoingId={NextOutgoingId}")]
    public static partial void SessionBeginReceived(ILogger logger, string connectionId, ushort channel, uint nextOutgoingId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} channel {Channel} received End")]
    public static partial void SessionEndReceived(ILogger logger, string connectionId, ushort channel);

    // Link performatives
    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} channel {Channel} received Attach, Name={Name}, Role={Role}")]
    public static partial void LinkAttachReceived(ILogger logger, string connectionId, ushort channel, string name, bool role);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} channel {Channel} received Detach, Handle={Handle}")]
    public static partial void LinkDetachReceived(ILogger logger, string connectionId, ushort channel, uint handle);

    // Flow control
    [LoggerMessage(Level = LogLevel.Trace, Message = "Connection {ConnectionId} channel {Channel} received Flow, NextIncomingId={NextIncomingId}, IncomingWindow={IncomingWindow}")]
    public static partial void FlowReceived(ILogger logger, string connectionId, ushort channel, uint? nextIncomingId, uint incomingWindow);

    // Transfer/Disposition
    [LoggerMessage(Level = LogLevel.Trace, Message = "Connection {ConnectionId} channel {Channel} received Transfer, DeliveryId={DeliveryId}, PayloadSize={PayloadSize}")]
    public static partial void TransferReceived(ILogger logger, string connectionId, ushort channel, uint? deliveryId, int payloadSize);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Connection {ConnectionId} channel {Channel} received Disposition, First={First}, Last={Last}")]
    public static partial void DispositionReceived(ILogger logger, string connectionId, ushort channel, uint first, uint? last);

    // Session management
    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} channel {Channel} session Begin processed: NextOutgoingId={NextOutgoingId}, IncomingWindow={IncomingWindow}, OutgoingWindow={OutgoingWindow}")]
    public static partial void SessionBeginProcessed(ILogger logger, string connectionId, ushort channel, uint nextOutgoingId, uint incomingWindow, uint outgoingWindow);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connection {ConnectionId} channel {Channel} session mapped to remote channel {RemoteChannel}")]
    public static partial void SessionMapped(ILogger logger, string connectionId, ushort channel, ushort? remoteChannel);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} channel {Channel} session End processed, Error={ErrorCondition}")]
    public static partial void SessionEndProcessed(ILogger logger, string connectionId, ushort channel, string? errorCondition);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} channel {Channel} session ended")]
    public static partial void SessionEnded(ILogger logger, string connectionId, ushort channel);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} channel {Channel} sent End")]
    public static partial void SessionEndSent(ILogger logger, string connectionId, ushort channel);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Connection {ConnectionId} channel {Channel} session flow updated: RemoteIncomingWindow={RemoteIncomingWindow}, RemoteOutgoingWindow={RemoteOutgoingWindow}")]
    public static partial void SessionFlowUpdated(ILogger logger, string connectionId, ushort channel, uint remoteIncomingWindow, uint remoteOutgoingWindow);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection {ConnectionId} channel {Channel} not found")]
    public static partial void SessionNotFound(ILogger logger, string connectionId, ushort channel);

    // Link management
    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} channel {Channel} link Attach processed: Name={Name}, Handle={Handle}, Role={Role}")]
    public static partial void LinkAttachProcessed(ILogger logger, string connectionId, ushort channel, string name, uint handle, string role);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connection {ConnectionId} channel {Channel} link attached: Name={Name}, Handle={Handle}")]
    public static partial void LinkAttached(ILogger logger, string connectionId, ushort channel, string name, uint handle);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} channel {Channel} link Detach processed: Name={Name}, Handle={Handle}, Closed={Closed}, Error={ErrorCondition}")]
    public static partial void LinkDetachProcessed(ILogger logger, string connectionId, ushort channel, string name, uint handle, bool closed, string? errorCondition);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connection {ConnectionId} channel {Channel} link detached: Name={Name}, Handle={Handle}")]
    public static partial void LinkDetached(ILogger logger, string connectionId, ushort channel, string name, uint handle);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connection {ConnectionId} channel {Channel} sent Detach: Name={Name}, Handle={Handle}")]
    public static partial void LinkDetachSent(ILogger logger, string connectionId, ushort channel, string name, uint handle);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection {ConnectionId} channel {Channel} link not found: Handle={Handle}")]
    public static partial void LinkNotFound(ILogger logger, string connectionId, ushort channel, uint handle);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Connection {ConnectionId} channel {Channel} link flow updated: Name={Name}, DeliveryCount={DeliveryCount}, LinkCredit={LinkCredit}, Drain={Drain}")]
    public static partial void LinkFlowUpdated(ILogger logger, string connectionId, ushort channel, string name, uint deliveryCount, uint linkCredit, bool drain);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection {ConnectionId} channel {Channel} unexpected Transfer on sender link: Name={Name}")]
    public static partial void UnexpectedTransferOnSender(ILogger logger, string connectionId, ushort channel, string name);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Connection {ConnectionId} channel {Channel} link transfer processed: Name={Name}, DeliveryId={DeliveryId}, PayloadSize={PayloadSize}")]
    public static partial void TransferProcessed(ILogger logger, string connectionId, ushort channel, string name, uint deliveryId, int payloadSize);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Connection {ConnectionId} channel {Channel} link delivery settled: Name={Name}, DeliveryId={DeliveryId}, State={State}")]
    public static partial void DeliverySettled(ILogger logger, string connectionId, ushort channel, string name, uint deliveryId, string state);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Connection {ConnectionId} channel {Channel} link credit granted: Name={Name}, Credit={Credit}, TotalCredit={TotalCredit}")]
    public static partial void CreditGranted(ILogger logger, string connectionId, ushort channel, string name, uint credit, uint totalCredit);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Connection {ConnectionId} channel {Channel} sent Transfer: Handle={Handle}, DeliveryId={DeliveryId}, PayloadSize={PayloadSize}")]
    public static partial void TransferSent(ILogger logger, string connectionId, ushort channel, uint handle, uint deliveryId, int payloadSize);
}
