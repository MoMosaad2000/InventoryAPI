using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace InventoryAPI.Security
{
    public class BasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public BasicAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                if (!Request.Headers.ContainsKey("Authorization"))
                    return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));

                var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);

                if (!string.Equals(authHeader.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Scheme"));

                if (string.IsNullOrWhiteSpace(authHeader.Parameter))
                    return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Parameter"));

                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentialsRaw = Encoding.UTF8.GetString(credentialBytes);

                var parts = credentialsRaw.Split(':', 2);
                if (parts.Length != 2)
                    return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));

                var username = (parts[0] ?? "").Trim();
                var password = (parts[1] ?? "").Trim();

                // ✅ هنا التثبيت النهائي: Areka (مش Areeka)
                if (username != "Areka@Mosaad25" || password != "35831629m")
                    return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));

                var claims = new[] { new Claim(ClaimTypes.Name, username) };
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "BasicAuth parsing failed");
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
            }
        }
    }
}
