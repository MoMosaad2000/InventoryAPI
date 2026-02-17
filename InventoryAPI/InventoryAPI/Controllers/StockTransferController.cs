// Controllers/StockTransferController.cs
using System;
using System.Linq;
using System.Threading.Tasks;
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
    public class StockTransferController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        public StockTransferController(InventoryDbContext context) => _context = context;

        // GET all
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // جلب التحويلات مع بنودها ومنتجاتها ومستودعاتها
            var transfers = await _context.StockTransfers
                .AsNoTracking()
                .Include(t => t.Items).ThenInclude(i => i.Product)
                .Include(t => t.Items).ThenInclude(i => i.Warehouse)
                .ToListAsync();

            // قاموس أسماء المستودعات لتسهيل الـ flatten
            var whNames = await _context.Warehouses
                .AsNoTracking()
                .ToDictionaryAsync(w => w.Id, w => w.Name);

            var result = transfers.Select(t => new
            {
                t.Id,
                FromWarehouseId = t.FromWarehouseId,
                FromWarehouseName = whNames.GetValueOrDefault(t.FromWarehouseId, ""),
                ToWarehouseId = t.ToWarehouseId,
                ToWarehouseName = whNames.GetValueOrDefault(t.ToWarehouseId, ""),
                t.WarehouseKeeperName,
                t.TransferDate,
                t.OperatingOrder,
                t.Notes,
                Items = t.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    ProductName = i.Product?.Name,
                    ProductCode = i.Product?.Code,
                    i.Unit,
                    i.Quantity,
                    i.Price,
                    TotalCost = i.Price * i.Quantity,
                    i.ColorCode
                })
            });

            return Ok(result);
        }

        // GET by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var t = await _context.StockTransfers
                .AsNoTracking()
                .Include(t => t.Items).ThenInclude(i => i.Product)
                .Include(t => t.Items).ThenInclude(i => i.Warehouse)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (t == null) return NotFound();

            var whNames = await _context.Warehouses
                .AsNoTracking()
                .ToDictionaryAsync(w => w.Id, w => w.Name);

            var result = new
            {
                t.Id,
                FromWarehouseId = t.FromWarehouseId,
                FromWarehouseName = whNames.GetValueOrDefault(t.FromWarehouseId, ""),
                ToWarehouseId = t.ToWarehouseId,
                ToWarehouseName = whNames.GetValueOrDefault(t.ToWarehouseId, ""),
                t.WarehouseKeeperName,
                t.TransferDate,
                t.OperatingOrder,
                t.Notes,
                Items = t.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    ProductName = i.Product?.Name,
                    ProductCode = i.Product?.Code,
                    i.Unit,
                    i.Quantity,
                    i.Price,
                    TotalCost = i.Price * i.Quantity,
                    i.ColorCode
                })
            };

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockTransfer dto)
        {
            if (dto?.Items == null || !dto.Items.Any())
                return BadRequest("⚠️ بيانات السند غير مكتملة!");
            if (dto.FromWarehouseId == dto.ToWarehouseId)
                return BadRequest("❌ لا يمكن التحويل لنفس المستودع.");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var alreadyDeductedFallback = new HashSet<int>();

                foreach (var item in dto.Items)
                {
                    if (item.Quantity <= 0)
                        return BadRequest($"⚠️ الكمية غير صحيحة للمنتج {item.ProductId}");

                    // 1) جلب دفعات FIFO من المستودع المصدر
                    var batches = await _context.StockInVoucherItems
                        .Where(i =>
                            i.ProductId == item.ProductId &&
                            i.WarehouseId == dto.FromWarehouseId &&
                            i.RemainingQuantity > 0)
                        .OrderBy(i => i.StockInVoucher.TransferDate)
                        .ThenBy(i => i.Id)
                        .ToListAsync();

                    var totalAvailableBatches = batches.Sum(b => b.RemainingQuantity);

                    if (totalAvailableBatches >= item.Quantity)
                    {
                        // FIFO عادي
                        var toConsume = item.Quantity;
                        decimal usedPrice = 0m;

                        foreach (var b in batches)
                        {
                            if (toConsume == 0) break;
                            var take = Math.Min(b.RemainingQuantity, toConsume);
                            b.RemainingQuantity -= take;
                            toConsume -= take;
                            if (usedPrice == 0m) usedPrice = b.Price;
                            _context.StockInVoucherItems.Update(b);
                        }

                        if (usedPrice <= 0)
                        {
                            var lastPur = await _context.PurchaseInvoiceItems
                                .Where(pi => pi.ProductId == item.ProductId)
                                .OrderByDescending(pi => pi.PurchaseInvoice.InvoiceDate)
                                .ThenByDescending(pi => pi.Id)
                                .FirstOrDefaultAsync();
                            if (lastPur != null) usedPrice = lastPur.Price;
                        }

                        item.Price = usedPrice;
                    }
                    else
                    {
                        // fallback → خصم مباشر من WarehouseStocks
                        var stock = await _context.WarehouseStocks
                            .FirstOrDefaultAsync(ws =>
                                ws.WarehouseId == dto.FromWarehouseId &&
                                ws.ProductId == item.ProductId);

                        if (stock == null)
                        {
                            return BadRequest($"⚠️ لا يوجد أي مخزون للمنتج {item.ProductId} في المستودع {dto.FromWarehouseId}");
                        }
                        else if (stock.Quantity < item.Quantity)
                        {
                            return BadRequest($"⚠️ الكمية المتاحة = {stock.Quantity} أقل من المطلوبة = {item.Quantity} للمنتج {item.ProductId}");
                        }

                        // خصم فوري مرة واحدة فقط
                        stock.Quantity -= item.Quantity;
                        alreadyDeductedFallback.Add(item.ProductId);

                        // آخر سعر شراء كـ fallback
                        var lastPur = await _context.PurchaseInvoiceItems
                            .Where(pi => pi.ProductId == item.ProductId)
                            .OrderByDescending(pi => pi.PurchaseInvoice.InvoiceDate)
                            .ThenByDescending(pi => pi.Id)
                            .FirstOrDefaultAsync();

                        item.Price = lastPur?.Price ?? 0m;
                    }

                    // إزالة مراجع دورية
                    item.StockTransfer = null;
                    item.Product = null;
                    item.Warehouse = null;
                }

                // 2) حفظ السند
                _context.StockTransfers.Add(dto);
                await _context.SaveChangesAsync();

                // 3) تحديث WarehouseStocks (مع تجنب الخصم المزدوج في fallback)
                foreach (var item in dto.Items)
                {
                    if (!alreadyDeductedFallback.Contains(item.ProductId))
                    {
                        var fromStock = await _context.WarehouseStocks
                            .FirstOrDefaultAsync(ws =>
                                ws.WarehouseId == dto.FromWarehouseId &&
                                ws.ProductId == item.ProductId);
                        if (fromStock != null)
                            fromStock.Quantity -= item.Quantity;
                    }

                    // إضافة للوجهة
                    var toStock = await _context.WarehouseStocks
                        .FirstOrDefaultAsync(ws =>
                            ws.WarehouseId == dto.ToWarehouseId &&
                            ws.ProductId == item.ProductId);
                    if (toStock != null)
                        toStock.Quantity += item.Quantity;
                    else
                        _context.WarehouseStocks.Add(new WarehouseStock
                        {
                            WarehouseId = dto.ToWarehouseId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity
                        });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                // 4) إرجاع السند الجديد
                var created = await _context.StockTransfers
                    .AsNoTracking()
                    .Include(t => t.Items).ThenInclude(i => i.Product)
                    .Include(t => t.Items).ThenInclude(i => i.Warehouse)
                    .FirstOrDefaultAsync(t => t.Id == dto.Id);

                var whNames = await _context.Warehouses
                    .AsNoTracking()
                    .ToDictionaryAsync(w => w.Id, w => w.Name);

                var result = new
                {
                    created.Id,
                    FromWarehouseId = created.FromWarehouseId,
                    FromWarehouseName = whNames.GetValueOrDefault(created.FromWarehouseId, ""),
                    ToWarehouseId = created.ToWarehouseId,
                    ToWarehouseName = whNames.GetValueOrDefault(created.ToWarehouseId, ""),
                    created.WarehouseKeeperName,
                    created.TransferDate,
                    created.OperatingOrder,
                    created.Notes,
                    Items = created.Items.Select(i => new
                    {
                        i.Id,
                        i.ProductId,
                        ProductName = i.Product?.Name,
                        ProductCode = i.Product?.Code,
                        i.Unit,
                        i.Quantity,
                        i.Price,
                        TotalCost = i.Price * i.Quantity,
                        i.ColorCode
                    })
                };

                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                var inner = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, $"❌ خطأ داخلي: {inner}");
            }
        }


    }
}
