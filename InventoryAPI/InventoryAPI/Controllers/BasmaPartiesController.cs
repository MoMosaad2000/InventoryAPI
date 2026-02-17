using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace InventoryAPI.Controllers
{
    [ApiController]
    [Route("api/basma")]
    public class BasmaPartiesController : ControllerBase
    {
        private readonly IHttpClientFactory _http;

        // ✅ Basma credentials (زي ما انت بعتهم)
        // IMPORTANT: دول بيتعاملوا كـ "username/password" عاديين، وبنعمل لهم BasicAuth داخل الكود
        private const string BASMA_USER = "ZmFyYWg";
        private const string BASMA_PASS = "Wm1GeVlXZw";

        private const string CUST_URL = "https://www.basma.saudisea.com.sa/account/cust";
        private const string SUP_URL = "https://www.basma.saudisea.com.sa/account/sup";

        public BasmaPartiesController(IHttpClientFactory http)
        {
            _http = http;
        }

        // شكل ثابت للفرونت
        public class PartyVm
        {
            public string accountCode { get; set; } = "";
            public string accountName { get; set; } = "";
            public string? mobile { get; set; }
            public string? taxNumber { get; set; }
            public string? address { get; set; }
            public string partyType { get; set; } = ""; // customer/supplier
        }

        [HttpGet("customers")]
        [HttpGet("cust")]
        public Task<IActionResult> Customers() => Fetch(CUST_URL, "customer");

        [HttpGet("suppliers")]
        [HttpGet("sup")]
        public Task<IActionResult> Suppliers() => Fetch(SUP_URL, "supplier");

        private async Task<IActionResult> Fetch(string url, string type)
        {
            try
            {
                var client = _http.CreateClient("basma");
                client.Timeout = TimeSpan.FromSeconds(30);

                // Basic auth: base64("user:pass")
                var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{BASMA_USER}:{BASMA_PASS}"));

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var res = await client.SendAsync(req);

                var contentType = res.Content.Headers.ContentType?.MediaType ?? "";
                var body = await res.Content.ReadAsStringAsync();

                // ✅ لو بسمه رجعت Redirect/Login/HTML هنعرفك صراحة بدل ما نعمل Parse ونقع 500
                if ((int)res.StatusCode is 301 or 302 or 303 or 307 or 308 || body.TrimStart().StartsWith("<"))
                {
                    return StatusCode(502, new
                    {
                        message = "Basma returned non-JSON (redirect/html). Check BasicAuth or endpoint availability.",
                        basmaStatus = (int)res.StatusCode,
                        basmaContentType = contentType,
                        endpoint = url,
                        sample = body.Length > 300 ? body.Substring(0, 300) : body
                    });
                }

                if (!res.IsSuccessStatusCode)
                {
                    return StatusCode((int)res.StatusCode, new
                    {
                        message = "Basma request failed",
                        basmaStatus = (int)res.StatusCode,
                        endpoint = url,
                        basmaResponse = body
                    });
                }

                // بسمه بيرجع Array مباشرة
                using var doc = JsonDocument.Parse(body);

                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return Ok(new List<PartyVm>());
                }

                var list = new List<PartyVm>();

                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var isError = GetString(el, "isError") ?? "";
                    if (!string.IsNullOrWhiteSpace(isError) &&
                        !string.Equals(isError, "Done", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // ✅ Customers: cust_ID/cust_NAME
                    // ✅ Suppliers: splY_ID/splY_NAME
                    var code = GetString(el, "cust_ID") ?? GetString(el, "splY_ID") ?? "";
                    var name = GetString(el, "cust_NAME") ?? GetString(el, "splY_NAME") ?? "";

                    code = code.Trim();
                    name = name.Trim();

                    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                        continue;

                    var mobile = GetString(el, "mobl") ?? "0";
                    var tax = GetString(el, "taxNumber") ?? "";
                    var address = GetString(el, "adrs") ?? BuildCustomerAddress(el);

                    list.Add(new PartyVm
                    {
                        accountCode = code,
                        accountName = name,
                        mobile = NormalizeNullable(mobile),
                        taxNumber = NormalizeNullable(tax),
                        address = NormalizeNullable(address),
                        partyType = type
                    });
                }

                return Ok(list.OrderBy(x => x.accountName).ToList());
            }
            catch (Exception ex)
            {
                // ✅ هنا هتظهرلك رسالة السبب الحقيقي بدل 500 مبهم
                return StatusCode(500, new
                {
                    message = "Internal server error while calling Basma",
                    error = ex.Message
                });
            }
        }

        private static string? GetString(JsonElement el, string prop)
        {
            if (!el.TryGetProperty(prop, out var p)) return null;
            if (p.ValueKind == JsonValueKind.Null) return null;
            return p.ToString();
        }

        private static string? NormalizeNullable(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var v = s.Trim();
            if (v == "0" || v == "0000" || v == "00000") return null;
            return v;
        }

        private static string BuildCustomerAddress(JsonElement el)
        {
            // basma customers address parts
            var buildNo = GetString(el, "buildNo");
            var street = GetString(el, "street");
            var haie = GetString(el, "haie");
            var city = GetString(el, "city");
            var country = GetString(el, "country");
            var postal = GetString(el, "postal");
            var addno = GetString(el, "addno");

            var parts = new List<string>();
            void Add(string? s)
            {
                var v = NormalizeNullable(s);
                if (!string.IsNullOrWhiteSpace(v)) parts.Add(v!);
            }

            Add(buildNo); Add(street); Add(haie); Add(city); Add(country); Add(postal); Add(addno);
            return string.Join(" - ", parts);
        }
    }
}
