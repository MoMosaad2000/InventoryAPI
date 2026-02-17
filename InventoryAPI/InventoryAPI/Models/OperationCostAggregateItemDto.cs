using System;
using System.Collections.Generic;

namespace InventoryAPI.Models
{
    public class OperationCostAggregateItemDto
    {
        public int Serial { get; set; }    // م
        public int OrderNumber { get; set; }    // رقم الأمر
        public DateTime OrderDate { get; set; }  // تاريخ الأمر
        public decimal DirectCost { get; set; }
        public decimal IndirectCost { get; set; }
        public decimal PeriodCost { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class OperationCostAggregateDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Status { get; set; }
        public List<OperationCostAggregateItemDto> Items { get; set; }

        public decimal TotalDirectCost { get; set; }
        public decimal TotalIndirectCost { get; set; }
        public decimal TotalPeriodCost { get; set; }
        public decimal TotalCost { get; set; }
    }
}
