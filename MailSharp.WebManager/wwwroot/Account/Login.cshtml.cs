using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MailSharp.WebManager.wwwroot.Account;

public class LoginController(IConfiguration configuration) : Controller
{

	[HttpGet("~/Account/Login")]
	public IActionResult Index() => View("/wwwroot/Account/Login.cshtml");

	[HttpGet("~/Account/AccessDenied")]
	public IActionResult AccessDenied() => View("/wwwroot/Account/AccessDenied.cshtml");

	[HttpGet("~/Account/Logout")]
	public async Task<IActionResult> Logout()
	{
		await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

		return RedirectToAction("Index", "Login");
	}

	// User configuration model for appsettings.json
	public class UserConfig
	{
		public string? UserName { get; set; }
		public string? Password { get; set; }
		public string? Role { get; set; }
		public int ExpireDays { get; set; } = 365;
	}

	[HttpPost("~/Account/Login")]
	public async Task<IActionResult> Login()
	{
		var username = Request.Form["username"];
		var password = Request.Form["password"];

		// Load users from appsettings.json
		var users = configuration.GetSection("Users").Get<List<UserConfig>>();
		var user = users?.FirstOrDefault(u => u.UserName == username && u.Password == password);

		TempData["ErrorMessage"] = "";

		if (user != null)
		{
			// Add claims including role
			List<Claim> claims =
			[
				new Claim(ClaimTypes.Name, username!),
				new Claim("CustomClaim", "UserRole"),
				new Claim(ClaimTypes.Role, user.Role ?? "User") // Add role claim
            ];
			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var authProperties = new AuthenticationProperties
			{
				IsPersistent = true,
				ExpiresUtc = DateTimeOffset.UtcNow.AddDays(user.ExpireDays)
			};

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

			return Redirect("/");
		}
		TempData["ErrorMessage"] = "Invalid login attempt";

		return RedirectToAction("Index", "Login");
	}
}
