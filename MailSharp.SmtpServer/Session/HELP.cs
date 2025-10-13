using MailSharp.SmtpServer.Extensions;

namespace MailSharp.SmtpServer.Session;

public partial class SmtpSession
{
	// Handle HELP command
	private async Task HandleHelpAsync(string[] parts, string line, CancellationToken ct)
	{
		await writer.WriteLineAsync(configuration["SmtpResponses:Help"], ct);
	}
}
