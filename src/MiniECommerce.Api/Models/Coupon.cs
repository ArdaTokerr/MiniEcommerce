namespace MiniECommerce.Api.Models;

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;

    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }
    public decimal? MinCartAmount { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;

    public int? UsageLimit { get; set; }
    public int TimesUsed { get; set; } = 0;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}