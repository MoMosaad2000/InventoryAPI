// Models/OperationCost.cs
using System;
using System.Collections.Generic;

namespace InventoryAPI.Models
{
    public class OperationCost
    {
        public int Id { get; set; }
        public int OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; }
        public ICollection<OperationCostItem> Items { get; set; }
    }

    public class OperationCostItem
    {
        public int Id { get; set; }
        public int OperationCostId { get; set; }
        public OperationCost OperationCost { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal MaterialsCost { get; set; }
        public decimal WagesCost { get; set; }
        public decimal AdditionalOperatingCost { get; set; }
        public decimal TotalOperatingCost { get; set; }
        public decimal SalesMarketingCost { get; set; }
        public decimal AdminCost { get; set; }
        public decimal TotalProductCost { get; set; }
        public string Status { get; set; }
    }


    public class UpdateOperationCostDto
    {
        public string OrderStatus { get; set; }
        public List<UpdateCostItemDto> Items { get; set; }
    }


public class UpdateCostItemDto
{
    public int Id { get; set; }  // zero for brand-new items
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal MaterialsCost { get; set; }
    public decimal WagesCost { get; set; }
    public decimal AdditionalOperatingCost { get; set; }
    public decimal TotalOperatingCost { get; set; }
    public decimal SalesMarketingCost { get; set; }
    public decimal AdminCost { get; set; }
    public decimal TotalProductCost { get; set; }
    public string Status { get; set; }
}

public class CreateOperationCostDto
    {
        public int OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; }
        public List<CreateItemDto> Items { get; set; }
    }
    public class CreateItemDto
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal MaterialsCost { get; set; }
        public decimal WagesCost { get; set; }
        public decimal AdditionalOperatingCost { get; set; }
        public decimal TotalOperatingCost { get; set; }
        public decimal SalesMarketingCost { get; set; }
        public decimal AdminCost { get; set; }
        public decimal TotalProductCost { get; set; }
        public string Status { get; set; }
}


   
}
