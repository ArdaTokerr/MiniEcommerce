using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniECommerce.Api.DTOs.Common;
using MiniECommerce.Api.DTOs.Products;
using MiniECommerce.Api.Services;

namespace MiniECommerce.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Lists products with optional category/price filtering, free-text search,
    /// sorting and pagination. Example:
    /// GET /api/products?categoryId=2&amp;minPrice=100&amp;maxPrice=1000&amp;page=1&amp;pageSize=10&amp;sortBy=price_asc
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> Get([FromQuery] ProductQueryParameters query)
    {
        return Ok(await _productService.GetAsync(query));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        return Ok(await _productService.GetByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Create(ProductCreateDto dto)
    {
        var created = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Update(int id, ProductUpdateDto dto)
    {
        return Ok(await _productService.UpdateAsync(id, dto));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteAsync(id);
        return NoContent();
    }
}
