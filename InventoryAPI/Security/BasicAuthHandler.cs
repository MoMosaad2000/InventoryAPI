using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace InventoryAPI.Security
{
	public class BasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		public BasicAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
								ILoggerFactory logger,
								UrlEncoder encoder,
								ISystemClock clock)
			: base(options, logger, encoder, clock)
		{
		}

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			if (!Request.Headers.ContainsKey("Authorization"))
				return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));

			try
			{
				var authHeader = Request.Headers["Authorization"].ToString();
				var credentialBytes = Convert.FromBase64String(authHeader.Replace("Basic ", ""));
				var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
				var username = credentials[0];
				var password = credentials[1];

				// ✅ عدل بيانات الدخول هنا:
				if (username != "Areka@Mosaad25" || password != "35831629m")
				{
					return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));
				}

				var claims = new[] {
					new Claim(ClaimTypes.NameIdentifier, username),
					new Claim(ClaimTypes.Name, username)
				};

				var identity = new ClaimsIdentity(claims, Scheme.Name);
				var principal = new ClaimsPrincipal(identity);
				var ticket = new AuthenticationTicket(principal, Scheme.Name);

				return Task.FromResult(AuthenticateResult.Success(ticket));
			}
			catch
			{
				return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
			}
		}
	}
}
