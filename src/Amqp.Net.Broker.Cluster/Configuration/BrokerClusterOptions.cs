// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Broker.Cluster.Configuration;

/// <summary>
/// Configuration options for the broker cluster.
/// </summary>
public sealed class BrokerClusterOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "BrokerCluster";

    /// <summary>
    /// Unique identifier for this node in the cluster.
    /// </summary>
    public required string NodeId { get; set; }

    /// <summary>
    /// The address this node listens on for cluster communication.
    /// </summary>
    public required Uri ListenAddress { get; set; }

    /// <summary>
    /// The public address other nodes use to reach this node.
    /// </summary>
    public Uri? PublicAddress { get; set; }

    /// <summary>
    /// List of seed nodes for cluster discovery.
    /// </summary>
    public IList<Uri> SeedNodes { get; } = new List<Uri>();

    /// <summary>
    /// Path to store persistent Raft log and snapshots.
    /// </summary>
    public string DataPath { get; set; } = "./data/raft";

    /// <summary>
    /// Lower bound of election timeout in milliseconds.
    /// </summary>
    public int ElectionTimeoutLowerMs { get; set; } = 150;

    /// <summary>
    /// Upper bound of election timeout in milliseconds.
    /// </summary>
    public int ElectionTimeoutUpperMs { get; set; } = 300;

    /// <summary>
    /// Heartbeat interval in milliseconds.
    /// </summary>
    public int HeartbeatIntervalMs { get; set; } = 50;

    /// <summary>
    /// Whether to enable cold start (single node can become leader without other nodes).
    /// </summary>
    public bool ColdStart { get; set; }

    /// <summary>
    /// Maximum number of log entries to send in a single append entries request.
    /// </summary>
    public int MaxLogEntriesPerRequest { get; set; } = 100;

    /// <summary>
    /// Gets the effective public address (falls back to listen address).
    /// </summary>
    public Uri EffectivePublicAddress => PublicAddress ?? ListenAddress;
}
