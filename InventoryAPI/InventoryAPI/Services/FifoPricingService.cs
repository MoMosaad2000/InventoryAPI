using InventoryAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Services
{
    public class FifoPricingService : IFifoPricingService
    {
        private readonly InventoryDbContext _context;

        public FifoPricingService(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetFifoUnitPriceAsync(int productId, decimal requestedQty)
        {
            // الصرف السابق من نفس نظام MaterialIssues
            var alreadyIssued = await _context.MaterialIssueLines
                .Where(x => x.ProductId == productId)
                .SumAsync(x => (decimal?)x.RequestedQty) ?? 0m;

            // فواتير الشراء (الأقدم أولاً)
            var invoiceItems = await _context.PurchaseInvoiceItems
                .Include(x => x.PurchaseInvoice)
                .Where(x => x.ProductId == productId)
                .OrderBy(x => x.PurchaseInvoice.InvoiceDate)
                .ThenBy(x => x.Id)
                .Select(x => new { x.Quantity, x.Price })
                .ToListAsync();

            if (!invoiceItems.Any())
                return 0m;

            decimal remainingSkip = alreadyIssued;
            decimal remainingTake = requestedQty;

            decimal totalCost = 0m;
            decimal totalTaken = 0m;

            foreach (var it in invoiceItems)
            {
                decimal lotQty = Convert.ToDecimal(it.Quantity);
                decimal lotPrice = Convert.ToDecimal(it.Price);

                if (remainingSkip > 0)
                {
                    var skip = Math.Min(remainingSkip, lotQty);
                    lotQty -= skip;
                    remainingSkip -= skip;
                }

                if (lotQty <= 0) continue;
                if (remainingTake <= 0) break;

                var take = Math.Min(remainingTake, lotQty);
                totalCost += take * lotPrice;
                totalTaken += take;
                remainingTake -= take;
            }

            if (totalTaken <= 0) return 0m;

            return totalCost / totalTaken;
        }
    }
}
