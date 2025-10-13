using System.Net;
using System.Text;
using MailSharp.SmtpServer.Extensions;
namespace MailSharp.SmtpServer.Session;

public partial class SmtpSession
{
	// Handle end of DATA (.)
	private async Task HandleDataEndAsync(string[] parts, string line, CancellationToken ct)
	{
		if (state != SmtpState.DataStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:BadSequence"], ct);
			return;
		}
		state = SmtpState.HeloReceived;

		// Gather connection info
		var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
		string clientIp = remoteEndPoint?.Address.ToString() ?? "Unknown";
		int clientPort = remoteEndPoint?.Port ?? 0;
		string ptrRecord = "Unknown";
		try
		{
			var ptrResult = await System.Net.Dns.GetHostEntryAsync(remoteEndPoint!.Address);
			ptrRecord = ptrResult.HostName;
		}
		catch
		{
			// PTR lookup failed, keep default
		}

		// Build .eml content with server and client headers
		StringBuilder emlContent = new();
		emlContent.AppendLine($"Received: from {clientIp} ({ptrRecord}) by {configuration["SmtpSettings:Host"]} with SMTP; {DateTime.UtcNow:ddd, dd MMM yyyy HH:mm:ss} +0000");
		emlContent.AppendLine($"From: {mailFrom}");
		emlContent.AppendLine($"To: {string.Join(", ", rcptTo)}");
		emlContent.AppendLine($"Date: {DateTime.UtcNow:ddd, dd MMM yyyy HH:mm:ss} +0000");
		emlContent.AppendLine($"Message-ID: <{Guid.NewGuid()}@{configuration["SmtpSettings:Host"]}>");

		// Append client-provided headers and body from data
		emlContent.Append(data); // Includes client headers and body with blank line separator

		// Save .eml file
		string storagePath = configuration["SmtpSettings:EmlStoragePath"] ?? throw new InvalidOperationException("EmlStoragePath not configured");
		Directory.CreateDirectory(storagePath);
		string fileName = Path.Combine(storagePath, $"{Guid.NewGuid()}.eml");
		await File.WriteAllTextAsync(fileName, emlContent.ToString(), ct);

		await writer.WriteLineAsync(configuration["SmtpResponses:MessageAccepted"], ct);
		Console.WriteLine($"Saved message from {mailFrom} to {string.Join(", ", rcptTo)} as {fileName}");
		mailFrom = null;
		rcptTo.Clear();
		data.Clear();
	}

}
