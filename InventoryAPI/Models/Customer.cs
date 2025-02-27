namespace InventoryAPI.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public List<StockOutVoucher> StockOutVouchers { get; set; } = new List<StockOutVoucher>();
    }

}
