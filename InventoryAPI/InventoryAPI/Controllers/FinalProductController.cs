using InventoryAPI.Data;
using InventoryAPI.Models;
<<<<<<< HEAD:InventoryAPI/Controllers/FinalProductController.cs
using Microsoft.AspNetCore.Authorization;
=======
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Controllers/FinalProductController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InventoryAPI.Controllers
{
<<<<<<< HEAD:InventoryAPI/Controllers/FinalProductController.cs
    [Authorize]
=======
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Controllers/FinalProductController.cs
    [ApiController]
    [Route("api/[controller]")]
    public class FinalProductController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FinalProductController(InventoryDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
<<<<<<< HEAD:InventoryAPI/Controllers/FinalProductController.cs

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] FinalProductCreateDTO dto)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var componentsDto = !string.IsNullOrWhiteSpace(dto.Components)
                ? JsonSerializer.Deserialize<List<FinalProductComponentDTO>>(dto.Components, jsonOptions)
                : new List<FinalProductComponentDTO>();

            var indirectsDto = !string.IsNullOrWhiteSpace(dto.IndirectCosts)
                ? JsonSerializer.Deserialize<List<IndirectCostDTO>>(dto.IndirectCosts, jsonOptions)
                : new List<IndirectCostDTO>();

            var product = new FinalProduct
            {
                Name = dto.Name,
                Code = dto.Code,
                MainCategoryId = dto.MainCategoryId,
                SubCategoryId = dto.SubCategoryId,
                Unit = dto.Unit,
                WarehouseId = dto.WarehouseId,
                Description = dto.Description,
                ProductionDurationHours = dto.ProductionDurationHours,
                Components = componentsDto
                    .Where(c => c.RawMaterialId != 0 && !string.IsNullOrWhiteSpace(c.Name))
                    .Select(c => new FinalProductComponent
                    {
                        RawMaterialId = c.RawMaterialId,
                        Name = c.Name,
                        Code = c.Code,
                        UnitId = c.UnitId,
                        Quantity = c.Quantity,
                        Price = c.Price
                    }).ToList(),

                IndirectCosts = indirectsDto
                    .Where(i => !string.IsNullOrWhiteSpace(i.AccountCode))
                    .Select(i => new IndirectCost
                    {
                        AccountCode = i.AccountCode,
                        AccountName = i.AccountName,
                        AllocationBasis = i.AllocationBasis,
                        UnitCost = i.UnitCost,
                        MainClassification = i.MainClassification
                    }).ToList()
            };

            _context.FinalProducts.Add(product);
            await _context.SaveChangesAsync();

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = Guid.NewGuid() + Path.GetExtension(dto.ImageFile.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await dto.ImageFile.CopyToAsync(stream);

                product.ImagePath = $"/uploads/{fileName}";
                await _context.SaveChangesAsync();
            }

            return Ok(product);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

