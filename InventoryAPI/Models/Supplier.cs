using InventoryAPI.Models;
namespace InventoryAPI.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;

        // ✅ التأكد من صحة العلاقة
        public List<StockInVoucher> StockInVouchers { get; set; } = new List<StockInVoucher>();
    }

}
