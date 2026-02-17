<<<<<<< HEAD:InventoryAPI/Controllers/StockReportController.cs
﻿using InventoryAPI.Data;
=======
﻿// Controllers/StockReportController.cs
using InventoryAPI.Data;
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Controllers/StockReportController.cs
using InventoryAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
<<<<<<< HEAD:InventoryAPI/Controllers/StockReportController.cs
using System.Linq;
=======
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Controllers/StockReportController.cs

namespace InventoryAPI.Controllers
{
    [Authorize]
<<<<<<< HEAD:InventoryAPI/Controllers/StockReportController.cs
    [ApiController]
    [Route("api/[controller]")]
    public class StockReportController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public StockReportController(InventoryDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetStockReport([FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int productId)
        {
            try
            {
                DateTime parsedStartDate = DateTime.ParseExact(startDate, "yyyy-MM-dd", null);
                DateTime parsedEndDate = DateTime.ParseExact(endDate, "yyyy-MM-dd", null);

                var stockInData = await _context.StockInVouchers
                    .Where(s => s.TransferDate.Date >= parsedStartDate.Date && s.TransferDate.Date <= parsedEndDate.Date)
                    .SelectMany(s => s.Items
                        .Where(i => i.ProductId == productId)
                        .Select(i => new StockReportItem
                        {
                            VoucherName = s.WarehouseKeeperName,
                            VoucherNumber = s.Id.ToString(),
                            TransferDate = s.TransferDate,
                            From = s.WarehouseKeeperName,
                            To = s.WarehouseKeeperName,
                            Quantity = i.Quantity,
                            Cost = i.Price,
                            OperatingOrder = "in",
                            ProductName = i.Product.Name,
                            ProductCode = i.Product.Code,
                            SupplierName = i.Supplier!= null ? i.Supplier.Name : "غير متوفر",
                            ColorCode = i.ColorCode,
                            TotalCost = i.TotalCost
                        }))
                    .ToListAsync();

                var stockOutData = await _context.StockOutVouchers
                    .Where(s => s.TransferDate.Date >= parsedStartDate.Date && s.TransferDate.Date <= parsedEndDate.Date)
                    .SelectMany(s => s.Items
                        .Where(i => i.ProductId == productId)
                        .Select(i => new StockReportItem
                        {
                            VoucherName = s.WarehouseKeeperName,
                            VoucherNumber = s.Id.ToString(),
                            TransferDate = s.TransferDate,
                            From = s.WarehouseKeeperName,
                            To = s.Customer != null ? s.Customer.Name : "غير متوفر",
                            Quantity = i.Quantity,
                            Cost = i.Price,
                            OperatingOrder = "out",
                            ProductName = i.Product.Name,
                            ProductCode = i.Product.Code,
                            CustomerName = s.Customer != null ? s.Customer.Name : "غير متوفر",
                            ColorCode = i.ColorCode,
                            TotalCost = i.Quantity * i.Price
                        }))
                    .ToListAsync();

                var reportData = stockInData.Concat(stockOutData).OrderBy(s => s.TransferDate).ToList();

                return Ok(reportData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", error = ex.Message });
            }
        }


=======
    [Route("api/[controller]")]
    [ApiController]
    public class StockReportController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        public StockReportController(InventoryDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetReport(
            DateTime startDate, DateTime endDate, int? productId, string colorCode = null)
        {
            // قواميس أسماء ثابتة للاستخدام في العرض
            var suppliersDict = await _context.Suppliers
                .AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);

            var warehousesDict = await _context.Warehouses
                .AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);

            var customersDict = await _context.Customers
                .AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name);

            // ===================== 1) الإضافات (مورد -> مخزن) =====================
            var ins = await _context.StockInVoucherItems
                .AsNoTracking()
                .Include(i => i.StockInVoucher)
                .Where(i =>
                    i.StockInVoucher.TransferDate.Date >= startDate.Date &&
                    i.StockInVoucher.TransferDate.Date <= endDate.Date &&
                    (!productId.HasValue || i.ProductId == productId) &&
                    (string.IsNullOrEmpty(colorCode) || i.ColorCode == colorCode)
                )
                .Select(i => new
                {
                    Date = i.StockInVoucher.TransferDate,
                    Qty = i.Quantity,
                    Price = i.Price,
                    SupplierId = (int?)i.SupplierId,   // cast لـ nullable لتفادي مشاكل ??
                    WarehouseId = (int?)i.WarehouseId,  // cast لـ nullable لتفادي مشاكل ??
                    OperatingOrder = i.StockInVoucher.OperatingOrder
                })
                .ToListAsync();

            var insMapped = ins.Select(r =>
            {
                int supplierId = r.SupplierId ?? 0;
                int warehouseId = r.WarehouseId ?? 0;

                return new RowShape
                {
                    Date = r.Date,
                    Type = "in",
                    Qty = r.Qty,
                    Price = r.Price,
                    From = (supplierId > 0 && suppliersDict.TryGetValue(supplierId, out var sName)) ? sName : "مورد",
                    To = (warehouseId > 0 && warehousesDict.TryGetValue(warehouseId, out var wName)) ? wName : "مخزن",
                    OperatingOrder = r.OperatingOrder ?? string.Empty
                };
            }).ToList();

            // ===================== 2) الصرف (مخزن -> عميل) =====================
            var outs = await _context.StockOutVoucherItems
                .AsNoTracking()
                .Include(i => i.StockOutVoucher)
                .Where(i =>
                    i.StockOutVoucher.TransferDate.Date >= startDate.Date &&
                    i.StockOutVoucher.TransferDate.Date <= endDate.Date &&
                    (!productId.HasValue || i.ProductId == productId) &&
                    (string.IsNullOrEmpty(colorCode) || i.ColorCode == colorCode)
                )
                .Select(i => new
                {
                    Date = i.StockOutVoucher.TransferDate,
                    Qty = (int)i.Quantity,
                    Price = i.Price,
                    WarehouseId = (int?)i.WarehouseId,              // مصدر الصرف
                    CustomerId = (int?)i.StockOutVoucher.CustomerId, // الوجهة = العميل
                    OperatingOrder = i.StockOutVoucher.OperatingOrder
                })
                .ToListAsync();

            var outsMapped = outs.Select(r =>
            {
                int warehouseId = r.WarehouseId ?? 0;
                int customerId = r.CustomerId ?? 0;

                return new RowShape
                {
                    Date = r.Date,
                    Type = "out",
                    Qty = r.Qty,
                    Price = r.Price,
                    From = (warehouseId > 0 && warehousesDict.TryGetValue(warehouseId, out var fwName)) ? fwName : "مخزن",
                    To = (customerId > 0 && customersDict.TryGetValue(customerId, out var cName)) ? cName : "عميل",
                    OperatingOrder = r.OperatingOrder ?? string.Empty
                };
            }).ToList();

            // ===================== 3) التحويلات (مخزن -> مخزن) — للعرض فقط =====================
            var xfers = await _context.StockTransferItems
                .AsNoTracking()
                .Include(i => i.StockTransfer)
                .Where(i =>
                    i.StockTransfer.TransferDate.Date >= startDate.Date &&
                    i.StockTransfer.TransferDate.Date <= endDate.Date &&
                    (!productId.HasValue || i.ProductId == productId) &&
                    (string.IsNullOrEmpty(colorCode) || i.ColorCode == colorCode)
                )
                .Select(i => new
                {
                    Date = i.StockTransfer.TransferDate,
                    Qty = i.Quantity,
                    Price = i.Price, // لن يُستخدم في الرصيد
                    FromWarehouseId = (int?)i.StockTransfer.FromWarehouseId,
                    ToWarehouseId = (int?)i.StockTransfer.ToWarehouseId,
                    OperatingOrder = i.StockTransfer.OperatingOrder
                })
                .ToListAsync();

            var xfersMapped = xfers.Select(r =>
            {
                int fromWhId = r.FromWarehouseId ?? 0;
                int toWhId = r.ToWarehouseId ?? 0;

                return new RowShape
                {
                    Date = r.Date,
                    Type = "xfer",
                    Qty = r.Qty,
                    Price = r.Price,
                    From = (fromWhId > 0 && warehousesDict.TryGetValue(fromWhId, out var fromWh)) ? fromWh : "مخزن",
                    To = (toWhId > 0 && warehousesDict.TryGetValue(toWhId, out var toWh)) ? toWh : "مخزن",
                    OperatingOrder = r.OperatingOrder ?? string.Empty
                };
            }).ToList();

            // ===================== 4) الرصيد الافتتاحي (إضافة - صرف) فقط =====================
            var beIns = await _context.StockInVoucherItems
                .AsNoTracking()
                .Include(i => i.StockInVoucher)
                .Where(i =>
                    i.StockInVoucher.TransferDate < startDate &&
                    (!productId.HasValue || i.ProductId == productId) &&
                    (string.IsNullOrEmpty(colorCode) || i.ColorCode == colorCode)
                )
                .ToListAsync();

            var beOuts = await _context.StockOutVoucherItems
                .AsNoTracking()
                .Include(i => i.StockOutVoucher)
                .Where(i =>
                    i.StockOutVoucher.TransferDate < startDate &&
                    (!productId.HasValue || i.ProductId == productId) &&
                    (string.IsNullOrEmpty(colorCode) || i.ColorCode == colorCode)
                )
                .ToListAsync();

            int openingQty = beIns.Sum(i => i.Quantity) - beOuts.Sum(i => (int)i.Quantity);
            decimal openingCost = beIns.Sum(i => i.Quantity * i.Price) - beOuts.Sum(i => i.Quantity * i.Price);

            // ===================== 5) دمج وترتيب =====================
            var unified = insMapped.Concat(outsMapped).Concat(xfersMapped)
                                   .OrderBy(x => x.Date)
                                   .ToList();

            // ===================== 6) جري الرصيد — التحويل لا يغيّر الرصيد =====================
            var rows = new List<StockReportRow>();
            int runningQty = openingQty;
            decimal runningCost = openingCost;

            foreach (var r in unified)
            {
                decimal unitPrice = r.Type == "xfer" ? 0m : r.Price;
                decimal totalCost = unitPrice * r.Qty;

                if (r.Type == "in")
                {
                    runningQty += r.Qty;
                    runningCost += totalCost;
                }
                else if (r.Type == "out")
                {
                    runningQty -= r.Qty;
                    runningCost -= totalCost;
                }
                // xfer → بدون تأثير على الرصيد

                rows.Add(new StockReportRow
                {
                    TransferDate = r.Date,
                    VoucherType = r.Type == "in" ? "سند إضافة"
                                     : r.Type == "out" ? "سند صرف"
                                                       : "تحويل مخزني",
                    From = r.From,
                    To = r.To,
                    OperatingOrder = r.OperatingOrder,
                    Quantity = r.Qty,
                    UnitPrice = unitPrice,
                    TotalCost = totalCost,
                    BalanceQty = runningQty,
                    BalanceCost = runningCost
                });
            }

            return Ok(new
            {
                Opening = new { Quantity = openingQty, Cost = openingCost },
                Data = rows
            });
        }

        // شكل داخلي للتجميع قبل بناء صف التقرير النهائي
        private class RowShape
        {
            public DateTime Date { get; set; }
            public string Type { get; set; } = "";
            public int Qty { get; set; }
            public decimal Price { get; set; }
            public string From { get; set; } = "";
            public string To { get; set; } = "";
            public string OperatingOrder { get; set; } = "";
        }
    }

    // DTO النهائي الذي تُعيده الدالة للواجهة
    public class StockReportRow
    {
        public DateTime TransferDate { get; set; }
        public string VoucherType { get; set; } = "";
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string OperatingOrder { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalCost { get; set; }
        public int BalanceQty { get; set; }
        public decimal BalanceCost { get; set; }
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Controllers/StockReportController.cs
    }
}
