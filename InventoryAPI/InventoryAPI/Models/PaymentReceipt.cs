using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryAPI.Models
{
    public class PaymentReceipt
    {
        public int Id { get; set; }

        // 1-1 with Quotation
        public int QuotationId { get; set; }
        public Quotation? Quotation { get; set; }

        // Header dotted fields
        [MaxLength(250)] public string FactoryName { get; set; } = "";
        [MaxLength(300)] public string FactoryAddress { get; set; } = "";
        [MaxLength(50)] public string FactoryPhone { get; set; } = "";
        [MaxLength(80)] public string FactoryCR { get; set; } = "";
        [MaxLength(80)] public string FactoryVAT { get; set; } = "";
        [MaxLength(60)] public string ReceiptNumber { get; set; } = "";

        public DateTime? ReceiptDate { get; set; }

        // Received from
        [MaxLength(250)] public string ReceivedFromName { get; set; } = "";
        [MaxLength(120)] public string ReceivedFromId { get; set; } = "";

        // Amount
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } = 0;

        [MaxLength(400)]
        public string AmountInWords { get; set; } = "";

        // Payment method
        public bool PayCash { get; set; }
        public bool PayCheck { get; set; }
        public bool PayMada { get; set; }
        public bool PayTransfer { get; set; }

        [MaxLength(200)] public string BankName { get; set; } = "";
        [MaxLength(120)] public string CheckNumber { get; set; } = "";

        // Payment for
        public bool PayForDownPayment { get; set; }
        public bool PayForReady { get; set; }
        public bool PayForStage { get; set; }
        public bool PayForFinal { get; set; }
        public bool PayForQuotation { get; set; } = true;
        public bool PayForSalesOrder { get; set; }
        public bool PayForOther { get; set; }

        [MaxLength(200)]
        public string PayForRef { get; set; } = "";

        // Receiver
        [MaxLength(200)] public string ReceiverName { get; set; } = "";
        [MaxLength(200)] public string ReceiverJob { get; set; } = "";

        // Signature as DataURL
        public string? SignatureDataUrl { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
