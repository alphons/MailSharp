namespace MailSharp.SmtpServer.Session;

public partial class SmtpSession
{
	// Handle QUIT command
	private async Task HandleQuitAsync(string[] parts, string line)
	{
		await writer!.WriteLineAsync(configuration["SmtpResponses:Quit"]);
	}

}
