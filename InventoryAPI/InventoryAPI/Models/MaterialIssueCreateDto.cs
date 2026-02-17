using System.ComponentModel.DataAnnotations;

namespace InventoryAPI.Models
{
    public class MaterialIssueCreateDto
    {
        public int? OperatingOrderNumber { get; set; }

        // ⚠️ في الـ Update هنثبت FromWarehouseId من الداتابيز ومش هنسمح بتغييره
        public int FromWarehouseId { get; set; }

        public int ToWarehouseId { get; set; }

        public int? DepartmentCategoryId { get; set; }

        [MaxLength(150)]
        public string? RequesterName { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// ممكن تبعتها DataURL أو base64 خام.
        /// في الـ UPDATE لو بعتها "" => يمسح التوقيع
        /// لو null => ما يغيرش التوقيع
        /// </summary>
        public string? RequesterSignatureBase64 { get; set; }

        /// <summary>
        /// ممكن تبعتها DataURL أو base64 خام.
        /// في الـ UPDATE لو بعتها "" => يمسح التوقيع
        /// لو null => ما يغيرش التوقيع
        /// </summary>
        public string? StoreKeeperSignatureBase64 { get; set; }

        [Required]
        public List<MaterialIssueLineCreateDto> Lines { get; set; } = new();
    }

    public class MaterialIssueLineCreateDto
    {
        public int ProductId { get; set; }
        public int RequestedQty { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
