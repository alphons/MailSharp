using Microsoft.Extensions.Configuration;
using MailSharp.SmtpServer.Extensions;
namespace MailSharp.SmtpServer.Session;

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
		await writer.WriteLineAsync($"{configuration["SmtpResponses:EhloSupport"]}", ct);
		await writer.WriteLineAsync(string.Format(configuration["SmtpResponses:EhloSizeFormat"]!, configuration.GetValue<long>("SmtpSettings:MaxMessageSize")), ct);
		if (configuration.GetValue<bool>("SmtpSettings:EnableAuth"))
		{
			await writer.WriteLineAsync("250-AUTH PLAIN CRAM-MD5 LOGIN", ct);
		}
		if (configuration.GetValue<bool>("SmtpSettings:EnableStartTls"))
		{
			await writer.WriteLineAsync("250-STARTTLS", ct);
		}
	}
}
