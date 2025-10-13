﻿using MailSharp.Smtp.Extensions;
namespace MailSharp.Smtp.Session;

public partial class SmtpSession
{
	// Handle HELO command

	private async Task HandleHeloAsync(string[] parts, string line, CancellationToken ct)
	{
		if (state != SmtpState.Initial && state != SmtpState.TlsStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:BadSequence"], ct);
			return;
		}
		state = SmtpState.HeloReceived;
		await writer.WriteLineAsync($"{configuration["SmtpResponses:Hello"]} {parts.ElementAtOrDefault(1) ?? "client"}", ct);
	}

}
