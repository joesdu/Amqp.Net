// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Amqp.Net.Broker.Host;

/// <summary>
/// Top-level configuration options for the AMQP broker.
/// </summary>
#pragma warning disable CA1812 // Internal class is instantiated by DI container
internal sealed class BrokerOptions
#pragma warning restore CA1812
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "AmqpBroker";

    /// <summary>
    /// The broker name/identifier.
    /// </summary>
    public string Name { get; set; } = "amqp-broker";

    /// <summary>
    /// The host to bind to. Default is "0.0.0.0".
    /// </summary>
    public string Host { get; set; } = "0.0.0.0";

    /// <summary>
    /// The port to listen on. Default is 5672.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Maximum number of concurrent connections. Default is 10000.
    /// </summary>
    public int MaxConnections { get; set; } = 10000;

    /// <summary>
    /// Maximum frame size in bytes. Default is 256 KB.
    /// </summary>
    public uint MaxFrameSize { get; set; } = 256 * 1024;

    /// <summary>
    /// Idle timeout in milliseconds. Default is 60 seconds.
    /// </summary>
    public uint IdleTimeoutMs { get; set; } = 60000;

    /// <summary>
    /// Whether to enable the management API. Default is true.
    /// </summary>
    public bool EnableManagementApi { get; set; } = true;

    /// <summary>
    /// The port for the management API. Default is 15672.
    /// </summary>
    public int ManagementPort { get; set; } = 15672;

    /// <summary>
    /// Gets the listen endpoint.
    /// </summary>
    public IPEndPoint GetListenEndpoint()
    {
        var address = Host == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(Host);
        return new IPEndPoint(address, Port);
    }
}
