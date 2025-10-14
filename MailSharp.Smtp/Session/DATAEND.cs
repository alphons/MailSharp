using MailSharp.Smtp.Extensions;
using System.Net;
using System.Text;

namespace MailSharp.Smtp.Session;

public partial class SmtpSession
{
	// In HandleDataEndAsync, add DKIM verification
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

		// Build .eml content
		StringBuilder emlContent = new();
		emlContent.AppendLine($"Received: from {clientIp} ({ptrRecord}) by {configuration["SmtpSettings:Host"]} with SMTP; {DateTime.UtcNow:ddd, dd MMM yyyy HH:mm:ss} +0000");
		emlContent.AppendLine($"From: {mailFrom}");
		emlContent.AppendLine($"To: {string.Join(", ", rcptTo)}");
		emlContent.AppendLine($"Date: {DateTime.UtcNow:ddd, dd MMM yyyy HH:mm:ss} +0000");
		emlContent.AppendLine($"Message-ID: <{Guid.NewGuid()}@{configuration["SmtpSettings:Host"]}>");
		emlContent.Append(data);

		// Verify DKIM signature
		bool dkimValid = await dkimVerifier.VerifyDkimAsync(emlContent.ToString(), clientIp);
		if (!dkimValid && configuration.GetValue<bool>("SmtpSettings:RequireDkim"))
		{
			await writer.WriteLineAsync("550 DKIM verification failed", ct);
			return;
		}

		// Sign with DKIM
		string domain = mailFrom!.Substring(mailFrom.IndexOf('@') + 1);
		string selector = configuration[$"SmtpSettings:Dkim:{domain}:Selector"] ?? "default";
		string signedEml = dkimSigner.SignEmail(emlContent.ToString(), selector, domain);

		// Save .eml file
		string storagePath = configuration["SmtpSettings:EmlStoragePath"] ?? throw new InvalidOperationException("EmlStoragePath not configured");
		Directory.CreateDirectory(storagePath);
		string fileName = Path.Combine(storagePath, $"{Guid.NewGuid()}.eml");
		await File.WriteAllTextAsync(fileName, signedEml, ct);

		await writer.WriteLineAsync(configuration["SmtpResponses:MessageAccepted"], ct);
		Console.WriteLine($"Saved message from {mailFrom} to {string.Join(", ", rcptTo)} as {fileName}");
		mailFrom = null;
		rcptTo.Clear();
		data.Clear();
	}


}
