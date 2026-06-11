using MailSharp.DNS.Records;

namespace MailSharp.DNS;

#region RFC info
/*
3.2. RR definitions

3.2.1. Format

All RRs have the same top level format shown below:

									1  1  1  1  1  1
	  0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|                                               |
	/                                               /
	/                      NAME                     /
	|                                               |
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|                      TYPE                     |
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|                     CLASS                     |
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|                      TTL                      |
	|                                               |
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	|                   RDLENGTH                    |
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--|
	/                     RDATA                     /
	/                                               /
	+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+


where:

NAME            an owner name, i.e., the name of the node to which this
				resource record pertains.

TYPE            two octets containing one of the RR TYPE codes.

CLASS           two octets containing one of the RR CLASS codes.

TTL             a 32 bit signed integer that specifies the time interval
				that the resource record may be cached before the source
				of the information should again be consulted.  Zero
				values are interpreted to mean that the RR can only be
				used for the transaction in progress, and should not be
				cached.  For example, SOA records are always distributed
				with a zero TTL to prohibit caching.  Zero values can
				also be used for extremely volatile data.

RDLENGTH        an unsigned 16 bit integer that specifies the length in
				octets of the RDATA field.

RDATA           a variable length string of octets that describes the
				resource.  The format of this information varies
				according to the TYPE and CLASS of the resource record.
*/
#endregion

/// <summary>
/// Resource Record (rfc1034 3.6.)
/// </summary>
public class RR : DnsResourceRecord<DnsType, DnsClass>
{
	/// <summary>
	/// Time to live, the time interval that the resource record may be cached
	/// </summary>
	public uint TTL
	{
		get
		{
			return (uint)Math.Max(0, m_TTL - TimeLived);
		}
		set
		{
			m_TTL = value;
		}
	}
	private uint m_TTL;

	/// <summary>
	/// 
	/// </summary>
	public ushort RDLength;

	/// <summary>
	/// One of the Record* classes
	/// </summary>
	public DnsRecord? Record;

	public int TimeLived => DateTime.Now.Subtract(TimeStamp).Seconds;

	// when this record was received
	public DateTime TimeStamp;

	public RR()
	{
	}

	public RR(RecordReader rr)
	{
		TimeStamp = DateTime.Now;
		Name = rr.ReadDomainName();
		Type = (DnsType)rr.ReadUInt16();
		Class = (DnsClass)rr.ReadUInt16();
		TTL = rr.ReadUInt32();
		RDLength = rr.ReadUInt16();
		Record = rr.ReadRecord(Type);
	}

	public override string ToString()
	{
		return string.Format("{0,-32} {1}\t{2}\t{3}\t{4}",
			Name,
			TTL,
			Class,
			Type,
			Record);
	}
}
