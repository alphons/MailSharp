using MailSharp.Smtp.Services;
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.WebManager.Controllers;

// API controller for status
[Route("~/")]
public class HomeController(SmtpServerStatus status) : ControllerBase
{
	[HttpGet]
	public IActionResult Index()
	{
		return Ok(new { SmtpRunning = status.IsRunning });
	}
}
