using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OperationTotalCostReportController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        public OperationTotalCostReportController(InventoryDbContext context)
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

        [HttpGet("aggregate")]
        public async Task<ActionResult<OperationCostAggregateDto>> GetAggregate(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] string status = "الكل")
        {
            var s = (status ?? "الكل").Trim().ToLowerInvariant();
            var normalized = "الكل";

            if (s == "completed" || s == "done" || s == "complete" || s == "مكتمل")
                normalized = "مكتمل";
            else if (s == "pending" || s == "incomplete" || s == "notcompleted" || s == "غير مكتمل" || s == "غيرمكتمل")
                normalized = "غير مكتمل";
            else if (s == "all" || s == "الكل")
                normalized = "الكل";

            var salesQuery = _context.SalesOrders
                .Include(so => so.Items)
                .Where(so => so.CreationDate.Date >= from.Date && so.CreationDate.Date <= to.Date);

            if (normalized == "مكتمل")
            {
                salesQuery = salesQuery.Where(so =>
                    _context.OperationCosts.Any(oc =>
                        oc.OrderNumber == so.OrderNumber &&
                        oc.OrderStatus != null &&
                        oc.OrderStatus.Trim() == "مكتمل"
                    ));
            }
            else if (normalized == "غير مكتمل")
            {
                salesQuery = salesQuery.Where(so =>
                    !_context.OperationCosts.Any(oc =>
                        oc.OrderNumber == so.OrderNumber &&
                        oc.OrderStatus != null &&
                        oc.OrderStatus.Trim() == "مكتمل"
                    ));
            }

            var salesOrders = await salesQuery.ToListAsync();

            var finalProducts = await _context.FinalProducts
                .Include(fp => fp.Components)
                .Include(fp => fp.IndirectCosts)
                .ToListAsync();

            var rows = salesOrders.Select((so, idx) =>
            {
                decimal direct = 0m, indirect = 0m, period = 0m;

                foreach (var si in so.Items)
                {
                    var fp = finalProducts.FirstOrDefault(f => f.Code == si.OrderCode || f.Name == si.OrderName);
                    if (fp == null) continue;

                    var qty = (decimal)si.Quantity;

                    direct += (decimal)fp.Components.Sum(c => c.UnitCost) * qty;

                    var wages = fp.IndirectCosts.Where(ic => IsWages(ic.MainClassification)).Sum(ic => ic.UnitCost);
                    var extra = fp.IndirectCosts.Where(ic => IsExtraOperating(ic.MainClassification)).Sum(ic => ic.UnitCost);
                    indirect += (decimal)(wages + extra) * qty;

                    var sales = fp.IndirectCosts.Where(ic => IsSalesMarketing(ic.MainClassification)).Sum(ic => ic.UnitCost);
                    var admin = fp.IndirectCosts.Where(ic => IsAdmin(ic.MainClassification)).Sum(ic => ic.UnitCost);
                    period += (decimal)(sales + admin) * qty;
                }

                var total = direct + indirect + period;

                return new OperationCostAggregateItemDto
                {
                    Serial = idx + 1,
                    OrderNumber = so.OrderNumber,
                    OrderDate = so.CreationDate,
                    DirectCost = direct,
                    IndirectCost = indirect,
                    PeriodCost = period,
                    TotalCost = total
                };
            }).ToList();

            var dto = new OperationCostAggregateDto
            {
                FromDate = from,
                ToDate = to,
                Status = normalized,
                Items = rows,
                TotalDirectCost = rows.Sum(r => r.DirectCost),
                TotalIndirectCost = rows.Sum(r => r.IndirectCost),
                TotalPeriodCost = rows.Sum(r => r.PeriodCost),
                TotalCost = rows.Sum(r => r.TotalCost)
            };

            return Ok(dto);
        }
    }
}
