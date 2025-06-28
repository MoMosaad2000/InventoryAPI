// ✅ Controllers/SalesOrderController.cs
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
    public class SalesOrderController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public SalesOrderController(InventoryDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] SalesOrder order)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.RepresentativeName) || order.Items == null || !order.Items.Any() || order.Customer == null)
                return BadRequest("⚠️ يرجى إدخال كل البيانات.");

            try
            {
                order.CreationDate = DateTime.UtcNow;

                var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c =>
                    c.Name == order.Customer.Name &&
                    c.ContactInfo == order.Customer.ContactInfo &&
                    c.TaxNumber == order.Customer.TaxNumber);

                if (existingCustomer != null)
                {
                    order.CustomerId = existingCustomer.Id;
                    order.Customer = existingCustomer;
                }
                else
                {
                    var cust = order.Customer;
                    _context.Customers.Add(cust);
                    await _context.SaveChangesAsync();
                    order.CustomerId = cust.Id;
                    order.Customer = cust;
                }

                decimal sub = 0, tot = 0;
                foreach (var it in order.Items)
                {
                    it.Notes ??= "";
                    it.OrderCode ??= "";
                    it.OrderName ??= "";
                    it.Unit ??= "";
                    var cost = it.Quantity * it.UnitPrice;
                    it.Total = cost + (cost * it.Tax / 100);
                    sub += cost;
                    tot += it.Total;
                }
                order.Subtotal = sub;
                order.TotalWithTax = tot;

                var lastOrder = await _context.SalesOrder.OrderByDescending(x => x.OrderNumber).FirstOrDefaultAsync();
                order.OrderNumber = (lastOrder?.OrderNumber ?? 0) + 1;

                _context.SalesOrder.Add(order);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ خطأ داخلي: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.SalesOrder
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
             
            if (order == null) return NotFound();

            var result = new
            {
                order.Id,
                order.OrderNumber,
                order.RepresentativeName,
                order.ExpirationDate,
                order.CreationDate,
                order.PaymentTerms,
                order.PaymentMethod,
                Customer = order.Customer,
                Items = order.Items.Select(i => new
                {
                    i.Id,
                    i.OrderName,
                    i.OrderCode,
                    i.Unit,
                    i.Quantity,
                    i.UnitPrice,
                    i.Tax,
                    i.Total,
                    i.Notes,
                    Drawing = i.Drawing // ✅ الصورة كـ Base64 ترجع كما هي
                })
            };

            return Ok(result);
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _context.SalesOrder
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .ToListAsync();

            return Ok(orders);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] SalesOrder updatedOrder)
        {
            var existing = await _context.SalesOrder
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (existing == null) return NotFound();

            // تحديث بيانات الطلب العامة
            existing.RepresentativeName = updatedOrder.RepresentativeName;
            existing.ExpirationDate = updatedOrder.ExpirationDate;
            existing.Notes = updatedOrder.Notes;
            existing.PaymentTerms = updatedOrder.PaymentTerms;
            existing.PaymentMethod = updatedOrder.PaymentMethod;

            // تحديث بيانات العميل
            if (updatedOrder.Customer != null)
            {
                var c = existing.Customer;
                c.Name = updatedOrder.Customer.Name;
                c.ContactInfo = updatedOrder.Customer.ContactInfo;
                c.TaxNumber = updatedOrder.Customer.TaxNumber;
                c.Address = updatedOrder.Customer.Address;
                c.DeliveryLocation = updatedOrder.Customer.DeliveryLocation;
                c.Email = updatedOrder.Customer.Email;
            }

            // حذف العناصر القديمة
            _context.SalesOrderItem.RemoveRange(existing.Items);

            // إنشاء العناصر الجديدة
            decimal subtotal = 0, totalWithTax = 0;

            var newItems = updatedOrder.Items.Select(it =>
            {
                it.Notes ??= "";
                it.OrderCode ??= "";
                it.OrderName ??= "";
                it.Unit ??= "";

                var cost = it.Quantity * it.UnitPrice;
                var total = cost + (cost * it.Tax / 100);

                subtotal += cost;
                totalWithTax += total;

                return new SalesOrderItem
                {
                    OrderName = it.OrderName,
                    OrderCode = it.OrderCode,
                    Unit = it.Unit,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    Tax = it.Tax,
                    Total = total,
                    Notes = it.Notes,
                    Drawing = it.Drawing // ✅ تأكد من تخزين Base64 string
                };
            }).ToList();

            existing.Items = newItems;
            existing.Subtotal = subtotal;
            existing.TotalWithTax = totalWithTax;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.SalesOrder.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            _context.SalesOrderItem.RemoveRange(order.Items);
            _context.SalesOrder.Remove(order);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
