
/*
https://www.rfc-editor.org/rfc/rfc3123.html

4. APL RDATA format

   The RDATA section consists of zero or more items (<apitem>) of the
   form

      +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
      |                          ADDRESSFAMILY                        |
      +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
      |             PREFIX            | N |         AFDLENGTH         |
      +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
      /                            AFDPART                            /
      |                                                               |
      +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+

      ADDRESSFAMILY     16 bit unsigned value as assigned by IANA
                        (see IANA Considerations)
      PREFIX            8 bit unsigned binary coded prefix length.
                        Upper and lower bounds and interpretation of
                        this value are address family specific.
      N                 negation flag, indicates the presence of the
                        "!" character in the textual format.  It has
                        the value "1" if the "!" was given, "0" else.
      AFDLENGTH         length in octets of the following address
                        family dependent part (7 bit unsigned).
      AFDPART           address family dependent part.  See below.

   AFDPARTs for address families 1 (IPv4) and 2 (IPv6) families.
 */

using MailSharp.DNS.Records;
using System.Net;

namespace MailSharp.DNS.Records;

public record RecordAPL : DnsRecord
{
	public class APLItem
	{
		public ushort AddressFamily { get; set; }
		public byte Prefix { get; set; }
		public bool NegationFlag { get; set; }
		public IPAddress? IPAddress { get; set; }
		public override string ToString()
		{
			string negation = NegationFlag ? "!" : "";
			return $"{negation}{AddressFamily}:{IPAddress}/{Prefix}";
		}
	}
	public List<APLItem> APLItems { get; set; } = [];

	public RecordAPL(RecordReader rr) : base(rr)
	{
		// re-read length
		ushort rdLength = rr.ReadUInt16(-2);
		while (rr.Position < rdLength)
		{
			ushort addressFamily = rr.ReadUInt16();
			byte prefix = rr.ReadByte();
			byte afdLength = rr.ReadByte();
			bool negationFlag = (afdLength & 0x80) == 0x80; // check negation flag
			afdLength = (byte)(afdLength & 0x7F); // mask out negation flag

			byte[] afdPart = rr.ReadBytes(afdLength);
			IPAddress ipAddress = addressFamily switch
			{
				1 => new IPAddress(afdPart), // IPv4
				2 => new IPAddress(afdPart), // IPv6
				_ => throw new NotSupportedException($"Address family {addressFamily} is not supported.")
			};
			APLItems.Add(new APLItem
			{
				AddressFamily = addressFamily,
				NegationFlag = negationFlag,
				Prefix = prefix,
				IPAddress = ipAddress
			});
		}
	}

	public override string ToString() => string.Join(" ", APLItems.Select(x => $"{x}"));

}
