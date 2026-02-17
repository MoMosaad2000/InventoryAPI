using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OperationCostReportController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        public OperationCostReportController(InventoryDbContext context)
            => _context = context;

        private static string Normalize(string? s)
            => (s ?? "").Trim().ToLowerInvariant();

        private static bool IsWages(string? cls)
        {
            var s = Normalize(cls);
            return s.Contains("رواتب") || s.Contains("أجور") || s.Contains("اجور");
        }

        private static bool IsExtraOperating(string? cls)
        {
            var s = Normalize(cls);
            return s.Contains("تشغيل") || s.Contains("تشغيلية") || s.Contains("تشغيليه");
        }

        private static bool IsSalesMarketing(string? cls)
        {
            var s = Normalize(cls);
            return s.Contains("بيع") || s.Contains("تسويق") || s.Contains("الدعاية") || s.Contains("اعلان") || s.Contains("إعلان");
        }

        private static bool IsAdmin(string? cls)
        {
            var s = Normalize(cls);
            return s.Contains("عمومية") || s.Contains("ادارية") || s.Contains("إدارية") || s.Contains("إداري");
        }

        [HttpGet("report/{orderNumber}")]
        public async Task<ActionResult<OperationCostReportDto>> GetReport(int orderNumber)
        {
            var op = await _context.OperationCosts
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            List<OperationCostReportItemDto> itemDtos;
            DateTime orderDate;

            if (op != null)
            {
                orderDate = op.OrderDate;

                itemDtos = op.Items.Select(i => new OperationCostReportItemDto
                {
                    Serial = i.Id, // أو 0
                    ProductCode = i.ProductCode,
                    ProductName = i.ProductName,
                    DirectCost = i.MaterialsCost,
                    IndirectCost = i.WagesCost + i.AdditionalOperatingCost,
                    PeriodCost = i.SalesMarketingCost + i.AdminCost,
                    TotalCost = i.TotalProductCost
                }).ToList();
            }
            else
            {
                var salesOrder = await _context.SalesOrders
                    .Include(so => so.Items)
                    .FirstOrDefaultAsync(so => so.OrderNumber == orderNumber);

                if (salesOrder == null)
                    return NotFound($"Order {orderNumber} not found.");

                var finalProducts = await _context.FinalProducts
                    .Include(fp => fp.Components)
                    .Include(fp => fp.IndirectCosts)
                    .ToListAsync();

                var computed = salesOrder.Items
                    .Select(si =>
                    {
                        var fp = finalProducts.FirstOrDefault(f =>
                            f.Code == si.OrderCode || f.Name == si.OrderName
                        );
                        if (fp == null) return null;

                        decimal qty = (decimal)si.Quantity;

                        decimal matCost = fp.Components.Sum(c => (decimal)c.UnitCost) * qty;

                        decimal wageCost = fp.IndirectCosts
                            .Where(ic => IsWages(ic.MainClassification))
                            .Sum(ic => (decimal)ic.UnitCost) * qty;

                        decimal extraOpCost = fp.IndirectCosts
                            .Where(ic => IsExtraOperating(ic.MainClassification))
                            .Sum(ic => (decimal)ic.UnitCost) * qty;

                        decimal salesCost = fp.IndirectCosts
                            .Where(ic => IsSalesMarketing(ic.MainClassification))
                            .Sum(ic => (decimal)ic.UnitCost) * qty;

                        decimal adminCost = fp.IndirectCosts
                            .Where(ic => IsAdmin(ic.MainClassification))
                            .Sum(ic => (decimal)ic.UnitCost) * qty;

                        decimal direct = matCost;
                        decimal indirect = wageCost + extraOpCost;
                        decimal period = salesCost + adminCost;
                        decimal total = direct + indirect + period;

                        return new OperationCostReportItemDto
                        {
                            Serial = 0,
                            ProductCode = fp.Code,
                            ProductName = fp.Name,
                            DirectCost = direct,
                            IndirectCost = indirect,
                            PeriodCost = period,
                            TotalCost = total
                        };
                    })
                    .Where(x => x != null)
                    .ToList();

                itemDtos = computed!;
                orderDate = salesOrder.CreationDate;
            }

            // ✅ FIX الحقيقي: شيل الدوبليكيت (Distinct) من غير ما تجمع الأرقام
            var distinctItems = itemDtos
                .GroupBy(x => new { x.ProductCode, x.ProductName })
                .Select(g =>
                {
                    // خد أول سجل (مش Sum)
                    var first = g.First();
                    return new OperationCostReportItemDto
                    {
                        Serial = 0,
                        ProductCode = first.ProductCode,
                        ProductName = first.ProductName,
                        DirectCost = first.DirectCost,
                        IndirectCost = first.IndirectCost,
                        PeriodCost = first.PeriodCost,
                        TotalCost = first.TotalCost
                    };
                })
                .ToList();

            distinctItems = distinctItems.Select((dto, idx) =>
            {
                dto.Serial = idx + 1;
                return dto;
            }).ToList();

            var report = new OperationCostReportDto
            {
                OrderNumber = orderNumber,
                OrderDate = orderDate,
                Items = distinctItems,
                TotalDirectCost = distinctItems.Sum(x => x.DirectCost),
                TotalIndirectCost = distinctItems.Sum(x => x.IndirectCost),
                TotalPeriodCost = distinctItems.Sum(x => x.PeriodCost),
                TotalCost = distinctItems.Sum(x => x.TotalCost)
            };

            return Ok(report);
        }
    }
}
