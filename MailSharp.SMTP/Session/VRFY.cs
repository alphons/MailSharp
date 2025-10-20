using MailSharp.Common;
using MailSharp.SMTP.Extensions;
using System.Text.Json;

namespace MailSharp.SMTP.Session;

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

		string username = parts[1];
		try
		{
			string userStorePath = configuration["SmtpSettings:UserStorePath"] ?? throw new InvalidOperationException("UserStorePath not configured");
			if (!File.Exists(userStorePath))
			{
				await writer.WriteLineAsync(configuration["SmtpResponses:VrfyFailed"], ct);
				return;
			}

			string json = await File.ReadAllTextAsync(userStorePath, ct);
			var users = JsonSerializer.Deserialize<List<UserConfig>>(json) ?? throw new InvalidOperationException("Invalid user store format");
			var user = users.FirstOrDefault(u => u.Username == username);

			if (user != null)
			{
				await writer.WriteLineAsync($"{configuration["SmtpResponses:VrfySuccess"]} {username}", ct);
			}
			else
			{
				await writer.WriteLineAsync(configuration["SmtpResponses:VrfyFailed"], ct);
			}
		}
		catch (Exception ex)
		{
			logger.LogError("Error verifying user {0}: {1}", username, ex.Message);
			await writer.WriteLineAsync(configuration["SmtpResponses:VrfyFailed"], ct);
		}
	}
}
