using Microsoft.Extensions.Configuration;

namespace MailSharp.SmtpServer.Session;

public partial class SmtpSession
{
	// Handle EHLO command
	private async Task HandleEhloAsync(string[] parts, string line)
	{
		if (state != SmtpState.Initial && state != SmtpState.TlsStarted)
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:BadSequence"]);
			return;
		}
		state = SmtpState.HeloReceived;
		await writer!.WriteLineAsync($"{configuration["SmtpResponses:EhloSupport"]}");
		await writer!.WriteLineAsync(string.Format(configuration["SmtpResponses:EhloSizeFormat"]!, configuration.GetValue<long>("SmtpSettings:MaxMessageSize")));
		if (configuration.GetValue<bool>("SmtpSettings:EnableAuth"))
		{
			await writer!.WriteLineAsync("250-AUTH PLAIN CRAM-MD5 LOGIN");
		}
		if (configuration.GetValue<bool>("SmtpSettings:EnableStartTls"))
		{
			await writer!.WriteLineAsync("250-STARTTLS");
		}
	}
}
