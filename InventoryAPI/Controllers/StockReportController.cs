using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InventoryAPI.Controllers
{
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


    }
}
