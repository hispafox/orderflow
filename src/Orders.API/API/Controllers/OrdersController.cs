using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orders.API.API.DTOs;
using Orders.API.API.DTOs.Requests;
using Orders.API.API.DTOs.Responses;
using Orders.API.Application.Commands;
using Orders.API.Application.Queries;
using Orders.API.Infrastructure.Persistence;
using Orders.API.Sagas;

namespace Orders.API.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[AllowAnonymous]
public class OrdersController : ControllerBase
{
    private readonly IMediator                 _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException("User ID not found in token");

    private bool IsAdmin() => User.IsInRole("admin");

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetAll(
        [FromQuery] OrderFilterParams filters,
        CancellationToken ct = default)
    {
        var userId = GetUserId();

        var result = await _mediator.Send(
            new ListOrdersQuery(
                filters.Status,
                filters.Page,
                filters.PageSize,
                CustomerId: null,
                UserId:     userId,
                IsAdmin:    IsAdmin()), ct);

        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Total-Pages"]  = result.TotalPages.ToString();

        return Ok(result);
    }

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

        if (!IsAdmin() && order.CustomerId.ToString() != GetUserId())
            return NotFound();  // 404 — previene enumeración de recursos (no revelar que existe)

        return Ok(order);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct = default)
    {
        // Demo: si viene autenticado usa el sub del JWT, si no el CustomerId del body
        var customerId = User.Identity?.IsAuthenticated == true
            ? Guid.Parse(GetUserId())
            : (request.CustomerId != Guid.Empty
                ? request.CustomerId
                : Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));

        var command = new CreateOrderCommand(
            CustomerId:    customerId,
            CustomerEmail: request.CustomerEmail,
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

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct = default)
    {
        await _mediator.Send(new ConfirmOrderCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderRequest request, CancellationToken ct = default)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id), ct);
        if (order is null) return NotFound();

        if (!IsAdmin() && order.CustomerId.ToString() != GetUserId())
            return Forbid();

        await _mediator.Send(new CancelOrderCommand(id, request.Reason), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/saga-state")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSagaState(
        Guid id,
        [FromServices] OrderDbContext dbContext,
        CancellationToken ct = default)
    {
        var state = await dbContext.Set<OrderSagaState>()
            .FirstOrDefaultAsync(s => s.CorrelationId == id, ct);

        if (state is null)
            return NotFound();

        return Ok(new
        {
            OrderId       = state.CorrelationId,
            State         = state.CurrentState,
            PaymentId     = state.PaymentId,
            FailureReason = state.FailureReason,
            CreatedAt     = state.CreatedAt,
            CompletedAt   = state.CompletedAt
        });
    }
}
