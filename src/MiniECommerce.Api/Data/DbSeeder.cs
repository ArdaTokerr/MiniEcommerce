using MiniECommerce.Api.Models;

namespace MiniECommerce.Api.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        db.Database.EnsureCreated();

        if (db.Users.Any())
        {
            return;
        }

        // Admin
        var admin = new User
        {
            FullName = "Admin User",
            Email = "admin@ecommerce.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.Admin
        };
        db.Users.Add(admin);
        db.Carts.Add(new Cart { User = admin });

        // Customer
        var customer = new User
        {
            FullName = "Customer User",
            Email = "arda@toker.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("arda123"),
            Role = UserRole.Customer
        };
        db.Users.Add(customer);
        db.Carts.Add(new Cart { User = customer });

        // Kategoriler
        var electronics = new Category { Name = "Electronics" };
        var fashion = new Category { Name = "Fashion" };
        db.Categories.AddRange(electronics, fashion);
        db.SaveChanges();

        var phones = new Category { Name = "Phones", ParentCategoryId = electronics.Id };
        var laptops = new Category { Name = "Laptops", ParentCategoryId = electronics.Id };
        var mensWear = new Category { Name = "Men's Wear", ParentCategoryId = fashion.Id };
        db.Categories.AddRange(phones, laptops, mensWear);
        db.SaveChanges();

        // Ürünler
        db.Products.AddRange(
            new Product { Name = "Galaxy S24", Description = "Flagship Android phone", Price = 999.99m, StockQuantity = 25, CategoryId = phones.Id },
            new Product { Name = "iPhone 15", Description = "Apple smartphone", Price = 1099.00m, StockQuantity = 15, CategoryId = phones.Id },
            new Product { Name = "ThinkPad X1", Description = "Business ultrabook", Price = 1450.50m, StockQuantity = 10, CategoryId = laptops.Id },
            new Product { Name = "MacBook Air M3", Description = "Apple laptop", Price = 1299.00m, StockQuantity = 1, CategoryId = laptops.Id },
            new Product { Name = "Classic Denim Jacket", Description = "100% cotton denim jacket", Price = 79.90m, StockQuantity = 50, CategoryId = mensWear.Id },
            new Product { Name = "Slim Fit Shirt", Description = "Wrinkle-resistant shirt", Price = 34.50m, StockQuantity = 0, CategoryId = mensWear.Id }
        );

        // Kuponlar
        db.Coupons.AddRange(
            new Coupon { Code = "WELCOME10", DiscountType = DiscountType.Percentage, Value = 10, ExpiryDate = DateTime.UtcNow.AddMonths(6) },
            new Coupon { Code = "FLAT50", DiscountType = DiscountType.FixedAmount, Value = 50, MinCartAmount = 300, ExpiryDate = DateTime.UtcNow.AddMonths(6) },
            new Coupon { Code = "EXPIRED5", DiscountType = DiscountType.Percentage, Value = 5, ExpiryDate = DateTime.UtcNow.AddDays(-1) }
        );

        db.SaveChanges();
    }
}