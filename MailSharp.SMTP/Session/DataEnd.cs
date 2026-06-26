using MailSharp.SMTP.Extensions;
using System.Net;
using System.Text;

namespace MailSharp.SMTP.Session;

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
			var ptrResult = await Dns.GetHostEntryAsync(remoteEndPoint!.Address);
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
		//bool dkimValid = await dkimVerifier.VerifyDkimAsync(emlContent.ToString(), clientIp);
		//if (!dkimValid && configuration.GetValue<bool>("SmtpSettings:RequireDkim"))
		//{
		//	await writer.WriteLineAsync(configuration["SmtpResponses:DkimFailed"], ct);
		//	return;
		//}

		// Verify DMARC
		string mailFromDomain = ExtractDomain(mailFrom!);

		bool dmarcValid = await dmarcChecker.CheckDmarcAsync(emlContent.ToString(), clientIp, mailFromDomain, mailFromDomain, ct);
		if (!dmarcValid && configuration.GetValue<bool>("DmarcSettings:RequireDmarc"))
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:DmarcFailed"], ct);
			return;
		}

		// Sign with DKIM
		//string selector = configuration[$"SmtpSettings:Dkim:{mailFromDomain}:Selector"] ?? "default";
		//string signedEml = dkimSigner.SignEmail(emlContent.ToString(), selector, mailFromDomain);

		string signedEml = emlContent.ToString();

		// Deliver .eml to each recipient's INBOX under {MailboxSettings:StoragePath}/{domain}/{user}/INBOX/
		string storagePath = configuration["MailboxSettings:StoragePath"] ?? throw new InvalidOperationException("MailboxSettings:StoragePath not configured");
		string messageId = Guid.NewGuid().ToString();
		foreach (string recipient in rcptTo)
		{
			string domain = ExtractDomain(recipient);
			string user = ExtractUser(recipient);
			string inboxPath = Path.Combine(storagePath, domain, user, "INBOX");
			Directory.CreateDirectory(inboxPath);
			string fileName = Path.Combine(inboxPath, $"{messageId}.eml");
			await File.WriteAllTextAsync(fileName, signedEml, ct);
			logger.LogInformation("Received email from {MailFrom} to {Recipient} saved as {FileName}", mailFrom, recipient, fileName);
		}

		await writer.WriteLineAsync(configuration["SmtpResponses:MessageAccepted"], ct);

		var rcptDomains = rcptTo.Select(r => ExtractDomain(r)).Where(d => d.Length > 0);
		metrics.MessageReceived(mailFromDomain, rcptDomains, signedEml.Length);

		mailFrom = null;
		rcptTo.Clear();
		data.Clear();
	}

	// Extracts the domain from any address format:
	//   user@domain.com            →  domain.com
	//   <user@domain.com>          →  domain.com
	//   Display Name <user@domain.com>  →  domain.com
	private static string ExtractDomain(string address)
	{
		int at = address.IndexOf('@');
		if (at < 0)
			return string.Empty;
		string after = address[(at + 1)..];
		int len = 0;
		while (len < after.Length && (char.IsLetterOrDigit(after[len]) || after[len] == '.' || after[len] == '-'))
			len++;
		return after[..len];
	}

	// Extracts the local part (user) from any address format:
	//   user@domain.com            →  user
	//   <user@domain.com>          →  user
	//   Display Name <user@domain.com>  →  user
	private static string ExtractUser(string address)
	{
		int at = address.IndexOf('@');
		if (at < 0)
			return string.Empty;
		int start = address.LastIndexOf('<', at);
		return start >= 0 ? address[(start + 1)..at] : address[..at];
	}

}
