// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using Amqp.Net.Broker.Core.Messages;
using Amqp.Net.Broker.Core.Routing;
using Amqp.Net.Broker.Server.Links;
using Amqp.Net.Protocol.Messaging;
using Amqp.Net.Protocol.Performatives;
using Microsoft.Extensions.Logging;

namespace Amqp.Net.Broker.Server.Handlers;

/// <summary>
/// Handles the integration between AMQP links and the broker's message routing system.
/// </summary>
/// <remarks>
/// Creates a new broker link handler.
/// </remarks>
public sealed class BrokerLinkHandler(IMessageRouter router, ILogger<BrokerLinkHandler> logger) : IBrokerLinkHandler
{
    private static readonly Action<ILogger, string?, string?, Exception?> s_receiverLinkAttached =
        LoggerMessage.Define<string?, string?>(LogLevel.Debug,
            new(1, nameof(s_receiverLinkAttached)),
            "Receiver link {LinkName} attached, ready to receive messages to {Target}");

    private static readonly Action<ILogger, string?, int, string, string, Exception?> s_routedMessage =
        LoggerMessage.Define<string?, int, string, string>(LogLevel.Debug,
            new(2, nameof(s_routedMessage)),
            "Routed message from link {LinkName} to {Count} queue(s) via exchange '{Exchange}' with key '{RoutingKey}'");

    private static readonly Action<ILogger, string?, Exception?> s_failedToProcessMessage =
        LoggerMessage.Define<string?>(LogLevel.Error,
            new(3, nameof(s_failedToProcessMessage)),
            "Failed to process message on link {LinkName}");

    private static readonly Action<ILogger, string?, string?, Exception?> s_startedConsumer =
        LoggerMessage.Define<string?, string?>(LogLevel.Debug,
            new(4, nameof(s_startedConsumer)),
            "Started consumer for link {LinkName} from queue {QueueName}");

    private static readonly Action<ILogger, string?, Exception?> s_stoppedConsumer =
        LoggerMessage.Define<string?>(LogLevel.Debug,
            new(5, nameof(s_stoppedConsumer)),
            "Stopped consumer for link {LinkName}");

    private static readonly Action<ILogger, string?, string?, Exception?> s_queueNotFound =
        LoggerMessage.Define<string?, string?>(LogLevel.Warning,
            new(6, nameof(s_queueNotFound)),
            "Queue {QueueName} not found for consumer on link {LinkName}");

    private static readonly Action<ILogger, long, string?, Exception?> s_sentMessage =
        LoggerMessage.Define<long, string?>(LogLevel.Debug,
            new(7, nameof(s_sentMessage)),
            "Sent message {MessageId} to link {LinkName}");

    private static readonly Action<ILogger, string?, Exception?> s_consumerLoopFailed =
        LoggerMessage.Define<string?>(LogLevel.Error,
            new(8, nameof(s_consumerLoopFailed)),
            "Consumer loop failed for link {LinkName}");

    private readonly ConcurrentDictionary<string, ConsumerState> _consumers = new();
    private long _nextDeliveryId;

    /// <inheritdoc />
    public void OnLinkAttached(AmqpLink link)
    {
        ArgumentNullException.ThrowIfNull(link);
        if (link.IsReceiver)
        {
            // We are receiver = client is sender = client publishes messages
            // Set up handler to route incoming messages
            link.OnMessageReceived = HandleIncomingMessageAsync;
            s_receiverLinkAttached(logger, link.Name, link.TargetAddress, null);
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
        ArgumentNullException.ThrowIfNull(link);
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
            var routedCount = await router.RouteAsync(exchangeName,
                                  routingKey,
                                  body,
                                  properties,
                                  cancellationToken).ConfigureAwait(false);
            s_routedMessage(logger, link.Name, routedCount, exchangeName, routingKey, null);

            // Settle the delivery if not pre-settled
            if (!delivery.Settled)
            {
                await link.SettleAsync(delivery.DeliveryId, Accepted.Instance, cancellationToken)
                          .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            s_failedToProcessMessage(logger, link.Name, ex);

            // Reject the delivery
            if (!delivery.Settled)
            {
                var rejected = new Rejected
                {
                    Error = new()
                    {
                        Condition = "amqp:internal-error",
                        Description = ex.Message
                    }
                };
                await link.SettleAsync(delivery.DeliveryId, rejected, cancellationToken)
                          .ConfigureAwait(false);
            }
            throw;
        }
    }

    /// <summary>
    /// Starts consuming messages from a queue and sending them to the client via the sender link.
    /// </summary>
    private void StartConsumer(AmqpLink link, string queueName)
    {
        CancellationTokenSource? cts = null;
        try
        {
            cts = new();
            var state = new ConsumerState(link, queueName, cts);
            if (_consumers.TryAdd(link.Name, state))
            {
                // Start the consumer loop
                _ = ConsumeLoopAsync(state);
                s_startedConsumer(logger, link.Name, queueName, null);
                cts = null;
            }
        }
        finally
        {
            cts?.Dispose();
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
            s_stoppedConsumer(logger, link.Name, null);
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
            var queue = router.GetQueue(queueName);
            if (queue == null)
            {
                s_queueNotFound(logger, queueName, link.Name, null);
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
                        Properties = new()
                        {
                            MessageId = storedMessage.MessageId.ToString(CultureInfo.InvariantCulture),
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
                    await link.Session.SendTransferAsync(link.LocalHandle,
                        deliveryId,
                        deliveryTag,
                        buffer.AsMemory(0, encodedSize),
                        storedMessage.MessageId,
                        cancellationToken).ConfigureAwait(false);
                    s_sentMessage(logger, storedMessage.MessageId, link.Name, null);
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
            s_consumerLoopFailed(logger, link.Name, ex);
            throw;
        }
    }

    /// <summary>
    /// Parses an AMQP address into exchange name and routing key.
    /// Format: "exchange/routing-key" or just "queue-name" (uses default exchange)
    /// </summary>
    private static (string ExchangeName, string RoutingKey) ParseAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            return ("", "");
        }
        var slashIndex = address.IndexOf('/', StringComparison.Ordinal);
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
