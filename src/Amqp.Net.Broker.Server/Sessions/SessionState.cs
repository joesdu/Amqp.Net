// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Broker.Server.Sessions;

/// <summary>
/// AMQP session state as per AMQP 1.0 specification.
/// </summary>
public enum AmqpSessionState
{
    /// <summary>
    /// Initial state - session not yet started.
    /// </summary>
    Unmapped,

    /// <summary>
    /// Begin sent, waiting for remote Begin.
    /// </summary>
    BeginSent,

    /// <summary>
    /// Begin received, waiting to send Begin.
    /// </summary>
    BeginReceived,

    /// <summary>
    /// Session is mapped and operational.
    /// </summary>
    Mapped,

    /// <summary>
    /// End sent, waiting for remote End.
    /// </summary>
    EndSent,

    /// <summary>
    /// End received, waiting to send End.
    /// </summary>
    EndReceived,

    /// <summary>
    /// Session has ended.
    /// </summary>
    Discarding
}

/// <summary>
/// Extension methods for session state transitions.
/// </summary>
public static class AmqpSessionStateExtensions
{
    /// <summary>
    /// Returns true if the session is in a state where it can send frames.
    /// </summary>
    public static bool CanSend(this AmqpSessionState state) => state is AmqpSessionState.Mapped or AmqpSessionState.EndReceived;

    /// <summary>
    /// Returns true if the session is in a state where it can receive frames.
    /// </summary>
    public static bool CanReceive(this AmqpSessionState state) => state is AmqpSessionState.Mapped or AmqpSessionState.EndSent;

    /// <summary>
    /// Returns true if the session is in a terminal state.
    /// </summary>
    public static bool IsTerminal(this AmqpSessionState state) => state == AmqpSessionState.Discarding;

    /// <summary>
    /// Returns true if the session is operational.
    /// </summary>
    public static bool IsOperational(this AmqpSessionState state) => state == AmqpSessionState.Mapped;
}
