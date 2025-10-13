using MailSharp.SmtpServer.Extensions;

namespace MailSharp.SmtpServer.Session;

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

		state = SmtpState.DataStarted;
		await writer.WriteLineAsync(configuration["SmtpResponses:DataStart"], ct);
	}

}
