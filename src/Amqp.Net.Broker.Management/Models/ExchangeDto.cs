// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Exchanges;

namespace Amqp.Net.Broker.Management.Models;

/// <summary>
/// Response model for an exchange.
/// </summary>
public sealed record ExchangeDto
{
    /// <summary>
    /// The exchange name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The exchange type (DIRECT, TOPIC, FANOUT, HEADERS).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Whether the exchange is durable.
    /// </summary>
    public bool Durable { get; init; }

    /// <summary>
    /// Whether the exchange is auto-delete.
    /// </summary>
    public bool AutoDelete { get; init; }

    /// <summary>
    /// Number of bindings on this exchange.
    /// </summary>
    public int BindingCount { get; init; }

    /// <summary>
    /// Creates a DTO from an exchange.
    /// </summary>
    public static ExchangeDto FromExchange(IExchange exchange)
    {
        ArgumentNullException.ThrowIfNull(exchange);
        return new()
        {
            Name = exchange.Name,
            Type = exchange.Type.ToString().ToUpperInvariant(),
            Durable = exchange.Durable,
            AutoDelete = exchange.AutoDelete,
            BindingCount = exchange.Bindings.Count
        };
    }
}

/// <summary>
/// Request model for creating an exchange.
/// </summary>
public sealed record CreateExchangeRequest
{
    /// <summary>
    /// The exchange name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The exchange type (direct, topic, fanout, headers).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Whether the exchange is durable.
    /// </summary>
    public bool Durable { get; init; }

    /// <summary>
    /// Whether the exchange is auto-delete.
    /// </summary>
    public bool AutoDelete { get; init; }
}
