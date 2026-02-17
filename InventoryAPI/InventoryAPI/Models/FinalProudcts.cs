<<<<<<< HEAD:InventoryAPI/Models/FinalProudcts.cs
﻿namespace InventoryAPI.Models
=======
﻿// Models/FinalProudcts.cs
using System.Collections.Generic;

namespace InventoryAPI.Models
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Models/FinalProudcts.cs
{
    public class FinalProduct
    {
        public int Id { get; set; }
<<<<<<< HEAD:InventoryAPI/Models/FinalProudcts.cs
        public string Name { get; set; }
        public string Code { get; set; }
        public int MainCategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public string Unit { get; set; }
        public int WarehouseId { get; set; }
        public string Description { get; set; }
=======
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public int MainCategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public string Unit { get; set; } = "";
        public int WarehouseId { get; set; }
        public string Description { get; set; } = "";
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Models/FinalProudcts.cs
        public string? ImagePath { get; set; }
        public double ProductionDurationHours { get; set; }

        public List<FinalProductComponent> Components { get; set; } = new();
        public List<IndirectCost> IndirectCosts { get; set; } = new();
    }

    public class FinalProductComponent
    {
        public int Id { get; set; }
<<<<<<< HEAD:InventoryAPI/Models/FinalProudcts.cs
        public int FinalProductId { get; set; }
        public int RawMaterialId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
=======
        public int? FinalProductId { get; set; }

        public int RawMaterialId { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Models/FinalProudcts.cs
        public int UnitId { get; set; }
        public double Quantity { get; set; }
        public double Price { get; set; }

<<<<<<< HEAD:InventoryAPI/Models/FinalProudcts.cs
=======
        // ✅ مهم جداً للـ OperationCost / التقارير
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Models/FinalProudcts.cs
        public double UnitCost => Quantity * Price;

        public FinalProduct? FinalProduct { get; set; }
    }

    public class IndirectCost
    {
        public int Id { get; set; }
        public int FinalProductId { get; set; }
<<<<<<< HEAD:InventoryAPI/Models/FinalProudcts.cs
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AllocationBasis { get; set; } = string.Empty;
        public double UnitCost { get; set; }
        public string MainClassification { get; set; } = string.Empty;
=======

        public string AccountCode { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string AllocationBasis { get; set; } = "";
        public double UnitCost { get; set; }
        public string MainClassification { get; set; } = "";
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Models/FinalProudcts.cs

        public FinalProduct? FinalProduct { get; set; }
    }
}
