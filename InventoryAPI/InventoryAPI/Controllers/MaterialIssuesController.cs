using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;

namespace InventoryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // BasicAuth
    public class MaterialIssuesController : ControllerBase
    {
        private readonly InventoryDbContext _db;

        public MaterialIssuesController(InventoryDbContext db)
        {
            _db = db;
        }

        // GET: api/MaterialIssues
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.MaterialIssues
                .AsNoTracking()
                .Include(x => x.Lines)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            var mapped = list.Select(MapToResponseDto).ToList();
            return Ok(mapped);
        }

        // GET: api/MaterialIssues/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var mi = await _db.MaterialIssues
                .AsNoTracking()
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (mi == null) return NotFound("Material Issue not found");
            return Ok(MapToResponseDto(mi));
        }

        // GET: api/MaterialIssues/next-number
        [HttpGet("next-number")]
        public async Task<IActionResult> GetNextNumber()
        {
            var lastId = await _db.MaterialIssues
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            return Ok(new { nextNumber = lastId + 1 });
        }

        // GET: api/MaterialIssues/{id}/storekeeper-signature
        [HttpGet("{id:int}/storekeeper-signature")]
        public async Task<IActionResult> GetStoreKeeperSignature(int id)
        {
            var mi = await _db.MaterialIssues.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (mi == null) return NotFound();
            if (mi.StoreKeeperSignature == null || mi.StoreKeeperSignature.Length == 0) return NoContent();

            var mime = string.IsNullOrWhiteSpace(mi.StoreKeeperSignatureMimeType)
                ? MediaTypeNames.Image.Png
                : mi.StoreKeeperSignatureMimeType;

            return File(mi.StoreKeeperSignature, mime);
        }

        // GET: api/MaterialIssues/{id}/requester-signature
        [HttpGet("{id:int}/requester-signature")]
        public async Task<IActionResult> GetRequesterSignature(int id)
        {
            var mi = await _db.MaterialIssues.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (mi == null) return NotFound();
            if (mi.RequesterSignature == null || mi.RequesterSignature.Length == 0) return NoContent();

            var mime = string.IsNullOrWhiteSpace(mi.RequesterSignatureMimeType)
                ? MediaTypeNames.Image.Png
                : mi.RequesterSignatureMimeType;

            return File(mi.RequesterSignature, mime);
        }

        // POST: api/MaterialIssues
        // ✅ ده "طلب صرف" فقط (Request) => لا يخصم من المخزون ولا يعدّل RemainingQuantity ولا WarehouseStocks.
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MaterialIssueCreateDto dto)
        {
            if (dto == null) return BadRequest("Invalid payload");
            if (dto.FromWarehouseId <= 0) return BadRequest("fromWarehouseId is required");
            if (dto.ToWarehouseId <= 0) return BadRequest("toWarehouseId is required");
            if (dto.Lines == null || dto.Lines.Count == 0) return BadRequest("At least one line is required");

            if (dto.FromWarehouseId == dto.ToWarehouseId)
                return BadRequest("fromWarehouseId and toWarehouseId cannot be the same");

            // ✅ signatures: accept BOTH DataURL and raw base64 (short format)
            byte[]? storeKeeperBytes = null;
            string? storeKeeperMime = null;
            if (!string.IsNullOrWhiteSpace(dto.StoreKeeperSignatureBase64))
            {
                if (!TryParseDataUrl(dto.StoreKeeperSignatureBase64!, out storeKeeperBytes, out storeKeeperMime))
                    return BadRequest("Invalid storeKeeperSignatureBase64");
            }

            byte[]? requesterBytes = null;
            string? requesterMime = null;
            if (!string.IsNullOrWhiteSpace(dto.RequesterSignatureBase64))
            {
                if (!TryParseDataUrl(dto.RequesterSignatureBase64!, out requesterBytes, out requesterMime))
                    return BadRequest("Invalid requesterSignatureBase64");
            }

            var mi = new MaterialIssue
            {
                CreatedAt = DateTime.Now,
                OperatingOrderNumber = dto.OperatingOrderNumber,
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes,
                Status = "جارى التعميد",

                RequesterName = string.IsNullOrWhiteSpace(dto.RequesterName) ? null : dto.RequesterName,
                DepartmentCategoryId = dto.DepartmentCategoryId,

                StoreKeeperSignature = storeKeeperBytes,
                StoreKeeperSignatureMimeType = storeKeeperMime,

                RequesterSignature = requesterBytes,
                RequesterSignatureMimeType = requesterMime,

                Lines = new List<MaterialIssueLine>()
            };

            // ✅ FIFO pricing from StockInVoucherItems (يدعم اختلاف الأسعار لنفس المنتج)
            // NOTE: ده تسعير Snapshot فقط - من غير خصم RemainingQuantity ومن غير تعديل WarehouseStocks

            int lineNo = 1;

            foreach (var l in dto.Lines)
            {
                if (l.ProductId <= 0) return BadRequest("productId is required");
                if (l.RequestedQty <= 0) return BadRequest("requestedQty must be positive");

                var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == l.ProductId);
                if (product == null) return BadRequest($"Product {l.ProductId} not found");

                // ✅ Snapshot availability + FIFO pricing from StockInVoucherItems (RemainingQuantity)
                // ملاحظة: بدون خصم RemainingQuantity وبدون تعديل WarehouseStocks (ده مجرد طلب)
                var lots = await _db.StockInVoucherItems
                    .AsNoTracking()
                    .Include(x => x.StockInVoucher)
                    .Where(x => x.WarehouseId == dto.FromWarehouseId && x.ProductId == l.ProductId && x.RemainingQuantity > 0)
                    .OrderBy(x => x.StockInVoucher!.TransferDate)
                    .ThenBy(x => x.Id)
                    .Select(x => new StockLot { Qty = x.RemainingQuantity, Price = x.Price })
                    .ToListAsync();

                var available = lots.Sum(x => x.Qty);

                if (available < (decimal)l.RequestedQty)
                    return BadRequest($"Insufficient stock for {product.Name}. Available={available}, Requested={l.RequestedQty}");

                var (unitPrice, totalCost) = ComputeFifoCost(lots, l.RequestedQty);

                // ✅ IMPORTANT:
                // This API is فقط "طلب صرف" (Request) وليس حركة صرف فعلية.
                // لذلك: لا نعمل أي خصم/إضافة على WarehouseStocks هنا.

                mi.Lines.Add(new MaterialIssueLine
                {
                    LineNo = lineNo++,
                    ProductId = l.ProductId,
                    ProductCode = product.Code ?? "",
                    ProductName = product.Name ?? "",
                    Unit = product.Unit ?? "",
                    ColorCode = product.ColorCode ?? "",
                    RequestedQty = l.RequestedQty,

                    // entity decimal => نخزن snapshot كقيمة رقمية (هنا بنخزنها كعدد صحيح عمليًا)
                    AvailableQtyAtTime = (int)Math.Floor(available),

                    UnitPrice = unitPrice,
                    TotalCost = totalCost,
                    Notes = l.Notes
                });
            }

            _db.MaterialIssues.Add(mi);
            await _db.SaveChangesAsync();

            return Ok(MapToResponseDto(mi));
        }

        // PUT: api/MaterialIssues/{id}
        // تحديث نفس الطلب (بدون إنشاء طلب جديد) + يسمح بتعديل البنود والتوقيعات
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] MaterialIssueUpdateDto dto)
        {
            if (dto == null) return BadRequest("Invalid payload");

            var mi = await _db.MaterialIssues
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (mi == null) return NotFound("Material Issue not found");

            // بعد التعميد أو الرفض: قفل التعديل
            if (mi.Status == "تم التعميد" || mi.Status == "مرفوض")
                return BadRequest("This request is locked and cannot be edited.");

            // ⚠️ الحقول الثابتة: Id / CreatedAt / FromWarehouseId لا تتغير
            // (باقي الحقول مسموح)

            mi.OperatingOrderNumber = dto.OperatingOrderNumber;

            if (dto.ToWarehouseId > 0)
                mi.ToWarehouseId = dto.ToWarehouseId;

            mi.DepartmentCategoryId = dto.DepartmentCategoryId;

            mi.RequesterName = string.IsNullOrWhiteSpace(dto.RequesterName) ? null : dto.RequesterName.Trim();
            mi.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

            // ✅ التوقيعات: تحديث/مسح
            if (dto.ClearRequesterSignature == true)
            {
                mi.RequesterSignature = null;
                mi.RequesterSignatureMimeType = null;
            }
            else if (!string.IsNullOrWhiteSpace(dto.RequesterSignatureBase64))
            {
                if (!TryParseDataUrl(dto.RequesterSignatureBase64!, out var bytes, out var mime))
                    return BadRequest("Invalid requesterSignatureBase64");
                mi.RequesterSignature = bytes;
                mi.RequesterSignatureMimeType = mime;
            }

            if (dto.ClearStoreKeeperSignature == true)
            {
                mi.StoreKeeperSignature = null;
                mi.StoreKeeperSignatureMimeType = null;
            }
            else if (!string.IsNullOrWhiteSpace(dto.StoreKeeperSignatureBase64))
            {
                if (!TryParseDataUrl(dto.StoreKeeperSignatureBase64!, out var bytes, out var mime))
                    return BadRequest("Invalid storeKeeperSignatureBase64");
                mi.StoreKeeperSignature = bytes;
                mi.StoreKeeperSignatureMimeType = mime;
            }

            // ✅ البنود: استبدال كامل (إضافة/حذف/تعديل)
            if (dto.Lines == null || dto.Lines.Count == 0)
                return BadRequest("At least one line is required");

            mi.Lines.Clear();

            int lineNo = 1;
            foreach (var l in dto.Lines)
            {
                if (l.ProductId <= 0) return BadRequest("productId is required");
                if (l.RequestedQty <= 0) return BadRequest("requestedQty must be positive");

                var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == l.ProductId);
                if (product == null) return BadRequest($"Product {l.ProductId} not found");

                // ✅ Snapshot availability + FIFO pricing from StockInVoucherItems (RemainingQuantity)
                // ملاحظة: بدون خصم RemainingQuantity وبدون تعديل WarehouseStocks (ده مجرد طلب)
                var lots = await _db.StockInVoucherItems
                    .AsNoTracking()
                    .Include(x => x.StockInVoucher)
                    .Where(x => x.WarehouseId == mi.FromWarehouseId && x.ProductId == l.ProductId && x.RemainingQuantity > 0)
                    .OrderBy(x => x.StockInVoucher!.TransferDate)
                    .ThenBy(x => x.Id)
                    .Select(x => new StockLot { Qty = x.RemainingQuantity, Price = x.Price })
                    .ToListAsync();

                var available = lots.Sum(x => x.Qty);

                if (available < l.RequestedQty)
                    return BadRequest($"Insufficient stock for {product.Name}. Available={available}, Requested={l.RequestedQty}");

                var (unitPrice, totalCost) = ComputeFifoCost(lots, l.RequestedQty);

                mi.Lines.Add(new MaterialIssueLine
                {
                    LineNo = lineNo++,
                    ProductId = l.ProductId,
                    ProductCode = product.Code ?? "",
                    ProductName = product.Name ?? "",
                    Unit = product.Unit ?? "",
                    ColorCode = product.ColorCode ?? "",
                    RequestedQty = l.RequestedQty,

                    // entity decimal => نخزن snapshot
                    AvailableQtyAtTime = (int)Math.Floor(available),

                    UnitPrice = unitPrice,
                    TotalCost = totalCost,
                    Notes = l.Notes
                });
            }

            await _db.SaveChangesAsync();
            return Ok(MapToResponseDto(mi));
        }

        // PUT: api/MaterialIssues/{id}/status
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] MaterialIssueStatusDto dto)
        {
            if (dto == null) return BadRequest("Invalid payload");
            if (string.IsNullOrWhiteSpace(dto.Status)) return BadRequest("status is required");

            var mi = await _db.MaterialIssues.FirstOrDefaultAsync(x => x.Id == id);
            if (mi == null) return NotFound("Material Issue not found");

            // لو اتعمل قرار نهائي، ممنوع تغيير الحالة
            if (mi.Status == "تم التعميد" || mi.Status == "مرفوض")
                return BadRequest("This request is locked and cannot change status.");

            var s = dto.Status.Trim();

            if (s != "جارى التعميد" && s != "تم التعميد" && s != "مرفوض")
                return BadRequest("Invalid status value");

            mi.Status = s;
            await _db.SaveChangesAsync();

            return Ok(new { status = mi.Status });
        }

        // PUT: api/MaterialIssues/{id}/decision
        // decision = "accept" => تم التعميد, decision = "reject" => مرفوض
        [HttpPut("{id:int}/decision")]
        public async Task<IActionResult> Decision(int id, [FromBody] MaterialIssueDecisionDto dto)
        {
            if (dto == null) return BadRequest("Invalid payload");
            if (string.IsNullOrWhiteSpace(dto.Decision)) return BadRequest("decision is required");

            var mi = await _db.MaterialIssues.FirstOrDefaultAsync(x => x.Id == id);
            if (mi == null) return NotFound("Material Issue not found");

            // لو اتعمل قرار نهائي قبل كده، متسمحش بالتغيير
            if (mi.Status == "تم التعميد" || mi.Status == "مرفوض")
                return BadRequest("This request is locked and cannot change decision.");

            var decision = dto.Decision.Trim().ToLowerInvariant();

            if (decision == "accept")
                mi.Status = "تم التعميد";
            else if (decision == "reject")
                mi.Status = "مرفوض";
            else
                return BadRequest("decision must be 'accept' or 'reject'");

            await _db.SaveChangesAsync();
            return Ok(new { status = mi.Status });
        }

        public class MaterialIssueUpdateDto
        {
            public int? OperatingOrderNumber { get; set; }
            public int ToWarehouseId { get; set; }
            public int? DepartmentCategoryId { get; set; }

            public string? RequesterName { get; set; }
            public string? Notes { get; set; }

            // signatures
            public string? RequesterSignatureBase64 { get; set; }
            public bool? ClearRequesterSignature { get; set; }

            public string? StoreKeeperSignatureBase64 { get; set; }
            public bool? ClearStoreKeeperSignature { get; set; }

            public List<MaterialIssueLineCreateDto> Lines { get; set; } = new();
        }

        public class MaterialIssueStatusDto
        {
            public string? Status { get; set; }
        }

        public class MaterialIssueDecisionDto
        {
            public string? Decision { get; set; }
        }

        // ✅ Accept:
        // 1) data:image/png;base64,xxxx
        // 2) raw base64: xxxx  (short)
        private static bool TryParseDataUrl(string input, out byte[] bytes, out string? mime)
        {
            bytes = Array.Empty<byte>();
            mime = null;

            var s = input.Trim();

            // DataURL
            if (s.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var commaIndex = s.IndexOf(',');
                if (commaIndex < 0) return false;

                var meta = s.Substring(5, commaIndex - 5);
                var base64 = s.Substring(commaIndex + 1);

                var semi = meta.IndexOf(';');
                mime = semi >= 0 ? meta.Substring(0, semi) : meta;

                try
                {
                    bytes = Convert.FromBase64String(base64);
                    if (bytes.Length == 0) return false;
                    if (string.IsNullOrWhiteSpace(mime)) mime = MediaTypeNames.Image.Png;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            // Raw base64 (short format)
            try
            {
                bytes = Convert.FromBase64String(s);
                if (bytes.Length == 0) return false;
                mime = MediaTypeNames.Image.Png;
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ✅ طبقات المخزون (FIFO) جاية من StockInVoucherItems.RemainingQuantity (بدون خصم)
        private sealed class StockLot
        {
            public decimal Qty { get; set; }
            public decimal Price { get; set; }
        }

        private static (decimal unitPrice, decimal totalCost) ComputeFifoCost(List<StockLot> lots, decimal qtyNeeded)
        {
            if (lots == null || lots.Count == 0 || qtyNeeded <= 0)
                return (0m, 0m);

            var remaining = qtyNeeded;
            decimal totalCost = 0m;
            decimal totalTake = 0m;

            foreach (var lot in lots)
            {
                if (remaining <= 0) break;
                var take = Math.Min(remaining, lot.Qty);
                totalCost += take * lot.Price;
                totalTake += take;
                remaining -= take;
            }

            if (totalTake <= 0) return (0m, 0m);

            // متوسط سعر (يطلع مضبوط حتى لو اتاخد من أكتر من سعر)
            var unitPrice = Math.Round(totalCost / totalTake, 2);
            return (unitPrice, Math.Round(totalCost, 2));
        }

        private MaterialIssueResponseDto MapToResponseDto(MaterialIssue mi)
        {
            // ✅ خلي الـ URL كاملة (تشتغل من أي مكان)
            string Abs(string relative)
                => $"{Request.Scheme}://{Request.Host}{relative}";

            var hasStore = mi.StoreKeeperSignature != null && mi.StoreKeeperSignature.Length > 0;
            var hasReq = mi.RequesterSignature != null && mi.RequesterSignature.Length > 0;

            return new MaterialIssueResponseDto
            {
                Id = mi.Id,
                CreatedAt = mi.CreatedAt,
                OperatingOrderNumber = mi.OperatingOrderNumber,
                FromWarehouseId = mi.FromWarehouseId,
                ToWarehouseId = mi.ToWarehouseId,
                Notes = mi.Notes,
                Status = mi.Status,

                RequesterName = mi.RequesterName,
                DepartmentCategoryId = mi.DepartmentCategoryId,

                HasStoreKeeperSignature = hasStore,
                StoreKeeperSignatureUrl = hasStore ? Abs($"/api/MaterialIssues/{mi.Id}/storekeeper-signature") : null,

                HasRequesterSignature = hasReq,
                RequesterSignatureUrl = hasReq ? Abs($"/api/MaterialIssues/{mi.Id}/requester-signature") : null,

                Lines = mi.Lines?.OrderBy(x => x.LineNo).Select(l => new MaterialIssueLineResponseDto
                {
                    LineNo = l.LineNo,
                    ProductId = l.ProductId,
                    ProductCode = l.ProductCode,
                    ProductName = l.ProductName,
                    Unit = l.Unit,
                    ColorCode = l.ColorCode,
                    RequestedQty = l.RequestedQty,

                    // ✅ FIX: decimal -> int (من غير ما نكسر الفرونت)
                    AvailableQtyAtTime = (int)Math.Floor(l.AvailableQtyAtTime),

                    UnitPrice = l.UnitPrice,
                    TotalCost = l.TotalCost,
                    Notes = l.Notes
                }).ToList() ?? new List<MaterialIssueLineResponseDto>()
            };
        }
    }
}
