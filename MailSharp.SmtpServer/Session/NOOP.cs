namespace MailSharp.SmtpServer.Session;

public partial class SmtpSession
{
	// Handle NOOP command
	private async Task HandleNoopAsync(string[] parts, string line)
	{
		await writer!.WriteLineAsync(configuration["SmtpResponses:Ok"]);
	}
}
