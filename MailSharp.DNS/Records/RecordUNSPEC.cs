/*

 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordUNSPEC : DnsRecord
{
	public byte[] Data { get; init; } = [];

	public RecordUNSPEC(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16();
		Data = rr.ReadBytes(rdLength);
	}

	public override string ToString() =>
		Convert.ToBase64String(Data);
}
