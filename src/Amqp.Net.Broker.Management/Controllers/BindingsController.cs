// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Routing;
using Amqp.Net.Broker.Management.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Amqp.Net.Broker.Management.Controllers;

/// <summary>
/// API controller for managing bindings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class BindingsController : ControllerBase
{
    private readonly IMessageRouter _router;

    /// <summary>
    /// Creates a new bindings controller.
    /// </summary>
    public BindingsController(IMessageRouter router)
    {
        _router = router;
    }

    /// <summary>
    /// Gets all bindings across all exchanges.
    /// </summary>
    /// <returns>List of bindings.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BindingDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<BindingDto>> GetAll()
    {
        var bindings = _router.Exchanges
                              .SelectMany(e => e.Bindings)
                              .Select(BindingDto.FromBinding)
                              .ToList();
        return Ok(bindings);
    }

    /// <summary>
    /// Creates a binding between an exchange and a queue.
    /// </summary>
    /// <param name="request">The binding creation request.</param>
    /// <returns>The created binding.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BindingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<BindingDto> Create([FromBody] CreateBindingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Destination))
        {
            return BadRequest("Destination queue name is required");
        }
        var exchange = _router.GetExchange(request.Source);
        if (exchange is null)
        {
            return NotFound($"Exchange '{request.Source}' not found");
        }
        var queue = _router.GetQueue(request.Destination);
        if (queue is null)
        {
            return NotFound($"Queue '{request.Destination}' not found");
        }
        _router.Bind(request.Destination, request.Source, request.RoutingKey);
        var dto = new BindingDto
        {
            Source = request.Source,
            Destination = request.Destination,
            RoutingKey = request.RoutingKey
        };
        var locationUri = new Uri($"/api/bindings/{Uri.EscapeDataString(request.Source)}/{Uri.EscapeDataString(request.Destination)}", UriKind.Relative);
        return Created(locationUri, dto);
    }

    /// <summary>
    /// Deletes a binding between an exchange and a queue.
    /// </summary>
    /// <param name="request">The binding deletion request.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult Delete([FromBody] DeleteBindingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Destination))
        {
            return BadRequest("Destination queue name is required");
        }
        _router.Unbind(request.Destination, request.Source, request.RoutingKey);
        return NoContent();
    }

    /// <summary>
    /// Gets all bindings for a specific queue.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>List of bindings for the queue.</returns>
    [HttpGet("queue/{queueName}")]
    [ProducesResponseType(typeof(IEnumerable<BindingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<BindingDto>> GetByQueue(string queueName)
    {
        var queue = _router.GetQueue(queueName);
        if (queue is null)
        {
            return NotFound();
        }
        var bindings = _router.Exchanges
                              .SelectMany(e => e.Bindings)
                              .Where(b => b.QueueName == queueName)
                              .Select(BindingDto.FromBinding)
                              .ToList();
        return Ok(bindings);
    }

    /// <summary>
    /// Gets all bindings for a specific exchange.
    /// </summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <returns>List of bindings for the exchange.</returns>
    [HttpGet("exchange/{exchangeName}")]
    [ProducesResponseType(typeof(IEnumerable<BindingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<BindingDto>> GetByExchange(string exchangeName)
    {
        var exchange = _router.GetExchange(exchangeName);
        if (exchange is null)
        {
            return NotFound();
        }
        var bindings = exchange.Bindings
                               .Select(BindingDto.FromBinding)
                               .ToList();
        return Ok(bindings);
    }
}
