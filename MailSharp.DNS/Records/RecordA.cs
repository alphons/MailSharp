using MailSharp.DNS;
using MailSharp.DNS.Records;
using System;
using System.Net;
/*
 3.4.1. A RDATA format

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                    ADDRESS                    |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

ADDRESS         A 32 bit Internet address.

Hosts that have multiple Internet addresses will have multiple A
records.
 * 
 */
namespace MailSharp.DNS.Records;

public record RecordA : DnsRecord
{
	public IPAddress Address { get; }

	public RecordA(RecordReader rr) : base(rr)
	{
		Address = new IPAddress(rr.ReadBytes(4));
	}

	public RecordA(string value)
	{
		Address = IPAddress.Parse(value);
	}

	public override string ToString() => Address.ToString();

}
