using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniECommerce.Api.Common;
using MiniECommerce.Api.DTOs.Orders;
using MiniECommerce.Api.Services;

namespace MiniECommerce.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderDto dto)
    {
        var userId = User.GetUserId();
        var order = await _orderService.CreateOrderAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetMine()
    {
        var orders = await _orderService.GetOrdersForUserAsync(User.GetUserId());
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(User.GetUserId(), id, User.IsAdmin());
        return Ok(order);
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(int id)
    {
        var order = await _orderService.CancelOrderAsync(User.GetUserId(), id, User.IsAdmin());
        return Ok(order);
    }
}