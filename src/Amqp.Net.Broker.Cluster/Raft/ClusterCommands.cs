// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Amqp.Net.Broker.Core.Exchanges;
using Amqp.Net.Broker.Core.Queues;

namespace Amqp.Net.Broker.Cluster.Raft;

/// <summary>
/// Types of commands that can be replicated through Raft.
/// </summary>
public enum CommandType
{
    /// <summary>
    /// No operation.
    /// </summary>
    None = 0,

    /// <summary>
    /// Declare an exchange.
    /// </summary>
    DeclareExchange = 1,

    /// <summary>
    /// Delete an exchange.
    /// </summary>
    DeleteExchange = 2,

    /// <summary>
    /// Declare a queue.
    /// </summary>
    DeclareQueue = 3,

    /// <summary>
    /// Delete a queue.
    /// </summary>
    DeleteQueue = 4,

    /// <summary>
    /// Bind a queue to an exchange.
    /// </summary>
    Bind = 5,

    /// <summary>
    /// Unbind a queue from an exchange.
    /// </summary>
    Unbind = 6
}

/// <summary>
/// Base class for Raft log entry commands.
/// </summary>
public abstract record ClusterCommand
{
    /// <summary>
    /// The command type.
    /// </summary>
    public abstract CommandType Type { get; }

    /// <summary>
    /// Serializes the command to bytes.
    /// </summary>
    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        stream.WriteByte((byte)Type);
        JsonSerializer.Serialize(stream, this, GetType(), ClusterCommandSerializerContext.Default.Options);
        return stream.ToArray();
    }

    /// <summary>
    /// Deserializes a command from bytes.
    /// </summary>
    public static ClusterCommand Deserialize(ReadOnlySpan<byte> data)
    {
        var type = (CommandType)data[0];
        var json = data[1..];
        return type switch
        {
            CommandType.DeclareExchange => JsonSerializer.Deserialize(json, ClusterCommandSerializerContext.Default.DeclareExchangeCommand)!,
            CommandType.DeleteExchange  => JsonSerializer.Deserialize(json, ClusterCommandSerializerContext.Default.DeleteExchangeCommand)!,
            CommandType.DeclareQueue    => JsonSerializer.Deserialize(json, ClusterCommandSerializerContext.Default.DeclareQueueCommand)!,
            CommandType.DeleteQueue     => JsonSerializer.Deserialize(json, ClusterCommandSerializerContext.Default.DeleteQueueCommand)!,
            CommandType.Bind            => JsonSerializer.Deserialize(json, ClusterCommandSerializerContext.Default.BindCommand)!,
            CommandType.Unbind          => JsonSerializer.Deserialize(json, ClusterCommandSerializerContext.Default.UnbindCommand)!,
            _                           => throw new InvalidOperationException($"Unknown command type: {type}")
        };
    }
}

/// <summary>
/// Command to declare an exchange.
/// </summary>
public sealed record DeclareExchangeCommand : ClusterCommand
{
    /// <inheritdoc />
    public override CommandType Type => CommandType.DeclareExchange;

    /// <summary>
    /// Exchange name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Exchange type.
    /// </summary>
    public required ExchangeType ExchangeType { get; init; }

    /// <summary>
    /// Whether the exchange is durable.
    /// </summary>
    public bool Durable { get; init; }

    /// <summary>
    /// Whether the exchange is auto-delete.
    /// </summary>
    public bool AutoDelete { get; init; }
}

/// <summary>
/// Command to delete an exchange.
/// </summary>
public sealed record DeleteExchangeCommand : ClusterCommand
{
    /// <inheritdoc />
    public override CommandType Type => CommandType.DeleteExchange;

    /// <summary>
    /// Exchange name.
    /// </summary>
    public required string Name { get; init; }
}

/// <summary>
/// Command to declare a queue.
/// </summary>
public sealed record DeclareQueueCommand : ClusterCommand
{
    /// <inheritdoc />
    public override CommandType Type => CommandType.DeclareQueue;

    /// <summary>
    /// Queue name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Queue options.
    /// </summary>
    public required QueueOptionsDto Options { get; init; }
}

/// <summary>
/// DTO for queue options (serializable).
/// </summary>
public sealed record QueueOptionsDto
{
    /// <summary>
    /// Whether the queue is durable.
    /// </summary>
    public bool Durable { get; init; }

