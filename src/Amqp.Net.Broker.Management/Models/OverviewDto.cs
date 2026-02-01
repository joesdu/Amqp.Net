// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Broker.Management.Models;

/// <summary>
/// Response model for broker overview.
/// </summary>
public sealed record OverviewDto
{
    /// <summary>
    /// Broker version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Broker node name.
    /// </summary>
    public required string NodeName { get; init; }

    /// <summary>
    /// Broker uptime in milliseconds.
    /// </summary>
    public long UptimeMs { get; init; }

    /// <summary>
    /// Queue statistics.
    /// </summary>
    public required QueueStatistics Queues { get; init; }

    /// <summary>
    /// Exchange statistics.
    /// </summary>
    public required ExchangeStatistics Exchanges { get; init; }

    /// <summary>
    /// Message statistics.
    /// </summary>
    public required MessageStatistics Messages { get; init; }
}

/// <summary>
/// Queue statistics.
/// </summary>
public sealed record QueueStatistics
{
    /// <summary>
    /// Total number of queues.
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// Number of durable queues.
    /// </summary>
    public int Durable { get; init; }

    /// <summary>
    /// Number of transient queues.
    /// </summary>
    public int Transient { get; init; }
}

/// <summary>
/// Exchange statistics.
/// </summary>
public sealed record ExchangeStatistics
{
    /// <summary>
    /// Total number of exchanges.
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// Number of direct exchanges.
    /// </summary>
    public int Direct { get; init; }

    /// <summary>
    /// Number of topic exchanges.
    /// </summary>
    public int Topic { get; init; }

    /// <summary>
    /// Number of fanout exchanges.
    /// </summary>
    public int Fanout { get; init; }

    /// <summary>
    /// Number of headers exchanges.
    /// </summary>
    public int Headers { get; init; }
}

/// <summary>
/// Message statistics.
/// </summary>
public sealed record MessageStatistics
{
    /// <summary>
    /// Total messages across all queues.
    /// </summary>
    public long Total { get; init; }

    /// <summary>
    /// Total size of all messages in bytes.
    /// </summary>
    public long TotalSizeBytes { get; init; }

    /// <summary>
    /// Total number of consumers.
    /// </summary>
    public int TotalConsumers { get; init; }
}
