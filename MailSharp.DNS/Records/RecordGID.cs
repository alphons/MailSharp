/*

 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordGID : DnsRecord
{
	public uint Gid { get; init; }

	public RecordGID(RecordReader rr) : base(rr)
	{
		Gid = rr.ReadUInt32();
	}

	public override string ToString() => Gid.ToString();
}
