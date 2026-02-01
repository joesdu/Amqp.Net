// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Broker.Core.Routing;

/// <summary>
/// Represents a binding between an exchange and a queue.
/// </summary>
public sealed record Binding
{
    /// <summary>
    /// The name of the bound queue.
    /// </summary>
    public required string QueueName { get; init; }

    /// <summary>
    /// The name of the exchange.
    /// </summary>
    public required string ExchangeName { get; init; }

    /// <summary>
    /// The routing key for this binding.
    /// </summary>
    public string RoutingKey { get; init; } = "";

    /// <summary>
    /// Optional arguments for the binding.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Arguments { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"Binding({ExchangeName} -> {QueueName}, key={RoutingKey})";
}
