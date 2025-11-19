using MailSharp.IMAP.Metrics;
using MailSharp.POP3.Metrics;
using MailSharp.SMTP.Metrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.WebManager.Controllers;

[Authorize(Roles = "Administrator")]
[Route("~/api/[controller]")]
[ApiController]
public class StatusController(SmtpMetrics smtpMetrics, ImapMetrics imapMetrics, Pop3Metrics pop3Metrics) : ControllerBase
{
	[HttpGet("all")]
	public IActionResult StatusAll()
	{
		return Ok(new
		{
			Smtp = smtpMetrics.IsRunning,
			Imap = imapMetrics.IsRunning,
			Pop3 = pop3Metrics.IsRunning
		});
	}
}
