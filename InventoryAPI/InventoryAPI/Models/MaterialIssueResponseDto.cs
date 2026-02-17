namespace InventoryAPI.Models
{
    public class MaterialIssueResponseDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? OperatingOrderNumber { get; set; }
        public int FromWarehouseId { get; set; }
        public int ToWarehouseId { get; set; }

        public int? DepartmentCategoryId { get; set; }
        public string? RequesterName { get; set; }

        public string? Notes { get; set; }
        public string Status { get; set; } = "";

        public bool HasRequesterSignature { get; set; }
        public string? RequesterSignatureUrl { get; set; }

        public bool HasStoreKeeperSignature { get; set; }
        public string? StoreKeeperSignatureUrl { get; set; }

        public List<MaterialIssueLineResponseDto> Lines { get; set; } = new();
    }

    public class MaterialIssueLineResponseDto
    {
        public int LineNo { get; set; }
        public int ProductId { get; set; }

        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Unit { get; set; } = "";
        public string ColorCode { get; set; } = "";

        public int RequestedQty { get; set; }
        public int AvailableQtyAtTime { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal TotalCost { get; set; }

        public string? Notes { get; set; }
    }
}
