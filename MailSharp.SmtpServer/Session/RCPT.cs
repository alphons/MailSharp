namespace MailSharp.SmtpServer.Session;

public partial class SmtpSession
{
	// Handle RCPT command
	private async Task HandleRcptAsync(string[] parts, string line)
	{
		if (state != SmtpState.MailFromReceived)
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:BadSequence"]);
			return;
		}

		if (requireTls && state != SmtpState.TlsStarted)
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:TlsRequired"]);
			return;
		}

		if (!line.Contains("TO:", StringComparison.OrdinalIgnoreCase))
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:SyntaxError"]);
			return;
		}
		string recipient = line.Substring(line.IndexOf("TO:", StringComparison.OrdinalIgnoreCase) + 3).Trim();
		rcptTo.Add(recipient);
		state = SmtpState.RcptToReceived;
		await writer!.WriteLineAsync(configuration["SmtpResponses:Ok"]);
	}
}
