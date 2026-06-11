using MailSharp.DNS.Records;
using System.Net;
/*
 * RFC8659
 RDATA format

+0-1-2-3-4-5-6-7-|0-1-2-3-4-5-6-7-|
| Flags          | Tag Length = n |
+----------------|----------------+...+---------------+
| Tag char 0     | Tag char 1     |...| Tag char n-1  |
+----------------|----------------+...+---------------+
+----------------|----------------+.....+----------------+
| Value byte 0   | Value byte 1   |.....| Value byte m-1 |
+----------------|----------------+.....+----------------+

 * 
 */
namespace MailSharp.DNS.Records;

public record RecordCAA : DnsRecord
{
	public byte Flags { get; }
	public string Tag { get; }
	public string Value { get; }

	public RecordCAA(RecordReader rr) : base(rr)
	{
		ushort rdlength = rr.ReadUInt16(-2);
		this.Flags = rr.ReadByte();
		ushort tagLength = rr.ReadByte();
		this.Tag = rr.ReadString(tagLength);
		this.Value = rr.ReadString(rdlength - tagLength - 2);
	}

	public override string ToString() => $"{Flags} {Tag} {Value}";
}
