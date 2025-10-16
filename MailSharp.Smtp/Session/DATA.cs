using MailSharp.Smtp.Extensions;

namespace MailSharp.Smtp.Session;

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

		if (startTls && state != SmtpState.TlsStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:TlsRequired"], ct);
			return;
		}

		state = SmtpState.HeaderStarted;
		await writer.WriteLineAsync(configuration["SmtpResponses:HeaderStart"], ct);
	}

}
