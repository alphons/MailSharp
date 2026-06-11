using MailSharp.DNS.Records;
using System.Security.Cryptography.X509Certificates;
using System.Text;
/*
 * http://tools.ietf.org/rfc/rfc3658.txt
 * 
2.4.  Wire Format of the DS record

   The DS (type=43) record contains these fields: key tag, algorithm,
   digest type, and the digest of a public key KEY record that is
   allowed and/or used to sign the child's apex KEY RRset.  Other keys
   MAY sign the child's apex KEY RRset.

                        1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |           key tag             |  algorithm    |  Digest type  |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                digest  (length depends on type)               |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                (SHA-1 digest is 20 bytes)                     |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                                                               |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-|
   |                                                               |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-|
   |                                                               |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

 */

namespace MailSharp.DNS.Records;

public sealed record RecordDS : DnsRecord
{
	public ushort KeyTag { get; init; }
	public byte Algorithm { get; init; }
	public byte DigestType { get; init; }
	public byte[] Digest { get; init; } = [];

	public RecordDS(RecordReader rr) : base(rr)
	{
		ushort length = rr.ReadUInt16(-2);

		KeyTag = rr.ReadUInt16();
		Algorithm = rr.ReadByte();
		DigestType = rr.ReadByte();
		Digest = rr.ReadBytes(length - 4);
	}

	public override string ToString()
	{
		// Digest wordt altijd in hex getoond (hoofdletters), zonder spaties
		string digestHex = Digest.Length == 0
			? "."
			: Convert.ToHexString(Digest);

		return $"{KeyTag} {Algorithm} {DigestType} {digestHex}";
	}
}
