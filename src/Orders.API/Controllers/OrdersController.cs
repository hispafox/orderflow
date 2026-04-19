using Microsoft.AspNetCore.Mvc;

namespace Orders.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ILogger<OrdersController> logger)
    {
        _logger = logger;
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
        _logger.LogInformation("Creating new order");
        return Created($"/api/orders/{Guid.NewGuid()}", null);
    }
}
