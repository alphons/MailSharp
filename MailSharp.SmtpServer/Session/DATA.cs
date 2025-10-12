namespace MailSharp.SmtpServer.Session;

public partial class SmtpSession
{
	// Handle DATA command
	private async Task HandleDataAsync(string[] parts, string line)
	{
		if (state != SmtpState.RcptToReceived)
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:BadSequence"]);
			return;
		}

		if (requireTls && state != SmtpState.TlsStarted)
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:TlsRequired"]);
			return;
		}

		state = SmtpState.DataStarted;
		await writer!.WriteLineAsync(configuration["SmtpResponses:DataStart"]);
	}

}
