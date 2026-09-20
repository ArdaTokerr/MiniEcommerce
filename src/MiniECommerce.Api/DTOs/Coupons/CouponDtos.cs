using System.ComponentModel.DataAnnotations;
using MiniECommerce.Api.Models;

namespace MiniECommerce.Api.DTOs.Coupons;

public class CouponDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }
    public decimal? MinCartAmount { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public int? UsageLimit { get; set; }
    public int TimesUsed { get; set; }
}

public class CouponCreateDto
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public DiscountType DiscountType { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Value { get; set; }

    public decimal? MinCartAmount { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? UsageLimit { get; set; }
}

public class CouponPreviewResultDto
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalTotal { get; set; }
}
