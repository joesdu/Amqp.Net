// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Server.Configuration;
using Amqp.Net.Broker.Server.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amqp.Net.Broker.Server.Connections;

/// <summary>
/// Default implementation of IAmqpConnectionHandler that creates and runs AmqpConnection instances.
/// </summary>
public sealed class AmqpConnectionHandler : IAmqpConnectionHandler
{
    private readonly AmqpServerOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IBrokerLinkHandler _linkHandler;

    /// <summary>
    /// Creates a new connection handler.
    /// </summary>
    public AmqpConnectionHandler(
        IOptions<AmqpServerOptions> options,
        ILoggerFactory loggerFactory,
        IBrokerLinkHandler linkHandler)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(linkHandler);

        _options = options.Value;
        _loggerFactory = loggerFactory;
        _linkHandler = linkHandler;
    }

    /// <inheritdoc />
    public async Task HandleConnectionAsync(AmqpConnectionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var connectionLogger = _loggerFactory.CreateLogger<AmqpConnection>();
        var connection = new AmqpConnection(context, _options, connectionLogger, _linkHandler);

        await using (connection.ConfigureAwait(false))
        {
            await connection.RunAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
