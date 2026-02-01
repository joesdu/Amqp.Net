// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Cluster.Configuration;
using Amqp.Net.Broker.Cluster.Raft;
using DotNext.Net.Cluster.Consensus.Raft;
using DotNext.Net.Cluster.Consensus.Raft.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Amqp.Net.Broker.Cluster;

/// <summary>
/// Extension methods for configuring the broker cluster.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds broker clustering services using Raft consensus.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBrokerCluster(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Bind cluster options
        services.Configure<BrokerClusterOptions>(configuration.GetSection(BrokerClusterOptions.SectionName));

        // Add cluster configurator
        services.AddSingleton<IClusterMemberLifetime, BrokerClusterConfigurator>();

        // Configure cluster membership storage
        var clusterSection = configuration.GetSection(BrokerClusterOptions.SectionName);
        var seedNodes = clusterSection.GetSection("SeedNodes").Get<string[]>() ?? [];
        services.UseInMemoryConfigurationStorage(members =>
        {
            foreach (var seedNode in seedNodes)
            {
                if (Uri.TryCreate(seedNode, UriKind.Absolute, out var uri))
                {
                    members.Add(new(uri));
                }
            }
        });

        // Add broker state machine
        var dataPath = clusterSection["DataPath"] ?? "./data/raft";
        services.AddSingleton(sp =>
        {
            var logger = sp.GetService<ILogger<BrokerStateMachine>>();
            return new BrokerStateMachine(dataPath, logger);
        });

        // Add cluster configuration
        services.ConfigureCluster<BrokerClusterConfigurator>();
        return services;
    }

    /// <summary>
    /// Configures the host to join the Raft cluster.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The host builder for chaining.</returns>
    public static IHostBuilder UseBrokerCluster(this IHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.JoinCluster();
        return builder;
    }

    /// <summary>
    /// Configures the web application builder to join the Raft cluster.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for chaining.</returns>
    public static WebApplicationBuilder UseBrokerCluster(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.JoinCluster();
        return builder;
    }
}
