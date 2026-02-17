// Controllers/PurchaseInvoiceController.cs
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

        // DTO خاص بالرفع (بدل ما نستقبل PurchaseInvoice مباشرة)
        // لأن كود المورد القادم من Basma أكبر من Int32
        private sealed class PurchaseInvoiceUploadVm
        {
            public DateTime InvoiceDate { get; set; }
            public string? SupplierCode { get; set; }   // مثال: "2201010201"
            public string? SupplierName { get; set; }   // مثال: "مورد خارجى..."
            public decimal TotalAmount { get; set; }    // اختياري (هنحسبه)
            public List<PurchaseInvoiceItemVm> Items { get; set; } = new();
        }

        private sealed class PurchaseInvoiceItemVm
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal Tax { get; set; }
            public decimal Discount { get; set; }
            public string Unit { get; set; } = "حبة";
            public decimal TotalCost { get; set; } // اختياري (هنحسبه)
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPurchaseInvoices()
        {
            var invoices = await _context.PurchaseInvoices
                .Include(i => i.Supplier)
                .Include(i => i.Items).ThenInclude(it => it.Product)
                .Include(i => i.Attachments)
                .ToListAsync();

            return Ok(invoices.Select(invoice => new
            {
                invoice.Id,
                invoice.InvoiceDate,
                invoice.TotalAmount,
                Supplier = new
                {
                    invoice.Supplier?.Id,
                    invoice.Supplier?.Name,
                    ContactInfo = invoice.Supplier?.ContactInfo,
                    TaxNumber = invoice.Supplier?.TaxNumber
                },
                Items = invoice.Items.Select(item => new
                {
                    item.Id,
                    item.ProductId,
                    Product = new
                    {
                        item.Product?.Id,
                        item.Product?.Name,
                        item.Product?.Code
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
                    Url = $"{Request.Scheme}://{Request.Host}/Uploads/{Path.GetFileName(att.FilePath)}"
                })
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPurchaseInvoice(int id)
        {
            var invoice = await _context.PurchaseInvoices
                .Include(i => i.Supplier)
                .Include(i => i.Items).ThenInclude(it => it.Product)
                .Include(i => i.Attachments)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            return Ok(new
            {
                invoice.Id,
                invoice.InvoiceDate,
                invoice.TotalAmount,
                Supplier = new
                {
                    invoice.Supplier?.Id,
                    invoice.Supplier?.Name,
                    ContactInfo = invoice.Supplier?.ContactInfo,
                    TaxNumber = invoice.Supplier?.TaxNumber
                },
                Items = invoice.Items.Select(item => new
                {
                    item.Id,
                    item.ProductId,
                    Product = new
                    {
                        item.Product?.Id,
                        item.Product?.Name,
                        item.Product?.Code
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
                    Url = $"{Request.Scheme}://{Request.Host}/Uploads/{Path.GetFileName(att.FilePath)}"
                })
            });
        }

        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> CreatePurchaseInvoiceWithFiles()
        {
            var form = await Request.ReadFormAsync();
            var invoiceJson = form["invoice"].ToString();

            PurchaseInvoiceUploadVm? dto;
            try
            {
                dto = JsonSerializer.Deserialize<PurchaseInvoiceUploadVm>(invoiceJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "⚠️ JSON غير صالح", error = ex.Message });
            }

            if (dto == null || string.IsNullOrWhiteSpace(dto.SupplierName) || dto.Items.Count == 0)
                return BadRequest(new { message = "⚠️ بيانات الفاتورة غير مكتملة!" });

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // ✅ نربط المورد على جدول Suppliers الداخلي بالاسم (من غير تعديل DB)
                // لو مش موجود هننشئه تلقائياً
                var supplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.Name == dto.SupplierName);

                if (supplier == null)
                {
                    supplier = new Supplier
                    {
                        Name = dto.SupplierName,
                        ContactInfo = string.Empty,
                        TaxNumber = string.Empty
                    };

                    _context.Suppliers.Add(supplier);
                    await _context.SaveChangesAsync();
                }

                var invoiceEntity = new PurchaseInvoice
                {
                    SupplierId = supplier.Id,
                    InvoiceDate = dto.InvoiceDate,
                    TotalAmount = 0m,
                    Items = new List<PurchaseInvoiceItem>()
                };

                decimal grandTotal = 0m;

                foreach (var it in dto.Items)
                {
                    if (it.ProductId <= 0 || it.Quantity <= 0 || it.Price <= 0)
                        return BadRequest(new { message = "⚠️ بيانات الأصناف غير صحيحة!" });

                    var cost = (it.Quantity * it.Price) - it.Discount;
                    var lineTotal = cost + (cost * (it.Tax / 100m));

                    invoiceEntity.Items.Add(new PurchaseInvoiceItem
                    {
                        ProductId = it.ProductId,
                        Quantity = it.Quantity,
                        Price = it.Price,
                        Tax = it.Tax,
                        Discount = it.Discount,
                        TotalCost = lineTotal,
                        Unit = it.Unit ?? "حبة"
                    });

                    grandTotal += lineTotal;
                }

                invoiceEntity.TotalAmount = grandTotal;

                _context.PurchaseInvoices.Add(invoiceEntity);
                await _context.SaveChangesAsync();

                // save attachments
                foreach (var file in form.Files)
                {
                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "Uploads");
                    Directory.CreateDirectory(uploadPath);
                    var filePath = Path.Combine(uploadPath, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    _context.InvoiceAttachments.Add(new InvoiceAttachment
                    {
                        PurchaseInvoiceId = invoiceEntity.Id,
                        FileName = file.FileName,
                        FilePath = Path.Combine("Uploads", fileName)
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return CreatedAtAction(nameof(GetPurchaseInvoice), new { id = invoiceEntity.Id }, invoiceEntity);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
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

            foreach (var att in invoice.Attachments)
            {
                var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot", att.FilePath);
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            _context.PurchaseInvoices.Remove(invoice);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
