<<<<<<< HEAD:InventoryAPI/Models/FinalProductCreateDTO.cs
﻿using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
=======
﻿// Models/FinalProductCreateDTO.cs
using Microsoft.AspNetCore.Http;
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Models/FinalProductCreateDTO.cs

namespace InventoryAPI.Models
{
    public class FinalProductCreateDTO
    {
<<<<<<< HEAD:InventoryAPI/Models/FinalProductCreateDTO.cs
        public string Name { get; set; }
        public string Code { get; set; }
        public int MainCategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public string Unit { get; set; }
        public int WarehouseId { get; set; }
        public string Description { get; set; }
        public double ProductionDurationHours { get; set; }
        public IFormFile? ImageFile { get; set; }

        public string Components { get; set; } // JSON string
        public string IndirectCosts { get; set; } // JSON string
=======
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";

        public int MainCategoryId { get; set; }
        public int SubCategoryId { get; set; }

        public string Unit { get; set; } = "";
        public int WarehouseId { get; set; }

        public string? Description { get; set; }
        public double ProductionDurationHours { get; set; }

        public IFormFile? ImageFile { get; set; }

        public string? Components { get; set; }
        public string? IndirectCosts { get; set; }
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Models/FinalProductCreateDTO.cs
    }

    public class FinalProductComponentDTO
    {
        public int RawMaterialId { get; set; }
<<<<<<< HEAD:InventoryAPI/Models/FinalProductCreateDTO.cs
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
=======
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Models/FinalProductCreateDTO.cs
        public int UnitId { get; set; }
        public double Quantity { get; set; }
        public double Price { get; set; }
    }
<<<<<<< HEAD:InventoryAPI/Models/FinalProductCreateDTO.cs

    public class IndirectCostDTO
    {
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AllocationBasis { get; set; } = string.Empty;
        public double UnitCost { get; set; }
        public string MainClassification { get; set; } = string.Empty;
    }
=======
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Models/FinalProductCreateDTO.cs
}
