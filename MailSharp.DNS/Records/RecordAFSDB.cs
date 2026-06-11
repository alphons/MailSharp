using MailSharp.DNS;
using MailSharp.DNS.Records;
using System;
/* http://tools.ietf.org/rfc/rfc1183.txt

 * 1. AFS Data Base location

   This section defines an extension of the DNS to locate servers both
   for AFS (AFS is a registered trademark of Transarc Corporation) and
   for the Open Software Foundation's (OSF) Distributed Computing
   Environment (DCE) authenticated naming system using HP/Apollo's NCA,
   both to be components of the OSF DCE.  The discussion assumes that
   the reader is familiar with AFS [5] and NCA [6].

   The AFS (originally the Andrew File System) system uses the DNS to
   map from a domain name to the name of an AFS cell database server.
   The DCE Naming service uses the DNS for a similar function: mapping
   from the domain name of a cell to authenticated name servers for that
   cell.  The method uses a new RR type with mnemonic AFSDB and type
   code of 18 (decimal).

   AFSDB has the following format:

   <owner> <ttl> <class> AFSDB <subtype> <hostname>

   Both RDATA fields are required in all AFSDB RRs.  The <subtype> field
   is a 16 bit integer.  The <hostname> field is a domain name of a host
   that has a server for the cell named by the owner name of the RR.

 */

namespace MailSharp.DNS.Records;

public record RecordAFSDB(RecordReader rr) : DnsRecord(rr)
{
	public ushort SubType { get; } = rr.ReadUInt16();
	public string HostName { get; } = rr.ReadDomainName();

	public override string ToString() => $"{SubType} {HostName}";
}
