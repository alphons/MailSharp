using MailSharp.IMAP.Services;
using MailSharp.POP3.Services;
using MailSharp.SMTP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.WebManager.Controllers;

[Authorize(Roles = "Administrator")]
[Route("api/[controller]")]
[ApiController]
public class SmtpController(
	SmtpService smtp,
	ImapService imap,
	Pop3Service pop3) : ControllerBase
{
	[HttpGet("status")]
	public IActionResult GetStatus()
	{
		return Ok(new 
		{ 
			IsSmtpRunning = smtp.IsRunning,
			IsImapRunning = imap.IsRunning,
			IsPop3Running = pop3.IsRunning
		});
	}
}
