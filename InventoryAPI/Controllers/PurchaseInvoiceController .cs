using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InventoryAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseInvoiceController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        private readonly IWebHostEnvironment _env;
        public PurchaseInvoiceController(InventoryDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        private string GetFileUrl(string relativePath)
        {
            var fileName = Path.GetFileName(relativePath);
            return $"{Request.Scheme}://{Request.Host}/Uploads/{fileName}";
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPurchaseInvoices()
        {
            var invoices = await _context.PurchaseInvoices
                .Include(i => i.Supplier)
                .Include(i => i.Items).ThenInclude(it => it.Product)
                .Include(i => i.Attachments)
                .ToListAsync();

            var result = invoices.Select(invoice => new
            {
                invoice.Id,
                invoice.InvoiceDate,
                invoice.TotalAmount,
                Supplier = new
                {
                    invoice.Supplier?.Id,
                    invoice.Supplier?.Name,
                    invoice.Supplier?.ContactInfo,
                    invoice.Supplier?.TaxNumber
                },
                Items = invoice.Items.Select(item => new
                {
                    item.Id,
                    item.ProductId,
                    Product = new
                    {
                        item.Product?.Id,
                        item.Product?.Name,
                        item.Product?.Code,
                        item.Product?.Description,
                        item.Product?.Unit

                    },
                    item.Quantity,
                    item.Price,
                    item.Tax,
                    item.Discount,
                    Unit = item.Product?.Unit ?? "حبة",

                    item.TotalCost
                }),
                Attachments = invoice.Attachments.Select(att => new
                {
                    att.Id,
                    att.FileName,
                    Url = GetFileUrl(att.FilePath)
                })
            });

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPurchaseInvoice(int id)
        {
            var invoice = await _context.PurchaseInvoices
                .Include(i => i.Supplier)
                .Include(i => i.Items).ThenInclude(it => it.Product)
                .Include(i => i.Attachments)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return NotFound();

            var result = new
            {
                invoice.Id,
                invoice.InvoiceDate,
                invoice.TotalAmount,
                Supplier = new
                {
                    invoice.Supplier?.Id,
                    invoice.Supplier?.Name,
                    invoice.Supplier?.ContactInfo,
                    invoice.Supplier?.TaxNumber
                },
                Items = invoice.Items.Select(item => new
                {
                    item.Id,
                    item.ProductId,
                    Product = new
                    {
                        item.Product?.Id,
                        item.Product?.Name,
                        item.Product?.Code,
                        item.Product?.Description,
                        item.Product?.Unit

                    },
                    item.Quantity,
                    item.Price,
                    item.Tax,
                    item.Discount,
                    item.TotalCost
                }),
                Attachments = invoice.Attachments.Select(att => new
                {
                    att.Id,
                    att.FileName,
                    Url = GetFileUrl(att.FilePath)
                })
            };

            return Ok(result);
        }

        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> CreatePurchaseInvoiceWithFiles()
        {
            var form = await Request.ReadFormAsync();
            var invoiceJson = form["invoice"];

            var invoice = JsonSerializer.Deserialize<PurchaseInvoice>(invoiceJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (invoice == null || invoice.SupplierId == 0 || invoice.Items == null || invoice.Items.Count == 0)
                return BadRequest(new { message = "⚠️ بيانات الفاتورة غير مكتملة!" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var supplier = await _context.Suppliers.FindAsync(invoice.SupplierId);
                if (supplier == null)
                    return BadRequest("⚠️ المورد غير موجود.");

                invoice.Supplier = supplier;

                decimal totalInvoiceAmount = 0;
                foreach (var item in invoice.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                        return BadRequest($"⚠️ المنتج بالمعرف {item.ProductId} غير موجود.");

                    var cost = (item.Quantity * item.Price) - item.Discount;
                    var totalCost = cost + (cost * (item.Tax / 100));
                    totalInvoiceAmount += totalCost;
                }

                invoice.TotalAmount = totalInvoiceAmount;
                _context.PurchaseInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                var files = form.Files;
                foreach (var file in files)
                {
                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "Uploads");
                    var filePath = Path.Combine(uploadPath, fileName);

                    Directory.CreateDirectory(uploadPath);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    var attachment = new InvoiceAttachment
                    {
                        PurchaseInvoiceId = invoice.Id,
                        FileName = file.FileName,
                        FilePath = Path.Combine("Uploads", fileName)
                    };

                    _context.InvoiceAttachments.Add(attachment);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetPurchaseInvoice), new { id = invoice.Id }, invoice);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"❌ حدث خطأ داخلي: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePurchaseInvoice(int id)
        {
            var invoice = await _context.PurchaseInvoices
                .Include(i => i.Attachments)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            foreach (var attachment in invoice.Attachments)
            {
                var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot", attachment.FilePath);
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            _context.PurchaseInvoices.Remove(invoice);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
