<<<<<<< HEAD:InventoryAPI/Models/OperationOrders.cs
﻿// Models/OperationOrder.cs
=======
﻿// Models/OperationOrders.cs
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Models/OperationOrders.cs
namespace InventoryAPI.Models
{
    public class OperationOrders
    {
        public int Id { get; set; }
<<<<<<< HEAD:InventoryAPI/Models/OperationOrders.cs
        public int OrderNumber { get; set; } // نفس orderNumber في أمر البيع
        public DateTime CreationDate { get; set; }
        public string CustomerName { get; set; }
=======
        public int OrderNumber { get; set; }
        public DateTime CreationDate { get; set; }
        public string CustomerName { get; set; } = "";
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Models/OperationOrders.cs
        public DateTime ExpirationDate { get; set; }

        public List<OperationOrderItem> Items { get; set; } = new();
    }

    public class OperationOrderItem
    {
        public int Id { get; set; }
<<<<<<< HEAD:InventoryAPI/Models/OperationOrders.cs
        public int OperationOrderId { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public string Unit { get; set; }
=======

        // FK
        public int OperationOrderId { get; set; }

        // Navigation
        public OperationOrders OperationOrder { get; set; } = null!;

        public string ProductName { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public string Unit { get; set; } = "";
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Models/OperationOrders.cs
        public int Quantity { get; set; }
        public double ProductionDurationHours { get; set; }

        public double TotalProductionHours => Quantity * ProductionDurationHours;
    }
}
