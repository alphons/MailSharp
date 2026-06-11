/*

 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordATMA : DnsRecord
{
	public byte Format { get; init; }  // 0 = E.164, 1 = NSAP
	public string Address { get; init; } = string.Empty;

	public RecordATMA(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16(-2);
		Format = rr.ReadByte();
		Address = Convert.ToHexString(rr.ReadBytes(rdLength - 1));
	}

	public override string ToString() => $"{Format} {Address}";
}
