/*
3.3.8. MR RDATA format (EXPERIMENTAL)

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                   NEWNAME                     /
    /                                               /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

NEWNAME         A <domain-name> which specifies a mailbox which is the
                proper rename of the specified mailbox.

MR records cause no additional section processing.  The main use for MR
is as a forwarding entry for a user who has moved to a different
mailbox.
*/
using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordMR(RecordReader rr) : DnsRecord(rr)
{
	public string NewName { get; } = rr.ReadDomainName();
	public override string ToString() => NewName;
}