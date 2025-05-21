using Microsoft.AspNetCore.Mvc;
using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace InventoryAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SubCategoriesController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public SubCategoriesController(InventoryDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubCategory>>> GetSubCategories()
        {
            var subCategories = await _context.SubCategories
                .Include(sc => sc.Category)  // تحميل الفئة الأم
                .ThenInclude(c => c!.SubCategories)  // تحميل الفئات الفرعية داخل الفئة الأم
                .ToListAsync();

            // تعديل هنا لجلب الفئات الفرعية بشكل كامل بدون مراجع
            var result = subCategories.Select(sc => new
            {
                sc.Id,
                sc.Name,
                sc.CategoryId,
                Category = new
                {
                    sc.Category?.Id,
                    sc.Category?.Name,
                    SubCategories = sc.Category?.SubCategories.Select(subCat => new { subCat.Id, subCat.Name }).ToList()
                }
            }).ToList();

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<SubCategory>> CreateSubCategory(SubCategory subCategory)
        {
            var category = await _context.Categories.FindAsync(subCategory.CategoryId);
            if (category == null) return BadRequest(new { message = "CategoryId not found" });


            _context.SubCategories.Add(subCategory);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSubCategories), new { id = subCategory.Id }, subCategory);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubCategory(int id)
        {
            var subCategory = await _context.SubCategories.FindAsync(id);
            if (subCategory == null) return NotFound();

            _context.SubCategories.Remove(subCategory);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
