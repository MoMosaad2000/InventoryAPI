using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryAPI.Models
{
    public class MaterialIssueLine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int MaterialIssueId { get; set; }
        public MaterialIssue MaterialIssue { get; set; } = null!;

        public int LineNo { get; set; }

        public int ProductId { get; set; }

        [MaxLength(100)]
        public string ProductCode { get; set; } = "";

        [MaxLength(200)]
        public string ProductName { get; set; } = "";

        [MaxLength(50)]
        public string Unit { get; set; } = "";

        [MaxLength(50)]
        public string ColorCode { get; set; } = "";

        public int RequestedQty { get; set; }
        public decimal AvailableQtyAtTime { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal TotalCost { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
