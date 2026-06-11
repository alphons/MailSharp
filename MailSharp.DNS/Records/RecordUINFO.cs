/*
IANA-Reserved
 */

using MailSharp.DNS.Records;
using System.Text;

namespace MailSharp.DNS.Records;

public record RecordUINFO : DnsRecord
{
	public string Info { get; init; } = string.Empty;

	public RecordUINFO(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16();
		Info = Encoding.ASCII.GetString(rr.ReadBytes(rdLength));
	}

	public override string ToString() => $"\"{Info}\"";
}