=======
        [HttpPost]
        public async Task<IActionResult> CreateFinalProduct([FromForm] FinalProductCreateDTO dto)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return BadRequest(new { message = "اسم المنتج مطلوب" });

                if (string.IsNullOrWhiteSpace(dto.Code))
                    return BadRequest(new { message = "كود المنتج مطلوب" });

                Console.WriteLine("📥 Receiving FinalProduct...");
                Console.WriteLine($"Name: {dto.Name}, Code: {dto.Code}");
                Console.WriteLine($"IndirectCosts: {dto.IndirectCosts}");
                Console.WriteLine($"Components: {dto.Components}");

                var product = new FinalProduct
                {
                    Name = dto.Name,
                    Code = dto.Code,
                    MainCategoryId = dto.MainCategoryId,
                    SubCategoryId = dto.SubCategoryId,
                    Unit = dto.Unit,
                    WarehouseId = dto.WarehouseId,
                    Description = dto.Description,
                    ProductionDurationHours = dto.ProductionDurationHours
                };

                // في دالة CreateFinalProduct
                if (dto.ImageFile != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.ImageFile.FileName)}";
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");

                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await dto.ImageFile.CopyToAsync(stream);

                    // هنا نضيف المسار الكامل بما في ذلك اسم النطاق
                    var request = HttpContext.Request;
                    product.ImagePath = $"{request.Scheme}://{request.Host}/uploads/{fileName}";
                }

                // Deserialize and validate components
                List<FinalProductComponentDTO> componentsDto;
                try
                {
                    componentsDto = JsonSerializer.Deserialize<List<FinalProductComponentDTO>>(dto.Components ?? "[]")
                        ?? new List<FinalProductComponentDTO>();
                }
                catch (JsonException)
                {
                    return BadRequest(new { message = "تنسيق مكونات المنتج غير صالح" });
                }

                if (componentsDto.Count == 0 || componentsDto.Any(c => c.RawMaterialId <= 0))
                    return BadRequest(new { message = "❌ مكونات المنتج غير صحيحة." });

                product.Components = componentsDto.Select(c => new FinalProductComponent
                {
                    RawMaterialId = c.RawMaterialId,
                    Name = c.Name,
                    Code = c.Code,
                    UnitId = c.UnitId,
                    Quantity = c.Quantity,
                    Price = c.Price
                }).ToList();

                // Deserialize and validate indirect costs
                List<IndirectCost> indirectCosts;
                try
                {
                    indirectCosts = JsonSerializer.Deserialize<List<IndirectCost>>(dto.IndirectCosts ?? "[]")
                        ?? new List<IndirectCost>();
                }
                catch (JsonException)
                {
                    return BadRequest(new { message = "تنسيق التكاليف غير المباشرة غير صالح" });
                }

                if (indirectCosts.Count == 0 || indirectCosts.Any(c => string.IsNullOrWhiteSpace(c.AccountCode)))
                    return BadRequest(new { message = "❌ التكاليف غير المباشرة غير صالحة." });

                // Process accounts
                foreach (var cost in indirectCosts)
                {
                    var existing = await _context.Accounts
                        .FirstOrDefaultAsync(a => a.AccountCode == cost.AccountCode);

                    if (existing == null)
                    {
                        _context.Accounts.Add(new Account
                        {
                            AccountCode = cost.AccountCode,
                            AccountName = cost.AccountName,
                            AllocationBasis = cost.AllocationBasis,
                            MainClassification = cost.MainClassification
                        });
                    }
                }

                product.IndirectCosts = indirectCosts;

                _context.FinalProducts.Add(product);
                await _context.SaveChangesAsync();

                return Ok(product);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ SERVER ERROR: " + ex.Message);
                return StatusCode(500, new
                {
                    message = "خطأ في السيرفر",
                    details = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FinalProduct>>> GetAll()
        {
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Controllers/FinalProductController.cs
            var products = await _context.FinalProducts
                .Include(p => p.Components)
                .Include(p => p.IndirectCosts)
                .ToListAsync();
<<<<<<< HEAD:InventoryAPI/Controllers/FinalProductController.cs

            return Ok(products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Code,
                p.MainCategoryId,
                p.SubCategoryId,
                p.Unit,
                p.WarehouseId,
                p.Description,
                p.ProductionDurationHours,
                ImageUrl = string.IsNullOrEmpty(p.ImagePath) ? null : $"{baseUrl}/{p.ImagePath.TrimStart('/')}",
                Components = p.Components.Select(c => new
                {
                    c.Id,
                    c.RawMaterialId,
                    c.Name,
                    c.Code,
                    c.UnitId,
                    c.Quantity,
                    c.Price,
                    c.UnitCost
                }),
                IndirectCosts = p.IndirectCosts.Select(i => new
                {
                    i.Id,
                    i.AccountCode,
                    i.AccountName,
                    i.AllocationBasis,
                    i.UnitCost,
                    i.MainClassification
                })
            }));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

=======
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FinalProduct>> GetById(int id)
        {
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Controllers/FinalProductController.cs
            var product = await _context.FinalProducts
                .Include(p => p.Components)
                .Include(p => p.IndirectCosts)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

<<<<<<< HEAD:InventoryAPI/Controllers/FinalProductController.cs
            return Ok(new
            {
                product.Id,
                product.Name,
                product.Code,
                product.MainCategoryId,
                product.SubCategoryId,
                product.Unit,
                product.WarehouseId,
                product.Description,
                product.ProductionDurationHours,
                ImageUrl = string.IsNullOrEmpty(product.ImagePath) ? null : $"{baseUrl}/{product.ImagePath.TrimStart('/')}",
                Components = product.Components.Select(c => new
                {
                    c.Id,
                    c.RawMaterialId,
                    c.Name,
                    c.Code,
                    c.UnitId,
                    c.Quantity,
                    c.Price,
                    c.UnitCost
                }),
                IndirectCosts = product.IndirectCosts.Select(i => new
                {
                    i.Id,
                    i.AccountCode,
                    i.AccountName,
                    i.AllocationBasis,
                    i.UnitCost,
                    i.MainClassification
                })
            });
=======
            return Ok(product);
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Controllers/FinalProductController.cs
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
<<<<<<< HEAD:InventoryAPI/Controllers/FinalProductController.cs
            var product = await _context.FinalProducts.FindAsync(id);
            if (product == null) return NotFound();

=======
            var product = await _context.FinalProducts
                .Include(p => p.Components)
                .Include(p => p.IndirectCosts)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            _context.FinalProductComponents.RemoveRange(product.Components);
            _context.IndirectCosts.RemoveRange(product.IndirectCosts);
>>>>>>> c3bfd43 (Fix SalesOrder snapshot fields + InvoicePDF fetch fallback):InventoryAPI/InventoryAPI/Controllers/FinalProductController.cs
            _context.FinalProducts.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
