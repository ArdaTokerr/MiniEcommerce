namespace MiniECommerce.Api.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}