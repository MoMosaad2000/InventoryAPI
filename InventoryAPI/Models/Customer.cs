namespace InventoryAPI.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string? TaxNumber { get; set; }
        public string? Address { get; set; }
        public string? DeliveryLocation { get; set; }
        public string? Email { get; set; }
        //public string? PaymentTerms { get; set; }
        //public string PaymentMethod { get; set; } = string.Empty;

        public List<SalesOrder> SalesOrders { get; set; } = new();
        public List<StockOutVoucher> StockOutVouchers { get; set; } = new();
    }
}
