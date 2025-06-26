// Models/OperationOrder.cs
namespace InventoryAPI.Models
{
    public class OperationOrders
    {
        public int Id { get; set; }
        public int OrderNumber { get; set; } // نفس orderNumber في أمر البيع
        public DateTime CreationDate { get; set; }
        public string CustomerName { get; set; }
        public DateTime ExpirationDate { get; set; }

        public List<OperationOrderItem> Items { get; set; } = new();
    }

    public class OperationOrderItem
    {
        public int Id { get; set; }
        public int OperationOrderId { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public string Unit { get; set; }
        public int Quantity { get; set; }
        public double ProductionDurationHours { get; set; }

        public double TotalProductionHours => Quantity * ProductionDurationHours;
    }
}
