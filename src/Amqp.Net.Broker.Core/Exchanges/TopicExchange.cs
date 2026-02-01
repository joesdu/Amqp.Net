// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Amqp.Net.Broker.Core.Routing;

namespace Amqp.Net.Broker.Core.Exchanges;

/// <summary>
/// Topic exchange - routes messages using pattern matching with * and # wildcards.
/// * matches exactly one word, # matches zero or more words.
/// </summary>
public sealed class TopicExchange : IExchange
{
    private readonly List<(Binding Binding, Regex Pattern)> _bindings = [];
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new topic exchange.
    /// </summary>
    public TopicExchange(string name, bool durable = false, bool autoDelete = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Durable = durable;
        AutoDelete = autoDelete;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ExchangeType Type => ExchangeType.Topic;

    /// <inheritdoc />
    public bool Durable { get; }

    /// <inheritdoc />
    public bool AutoDelete { get; }

    /// <inheritdoc />
    public IReadOnlyList<Binding> Bindings
    {
        get
        {
            lock (_lock)
            {
                return _bindings.Select(b => b.Binding).ToList();
            }
        }
    }

    /// <inheritdoc />
    public void AddBinding(Binding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        lock (_lock)
        {
            // Check for duplicate
            if (_bindings.Any(b => b.Binding.QueueName == binding.QueueName && b.Binding.RoutingKey == binding.RoutingKey))
            {
                return;
            }
            var pattern = ConvertToRegex(binding.RoutingKey);
            _bindings.Add((binding, pattern));
        }
    }

    /// <inheritdoc />
    public bool RemoveBinding(string queueName, string? routingKey = null)
    {
        lock (_lock)
        {
            var removed = routingKey == null
                              ? _bindings.RemoveAll(b => b.Binding.QueueName == queueName)
                              : _bindings.RemoveAll(b => b.Binding.QueueName == queueName && b.Binding.RoutingKey == routingKey);
            return removed > 0;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Route(string routingKey)
    {
        lock (_lock)
        {
            return _bindings
                   .Where(b => b.Pattern.IsMatch(routingKey))
                   .Select(b => b.Binding.QueueName)
                   .Distinct()
                   .ToList();
        }
    }

    /// <summary>
    /// Converts a topic pattern to a regex.
    /// * matches exactly one word (no dots)
    /// # matches zero or more words (including dots)
    /// </summary>
    private static Regex ConvertToRegex(string pattern)
    {
        // Escape regex special characters except * and #
        var escaped = Regex.Escape(pattern)
                           .Replace(@"\*", @"[^.]+", StringComparison.Ordinal) // * = one word (no dots)
                           .Replace(@"\#", @".*", StringComparison.Ordinal);   // # = zero or more words
        return new($"^{escaped}$", RegexOptions.Compiled);
    }
}
