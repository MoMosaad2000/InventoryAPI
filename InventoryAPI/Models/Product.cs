using System.Text.Json.Serialization;

namespace InventoryAPI.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public string Unit { get; set; } = "حبة";
        public int SubCategoryId { get; set; }
        public string? ColorCode { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; } = 0;
        public SubCategory? SubCategory { get; set; }
        [JsonIgnore]
        public Warehouse? Warehouse { get; set; }
    }

    public class AddProductRequest
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; }
    }
    public class ProductUploadDto
    {
        public string Product { get; set; } = string.Empty;
        public IFormFile? Image { get; set; }
    }
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Unit { get; set; } = "حبة";
        public string? ColorCode { get; set; }
        public int SubCategoryId { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; } = 0;
        public string? ImageUrl { get; set; }
        public string? SubCategoryName { get; set; }
        public string? WarehouseName { get; set; }
    }

}
