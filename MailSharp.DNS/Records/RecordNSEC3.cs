/*
https://www.rfc-editor.org/rfc/rfc5155.html

NSEC3 RDATA Wire Format

   The RDATA of the NSEC3 RR is as shown below:

                        1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |   Hash Alg.   |     Flags     |          Iterations           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |  Salt Length  |                     Salt                      /
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |  Hash Length  |             Next Hashed Owner Name            /
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   /                         Type Bit Maps                         /
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   Hash Algorithm is a single octet.

   Flags field is a single octet, the Opt-Out flag is the least
   significant bit, as shown below:

    0 1 2 3 4 5 6 7
   +-+-+-+-+-+-+-+-+
   |             |O|
   +-+-+-+-+-+-+-+-+
 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordNSEC3 : DnsRecord
{
	public byte HashAlgorithm { get; init; }
	public byte Flags { get; init; }
	public ushort Iterations { get; init; }
	public byte[] Salt { get; init; } = [];
	public string NextHashedOwnerName { get; init; } = string.Empty;
	public List<DnsType> TypeBitMaps { get; init; } = [];

	public bool OptOut => (Flags & 0x01) == 0x01;

	public RecordNSEC3(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16(-2);

		HashAlgorithm = rr.ReadByte();
		Flags = rr.ReadByte();
		Iterations = rr.ReadUInt16();

		byte saltLength = rr.ReadByte();
		Salt = saltLength == 0 ? Array.Empty<byte>() : rr.ReadBytes(saltLength);

		byte hashLength = rr.ReadByte();
		byte[] hash = rr.ReadBytes(hashLength);
		NextHashedOwnerName = Convert.ToBase64String(hash);

		while (rr.Position < rdLength)
		{
			byte window = rr.ReadByte();
			byte len = rr.ReadByte();

			for (int i = 0; i < len * 8; i++)
			{
				if ((rr.PeekPosition(i / 8) & (0x80 >> (i % 8))) != 0)
					TypeBitMaps.Add((DnsType)(window * 256 + i));
			}
			rr.Position += len;
		}
	}

	public override string ToString()
	{
		string saltStr = Salt.Length == 0 ? "-" : Convert.ToHexString(Salt);
		string optOut = OptOut ? " [Opt-Out]" : "";

		var sortedTypes = TypeBitMaps.Count == 0
			? ""
			: " " + string.Join(" ", TypeBitMaps.OrderBy(t => (ushort)t));

		return $"{HashAlgorithm} {Flags} {Iterations} {saltStr} {NextHashedOwnerName}{sortedTypes}{optOut}";
	}

}
