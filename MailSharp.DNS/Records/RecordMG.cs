/*
3.3.6. MG RDATA format (EXPERIMENTAL)

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                   MGMNAME                     /
    /                                               /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

MGMNAME         A <domain-name> which specifies a mailbox which is a
                member of the mail group specified by the domain name.

MG records cause no additional section processing.
*/
using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordMG(RecordReader rr) : DnsRecord(rr)
{
	public string MgName { get; } = rr.ReadDomainName();
	public override string ToString() => MgName;
}