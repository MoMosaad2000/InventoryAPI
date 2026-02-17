<<<<<<< HEAD:InventoryAPI/Models/StockReportItem.cs
﻿namespace InventoryAPI.Models
{
    public class StockReportItem
    {
        public string VoucherName { get; set; } = "";
        public string VoucherNumber { get; set; } = "";
        public DateTime TransferDate { get; set; }
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
        public string OperatingOrder { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public string SupplierName { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string ColorCode { get; set; } = "";

        public decimal TotalCost { get; set; }
=======
﻿// Models/StockReportItem.cs
namespace InventoryAPI.Models
{
    public class StockReportItem
    {
        public int Id { get; set; }

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

        public string? SequenceNumber { get; set; }
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Models/StockReportItem.cs
    }
}
