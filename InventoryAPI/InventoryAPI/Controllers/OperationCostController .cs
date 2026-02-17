// Controllers/OperationCostController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using InventoryAPI.Data;
using InventoryAPI.Models;

[ApiController]
[Route("api/[controller]")]
public class OperationCostController : ControllerBase
{
    private readonly InventoryDbContext _context;
    public OperationCostController(InventoryDbContext context)
    {
        _context = context;
    }

    // ✅ تصنيف مرن بدل المساواة الحرفية
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

    [HttpGet("preview/{orderNumber}")]
    public async Task<IActionResult> Preview(int orderNumber)
    {
        var salesOrder = await _context.SalesOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        if (salesOrder == null)
            return NotFound();

        var existingOp = await _context.OperationCosts
            .Include(oc => oc.Items)
            .FirstOrDefaultAsync(oc => oc.OrderNumber == orderNumber);

        var finalProducts = await _context.FinalProducts
            .Include(fp => fp.Components)
            .Include(fp => fp.IndirectCosts)
            .ToListAsync();

        var items = salesOrder.Items.Select(si =>
        {
            var fp = finalProducts.FirstOrDefault(
                f => f.Code == si.OrderCode || f.Name == si.OrderName
            );
            if (fp == null) return null;

            decimal qty = si.Quantity;

            decimal materialsCost = fp.Components.Sum(c => (decimal)c.UnitCost) * qty;

            // ✅ بدل 4 حسابات ثابتة — نجمع حسب MainClassification مرن
            decimal wagesCost = fp.IndirectCosts
                .Where(ic => IsWages(ic.MainClassification))
                .Sum(ic => (decimal)ic.UnitCost) * qty;

            decimal extraCost = fp.IndirectCosts
                .Where(ic => IsExtraOperating(ic.MainClassification))
                .Sum(ic => (decimal)ic.UnitCost) * qty;

            decimal salesCost = fp.IndirectCosts
                .Where(ic => IsSalesMarketing(ic.MainClassification))
                .Sum(ic => (decimal)ic.UnitCost) * qty;

            decimal adminCost = fp.IndirectCosts
                .Where(ic => IsAdmin(ic.MainClassification))
                .Sum(ic => (decimal)ic.UnitCost) * qty;

            decimal totalOp = materialsCost + wagesCost + extraCost;
            decimal totalAll = totalOp + salesCost + adminCost;

            var existingItem = existingOp?.Items
                .FirstOrDefault(x => x.ProductCode == fp.Code && x.ProductName == fp.Name);

            return new OperationCostItem
            {
                Id = existingItem?.Id ?? 0,
                ProductCode = fp.Code,
                ProductName = fp.Name,
                Quantity = qty,
                MaterialsCost = materialsCost,
                WagesCost = wagesCost,
                AdditionalOperatingCost = extraCost,
                TotalOperatingCost = totalOp,
                SalesMarketingCost = salesCost,
                AdminCost = adminCost,
                TotalProductCost = totalAll,
                Status = existingItem?.Status ?? "غير مكتمل"
            };
        })
        .Where(i => i != null)
        .ToList();

        return Ok(new
        {
            orderNumber = salesOrder.OrderNumber,
            orderDate = salesOrder.CreationDate,
            orderStatus = existingOp?.OrderStatus ?? "غير مكتمل",
            items
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOperationCostDto dto)
    {
        if (await _context.OperationCosts.AnyAsync(o => o.OrderNumber == dto.OrderNumber))
            return Conflict($"Order {dto.OrderNumber} already exists.");

        var op = new OperationCost
        {
            OrderNumber = dto.OrderNumber,
            OrderDate = dto.OrderDate == default ? DateTime.UtcNow : dto.OrderDate,
            OrderStatus = dto.OrderStatus,
            Items = dto.Items.Select(i => new OperationCostItem
            {
                ProductCode = i.ProductCode,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                MaterialsCost = i.MaterialsCost,
                WagesCost = i.WagesCost,
                AdditionalOperatingCost = i.AdditionalOperatingCost,
                TotalOperatingCost = i.TotalOperatingCost,
                SalesMarketingCost = i.SalesMarketingCost,
                AdminCost = i.AdminCost,
                TotalProductCost = i.TotalProductCost,
                Status = i.Status
            }).ToList()
        };

        _context.OperationCosts.Add(op);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(Preview), new { orderNumber = op.OrderNumber }, null);
    }

    [HttpPut("{orderNumber}")]
    public async Task<IActionResult> UpsertOperationCost(int orderNumber, [FromBody] UpdateOperationCostDto dto)
    {
        var op = await _context.OperationCosts
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        if (op == null)
        {
            op = new OperationCost
            {
                OrderNumber = orderNumber,
                OrderDate = DateTime.UtcNow,
                OrderStatus = dto.OrderStatus,
                Items = new System.Collections.Generic.List<OperationCostItem>()
            };
            _context.OperationCosts.Add(op);
        }
        else
        {
            op.OrderStatus = dto.OrderStatus;
        }

        foreach (var itemDto in dto.Items)
        {
            var existing = op.Items.FirstOrDefault(i => i.Id == itemDto.Id && i.Id > 0);
            if (existing != null)
            {
                existing.Status = itemDto.Status;
            }
            else
            {
                op.Items.Add(new OperationCostItem
                {
                    ProductCode = itemDto.ProductCode,
                    ProductName = itemDto.ProductName,
                    Quantity = itemDto.Quantity,
                    MaterialsCost = itemDto.MaterialsCost,
                    WagesCost = itemDto.WagesCost,
                    AdditionalOperatingCost = itemDto.AdditionalOperatingCost,
                    TotalOperatingCost = itemDto.TotalOperatingCost,
                    SalesMarketingCost = itemDto.SalesMarketingCost,
                    AdminCost = itemDto.AdminCost,
                    TotalProductCost = itemDto.TotalProductCost,
                    Status = itemDto.Status
                });
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{orderNumber}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int orderNumber, [FromBody] string status)
    {
        var op = await _context.OperationCosts.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        if (op == null) return NotFound();

        op.OrderStatus = status;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{orderNumber}/item/{itemId}/status")]
    public async Task<IActionResult> UpdateItemStatus(int orderNumber, int itemId, [FromBody] string status)
    {
        var op = await _context.OperationCosts
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        if (op == null) return NotFound();

        var item = op.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return NotFound();

        item.Status = status;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{orderNumber}/item/{itemId}")]
    public async Task<IActionResult> DeleteItem(int orderNumber, int itemId)
    {
        var op = await _context.OperationCosts
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        var item = op?.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return NotFound($"Item {itemId} not found for order {orderNumber}.");

        _context.OperationCostItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{orderNumber}")]
    public async Task<IActionResult> DeleteOrder(int orderNumber)
    {
        var op = await _context.OperationCosts
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        if (op == null) return NotFound($"OperationCost {orderNumber} not found.");

        _context.OperationCostItems.RemoveRange(op.Items);
        _context.OperationCosts.Remove(op);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
