/*
http://ana-3.lcs.mit.edu/~jnc/nimrod/dns.txt


 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordEID : DnsRecord
{
	public byte[] EndpointId { get; init; } = [];

	public RecordEID(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16(-2);
		EndpointId = rr.ReadBytes(rdLength);
	}

	public override string ToString() =>
		BitConverter.ToString(EndpointId).Replace("-", "").ToLowerInvariant();
}
