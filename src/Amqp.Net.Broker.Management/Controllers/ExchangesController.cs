// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Exchanges;
using Amqp.Net.Broker.Core.Routing;
using Amqp.Net.Broker.Management.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Amqp.Net.Broker.Management.Controllers;

/// <summary>
/// API controller for managing exchanges.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ExchangesController : ControllerBase
{
    private readonly IMessageRouter _router;

    /// <summary>
    /// Creates a new exchanges controller.
    /// </summary>
    public ExchangesController(IMessageRouter router)
    {
        _router = router;
    }

    /// <summary>
    /// Gets all exchanges.
    /// </summary>
    /// <returns>List of exchanges.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExchangeDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ExchangeDto>> GetAll()
    {
        var exchanges = _router.Exchanges
                               .Select(ExchangeDto.FromExchange)
                               .ToList();
        return Ok(exchanges);
    }

    /// <summary>
    /// Gets an exchange by name.
    /// </summary>
    /// <param name="name">The exchange name.</param>
    /// <returns>The exchange.</returns>
    [HttpGet("{name}")]
    [ProducesResponseType(typeof(ExchangeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ExchangeDto> Get(string name)
    {
        var exchange = _router.GetExchange(name);
        if (exchange is null)
        {
            return NotFound();
        }
        return Ok(ExchangeDto.FromExchange(exchange));
    }

    /// <summary>
    /// Creates or declares an exchange.
    /// </summary>
    /// <param name="request">The exchange creation request.</param>
    /// <returns>The created exchange.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ExchangeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<ExchangeDto> Create([FromBody] CreateExchangeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Exchange name is required");
        }
        if (!Enum.TryParse<ExchangeType>(request.Type, true, out var exchangeType))
        {
            return BadRequest($"Invalid exchange type: {request.Type}. Valid types are: direct, topic, fanout, headers");
        }
        var exchange = _router.DeclareExchange(request.Name, exchangeType, request.Durable, request.AutoDelete);
        var dto = ExchangeDto.FromExchange(exchange);
        return CreatedAtAction(nameof(Get), new { name = exchange.Name }, dto);
    }

    /// <summary>
    /// Deletes an exchange.
    /// </summary>
    /// <param name="name">The exchange name.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{name}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Delete(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Exchange name is required");
        }

        // Prevent deletion of default exchanges
        if (name.Length == 0 || name.StartsWith("amq.", StringComparison.Ordinal))
        {
            return BadRequest("Cannot delete default exchanges");
        }
        if (!_router.DeleteExchange(name))
        {
            return NotFound();
        }
        return NoContent();
    }

    /// <summary>
    /// Gets all bindings for an exchange.
    /// </summary>
    /// <param name="name">The exchange name.</param>
    /// <returns>List of bindings.</returns>
    [HttpGet("{name}/bindings")]
    [ProducesResponseType(typeof(IEnumerable<BindingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<BindingDto>> GetBindings(string name)
    {
        var exchange = _router.GetExchange(name);
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
