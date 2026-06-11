using MailSharp.DNS;
using System.Buffers.Binary;

namespace MailSharp.DNS;

#region RFC specification
/*
4.1.1. Header section format

The header contains the following fields:

									1  1  1  1  1  1
	  0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|                      ID                       |
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|QR|   Opcode  |AA|TC|RD|RA|   Z    |   RCODE   |
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|                    QDCOUNT                    |
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|                    ANCOUNT                    |
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|                    NSCOUNT                    |
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|                    ARCOUNT                    |
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

	where:

	ID              A 16 bit identifier assigned by the program that
					generates any kind of query.  This identifier is copied
					the corresponding reply and can be used by the requester
					to match up replies to outstanding queries.

	QR              A one bit field that specifies whether this message is a
					query (0), or a response (1).

	OPCODE          A four bit field that specifies kind of query in this
					message.  This value is set by the originator of a query
					and copied into the response.  The values are:

					0               a standard query (QUERY)

					1               an inverse query (IQUERY)

					2               a server status request (STATUS)

					3-15            reserved for future use

	AA              Authoritative Answer - this bit is valid in responses,
					and specifies that the responding name server is an
					authority for the domain name in question section.

					Note that the contents of the answer section may have
					multiple owner names because of aliases.  The AA bit
					corresponds to the name which matches the query name, or
					the first owner name in the answer section.

	TC              TrunCation - specifies that this message was truncated
					due to length greater than that permitted on the
					transmission channel.

	RD              Recursion Desired - this bit may be set in a query and
					is copied into the response.  If RD is set, it directs
					the name server to pursue the query recursively.
					Recursive query support is optional.

	RA              Recursion Available - this be is set or cleared in a
					response, and denotes whether recursive query support is
					available in the name server.

	Z               Reserved for future use.  Must be zero in all queries
					and responses.

	RCODE           Response code - this 4 bit field is set as part of
					responses.  The values have the following
					interpretation:

					0               No error condition

					1               Format error - The name server was
									unable to interpret the query.

					2               Server failure - The name server was
									unable to process this query due to a
									problem with the name server.

					3               Name Error - Meaningful only for
									responses from an authoritative name
									server, this code signifies that the
									domain name referenced in the query does
									not exist.

					4               Not Implemented - The name server does
									not support the requested kind of query.

					5               Refused - The name server refuses to
									perform the specified operation for
									policy reasons.  For example, a name
									server may not wish to provide the
									information to the particular requester,
									or a name server may not wish to perform
									a particular operation (e.g., zone
									transfer) for particular data.

					6-15            Reserved for future use.

	QDCOUNT         an unsigned 16 bit integer specifying the number of
					entries in the question section.

	ANCOUNT         an unsigned 16 bit integer specifying the number of
					resource records in the answer section.

	NSCOUNT         an unsigned 16 bit integer specifying the number of name
					server resource records in the authority records
					section.

	ARCOUNT         an unsigned 16 bit integer specifying the number of
					resource records in the additional records section.

	*/
#endregion

public sealed class DnsHeader
{
	private readonly Memory<byte> memory;
	private Span<byte> Span => memory.Span;

	public DnsHeader(byte[] buffer)
	{
        ArgumentNullException.ThrowIfNull(buffer);
        if (buffer.Length < 12)
			throw new ArgumentException("Buffer too small for DNS header", nameof(buffer));

		memory = buffer.AsMemory(0,12);
	}

	public ushort ID
	{
		get => BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(0, 2));
		set => BinaryPrimitives.WriteUInt16BigEndian(Span.Slice(0, 2), value);
	}

	private ushort Flags
	{
		get => BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(2, 2));
		set => BinaryPrimitives.WriteUInt16BigEndian(Span.Slice(2, 2), value);
	}

	public ushort QDCount
	{
		get => BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(4, 2));
		set => BinaryPrimitives.WriteUInt16BigEndian(Span.Slice(4, 2), value);
	}

	public ushort ANCount
	{
		get => BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(6, 2));
		//set => BinaryPrimitives.WriteUInt16BigEndian(Span.Slice(6, 2), value);
	}

	public ushort NSCount
	{
		get => BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(8, 2));
		set => BinaryPrimitives.WriteUInt16BigEndian(Span.Slice(8, 2), value);
	}
	public ushort ARCount => BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(10, 2));

	// Bit fields – perfect zoals je ze hebt
	public bool QR
	{
		get => (Flags & 0x8000) != 0;
		set => Flags = (ushort)(value ? Flags | 0x8000 : Flags & ~0x8000);
	}

	public OPCode OPCODE
	{
		get => (OPCode)((Flags >> 11) & 0xF);
		set => Flags = (ushort)(Flags & ~0x7800 | ((ushort)value << 11));
	}

	public bool AA
	{
		get => (Flags & 0x0400) != 0;
		set => Flags = (ushort)(value ? Flags | 0x0400 : Flags & ~0x0400);
	}

	public bool TC
	{
		get => (Flags & 0x0200) != 0;
		set => Flags = (ushort)(value ? Flags | 0x0200 : Flags & ~0x0200);
	}

	public bool RD
	{
		get => (Flags & 0x0100) != 0;
		set => Flags = (ushort)(value ? Flags | 0x0100 : Flags & ~0x0100);
	}

	public bool RA
	{
		get => (Flags & 0x0080) != 0;
		set => Flags = (ushort)(value ? Flags | 0x0080 : Flags & ~0x0080);
	}

	public ushort Z
	{
		get => (ushort)((Flags >> 4) & 0x7);
		set => Flags = (ushort)(Flags & ~0x0070 | ((value & 0x7) << 4));
	}

	public RCode RCODE
	{
		get => (RCode)(Flags & 0x000F);
		set => Flags = (ushort)(Flags & ~0x000F | (ushort)value);
	}

	public ReadOnlySpan<byte> AsReadOnlySpan() => Span;
}
