using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InventoryAPI.Controllers
{
    [Authorize]
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

            var products = await _context.FinalProducts
                .Include(p => p.Components)
                .Include(p => p.IndirectCosts)
                .ToListAsync();

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

            var product = await _context.FinalProducts
                .Include(p => p.Components)
                .Include(p => p.IndirectCosts)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

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
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.FinalProducts.FindAsync(id);
            if (product == null) return NotFound();

            _context.FinalProducts.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
