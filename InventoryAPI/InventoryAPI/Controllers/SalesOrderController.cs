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

        // =========================
        // POST: api/SalesOrder
        // =========================
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] SalesOrder order)
        {
            if (order == null)
                return BadRequest("⚠️ بيانات الطلب غير صحيحة.");

            if (string.IsNullOrWhiteSpace(order.RepresentativeName))
                return BadRequest("⚠️ يرجى إدخال اسم المندوب.");

            if (order.Items == null || !order.Items.Any())
                return BadRequest("⚠️ يرجى إضافة عناصر للطلب.");

            // ✅ لازم يكون عندنا عميل: يا CustomerId يا CustomerName يا order.Customer.Name
            var incomingCustomerName =
                order.CustomerName ??
                order.Customer?.Name;

            if (order.CustomerId == null && string.IsNullOrWhiteSpace(incomingCustomerName))
                return BadRequest("⚠️ يرجى إدخال اسم العميل.");

            try
            {
                // ✅ تاريخ الإنشاء
                order.CreationDate = DateTime.UtcNow;

                // ✅ Normalize snapshot fields
                order.CustomerName = string.IsNullOrWhiteSpace(order.CustomerName) ? null : order.CustomerName.Trim();
                order.CustomerExternalCode = string.IsNullOrWhiteSpace(order.CustomerExternalCode) ? null : order.CustomerExternalCode.Trim();
                order.CustomerMobile = string.IsNullOrWhiteSpace(order.CustomerMobile) ? null : order.CustomerMobile.Trim();
                order.CustomerAddress = string.IsNullOrWhiteSpace(order.CustomerAddress) ? null : order.CustomerAddress.Trim();

                // ✅ ربط/إنشاء عميل داخل جدول Customers (عشان يظهر في باقي القوائم)
                if (order.CustomerId != null && order.CustomerId > 0)
                {
                    var dbCust = await _context.Customers.FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value);
                    if (dbCust == null)
                        return BadRequest("⚠️ العميل المحدد غير موجود.");

                    // ✅ لو الداتا جاية من Basma (أو Snapshot) وCustomer عندنا ناقص معلومات، حدثها
                    if (!string.IsNullOrWhiteSpace(order.CustomerMobile) && string.IsNullOrWhiteSpace(dbCust.ContactInfo))
                        dbCust.ContactInfo = order.CustomerMobile;
                    if (!string.IsNullOrWhiteSpace(order.CustomerAddress) && string.IsNullOrWhiteSpace(dbCust.Address))
                        dbCust.Address = order.CustomerAddress;

                    order.Customer = dbCust;
                    order.CustomerName = dbCust.Name;

                    // ✅ حفظ Snapshot في نفس أمر البيع (للفاتورة)
                    order.CustomerMobile ??= dbCust.ContactInfo;
                    order.CustomerAddress ??= dbCust.Address;
                }
                else
                {
                    var name = (incomingCustomerName ?? "").Trim();
                    // ممكن يكون العميل موجود بالفعل بنفس الاسم
                    var dbCust = await _context.Customers.FirstOrDefaultAsync(c => c.Name == name);

                    if (dbCust == null)
                    {
                        dbCust = new Customer
                        {
                            Name = name,
                            ContactInfo = order.CustomerMobile ?? order.Customer?.ContactInfo ?? string.Empty,
                            TaxNumber = order.Customer?.TaxNumber,
                            Address = order.CustomerAddress ?? order.Customer?.Address,
                            DeliveryLocation = order.Customer?.DeliveryLocation,
                            Email = order.Customer?.Email
                        };

                        _context.Customers.Add(dbCust);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // ✅ لو موجود عندنا باسم، وعايزين نكمل بياناته لو ناقصة
                        if (!string.IsNullOrWhiteSpace(order.CustomerMobile) && string.IsNullOrWhiteSpace(dbCust.ContactInfo))
                            dbCust.ContactInfo = order.CustomerMobile;
                        if (!string.IsNullOrWhiteSpace(order.CustomerAddress) && string.IsNullOrWhiteSpace(dbCust.Address))
                            dbCust.Address = order.CustomerAddress;
                    }

                    order.CustomerId = dbCust.Id;
                    order.Customer = dbCust;
                    order.CustomerName = dbCust.Name;

                    // ✅ حفظ Snapshot في نفس أمر البيع (للفاتورة)
                    order.CustomerMobile ??= dbCust.ContactInfo;
                    order.CustomerAddress ??= dbCust.Address;
                }

                // ✅ حساب الإجماليات من العناصر
                decimal subtotal = 0, totalWithTax = 0;
                foreach (var it in order.Items)
                {
                    it.Notes ??= "";
                    it.OrderCode ??= "";
                    it.OrderName ??= "";
                    it.Unit ??= "";

                    var cost = it.Quantity * it.UnitPrice;
                    it.Total = cost + (cost * it.Tax / 100);

                    subtotal += cost;
                    totalWithTax += it.Total;
                }

                order.Subtotal = subtotal;
                order.TotalWithTax = totalWithTax;

                // ✅ رقم أمر بيع تسلسلي (لا يعتمد على العميل)
                var lastOrder = await _context.SalesOrder
                    .OrderByDescending(x => x.OrderNumber)
                    .FirstOrDefaultAsync();

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

        // =========================
        // GET: api/SalesOrder/{id}
        // =========================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.SalesOrder
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return Ok(new
            {
                order.Id,
                order.OrderNumber,
                order.QuotationNumber,
                order.RepresentativeName,
                order.ExpirationDate,
                order.CreationDate,
                order.PaymentTerms,
                order.PaymentMethod,
                Customer = order.Customer, // للعرض فقط
                order.CustomerName,
                order.CustomerExternalCode,
                order.CustomerMobile,
                order.CustomerAddress,
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
                    i.Drawing
                }),
                order.Subtotal,
                order.TotalWithTax
            });
        }

        // =========================
        // GET: api/SalesOrder
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _context.SalesOrder
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .OrderByDescending(o => o.Id)
                .ToListAsync();

            return Ok(orders);
        }

        // =========================
        // PUT: api/SalesOrder/{id}
        // =========================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] SalesOrder updatedOrder)
        {
            var existing = await _context.SalesOrder
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (existing == null) return NotFound();

            if (updatedOrder.Items == null || !updatedOrder.Items.Any())
                return BadRequest("⚠️ يرجى إضافة عناصر للطلب.");

            existing.RepresentativeName = updatedOrder.RepresentativeName ?? "";
            existing.ExpirationDate = updatedOrder.ExpirationDate;
            existing.Notes = updatedOrder.Notes ?? "";
            existing.PaymentTerms = updatedOrder.PaymentTerms ?? "";
            existing.PaymentMethod = updatedOrder.PaymentMethod ?? "";
            existing.QuotationNumber = updatedOrder.QuotationNumber;

            // ✅ snapshot fields
            existing.CustomerExternalCode = string.IsNullOrWhiteSpace(updatedOrder.CustomerExternalCode) ? null : updatedOrder.CustomerExternalCode.Trim();
            existing.CustomerName = string.IsNullOrWhiteSpace(updatedOrder.CustomerName) ? existing.CustomerName : updatedOrder.CustomerName.Trim();
            existing.CustomerMobile = string.IsNullOrWhiteSpace(updatedOrder.CustomerMobile) ? existing.CustomerMobile : updatedOrder.CustomerMobile.Trim();
            existing.CustomerAddress = string.IsNullOrWhiteSpace(updatedOrder.CustomerAddress) ? existing.CustomerAddress : updatedOrder.CustomerAddress.Trim();

            // ✅ تحديث العميل: لو جالك CustomerId (قائمة) أو اسم يدوي
            var incomingCustomerName =
                updatedOrder.CustomerName ??
                updatedOrder.Customer?.Name;

            if (updatedOrder.CustomerId != null && updatedOrder.CustomerId > 0)
            {
                var dbCust = await _context.Customers.FirstOrDefaultAsync(c => c.Id == updatedOrder.CustomerId.Value);
                if (dbCust == null) return BadRequest("⚠️ العميل المحدد غير موجود.");

                // ✅ كمل بيانات العميل لو ناقصة
                if (!string.IsNullOrWhiteSpace(existing.CustomerMobile) && string.IsNullOrWhiteSpace(dbCust.ContactInfo))
                    dbCust.ContactInfo = existing.CustomerMobile;
                if (!string.IsNullOrWhiteSpace(existing.CustomerAddress) && string.IsNullOrWhiteSpace(dbCust.Address))
                    dbCust.Address = existing.CustomerAddress;

                existing.CustomerId = dbCust.Id;
                existing.Customer = dbCust;
                existing.CustomerName = dbCust.Name;
            }
            else if (!string.IsNullOrWhiteSpace(incomingCustomerName))
            {
                var name = incomingCustomerName.Trim();
                var dbCust = await _context.Customers.FirstOrDefaultAsync(c => c.Name == name);

                if (dbCust == null)
                {
                    dbCust = new Customer
                    {
                        Name = name,
                        ContactInfo = existing.CustomerMobile ?? updatedOrder.Customer?.ContactInfo ?? string.Empty,
                        TaxNumber = updatedOrder.Customer?.TaxNumber,
                        Address = existing.CustomerAddress ?? updatedOrder.Customer?.Address,
                        DeliveryLocation = updatedOrder.Customer?.DeliveryLocation,
                        Email = updatedOrder.Customer?.Email
                    };
                    _context.Customers.Add(dbCust);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(existing.CustomerMobile) && string.IsNullOrWhiteSpace(dbCust.ContactInfo))
                        dbCust.ContactInfo = existing.CustomerMobile;
                    if (!string.IsNullOrWhiteSpace(existing.CustomerAddress) && string.IsNullOrWhiteSpace(dbCust.Address))
                        dbCust.Address = existing.CustomerAddress;
                }

                existing.CustomerId = dbCust.Id;
                existing.Customer = dbCust;
                existing.CustomerName = dbCust.Name;
            }

            // ✅ لو ما اتبعتش Snapshot، خده من Customer relation
            existing.CustomerMobile ??= existing.Customer?.ContactInfo;
            existing.CustomerAddress ??= existing.Customer?.Address;

            // حذف العناصر القديمة
            _context.SalesOrderItem.RemoveRange(existing.Items);

            // إنشاء العناصر الجديدة + حساب الإجمالي
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
                    Drawing = it.Drawing
                };
            }).ToList();

            existing.Items = newItems;
            existing.Subtotal = subtotal;
            existing.TotalWithTax = totalWithTax;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // =========================
        // DELETE: api/SalesOrder/{id}
        // =========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.SalesOrder
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            _context.SalesOrderItem.RemoveRange(order.Items);
            _context.SalesOrder.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
