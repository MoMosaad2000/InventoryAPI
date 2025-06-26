using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OperationOrderController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public OperationOrderController(InventoryDbContext context)
        {
            _context = context;
        }

        // ✅ GET /api/OperationOrder/{orderNumber}
        [HttpGet("{orderNumber}")]
        public async Task<IActionResult> GetByOrderNumber(int orderNumber)
        {
            var salesOrder = await _context.SalesOrder
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (salesOrder == null)
                return NotFound("أمر البيع غير موجود");

            var finalProducts = await _context.FinalProducts.ToListAsync();

            var operationOrder = new OperationOrders
            {
                OrderNumber = orderNumber,
                CreationDate = salesOrder.CreationDate,
                ExpirationDate = salesOrder.ExpirationDate,
                CustomerName = salesOrder.Customer?.Name ?? "غير معروف",

                Items = salesOrder.Items.Select((item, index) =>
                {
                    var product = finalProducts.FirstOrDefault(p => p.Name.Trim() == item.OrderName.Trim());

                    return new OperationOrderItem
                    {
                        ProductName = item.OrderName,
                        ProductCode = product?.Code ?? item.OrderCode,
                        Unit = item.Unit,
                        Quantity = item.Quantity,
                        ProductionDurationHours = product?.ProductionDurationHours ?? 0
                    };
                }).ToList()
            };

            return Ok(operationOrder);
        }

        // ✅ POST /api/OperationOrder
        [HttpPost]
        public async Task<IActionResult> Create(OperationOrders order)
        {
            _context.OperationOrders.Add(order);
            await _context.SaveChangesAsync();
            return Ok(order);
        }

        // ✅ GET ALL saved orders
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _context.OperationOrders
                .Include(o => o.Items)
                .ToListAsync();

            return Ok(orders);
        }

        // ✅ DELETE /api/OperationOrder/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.OperationOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            _context.OperationOrders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
