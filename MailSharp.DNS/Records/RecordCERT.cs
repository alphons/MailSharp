/*
https://www.rfc-editor.org/rfc/rfc4398.html

The CERT Resource Record

   The CERT resource record (RR) has the structure given below.  Its RR
   type code is 37.

                       1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |             type              |             key tag           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |   algorithm   |                                               /
   +---------------+            certificate or CRL                 /
   /                                                               /
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-|
 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordCERT : DnsRecord
{
	public ushort CertType { get; init; }  // 1=PKIX, 2=SPKI, 3=PGP, 4=IPKIX, etc.
	public ushort KeyTag { get; init; }
	public byte Algorithm { get; init; }
	public string Certificate { get; init; } = string.Empty;  // altijd Base64

	public RecordCERT(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16(-2);

		CertType = rr.ReadUInt16();
		KeyTag = rr.ReadUInt16();
		Algorithm = rr.ReadByte();

		byte[] certData = rr.ReadBytes(rdLength - rr.Position);

		Certificate = certData.Length == 0
			? "."
			: Convert.ToBase64String(certData);
	}

	public override string ToString()
	{
		return $"{CertType} {KeyTag} {Algorithm} {Certificate}";
	}
}
