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
    public class WarehousesController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public WarehousesController(InventoryDbContext context)
        {
            _context = context;
        }

        // ✅ **جلب جميع المستودعات**
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Warehouse>>> GetWarehouses()
        {
            var warehouses = await _context.Warehouses
                .Include(w => w.Stocks)
                    .ThenInclude(s => s.Product)
                .AsNoTracking() // 🔹 يمنع المشاكل المتعلقة بالمراجع الدائرية
                .ToListAsync();

            return Ok(warehouses);
        }

        // ✅ **جلب مستودع واحد بالـ ID**
        [HttpGet("{id}")]
        public async Task<ActionResult<Warehouse>> GetWarehouse(int id)
        {
            var warehouse = await _context.Warehouses
                .Include(w => w.Stocks)
                .ThenInclude(ws => ws.Product)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (warehouse == null)
            {
                return NotFound($"لا يوجد مستودع بالرقم {id}");
            }

            return Ok(warehouse);
        }

        // إضافة مستودع جديد
        [HttpPost]
        public async Task<ActionResult<Warehouse>> CreateWarehouse(Warehouse warehouse)
        {
            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetWarehouses), new { id = warehouse.Id }, warehouse);
        }

        // حذف مستودع
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null) return NotFound();

            _context.Warehouses.Remove(warehouse);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
