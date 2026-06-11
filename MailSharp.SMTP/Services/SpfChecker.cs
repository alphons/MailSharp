using MailSharp.DNS;
using System.Net;

namespace MailSharp.SMTP.Services;

public class SpfChecker(IConfiguration configuration, Resolver resolver)
{
	public async Task<bool> CheckSpfAsync(string clientIp, string mailFromDomain, string heloDomain, CancellationToken cancellationToken = default)
	{
		string spfRecord = await ResolveSpfRecordAsync(mailFromDomain, cancellationToken)
			?? await ResolveSpfRecordAsync(heloDomain, cancellationToken)
			?? string.Empty;

		if (string.IsNullOrEmpty(spfRecord))
			return false;

		return await EvaluateSpfRecordAsync(spfRecord, clientIp, mailFromDomain, cancellationToken);
	}

	private async Task<string?> ResolveSpfRecordAsync(string domain, CancellationToken cancellationToken)
	{
		try
		{
			var dnsServer = GetDnsServer();
			var response = await resolver.QueryAsync(dnsServer, domain, DnsQType.TXT, DnsQClass.IN, cancellationToken);
			return response.RecordsTXT
				.SelectMany(r => r.Texts)
				.FirstOrDefault(t => t.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private async Task<bool> EvaluateSpfRecordAsync(string spfRecord, string clientIp, string mailFromDomain, CancellationToken cancellationToken)
	{
		if (!spfRecord.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase))
			return false;

		string[] mechanisms = spfRecord.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		foreach (string mechanism in mechanisms.Skip(1))
		{
			if (mechanism.StartsWith("ip4:"))
			{
				if (IsIpInRange(clientIp, mechanism[4..]))
					return true;
			}
			else if (mechanism.StartsWith("include:"))
			{
				string includeDomain = mechanism[8..];
				string? includeSpf = await ResolveSpfRecordAsync(includeDomain, cancellationToken);
				if (includeSpf != null && await EvaluateSpfRecordAsync(includeSpf, clientIp, mailFromDomain, cancellationToken))
					return true;
			}
			else if (mechanism == "-all")
			{
				return false;
			}
			else if (mechanism == "+all")
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsIpInRange(string clientIp, string ipRange)
	{
		try
		{
			if (ipRange.Contains('/'))
			{
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
						return false;
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

	private IPEndPoint GetDnsServer()
	{
		string host = configuration["SpfSettings:DnsServer"] ?? "8.8.8.8";
		int port = configuration.GetValue<int?>("SpfSettings:DnsPort") ?? 53;
		return new IPEndPoint(IPAddress.Parse(host), port);
	}
}
