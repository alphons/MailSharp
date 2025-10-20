using MailSharp.SMTP.Extensions;

namespace MailSharp.SMTP.Session;

public partial class SmtpSession
{
	private async Task HandleHeaderEndAsync(string[] parts, string line, CancellationToken ct)
	{
		if (state != SmtpState.HeaderStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:BadSequence"], ct);
			return;
		}

		// TODO check if header is valid (e.g. has From, To, Date fields)
		// If not, respond with 554 and reset state to RcptToReceived
		// Also check for known spam such as Subject: Buy now, etc.

		state = SmtpState.DataStarted;
	}


}
