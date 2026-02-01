// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Broker.Core.Exchanges;

/// <summary>
/// Types of exchanges supported by the broker.
/// </summary>
public enum ExchangeType
{
    /// <summary>
    /// Direct exchange - routes to queues with exact routing key match.
    /// </summary>
    Direct,

    /// <summary>
    /// Topic exchange - routes to queues with pattern matching (* and #).
    /// </summary>
    Topic,

    /// <summary>
    /// Fanout exchange - routes to all bound queues regardless of routing key.
    /// </summary>
    Fanout,

    /// <summary>
    /// Headers exchange - routes based on message headers.
    /// </summary>
    Headers
}
