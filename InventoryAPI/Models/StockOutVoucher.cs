using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace InventoryAPI.Models
{
    public class StockOutVoucher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime TransferDate { get; set; } = DateTime.UtcNow;

        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public string? WarehouseKeeperName { get; set; }

        // الحقل الجديد: أمر التشغيل
        public string? OperatingOrder { get; set; }

        // الحقل الجديد: الملاحظات (يمكن أن تكون فارغة)
        public string? Notes { get; set; }

        public List<StockOutVoucherItem> Items { get; set; } = new List<StockOutVoucherItem>();
    
    }

    public class StockOutVoucherItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StockOutVoucherId { get; set; }

        [ForeignKey("StockOutVoucherId")]
        public StockOutVoucher? StockOutVoucher { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        [ForeignKey("WarehouseId")]
        public Warehouse? Warehouse { get; set; }

        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        [Required]
        public int Quantity { get; set; }

        public decimal Price { get; set; }
        public decimal Tax { get; set; } = 0;
        public decimal Discount { get; set; } = 0;

        // الحقل الجديد: كود اللون
        public string? ColorCode { get; set; }
    }
}
