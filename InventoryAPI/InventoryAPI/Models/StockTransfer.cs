using InventoryAPI.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class StockTransfer
{
    public int Id { get; set; }
    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }
    public string? WarehouseKeeperName { get; set; }
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public string? OperatingOrder { get; set; }
    public string? Notes { get; set; }

    public List<StockTransferItem> Items { get; set; } = new();
}

public class StockTransferItem
{
    public int Id { get; set; }

    public int StockTransferId { get; set; }
    public StockTransfer? StockTransfer { get; set; }

    // ده فعلاً nullable ومش محتاج تعديل
    public int? StockInVoucherId { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    // المستودع المصدر — للتوثيق فقط
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public string Unit { get; set; } = "حبة";
    public int Quantity { get; set; }
    public string? ColorCode { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [NotMapped]
    public decimal TotalCost => Quantity * Price;
}
// === DTO لصف التقرير ===
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
}