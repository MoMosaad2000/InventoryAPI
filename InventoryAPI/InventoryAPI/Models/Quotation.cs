using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryAPI.Models
{
    public class Quotation
    {
        public int Id { get; set; }

        [MaxLength(50)]
        public string QuotationNumber { get; set; } = "";

        // Customer info (manual for now)
        [MaxLength(200)]
        public string CustomerName { get; set; } = "";

        [MaxLength(300)]
        public string CustomerAddress { get; set; } = "";

        [MaxLength(30)]
        public string CustomerMobile { get; set; } = "";

        [MaxLength(150)]
        public string RepresentativeName { get; set; } = "";

        public DateTime? InstallationDate { get; set; }

        [MaxLength(500)]
        public string PaymentTerms { get; set; } = "";

        [MaxLength(200)]
        public string PaymentMethod { get; set; } = "";

        public string? Notes { get; set; }

        // New fields
        [Column(TypeName = "decimal(18,2)")]
        public decimal DownPayment { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxRate { get; set; } = 0.15m;

        // Totals
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDiscount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAfterDiscount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxTotal { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalWithTax { get; set; } = 0;

        public string? SignatureDataUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public List<QuotationLine> Lines { get; set; } = new();

        // 1-1 receipt
        public PaymentReceipt? PaymentReceipt { get; set; }
    }

    public class QuotationLine
    {
        public int Id { get; set; }

        public int QuotationId { get; set; }
        public Quotation? Quotation { get; set; }

        [MaxLength(50)]
        public string ProductCode { get; set; } = "";

        [MaxLength(200)]
        public string ProductName { get; set; } = "";

        [MaxLength(50)]
        public string Unit { get; set; } = "";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineSubtotal { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTax { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotalWithTax { get; set; } = 0;
    }
}
