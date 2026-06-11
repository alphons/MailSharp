/*
https://www.rfc-editor.org/rfc/rfc4255.html

The SSHFP RDATA Format

   The RDATA for a SSHFP RR consists of an algorithm number, fingerprint
   type and the fingerprint of the public host key.

       1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
       0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
       |   algorithm   |    fp type    |                               /
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+                               /
       /                                                               /
       /                          fingerprint                          /
       /                                                               /
       +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordSSHFP : DnsRecord
{
	public byte Algorithm { get; init; }  // 1=RSA, 2=DSA, 3=ECDSA, 4=Ed25519, 6=Ed448
	public byte FingerprintType { get; init; }  // 1=SHA-1, 2=SHA-256
	public byte[] Fingerprint { get; init; } = [];

	public RecordSSHFP(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16(-2);
		Algorithm = rr.ReadByte();
		FingerprintType = rr.ReadByte();
		Fingerprint = rr.ReadBytes(rdLength - rr.Position);
	}

	public override string ToString() =>
		$"{Algorithm} {FingerprintType} {BitConverter.ToString(Fingerprint).Replace("-", "").ToLowerInvariant()}";
}
