// Models/StockInVoucher.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryAPI.Models
{
    public class StockInVoucher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SequenceNumber { get; set; }

        [Required]
        public DateTime TransferDate { get; set; } = DateTime.UtcNow;

        public string? WarehouseKeeperName { get; set; }
        public string? OperatingOrder { get; set; }
        public string? Notes { get; set; }

        public List<StockInVoucherItem> Items { get; set; } = new();
    }

    public class StockInVoucherItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StockInVoucherId { get; set; }
        public StockInVoucher? StockInVoucher { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        [Required]
        public int WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        [Required]
        public int Quantity { get; set; }

        // السعر المجمد عند الإنشاء (يُمرَّر من الـ frontend أو يُحسَب هنا)
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        // التكلفة المحسوبة والمجمدة
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }

        // متبقي لاستقطاع FIFO
        [Required]
        public int RemainingQuantity { get; set; }

        public string? Unit { get; set; } = "حبة";
        public string? ColorCode { get; set; } = "";
    }
}
