// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Broker.Server.Connections;

/// <summary>
/// AMQP connection state as per AMQP 1.0 specification.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Initial state - connection not yet started.
    /// </summary>
    Start,

    /// <summary>
    /// Protocol header received, waiting for Open.
    /// </summary>
    HeaderReceived,

    /// <summary>
    /// Protocol header sent, waiting for Open.
    /// </summary>
    HeaderSent,

    /// <summary>
    /// Protocol header exchanged, waiting for Open.
    /// </summary>
    HeaderExchanged,

    /// <summary>
    /// Open sent, waiting for remote Open.
    /// </summary>
    OpenPipe,

    /// <summary>
    /// Open received, waiting to send Open.
    /// </summary>
    OpenReceived,

    /// <summary>
    /// Open sent, waiting for remote Open.
    /// </summary>
    OpenSent,

    /// <summary>
    /// Connection is open and operational.
    /// </summary>
    Opened,

    /// <summary>
    /// Close sent, waiting for remote Close.
    /// </summary>
    ClosePipe,

    /// <summary>
    /// Close received, waiting to send Close.
    /// </summary>
    CloseReceived,

    /// <summary>
    /// Connection is closed.
    /// </summary>
    End
}

/// <summary>
/// Extension methods for connection state transitions.
/// </summary>
public static class ConnectionStateExtensions
{
    /// <summary>
    /// Returns true if the connection is in a state where it can send frames.
    /// </summary>
    public static bool CanSend(this ConnectionState state)
    {
        return state is ConnectionState.Opened or ConnectionState.CloseReceived;
    }

    /// <summary>
    /// Returns true if the connection is in a state where it can receive frames.
    /// </summary>
    public static bool CanReceive(this ConnectionState state)
    {
        return state is ConnectionState.Opened or ConnectionState.ClosePipe;
    }

    /// <summary>
    /// Returns true if the connection is in a terminal state.
    /// </summary>
    public static bool IsTerminal(this ConnectionState state)
    {
        return state == ConnectionState.End;
    }

    /// <summary>
    /// Returns true if the connection is in the process of opening.
    /// </summary>
    public static bool IsOpening(this ConnectionState state)
    {
        return state is ConnectionState.Start 
            or ConnectionState.HeaderReceived 
            or ConnectionState.HeaderSent 
            or ConnectionState.HeaderExchanged
            or ConnectionState.OpenPipe 
            or ConnectionState.OpenReceived 
            or ConnectionState.OpenSent;
    }

    /// <summary>
    /// Returns true if the connection is in the process of closing.
    /// </summary>
    public static bool IsClosing(this ConnectionState state)
    {
        return state is ConnectionState.ClosePipe or ConnectionState.CloseReceived;
    }
}
