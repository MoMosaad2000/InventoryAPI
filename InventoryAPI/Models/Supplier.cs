using InventoryAPI.Models;
using System.ComponentModel.DataAnnotations;
namespace InventoryAPI.Models
{
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string ContactInfo { get; set; } = string.Empty;

        public string TaxNumber { get; set; } = string.Empty;
        public ICollection<StockInVoucher>? StockInVouchers { get; set; }

    }
}
