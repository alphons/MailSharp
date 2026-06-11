#region Rfc info
/*
3.3.14. TXT RDATA format

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                   TXT-DATA                    /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

TXT-DATA        One or more <character-string>s.

TXT RRs are used to hold descriptive text.  The semantics of the text
depends on the domain where it is found.
 * 
*/
#endregion

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public sealed record RecordTXT : DnsRecord
{
	public List<string> Texts { get; } = [];

	public RecordTXT(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16(-2);
		int pos = rr.Position;
		while (rr.Position - pos < rdLength)
			Texts.Add(rr.ReadString());
	}
	public override string ToString() => string.Join(" ", Texts.Select(t => $"\"{t}\""));
}
