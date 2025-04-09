using System.Text.Json.Serialization;

namespace InventoryAPI.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Unit { get; set; } = "حبة";
        public int SubCategoryId { get; set; }
        public string? ColorCode { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; } = 0;
        public SubCategory? SubCategory { get; set; }

        [JsonIgnore]  // ← هذه الخاصية قد تسبب دورة لذا يتم تجاهلها أثناء السيريالايز
        public Warehouse? Warehouse { get; set; }
    }

    public class AddProductRequest
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; }
    }
}