    /// <summary>
    /// Whether the queue is exclusive.
    /// </summary>
    public bool Exclusive { get; init; }

    /// <summary>
    /// Whether the queue is auto-delete.
    /// </summary>
    public bool AutoDelete { get; init; }

    /// <summary>
    /// Maximum number of messages.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Maximum total size in bytes.
    /// </summary>
    public long? MaxLengthBytes { get; init; }

    /// <summary>
    /// Message TTL in milliseconds.
    /// </summary>
    public long? MessageTtlMs { get; init; }

    /// <summary>
    /// Dead letter exchange.
    /// </summary>
    public string? DeadLetterExchange { get; init; }

    /// <summary>
    /// Dead letter routing key.
    /// </summary>
    public string? DeadLetterRoutingKey { get; init; }

    /// <summary>
    /// Maximum priority.
    /// </summary>
    public byte MaxPriority { get; init; }

    /// <summary>
    /// Creates from QueueOptions.
    /// </summary>
    public static QueueOptionsDto FromQueueOptions(QueueOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new()
        {
            Durable = options.Durable,
            Exclusive = options.Exclusive,
            AutoDelete = options.AutoDelete,
            MaxLength = options.MaxLength,
            MaxLengthBytes = options.MaxLengthBytes,
            MessageTtlMs = options.MessageTtl?.TotalMilliseconds is { } ms ? (long)ms : null,
            DeadLetterExchange = options.DeadLetterExchange,
            DeadLetterRoutingKey = options.DeadLetterRoutingKey,
            MaxPriority = options.MaxPriority
        };
    }

    /// <summary>
    /// Converts to QueueOptions.
    /// </summary>
    public QueueOptions ToQueueOptions() =>
        new()
        {
            Durable = Durable,
            Exclusive = Exclusive,
            AutoDelete = AutoDelete,
            MaxLength = MaxLength,
            MaxLengthBytes = MaxLengthBytes,
            MessageTtl = MessageTtlMs.HasValue ? TimeSpan.FromMilliseconds(MessageTtlMs.Value) : null,
            DeadLetterExchange = DeadLetterExchange,
            DeadLetterRoutingKey = DeadLetterRoutingKey,
            MaxPriority = MaxPriority
        };
}

/// <summary>
/// Command to delete a queue.
/// </summary>
public sealed record DeleteQueueCommand : ClusterCommand
{
    /// <inheritdoc />
    public override CommandType Type => CommandType.DeleteQueue;

    /// <summary>
    /// Queue name.
    /// </summary>
    public required string Name { get; init; }
}

/// <summary>
/// Command to bind a queue to an exchange.
/// </summary>
public sealed record BindCommand : ClusterCommand
{
    /// <inheritdoc />
    public override CommandType Type => CommandType.Bind;

    /// <summary>
    /// Queue name.
    /// </summary>
    public required string QueueName { get; init; }

    /// <summary>
    /// Exchange name.
    /// </summary>
    public required string ExchangeName { get; init; }

    /// <summary>
    /// Routing key.
    /// </summary>
    public string RoutingKey { get; init; } = "";
}

/// <summary>
/// Command to unbind a queue from an exchange.
/// </summary>
public sealed record UnbindCommand : ClusterCommand
{
    /// <inheritdoc />
    public override CommandType Type => CommandType.Unbind;

    /// <summary>
    /// Queue name.
    /// </summary>
    public required string QueueName { get; init; }

    /// <summary>
    /// Exchange name.
    /// </summary>
    public required string ExchangeName { get; init; }

    /// <summary>
    /// Routing key.
    /// </summary>
    public string RoutingKey { get; init; } = "";
}

/// <summary>
/// JSON serialization context for cluster commands.
/// </summary>
[JsonSerializable(typeof(DeclareExchangeCommand))]
[JsonSerializable(typeof(DeleteExchangeCommand))]
[JsonSerializable(typeof(DeclareQueueCommand))]
[JsonSerializable(typeof(DeleteQueueCommand))]
[JsonSerializable(typeof(BindCommand))]
[JsonSerializable(typeof(UnbindCommand))]
[JsonSerializable(typeof(QueueOptionsDto))]
internal sealed partial class ClusterCommandSerializerContext : JsonSerializerContext { }
