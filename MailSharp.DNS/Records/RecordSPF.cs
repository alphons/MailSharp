/*

 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordSPF : DnsRecord
{
	public List<string> Texts { get; } = [];

	public RecordSPF(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16(-2);
		int pos = rr.Position;
		while (rr.Position - pos < rdLength)
			Texts.Add(rr.ReadString());
	}
	public override string ToString() => string.Join(" ", Texts.Select(t => $"\"{t}\""));
}

