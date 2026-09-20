using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniECommerce.Api.Common;
using MiniECommerce.Api.DTOs.Cart;
using MiniECommerce.Api.Services;

namespace MiniECommerce.Api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var cart = await _cartService.GetCartAsync(User.GetUserId());
        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(AddCartItemDto dto)
    {
        var cart = await _cartService.AddItemAsync(User.GetUserId(), dto);
        return Ok(cart);
    }

    [HttpPut("items/{productId:int}")]
    public async Task<ActionResult<CartDto>> UpdateItem(int productId, UpdateCartItemDto dto)
    {
        var cart = await _cartService.UpdateItemAsync(User.GetUserId(), productId, dto);
        return Ok(cart);
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<ActionResult<CartDto>> RemoveItem(int productId)
    {
        var cart = await _cartService.RemoveItemAsync(User.GetUserId(), productId);
        return Ok(cart);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        await _cartService.ClearCartAsync(User.GetUserId());
        return NoContent();
    }
}