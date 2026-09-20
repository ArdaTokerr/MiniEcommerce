using MiniECommerce.Api.Models;

namespace MiniECommerce.Api.Services.Discounts;

public class FixedAmountDiscountStrategy : IDiscountStrategy
{
    public DiscountType Type => DiscountType.FixedAmount;

    public decimal CalculateDiscount(decimal cartTotal, Coupon coupon)
    {
        return Math.Min(coupon.Value, cartTotal);
    }
}

public class PercentageDiscountStrategy : IDiscountStrategy
{
    public DiscountType Type => DiscountType.Percentage;

    public decimal CalculateDiscount(decimal cartTotal, Coupon coupon)
    {
        var clampedPercentage = Math.Clamp(coupon.Value, 0, 100);
        var discount = cartTotal * (clampedPercentage / 100m);
        return Math.Round(discount, 2, MidpointRounding.AwayFromZero);
    }
}

public interface IDiscountStrategyFactory
{
    IDiscountStrategy GetStrategy(DiscountType type);
}

public class DiscountStrategyFactory : IDiscountStrategyFactory
{
    private readonly Dictionary<DiscountType, IDiscountStrategy> _strategies;

    public DiscountStrategyFactory(IEnumerable<IDiscountStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Type);
    }

    public IDiscountStrategy GetStrategy(DiscountType type)
    {
        if (!_strategies.TryGetValue(type, out var strategy))
        {
            throw new NotSupportedException($"Tanımlı indirim stratejisi bulunamadı: '{type}'.");
        }
        return strategy;
    }
}