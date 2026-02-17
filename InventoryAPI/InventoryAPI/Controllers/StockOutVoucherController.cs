// Controllers/StockOutVoucherController.cs
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
    [ApiController]
    [Route("api/[controller]")]
    public class StockOutVoucherController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        public StockOutVoucherController(InventoryDbContext context)
            => _context = context;

        [HttpGet("next-id")]
        public async Task<ActionResult<int>> GetNextId()
        {
            var max = await _context.StockOutVouchers.MaxAsync(v => (int?)v.Id) ?? 0;
            return Ok(max + 1);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var vs = await _context.StockOutVouchers
                .AsNoTracking()
                .Include(v => v.Customer)
                .Include(v => v.Items).ThenInclude(i => i.Product)
                .Include(v => v.Items).ThenInclude(i => i.Warehouse)
                .ToListAsync();

            var dto = vs.Select(v => new
            {
                id = v.Id,
                transferDate = v.TransferDate,
                customerName = v.Customer?.Name,
                warehouseKeeper = v.WarehouseKeeperName,
                operatingOrder = v.OperatingOrder,
                notes = v.Notes,
                items = v.Items.Select(i => new
                {
                    id = i.Id,
                    productCode = i.Product?.Code,
                    productName = i.Product?.Name,
                    unit = i.Product?.Unit,
                    quantity = i.Quantity,
                    fromWarehouse = i.Warehouse?.Name,
                    price = i.Price,
                    totalCost = i.TotalCost
                })
            });

            return Ok(dto);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var v = await _context.StockOutVouchers
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.Items).ThenInclude(i => i.Product)
                .Include(x => x.Items).ThenInclude(i => i.Warehouse)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (v == null) return NotFound();

            var dto = new
            {
                id = v.Id,
                transferDate = v.TransferDate,
                customerName = v.Customer?.Name,
                warehouseKeeper = v.WarehouseKeeperName,
                operatingOrder = v.OperatingOrder,
                notes = v.Notes,
                items = v.Items.Select(i => new
                {
                    id = i.Id,
                    productCode = i.Product?.Code,
                    productName = i.Product?.Name,
                    unit = i.Product?.Unit,
                    quantity = i.Quantity,
                    fromWarehouse = i.Warehouse?.Name,
                    price = i.Price,
                    totalCost = i.TotalCost
                })
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockOutVoucher dto)
        {
            if (dto?.Items == null || !dto.Items.Any())
                return BadRequest("⚠️ بيانات السند غير مكتملة!");

            if (!await _context.Customers.AnyAsync(c => c.Id == dto.CustomerId))
                return BadRequest("❌ العميل غير موجود.");

            dto.TransferDate = dto.TransferDate == default ? DateTime.UtcNow : dto.TransferDate;

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in dto.Items)
                {
                    if (item.Quantity <= 0)
                        return BadRequest($"⚠️ الكمية غير صحيحة للصنف {item.ProductId}");

                    var ws = await _context.WarehouseStocks
                        .FirstOrDefaultAsync(s => s.WarehouseId == item.WarehouseId && s.ProductId == item.ProductId);

                    if (ws == null || ws.Quantity < item.Quantity)
                        return BadRequest($"⚠️ رصيد غير كافٍ للصنف {item.ProductId}");

                    // FIFO: دفعات الإضافة الأقدم أولاً
                    var batchList = await _context.StockInVoucherItems
                        .Include(b => b.StockInVoucher)
                        .Where(b =>
                            b.ProductId == item.ProductId &&
                            b.WarehouseId == item.WarehouseId &&
                            b.RemainingQuantity > 0)
                        .OrderBy(b => b.StockInVoucher.TransferDate)
                        .ThenBy(b => b.Id)
                        .ToListAsync();

                    decimal totalCost = 0m;
                    int qtyNeeded = Convert.ToInt32(item.Quantity);

                    foreach (var b in batchList)
                    {
                        if (qtyNeeded <= 0) break;

                        int available = Convert.ToInt32(b.RemainingQuantity);
                        if (available <= 0) continue;

                        int take = Math.Min(available, qtyNeeded);

                        totalCost += (decimal)take * b.Price;

                        b.RemainingQuantity -= take;
                        qtyNeeded -= take;
                    }

                    if (qtyNeeded > 0)
                        return BadRequest($"⚠️ رصيد غير كافٍ (FIFO) للصنف {item.ProductId}");

                    item.TotalCost = totalCost;
                    item.Price = item.Quantity > 0
                        ? totalCost / Convert.ToDecimal(item.Quantity)
                        : 0m;

                    item.CustomerId = dto.CustomerId;
                }

                _context.StockOutVouchers.Add(dto);
                await _context.SaveChangesAsync();

                foreach (var item in dto.Items)
                {
                    var ws = await _context.WarehouseStocks
                        .FirstOrDefaultAsync(s => s.WarehouseId == item.WarehouseId && s.ProductId == item.ProductId);

                    ws.Quantity -= Convert.ToInt32(item.Quantity);
                    if (ws.Quantity < 0) ws.Quantity = 0;
                    _context.WarehouseStocks.Update(ws);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"❌ خطأ داخلي: {ex.Message}");
            }
        }
    }
}
