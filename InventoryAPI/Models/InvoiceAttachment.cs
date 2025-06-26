using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryAPI.Models
{
    public class InvoiceAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PurchaseInvoiceId { get; set; }

        [ForeignKey("PurchaseInvoiceId")]
        public PurchaseInvoice? PurchaseInvoice { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }
}
