using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniECommerce.Api.DTOs.Coupons;
using MiniECommerce.Api.Services;

namespace MiniECommerce.Api.Controllers;

[ApiController]
[Route("api/coupons")]
public class CouponsController : ControllerBase
{
    private readonly ICouponService _couponService;

    public CouponsController(ICouponService couponService)
    {
        _couponService = couponService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<CouponDto>>> GetAll()
    {
        var coupons = await _couponService.GetAllAsync();
        return Ok(coupons);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CouponDto>> Create(CouponCreateDto dto)
    {
        var coupon = await _couponService.CreateAsync(dto);
        return Ok(coupon);
    }

    [HttpGet("{code}/preview")]
    [Authorize]
    public async Task<ActionResult<CouponPreviewResultDto>> Preview(string code, [FromQuery] decimal cartTotal)
    {
        var preview = await _couponService.PreviewAsync(code, cartTotal);
        return Ok(preview);
    }
}