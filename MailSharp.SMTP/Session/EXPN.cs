using MailSharp.SMTP.Extensions;
using System.Text.Json;
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

		string mailingList = parts[1];
		try
		{
			string mailingListPath = configuration["SmtpSettings:MailingListPath"] ?? throw new InvalidOperationException("MailingListPath not configured");
			if (!File.Exists(mailingListPath))
			{
				await writer.WriteLineAsync(configuration["SmtpResponses:ExpnFailed"], ct);
				return;
			}

			string json = await File.ReadAllTextAsync(mailingListPath, ct);
			var lists = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? throw new InvalidOperationException("Invalid mailing list format");

			if (lists.TryGetValue(mailingList, out var members))
			{
				await writer.WriteLineAsync($"250 {string.Join(", ", members)}", ct);
			}
			else
			{
				await writer.WriteLineAsync(configuration["SmtpResponses:ExpnFailed"], ct);
			}
		}
		catch (Exception ex)
		{
			logger.LogError("Error expanding mailing list {0}: {1}", mailingList, ex.Message);
			await writer.WriteLineAsync(configuration["SmtpResponses:ExpnFailed"], ct);
		}
	}
}