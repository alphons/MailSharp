using Microsoft.Extensions.Configuration;

namespace MailSharp.SmtpServer.Session;

public partial class SmtpSession
{
	// Handle EXPN command
	private async Task HandleExpnAsync(string[] parts, string line)
	{
		if (!configuration.GetValue<bool>("SmtpSettings:EnableExpn"))
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:ExpnDisabled"]);
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

		// Mock expansion (replace with actual mailing list lookup in production)
		await writer!.WriteLineAsync("250 user1@example.com, user2@example.com");
	}
}