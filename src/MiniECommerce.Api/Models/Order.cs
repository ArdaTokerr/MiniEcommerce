namespace MiniECommerce.Api.Models;

public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public int? CouponId { get; set; }
    public Coupon? Coupon { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}