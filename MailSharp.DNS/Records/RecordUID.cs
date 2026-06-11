/*

 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordUID : DnsRecord
{
	public uint Uid { get; init; }

	public RecordUID(RecordReader rr) : base(rr)
	{
		Uid = rr.ReadUInt32();
	}
	public override string ToString() => Uid.ToString();
}

