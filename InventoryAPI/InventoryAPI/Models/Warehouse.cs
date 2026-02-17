using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryAPI.Models
{
    public class Warehouse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<WarehouseStock> Stocks { get; set; } = new List<WarehouseStock>();
    }
    public class WarehouseStock
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; }
    }
}
