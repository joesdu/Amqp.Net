// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amqp.Net.Broker.Core.Routing;
using Amqp.Net.Broker.Management.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Amqp.Net.Broker.Management.Controllers;

/// <summary>
/// API controller for managing queues.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class QueuesController : ControllerBase
{
    private readonly IMessageRouter _router;

    /// <summary>
    /// Creates a new queues controller.
    /// </summary>
    public QueuesController(IMessageRouter router)
    {
        _router = router;
    }

    /// <summary>
    /// Gets all queues.
    /// </summary>
    /// <returns>List of queues.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<QueueDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<QueueDto>> GetAll()
    {
        var queues = _router.Queues
            .Select(QueueDto.FromQueue)
            .ToList();

        return Ok(queues);
    }

    /// <summary>
    /// Gets a queue by name.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The queue.</returns>
    [HttpGet("{name}")]
    [ProducesResponseType(typeof(QueueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<QueueDto> Get(string name)
    {
        var queue = _router.GetQueue(name);
        if (queue is null)
        {
            return NotFound();
        }

        return Ok(QueueDto.FromQueue(queue));
    }

    /// <summary>
    /// Creates or declares a queue.
    /// </summary>
    /// <param name="request">The queue creation request.</param>
    /// <returns>The created queue.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(QueueDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<QueueDto> Create([FromBody] CreateQueueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Queue name is required");
        }

        var options = request.ToQueueOptions();
        var queue = _router.DeclareQueue(request.Name, options);
        var dto = QueueDto.FromQueue(queue);

        return CreatedAtAction(nameof(Get), new { name = queue.Name }, dto);
    }

    /// <summary>
    /// Deletes a queue.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{name}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Delete(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Queue name is required");
        }

        if (!_router.DeleteQueue(name))
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Purges all messages from a queue.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>Number of messages purged.</returns>
    [HttpDelete("{name}/contents")]
    [ProducesResponseType(typeof(PurgeQueueResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurgeQueueResponse>> Purge(string name)
    {
        var queue = _router.GetQueue(name);
        if (queue is null)
        {
            return NotFound();
        }

        var purged = await queue.PurgeAsync().ConfigureAwait(false);

        return Ok(new PurgeQueueResponse { MessagesPurged = purged });
    }
}
