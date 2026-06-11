using MailSharp.DNS.Records;
using System.Buffers.Binary;
using System.Text;

namespace MailSharp.DNS;

public class RecordReader
{
	private readonly byte[] data;
	private int position;
	public RecordReader(byte[] data)
	{
		this.data = data;
		position = 0;
	}

	public RecordReader(byte[] data, int position)
	{
		this.data = data;
		this.position = position;
	}
	public int Position
	{
		get => position;
		set => position = value;
	}

	public byte ReadByte()
	{
		return data[position++];
	}

	public byte PeekPosition(int pos)
	{
		return data[pos];
	}

	public byte PeekByte()
	{
		return data[position];
	}

	public char ReadChar()
	{
		return (char)ReadByte();
	}

	public ushort ReadUInt16()
	{
		ushort value = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position));
		position += 2;
		return value;
	}

	public UInt16 ReadUInt16(int offset)
	{
		position += offset;
		return ReadUInt16();
	}

	public uint ReadUInt32()
	{
		uint value = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position));
		position += 4;
		return value;
	}

	public string ReadDomainName()
	{
		StringBuilder name = new();
		int length;

		// get  the length of the first label
		while ((length = ReadByte()) != 0)
		{
			// top 2 bits set denotes domain name compression and to reference elsewhere
			if ((length & 0xc0) == 0xc0)
			{
				// work out the existing domain name, copy this pointer
				RecordReader newRecordReader = new(data, (length & 0x3f) << 8 | ReadByte());

				name.Append(newRecordReader.ReadDomainName());
				return name.ToString();
			}

			// if not using compression, copy a char at a time to the domain name
			while (length > 0)
			{
				name.Append(ReadChar());
				length--;
			}
			name.Append('.');
		}
		if (name.Length == 0)
			return ".";
		else
			return name.ToString();
	}

	public string ReadString() =>ReadString(ReadByte());

	public string ReadString(int length) => Encoding.ASCII.GetString(ReadBytes(length));

	// changed 28 augustus 2008
	public byte[] ReadBytes(int count)
	{
		byte[] result = new byte[count];
		Buffer.BlockCopy(data, position, result, 0, count);
		position += count;
		return result;
	}

	public DnsRecord ReadRecord(DnsType type)
	{
		return type switch
		{
			DnsType.A => new RecordA(this),
			DnsType.NS => new RecordNS(this),
			DnsType.CNAME => new RecordCNAME(this),
			DnsType.SOA => new RecordSOA(this),
			DnsType.MB => new RecordMB(this),
			DnsType.MG => new RecordMG(this),
			DnsType.MR => new RecordMR(this),
			DnsType.NULL => new RecordNULL(this),
			DnsType.WKS => new RecordWKS(this),
			DnsType.PTR => new RecordPTR(this),
			DnsType.HINFO => new RecordHINFO(this),
			DnsType.MINFO => new RecordMINFO(this),
			DnsType.MX => new RecordMX(this),
			DnsType.TXT => new RecordTXT(this),
			DnsType.RP => new RecordRP(this),
			DnsType.AFSDB => new RecordAFSDB(this),
			DnsType.X25 => new RecordX25(this),
			DnsType.ISDN => new RecordISDN(this),
			DnsType.RT => new RecordRT(this),
			DnsType.NSAP => new RecordNSAP(this),
			DnsType.SIG => new RecordSIG(this),
			DnsType.KEY => new RecordKEY(this),
			DnsType.PX => new RecordPX(this),
			DnsType.AAAA => new RecordAAAA(this),
			DnsType.LOC => new RecordLOC(this),
			DnsType.SRV => new RecordSRV(this),
			DnsType.NAPTR => new RecordNAPTR(this),
			DnsType.KX => new RecordKX(this),
			DnsType.DNAME => new RecordDNAME(this),
			DnsType.DS => new RecordDS(this),
			DnsType.TKEY => new RecordTKEY(this),
			DnsType.TSIG => new RecordTSIG(this),
			DnsType.CAA => new RecordCAA(this),

			DnsType.A6 => new RecordA6(this),
			DnsType.SPF => new RecordSPF(this),
			DnsType.IPSECKEY => new RecordIPSECKEY(this),
			DnsType.APL => new RecordAPL(this),

			DnsType.RRSIG => new RecordRRSIG(this),
			DnsType.DNSKEY => new RecordDNSKEY(this),
			DnsType.NSEC => new RecordNSEC(this),
			DnsType.NSEC3 => new RecordNSEC3(this),
			DnsType.NSEC3PARAM => new RecordNSEC3PARAM(this),

			DnsType.CERT => new RecordCERT(this),

			DnsType.EID => new RecordEID(this),
			DnsType.NIMLOC => new RecordNIMLOC(this),
			DnsType.ATMA => new RecordATMA(this),
			DnsType.SINK => new RecordSINK(this),
			DnsType.SSHFP => new RecordSSHFP(this),
			DnsType.DHCID => new RecordDHCID(this),
			DnsType.HIP => new RecordHIP(this),
			DnsType.UINFO => new RecordUINFO(this),
			DnsType.UID => new RecordUID(this),
			DnsType.GID => new RecordGID(this),
			DnsType.UNSPEC => new RecordUNSPEC(this),

			// Incomplete implementation
			DnsType.OPT => new RecordOPT(this),

			// Obsolete records
			DnsType.GPOS => new RecordGPOS(this),
			DnsType.NSAPPTR => new RecordNSAPPTR(this),
			DnsType.NXT => new RecordNXT(this),
			DnsType.MD => new RecordMD(this),
			DnsType.MF => new RecordMF(this),


			_ => new RecordUnknown(this),
		};
	}

}
