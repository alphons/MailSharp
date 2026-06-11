
/*
https://www.rfc-editor.org/rfc/rfc4025.html

 The RDATA for an IPSECKEY RR consists of a precedence value, a
   gateway type, a public key, algorithm type, and an optional gateway
   address.

       0                   1                   2                   3
       0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |  precedence   | gateway type  |  algorithm  |     gateway     |
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-------------+                 +
      ~                            gateway                            ~
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
      |                                                               /
      /                          public key                           /
      /                                                               /
      +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-|
 */

using MailSharp.DNS.Records;
using System.Net;

namespace MailSharp.DNS.Records;

public record RecordIPSECKEY : DnsRecord
{
	public byte Precedence { get; init; }
	public GatewayType GatewayType { get; init; }
	public byte Algorithm { get; init; }
	public IPAddress? GatewayAddress { get; init; }
	public string? GatewayName { get; init; }

	public byte[] PublicKey { get; init; }

	public RecordIPSECKEY(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16(-2);

		Precedence = rr.ReadByte();
		GatewayType = (GatewayType)rr.ReadByte();
		Algorithm = rr.ReadByte();

		switch (GatewayType)
		{
			case GatewayType.NoGateway:
				// 1 byte "."
				if (rr.ReadByte() != 0) throw new FormatException("Expected root label for . gateway");
				break;

			case GatewayType.IPv4:
				GatewayAddress = new IPAddress(rr.ReadBytes(4));
				break;

			case GatewayType.IPv6:
				GatewayAddress = new IPAddress(rr.ReadBytes(16));
				break;

			case GatewayType.WireFormatDomainName:
				GatewayName = rr.ReadDomainName();
				break;
		}

		PublicKey = rr.ReadBytes(rdLength - rr.Position);
	}

	public override string ToString()
	{
		string gateway = GatewayType switch
		{
			GatewayType.NoGateway => ".",
			GatewayType.IPv4 => GatewayAddress?.ToString() ?? "0.0.0.0",
			GatewayType.IPv6 => GatewayAddress?.ToString() ?? "::",
			GatewayType.WireFormatDomainName => GatewayName ?? ".",
			_ => "?"
		};

		// Public key wordt standaard als Base64 getoond (zonder newlines)
		string base64Key = PublicKey.Length == 0
			? "."
			: Convert.ToBase64String(PublicKey);

		return $"{Precedence} {(byte)GatewayType} {Algorithm} {gateway} {base64Key}";
	}

}
