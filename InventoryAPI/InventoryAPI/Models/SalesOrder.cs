// ✅ Models/SalesOrder.cs
using System;
using System.Collections.Generic;

namespace InventoryAPI.Models
{
    public class SalesOrder
    {
        public int Id { get; set; }

        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        // ✅ رقم أمر البيع (Sequence)
        public int OrderNumber { get; set; }

        // ✅ ربط أمر البيع بعرض سعر (اختياري)
        public string? QuotationNumber { get; set; }

        public string RepresentativeName { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public string Notes { get; set; } = string.Empty;

        public string PaymentTerms { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }
        public decimal TotalWithTax { get; set; }

        // ✅ العميل: ممكن يكون من قاعدة بياناتنا أو مجرد Snapshot
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // Snapshot fields (لو العميل اتكتب يدوي أو جاي من نظام خارجي)
        public string? CustomerName { get; set; }
        public string? CustomerExternalCode { get; set; }

        // ✅ Snapshot contact fields (عشان الفاتورة ما تطلعش فاضية)
        public string? CustomerMobile { get; set; }
        public string? CustomerAddress { get; set; }

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

        // ✅ احفظ الرسم/الصورة Base64 (اختياري)
        public string? Drawing { get; set; }
    }
}
