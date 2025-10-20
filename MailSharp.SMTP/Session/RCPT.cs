using MailSharp.SMTP.Extensions;
namespace MailSharp.SMTP.Session;

public partial class SmtpSession
{
	// Handle RCPT command
	private async Task HandleRcptAsync(string[] parts, string line, CancellationToken ct)
	{
		if (state != SmtpState.MailFromReceived)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:BadSequence"], ct);
			return;
		}

		if (startTls && state != SmtpState.TlsStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:TlsRequired"], ct);
			return;
		}

		if (!line.Contains("TO:", StringComparison.OrdinalIgnoreCase))
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:SyntaxError"], ct);
			return;
		}
		string recipient = line[(line.IndexOf("TO:", StringComparison.OrdinalIgnoreCase) + 3)..].Trim();
		rcptTo.Add(recipient);
		state = SmtpState.RcptToReceived;
		await writer.WriteLineAsync(configuration["SmtpResponses:Ok"], ct);
	}
}
