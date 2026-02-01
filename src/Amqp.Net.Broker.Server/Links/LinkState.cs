// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Broker.Server.Links;

/// <summary>
/// AMQP link state as per AMQP 1.0 specification.
/// </summary>
public enum LinkState
{
    /// <summary>
    /// Initial state - link not yet attached.
    /// </summary>
    Detached,

    /// <summary>
    /// Attach sent, waiting for remote Attach.
    /// </summary>
    AttachSent,

    /// <summary>
    /// Attach received, waiting to send Attach.
    /// </summary>
    AttachReceived,

    /// <summary>
    /// Link is attached and operational.
    /// </summary>
    Attached,

    /// <summary>
    /// Detach sent, waiting for remote Detach.
    /// </summary>
    DetachSent,

    /// <summary>
    /// Detach received, waiting to send Detach.
    /// </summary>
    DetachReceived
}

/// <summary>
/// Extension methods for link state transitions.
/// </summary>
public static class LinkStateExtensions
{
    /// <summary>
    /// Returns true if the link is in a state where it can send transfers.
    /// </summary>
    public static bool CanSend(this LinkState state)
    {
        return state is LinkState.Attached or LinkState.DetachReceived;
    }

    /// <summary>
    /// Returns true if the link is in a state where it can receive transfers.
    /// </summary>
    public static bool CanReceive(this LinkState state)
    {
        return state is LinkState.Attached or LinkState.DetachSent;
    }

    /// <summary>
    /// Returns true if the link is in a terminal state.
    /// </summary>
    public static bool IsTerminal(this LinkState state)
    {
        return state == LinkState.Detached;
    }

    /// <summary>
    /// Returns true if the link is operational.
    /// </summary>
    public static bool IsOperational(this LinkState state)
    {
        return state == LinkState.Attached;
    }
}
