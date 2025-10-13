using MailSharp.Smtp.Services;
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.Smtp.Controllers;

// API controller for status
[Route("api/[controller]")]
[ApiController]
public class SmtpController(SmtpServerStatus status) : ControllerBase
{
	[HttpGet]
	public IActionResult Get()
	{
		return Ok(new { IsSmtpRunning = status.IsRunning });
	}
}
