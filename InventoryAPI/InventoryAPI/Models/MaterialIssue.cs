using System.ComponentModel.DataAnnotations;

namespace InventoryAPI.Models
{
    public class MaterialIssue
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? OperatingOrderNumber { get; set; }

        public int FromWarehouseId { get; set; }
        public Warehouse? FromWarehouse { get; set; }

        public int ToWarehouseId { get; set; }
        public Warehouse? ToWarehouse { get; set; }

        public int? DepartmentCategoryId { get; set; }

        [MaxLength(150)]
        public string? RequesterName { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "جارى التعميد";

        public byte[]? RequesterSignature { get; set; }
        public string? RequesterSignatureMimeType { get; set; }

        public byte[]? StoreKeeperSignature { get; set; }
        public string? StoreKeeperSignatureMimeType { get; set; }

        public List<MaterialIssueLine> Lines { get; set; } = new();
    }
}
