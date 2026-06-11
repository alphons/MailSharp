/*
 * http://tools.ietf.org/rfc/rfc2230.txt
 * 
 * 3.1 KX RDATA format

   The KX DNS record has the following RDATA format:

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                  PREFERENCE                   |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                   EXCHANGER                   /
    /                                               /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

   where:

   PREFERENCE      A 16 bit non-negative integer which specifies the
                   preference given to this RR among other KX records
                   at the same owner.  Lower values are preferred.

   EXCHANGER       A <domain-name> which specifies a host willing to
                   act as a mail exchange for the owner name.

   KX records MUST cause type A additional section processing for the
   host specified by EXCHANGER.  In the event that the host processing
   the DNS transaction supports IPv6, KX records MUST also cause type
   AAAA additional section processing.

   The KX RDATA field MUST NOT be compressed.

 */
using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordKX(RecordReader rr) : DnsRecord(rr), IComparable<RecordKX>
{
	public ushort Preference { get; } = rr.ReadUInt16();
	public string Exchange { get; } = rr.ReadDomainName();

	public override string ToString() => $"{Preference} {Exchange}";
	public int CompareTo(RecordKX? other) =>
		other is null ? 1 :
		Preference != other.Preference ? Preference.CompareTo(other.Preference) :
		string.Compare(Exchange, other.Exchange, StringComparison.OrdinalIgnoreCase);
}