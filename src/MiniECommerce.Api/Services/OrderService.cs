using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MiniECommerce.Api.Common.Exceptions;
using MiniECommerce.Api.Data;
using MiniECommerce.Api.DTOs.Orders;
using MiniECommerce.Api.Models;

namespace MiniECommerce.Api.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(int userId, CreateOrderDto dto);
    Task<List<OrderDto>> GetOrdersForUserAsync(int userId);
    Task<OrderDto> GetOrderByIdAsync(int userId, int orderId, bool isAdmin);
    Task<OrderDto> CancelOrderAsync(int userId, int orderId, bool isAdmin);
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly ICouponService _couponService;

    public OrderService(AppDbContext db, ICouponService couponService)
    {
        _db = db;
        _couponService = couponService;
    }

    public async Task<OrderDto> CreateOrderAsync(int userId, CreateOrderDto dto)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || cart.Items.Count == 0)
        {
            throw new BadRequestException("Sepetinizde ürün bulunmamaktadır.");
        }

        await using IDbContextTransaction transaction = await _db.Database.BeginTransactionAsync();

        var orderItems = new List<OrderItem>();
        decimal subTotal = 0m;

        foreach (var cartItem in cart.Items)
        {
            var product = cartItem.Product ?? throw new NotFoundException(nameof(Product), cartItem.ProductId);

            // Veritabanı seviyesinde atomik stok düşüşü (concurrency & race-condition koruması)
            var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE Products
                SET StockQuantity = StockQuantity - {cartItem.Quantity}
                WHERE Id = {product.Id} AND StockQuantity >= {cartItem.Quantity}");

            if (affectedRows == 0)
            {
                var currentStock = await _db.Products
                    .AsNoTracking()
                    .Where(p => p.Id == product.Id)
                    .Select(p => p.StockQuantity)
                    .FirstOrDefaultAsync();

                throw new InsufficientStockException(product.Name, cartItem.Quantity, currentStock);
            }

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = cartItem.Quantity
            });

            subTotal += product.Price * cartItem.Quantity;
        }

        decimal discountAmount = 0m;
        Coupon? appliedCoupon = null;

        if (!string.IsNullOrWhiteSpace(dto.CouponCode))
        {
            var (coupon, discount) = await _couponService.ValidateAndCalculateAsync(dto.CouponCode, subTotal);
            appliedCoupon = coupon;
            discountAmount = discount;
            coupon.TimesUsed += 1;
        }

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Completed,
            SubTotal = subTotal,
            DiscountAmount = discountAmount,
            TotalAmount = subTotal - discountAmount,
            CouponId = appliedCoupon?.Id,
            Items = orderItems
        };

        _db.Orders.Add(order);
        _db.CartItems.RemoveRange(cart.Items);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToDto(order, appliedCoupon?.Code);
    }

    public async Task<List<OrderDto>> GetOrdersForUserAsync(int userId)
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Coupon)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return orders.Select(o => ToDto(o, o.Coupon?.Code)).ToList();
    }

    public async Task<OrderDto> GetOrderByIdAsync(int userId, int orderId, bool isAdmin)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Coupon)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException(nameof(Order), orderId);

        if (!isAdmin && order.UserId != userId)
        {
            throw new NotFoundException(nameof(Order), orderId);
        }

        return ToDto(order, order.Coupon?.Code);
    }

    public async Task<OrderDto> CancelOrderAsync(int userId, int orderId, bool isAdmin)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Coupon)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException(nameof(Order), orderId);

        if (!isAdmin && order.UserId != userId)
        {
            throw new NotFoundException(nameof(Order), orderId);
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new BadRequestException("Bu sipariş zaten iptal edilmiş.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        foreach (var item in order.Items)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE Products
                SET StockQuantity = StockQuantity + {item.Quantity}
                WHERE Id = {item.ProductId}");
        }

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToDto(order, order.Coupon?.Code);
    }

    private static OrderDto ToDto(Order order, string? couponCode) => new()
    {
        Id = order.Id,
        OrderDate = order.OrderDate,
        Status = order.Status,
        SubTotal = order.SubTotal,
        DiscountAmount = order.DiscountAmount,
        TotalAmount = order.TotalAmount,
        CouponCode = couponCode,
        Items = order.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList()
    };
}