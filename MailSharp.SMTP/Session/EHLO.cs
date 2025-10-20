using MailSharp.SMTP.Extensions;

namespace MailSharp.SMTP.Session;

public partial class SmtpSession
{
	// Handle EHLO command
	private async Task HandleEhloAsync(string[] parts, string line, CancellationToken ct)
	{
		if (state != SmtpState.Initial && state != SmtpState.TlsStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:BadSequence"], ct);
			return;
		}
		state = SmtpState.HeloReceived;
		await writer.WriteLineAsync(configuration["SmtpResponses:EhloSupport"], ct);
		await writer.WriteLineAsync(string.Format(configuration["SmtpResponses:EhloSizeFormat"]!, configuration.GetValue<long>("SmtpSettings:MaxMessageSize")), ct);
		if (configuration.GetValue<bool>("SmtpSettings:EnableAuth"))
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:AuthSupport"], ct);
		}
		if (configuration.GetValue<bool>("SmtpSettings:EnableStartTls"))
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:StartTlsSupport"], ct);
		}
	}
}
