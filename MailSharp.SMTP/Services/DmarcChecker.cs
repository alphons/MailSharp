using MailSharp.Common;
using MailSharp.SMTP.Metrics;
using System.Net;

namespace MailSharp.SMTP.Services;

public class DmarcChecker(
	IConfiguration configuration,
	DkimVerifier dkimVerifier,
	SpfChecker spfChecker,
	SmtpMetrics metrics,
	ILogger<DmarcChecker> logger)
{

	// Check DMARC policy for incoming email
	public async Task<bool> CheckDmarcAsync(string emlContent, string clientIp, string mailFromDomain, string heloDomain, CancellationToken cancellationToken)
	{
		var eventIdConfig = configuration.GetSection("DmarcEventIds:DmarcCheck").Get<EventIdConfig>()
			?? throw new InvalidOperationException("Missing DmarcEventIds:DmarcCheck");
		logger.LogInformation(
			new EventId(eventIdConfig.Id, eventIdConfig.Name),
			configuration["DmarcLogMessages:DmarcCheck"],
			mailFromDomain);

		// Fetch DMARC policy from DNS
		string? dmarcRecord = await ResolveDmarcRecordAsync(mailFromDomain, cancellationToken);
		if (string.IsNullOrEmpty(dmarcRecord))
		{
			logger.LogWarning(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				configuration["DmarcLogMessages:NoDmarcRecord"],
				mailFromDomain);
			return configuration.GetValue<bool>("DmarcSettings:FailOpen");
		}

		// Parse DMARC record
		var dmarcFields = ParseDmarcRecord(dmarcRecord);
		if (!dmarcFields.TryGetValue("v", out string? version) || version != "DMARC1")
		{
			logger.LogWarning(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				configuration["DmarcLogMessages:InvalidDmarcRecord"],
				mailFromDomain);
			return configuration.GetValue<bool>("DmarcSettings:FailOpen");
		}

		if (!dmarcFields.TryGetValue("p", out string? policy))
		{
			logger.LogWarning(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				configuration["DmarcLogMessages:MissingPolicy"],
				mailFromDomain);
			return configuration.GetValue<bool>("DmarcSettings:FailOpen");
		}

		// Check SPF and DKIM alignment
		bool spfPass = await spfChecker.CheckSpfAsync(clientIp, mailFromDomain, heloDomain);

		if(spfPass == false)
			metrics.IncrementRejectedSpf();

		bool dkimPass = await dkimVerifier.VerifyDkimAsync(emlContent, clientIp);

		if(dkimPass == false)
			metrics.IncrementRejectedDkim();	

		bool spfAligned = spfPass && IsSpfAligned(mailFromDomain, heloDomain);
		bool dkimAligned = dkimPass && IsDkimAligned(emlContent, mailFromDomain);

		bool dmarcPass = (spfAligned && dmarcFields.GetValueOrDefault("aspf", "r") == "r") ||
						 (dkimAligned && dmarcFields.GetValueOrDefault("adkim", "r") == "r");

		if(dmarcPass == false)
			metrics.IncrementRejectedDmarc();

		// Apply DMARC policy
		switch (policy.ToLower())
		{
			case "none":
				return true; // Always pass for monitoring
			case "quarantine":
			case "reject":
				if (!dmarcPass)
				{
					logger.LogWarning(
						new EventId(eventIdConfig.Id, eventIdConfig.Name),
						configuration["DmarcLogMessages:PolicyFailed"],
						policy, mailFromDomain);
					return false;
				}
				return true;
			default:
				logger.LogWarning(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["DmarcLogMessages:UnknownPolicy"],
					policy, mailFromDomain);
				return configuration.GetValue<bool>("DmarcSettings:FailOpen");
		}
	}

	// Resolve DMARC TXT record via DNS
	private async Task<string?> ResolveDmarcRecordAsync(string domain, CancellationToken cancellationToken)
	{
		try
		{
			string query = $"_dmarc.{domain}";
			var result = await Dns.GetHostEntryAsync(query, cancellationToken);
			string[] txtRecords = result.Aliases
				.Select(a => Dns.GetHostEntry(a).HostName)
				.Where(h => h.StartsWith("v=DMARC1", StringComparison.OrdinalIgnoreCase))
				.ToArray();
			return txtRecords.FirstOrDefault();
		}
		catch
		{
			return null;
		}
	}

	// Parse DMARC record into key-value pairs
	private static Dictionary<string, string> ParseDmarcRecord(string dmarcRecord)
	{
		var fields = new Dictionary<string, string>();
		string[] parts = dmarcRecord.Split(';', StringSplitOptions.RemoveEmptyEntries);
		foreach (string part in parts)
		{
			string trimmed = part.Trim();
			if (string.IsNullOrEmpty(trimmed))
				continue;
			int equalsIndex = trimmed.IndexOf('=');
			if (equalsIndex == -1)
				continue;
			string key = trimmed[..equalsIndex].Trim();
			string value = trimmed[(equalsIndex + 1)..].Trim();
			fields[key] = value;
		}
		return fields;
	}

	// Check SPF alignment (relaxed alignment)
	private static bool IsSpfAligned(string mailFromDomain, string heloDomain)
	{
		return mailFromDomain.Equals(heloDomain, StringComparison.OrdinalIgnoreCase);
	}

	// Check DKIM alignment (relaxed alignment)
	private static bool IsDkimAligned(string emlContent, string mailFromDomain)
	{
		string[] headers = emlContent.Split(new[] { "\r\n" }, StringSplitOptions.None);
		string? dkimHeader = headers.FirstOrDefault(h => h.StartsWith("DKIM-Signature:", StringComparison.OrdinalIgnoreCase));
		if (dkimHeader == null)
			return false;

		var dkimFields = ParseDmarcRecord(dkimHeader["DKIM-Signature:".Length..]);
		return dkimFields.TryGetValue("d", out string? dkimDomain) &&
			   dkimDomain.Equals(mailFromDomain, StringComparison.OrdinalIgnoreCase);
	}
}
