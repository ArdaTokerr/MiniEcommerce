using Microsoft.EntityFrameworkCore;
using MiniECommerce.Api.Common.Exceptions;
using MiniECommerce.Api.Data;
using MiniECommerce.Api.DTOs.Cart;
using MiniECommerce.Api.Models;

namespace MiniECommerce.Api.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(int userId);
    Task<CartDto> AddItemAsync(int userId, AddCartItemDto dto);
    Task<CartDto> UpdateItemAsync(int userId, int productId, UpdateCartItemDto dto);
    Task<CartDto> RemoveItemAsync(int userId, int productId);
    Task ClearCartAsync(int userId);
}

public class CartService : ICartService
{
    private readonly AppDbContext _db;

    public CartService(AppDbContext db)
    {
        _db = db;
    }

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart { UserId = userId };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync();
        return cart;
    }

    public async Task<CartDto> GetCartAsync(int userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        return ToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(int userId, AddCartItemDto dto)
    {
        var cart = await GetOrCreateCartAsync(userId);

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId)
            ?? throw new NotFoundException(nameof(Product), dto.ProductId);

        if (!product.IsActive)
        {
            throw new BadRequestException("Ürün satışta değildir.");
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);
        var requestedTotalQuantity = (existingItem?.Quantity ?? 0) + dto.Quantity;

        if (requestedTotalQuantity > product.StockQuantity)
        {
            throw new InsufficientStockException(product.Name, requestedTotalQuantity, product.StockQuantity);
        }

        if (existingItem is not null)
        {
            existingItem.Quantity = requestedTotalQuantity;
        }
        else
        {
            cart.Items.Add(new CartItem 
            { 
                CartId = cart.Id, 
                ProductId = dto.ProductId, 
                Quantity = dto.Quantity 
            });
        }

        await _db.SaveChangesAsync();

        var refreshed = await GetOrCreateCartAsync(userId);
        return ToDto(refreshed);
    }

    public async Task<CartDto> UpdateItemAsync(int userId, int productId, UpdateCartItemDto dto)
    {
        var cart = await GetOrCreateCartAsync(userId);

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new NotFoundException("CartItem", productId);

        if (item.Product is not null && dto.Quantity > item.Product.StockQuantity)
        {
            throw new InsufficientStockException(item.Product.Name, dto.Quantity, item.Product.StockQuantity);
        }

        item.Quantity = dto.Quantity;
        await _db.SaveChangesAsync();

        var refreshed = await GetOrCreateCartAsync(userId);
        return ToDto(refreshed);
    }

    public async Task<CartDto> RemoveItemAsync(int userId, int productId)
    {
        var cart = await GetOrCreateCartAsync(userId);

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new NotFoundException("CartItem", productId);

        cart.Items.Remove(item);
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();

        var refreshed = await GetOrCreateCartAsync(userId);
        return ToDto(refreshed);
    }

    public async Task ClearCartAsync(int userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync();
    }

    private static CartDto ToDto(Cart cart) => new()
    {
        CartId = cart.Id,
        Items = cart.Items.Select(i => new CartItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? string.Empty,
            UnitPrice = i.Product?.Price ?? 0,
            Quantity = i.Quantity,
            AvailableStock = i.Product?.StockQuantity ?? 0
        }).ToList()
    };
}