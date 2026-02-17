// Controllers/StockInVoucherController.cs
using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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

        // =========================
        // DTOs (accept Basma supplier codes safely)
        // =========================
        public class StockInVoucherCreateDto
        {
            public int? SequenceNumber { get; set; } // ignored (server assigns next)
            [Required]
            public DateTime TransferDate { get; set; }
            public string? WarehouseKeeperName { get; set; }
            public string? OperatingOrder { get; set; }
            public string? Notes { get; set; }

            [Required]
            public List<StockInVoucherItemCreateDto> Items { get; set; } = new();
        }

        public class StockInVoucherItemCreateDto
        {
            [Required]
            public int ProductId { get; set; }

            // SupplierId in DB (int). Optional because UI now selects Basma supplier code.
            public int? SupplierId { get; set; }

            // Basma supplier info (safe as string)
            public string? SupplierCode { get; set; }
            public string? SupplierName { get; set; }

            [Required]
            public int WarehouseId { get; set; }

            [Required]
            public int Quantity { get; set; }

            // price may come 0 and FIFO will compute it
            public decimal Price { get; set; }

            public decimal Tax { get; set; }
            public decimal Discount { get; set; }
            public decimal TotalCost { get; set; }

            public string? Unit { get; set; }
            public string? ColorCode { get; set; }
        }

        // =========================
        // GET next sequence
        // =========================
        [HttpGet("next-id")]
        public async Task<ActionResult<int>> GetNextId()
        {
            var maxSeq = await _context.StockInVouchers.MaxAsync(v => (int?)v.SequenceNumber) ?? 0;
            return Ok(maxSeq + 1);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var vouchers = await _context.StockInVouchers
                .AsNoTracking()
                .Include(v => v.Items).ThenInclude(i => i.Product)
                .Include(v => v.Items).ThenInclude(i => i.Supplier)
                .Include(v => v.Items).ThenInclude(i => i.Warehouse)
                .OrderBy(v => v.SequenceNumber)
                .ToListAsync();

            var result = vouchers.Select(v => new
            {
                v.Id,
                v.SequenceNumber,
                v.TransferDate,
                v.WarehouseKeeperName,
                v.OperatingOrder,
                v.Notes,
                Items = v.Items.Select(i => new
                {
                    i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product != null ? i.Product.Name : null,
                    ProductCode = i.Product != null ? i.Product.Code : null,
                    i.SupplierId,
                    SupplierName = i.Supplier != null ? i.Supplier.Name : null,
                    i.WarehouseId,
                    WarehouseName = i.Warehouse != null ? i.Warehouse.Name : null,
                    i.Quantity,
                    i.Price,
                    i.TotalCost,
                    i.Unit,
                    i.ColorCode
                })
            });

            return Ok(result);
        }

        [HttpGet("{seq:int}")]
        public async Task<IActionResult> GetBySeq(int seq)
        {
            var v = await _context.StockInVouchers
                .AsNoTracking()
                .Include(x => x.Items).ThenInclude(i => i.Product)
                .Include(x => x.Items).ThenInclude(i => i.Supplier)
                .Include(x => x.Items).ThenInclude(i => i.Warehouse)
                .FirstOrDefaultAsync(x => x.SequenceNumber == seq);

            if (v == null) return NotFound();

            var result = new
            {
                v.Id,
                v.SequenceNumber,
                v.TransferDate,
                v.WarehouseKeeperName,
                v.OperatingOrder,
                v.Notes,
                Items = v.Items.Select(i => new
                {
                    i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product != null ? i.Product.Name : null,
                    ProductCode = i.Product != null ? i.Product.Code : null,
                    i.SupplierId,
                    SupplierName = i.Supplier != null ? i.Supplier.Name : null,
                    i.WarehouseId,
                    WarehouseName = i.Warehouse != null ? i.Warehouse.Name : null,
                    i.Quantity,
                    i.Price,
                    i.TotalCost,
                    i.Unit,
                    i.ColorCode
                })
            };

            return Ok(result);
        }

        // ✅ FIFO Price based on PurchaseInvoiceItems (oldest invoice first) - Supplier NOT important
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockInVoucherCreateDto dto)
        {
            if (dto?.Items == null || dto.Items.Count == 0)
                return BadRequest("⚠️ بيانات السند غير مكتملة!");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Assign next sequence number on server (ignore client)
                var maxSeq = await _context.StockInVouchers.MaxAsync(v => (int?)v.SequenceNumber) ?? 0;
                var entity = new StockInVoucher
                {
                    SequenceNumber = maxSeq + 1,
                    TransferDate = dto.TransferDate,
                    WarehouseKeeperName = dto.WarehouseKeeperName,
                    OperatingOrder = dto.OperatingOrder,
                    Notes = dto.Notes,
                    Items = new List<StockInVoucherItem>()
                };

                // Resolve supplier ids + build base items
                var baseItems = new List<StockInVoucherItem>();

                foreach (var it in dto.Items)
                {
                    if (it.Quantity <= 0)
                        return BadRequest($"⚠️ الكمية غير صحيحة للصنف {it.ProductId}");

                    // Resolve SupplierId:
                    int supplierId;
                    if (it.SupplierId.HasValue && it.SupplierId.Value > 0)
                    {
                        supplierId = it.SupplierId.Value;
                    }
                    else
                    {
                        var name = (it.SupplierName ?? "").Trim();
                        if (string.IsNullOrWhiteSpace(name))
                            return BadRequest("⚠️ المورد مطلوب لكل بند.");

                        // Find by Name (safe even if Supplier has no Code field)
                        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Name == name);
                        if (supplier == null)
                        {
                            supplier = new Supplier { Name = name };
                            _context.Suppliers.Add(supplier);
                            await _context.SaveChangesAsync();
                        }
                        supplierId = supplier.Id;
                    }

                    baseItems.Add(new StockInVoucherItem
                    {
                        ProductId = it.ProductId,
                        SupplierId = supplierId,
                        WarehouseId = it.WarehouseId,
                        Quantity = it.Quantity,
                        Price = it.Price,
                        Tax = it.Tax,
                        Discount = it.Discount,
                        TotalCost = it.TotalCost,
                        RemainingQuantity = it.Quantity,
                        Unit = it.Unit ?? "حبة",
                        ColorCode = it.ColorCode ?? ""
                    });
                }

                // FIFO expansion
                var expandedItems = new List<StockInVoucherItem>();

                foreach (var it in baseItems)
                {
                    int qtyToAdd = it.Quantity;
                    decimal manualPrice = it.Price > 0 ? it.Price : 0m;

                    var purchaseBatches = await _context.PurchaseInvoiceItems
                        .AsNoTracking()
                        .Where(pi => pi.ProductId == it.ProductId && pi.PurchaseInvoice != null)
                        .Include(pi => pi.PurchaseInvoice)
                        .OrderBy(pi => pi.PurchaseInvoice.InvoiceDate)
                        .ThenBy(pi => pi.Id)
                        .Select(pi => new
                        {
                            pi.Id,
                            pi.Price,
                            Qty = pi.Quantity,
                            InvoiceDate = pi.PurchaseInvoice.InvoiceDate
                        })
                        .ToListAsync();

                    int alreadyStockedTotal = await _context.StockInVoucherItems
                        .AsNoTracking()
                        .Where(si => si.ProductId == it.ProductId)
                        .SumAsync(si => (int?)si.Quantity) ?? 0;

                    if (purchaseBatches.Count == 0)
                    {
                        expandedItems.Add(CloneItemForInsert(it, qtyToAdd, manualPrice));
                        continue;
                    }

                    int remaining = qtyToAdd;

                    foreach (var b in purchaseBatches)
                    {
                        if (remaining <= 0) break;

                        int batchQty = Convert.ToInt32(b.Qty);

                        int consumedFromThisBatch = Math.Min(batchQty, alreadyStockedTotal);
                        alreadyStockedTotal -= consumedFromThisBatch;

                        int available = batchQty - consumedFromThisBatch;
                        if (available <= 0) continue;

                        int take = Math.Min(available, remaining);
                        if (take <= 0) continue;

                        expandedItems.Add(CloneItemForInsert(it, take, b.Price));
                        remaining -= take;
                    }

                    if (remaining > 0)
                    {
                        var lastPrice = purchaseBatches.Last().Price;
                        var fallback = manualPrice > 0 ? manualPrice : lastPrice;
                        expandedItems.Add(CloneItemForInsert(it, remaining, fallback));
                    }
                }

                entity.Items = expandedItems;

                foreach (var it in entity.Items)
                {
                    it.TotalCost = it.Price * Convert.ToDecimal(it.Quantity);
                    it.RemainingQuantity = it.Quantity;

                    it.Product = null;
                    it.Supplier = null;
                    it.Warehouse = null;
                }

                _context.StockInVouchers.Add(entity);
                await _context.SaveChangesAsync();

                foreach (var it in entity.Items)
                {
                    var stock = await _context.WarehouseStocks
                        .FirstOrDefaultAsync(ws => ws.WarehouseId == it.WarehouseId && ws.ProductId == it.ProductId);

                    if (stock != null)
                        stock.Quantity += it.Quantity;
                    else
                        _context.WarehouseStocks.Add(new WarehouseStock
                        {
                            WarehouseId = it.WarehouseId,
                            ProductId = it.ProductId,
                            Quantity = it.Quantity
                        });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return CreatedAtAction(nameof(GetBySeq), new { seq = entity.SequenceNumber }, new { entity.SequenceNumber });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"❌ خطأ داخلي: {ex.Message}");
            }
        }

        private static StockInVoucherItem CloneItemForInsert(StockInVoucherItem source, int quantity, decimal price)
        {
            return new StockInVoucherItem
            {
                ProductId = source.ProductId,
                SupplierId = source.SupplierId,
                WarehouseId = source.WarehouseId,
                Unit = source.Unit,
                ColorCode = source.ColorCode,
                Quantity = quantity,
                Price = price,
                TotalCost = price * quantity,
                RemainingQuantity = quantity
            };
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            var voucher = await _context.StockInVouchers
                .Include(v => v.Items)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (voucher == null) return NotFound();

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in voucher.Items)
                {
                    var stock = await _context.WarehouseStocks
                        .FirstOrDefaultAsync(ws => ws.WarehouseId == item.WarehouseId && ws.ProductId == item.ProductId);

                    if (stock != null)
                    {
                        stock.Quantity -= item.Quantity;
                        if (stock.Quantity < 0) stock.Quantity = 0;
                    }
                }

                _context.StockInVoucherItems.RemoveRange(voucher.Items);
                _context.StockInVouchers.Remove(voucher);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"❌ خطأ أثناء الحذف: {ex.Message}");
            }
        }
    }
}
