
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MailSharp.WebManager.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IConfiguration configuration) : ControllerBase
{
	// POST: api/auth/login
	[HttpPost("login")]
	public async Task<IActionResult> Login()
	{
		var username = Request.Form["username"];
		var password = Request.Form["password"];

		// Load users from appsettings.json
		var users = configuration.GetSection("Users").Get<List<UserConfig>>();
		var user = users?.FirstOrDefault(u => u.UserName == username && u.Password == password);

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
				ExpiresUtc = DateTimeOffset.UtcNow.AddYears(1)
			};

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
			return RedirectToAction("Index", "Home");
		}

		return RedirectToAction("Login", "Account", new { error = "Invalid login attempt" });
	}

	// GET: api/auth/logout
	[HttpGet("logout")]
	public async Task<IActionResult> Logout()
	{
		await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
		return RedirectToAction("Login", "Account");
	}

	// GET: api/auth/protected
	[HttpGet("protected")]
	[Authorize(Roles = "Administrator")] // Restrict to Administrator role
	public IActionResult Protected()
	{
		var username = User.Identity?.Name;
		var customClaim = User.FindFirst("CustomClaim")?.Value;
		var role = User.FindFirst(ClaimTypes.Role)?.Value;
		return Ok(new { Message = $"Protected endpoint accessed by {username}, Role: {role}, CustomClaim: {customClaim}" });
	}
}

// User configuration model for appsettings.json
public class UserConfig
{
	public string? UserName { get; set; }
	public string? Password { get; set; }
	public string? Role { get; set; }
}
