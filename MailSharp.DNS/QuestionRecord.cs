using System.Buffers.Binary;

namespace MailSharp.DNS;

#region Rfc 1034/1035
/*
4.1.2. Question section format

The question section is used to carry the "question" in most queries,
i.e., the parameters that define what is being asked.  The section
contains QDCOUNT (usually 1) entries, each of the following format:

									1  1  1  1  1  1
	  0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|                                               |
	/                     QNAME                     /
	/                                               /
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|                     QTYPE                     |
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|                     QCLASS                    |
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

where:

QNAME           a domain name represented as a sequence of labels, where
				each label consists of a length octet followed by that
				number of octets.  The domain name terminates with the
				zero length octet for the null label of the root.  Note
				that this field may be an odd number of octets; no
				padding is used.

QTYPE           a two octet code which specifies the type of the query.
				The values for this field include all codes valid for a
				TYPE field, together with some more general codes which
				can match more than one type of RR.


QCLASS          a two octet code that specifies the class of the query.
				For example, the QCLASS field is IN for the Internet.
*/
#endregion

public sealed class QuestionRecord : DnsResourceRecord<DnsQType, DnsQClass>
{
	public QuestionRecord(string name, DnsQType type, DnsQClass @class = DnsQClass.IN)
		: base(name, type, @class)
	{
	}

	// Parsing constructor
	public QuestionRecord(ref RecordReader rr)
	{
		Name = rr.ReadDomainName();
		Type = (DnsQType)rr.ReadUInt16();
		Class = (DnsQClass)rr.ReadUInt16();
	}

	// Schrijft direct naar een Span<byte> → zero alloc
	public int WriteTo(Span<byte> destination)
	{
		int written = DnsWriter.WriteDomainName(destination, Name);
		BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(written, 2), (ushort)Type);
		BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(written + 2, 2), (ushort)Class);
		return written + 4;
	}
}
