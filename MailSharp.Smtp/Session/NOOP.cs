using MailSharp.SMTP.Extensions;
namespace MailSharp.SMTP.Session;

public partial class SmtpSession
{
	// Handle NOOP command
	private async Task HandleNoopAsync(string[] parts, string line, CancellationToken ct)
	{
		await writer.WriteLineAsync(configuration["SmtpResponses:Ok"], ct);
	}
}
