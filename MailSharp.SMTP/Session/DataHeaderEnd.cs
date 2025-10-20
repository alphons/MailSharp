using MailSharp.SMTP.Extensions;

namespace MailSharp.SMTP.Session;

public partial class SmtpSession
{
	// Handle end of email headers
	private async Task HandleHeaderEndAsync(string[] parts, string line, CancellationToken ct)
	{
		if (state != SmtpState.HeaderStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:BadSequence"], ct);
			return;
		}

		// Validate required headers
		string[] headers = data.ToString().Split(new[] { "\r\n" }, StringSplitOptions.None);
		bool hasFrom = headers.Any(h => h.StartsWith("From:", StringComparison.OrdinalIgnoreCase));
		bool hasTo = headers.Any(h => h.StartsWith("To:", StringComparison.OrdinalIgnoreCase));
		bool hasDate = headers.Any(h => h.StartsWith("Date:", StringComparison.OrdinalIgnoreCase));

		if (!hasFrom || !hasTo || !hasDate)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:InvalidHeaders"], ct);
			state = SmtpState.RcptToReceived;
			return;
		}

		// Basic spam check
		bool isSpam = headers.Any(h =>
			h.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase) &&
			h.Contains("Buy now", StringComparison.OrdinalIgnoreCase));

		if (isSpam)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:SpamDetected"], ct);
			state = SmtpState.RcptToReceived;
			return;
		}

		state = SmtpState.DataStarted;
	}
}
