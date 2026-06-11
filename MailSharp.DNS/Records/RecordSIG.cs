#region Rfc info
/*
 * http://www.ietf.org/rfc/rfc2535.txt
 * 4.1 SIG RDATA Format

   The RDATA portion of a SIG RR is as shown below.  The integrity of
   the RDATA information is protected by the signature field.

                           1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
       0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |        type covered           |  algorithm    |     labels    |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                         original TTL                          |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                      signature expiration                     |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                      signature inception                      |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |            key  tag           |                               |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+         signer's name         +
      |                                                               /
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-/
      /                                                               /
      /                            signature                          /
      /                                                               /
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+


*/
#endregion

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordSIG(RecordReader rr) : DnsRecord(rr)
{
	public ushort TypeCovered { get; } = rr.ReadUInt16();
	public byte Algorithm { get; } = rr.ReadByte();
	public byte Labels { get; } = rr.ReadByte();
	public uint OriginalTtl { get; } = rr.ReadUInt32();
	public uint SigExpiration { get; } = rr.ReadUInt32();
	public uint SigInception { get; } = rr.ReadUInt32();
	public ushort KeyTag { get; } = rr.ReadUInt16();
	public string SignerName { get; } = rr.ReadDomainName();
	public string Signature { get; } = rr.ReadString();

	public override string ToString() =>
		$"{TypeCovered} {Algorithm} {Labels} {OriginalTtl} {SigExpiration} {SigInception} {KeyTag} {SignerName} \"{Signature}\"";
}
