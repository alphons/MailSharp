/*
https://datatracker.ietf.org/doc/draft-eastlake-kitchen-sink/02/
https://www.ietf.org/archive/id/draft-eastlake-kitchen-sink-02.txt

The RDATA portion of the SINK RR is structured as follows:

                                          1  1  1  1  1  1
            0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          |         coding        |       subcoding       |
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
          |                                               /
          /                     data                      /
          /                                               /
          +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordSINK : DnsRecord
{
	public byte Coding { get; init; }
	public byte SubCoding { get; init; }
	public byte[] Data { get; init; } = [];

	public RecordSINK(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16(-2);
		Coding = rr.ReadByte();
		SubCoding = rr.ReadByte();
		Data = rr.ReadBytes(rdLength - 2);
	}

	public override string ToString() =>
		$"{Coding} {SubCoding} {Convert.ToBase64String(Data)}";
}
