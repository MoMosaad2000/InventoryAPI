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

            // إزالة الكائنات الملاحيّة من البنود لتفادي مشاكل التتبع
            foreach (var item in stockTransfer.Items)
            {
                item.Product = null;
                item.Warehouse = null;
                // ربط البند بالسند الرئيسي
                item.StockTransfer = stockTransfer;
                // تعيين WarehouseId لكل بند إلى FromWarehouseId (أو حسب المنطق الذي تريده)
                item.WarehouseId = stockTransfer.FromWarehouseId;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (stockTransfer.FromWarehouseId == stockTransfer.ToWarehouseId)
                {
                    return BadRequest("لا يمكن التحويل لنفس المستودع");
                }

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
                    // لا حاجة لاستدعاء Update لأن الكيان متتبع

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


        // DELETE: api/StockTransfer/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStockTransfer(int id)
        {
            var stockTransfer = await _context.StockTransfers
                .Include(st => st.Items)
                .FirstOrDefaultAsync(st => st.Id == id);

            if (stockTransfer == null)
            {
                return NotFound();
            }

            _context.StockTransferItems.RemoveRange(stockTransfer.Items);
            _context.StockTransfers.Remove(stockTransfer);

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
