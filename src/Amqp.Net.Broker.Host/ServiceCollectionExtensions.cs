// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Delivery;
using Amqp.Net.Broker.Core.Routing;
using Amqp.Net.Broker.Core.Storage;
using Amqp.Net.Broker.Server.Configuration;
using Amqp.Net.Broker.Server.Connections;
using Amqp.Net.Broker.Server.Handlers;
using Amqp.Net.Broker.Server.Transport;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Amqp.Net.Broker.Host;

/// <summary>
/// Extension methods for configuring AMQP broker services.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds AMQP broker services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAmqpBroker(
        this IServiceCollection services,
        Action<BrokerOptions>? configure = null)
    {
        // Configure options
        var optionsBuilder = services.AddOptions<BrokerOptions>();
        if (configure != null)
        {
            optionsBuilder.Configure(configure);
        }

        // Configure server options from broker options
        services.AddOptions<AmqpServerOptions>()
                .Configure<IOptions<BrokerOptions>>((serverOptions, brokerOptions) =>
                {
                    var broker = brokerOptions.Value;
                    serverOptions.ClearListenEndpoints();
                    serverOptions.AddListenEndpoint(broker.GetListenEndpoint());
                    serverOptions.MaxConnections = broker.MaxConnections;
                    serverOptions.MaxFrameSize = broker.MaxFrameSize;
                    serverOptions.IdleTimeoutMs = broker.IdleTimeoutMs;
                    serverOptions.ContainerId = broker.Name;
                });

        // Register core services
        services.TryAddSingleton<IMessageStore, InMemoryMessageStore>();
        services.TryAddSingleton<IMessageRouter, MessageRouter>();
        services.TryAddSingleton<IDeliveryTracker, DeliveryTracker>();

        // Register server services
        services.TryAddSingleton<IBrokerLinkHandler, BrokerLinkHandler>();
        services.TryAddSingleton<IAmqpConnectionHandler, AmqpConnectionHandler>();
        services.TryAddSingleton<AmqpListener>();

        // Register hosted service
        services.AddHostedService<BrokerHostedService>();
        return services;
    }

    /// <summary>
    /// Adds AMQP broker services with configuration from a configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAmqpBroker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.Configure<BrokerOptions>(configuration.GetSection(BrokerOptions.SectionName));
        return services.AddAmqpBroker();
    }
}
