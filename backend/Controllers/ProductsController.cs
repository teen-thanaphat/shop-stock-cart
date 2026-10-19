using Backend.Data;
using Backend.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    // GET api/products — รายการสินค้าพร้อมยอด Stock
    [HttpGet]
    public async Task<List<ProductDto>> GetAll() =>
        await db.Products
            .OrderBy(p => p.ProductCode)
            .Select(p => new ProductDto(
                p.ProductCode,
                p.Name,
                p.UnitPrice,
                p.Stock!.Quantity,
                p.Stock.Reserved,
                p.Stock.Quantity - p.Stock.Reserved))
            .ToListAsync();
}
