using Microsoft.Extensions.Configuration;

namespace MailSharp.SmtpServer.Session;

public partial class SmtpSession
{
	// Handle VRFY command
	private async Task HandleVrfyAsync(string[] parts, string line)
	{
		if (!configuration.GetValue<bool>("SmtpSettings:EnableVrfy"))
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:VrfyDisabled"]);
			return;
		}

		if (state != SmtpState.HeloReceived)
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:BadSequence"]);
			return;
		}

		if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:SyntaxError"]);
			return;
		}

		// Mock verification (replace with actual user lookup in production)
		await writer!.WriteLineAsync(configuration["SmtpResponses:VrfySuccess"]);
	}
}
