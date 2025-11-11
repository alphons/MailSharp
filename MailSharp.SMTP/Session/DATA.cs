using MailSharp.Common;
using MailSharp.SMTP.Extensions;

namespace MailSharp.SMTP.Session;

public partial class SmtpSession
{
	// Handle DATA command
	private async Task HandleDataAsync(string[] parts, string line, CancellationToken ct)
	{
		if (state != SmtpState.RcptToReceived)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:BadSequence"] ,ct);
			return;
		}

		if (security == SecurityEnum.StartTls && state != SmtpState.TlsStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:TlsRequired"], ct);
			return;
		}

		state = SmtpState.HeaderStarted;
		await writer.WriteLineAsync(configuration["SmtpResponses:HeaderStart"], ct);
	}

}
