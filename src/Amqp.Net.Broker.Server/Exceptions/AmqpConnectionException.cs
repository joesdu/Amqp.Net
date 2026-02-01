// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Amqp.Net.Broker.Server.Exceptions;

/// <summary>
/// Exception thrown when an AMQP connection error occurs.
/// </summary>
public class AmqpConnectionException : Exception
{
    /// <summary>
    /// Creates a new AMQP connection exception.
    /// </summary>
    public AmqpConnectionException(string message) : base(message) { }

    /// <summary>
    /// Creates a new AMQP connection exception with an inner exception.
    /// </summary>
    public AmqpConnectionException(string message, Exception innerException) : base(message, innerException) { }
}
