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

            // مثال تسطيح البيانات
            var result = vouchers.Select(v => new
            {
                v.Id,
                v.TransferDate,
                v.CustomerId,
                Customer = v.Customer == null ? null : new
                {
                    v.Customer.Id,
                    v.Customer.Name
                },
                v.WarehouseKeeperName,
                v.OperatingOrder, // ✅
                v.Notes,
                Items = v.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    Product = i.Product == null ? null : new
                    {
                        i.Product.Id,
                        i.Product.Name,
                        i.Product.Code,
                        i.Product.Unit
                    },
                    i.WarehouseId,
                    Warehouse = i.Warehouse == null ? null : new
                    {
                        i.Warehouse.Id,
                        i.Warehouse.Name
                    },
                    i.CustomerId,
                    Customer = i.Customer == null ? null : new
                    {
                        i.Customer.Id,
                        i.Customer.Name
                    },
                    i.Quantity,
                    i.Price,
                    i.Tax,
                    i.Discount,
                    i.ColorCode // ✅
                })
            });

            return Ok(result);
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
                // التحقق من وجود العميل
                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == voucher.CustomerId);
                if (customer == null)
                {
                    return NotFound(new { message = $"⚠️ العميل ذو المعرف {voucher.CustomerId} غير موجود." });
                }

                // التحقق من كل بند
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
                        return BadRequest(new { message = $"⚠️ الكمية المطلوبة غير متوفرة! الكمية المتاحة: {warehouseStock?.Quantity ?? 0}" });
                    }

                    // إنقاص الكمية من المخزن
                    warehouseStock.Quantity -= item.Quantity;

                    // تعيين نفس العميل على البند
                    item.CustomerId = voucher.CustomerId;

                    // لا نقوم بحذف الحقول الجديدة (OperatingOrder, Notes, ColorCode)
                    // فقط حذف الكائنات الملاحيّة لتفادي مشاكل EF
                    item.Product = null;
                    item.Warehouse = null;
                    item.StockOutVoucher = voucher;
                }

                _context.StockOutVouchers.Add(voucher);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

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
        // GET: api/StockOutVoucher/next-id
        [HttpGet("next-id")]
        public async Task<ActionResult<int>> GetNextStockOutVoucherId()
        {
            // إذا كان الجدول فارغًا، نُعيد 1
            bool isEmpty = !await _context.StockOutVouchers.AnyAsync();
            if (isEmpty)
            {
                return Ok(1);
            }
            // بدلاً من استخدام MaxAsync (الذي يحتفظ بالقيمة السابقة حتى لو تم حذف السجلات)
            int count = await _context.StockOutVouchers.CountAsync();
            return Ok(count + 1);
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

            _context.StockOutVoucherItems.RemoveRange(voucher.Items);
            _context.StockOutVouchers.Remove(voucher);

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
