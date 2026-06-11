using MailSharp.DNS.Records;
using System.Net;

namespace MailSharp.DNS;

public partial class Resolver
{
	public async Task<Response> QueryAsync(IPEndPoint server, string name, DnsQType type, DnsQClass @class, CancellationToken ct = default)
	{
		if (!name.EndsWith('.'))
			name += '.';

		if (recordCache.IsEmpty)
			await BootstrapRootAsync(ct);

		var question = new QuestionRecord(name, type, @class);
		if (SearchInRecordCache(question) is Response cached)
			return cached;

		var request = new Request();
		request.AddQuestion(question);
		return await GetResponseAsync(server, request);
	}

	private async Task BootstrapRootAsync(CancellationToken ct)
	{
		List<RR> authorities = [];
		await foreach (var authority in RootHintsParser.ParseAsync("root\\named_root.txt", ct))
			authorities.Add(authority);
		AddToCache(new Response
		{
			Questions = [new QuestionRecord(".", DnsQType.NS, DnsQClass.IN)],
			Authorities = authorities
		});
	}

	public async Task<IPHostEntry> GetHostEntryAsync(IPEndPoint server, string hostNameOrAddress, CancellationToken ct = default)
		=> IPAddress.TryParse(hostNameOrAddress, out var ip)
			? await GetHostEntryAsync(server, ip, ct)
			: await ResolveForwardAsync(server, hostNameOrAddress, ct);

	public async Task<IPHostEntry> GetHostEntryAsync(IPEndPoint server, IPAddress ip, CancellationToken ct = default)
	{
		var response = await QueryAsync(server, GetArpaFromIp(ip), DnsQType.PTR, DnsQClass.IN, ct);
		return response.RecordsPTR.Length > 0
			? await ResolveForwardAsync(server, response.RecordsPTR[0].PtrName, ct)
			: new IPHostEntry();
	}

	private async Task<IPHostEntry> ResolveForwardAsync(IPEndPoint server, string hostName, CancellationToken ct)
	{
		var response = await QueryAsync(server, hostName, DnsQType.A, DnsQClass.IN, ct);
		var entry = new IPHostEntry { HostName = hostName };
		var addresses = new List<IPAddress>();
		var aliases = new List<string>();

		foreach (var rr in response.Answers)
		{
			if (rr.Type == DnsType.A && rr.Record is RecordA a)
				addresses.Add(a.Address);
			else if (rr.Type == DnsType.CNAME)
				aliases.Add(rr.Name);
		}

		entry.AddressList = [.. addresses];
		entry.Aliases = [.. aliases];
		return entry;
	}
}
