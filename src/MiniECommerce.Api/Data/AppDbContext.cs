using Microsoft.EntityFrameworkCore;
using MiniECommerce.Api.Models;

namespace MiniECommerce.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(150);

            entity.HasOne(c => c.ParentCategory)
                  .WithMany(c => c.SubCategories)
                  .HasForeignKey(c => c.ParentCategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            entity.Property(p => p.RowVersion).IsRowVersion();

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint("CK_Product_Price_NonNegative", "[Price] >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Product_Stock_NonNegative", "[StockQuantity] >= 0"));
        });

        // Cart
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasIndex(c => c.UserId).IsUnique();

            entity.HasOne(c => c.User)
                  .WithOne(u => u.Cart)
                  .HasForeignKey<Cart>(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasIndex(ci => new { ci.CartId, ci.ProductId }).IsUnique();

            entity.HasOne(ci => ci.Cart)
                  .WithMany(c => c.Items)
                  .HasForeignKey(ci => ci.CartId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ci => ci.Product)
                  .WithMany(p => p.CartItems)
                  .HasForeignKey(ci => ci.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint("CK_CartItem_Quantity_Positive", "[Quantity] > 0"));
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.SubTotal).HasColumnType("decimal(18,2)");
            entity.Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

            entity.HasOne(o => o.User)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Coupon)
                  .WithMany(c => c.Orders)
                  .HasForeignKey(o => o.CouponId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(oi => oi.ProductName).IsRequired().HasMaxLength(200);

            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.Items)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Product)
                  .WithMany(p => p.OrderItems)
                  .HasForeignKey(oi => oi.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint("CK_OrderItem_Quantity_Positive", "[Quantity] > 0"));
        });

        // Coupon
        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasIndex(c => c.Code).IsUnique();
            entity.Property(c => c.Code).IsRequired().HasMaxLength(50);
            entity.Property(c => c.Value).HasColumnType("decimal(18,2)");
            entity.Property(c => c.MinCartAmount).HasColumnType("decimal(18,2)");
        });
    }
}