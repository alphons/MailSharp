using Microsoft.Extensions.Configuration;
using MailSharp.SMTP.Extensions;
namespace MailSharp.SMTP.Session;

public partial class SmtpSession
{
	// Handle EXPN command
	private async Task HandleExpnAsync(string[] parts, string line, CancellationToken ct)
	{
		if (!configuration.GetValue<bool>("SmtpSettings:EnableExpn"))
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:ExpnDisabled"], ct);
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

		// Mock expansion (replace with actual mailing list lookup in production)
		await writer.WriteLineAsync("250 user1@example.com, user2@example.com", ct);
	}
}