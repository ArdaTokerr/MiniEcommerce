using Microsoft.EntityFrameworkCore;
using MiniECommerce.Api.Common.Exceptions;
using MiniECommerce.Api.Data;
using MiniECommerce.Api.DTOs.Categories;
using MiniECommerce.Api.Models;

namespace MiniECommerce.Api.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetTreeAsync();
    Task<CategoryDto> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CategoryCreateDto dto);
    Task<CategoryDto> UpdateAsync(int id, CategoryUpdateDto dto);
    Task DeleteAsync(int id);
}

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryDto>> GetTreeAsync()
    {
        var all = await _db.Categories.AsNoTracking().ToListAsync();

        var lookup = all.ToDictionary(c => c.Id, c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            ParentCategoryId = c.ParentCategoryId
        });

        var roots = new List<CategoryDto>();
        foreach (var category in all)
        {
            var dto = lookup[category.Id];
            if (category.ParentCategoryId.HasValue && lookup.TryGetValue(category.ParentCategoryId.Value, out var parentDto))
            {
                parentDto.SubCategories.Add(dto);
            }
            else
            {
                roots.Add(dto);
            }
        }

        return roots;
    }

    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException(nameof(Category), id);

        return new CategoryDto 
        { 
            Id = category.Id, 
            Name = category.Name, 
            ParentCategoryId = category.ParentCategoryId 
        };
    }

    public async Task<CategoryDto> CreateAsync(CategoryCreateDto dto)
    {
        if (dto.ParentCategoryId.HasValue)
        {
            var parentExists = await _db.Categories.AnyAsync(c => c.Id == dto.ParentCategoryId.Value);
            if (!parentExists)
            {
                throw new BadRequestException("Belirtilen üst kategori bulunamadı.");
            }
        }

        var category = new Category 
        { 
            Name = dto.Name.Trim(), 
            ParentCategoryId = dto.ParentCategoryId 
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return new CategoryDto 
        { 
            Id = category.Id, 
            Name = category.Name, 
            ParentCategoryId = category.ParentCategoryId 
        };
    }

    public async Task<CategoryDto> UpdateAsync(int id, CategoryUpdateDto dto)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException(nameof(Category), id);

        if (dto.ParentCategoryId == id)
        {
            throw new BadRequestException("Bir kategori kendisinin üst kategorisi olamaz.");
        }

        category.Name = dto.Name.Trim();
        category.ParentCategoryId = dto.ParentCategoryId;

        await _db.SaveChangesAsync();
        return new CategoryDto 
        { 
            Id = category.Id, 
            Name = category.Name, 
            ParentCategoryId = category.ParentCategoryId 
        };
    }

    public async Task DeleteAsync(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException(nameof(Category), id);

        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
        {
            throw new BadRequestException("İçerisinde ürün bulunan bir kategori silinemez.");
        }

        var hasChildren = await _db.Categories.AnyAsync(c => c.ParentCategoryId == id);
        if (hasChildren)
        {
            throw new BadRequestException("Alt kategorileri bulunan bir kategori silinemez.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
    }
}