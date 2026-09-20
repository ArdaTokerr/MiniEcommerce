using MiniECommerce.Api.Models;

namespace MiniECommerce.Api.Services.Discounts;

public interface IDiscountStrategy
{
    DiscountType Type { get; }
    decimal CalculateDiscount(decimal cartTotal, Coupon coupon);
}