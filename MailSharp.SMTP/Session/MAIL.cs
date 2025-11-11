using MailSharp.Common;
using MailSharp.SMTP.Extensions;
using System.Net;
namespace MailSharp.SMTP.Session;

public partial class SmtpSession
{
	// In HandleMailAsync, add SPF check
	private async Task HandleMailAsync(string[] parts, string line, CancellationToken ct)
	{
		if (state != SmtpState.HeloReceived && state != SmtpState.TlsStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:BadSequence"], ct);
			return;
		}

		if (security == SecurityEnum.StartTls && state != SmtpState.TlsStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:TlsRequired"], ct);
			return;
		}

		if (!line.Contains("FROM:", StringComparison.OrdinalIgnoreCase))
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:SyntaxError"], ct);
			return;
		}

		string mailFromAddress = line.Substring(line.IndexOf("FROM:", StringComparison.OrdinalIgnoreCase) + 5).Trim();
		string mailFromDomain = mailFromAddress.Substring(mailFromAddress.IndexOf('@') + 1);
		string clientIp = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "Unknown";

		if (configuration.GetValue<bool>("SmtpSettings:RequireDkim"))
		{
			bool spfValid = await spfChecker.CheckSpfAsync(clientIp, mailFromDomain, mailFromDomain);

			if (!spfValid)
			{
				await writer.WriteLineAsync("550 SPF check failed", ct);
				return;
			}
		}

		mailFrom = mailFromAddress;
		state = SmtpState.MailFromReceived;
		await writer.WriteLineAsync(configuration["SmtpResponses:Ok"], ct);
	}

}
