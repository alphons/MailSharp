using MailSharp.SMTP.Extensions;

namespace MailSharp.SMTP.Session;

public partial class SmtpSession
{
	// Handle HELP command
	private async Task HandleHelpAsync(string[] parts, string line, CancellationToken ct)
	{
		await writer.WriteLineAsync(configuration["SmtpResponses:Help"], ct);
	}
}
