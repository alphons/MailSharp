/*
	A6 (OBSOLETE - use AAAA)
 */

using MailSharp.DNS.Records;
using System.Net;

namespace MailSharp.DNS.Records;

public record RecordA6(RecordReader rr) : DnsRecord(rr)
{
	public byte PrefixLength { get; init; } = rr.ReadByte();
	public byte[] Address { get; init; } = rr.ReadBytes(16);
	public string PrefixName { get; init; } = rr.ReadDomainName();

	public override string ToString() => $"{PrefixLength} {new IPAddress(Address)} {PrefixName}";

}
