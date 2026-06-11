using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordUnknown : DnsRecord
{
	public byte[] RDATA;
	public RecordUnknown(RecordReader rr) : base(rr)
	{
		// re-read length
		ushort RDLENGTH = rr.ReadUInt16(-2);
		RDATA = rr.ReadBytes(RDLENGTH);
	}

	public override string ToString() => Convert.ToHexString(RDATA);
}
