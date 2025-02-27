namespace InventoryAPI.Models
{
    public class StockTransfer
    {
        public int Id { get; set; }  // يُفترض أن يكون هذا العمود Auto Increment في قاعدة البيانات
        public int FromWarehouseId { get; set; }
        public int ToWarehouseId { get; set; }
        public string? WarehouseKeeperName { get; set; }
        public DateTime TransferDate { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public List<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
    }

    public class StockTransferItem
    {
        public int Id { get; set; }
        public int StockTransferId { get; set; } // تم تعديل الاسم ليطابق StockTransfer
        public StockTransfer? StockTransfer { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }
        public string Unit { get; set; } = "حبة";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalCost => Quantity * Price;
    }
}
