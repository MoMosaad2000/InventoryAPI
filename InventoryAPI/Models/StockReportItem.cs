namespace InventoryAPI.Models
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
    }
}
