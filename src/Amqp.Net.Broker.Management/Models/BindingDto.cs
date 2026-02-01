// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Routing;

namespace Amqp.Net.Broker.Management.Models;

/// <summary>
/// Response model for a binding.
/// </summary>
public sealed record BindingDto
{
    /// <summary>
    /// The source exchange name.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// The destination queue name.
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// The routing key.
    /// </summary>
    public string RoutingKey { get; init; } = "";

    /// <summary>
    /// Creates a DTO from a binding.
    /// </summary>
    public static BindingDto FromBinding(Binding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);

        return new()
        {
            Source = binding.ExchangeName,
            Destination = binding.QueueName,
            RoutingKey = binding.RoutingKey
        };
    }
}

/// <summary>
/// Request model for creating a binding.
/// </summary>
public sealed record CreateBindingRequest
{
    /// <summary>
    /// The source exchange name.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// The destination queue name.
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// The routing key.
    /// </summary>
    public string RoutingKey { get; init; } = "";
}

/// <summary>
/// Request model for deleting a binding.
/// </summary>
public sealed record DeleteBindingRequest
{
    /// <summary>
    /// The source exchange name.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// The destination queue name.
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// The routing key.
    /// </summary>
    public string RoutingKey { get; init; } = "";
}
