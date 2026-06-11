/*

 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordDNSKEY : DnsRecord
{
	public ushort Flags { get; init; }
	public byte Protocol { get; init; }
	public byte Algorithm { get; init; }
	public byte[] PublicKey{ get; init; }

	public bool ZoneKey => (Flags & 0x0100) == 0x0100;
	public bool SecureEntryPoint => (Flags & 0x0001) == 0x0001;

	public RecordDNSKEY(RecordReader rr) : base(rr)
	{
		// re-read length
		ushort rdLength= rr.ReadUInt16(-2);
		Flags = rr.ReadUInt16();
		Protocol = rr.ReadByte();
		Algorithm = rr.ReadByte();
		PublicKey = rr.ReadBytes(rdLength - 4);
	}

	public override string ToString()
	{
		// Flags altijd als decimaal getal (geen 256/257 truc meer sinds RFC 8624 sectie 3.1)
		// Protocol altijd 3
		// Public key altijd Base64 (geen spaties, geen newlines)
		string base64 = PublicKey.Length == 0
			? "."
			: Convert.ToBase64String(PublicKey);

		return $"{Flags} {Protocol} {Algorithm} {base64}";
	}

}
