using MailSharp.SMTP.Extensions;
namespace MailSharp.SMTP.Session;

public partial class SmtpSession
{
	// Handle QUIT command
	private async Task HandleQuitAsync(string[] parts, string line, CancellationToken ct)
	{
		await writer.WriteLineAsync(configuration["SmtpResponses:Quit"], ct);
	}

}
