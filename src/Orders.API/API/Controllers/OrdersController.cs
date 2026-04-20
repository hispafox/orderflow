using MediatR;
using Microsoft.AspNetCore.Mvc;
using Orders.API.API.DTOs;
using Orders.API.API.DTOs.Requests;
using Orders.API.API.DTOs.Responses;
using Orders.API.Application.Commands;
using Orders.API.Application.Queries;

namespace Orders.API.API.Controllers;

/// <summary>
/// Controller de gestión de pedidos.
/// Principio del Controller delgado: solo traduce HTTP ↔ Commands/Queries.
/// Toda la lógica de negocio vive en los Handlers de MediatR.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    /// <summary>Lista pedidos con filtros opcionales y paginación.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetAll(
        [FromQuery] OrderFilterParams filters,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Listing orders. Status: {Status}, Page: {Page}", filters.Status, filters.Page);

        var result = await _mediator.Send(
            new ListOrdersQuery(filters.Status, filters.Page, filters.PageSize, filters.CustomerId), ct);

        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Total-Pages"]  = result.TotalPages.ToString();

        return Ok(result);
    }

    /// <summary>Obtiene el detalle completo de un pedido.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id), ct);

        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found", id);
            return NotFound();
        }

        return Ok(order);
    }

    /// <summary>Crea un nuevo pedido.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct = default)
    {
        var command = new CreateOrderCommand(
            CustomerId: request.CustomerId,
            Items: request.Items.Select(i => new CreateOrderItemDto(
                i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.Currency)).ToList(),
            ShippingAddress: new CreateOrderAddressDto(
                request.ShippingAddress.Street,
                request.ShippingAddress.City,
                request.ShippingAddress.ZipCode,
                request.ShippingAddress.Country));

        var result = await _mediator.Send(command, ct);

        _logger.LogInformation("Order {OrderId} created for customer {CustomerId}", result.Id, result.CustomerId);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Confirma un pedido pendiente.</summary>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct = default)
    {
        await _mediator.Send(new ConfirmOrderCommand(id), ct);
        return NoContent();
    }

    /// <summary>Cancela un pedido.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderRequest request, CancellationToken ct = default)
    {
        await _mediator.Send(new CancelOrderCommand(id, request.Reason), ct);
        return NoContent();
    }
}
