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
                    i.ColorCode, // ✅
                    i.Unit
                })
            });

            return Ok(result);
        }

        // إضافة سند صرف جديد
        // ✅ تعديل الكنترولر لقبول بيانات الـ productId سواء كانت كـ int أو object
        [HttpPost]
        public async Task<IActionResult> CreateStockOutVoucher([FromBody] StockOutVoucher voucher)
        {
            if (voucher == null || voucher.Items == null || !voucher.Items.Any())
            {
                return BadRequest(new { message = "⚠️ بيانات السند غير مكتملة!", voucher });
            }

            foreach (var item in voucher.Items)
            {
                if (item.ProductId == 0)
                {
                    return BadRequest(new { message = "⚠️ المنتج غير معرف بشكل صحيح!", productId = item.ProductId });
                }
            }

            var stockOutVoucher = new StockOutVoucher
            {
                TransferDate = voucher.TransferDate,
                WarehouseKeeperName = voucher.WarehouseKeeperName,
                OperatingOrder = voucher.OperatingOrder,
                Notes = voucher.Notes,
                CustomerId = voucher.CustomerId,
                Items = voucher.Items.Select(i => new StockOutVoucherItem
                {
                    ProductId = i.ProductId,
                    CustomerId = voucher.CustomerId,
                    WarehouseId = i.WarehouseId,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    Unit = i.Unit,
                    Tax = i.Tax,
                    Discount = i.Discount,
                    ColorCode = i.ColorCode
                }).ToList()
            };

            _context.StockOutVouchers.Add(stockOutVoucher);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStockOutVoucherById), new { id = stockOutVoucher.Id }, stockOutVoucher);
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
