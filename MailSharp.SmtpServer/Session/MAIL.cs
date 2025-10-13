using MailSharp.Smtp.Extensions;
namespace MailSharp.Smtp.Session;

public partial class SmtpSession
{
	// Handle MAIL command
	private async Task HandleMailAsync(string[] parts, string line, CancellationToken ct)
	{
		if (state != SmtpState.HeloReceived && state != SmtpState.TlsStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:BadSequence"], ct);
			return;
		}

		if (startTls && state != SmtpState.TlsStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:TlsRequired"], ct);
			return;
		}

		if (!line.Contains("FROM:", StringComparison.OrdinalIgnoreCase))
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:SyntaxError"], ct);
			return;
		}
		mailFrom = line.Substring(line.IndexOf("FROM:", StringComparison.OrdinalIgnoreCase) + 5).Trim();
		state = SmtpState.MailFromReceived;
		await writer.WriteLineAsync(configuration["SmtpResponses:Ok"], ct);
	}

}
