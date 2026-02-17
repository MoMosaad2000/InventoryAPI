// ملف: Controllers/AccountController.cs
using InventoryAPI.Data;
using InventoryAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly InventoryDbContext _context;

        public AccountController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET api/Account
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountDTO>>> GetAccounts()
        {
            var list = await _context.Accounts
                .Select(a => new AccountDTO
                {
                    AccountCode = a.AccountCode,
                    AccountName = a.AccountName
                })
                .ToListAsync();

            return Ok(list);
        }

        // POST api/Account
        [HttpPost]
        public async Task<IActionResult> Create(Account account)
        {
            if (await _context.Accounts.AnyAsync(a => a.AccountCode == account.AccountCode))
                return Conflict(new { message = "Account already exists" });

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            // نرجع DTO فقط
            var dto = new AccountDTO
            {
                AccountCode = account.AccountCode,
                AccountName = account.AccountName
            };
            return Ok(dto);
        }

        // GET api/Account/{code}
        [HttpGet("{code}")]
        public async Task<ActionResult<AccountDTO>> GetByCode(string code)
        {
            var a = await _context.Accounts
                .Where(x => x.AccountCode == code)
                .Select(x => new AccountDTO
                {
                    AccountCode = x.AccountCode,
                    AccountName = x.AccountName
                })
                .FirstOrDefaultAsync();

            if (a == null)
                return NotFound();

            return Ok(a);
        }

        // DELETE api/Account/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var acc = await _context.Accounts.FindAsync(id);
            if (acc == null)
                return NotFound();
            _context.Accounts.Remove(acc);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
