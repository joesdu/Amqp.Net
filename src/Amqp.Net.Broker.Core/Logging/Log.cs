// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Exchanges;
using Microsoft.Extensions.Logging;

namespace Amqp.Net.Broker.Core.Logging;

/// <summary>
/// High-performance logging for Broker.Core components.
/// </summary>
internal static partial class Log
{
    // Exchange operations
    [LoggerMessage(Level = LogLevel.Information, Message = "Exchange declared: {Name} ({Type}), Durable={Durable}")]
    public static partial void ExchangeDeclared(ILogger logger, string name, ExchangeType type, bool durable);

    [LoggerMessage(Level = LogLevel.Information, Message = "Exchange deleted: {Name}")]
    public static partial void ExchangeDeleted(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Exchange {Name} binding added: {QueueName} with key '{RoutingKey}'")]
    public static partial void BindingAdded(ILogger logger, string name, string queueName, string routingKey);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Exchange {Name} binding removed: {QueueName}")]
    public static partial void BindingRemoved(ILogger logger, string name, string queueName);

    // Queue operations
    [LoggerMessage(Level = LogLevel.Information, Message = "Queue declared: {Name}, Durable={Durable}, Exclusive={Exclusive}")]
    public static partial void QueueDeclared(ILogger logger, string name, bool durable, bool exclusive);

    [LoggerMessage(Level = LogLevel.Information, Message = "Queue deleted: {Name}")]
    public static partial void QueueDeleted(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Queue {Name} message enqueued: MessageId={MessageId}")]
    public static partial void MessageEnqueued(ILogger logger, string name, long messageId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Queue {Name} message dequeued: MessageId={MessageId}")]
    public static partial void MessageDequeued(ILogger logger, string name, long messageId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Queue {Name} message acknowledged: MessageId={MessageId}")]
    public static partial void MessageAcknowledged(ILogger logger, string name, long messageId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Queue {Name} message rejected: MessageId={MessageId}, Requeue={Requeue}")]
    public static partial void MessageRejected(ILogger logger, string name, long messageId, bool requeue);

    [LoggerMessage(Level = LogLevel.Information, Message = "Queue {Name} purged: {Count} messages removed")]
    public static partial void QueuePurged(ILogger logger, string name, int count);

    // Routing operations
    [LoggerMessage(Level = LogLevel.Trace, Message = "Message routed: Exchange={Exchange}, RoutingKey={RoutingKey}, Queues={QueueCount}")]
    public static partial void MessageRouted(ILogger logger, string exchange, string routingKey, int queueCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Message not routed: Exchange={Exchange}, RoutingKey={RoutingKey} (no matching queues)")]
    public static partial void MessageNotRouted(ILogger logger, string exchange, string routingKey);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exchange not found: {Name}")]
    public static partial void ExchangeNotFound(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Queue not found: {Name}")]
    public static partial void QueueNotFound(ILogger logger, string name);

    // Delivery tracking
    [LoggerMessage(Level = LogLevel.Trace, Message = "Delivery tracked: DeliveryId={DeliveryId}, MessageId={MessageId}, Link={LinkName}")]
    public static partial void DeliveryTracked(ILogger logger, uint deliveryId, long messageId, string linkName);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Delivery settled: DeliveryId={DeliveryId}")]
    public static partial void DeliverySettled(ILogger logger, uint deliveryId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stale deliveries found: {Count} deliveries older than {Threshold}")]
    public static partial void StaleDeliveriesFound(ILogger logger, int count, TimeSpan threshold);

    // Message store operations
    [LoggerMessage(Level = LogLevel.Debug, Message = "Message stored: MessageId={MessageId}, Size={Size} bytes")]
    public static partial void MessageStored(ILogger logger, long messageId, int size);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Message deleted: MessageId={MessageId}")]
    public static partial void MessageDeleted(ILogger logger, long messageId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Expired messages purged: {Count} messages removed")]
    public static partial void ExpiredMessagesPurged(ILogger logger, int count);
}
