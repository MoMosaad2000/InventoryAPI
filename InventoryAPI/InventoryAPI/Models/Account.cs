using System.ComponentModel.DataAnnotations;

namespace InventoryAPI.Models
{
    public class Account
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AccountCode { get; set; } = "";

        public string AccountName { get; set; } = "";

        public string? AllocationBasis { get; set; }
        public string? MainClassification { get; set; }
    }
}
