using System;
using System.Collections.Generic;

namespace InventoryAPI.Models
{
    public class OperationCostReportItemDto
    {
        public int Serial { get; set; }  // رقم تسلسلي
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public decimal DirectCost { get; set; }  // materialsCost
        public decimal IndirectCost { get; set; }  // wagesCost + additionalOperatingCost
        public decimal PeriodCost { get; set; }  // salesMarketingCost + adminCost
        public decimal TotalCost { get; set; }  // totalProductCost
    }

    public class OperationCostReportDto
    {
        public int OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OperationCostReportItemDto> Items { get; set; }

        // Footer totals
        public decimal TotalDirectCost { get; set; }
        public decimal TotalIndirectCost { get; set; }
        public decimal TotalPeriodCost { get; set; }
        public decimal TotalCost { get; set; }
    }
}
