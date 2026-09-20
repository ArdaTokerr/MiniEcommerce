using Microsoft.EntityFrameworkCore;
using MiniECommerce.Api.Common.Exceptions;
using MiniECommerce.Api.Data;
using MiniECommerce.Api.DTOs.Coupons;
using MiniECommerce.Api.Models;
using MiniECommerce.Api.Services.Discounts;

namespace MiniECommerce.Api.Services;

public interface ICouponService
{
    Task<CouponDto> CreateAsync(CouponCreateDto dto);
    Task<List<CouponDto>> GetAllAsync();
    Task<(Coupon Coupon, decimal DiscountAmount)> ValidateAndCalculateAsync(string code, decimal cartTotal);
    Task<CouponPreviewResultDto> PreviewAsync(string code, decimal cartTotal);
}

public class CouponService : ICouponService
{
    private readonly AppDbContext _db;
    private readonly IDiscountStrategyFactory _strategyFactory;

    public CouponService(AppDbContext db, IDiscountStrategyFactory strategyFactory)
    {
        _db = db;
        _strategyFactory = strategyFactory;
    }

    public async Task<CouponDto> CreateAsync(CouponCreateDto dto)
    {
        var normalizedCode = dto.Code.Trim().ToUpperInvariant();

        var exists = await _db.Coupons.AnyAsync(c => c.Code == normalizedCode);
        if (exists)
        {
            throw new BadRequestException($"'{normalizedCode}' kodlu kupon zaten mevcut.");
        }

        var coupon = new Coupon
        {
            Code = normalizedCode,
            DiscountType = dto.DiscountType,
            Value = dto.Value,
            MinCartAmount = dto.MinCartAmount,
            ExpiryDate = dto.ExpiryDate,
            UsageLimit = dto.UsageLimit,
            IsActive = true
        };

        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync();

        return ToDto(coupon);
    }

    public async Task<List<CouponDto>> GetAllAsync()
    {
        var coupons = await _db.Coupons.OrderByDescending(c => c.Id).ToListAsync();
        return coupons.Select(ToDto).ToList();
    }

    public async Task<(Coupon Coupon, decimal DiscountAmount)> ValidateAndCalculateAsync(string code, decimal cartTotal)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == normalizedCode);

        if (coupon is null)
        {
            throw new CouponInvalidException($"'{code}' geçerli bir kupon kodu değil.");
        }

        ApplyBusinessRules(coupon, cartTotal);

        var strategy = _strategyFactory.GetStrategy(coupon.DiscountType);
        var discount = strategy.CalculateDiscount(cartTotal, coupon);

        return (coupon, discount);
    }

    public async Task<CouponPreviewResultDto> PreviewAsync(string code, decimal cartTotal)
    {
        try
        {
            var (_, discount) = await ValidateAndCalculateAsync(code, cartTotal);
            return new CouponPreviewResultDto
            {
                IsValid = true,
                DiscountAmount = discount,
                FinalTotal = cartTotal - discount
            };
        }
        catch (CouponInvalidException ex)
        {
            return new CouponPreviewResultDto
            {
                IsValid = false,
                Message = ex.Message,
                DiscountAmount = 0,
                FinalTotal = cartTotal
            };
        }
    }

    private static void ApplyBusinessRules(Coupon coupon, decimal cartTotal)
    {
        if (!coupon.IsActive)
        {
            throw new CouponInvalidException("Bu kupon aktif değil.");
        }

        if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < DateTime.UtcNow)
        {
            throw new CouponInvalidException("Bu kuponun kullanım süresi dolmuş.");
        }

        if (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit.Value)
        {
            throw new CouponInvalidException("Bu kuponun kullanım limiti tükendi.");
        }

        if (coupon.MinCartAmount.HasValue && cartTotal < coupon.MinCartAmount.Value)
        {
            throw new CouponInvalidException(
                $"Bu kuponu kullanabilmek için sepet tutarı en az {coupon.MinCartAmount.Value:C} olmalıdır.");
        }
    }

    private static CouponDto ToDto(Coupon c) => new()
    {
        Id = c.Id,
        Code = c.Code,
        DiscountType = c.DiscountType,
        Value = c.Value,
        MinCartAmount = c.MinCartAmount,
        ExpiryDate = c.ExpiryDate,
        IsActive = c.IsActive,
        UsageLimit = c.UsageLimit,
        TimesUsed = c.TimesUsed
    };
}