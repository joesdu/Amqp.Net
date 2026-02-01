// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Amqp.Net.Broker.Core.Messages;
using Amqp.Net.Broker.Core.Queues;
using Amqp.Net.Broker.Core.Routing;
using Amqp.Net.Broker.Server.Links;
using Amqp.Net.Protocol.Messaging;
using Amqp.Net.Protocol.Performatives;
using Microsoft.Extensions.Logging;

namespace Amqp.Net.Broker.Server.Handlers;

/// <summary>
/// Handles the integration between AMQP links and the broker's message routing system.
/// </summary>
public sealed class BrokerLinkHandler : IBrokerLinkHandler
{
    private readonly IMessageRouter _router;
    private readonly ILogger<BrokerLinkHandler> _logger;
    private readonly ConcurrentDictionary<string, ConsumerState> _consumers = new();
    private long _nextDeliveryId;

    /// <summary>
    /// Creates a new broker link handler.
    /// </summary>
    public BrokerLinkHandler(IMessageRouter router, ILogger<BrokerLinkHandler> logger)
    {
        _router = router;
        _logger = logger;
    }

    /// <inheritdoc />
    public void OnLinkAttached(AmqpLink link)
    {
        if (link.IsReceiver)
        {
            // We are receiver = client is sender = client publishes messages
            // Set up handler to route incoming messages
            link.OnMessageReceived = HandleIncomingMessageAsync;
            _logger.LogDebug("Receiver link {LinkName} attached, ready to receive messages to {Target}",
                link.Name, link.TargetAddress);
        }
        else
        {
            // We are sender = client is receiver = client consumes messages
            // Start consuming from the source queue
            var sourceAddress = link.SourceAddress;
            if (!string.IsNullOrEmpty(sourceAddress))
            {
                StartConsumer(link, sourceAddress);
            }
        }
    }

    /// <inheritdoc />
    public void OnLinkDetached(AmqpLink link)
    {
        if (link.IsSender)
        {
            StopConsumer(link);
        }
    }

