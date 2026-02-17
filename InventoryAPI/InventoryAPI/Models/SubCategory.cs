using System.Text.Json.Serialization;

namespace InventoryAPI.Models
{
    public class SubCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // ✅ منع الـ null
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}
