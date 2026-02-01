// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DotNext.Net.Cluster;
using DotNext.Net.Cluster.Consensus.Raft;
using Microsoft.Extensions.Logging;

namespace Amqp.Net.Broker.Cluster.Raft;

/// <summary>
/// Configures the Raft cluster behavior.
/// </summary>
public sealed class BrokerClusterConfigurator : IClusterMemberLifetime
{
    private readonly ILogger<BrokerClusterConfigurator> _logger;

    /// <summary>
    /// Creates a new cluster configurator.
    /// </summary>
    public BrokerClusterConfigurator(ILogger<BrokerClusterConfigurator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void OnStart(IRaftCluster cluster, IDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        cluster.LeaderChanged += OnLeaderChanged;
        _logger.LogInformation("Cluster node started");
    }

    /// <inheritdoc />
    public void OnStop(IRaftCluster cluster)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        cluster.LeaderChanged -= OnLeaderChanged;
        _logger.LogInformation("Cluster node stopped");
    }

    private void OnLeaderChanged(ICluster cluster, IClusterMember? leader)
    {
        if (leader is null)
        {
            _logger.LogWarning("Cluster has no leader");
        }
        else
        {
            _logger.LogInformation("New cluster leader: {LeaderEndpoint}", leader.EndPoint);
        }
    }
}
