using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Controllers
{
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
                .Include(v => v.Items)
                    .ThenInclude(i => i.Product)
                .Include(v => v.Items)
                    .ThenInclude(i => i.Customer)
                .Include(v => v.Items)
                    .ThenInclude(i => i.Warehouse)
                .Include(v => v.Customer)
                .ToListAsync();

            return Ok(vouchers);
        }

        // إضافة سند صرف جديد
        [HttpPost]
        public async Task<ActionResult<StockOutVoucher>> CreateStockOutVoucher([FromBody] StockOutVoucher voucher)
        {
            if (voucher == null || voucher.Items == null || !voucher.Items.Any())
            {
                return BadRequest(new { message = "⚠️ بيانات السند غير مكتملة!" });
            }

            // إذا كان CustomerId في السند = 0، نأخذه من أول بند
            if (voucher.CustomerId == 0)
            {
                voucher.CustomerId = voucher.Items.First().CustomerId;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // التحقق من وجود العميل في قاعدة البيانات
                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == voucher.CustomerId);

                if (customer == null)
                {
                    return NotFound(new { message = $"⚠️ العميل ذو المعرف {voucher.CustomerId} غير موجود." });
                }

                // التحقق من كل بند في السند
                foreach (var item in voucher.Items)
                {
                    if (item.Quantity <= 0)
                    {
                        return BadRequest(new { message = $"⚠️ البيانات غير صحيحة للمنتج {item.ProductId}" });
                    }

                    var product = await _context.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                    if (product == null)
                    {
                        return NotFound(new { message = $"⚠️ المنتج ذو المعرف {item.ProductId} غير موجود." });
                    }

                    var warehouse = await _context.Warehouses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(w => w.Id == item.WarehouseId);

                    if (warehouse == null)
                    {
                        return NotFound(new { message = $"⚠️ المستودع ذو المعرف {item.WarehouseId} غير موجود." });
                    }

                    var warehouseStock = await _context.WarehouseStocks
                        .FirstOrDefaultAsync(ws => ws.WarehouseId == item.WarehouseId && ws.ProductId == item.ProductId);

                    if (warehouseStock == null || warehouseStock.Quantity < item.Quantity)
                    {
                        return BadRequest(new
                        {
                            message = $"⚠️ الكمية المطلوبة غير متوفرة! الكمية المتاحة: {warehouseStock?.Quantity ?? 0}"
                        });
                    }

                    // إنقاص الكمية من المخزن
                    warehouseStock.Quantity -= item.Quantity;

                    // تعيين نفس العميل على البند
                    item.CustomerId = voucher.CustomerId;

                    // تعطيل الكائنات الملاحية
                    item.Product = null;
                    item.Warehouse = null!;
                    item.StockOutVoucher = voucher;
                }

                _context.StockOutVouchers.Add(voucher);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // إرجاع السند الذي تم إنشاؤه
                return CreatedAtAction(nameof(GetStockOutVoucherById), new { id = voucher.Id }, voucher);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var errorMessage = $"❌ حدث خطأ داخلي: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" - InnerException: {ex.InnerException.Message}";
                }

                return StatusCode(500, new { message = errorMessage });
            }
        }

        // جلب رقم السند التالي
        [HttpGet("next-id")]
        public async Task<ActionResult<int>> GetNextStockOutVoucherId()
        {
            // إذا لم يكن هناك أي سندات في الجدول، نعيد 1
            bool isEmpty = !await _context.StockOutVouchers.AnyAsync();
            if (isEmpty)
            {
                return Ok(1);
            }

            // خلاف ذلك، نعيد أعلى رقم سند + 1
            int maxId = await _context.StockOutVouchers.MaxAsync(v => v.Id);
            int nextId = maxId + 1;

            return Ok(nextId);
        }


        // جلب سند صرف محدد بالمعرف
        [HttpGet("{id}")]
        public async Task<ActionResult<StockOutVoucher>> GetStockOutVoucherById(int id)
        {
            var voucher = await _context.StockOutVouchers
                .AsNoTracking()
                .Include(v => v.Items)
                    .ThenInclude(i => i.Product)
                .Include(v => v.Items)
                    .ThenInclude(i => i.Customer)
                .Include(v => v.Items)
                    .ThenInclude(i => i.Warehouse)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (voucher == null)
            {
                return NotFound();
            }

            return Ok(voucher);
        }

        // حذف سند صرف
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStockOutVoucher(int id)
        {
            var voucher = await _context.StockOutVouchers
                 .Include(st => st.Items)
                 .FirstOrDefaultAsync(st => st.Id == id);

            if (voucher == null)
            {
                return NotFound();
            }

            // حذف البنود المرتبطة بالسند
            _context.StockOutVoucherItems.RemoveRange(voucher.Items);
            // حذف السند نفسه
            _context.StockOutVouchers.Remove(voucher);

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
