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
	public IActionResult StatusAll() => Ok(new
	{
		smtp = smtpMetrics.IsRunning,
		imap = imapMetrics.IsRunning,
		pop3 = pop3Metrics.IsRunning
	});

	[HttpGet("smtp")]
	public IActionResult SmtpStatus()
	{
		var topSenders = smtpMetrics.TopSenderDomains
			.OrderByDescending(x => x.Value).Take(10)
			.Select(x => new { domain = x.Key, count = x.Value });

		var topRecipients = smtpMetrics.TopRecipientDomains
			.OrderByDescending(x => x.Value).Take(10)
			.Select(x => new { domain = x.Key, count = x.Value });

		return Ok(new
		{
			isRunning = smtpMetrics.IsRunning,
			startTime = smtpMetrics.StartTime,
			uptimeSeconds = smtpMetrics.UptimeSeconds,
			connections = new
			{
				total = smtpMetrics.TotalConnections,
				active = smtpMetrics.ActiveSessions
			},
			messages = new
			{
				received = smtpMetrics.MessagesReceived,
				relayed = smtpMetrics.MessagesRelayed,
				bytesReceived = smtpMetrics.BytesReceived,
				lastReceivedAt = smtpMetrics.LastMessageReceivedAt
			},
			rejections = new
			{
				total = smtpMetrics.MessagesRejectedTotal,
				spf = smtpMetrics.MessagesRejectedSpf,
				dkim = smtpMetrics.MessagesRejectedDkim,
				dmarc = smtpMetrics.MessagesRejectedDmarc
			},
			auth = new
			{
				success = smtpMetrics.AuthSuccess,
				failed = smtpMetrics.AuthFailed
			},
			topSenders,
			topRecipients
		});
	}

	[HttpGet("imap")]
	public IActionResult ImapStatus()
	{
		var topUsers = imapMetrics.TopUsers
			.OrderByDescending(x => x.Value).Take(10)
			.Select(x => new { user = x.Key, count = x.Value });

		return Ok(new
		{
			isRunning = imapMetrics.IsRunning,
			startTime = imapMetrics.StartTime,
			uptimeSeconds = imapMetrics.UptimeSeconds,
			connections = new
			{
				total = imapMetrics.TotalConnections,
				active = imapMetrics.ActiveSessions
			},
			auth = new
			{
				success = imapMetrics.LoginSuccess,
				failed = imapMetrics.LoginFailed
			},
			messages = new
			{
				fetched = imapMetrics.MessagesFetched,
				bytesSent = imapMetrics.BytesSent,
				lastActivityAt = imapMetrics.LastActivityAt
			},
			commands = imapMetrics.CommandsProcessed,
			folderSelects = imapMetrics.FolderSelects,
			topUsers
		});
	}

	[HttpGet("pop3")]
	public IActionResult Pop3Status()
	{
		var topUsers = pop3Metrics.TopUsers
			.OrderByDescending(x => x.Value).Take(10)
			.Select(x => new { user = x.Key, count = x.Value });

		return Ok(new
		{
			isRunning = pop3Metrics.IsRunning,
			startTime = pop3Metrics.StartTime,
			uptimeSeconds = pop3Metrics.UptimeSeconds,
			connections = new
			{
				total = pop3Metrics.TotalConnections,
				active = pop3Metrics.ActiveSessions
			},
			auth = new
			{
				success = pop3Metrics.LoginSuccess,
				failed = pop3Metrics.LoginFailed
			},
			messages = new
			{
				retrieved = pop3Metrics.MessagesRetrieved,
				deleted = pop3Metrics.MessagesDeleted,
				bytesSent = pop3Metrics.BytesSent,
				lastActivityAt = pop3Metrics.LastActivityAt
			},
			topUsers
		});
	}
}
