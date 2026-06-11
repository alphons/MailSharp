/*
https://www.rfc-editor.org/rfc/rfc4034.html

RRSIG RDATA Wire Format

   The RDATA for an RRSIG RR consists of a 2 octet Type Covered field, a
   1 octet Algorithm field, a 1 octet Labels field, a 4 octet Original
   TTL field, a 4 octet Signature Expiration field, a 4 octet Signature
   Inception field, a 2 octet Key tag, the Signer's Name field, and the
   Signature field.

                        1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |        Type Covered           |  Algorithm    |     Labels    |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                         Original TTL                          |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                      Signature Expiration                     |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                      Signature Inception                      |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |            Key Tag            |                               /
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+         Signer's Name         /
   /                                                               /
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   /                                                               /
   /                            Signature                          /
   /                                                               /
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordRRSIG : DnsRecord
{
	public DnsType TypeCovered { get; init; }
	public byte Algorithm { get; init; }
	public byte Labels { get; init; }
	public uint OriginalTtl { get; init; }
	public DateTime Expiration { get; init; }  // UTC
	public DateTime Inception { get; init; }    // UTC
	public ushort KeyTag { get; init; }
	public string SignerName { get; init; } = string.Empty;
	public byte[] Signature { get; init; } = [];

	public RecordRRSIG(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16(-2);

		TypeCovered = (DnsType)rr.ReadUInt16();
		Algorithm = rr.ReadByte();
		Labels = rr.ReadByte();
		OriginalTtl = rr.ReadUInt32();
		Expiration = DateTimeOffset.FromUnixTimeSeconds(rr.ReadUInt32()).UtcDateTime;
		Inception = DateTimeOffset.FromUnixTimeSeconds(rr.ReadUInt32()).UtcDateTime;
		KeyTag = rr.ReadUInt16();
		SignerName = rr.ReadDomainName();

		Signature = rr.ReadBytes(rdLength - rr.Position);
	}

	public override string ToString()
	{
		// Tijdstempels in YYYYMMDDHHmmSS formaat (UTC), precies zoals BIND/dig
		string exp = Expiration.ToString("yyyyMMddHHmmss");
		string inc = Inception.ToString("yyyyMMddHHmmss");

		string sigBase64 = Signature.Length == 0
			? "."
			: Convert.ToBase64String(Signature);

		return $"{TypeCovered} {Algorithm} {Labels} {OriginalTtl} {exp} {inc} {KeyTag} {SignerName} {sigBase64}";
	}

}
