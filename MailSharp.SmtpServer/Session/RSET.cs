namespace MailSharp.SmtpServer.Session;

public partial class SmtpSession
{
	// Handle RSET command
	private async Task HandleRsetAsync(string[] parts, string line)
	{
		state = SmtpState.HeloReceived;
		mailFrom = null;
		rcptTo.Clear();
		data.Clear();
		await writer!.WriteLineAsync(configuration["SmtpResponses:Ok"]);
	}
}
