// ✅ Models/SalesOrder.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryAPI.Models
{
    public class SalesOrder
    {
        public int Id { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public string PaymentTerms { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public string RepresentativeName { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public string Notes { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }
        public decimal TotalWithTax { get; set; }
        public int OrderNumber { get; set; }

        public List<SalesOrderItem> Items { get; set; } = new();
    }

    public class SalesOrderItem
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }
        public SalesOrder? SalesOrder { get; set; }

        public string Notes { get; set; } = string.Empty;
        public string OrderName { get; set; } = string.Empty;
        public string OrderCode { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string? Drawing { get; set; } // ✅ احفظ الصورة كـ base64 string

    }
}