using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public SuppliersController(InventoryDbContext context)
        {
            _context = context;
        }

        // ✅ Public list for dropdowns (no auth)
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
        {
            var suppliers = await _context.Suppliers.AsNoTracking().ToListAsync();
            return Ok(suppliers);
        }

        // ✅ Public details (no auth)
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<Supplier>> GetSupplier(int id)
        {
            var supplier = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (supplier == null) return NotFound();
            return Ok(supplier);
        }

        // ✅ (Optional) public products-by-supplier (leave public to avoid UI breaking)
        [AllowAnonymous]
        [HttpGet("{id}/proudcts")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProudctsBySupplier(int id)
        {
            var supplier = await _context.Suppliers
                .AsNoTracking()
                .Include(s => s.StockInVouchers)
                .ThenInclude(v => v.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null) return NotFound();

            var products = supplier.StockInVouchers
                .SelectMany(v => v.Items)
                .Select(i => i.Product)
                .Where(p => p != null)
                .Distinct()
                .ToList();

            return Ok(products);
        }

        // 🔒 Keep writes protected
        [HttpPost]
        public async Task<ActionResult<Supplier>> CreateSupplier(Supplier supplier)
        {
            if (string.IsNullOrEmpty(supplier.Name) || string.IsNullOrEmpty(supplier.ContactInfo))
            {
                return BadRequest("أسم المورد وبيانات الأتصال لا يجب ان تكون فارغة");
            }

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
