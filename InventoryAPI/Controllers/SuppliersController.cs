using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public SuppliersController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: api/suppliers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
        {
            var suppliers = await _context.Suppliers.ToListAsync();
            return Ok(suppliers);
        }

        // GET: api/suppliers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Supplier>> GetSupplier(int id)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null)
            {
                return NotFound();
            }
            return Ok(supplier);
        }

        //get products by supplier id 

        [HttpGet ("{id}/proudcts")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProudctsBySupplier (int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.StockInVouchers)
                .ThenInclude(v => v.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (supplier==null)
            {
                return NotFound();
            }
            var products = supplier.StockInVouchers
                .SelectMany(v => v.Items)
                .Select(i => i.Product)
                .Distinct()
                .ToList();
            return Ok(products);
        }
        // POST: api/suppliers
        [HttpPost]
        public async Task<ActionResult<Supplier>> CreateSupplier(Supplier supplier)
        {
            if (string.IsNullOrEmpty(supplier.Name)||string.IsNullOrEmpty(supplier.ContactInfo))
            {
                return BadRequest("أسم المورد وبيانات الأتصال لا يجب ان تكون فارغة");
            }
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
        }

        // DELETE: api/suppliers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
