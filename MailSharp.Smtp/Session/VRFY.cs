using MailSharp.Smtp.Extensions;
using Microsoft.Extensions.Configuration;

namespace MailSharp.Smtp.Session;

public partial class SmtpSession
{
	// Handle VRFY command
	private async Task HandleVrfyAsync(string[] parts, string line, CancellationToken ct)
	{
		if (!configuration.GetValue<bool>("SmtpSettings:EnableVrfy"))
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:VrfyDisabled"], ct);
			return;
		}

		if (state != SmtpState.HeloReceived)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:BadSequence"], ct);
			return;
		}

		if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:SyntaxError"], ct);
			return;
		}

		// Mock verification (replace with actual user lookup in production)
		await writer.WriteLineAsync(configuration["SmtpResponses:VrfySuccess"], ct);
	}
}
