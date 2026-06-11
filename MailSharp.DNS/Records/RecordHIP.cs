/*
https://www.rfc-editor.org/rfc/rfc8005.html
HIP RR Storage Format

   The RDATA for a HIP RR consists of a PK Algorithm Type, the HIT
   length, a HIT, a PK (i.e., an HI), and optionally one or more RVSs.

    0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |  HIT length   | PK algorithm  |          PK length            |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                                                               |
   ~                           HIT                                 ~
   |                                                               |
   +                     +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                     |                                         |
   +-+-+-+-+-+-+-+-+-+-+-+                                         +
   |                           Public Key                          |
   ~                                                               ~
   |                                                               |
   +                               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |                               |                               |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+                               +
   |                                                               |
   ~                       Rendezvous Servers                      ~
   |                                                               |
   +             +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |             |
   +-+-+-+-+-+-+-+

   The HIT length, PK algorithm, PK length, HIT, and Public Key fields
   are REQUIRED.  The Rendezvous Server field is OPTIONAL.


 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordHIP : DnsRecord
{
	public byte HitLength { get; init; }
	public byte PkAlgorithm { get; init; }
	public ushort PkLength { get; init; }
	public byte[] HostIdentityTag { get; init; } = Array.Empty<byte>();
	public byte[] PublicKey { get; init; } = Array.Empty<byte>();
	public List<string> RendezvousServers { get; init; } = [];

	public RecordHIP(RecordReader rr) : base(rr)
	{
		ushort rdlength = rr.ReadUInt16(-2);
		HitLength = rr.ReadByte();
		PkAlgorithm = rr.ReadByte();
		PkLength = rr.ReadUInt16();
		HostIdentityTag = rr.ReadBytes(HitLength);
		PublicKey = rr.ReadBytes(PkLength);

		while (rr.Position < rdlength)
			RendezvousServers.Add(rr.ReadDomainName());
	}

	public override string ToString()
	{
		string hit = Convert.ToBase64String(HostIdentityTag);
		string pk = Convert.ToBase64String(PublicKey);
		string srv = RendezvousServers.Count == 0 ? "" : " " + string.Join(" ", RendezvousServers);
		return $"{PkAlgorithm} {hit} {pk}{srv}";
	}
}
