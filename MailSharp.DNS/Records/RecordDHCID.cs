/*
https://www.rfc-editor.org/rfc/rfc4701.html
The RDATA section of a DHCID RR in transmission contains RDLENGTH
   octets of binary data.  The format of this data and its
   interpretation by DHCP servers and clients are described below.

   DNS software should consider the RDATA section to be opaque.  DHCP
   clients or servers use the DHCID RR to associate a DHCP client's
   identity with a DNS name, so that multiple DHCP clients and servers
   may deterministically perform dynamic DNS updates to the same zone.
   From the updater's perspective, the DHCID resource record RDATA
   consists of a 2-octet identifier type, in network byte order,
   followed by a 1-octet digest type, followed by one or more octets
   representing the actual identifier:

           < 2 octets >    Identifier type code
           < 1 octet >     Digest type code
           < n octets >    Digest (length depends on digest type)
 */

using MailSharp.DNS.Records;

namespace MailSharp.DNS.Records;

public record RecordDHCID : DnsRecord
{
	public byte[] Data { get; init; } = [];

	public RecordDHCID(RecordReader rr) : base(rr)
	{
		ushort rdLength = rr.ReadUInt16(-2);
		Data = rr.ReadBytes(rdLength);
	}

	public override string ToString() =>
		Convert.ToBase64String(Data);
}