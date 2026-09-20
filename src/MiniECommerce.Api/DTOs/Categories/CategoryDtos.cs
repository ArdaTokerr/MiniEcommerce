using System.ComponentModel.DataAnnotations;

namespace MiniECommerce.Api.DTOs.Categories;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public List<CategoryDto> SubCategories { get; set; } = new();
}

public class CategoryCreateDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
}

public class CategoryUpdateDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
}
