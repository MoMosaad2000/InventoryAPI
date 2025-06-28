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
    public class StockInVoucherController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public StockInVoucherController(InventoryDbContext context)
        {
            _context = context;
        }

        // ✅ GET: api/StockInVoucher
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockInVoucher>>> GetStockInVouchers()
        {
            var vouchers = await _context.StockInVouchers
                .AsNoTracking()
                .Include(v => v.Items)
                    .ThenInclude(i => i.Product)
                .Include(v => v.Items)
                    .ThenInclude(i => i.Supplier)
                .Include(v => v.Items)
                    .ThenInclude(i => i.Warehouse)
                .ToListAsync();

            return Ok(vouchers);
        }

        // ✅ POST: api/StockInVoucher
        [HttpPost]
        public async Task<IActionResult> CreateStockInVoucher([FromBody] StockInVoucher voucher)
        {
            if (voucher == null || voucher.Items == null || !voucher.Items.Any())
            {
                return BadRequest(new { message = "⚠️ بيانات السند غير مكتملة!" });
            }

            var stockInVoucher = new StockInVoucher
            {
                TransferDate = voucher.TransferDate,
                WarehouseKeeperName = voucher.WarehouseKeeperName,
                Notes = voucher.Notes,
                OperatingOrder = voucher.OperatingOrder,
                Items = voucher.Items.Select(i => new StockInVoucherItem
                {
                    ProductId = i.ProductId,
                    SupplierId = i.SupplierId,
                    WarehouseId = i.WarehouseId,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    Price = i.Price,
                    Tax = i.Tax,
                    Discount = i.Discount,
                    ColorCode = i.ColorCode
                }).ToList()
            };

            try
            {
                _context.StockInVouchers.Add(stockInVoucher);
                await _context.SaveChangesAsync();
                return Ok(stockInVoucher);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"❌ حدث خطأ أثناء إضافة المخزون: {ex.Message}" });
            }
        }

        // ✅ GET: api/StockInVoucher/next-id
        [HttpGet("next-id")]
        public async Task<ActionResult<int>> GetNextVoucherId()
        {
            bool isEmpty = !await _context.StockInVouchers.AnyAsync();
            if (isEmpty)
                return Ok(1);

            int count = await _context.StockInVouchers.CountAsync();
            return Ok(count + 1);
        }

        // ✅ GET: api/StockInVoucher/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<StockInVoucher>> GetVoucherById(int id)
        {
            var voucher = await _context.StockInVouchers
                .Include(v => v.Items)
                    .ThenInclude(i => i.Product)
                .Include(v => v.Items)
                    .ThenInclude(i => i.Supplier)
                .Include(v => v.Items)
                    .ThenInclude(i => i.Warehouse)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (voucher == null)
                return NotFound();

            return Ok(voucher);
        }

        // ✅ DELETE: api/StockInVoucher/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            var voucher = await _context.StockInVouchers
                .Include(v => v.Items)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (voucher == null)
                return NotFound();

            _context.StockInVoucherItems.RemoveRange(voucher.Items);
            _context.StockInVouchers.Remove(voucher);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
