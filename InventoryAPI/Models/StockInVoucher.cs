using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryAPI.Models
{
    public class StockInVoucher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime TransferDate { get; set; } = DateTime.UtcNow;

        public string? WarehouseKeeperName { get; set; }
        public string? Notes { get; set; }

        public List<StockInVoucherItem> Items { get; set; } = new List<StockInVoucherItem>();
    }
    public class StockInVoucherItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StockInVoucherId { get; set; }

        [ForeignKey("StockInVoucherId")]
        public StockInVoucher? StockInVoucher { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required]
        public int SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        [ForeignKey("WarehouseId")]
        public Warehouse? Warehouse { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal Price { get; set; }

        public decimal Tax { get; set; } = 0;
        public decimal Discount { get; set; } = 0;

        // يتم احتساب التكلفة الإجمالية وفقاً للصيغة التالية:
        public decimal TotalCost => (Quantity * Price) + (Quantity * Price * Tax / 100) - Discount;
    }
}
