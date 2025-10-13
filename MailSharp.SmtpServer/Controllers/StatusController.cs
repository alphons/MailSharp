using MailSharp.SmtpServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.SmtpServer.Controllers;

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
