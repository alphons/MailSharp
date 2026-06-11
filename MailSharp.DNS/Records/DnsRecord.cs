// Stuff records are made of

namespace MailSharp.DNS.Records;

public abstract record DnsRecord
{
	protected DnsRecord() { }
	protected DnsRecord(RecordReader reader) { }
}
