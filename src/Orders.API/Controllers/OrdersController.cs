using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Orders.API.Application.Settings;

namespace Orders.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly OrdersSettings _settings;

    public OrdersController(
        ILogger<OrdersController> logger,
        IOptions<OrdersSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        _logger.LogInformation("Getting all orders");
        return Ok(new List<object>());
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        _logger.LogInformation("Getting order {OrderId}", id);
        return NotFound();
    }

    [HttpPost]
    public IActionResult Create([FromBody] object request)
    {
        _logger.LogInformation(
            "Creating new order. MaxLines: {MaxLines}, Currency: {Currency}",
            _settings.MaxLinesPerOrder,
            _settings.Currency);
        return Created($"/api/orders/{Guid.NewGuid()}", null);
    }
}
