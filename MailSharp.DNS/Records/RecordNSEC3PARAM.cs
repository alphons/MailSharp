/*
 * https://www.rfc-editor.org/rfc/rfc5155.html
 * 
 * NSEC3PARAM RDATA Wire Format

   The RDATA of the NSEC3PARAM RR is as shown below:

                        1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |   Hash Alg.   |     Flags     |          Iterations           |
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
   |  Salt Length  |                     Salt                      /
   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

   
 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordNSEC3PARAM : DnsRecord
{
	public byte HashAlgorithm { get; init; }
	public byte Flags { get; init; }
	public ushort Iterations { get; init; }
	public byte[] Salt { get; init; } = [];

	public RecordNSEC3PARAM(RecordReader rr) : base(rr)
	{
		HashAlgorithm = rr.ReadByte();
		Flags = rr.ReadByte();
		Iterations = rr.ReadUInt16();

		byte saltLength = rr.ReadByte();
		Salt = saltLength == 0 ? [] : rr.ReadBytes(saltLength);
	}

	public override string ToString()
	{
		string saltStr = Salt.Length == 0 ? "-" : Convert.ToHexString(Salt);

		// Flags wordt altijd als decimaal getal getoond (meestal 0 of 1)
		return $"{HashAlgorithm} {Flags} {Iterations} {saltStr}";
	}

}
