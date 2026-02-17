using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;

namespace InventoryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuotationsController : ControllerBase
    {
        private readonly InventoryDbContext _db;
        public QuotationsController(InventoryDbContext db) => _db = db;

        // ✅ Create/ensure local customer record (NOT Basma) for manual entry names.
        // This lets the new customer appear in other pages that read from /api/Customers.
        public class EnsureCustomerDto
        {
            public string? Name { get; set; }
        }

        [HttpPost("ensure-customer")]
        public async Task<IActionResult> EnsureCustomer([FromBody] EnsureCustomerDto dto)
        {
            var name = (dto?.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Customer name is required" });

            // Try find existing by name
            var existing = await _db.Customers.FirstOrDefaultAsync(x => x.Name == name);
            if (existing != null)
                return Ok(new { id = existing.Id, name = existing.Name });

            var customer = new Customer
            {
                Name = name,
                ContactInfo = "",
                TaxNumber = "",
                Address = "",
                DeliveryLocation = "",
                Email = ""
            };

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            return Ok(new { id = customer.Id, name = customer.Name });
        }



        private void ComputeTotals(Quotation q)
        {
            q.TaxRate = 0.15m;

            decimal subtotalAfterDiscount = 0m;
            decimal totalDiscount = 0m;

            foreach (var l in q.Lines)
            {
                var raw = l.UnitPrice * l.Quantity;
                var disc = l.Discount < 0 ? 0 : l.Discount;

                var after = raw - disc;
                if (after < 0) after = 0;

                l.LineSubtotal = Math.Round(after, 2);
                l.LineTax = Math.Round(l.LineSubtotal * q.TaxRate, 2);
                l.LineTotalWithTax = Math.Round(l.LineSubtotal + l.LineTax, 2);

                subtotalAfterDiscount += l.LineSubtotal;
                totalDiscount += disc;
            }

            q.TotalDiscount = Math.Round(totalDiscount, 2);
            q.Subtotal = Math.Round(subtotalAfterDiscount, 2);
            q.TotalAfterDiscount = q.Subtotal;

            q.TaxTotal = Math.Round(q.TotalAfterDiscount * q.TaxRate, 2);
            q.TotalWithTax = Math.Round(q.TotalAfterDiscount + q.TaxTotal, 2);

            if (q.DownPayment < 0) q.DownPayment = 0;
            q.RemainingAmount = Math.Round(q.TotalWithTax - q.DownPayment, 2);
            if (q.RemainingAmount < 0) q.RemainingAmount = 0;
        }

        private string GenerateQuotationNumber(int id)
        {
            var year = DateTime.UtcNow.Year;
            return $"Q-{year}-{id.ToString().PadLeft(6, '0')}";
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search)
        {
            var query = _db.Quotations
                .Include(x => x.Lines)
                .OrderByDescending(x => x.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.QuotationNumber.Contains(search));

            var list = await query.ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var q = await _db.Quotations.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (q == null) return NotFound();
            return Ok(q);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Quotation q)
        {
            q.Id = 0;
            q.Lines ??= new();
            foreach (var l in q.Lines) l.Id = 0;

            ComputeTotals(q);

            _db.Quotations.Add(q);
            await _db.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(q.QuotationNumber))
            {
                q.QuotationNumber = GenerateQuotationNumber(q.Id);
                await _db.SaveChangesAsync();
            }

            return Ok(q);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Quotation dto)
        {
            var q = await _db.Quotations.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
            if (q == null) return NotFound();

            q.CustomerName = dto.CustomerName ?? "";
            q.CustomerAddress = dto.CustomerAddress ?? "";
            q.CustomerMobile = dto.CustomerMobile ?? "";
            q.RepresentativeName = dto.RepresentativeName ?? "";
            q.InstallationDate = dto.InstallationDate;

            q.PaymentTerms = dto.PaymentTerms ?? "";
            q.PaymentMethod = dto.PaymentMethod ?? "";
            q.Notes = dto.Notes;

            q.DownPayment = dto.DownPayment;

            q.Lines.Clear();
            foreach (var l in dto.Lines ?? new List<QuotationLine>())
            {
                q.Lines.Add(new QuotationLine
                {
                    ProductCode = l.ProductCode ?? "",
                    ProductName = l.ProductName ?? "",
                    Unit = l.Unit ?? "",
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    Discount = l.Discount,
                    ImageUrl = l.ImageUrl
                });
            }

            ComputeTotals(q);
            await _db.SaveChangesAsync();
            return Ok(q);
        }

        // ✅ GET receipt (Upsert default instead of 404)
        [HttpGet("{id:int}/receipt")]
        public async Task<IActionResult> GetReceipt(int id)
        {
            var exists = await _db.Quotations.AnyAsync(x => x.Id == id);
            if (!exists) return NotFound();

            var r = await _db.PaymentReceipts.FirstOrDefaultAsync(x => x.QuotationId == id);
            if (r == null)
            {
                r = new PaymentReceipt
                {
                    QuotationId = id,
                    ReceiptDate = DateTime.UtcNow.Date,
                    ReceiptNumber = $"RC-{DateTime.UtcNow.Year}-{id.ToString().PadLeft(6, '0')}",
                    UpdatedAt = DateTime.UtcNow
                };

                _db.PaymentReceipts.Add(r);
                await _db.SaveChangesAsync();
            }

            return Ok(r);
        }


        // ✅ PUT signature (Upsert)
        public class SignatureUpsertDto
        {
            public string? SignatureDataUrl { get; set; }
        }

        // Primary route used by frontend
        [HttpPut("{id:int}/signature")]
        public async Task<IActionResult> UpsertSignature(int id, [FromBody] SignatureUpsertDto dto)
        {
            var q = await _db.Quotations.FirstOrDefaultAsync(x => x.Id == id);
            if (q == null) return NotFound();

            q.SignatureDataUrl = dto?.SignatureDataUrl;
            q.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { id = q.Id, signatureDataUrl = q.SignatureDataUrl });
        }

        // Alias route (some frontends may call this)
        [HttpPut("signature/{id:int}")]
        public Task<IActionResult> UpsertSignatureAlias(int id, [FromBody] SignatureUpsertDto dto)
            => UpsertSignature(id, dto);

        // ✅ PUT receipt (Upsert)
        [HttpPut("{id:int}/receipt")]
        public async Task<IActionResult> UpsertReceipt(int id, [FromBody] PaymentReceiptUpsertDto dto)
        {
            var quotationExists = await _db.Quotations.AnyAsync(x => x.Id == id);
            if (!quotationExists) return NotFound();

            var r = await _db.PaymentReceipts.FirstOrDefaultAsync(x => x.QuotationId == id);
            if (r == null)
            {
                r = new PaymentReceipt { QuotationId = id };
                _db.PaymentReceipts.Add(r);
            }

            // ===== Parse amount safely =====
            decimal amount = 0m;
            if (!string.IsNullOrWhiteSpace(dto.Amount))
            {
                // يقبل 500 أو 500.00 أو 500,00
                var normalized = dto.Amount.Trim().Replace(",", ".");
                decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
            }

            // ===== Parse date safely =====
            DateTime? receiptDate = null;
            if (!string.IsNullOrWhiteSpace(dto.ReceiptDate))
            {
                // يقبل "YYYY-MM-DD" أو ISO
                if (DateTime.TryParse(dto.ReceiptDate, out var d))
                    receiptDate = d;
            }

            r.FactoryName = dto.FactoryName ?? "";
            r.FactoryAddress = dto.FactoryAddress ?? "";
            r.FactoryPhone = dto.FactoryPhone ?? "";
            r.FactoryCR = dto.FactoryCR ?? "";
            r.FactoryVAT = dto.FactoryVAT ?? "";
            r.ReceiptNumber = dto.ReceiptNumber ?? "";

            r.ReceiptDate = receiptDate;

            r.ReceivedFromName = dto.ReceivedFromName ?? "";
            r.ReceivedFromId = dto.ReceivedFromId ?? "";

            r.Amount = amount;
            r.AmountInWords = dto.AmountInWords ?? "";

            r.PayCash = dto.PayCash;
            r.PayCheck = dto.PayCheck;
            r.PayMada = dto.PayMada;
            r.PayTransfer = dto.PayTransfer;

            r.BankName = dto.BankName ?? "";
            r.CheckNumber = dto.CheckNumber ?? "";

            r.PayForDownPayment = dto.PayForDownPayment;
            r.PayForReady = dto.PayForReady;
            r.PayForStage = dto.PayForStage;
            r.PayForFinal = dto.PayForFinal;
            r.PayForQuotation = dto.PayForQuotation;
            r.PayForSalesOrder = dto.PayForSalesOrder;
            r.PayForOther = dto.PayForOther;

            r.PayForRef = dto.PayForRef ?? "";
            r.ReceiverName = dto.ReceiverName ?? "";
            r.ReceiverJob = dto.ReceiverJob ?? "";

            r.SignatureDataUrl = dto.SignatureDataUrl;
            r.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(r);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var q = await _db.Quotations.FirstOrDefaultAsync(x => x.Id == id);
            if (q == null) return NotFound();
            _db.Quotations.Remove(q);
            await _db.SaveChangesAsync();
            return Ok(new { ok = true });
        }
    }
}
