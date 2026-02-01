// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Server.Transport;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amqp.Net.Broker.Host;

/// <summary>
/// Hosted service that manages the AMQP broker lifecycle.
/// </summary>
#pragma warning disable CA1812 // Internal class is instantiated by DI container
internal sealed class BrokerHostedService : IHostedService
#pragma warning restore CA1812
{
    private readonly AmqpListener _listener;
    private readonly BrokerOptions _options;
    private readonly ILogger<BrokerHostedService> _logger;

    /// <summary>
    /// Creates a new broker hosted service.
    /// </summary>
    public BrokerHostedService(
        AmqpListener listener,
        IOptions<BrokerOptions> options,
        ILogger<BrokerHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _listener = listener;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Log.BrokerStarting(_logger, _options.Name, _options.Host, _options.Port);

        try
        {
            await _listener.StartAsync(cancellationToken).ConfigureAwait(false);
            Log.BrokerStarted(_logger, _options.Name, _options.Host, _options.Port);
        }
        catch (Exception ex)
        {
            Log.BrokerStartFailed(_logger, ex, _options.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.BrokerStopping(_logger, _options.Name);

        try
        {
            await _listener.StopAsync(cancellationToken).ConfigureAwait(false);
            Log.BrokerStopped(_logger, _options.Name);
        }
        catch (Exception ex)
        {
            Log.BrokerStopFailed(_logger, ex, _options.Name);
            throw;
        }
    }
}

/// <summary>
/// High-performance logging for broker host.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Starting AMQP broker '{Name}' on {Host}:{Port}...")]
    public static partial void BrokerStarting(ILogger logger, string name, string host, int port);

    [LoggerMessage(Level = LogLevel.Information, Message = "AMQP broker '{Name}' started on {Host}:{Port}")]
    public static partial void BrokerStarted(ILogger logger, string name, string host, int port);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to start AMQP broker '{Name}'")]
    public static partial void BrokerStartFailed(ILogger logger, Exception exception, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping AMQP broker '{Name}'...")]
    public static partial void BrokerStopping(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "AMQP broker '{Name}' stopped")]
    public static partial void BrokerStopped(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error stopping AMQP broker '{Name}'")]
    public static partial void BrokerStopFailed(ILogger logger, Exception exception, string name);
}
