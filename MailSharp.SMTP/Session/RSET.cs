using MailSharp.SMTP.Extensions;
namespace MailSharp.SMTP.Session;

public partial class SmtpSession
{
	// Handle RSET command
	private async Task HandleRsetAsync(string[] parts, string line, CancellationToken ct)
	{
		state = SmtpState.HeloReceived;
		mailFrom = null;
		rcptTo.Clear();
		data.Clear();
		await writer.WriteLineAsync(configuration["SmtpResponses:Ok"], ct);
	}
}
