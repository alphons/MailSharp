using MailSharp.DNS.Records;
using System.Runtime.CompilerServices;

namespace MailSharp.DNS;

public record RootHint(string Domain, long TTL, string RecordType, string Value);

public static class RootHintsParser
{
	public static async IAsyncEnumerable<RR> ParseAsync(string filePath,
		[EnumeratorCancellation]
		CancellationToken ct)
	{
		await foreach (var line in File.ReadLinesAsync(filePath, ct))
		{
			if (string.IsNullOrWhiteSpace(line))
				continue;

			var clean = line.Split(';', 2)[0].TrimEnd();
			if (string.IsNullOrWhiteSpace(clean))
				continue;

			var fields = clean.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
			if (fields.Length < 4)
				continue;

            RR hint = new()
            {
                Name = fields[0],
                TTL = uint.Parse(fields[1]),
				Type = Enum.Parse<DnsType>(fields[2]),
				Class = DnsClass.IN,
			};
            hint.Record = hint.Type switch
            {
                DnsType.A => new RecordA(fields[3]),
                DnsType.AAAA => new RecordAAAA(fields[3]),
                DnsType.NS => new RecordNS(fields[3]),
                _ => throw new Exception($"Unsupported root hint type: {fields[2]}"),
			};
            yield return hint;
		}
	}

	
}
