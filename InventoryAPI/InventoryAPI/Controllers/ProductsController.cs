using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InventoryAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(InventoryDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponseDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.SubCategory)
                .Include(p => p.Warehouse)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            var dto = new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Code = product.Code,
                Description = product.Description,
                Unit = product.Unit,
                ColorCode = product.ColorCode,
                SubCategoryId = product.SubCategoryId,
                WarehouseId = product.WarehouseId,
                Quantity = product.Quantity,

                // ✅ حد الطلب
                ReorderPoint = product.ReorderPoint,

                ImageUrl = string.IsNullOrEmpty(product.ImagePath)
                    ? null
                    : $"{Request.Scheme}://{Request.Host}/{product.ImagePath.Replace("\\", "/")}",
                SubCategoryName = product.SubCategory?.Name,
                WarehouseName = product.Warehouse?.Name
            };

            return Ok(dto);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProductsByCategoryAndSubCategory(int? categoryId = null, int? subCategoryId = null)
        {
            var query = _context.Products
                .Include(p => p.SubCategory)
                .Include(p => p.Warehouse)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.SubCategory!.CategoryId == categoryId.Value);
            }

            if (subCategoryId.HasValue)
            {
                query = query.Where(p => p.SubCategoryId == subCategoryId.Value);
            }

            var products = await query.ToListAsync();

            return Ok(products.Select(product => new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Code = product.Code,
                Description = product.Description,
                Unit = product.Unit,
                ColorCode = product.ColorCode,
                SubCategoryId = product.SubCategoryId,
                WarehouseId = product.WarehouseId,
                Quantity = product.Quantity,

                // ✅ حد الطلب
                ReorderPoint = product.ReorderPoint,

                ImageUrl = string.IsNullOrEmpty(product.ImagePath)
                    ? null
                    : $"{Request.Scheme}://{Request.Host}/{product.ImagePath.Replace("\\", "/")}",
                SubCategoryName = product.SubCategory?.Name,
                WarehouseName = product.Warehouse?.Name
            }));
        }

        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> UploadProduct([FromForm] ProductUploadDto dto)
        {
            try
            {
                var newProduct = JsonSerializer.Deserialize<Product>(dto.Product, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (newProduct == null || newProduct.SubCategoryId == 0 || newProduct.WarehouseId == 0)
                    return BadRequest("⚠️ بيانات المنتج غير مكتملة");

                if (dto.Image != null && dto.Image.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "Uploads");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{dto.Image.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.Image.CopyToAsync(stream);
                    }

                    newProduct.ImagePath = Path.Combine("Uploads", uniqueFileName);
                }

                // زي ما انت عامل: المنتج الجديد يبدأ بكمية صفر
                newProduct.Quantity = 0;

                // ✅ حماية: لو حد الطلب جاي سالب نخليه صفر
                if (newProduct.ReorderPoint < 0) newProduct.ReorderPoint = 0;

                _context.Products.Add(newProduct);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProduct), new { id = newProduct.Id }, newProduct);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ خطأ أثناء إضافة المنتج: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
