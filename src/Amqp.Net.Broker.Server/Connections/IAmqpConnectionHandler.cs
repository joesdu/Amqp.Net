// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Broker.Server.Connections;

/// <summary>
/// Handles AMQP connections after they have been accepted and the protocol header validated.
/// </summary>
public interface IAmqpConnectionHandler
{
    /// <summary>
    /// Called when a new connection is established and ready for AMQP processing.
    /// </summary>
    /// <param name="context">The connection context containing transport and state.</param>
    /// <param name="cancellationToken">Cancellation token for the connection lifetime.</param>
    /// <returns>A task that completes when the connection is closed.</returns>
    Task HandleConnectionAsync(AmqpConnectionContext context, CancellationToken cancellationToken);
}
