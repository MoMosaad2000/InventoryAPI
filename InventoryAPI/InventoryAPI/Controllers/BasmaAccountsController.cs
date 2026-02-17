using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace InventoryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BasmaAccountsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // ✅ Basma credentials (AS-IS) — زي ما بتحطهم في المتصفح
        private const string USERNAME = "ZmFyYWg";
        private const string PASSWORD = "Wm1GeVlXZw";

        public BasmaAccountsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public class BasmaDto
        {
            public string? acC_ID { get; set; }
            public string? acC_NAME { get; set; }
            public string? isError { get; set; }
        }

        public class AccountVm
        {
            public string accountCode { get; set; } = "";
            public string accountName { get; set; } = "";
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                // ✅ Basic Auth = base64("USERNAME:PASSWORD")
                var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{USERNAME}:{PASSWORD}"));

                using var req = new HttpRequestMessage(HttpMethod.Get, "https://www.basma.saudisea.com.sa/account");
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var res = await client.SendAsync(req);
                var body = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    return StatusCode((int)res.StatusCode, new
                    {
                        message = "Basma request failed",
                        status = (int)res.StatusCode,
                        basmaResponse = body
                    });
                }

                var data = JsonSerializer.Deserialize<List<BasmaDto>>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                var result = data
                    .Where(x => x != null && (x.isError == "Done" || x.isError == null))
                    .Select(x => new AccountVm
                    {
                        accountCode = (x.acC_ID ?? "").Trim(),
                        accountName = (x.acC_NAME ?? "").Trim()
                    })
                    .Where(x => x.accountCode != "" && x.accountName != "")
                    .OrderBy(x => x.accountCode)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal error", error = ex.Message });
            }
        }
    }
}
