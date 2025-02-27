using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InventoryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockInVoucherController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public StockInVoucherController(InventoryDbContext context)
        {
            _context = context;
        }
        // GET: api/StockInVoucher
        [HttpGet]
        public async Task<IActionResult> GetStockInVouchers()
        {
            // جلب السندات مع العلاقات
            var vouchers = await _context.StockInVouchers
                .AsNoTracking()
                .Include(v => v.Items).ThenInclude(i => i.Product)
                .Include(v => v.Items).ThenInclude(i => i.Supplier)
                .Include(v => v.Items).ThenInclude(i => i.Warehouse)
                .ToListAsync();

            // نقوم بتسطيح البيانات حتى لا تُعيد مرجعيات $id
            var result = vouchers.Select(v => new
            {
                v.Id,
                v.TransferDate,
                v.WarehouseKeeperName,
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
                    i.SupplierId,
                    Supplier = i.Supplier == null ? null : new
                    {
                        i.Supplier.Id,
                        i.Supplier.Name
                    },
                    i.WarehouseId,
                    Warehouse = i.Warehouse == null ? null : new
                    {
                        i.Warehouse.Id,
                        i.Warehouse.Name
                    },
                    i.Quantity,
                    i.Price,
                    i.Tax,
                    i.Discount,
                    i.TotalCost
                })
            });

            return Ok(result);
        }

        // POST: api/StockInVoucher
        [HttpPost]
        public async Task<ActionResult<StockInVoucher>> CreateStockInVoucher([FromBody] StockInVoucher voucher)
        {
            if (voucher == null || voucher.Items == null || voucher.Items.Count == 0)
            {
                return BadRequest(new { message = "⚠️ بيانات السند غير مكتملة!" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // إذا كان رقم السند يتم توليده تلقائيًا فلا حاجة لتعيينه يدويًا.
                // مثال: voucher.Id = (await _context.StockInVouchers.MaxAsync(s => (int?)s.Id) ?? 0) + 1;

                foreach (var item in voucher.Items)
                {
                    if (item.Quantity <= 0)
                    {
                        return BadRequest($"⚠️ البيانات غير صحيحة للمنتج {item.ProductId}");
                    }

                    // التأكد من وجود الكيانات المطلوبة
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                        return BadRequest($"⚠️ المنتج ذو المعرف {item.ProductId} غير موجود.");

                    var supplier = await _context.Suppliers.FindAsync(item.SupplierId);
                    if (supplier == null)
                        return BadRequest($"⚠️ المورد ذو المعرف {item.SupplierId} غير موجود.");

                    var warehouse = await _context.Warehouses.FindAsync(item.WarehouseId);
                    if (warehouse == null)
                        return BadRequest($"⚠️ المستودع ذو المعرف {item.WarehouseId} غير موجود.");

                    // تعيين السعر والضرائب والخصم إلى 0 لأن العميل لا يُرسلها
                    item.Price = 0;
                    item.Tax = 0;
                    item.Discount = 0;

                    // تحديث كمية المخزون في المستودع
                    var warehouseStock = await _context.WarehouseStocks
                        .FirstOrDefaultAsync(ws => ws.WarehouseId == item.WarehouseId && ws.ProductId == item.ProductId);

                    if (warehouseStock != null)
                        warehouseStock.Quantity += item.Quantity;
                    else
                    {
                        _context.WarehouseStocks.Add(new WarehouseStock
                        {
                            WarehouseId = item.WarehouseId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity
                        });
                    }

                    // إسناد الكائنات الملاحيّة (Navigation Properties) على جانب الخادم
                    item.Product = product;
                    item.Supplier = supplier;
                    item.Warehouse = warehouse;
                    item.StockInVoucher = voucher;
                }

                _context.StockInVouchers.Add(voucher);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetStockInVouchers), new { id = voucher.Id }, voucher);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"حدث خطأ داخلي: {ex.Message}");
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStockInVoucherById(int id)
        {
            var voucher = await _context.StockInVouchers
                .AsNoTracking()
                .Include(v => v.Items).ThenInclude(i => i.Product)
                .Include(v => v.Items).ThenInclude(i => i.Supplier)
                .Include(v => v.Items).ThenInclude(i => i.Warehouse)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (voucher == null)
                return NotFound();

            // نفس تسطيح البيانات
            var result = new
            {
                voucher.Id,
                voucher.TransferDate,
                voucher.WarehouseKeeperName,
                voucher.Notes,
                Items = voucher.Items.Select(i => new
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
                    i.SupplierId,
                    Supplier = i.Supplier == null ? null : new
                    {
                        i.Supplier.Id,
                        i.Supplier.Name
                    },
                    i.WarehouseId,
                    Warehouse = i.Warehouse == null ? null : new
                    {
                        i.Warehouse.Id,
                        i.Warehouse.Name
                    },
                    i.Quantity,
                    i.Price,
                    i.Tax,
                    i.Discount,
                    i.TotalCost
                })
            };

            return Ok(result);
        }

        [HttpGet("next-id")]
        public async Task<ActionResult<int>> GetNextVoucherId()
        {
            int nextId = await _context.StockInVouchers
                .OrderByDescending(s => s.Id)
                .Select(s => s.Id)
                .FirstOrDefaultAsync() + 1;

            return Ok(nextId);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStockInVoucher(int id)
        {
            var voucher = await _context.StockInVouchers
                .Include(v => v.Items)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (voucher == null)
            {
                return NotFound();
            }

            // حذف البنود الخاصة بالسند أولاً
            _context.StockInVoucherItems.RemoveRange(voucher.Items);
            // ثم حذف السند
            _context.StockInVouchers.Remove(voucher);

            await _context.SaveChangesAsync();
            return NoContent(); // 204 No Content
        }

    }
}
