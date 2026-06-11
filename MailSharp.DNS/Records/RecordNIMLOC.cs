/*

 */

using MailSharp.DNS.Records;
using System.Text;

namespace MailSharp.DNS.Records;

public record RecordNIMLOC : DnsRecord
{
	public string Locator { get; init; } = string.Empty;

	public RecordNIMLOC(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16(-2);
		Locator = rr.ReadString(rdLength);
	}

	public override string ToString() => Locator;
}
