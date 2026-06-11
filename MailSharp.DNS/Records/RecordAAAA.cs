using MailSharp.DNS.Records;
using System.Net;


#region Rfc info
/*
2.2 AAAA data format

   A 128 bit IPv6 address is encoded in the data portion of an AAAA
   resource record in network byte order (high-order byte first).
 */
#endregion

namespace MailSharp.DNS.Records;

public record RecordAAAA : DnsRecord
{
	public IPAddress Address { get; }

	public RecordAAAA(RecordReader rr) : base(rr)
	{
		Address = new IPAddress(rr.ReadBytes(16));
	}

	public RecordAAAA(string value)
	{
		Address = IPAddress.Parse(value);
	}

	public override string ToString() => Address.ToString();

}
