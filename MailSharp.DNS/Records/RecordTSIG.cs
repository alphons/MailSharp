using MailSharp.DNS;
using MailSharp.DNS.Records;
using System;
/*
 * http://www.ietf.org/rfc/rfc2845.txt
 * 
 * Field Name       Data Type      Notes
      --------------------------------------------------------------
      Algorithm Name   domain-name    Name of the algorithm
                                      in domain name syntax.
      Time Signed      u_int48_t      seconds since 1-Jan-70 UTC.
      Fudge            u_int16_t      seconds of error permitted
                                      in Time Signed.
      MAC Size         u_int16_t      number of octets in MAC.
      MAC              octet stream   defined by Algorithm Name.
      Original ID      u_int16_t      original message ID
      Error            u_int16_t      expanded RCODE covering
                                      TSIG processing.
      Other Len        u_int16_t      length, in octets, of
                                      Other Data.
      Other Data       octet stream   empty unless Error == BADTIME

 */

namespace MailSharp.DNS.Records;

public record RecordTSIG : DnsRecord
{
	public string AlgorithmName { get; }
	public long TimeSigned { get; }
	public ushort Fudge { get; }
	public ushort MacSize { get; }
	public byte[] Mac { get; }
	public ushort OriginalId { get; }
	public ushort Error { get; }
	public ushort OtherLen { get; }
	public byte[] OtherData { get; }

	public RecordTSIG(RecordReader rr) : base(rr)
	{
		AlgorithmName = rr.ReadDomainName();

		uint high = rr.ReadUInt32();
		uint low = rr.ReadUInt32();
		TimeSigned = (long)high << 32 | low;

		Fudge = rr.ReadUInt16();
		MacSize = rr.ReadUInt16();
		Mac = rr.ReadBytes(MacSize);
		OriginalId = rr.ReadUInt16();
		Error = rr.ReadUInt16();
		OtherLen = rr.ReadUInt16();
		OtherData = rr.ReadBytes(OtherLen);
	}

	public override string ToString()
	{
		DateTime dt = DateTime.UnixEpoch.AddSeconds(TimeSigned);
		return $"{AlgorithmName} {dt:yyyy-MM-dd HH:mm:ss} {Fudge} {OriginalId} {Error}";
	}
}