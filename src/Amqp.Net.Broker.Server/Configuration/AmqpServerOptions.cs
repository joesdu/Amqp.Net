// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Amqp.Net.Broker.Server.Configuration;

/// <summary>
/// Configuration options for the AMQP server.
/// </summary>
public sealed class AmqpServerOptions
{
    /// <summary>
    /// Default AMQP port.
    /// </summary>
    public const int DefaultPort = 5672;

    /// <summary>
    /// Default maximum number of concurrent connections.
    /// </summary>
    public const int DefaultMaxConnections = 10000;

    /// <summary>
    /// Default maximum frame size in bytes.
    /// </summary>
    public const uint DefaultMaxFrameSize = 256 * 1024; // 256 KB

    /// <summary>
    /// Default idle timeout in milliseconds.
    /// </summary>
    public const uint DefaultIdleTimeoutMs = 60000; // 60 seconds

    private List<IPEndPoint> _listenEndpoints = [new IPEndPoint(IPAddress.Any, DefaultPort)];
    private string? _containerId;

    /// <summary>
    /// The endpoints to listen on. Defaults to 0.0.0.0:5672.
    /// </summary>
    public IReadOnlyList<IPEndPoint> ListenEndpoints => _listenEndpoints;

    /// <summary>
    /// Adds an endpoint to listen on.
    /// </summary>
    public void AddListenEndpoint(IPEndPoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        _listenEndpoints.Add(endpoint);
    }

    /// <summary>
    /// Sets the listen endpoints, replacing any existing ones.
    /// </summary>
    public void SetListenEndpoints(IEnumerable<IPEndPoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        _listenEndpoints = [.. endpoints];
    }

    /// <summary>
    /// Clears all listen endpoints.
    /// </summary>
    public void ClearListenEndpoints() => _listenEndpoints.Clear();

    /// <summary>
    /// Maximum number of concurrent connections. Default is 10000.
    /// </summary>
    public int MaxConnections { get; set; } = DefaultMaxConnections;

    /// <summary>
    /// Maximum frame size in bytes. Default is 256 KB.
    /// </summary>
    public uint MaxFrameSize { get; set; } = DefaultMaxFrameSize;

    /// <summary>
    /// Idle timeout in milliseconds. Connections with no activity for this duration will be closed.
    /// Default is 60 seconds. Set to 0 to disable.
    /// </summary>
    public uint IdleTimeoutMs { get; set; } = DefaultIdleTimeoutMs;

    /// <summary>
    /// TCP backlog size for pending connections. Default is 512.
    /// </summary>
    public int Backlog { get; set; } = 512;

    /// <summary>
    /// Size of the input pipe buffer in bytes. Default is 64 KB.
    /// </summary>
    public int InputBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Size of the output pipe buffer in bytes. Default is 64 KB.
    /// </summary>
    public int OutputBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Container ID for this broker. If not set, a unique ID will be generated on first access.
    /// </summary>
    public string ContainerId
    {
        get => _containerId ??= $"amqp-broker-{Guid.NewGuid():N}";
        set => _containerId = value;
    }
}
