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
    public class StockOutVoucherController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public StockOutVoucherController(InventoryDbContext context)
        {
            _context = context;
        }

        // جلب جميع سندات الصرف
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockOutVoucher>>> GetStockOutVouchers()
        {
            var vouchers = await _context.StockOutVouchers
                .AsNoTracking()
                .Include(v => v.Items).ThenInclude(i => i.Product)
                .Include(v => v.Items).ThenInclude(i => i.Customer)
                .Include(v => v.Items).ThenInclude(i => i.Warehouse)
                .Include(v => v.Customer)
                .ToListAsync();

            var result = vouchers.Select(v => new
            {
                v.Id,
                v.TransferDate,
                v.CustomerId,
                Customer = v.Customer == null ? null : new { v.Customer.Id, v.Customer.Name },
                v.WarehouseKeeperName,
                v.OperatingOrder,
                v.Notes,
                Items = v.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    Product = i.Product == null ? null : new { i.Product.Id, i.Product.Name, i.Product.Code, i.Product.Unit },
                    i.WarehouseId,
                    Warehouse = i.Warehouse == null ? null : new { i.Warehouse.Id, i.Warehouse.Name },
                    i.CustomerId,
                    Customer = i.Customer == null ? null : new { i.Customer.Id, i.Customer.Name },
                    i.Quantity,
                    i.Price,
                    i.Tax,
                    i.Discount,
                    i.ColorCode,
                    i.Unit
                })
            });

            return Ok(result);
        }

        // إضافة سند صرف جديد
        [HttpPost]
        public async Task<IActionResult> CreateStockOutVoucher([FromBody] StockOutVoucher voucher)
        {
            if (voucher == null || voucher.Items == null || !voucher.Items.Any())
                return BadRequest(new { message = "⚠️ بيانات السند غير مكتملة!" });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in voucher.Items)
                {
                    if (item.ProductId == 0)
                        return BadRequest(new { message = "⚠️ المنتج غير معرف بشكل صحيح!", productId = item.ProductId });

                    if (item.Quantity <= 0)
                        return BadRequest(new { message = $"⚠️ الكمية غير صحيحة للمنتج {item.ProductId}" });

                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                        return NotFound(new { message = $"⚠️ المنتج {item.ProductId} غير موجود" });

                    var warehouse = await _context.Warehouses.FindAsync(item.WarehouseId);
                    if (warehouse == null)
                        return NotFound(new { message = $"⚠️ المخزن {item.WarehouseId} غير موجود" });

                    var warehouseStock = await _context.WarehouseStocks
                        .FirstOrDefaultAsync(ws => ws.WarehouseId == item.WarehouseId && ws.ProductId == item.ProductId);

                    if (warehouseStock == null || warehouseStock.Quantity < item.Quantity)
                        return BadRequest(new { message = $"⚠️ الكمية غير متوفرة! الكمية المتاحة: {warehouseStock?.Quantity ?? 0}" });

                    warehouseStock.Quantity -= item.Quantity;
                    item.Product = null;
                    item.Warehouse = null;
                }

                _context.StockOutVouchers.Add(voucher);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetStockOutVoucherById), new { id = voucher.Id }, voucher);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = $"❌ خطأ أثناء الحفظ: {ex.Message}" });
            }
        }

        // جلب رقم السند التالي
        [HttpGet("next-id")]
        public async Task<ActionResult<int>> GetNextStockOutVoucherId()
        {
            if (!await _context.StockOutVouchers.AnyAsync())
                return Ok(1);

            int count = await _context.StockOutVouchers.CountAsync();
            return Ok(count + 1);
        }

        // جلب سند صرف معين
        [HttpGet("{id}")]
        public async Task<ActionResult<StockOutVoucher>> GetStockOutVoucherById(int id)
        {
            var voucher = await _context.StockOutVouchers
                .AsNoTracking()
                .Include(v => v.Items).ThenInclude(i => i.Product)
                .Include(v => v.Items).ThenInclude(i => i.Customer)
                .Include(v => v.Items).ThenInclude(i => i.Warehouse)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (voucher == null)
                return NotFound();

            return Ok(voucher);
        }

        // حذف سند صرف
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStockOutVoucher(int id)
        {
            var voucher = await _context.StockOutVouchers
                 .Include(v => v.Items)
                 .FirstOrDefaultAsync(v => v.Id == id);

            if (voucher == null)
                return NotFound();

            _context.StockOutVoucherItems.RemoveRange(voucher.Items);
            _context.StockOutVouchers.Remove(voucher);

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
