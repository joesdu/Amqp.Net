// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Amqp.Net.Broker.Core.Exchanges;
using Amqp.Net.Broker.Core.Routing;
using Amqp.Net.Broker.Management.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Amqp.Net.Broker.Management.Controllers;

/// <summary>
/// API controller for broker overview and statistics.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class OverviewController : ControllerBase
{
    private static readonly DateTimeOffset StartTime = DateTimeOffset.UtcNow;

    private static readonly string Version = Assembly.GetExecutingAssembly()
                                                     .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
                                             Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    private readonly IMessageRouter _router;

    /// <summary>
    /// Creates a new overview controller.
    /// </summary>
    public OverviewController(IMessageRouter router)
    {
        _router = router;
    }

    /// <summary>
    /// Gets the broker overview with statistics.
    /// </summary>
    /// <returns>Broker overview.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(OverviewDto), StatusCodes.Status200OK)]
    public ActionResult<OverviewDto> Get()
    {
        var queues = _router.Queues;
        var exchanges = _router.Exchanges;
        var overview = new OverviewDto
        {
            Version = Version,
            NodeName = Environment.MachineName,
            UptimeMs = (long)(DateTimeOffset.UtcNow - StartTime).TotalMilliseconds,
            Queues = new()
            {
                Total = queues.Count,
                Durable = queues.Count(q => q.Options.Durable),
                Transient = queues.Count(q => !q.Options.Durable)
            },
            Exchanges = new()
            {
                Total = exchanges.Count,
                Direct = exchanges.Count(e => e.Type == ExchangeType.Direct),
                Topic = exchanges.Count(e => e.Type == ExchangeType.Topic),
                Fanout = exchanges.Count(e => e.Type == ExchangeType.Fanout),
                Headers = exchanges.Count(e => e.Type == ExchangeType.Headers)
            },
            Messages = new()
            {
                Total = queues.Sum(q => (long)q.MessageCount),
                TotalSizeBytes = queues.Sum(q => q.TotalSizeBytes),
                TotalConsumers = queues.Sum(q => q.ConsumerCount)
            }
        };
        return Ok(overview);
    }

    /// <summary>
    /// Gets a simple health check response.
    /// </summary>
    /// <returns>Health status.</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Health() =>
        Ok(new HealthResponse
        {
            Status = "ok",
            Version = Version,
            UptimeMs = (long)(DateTimeOffset.UtcNow - StartTime).TotalMilliseconds
        });
}

/// <summary>
/// Health check response.
/// </summary>
public sealed record HealthResponse
{
    /// <summary>
    /// Health status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Broker version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Uptime in milliseconds.
    /// </summary>
    public long UptimeMs { get; init; }
}
