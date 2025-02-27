using System.Text.Json.Serialization;

namespace InventoryAPI.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // ✅ منع الـ null
        public ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
    }

}
