/*
3.3.3. MB RDATA format (EXPERIMENTAL)

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    /                   MADNAME                     /
    /                                               /
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

MADNAME         A <domain-name> which specifies a host which has the
                specified mailbox.

MB records cause additional section processing which looks up an A type
RRs corresponding to MADNAME.
*/
using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordMB(RecordReader rr) : DnsRecord(rr)
{
	public string MadName { get; } = rr.ReadDomainName();
	public override string ToString() => MadName;
}