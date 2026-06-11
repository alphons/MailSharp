/*
https://www.rfc-editor.org/rfc/rfc3225.html

The variable part of an OPT RR may contain zero or more options in
   the RDATA.  Each option MUST be treated as a bit field.  Each option
   is encoded as:

                  +0 (MSB)                            +1 (LSB)
       +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
    0: |                          OPTION-CODE                          |
       +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
    2: |                         OPTION-LENGTH                         |
       +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
    4: |                                                               |
       /                          OPTION-DATA                          /
       /                                                               /
       +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+

   OPTION-CODE
      Assigned by the Expert Review process as defined by the DNSEXT
      working group and the IESG.

   OPTION-LENGTH
      Size (in octets) of OPTION-DATA.

   OPTION-DATA
      Varies per OPTION-CODE.  MUST be treated as a bit field.
 */

using MailSharp.DNS.Records;
using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace MailSharp.DNS.Records;


public record RecordOPT : DnsRecord
{
	//public ushort UdpPayloadSize { get; init; }
	//public byte ExtendedRcode { get; init; }
	//public byte Version { get; init; }  // moet 0 zijn
	//public ushort Flags { get; init; }  // bit 0 = DO (DNSSEC OK)
	public List<EdnsOption> Options { get; init; } = new();

	//public bool DNSSEC_OK => (Flags & 0x8000) != 0;

	public RecordOPT(RecordReader rr) : base(rr)
	{
		// OPT is een pseudo-record: owner = root, type = 41, class = UDP size, TTL = speciaal
		//UdpPayloadSize = rr.Class;           // "class" veld is hier UDP payload size
		//ExtendedRcode = (byte)(rr.Ttl >> 24);
		//Version = (byte)(rr.Ttl >> 16);
		//Flags = (ushort)(rr.Ttl & 0xFFFF);

		ushort rdLength = rr.ReadUInt16(-2);  // RDLEN

		while (rr.Position < rdLength)
		{
			ushort code = rr.ReadUInt16();
			ushort len = rr.ReadUInt16();
			byte[] data = rr.ReadBytes(len);
			Options.Add(new EdnsOption(code, data));
		}
	}

	public override string ToString()
	{
		//var parts = new List<string>
		//{
		//	$"; EDNS: version: {Version}, flags:{(DNSSEC_OK ? " do" : "")}; udp: {UdpPayloadSize}"
		//};

		//if (Options.Count > 0)
		//	parts.Add("; " + string.Join("; ", Options));

		//if (ExtendedRcode != 0)
		//	parts.Insert(0, $"; EDE: {ExtendedRcode}");

		return "Incomplete";// string.Join(Environment.NewLine, parts);
	}




	public record EdnsOption(ushort Code, byte[] Data)
	{
		public override string ToString()
		{
			return Code switch
			{
				// https://www.iana.org/assignments/dns-parameters/dns-parameters.xhtml#dns-parameters-11
				1 => "LLQ",                                  // Legacy
				2 => "UL",                                   // Legacy
				3 => $"NSID {ToHexOrAscii(Data)}",           // NSID
				5 => $"DAU {FormatByteList(Data)}",          // DNSSEC Algorithm Understood
				6 => $"DHU {FormatByteList(Data)}",          // DS Hash Understood
				7 => $"N3U {FormatByteList(Data)}",          // NSEC3 Hash Understood
				8 => $"CLIENT-SUBNET {FormatClientSubnet(Data)}", // EDNS Client Subnet (RFC 7871)
				9 => $"EXPIRE {BinaryPrimitives.ReadUInt32BigEndian(Data)}",
				10 => $"COOKIE {Convert.ToHexString(Data)}",
				11 => "TCP-KEEPALIVE",                       // RFC 7828
				12 => $"PADDING {Data.Length} bytes",
				13 => "CHAIN",                               // RFC 7901
				14 => $"KEY-TAG {string.Join(" ", Data.Select(b => b.ToString()))}",
				15 => "DEVICEID",                            // Draft
				16 => "EXTENDED-ERROR",                      // RFC 8914
				26944 => "PROXY",                            // Custom, vaak gebruikt
				_ => $"OPTION-{Code} {Convert.ToHexString(Data)}"
			};
		}

		private static string ToHexOrAscii(byte[] data)
		{
			if (data.All(b => b >= 32 && b <= 126))
				return $"\"{Encoding.ASCII.GetString(data)}\"";
			return Convert.ToHexString(data);
		}

		private static string FormatByteList(byte[] data) =>
			string.Join(" ", data);

		private static string FormatClientSubnet(byte[] data)
		{
			if (data.Length < 4) return Convert.ToHexString(data);

			ushort family = BinaryPrimitives.ReadUInt16BigEndian(data);
			byte sourcePrefix = data[2];
			byte scopePrefix = data[3];
			var addrBytes = data.AsSpan(4);

			string address = family switch
			{
				1 => new IPAddress(addrBytes.Slice(0, 4)).ToString(),
				2 => new IPAddress(addrBytes.Slice(0, 16)).ToString(),
				_ => Convert.ToHexString(addrBytes.ToArray())
			};

			return $"{family} {address}/{sourcePrefix}/{scopePrefix}";
		}
	}
}