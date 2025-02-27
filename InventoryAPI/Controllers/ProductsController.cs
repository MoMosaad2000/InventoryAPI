using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public ProductsController(InventoryDbContext context)
        {
            _context = context;
        }

        // ✅ **جلب جميع المنتجات**
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        //{
        //    var products = await _context.Products
        //        .Include(p => p.SubCategory)
        //        .Include(p => p.Warehouse)
        //        .AsNoTracking()
        //        .ToListAsync();

        //    return Ok(products);
        //}

        // ✅ **جلب منتج واحد حسب الـ ID**
        // جلب منتج واحد حسب الـ ID مع بيانات الفئة الفرعية والمستودع (المستودع لن يتم عرضه بفضل [JsonIgnore])
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.SubCategory)
                .Include(p => p.Warehouse)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new { message = "المنتج غير موجود" });
            }

            return Ok(product);
        }
        // جلب المنتجات بحسب الفئة والفئة الفرعية
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategoryAndSubCategory(int? categoryId = null, int? subCategoryId = null)
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
            return Ok(products);
        }

        // إضافة منتج جديد مع إضافته تلقائيًا للمخزون
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            if (product == null)
            {
                return BadRequest(new { message = "بيانات المنتج غير صحيحة" });
            }

            product.Quantity = 0; // تأكيد أن المنتج يبدأ بكمية 0
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // التأكد من وجود المستودع
            var warehouse = await _context.Warehouses.FindAsync(product.WarehouseId);
            if (warehouse == null)
            {
                return BadRequest("المستودع غير موجود.");
            }

            // إضافة المنتج إلى المخزون عند إنشائه
            var warehouseStock = new WarehouseStock
            {
                ProductId = product.Id,
                WarehouseId = product.WarehouseId,
                Quantity = 0,
                Product = product
            };

            _context.WarehouseStocks.Add(warehouseStock);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        // حذف منتج
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
