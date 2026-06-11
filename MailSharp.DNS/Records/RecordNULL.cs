/*
3.3.10. NULL RDATA format (EXPERIMENTAL)

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                  <anything>                   /
    /                                               /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

Anything at all may be in the RDATA field so long as it is 65535 octets
or less.

NULL records cause no additional section processing.  NULL RRs are not
allowed in master files.  NULLs are used as placeholders in some
experimental extensions of the DNS.
*/
using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordNULL : DnsRecord
{
	public byte[] Data { get; } = [];
	public RecordNULL(RecordReader rr) : base(rr)
	{
		ushort rdlength = rr.ReadUInt16();
		Data = rr.ReadBytes(rdlength);
	}
	public override string ToString() => $"...binary... ({Data.Length} bytes)";
}