    /// <summary>
    /// Handles incoming messages from a receiver link (client publishing).
    /// </summary>
    private async Task HandleIncomingMessageAsync(AmqpLink link, AmqpDelivery delivery, CancellationToken cancellationToken)
    {
        try
        {
            // Parse the AMQP message from payload
            var message = AmqpMessage.Decode(delivery.Payload.Span);
            
            // Determine routing key and exchange
            var targetAddress = link.TargetAddress ?? "";
            var (exchangeName, routingKey) = ParseAddress(targetAddress);
            
            // Extract message properties
            var properties = new MessageProperties
            {
                MessageId = message.Properties?.MessageId?.ToString(),
                CorrelationId = message.Properties?.CorrelationId?.ToString(),
                ContentType = message.Properties?.ContentType,
                ReplyTo = message.Properties?.ReplyTo
            };
            
            // Get message body
            var body = message.GetBodyAsBinary() ?? ReadOnlyMemory<byte>.Empty;
            
            // Route the message
            var routedCount = await _router.RouteAsync(
                exchangeName,
                routingKey,
                body,
                properties,
                cancellationToken).ConfigureAwait(false);
            
            _logger.LogDebug("Routed message from link {LinkName} to {Count} queue(s) via exchange '{Exchange}' with key '{RoutingKey}'",
                link.Name, routedCount, exchangeName, routingKey);
            
            // Settle the delivery if not pre-settled
            if (!delivery.Settled)
            {
                await link.SettleAsync(delivery.DeliveryId, Accepted.Instance, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message on link {LinkName}", link.Name);
            
            // Reject the delivery
            if (!delivery.Settled)
            {
                var rejected = new Rejected
                {
                    Error = new AmqpError
                    {
                        Condition = "amqp:internal-error",
                        Description = ex.Message
                    }
                };
                await link.SettleAsync(delivery.DeliveryId, rejected, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Starts consuming messages from a queue and sending them to the client via the sender link.
    /// </summary>
    private void StartConsumer(AmqpLink link, string queueName)
    {
        var cts = new CancellationTokenSource();
        var state = new ConsumerState(link, queueName, cts);
        
        if (_consumers.TryAdd(link.Name, state))
        {
            // Start the consumer loop
            _ = ConsumeLoopAsync(state);
            _logger.LogDebug("Started consumer for link {LinkName} from queue {QueueName}",
                link.Name, queueName);
        }
    }

    /// <summary>
    /// Stops consuming messages for a link.
    /// </summary>
    private void StopConsumer(AmqpLink link)
    {
        if (_consumers.TryRemove(link.Name, out var state))
        {
            state.CancellationTokenSource.Cancel();
            state.CancellationTokenSource.Dispose();
            _logger.LogDebug("Stopped consumer for link {LinkName}", link.Name);
        }
    }

    /// <summary>
    /// Consumer loop that dequeues messages and sends them to the client.
    /// </summary>
    private async Task ConsumeLoopAsync(ConsumerState state)
    {
        var link = state.Link;
        var queueName = state.QueueName;
        var cancellationToken = state.CancellationTokenSource.Token;
        
        try
        {
            var queue = _router.GetQueue(queueName);
            if (queue == null)
            {
                _logger.LogWarning("Queue {QueueName} not found for consumer on link {LinkName}",
                    queueName, link.Name);
                return;
            }
            
            queue.AddConsumer();
            
            try
            {
                while (!cancellationToken.IsCancellationRequested && link.State == LinkState.Attached)
                {
                    // Wait for credit
                    if (link.LinkCredit == 0)
                    {
                        await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    
                    // Dequeue a message with timeout
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(100));
                    
                    StoredMessage? storedMessage;
                    try
                    {
                        storedMessage = await queue.DequeueAsync(timeoutCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        // Timeout, continue loop
                        continue;
                    }
                    
                    if (storedMessage == null)
                    {
                        continue;
                    }
                    
                    // Build AMQP message
                    var amqpMessage = new AmqpMessage
                    {
                        Properties = new Properties
                        {
                            MessageId = storedMessage.MessageId.ToString(),
                            CorrelationId = storedMessage.CorrelationId,
                            ContentType = storedMessage.ContentType,
                            ReplyTo = storedMessage.ReplyTo,
                            Subject = storedMessage.RoutingKey
                        },
                        Body = [new DataSection { Binary = storedMessage.Body }]
                    };
                    
                    // Encode message
                    var buffer = new byte[amqpMessage.GetEncodedSize()];
                    var encodedSize = amqpMessage.Encode(buffer);
                    
                    var deliveryId = (uint)Interlocked.Increment(ref _nextDeliveryId);
                    var deliveryTag = BitConverter.GetBytes(deliveryId);
                    
                    // Send via the link's session
                    await link.Session.SendTransferAsync(
                        link.LocalHandle,
                        deliveryId,
                        deliveryTag,
                        buffer.AsMemory(0, encodedSize),
                        storedMessage.MessageId,
                        cancellationToken).ConfigureAwait(false);
                    
                    _logger.LogDebug("Sent message {MessageId} to link {LinkName}",
                        storedMessage.MessageId, link.Name);
                }
            }
            finally
            {
                queue.RemoveConsumer();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Consumer loop failed for link {LinkName}", link.Name);
        }
    }

    /// <summary>
    /// Parses an AMQP address into exchange name and routing key.
    /// Format: "exchange/routing-key" or just "queue-name" (uses default exchange)
    /// </summary>
    private static (string ExchangeName, string RoutingKey) ParseAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return ("", "");
        
        var slashIndex = address.IndexOf('/');
        if (slashIndex > 0)
        {
            return (address[..slashIndex], address[(slashIndex + 1)..]);
        }
        
        // No slash - treat as queue name with default exchange
        return ("", address);
    }

    private sealed record ConsumerState(AmqpLink Link, string QueueName, CancellationTokenSource CancellationTokenSource);
}

/// <summary>
/// Interface for broker link handling.
/// </summary>
public interface IBrokerLinkHandler
{
    /// <summary>
    /// Called when a link is attached.
    /// </summary>
    void OnLinkAttached(AmqpLink link);
    
    /// <summary>
    /// Called when a link is detached.
    /// </summary>
    void OnLinkDetached(AmqpLink link);
}
