// Models/StockOutVoucher.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryAPI.Models
{
    public class StockOutVoucher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime TransferDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public string? WarehouseKeeperName { get; set; }
        public string? OperatingOrder { get; set; }
        public string? Notes { get; set; }

        public List<StockOutVoucherItem> Items { get; set; } = new();
    }

    public class StockOutVoucherItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StockOutVoucherId { get; set; }
        public StockOutVoucher? StockOutVoucher { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        public int WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }

        public string? ColorCode { get; set; } = "";
    }
}
