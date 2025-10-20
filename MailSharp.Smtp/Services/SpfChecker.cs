using System.Net;

namespace MailSharp.SMTP.Services;

public class SpfChecker(IConfiguration configuration)
{

	// Check SPF for incoming email
	public async Task<bool> CheckSpfAsync(string clientIp, string mailFromDomain, string heloDomain)
	{
		// Resolve SPF record via DNS
		string spfRecord = await ResolveSpfRecordAsync(mailFromDomain)
			?? await ResolveSpfRecordAsync(heloDomain)
			?? string.Empty;

		if (string.IsNullOrEmpty(spfRecord))
		{
			return false; // No SPF record, fail open
		}

		// Parse and evaluate SPF record
		return EvaluateSpfRecord(spfRecord, clientIp, mailFromDomain);
	}

	// Resolve SPF record via DNS TXT lookup
	private static async Task<string?> ResolveSpfRecordAsync(string domain)
	{
		try
		{
			var result = await Dns.GetHostEntryAsync(domain);
			string[] txtRecords = [.. result.Aliases
				.Select(a => Dns.GetHostEntry(a).HostName)
				.Where(h => h.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase))];

			return txtRecords.FirstOrDefault();
		}
		catch
		{
			return null;
		}
	}

	// Evaluate SPF record against client IP
	private static bool EvaluateSpfRecord(string spfRecord, string clientIp, string mailFromDomain)
	{
		if (!spfRecord.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string[] mechanisms = spfRecord.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		bool isMatch = false;

		foreach (string mechanism in mechanisms.Skip(1)) // Skip v=spf1
		{
			if (mechanism.StartsWith("ip4:"))
			{
				string ipRange = mechanism[4..];
				if (IsIpInRange(clientIp, ipRange))
				{
					isMatch = true;
					break;
				}
			}
			else if (mechanism.StartsWith("include:"))
			{
				string includeDomain = mechanism[8..];
				string? includeSpf = ResolveSpfRecordAsync(includeDomain).GetAwaiter().GetResult();
				if (includeSpf != null && EvaluateSpfRecord(includeSpf, clientIp, mailFromDomain))
				{
					isMatch = true;
					break;
				}
			}
			else if (mechanism == "-all")
			{
				isMatch = false;
				break;
			}
			else if (mechanism == "+all")
			{
				isMatch = true;
				break;
			}
		}

		return isMatch;
	}

	// Check if client IP is in the specified range (simplified)
	private static bool IsIpInRange(string clientIp, string ipRange)
	{
		try
		{
			if (ipRange.Contains('/'))
			{
				// Handle CIDR notation (e.g., 192.168.1.0/24)
				var parts = ipRange.Split('/');
				var networkAddress = IPAddress.Parse(parts[0]);
				var cidr = int.Parse(parts[1]);
				var clientAddress = IPAddress.Parse(clientIp);

				byte[] networkBytes = networkAddress.GetAddressBytes();
				byte[] clientBytes = clientAddress.GetAddressBytes();
				int maskBytes = cidr / 8;
				int maskRemainder = cidr % 8;

				for (int i = 0; i < maskBytes; i++)
				{
					if (networkBytes[i] != clientBytes[i])
					{
						return false;
					}
				}

				if (maskRemainder > 0)
				{
					byte mask = (byte)(0xFF << (8 - maskRemainder));
					return (networkBytes[maskBytes] & mask) == (clientBytes[maskBytes] & mask);
				}

				return true;
			}

			return clientIp == ipRange;
		}
		catch
		{
			return false;
		}
	}
}
