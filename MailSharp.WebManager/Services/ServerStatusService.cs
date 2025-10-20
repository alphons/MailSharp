using MailSharp.IMAP.Services;
using MailSharp.POP3.Services;
using MailSharp.SMTP.Services;

namespace MailSharp.WebManager.Services;

public class ServerStatusService(
	Pop3Service pop3Service,
	ImapService imapService,
	SmtpService smtpService)
{
	public bool IsPop3Running => pop3Service.IsRunning;
	public bool IsImapRunning => imapService.IsRunning;
	public bool IsSmtpRunning => smtpService.IsRunning;
}
