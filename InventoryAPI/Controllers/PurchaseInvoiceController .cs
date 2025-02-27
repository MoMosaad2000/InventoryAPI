using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseInvoiceController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public PurchaseInvoiceController(InventoryDbContext context)
        {
            _context = context;
        }

        // 🔹 **جلب جميع فواتير الشراء**
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseInvoice>>> GetPurchaseInvoices()
        {
            return await _context.PurchaseInvoices
                .Include(i => i.Supplier) // ✅ تضمين بيانات المورد
                .Include(i => i.Items)
                .ThenInclude(it => it.Product) // ✅ تضمين المنتجات
                .ToListAsync();
        }

        // 🔹 **إضافة فاتورة شراء جديدة**
        [HttpPost]
        public async Task<ActionResult<PurchaseInvoice>> CreatePurchaseInvoice([FromBody] PurchaseInvoice invoice)
        {
            if (invoice == null || invoice.SupplierId == 0 || invoice.Items == null || invoice.Items.Count == 0)
            {
                return BadRequest(new { message = "⚠️ بيانات الفاتورة غير مكتملة!" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var supplier = await _context.Suppliers.FindAsync(invoice.SupplierId);
                if (supplier == null)
                {
                    return BadRequest("⚠️ المورد غير موجود.");
                }

                invoice.Supplier = supplier;
                decimal totalInvoiceAmount = 0;

                foreach (var item in invoice.Items)
                {
                    if (item.Quantity <= 0 || item.Price <= 0)
                    {
                        return BadRequest($"⚠️ البيانات غير صحيحة للمنتج {item.ProductId}");
                    }

                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        return BadRequest($"⚠️ المنتج ذو المعرف {item.ProductId} غير موجود.");
                    }
                    var cost = (item.Quantity * item.Price)- item.Discount;
                    var totalCost = cost +( cost * (item.Tax / 100) );
                    totalInvoiceAmount += totalCost;

                    //var warehouseStock = await _context.WarehouseStocks
                    //    .Where(ws => ws.ProductId == item.ProductId)
                    //    .FirstOrDefaultAsync();

                    //if (warehouseStock != null)
                    //{
                    //    warehouseStock.Quantity += item.Quantity;
                    //}
                    //else
                    //{
                    //    _context.WarehouseStocks.Add(new WarehouseStock
                    //    {
                    //        ProductId = item.ProductId,
                    //        WarehouseId = product.WarehouseId,
                    //        Quantity = item.Quantity
                    //    });
                    //}
                }

                invoice.TotalAmount = totalInvoiceAmount;
                _context.PurchaseInvoices.Add(invoice);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetPurchaseInvoices), new { id = invoice.Id }, invoice);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"حدث خطأ داخلي: {ex.Message}");
            }
        }
        // 🔹 **حذف فاتورة شراء**
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePurchaseInvoice(int id)
        {
            var invoice = await _context.PurchaseInvoices.FindAsync(id);
            if (invoice == null) return NotFound();

            _context.PurchaseInvoices.Remove(invoice);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
