namespace InventoryAPI.Models
{
    public class PaymentReceiptUpsertDto
    {
        public string? FactoryName { get; set; }
        public string? FactoryAddress { get; set; }
        public string? FactoryPhone { get; set; }
        public string? FactoryCR { get; set; }
        public string? FactoryVAT { get; set; }
        public string? ReceiptNumber { get; set; }

        // استقبلها كنص عشان متكسرش ModelBinding
        public string? ReceiptDate { get; set; } // "2026-02-08" أو ISO

        public string? ReceivedFromName { get; set; }
        public string? ReceivedFromId { get; set; }

        public string? Amount { get; set; } // "500" / "500.00"
        public string? AmountInWords { get; set; }

        public bool PayCash { get; set; }
        public bool PayCheck { get; set; }
        public bool PayMada { get; set; }
        public bool PayTransfer { get; set; }

        public string? BankName { get; set; }
        public string? CheckNumber { get; set; }

        public bool PayForDownPayment { get; set; }
        public bool PayForReady { get; set; }
        public bool PayForStage { get; set; }
        public bool PayForFinal { get; set; }
        public bool PayForQuotation { get; set; }
        public bool PayForSalesOrder { get; set; }
        public bool PayForOther { get; set; }

        public string? PayForRef { get; set; }

        public string? ReceiverName { get; set; }
        public string? ReceiverJob { get; set; }

        public string? SignatureDataUrl { get; set; }
    }
}
