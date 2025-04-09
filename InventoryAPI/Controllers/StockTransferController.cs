using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockTransferController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public StockTransferController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: api/StockTransfer
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StockTransfer>>> GetStockTransfers()
        {
            var stockTransfers = await _context.StockTransfers
                .AsNoTracking()
                .Include(t => t.Items)
                    .ThenInclude(i => i.Product)
                .Include(t => t.Items)
                    .ThenInclude(i => i.Warehouse)
                .ToListAsync();
            return Ok(stockTransfers);
        }

        // GET: api/StockTransfer/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<StockTransfer>> GetStockTransfer(int id)
        {
            var stockTransfer = await _context.StockTransfers
                .AsNoTracking()
                .Include(t => t.Items)
                    .ThenInclude(i => i.Product)
                .Include(t => t.Items)
                    .ThenInclude(i => i.Warehouse)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (stockTransfer == null)
            {
                return NotFound($"لا يوجد تحويل مخزني بالرقم {id}");
            }

            return Ok(stockTransfer);
        }

        // POST: api/StockTransfer
        [HttpPost]
        public async Task<ActionResult<StockTransfer>> CreateStockTransfer([FromBody] StockTransfer stockTransfer)
        {
            if (stockTransfer == null || stockTransfer.Items == null || !stockTransfer.Items.Any())
            {
                return BadRequest(new { message = "⚠️ بيانات السند غير مكتملة!" });
            }

            // لا نقوم بإزالة الحقول الجديدة؛ فقط إزالة الكائنات الملاحيّة غير الضرورية:
            foreach (var item in stockTransfer.Items)
            {
                item.Product = null;
                item.Warehouse = null;
                item.StockTransfer = stockTransfer;
                item.WarehouseId = stockTransfer.FromWarehouseId;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (stockTransfer.FromWarehouseId == stockTransfer.ToWarehouseId)
                {
                    return BadRequest("لا يمكن التحويل لنفس المستودع");
                }

                // لكل بند، تحقق من الكميات وتحديث المخزون
                foreach (var item in stockTransfer.Items)
                {
                    if (item.Quantity <= 0)
                    {
                        return BadRequest(new { message = $"⚠️ البيانات غير صحيحة للمنتج {item.ProductId}" });
                    }

                    var fromWarehouseStock = await _context.WarehouseStocks
                        .FirstOrDefaultAsync(ws => ws.WarehouseId == stockTransfer.FromWarehouseId && ws.ProductId == item.ProductId);

                    if (fromWarehouseStock == null || fromWarehouseStock.Quantity < item.Quantity)
                    {
                        return BadRequest($"الكمية في المستودع المصدر غير كافية للمنتج {item.ProductId}");
                    }

                    fromWarehouseStock.Quantity -= item.Quantity;

                    var toWarehouseStock = await _context.WarehouseStocks
                        .FirstOrDefaultAsync(ws => ws.WarehouseId == stockTransfer.ToWarehouseId && ws.ProductId == item.ProductId);

                    if (toWarehouseStock != null)
                    {
                        toWarehouseStock.Quantity += item.Quantity;
                    }
                    else
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product == null)
                        {
                            return BadRequest("المنتج غير موجود في قاعدة البيانات.");
                        }
                        _context.WarehouseStocks.Add(new WarehouseStock
                        {
                            WarehouseId = stockTransfer.ToWarehouseId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity
                        });
                    }
                }

                _context.StockTransfers.Add(stockTransfer);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetStockTransfer), new { id = stockTransfer.Id }, stockTransfer);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
                return StatusCode(500, $"حدث خطأ داخلي: {ex.Message} {innerMsg}");
            }
        }

        // GET: api/StockTransfer/next-id
        [HttpGet("next-id")]
        public async Task<ActionResult<int>> GetNextStockTransferId()
        {
            // إن لم يوجد أي سجل في الجدول، فليكن الرقم 1
            bool isEmpty = !await _context.StockTransfers.AnyAsync();
            if (isEmpty)
            {
                return 1;
            }

            // خلاف ذلك، نعتمد على عدد السجلات
            int count = await _context.StockTransfers.CountAsync();
            return count + 1;
        }

        // PUT: api/StockTransfer/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStockTransfer(int id, [FromBody] StockTransfer stockTransfer)
        {
            if (id != stockTransfer.Id)
            {
                return BadRequest("الرقم غير متطابق مع السند المحدد.");
            }

            var existingStockTransfer = await _context.StockTransfers
                .Include(st => st.Items)
                .FirstOrDefaultAsync(st => st.Id == id);

            if (existingStockTransfer == null)
            {
                return NotFound($"لا يوجد تحويل مخزني بالرقم {id}");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (stockTransfer.FromWarehouseId == stockTransfer.ToWarehouseId)
                {
                    return BadRequest("لا يمكن التحويل لنفس المستودع");
                }

                // تحديث السجلات المرتبطة في stocktransferitems
                foreach (var item in stockTransfer.Items)
                {
                    item.StockTransferId = id; // تأكد من تحديث StockTransferId
                }

                _context.StockTransfers.Update(stockTransfer);

                // التحقق من الكميات في المستودعات وتحديثها
                foreach (var item in stockTransfer.Items)
                {
                    var fromWarehouseStock = await _context.WarehouseStocks
                        .FirstOrDefaultAsync(ws => ws.WarehouseId == stockTransfer.FromWarehouseId && ws.ProductId == item.ProductId);

                    if (fromWarehouseStock == null || fromWarehouseStock.Quantity < item.Quantity)
                    {
                        return BadRequest($"الكمية في المستودع المصدر غير كافية للمنتج {item.ProductId}");
                    }

                    fromWarehouseStock.Quantity -= item.Quantity;

                    var toWarehouseStock = await _context.WarehouseStocks
                        .FirstOrDefaultAsync(ws => ws.WarehouseId == stockTransfer.ToWarehouseId && ws.ProductId == item.ProductId);

                    if (toWarehouseStock != null)
                    {
                        toWarehouseStock.Quantity += item.Quantity;
                    }
                    else
                    {
                        _context.WarehouseStocks.Add(new WarehouseStock
                        {
                            WarehouseId = stockTransfer.ToWarehouseId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(stockTransfer);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"حدث خطأ داخلي: {ex.Message}");
            }
        }

        // DELETE: api/StockTransfer/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStockTransfer(int id)
        {
            var stockTransfer = await _context.StockTransfers
                .Include(st => st.Items)
                .FirstOrDefaultAsync(st => st.Id == id);

            if (stockTransfer == null)
            {
                return NotFound($"لا يوجد تحويل مخزني بالرقم {id}");
            }

            // حذف العناصر المرتبطة
            _context.StockTransferItems.RemoveRange(stockTransfer.Items);

            // حذف السند نفسه
            _context.StockTransfers.Remove(stockTransfer);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
