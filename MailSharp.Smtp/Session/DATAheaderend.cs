using MailSharp.Smtp.Extensions;

namespace MailSharp.Smtp.Session;

public partial class SmtpSession
{
	private async Task HandleHeaderEndAsync(string[] parts, string line, CancellationToken ct)
	{
		if (state != SmtpState.HeaderStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:BadSequence"], ct);
			return;
		}
		state = SmtpState.DataStarted;
	}


}
