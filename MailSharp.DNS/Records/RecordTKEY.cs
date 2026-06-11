using MailSharp.DNS;
using MailSharp.DNS.Records;
using System;
/*
 * http://tools.ietf.org/rfc/rfc2930.txt
 * 
2. The TKEY Resource Record

   The TKEY resource record (RR) has the structure given below.  Its RR
   type code is 249.

      Field       Type         Comment
      -----       ----         -------
       Algorithm:   domain
       Inception:   u_int32_t
       Expiration:  u_int32_t
       Mode:        u_int16_t
       Error:       u_int16_t
       Key Size:    u_int16_t
       Key Data:    octet-stream
       Other Size:  u_int16_t
       Other Data:  octet-stream  undefined by this specification

 */

namespace MailSharp.DNS.Records;

public record RecordTKEY : DnsRecord
{
	public string Algorithm { get; }
	public uint Inception { get; }
	public uint Expiration { get; }
	public ushort Mode { get; }
	public ushort Error { get; }
	public ushort KeySize { get; }
	public byte[] KeyData { get; }
	public ushort OtherSize { get; }
	public byte[] OtherData { get; }

	public RecordTKEY(RecordReader rr) : base(rr)
	{
		Algorithm = rr.ReadDomainName();
		Inception = rr.ReadUInt32();
		Expiration = rr.ReadUInt32();
		Mode = rr.ReadUInt16();
		Error = rr.ReadUInt16();
		KeySize = rr.ReadUInt16();
		KeyData = rr.ReadBytes(KeySize);
		OtherSize = rr.ReadUInt16();
		OtherData = rr.ReadBytes(OtherSize);
	}

	public override string ToString() =>
		$"{Algorithm} {Inception} {Expiration} {Mode} {Error}";
}