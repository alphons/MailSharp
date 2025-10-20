using MailSharp.Smtp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.SMTP.Controllers;

[Authorize(Roles = "Administrator")]
[Route("api/[controller]")]
[ApiController]
public class SmtpController(SmtpServerStatus status) : ControllerBase
{
	[HttpGet("status")]
	public IActionResult GetStatus()
	{
		return Ok(new { IsSmtpRunning = status.IsRunning });
	}
}
