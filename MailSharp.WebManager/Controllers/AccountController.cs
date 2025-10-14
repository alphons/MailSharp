using Microsoft.AspNetCore.Mvc;

namespace MailSharp.WebManager.Controllers;

public class AccountController : Controller
{
	// GET: Account/Login
	public IActionResult Login(string? error)
	{
		ViewData["ErrorMessage"] = error;
		return View("/wwwroot/Account/Login.cshtml");
	}

	// GET: Account/AccessDenied
	public IActionResult AccessDenied()
	{
		return View("/wwwroot/Account/AccessDenied.cshtml");
	}
}
