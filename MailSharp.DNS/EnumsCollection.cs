/*
 * http://www.iana.org/assignments/dns-parameters
 * 
 * https://www.iana.org/assignments/dns-parameters/dns-parameters.xhtml
 * 
 */


namespace MailSharp.DNS;

/*
 * 3.2.2. TYPE values
 *
 * TYPE fields are used in resource records.
 * Note that these types are a subset of QTYPEs.
 *
 *		TYPE		value			meaning
 */
public enum DnsType : ushort
{
	Unknown		= 0,        // Reserved Unknown type
	A			= 1,		// a IPV4 host address
	NS			= 2,		// an authoritative name server
	MD			= 3,		// a mail destination (Obsolete - use MX)
	MF			= 4,		// a mail forwarder (Obsolete - use MX)
	CNAME		= 5,		// the canonical name for an alias
	SOA			= 6,		// marks the start of a zone of authority
	MB			= 7,		// a mailbox domain name (EXPERIMENTAL)
	MG			= 8,		// a mail group member (EXPERIMENTAL)
	MR			= 9,		// a mail rename domain name (EXPERIMENTAL)
	NULL		= 10,		// a null RR (EXPERIMENTAL)
	WKS			= 11,		// a well known service description
	PTR			= 12,		// a domain name pointer
	HINFO		= 13,		// host information
	MINFO		= 14,		// mailbox or mail list information
	MX			= 15,		// mail exchange
	TXT			= 16,		// text strings

	RP			= 17,		// The Responsible Person rfc1183
	AFSDB		= 18,		// AFS Data Base location
	X25			= 19,		// X.25 address rfc1183
	ISDN		= 20,		// ISDN address rfc1183 
	RT			= 21,		// The Route Through rfc1183

	NSAP		= 22,		// Network service access point address rfc1706
	NSAPPTR		= 23,		// Obsolete, rfc1348

	SIG			= 24,		// Cryptographic public key signature rfc2931 / rfc2535
	KEY			= 25,		// Public key as used in DNSSEC rfc2535

	PX			= 26,		// Pointer to X.400/RFC822 mail mapping information rfc2163

	GPOS		= 27,		// Geographical position rfc1712 (obsolete)

	AAAA		= 28,		// a IPV6 host address, rfc3596

	LOC			= 29,		// Location information rfc1876

	NXT			= 30,		// Next Domain, Obsolete rfc2065 / rfc2535

	EID			= 31,		// *** Endpoint Identifier (Patton)
	NIMLOC		= 32,		// *** Nimrod Locator (Patton)

	SRV			= 33,		// Location of services rfc2782

	ATMA		= 34,		// *** ATM Address (Dobrowski)

	NAPTR		= 35,		// The Naming Authority Pointer rfc3403

	KX			= 36,		// Key Exchange Delegation Record rfc2230

	CERT		= 37,		// *** CERT RFC2538

	A6			= 38,		// IPv6 address rfc3363 (rfc2874 rfc3226)
	DNAME		= 39,		// A way to provide aliases for a whole domain, not just a single domain name as with CNAME. rfc2672

	SINK		= 40,		// *** SINK Eastlake
	OPT			= 41,		// *** OPT RFC2671

	APL			= 42,		// *** APL [RFC3123]

	DS			= 43,		// Delegation Signer rfc3658

	SSHFP		= 44,		// SSH Key Fingerprint rfc4255
	IPSECKEY	= 45,		// IPSECKEY rfc4025
	RRSIG		= 46,		// RRSIG rfc3755
	NSEC		= 47,		// NSEC rfc3755
	DNSKEY		= 48,		// DNSKEY 3755
	DHCID		= 49,		// DHCID rfc4701

	NSEC3		= 50,		// NSEC3 rfc5155
	NSEC3PARAM	= 51,       // NSEC3PARAM rfc5155

	TLSA		= 52,		// TLSA    [RFC6698]
	SMIMEA		= 53,		// S/MIME cert association [RFC8162] SMIMEA/smimea-completed-template	2015-12-01
	Unassigned	= 54,				
	HIP			= 55,		// Host Identity Protocol  [RFC8005]
	NINFO		= 56,		// NINFO   [Jim_Reid] NINFO/ninfo-completed-template	2008-01-21
	RKEY		= 57,		// RKEY    [Jim_Reid] RKEY/rkey-completed-template	2008-01-21
	TALINK		= 58,		// Trust Anchor LINK   [Wouter_Wijngaards] TALINK/talink-completed-template	2010-02-17
	CDS			= 59,		// Child DS    [RFC7344] CDS/cds-completed-template	2011-06-06
	CDNSKEY		= 60,		// DNSKEY(s) the Child wants reflected in DS   [RFC7344]       2014-06-16
	OPENPGPKEY	= 61,		// OpenPGP Key [RFC7929] OPENPGPKEY/openpgpkey-completed-template	2014-08-12
	CSYNC		= 62,		// Child-To-Parent Synchronization [RFC7477]       2015-01-27
	ZONEMD		= 63,		// Message Digest Over Zone Data   [RFC8976] ZONEMD/zonemd-completed-template	2018-12-12
	SVCB		= 64,		// General-purpose service binding [RFC9460] SVCB/svcb-completed-template	2020-06-30
	HTTPS		= 65,		// SVCB-compatible type for use with HTTP  [RFC9460] HTTPS/https-completed-template	2020-06-30
	DSYNC		= 66,		// Endpoint discovery for delegation synchronization   [RFC9859] DSYNC/dsync-completed-template	2024-12-10
	HHIT		= 67,		// Hierarchical Host Identity Tag  [draft-ietf-drip-registries-28] HHIT/hhit-completed-template	2025-05-20
	BRID		= 68,		// UAS Broadcast Remote Identification [draft-ietf-drip-registries-28] BRID/brid-completed-template	2025-05-20

	//Unassigned 69-98				

	SPF			= 99,		// SPF rfc4408

	UINFO		= 100,		// *** IANA-Reserved
	UID			= 101,		// *** IANA-Reserved
	GID			= 102,		// *** IANA-Reserved
	UNSPEC		= 103,      // *** IANA-Reserved

	NID			= 104,		// [RFC6742]	ILNP/nid-completed-template	
	L32			= 105,		// [RFC6742]	ILNP/l32-completed-template	
	L64			= 106,		// [RFC6742]	ILNP/l64-completed-template	
	LP			= 107,		// [RFC6742]	ILNP/lp-completed-template	
	EUI48		= 108,		// an EUI-48 address	[RFC7043]	EUI48/eui48-completed-template	2013-03-27
	EUI64		= 109,		// an EUI-64 address	[RFC7043]	EUI64/eui64-completed-template	2013-03-27
							// Unassigned	110-127				
	NXNAME		= 128,		// NXDOMAIN indicator for Compact Denial of Existence	[RFC9824]
							// Unassigned	129-248				
	TKEY		= 249,		// Transaction Key	[RFC2930]		
	TSIG		= 250,		// Transaction Signature	[RFC8945]		
	IXFR		= 251,		// incremental transfer	[RFC1995]		
	AXFR		= 252,		// transfer of an entire zone	[RFC1035][RFC5936]		
	MAILB		= 253,		// mailbox-related RRs (MB, MG or MR)	[RFC1035]		
	MAILA		= 254,		// mail agent RRs (OBSOLETE - see MX)	[RFC1035]

	ANY			= 255,      // A request for some or all records the server has available	[RFC1035][RFC6895][RFC8482]

	URI			= 256,		// URI	[RFC7553]	URI/uri-completed-template	2011-02-22
	CAA			= 257,		// Certification Authority Restriction	[RFC8659]	CAA/caa-completed-template	2011-04-07
	AVC			= 258,		// Application Visibility and Control	[Wolfgang_Riedel]	AVC/avc-completed-template	2016-02-26
	DOA			= 259,		// Digital Object Architecture	[draft-durand-doa-over-dns-02]	DOA/doa-completed-template	2017-08-30
	AMTRELAY	= 260,		// Automatic Multicast Tunneling Relay	[RFC8777]	AMTRELAY/amtrelay-completed-template	2019-02-06
	RESINFO		= 261,		// Resolver Information as Key/Value Pairs	[RFC9606]	RESINFO/resinfo-completed-template	2023-11-02
	WALLET		= 262,		// Public wallet address	[Paul_Hoffman]	WALLET/wallet-completed-template	2024-06-21
	CLA			= 263,		// BP Convergence Layer Adapter	[draft-johnson-dns-ipn-cla-07]	CLA/cla-completed-template	2024-07-25
	IPN			= 264,		// BP Node Number	[draft-johnson-dns-ipn-cla-07]	IPN/ipn-completed-template	2024-07-25
							// Unassigned	265-32767				
	TA			= 32768,	// DNSSEC Trust Authorities	[Sam_Weiler]
	DLV			= 32769		// DNSSEC Lookaside Validation (OBSOLETE)	[RFC8749][RFC4431]
}

/*
 * 3.2.3. QTYPE values
 *
 * QTYPE fields appear in the question part of a query.  QTYPES are a
 * superset of TYPEs, hence all TYPEs are valid QTYPEs.  In addition, the
 * following QTYPEs are defined:
 *
 *		QTYPE		value			meaning
 */
public enum DnsQType : ushort
{
	A			= DnsType.A,		// a IPV4 host address
	NS			= DnsType.NS,		// an authoritative name server
	MD			= DnsType.MD,		// a mail destination (Obsolete - use MX)
	MF			= DnsType.MF,		// a mail forwarder (Obsolete - use MX)
	CNAME		= DnsType.CNAME,	// the canonical name for an alias
	SOA			= DnsType.SOA,		// marks the start of a zone of authority
	MB			= DnsType.MB,		// a mailbox domain name (EXPERIMENTAL)
	MG			= DnsType.MG,		// a mail group member (EXPERIMENTAL)
	MR			= DnsType.MR,		// a mail rename domain name (EXPERIMENTAL)
	NULL		= DnsType.NULL,		// a null RR (EXPERIMENTAL)
	WKS			= DnsType.WKS,		// a well known service description
	PTR			= DnsType.PTR,		// a domain name pointer
	HINFO		= DnsType.HINFO,	// host information
	MINFO		= DnsType.MINFO,	// mailbox or mail list information
	MX			= DnsType.MX,		// mail exchange
	TXT			= DnsType.TXT,		// text strings

	RP			= DnsType.RP,		// The Responsible Person rfc1183
	AFSDB		= DnsType.AFSDB,	// AFS Data Base location
	X25			= DnsType.X25,		// X.25 address rfc1183
	ISDN		= DnsType.ISDN,		// ISDN address rfc1183
	RT			= DnsType.RT,		// The Route Through rfc1183

	NSAP		= DnsType.NSAP,		// Network service access point address rfc1706
	NSAP_PTR	= DnsType.NSAPPTR,	// Obsolete, rfc1348

	SIG			= DnsType.SIG,		// Cryptographic public key signature rfc2931 / rfc2535
	KEY			= DnsType.KEY,		// Public key as used in DNSSEC rfc2535

	PX			= DnsType.PX,		// Pointer to X.400/RFC822 mail mapping information rfc2163

	GPOS		= DnsType.GPOS,		// Geographical position rfc1712 (obsolete)

	AAAA		= DnsType.AAAA,		// a IPV6 host address

	LOC			= DnsType.LOC,		// Location information rfc1876

	NXT			= DnsType.NXT,		// Obsolete rfc2065 / rfc2535

	EID			= DnsType.EID,		// *** Endpoint Identifier (Patton)
	NIMLOC		= DnsType.NIMLOC,	// *** Nimrod Locator (Patton)

	SRV			= DnsType.SRV,		// Location of services rfc2782

	ATMA		= DnsType.ATMA,		// *** ATM Address (Dobrowski)

	NAPTR		= DnsType.NAPTR,	// The Naming Authority Pointer rfc3403

	KX			= DnsType.KX,		// Key Exchange Delegation Record rfc2230

	CERT		= DnsType.CERT,		// *** CERT RFC2538

	A6			= DnsType.A6,		// IPv6 address rfc3363
	DNAME		= DnsType.DNAME,	// A way to provide aliases for a whole domain, not just a single domain name as with CNAME. rfc2672

	SINK		= DnsType.SINK,		// *** SINK Eastlake
	OPT			= DnsType.OPT,		// *** OPT RFC2671

	APL			= DnsType.APL,		// *** APL [RFC3123]

	DS			= DnsType.DS,		// Delegation Signer rfc3658

	SSHFP		= DnsType.SSHFP,	// *** SSH Key Fingerprint RFC-ietf-secsh-dns
	IPSECKEY	= DnsType.IPSECKEY,	// rfc4025
	RRSIG		= DnsType.RRSIG,	// *** RRSIG RFC-ietf-dnsext-dnssec-2535
	NSEC		= DnsType.NSEC,		// *** NSEC RFC-ietf-dnsext-dnssec-2535
	DNSKEY		= DnsType.DNSKEY,	// *** DNSKEY RFC-ietf-dnsext-dnssec-2535
	DHCID		= DnsType.DHCID,	// rfc4701

	NSEC3		= DnsType.NSEC3,	// RFC5155
	NSEC3PARAM	= DnsType.NSEC3PARAM, // RFC5155

	TLSA		= DnsType.TLSA,		// TLSA    [RFC6698]
	SMIMEA		= DnsType.SMIMEA,	// S/MIME cert association [RFC8162] SMIMEA/smimea-completed-template	2015-12-01
	Unassigned	= DnsType.Unassigned,
	HIP			= DnsType.HIP,		// Host Identity Protocol  [RFC8005]
	NINFO		= DnsType.NINFO,	// NINFO   [Jim_Reid] NINFO/ninfo-completed-template	2008-01-21
	RKEY		= DnsType.RKEY,		// RKEY    [Jim_Reid] RKEY/rkey-completed-template	2008-01-21
	TALINK		= DnsType.TALINK,	// Trust Anchor LINK   [Wouter_Wijngaards] TALINK/talink-completed-template	2010-02-17
	CDS			= DnsType.CDS,		// Child DS    [RFC7344] CDS/cds-completed-template	2011-06-06
	CDNSKEY		= DnsType.CDNSKEY,	// DNSKEY(s) the Child wants reflected in DS   [RFC7344]       2014-06-16
	OPENPGPKEY	= DnsType.OPENPGPKEY, // OpenPGP Key [RFC7929] OPENPGPKEY/openpgpkey-completed-template	2014-08-12
	CSYNC		= DnsType.CSYNC,	// Child-To-Parent Synchronization [RFC7477]       2015-01-27
	ZONEMD		= DnsType.ZONEMD,	// Message Digest Over Zone Data   [RFC8976] ZONEMD/zonemd-completed-template	2018-12-12
	SVCB		= DnsType.SVCB,		// General-purpose service binding [RFC9460] SVCB/svcb-completed-template	2020-06-30
	HTTPS		= DnsType.HTTPS,	// SVCB-compatible type for use with HTTP  [RFC9460] HTTPS/https-completed-template	2020-06-30
	DSYNC		= DnsType.DSYNC,	// Endpoint discovery for delegation synchronization   [RFC9859] DSYNC/dsync-completed-template	2024-12-10
	HHIT		= DnsType.HHIT,		// Hierarchical Host Identity Tag  [draft-ietf-drip-registries-28] HHIT/hhit-completed-template	2025-05-20
	BRID		= DnsType.BRID,		// UAS Broadcast Remote Identification [draft-ietf-drip-registries-28] BRID/brid-completed-template	2025-05-20

	//Unassigned 69-98				

	SPF			= DnsType.SPF,		// RFC4408
	UINFO		= DnsType.UINFO,	// *** IANA-Reserved
	UID			= DnsType.UID,		// *** IANA-Reserved
	GID			= DnsType.GID,		// *** IANA-Reserved
	UNSPEC		= DnsType.UNSPEC,   // *** IANA-Reserved

	NID			= DnsType.NID,		// [RFC6742]	ILNP/nid-completed-template	
	L32			= DnsType.L32,		// [RFC6742]	ILNP/l32-completed-template	
	L64			= DnsType.L64,		// [RFC6742]	ILNP/l64-completed-template	
	LP			= DnsType.LP,		// [RFC6742]	ILNP/lp-completed-template	
	EUI48		= DnsType.EUI48,	// an EUI-48 address	[RFC7043]	EUI48/eui48-completed-template	2013-03-27
	EUI64		= DnsType.EUI64,	// an EUI-64 address	[RFC7043]	EUI64/eui64-completed-template	2013-03-27
									// Unassigned	110-127				
	NXNAME		= DnsType.NXNAME,	// NXDOMAIN indicator for Compact Denial of Existence	[RFC9824]
									// Unassigned	129-248				
	TKEY		= DnsType.TKEY,		// Transaction Key	[RFC2930]		
	TSIG		= DnsType.TSIG,		// Transaction Signature	[RFC8945]		
	IXFR		= DnsType.IXFR,		// incremental transfer	[RFC1995]		
	AXFR		= DnsType.AXFR,		// transfer of an entire zone	[RFC1035][RFC5936]		
	MAILB		= DnsType.MAILB,	// mailbox-related RRs (MB, MG or MR)	[RFC1035]		
	MAILA		= DnsType.MAILA,	// mail agent RRs (OBSOLETE - see MX)	[RFC1035]

	ANY			= DnsType.ANY,      // A request for some or all records the server has available	[RFC1035][RFC6895][RFC8482]	

	URI			= DnsType.URI,		// URI	[RFC7553]	URI/uri-completed-template	2011-02-22
	CAA			= DnsType.CAA,		// Certification Authority Restriction	[RFC8659]	CAA/caa-completed-template	2011-04-07
	AVC			= DnsType.AVC,		// Application Visibility and Control	[Wolfgang_Riedel]	AVC/avc-completed-template	2016-02-26
	DOA			= DnsType.DOA,		// Digital Object Architecture	[draft-durand-doa-over-dns-02]	DOA/doa-completed-template	2017-08-30
	AMTRELAY	= DnsType.AMTRELAY,	// Automatic Multicast Tunneling Relay	[RFC8777]	AMTRELAY/amtrelay-completed-template	2019-02-06
	RESINFO		= DnsType.RESINFO,	// Resolver Information as Key/Value Pairs	[RFC9606]	RESINFO/resinfo-completed-template	2023-11-02
	WALLET		= DnsType.WALLET,	// Public wallet address	[Paul_Hoffman]	WALLET/wallet-completed-template	2024-06-21
	CLA			= DnsType.CLA,		// BP Convergence Layer Adapter	[draft-johnson-dns-ipn-cla-07]	CLA/cla-completed-template	2024-07-25
	IPN			= DnsType.IPN,		// BP Node Number	[draft-johnson-dns-ipn-cla-07]	IPN/ipn-completed-template	2024-07-25
									// Unassigned	265-32767				
	TA			= DnsType.TA,		// DNSSEC Trust Authorities	[Sam_Weiler]
	DLV			= DnsType.DLV		// DNSSEC Lookaside Validation (OBSOLETE)	[RFC8749][RFC4431]
}
/*
 * 3.2.4. CLASS values
 *
 * CLASS fields appear in resource records.  The following CLASS mnemonics
 *and values are defined:
 *
 *		CLASS		value			meaning
 */
public enum DnsClass : ushort
{
	IN = 1,				// the Internet
	CS = 2,				// the CSNET class (Obsolete - used only for examples in some obsolete RFCs)
	CH = 3,				// the CHAOS class
	HS = 4				// Hesiod [Dyer 87]
}
/*
 * 3.2.5. QCLASS values
 *
 * QCLASS fields appear in the question section of a query.  QCLASS values
 * are a superset of CLASS values; every CLASS is a valid QCLASS.  In
 * addition to CLASS values, the following QCLASSes are defined:
 *
 *		QCLASS		value			meaning
 */
public enum DnsQClass : ushort
{
	IN = DnsClass.IN,		// the Internet
	CS = DnsClass.CS,		// the CSNET class (Obsolete - used only for examples in some obsolete RFCs)
	CH = DnsClass.CH,		// the CHAOS class
	HS = DnsClass.HS,		// Hesiod [Dyer 87]

	ANY = 255			// any class
}

/*
RCODE           Response code - this 4 bit field is set as part of
                responses.  The values have the following
                interpretation:
 */
public enum RCode : ushort
{
	NoError		= 0,	// No Error                           [RFC1035]
	FormErr		= 1,	// Format Error                       [RFC1035]
	ServFail	= 2,	// Server Failure                     [RFC1035]
	NXDomain	= 3,	// Non-Existent Domain                [RFC1035]
	NotImp		= 4,	// Not Implemented                    [RFC1035]
	Refused		= 5,	// Query Refused                      [RFC1035]
	YXDomain	= 6,	// Name Exists when it should not     [RFC2136]
	YXRRSet		= 7,	// RR Set Exists when it should not   [RFC2136]
	NXRRSet		= 8,	// RR Set that should exist does not  [RFC2136]
	NotAuth		= 9,	// Server Not Authoritative for zone  [RFC2136]
	NotZone		= 10,	// Name not contained in zone         [RFC2136]

	RESERVED11	= 11,	// Reserved
	RESERVED12	= 12,	// Reserved
	RESERVED13	= 13,	// Reserved
	RESERVED14	= 14,	// Reserved
	RESERVED15	= 15,	// Reserved

	BADVERSSIG	= 16,	// Bad OPT Version                    [RFC2671]
						// TSIG Signature Failure             [RFC2845]
	BADKEY		= 17,	// Key not recognized                 [RFC2845]
	BADTIME		= 18,	// Signature out of time window       [RFC2845]
	BADMODE		= 19,	// Bad TKEY Mode                      [RFC2930]
	BADNAME		= 20,	// Duplicate key name                 [RFC2930]
	BADALG		= 21,	// Algorithm not supported            [RFC2930]
	BADTRUNC	= 22	// Bad Truncation                     [RFC4635]
	/*
		23-3840              available for assignment
			0x0016-0x0F00
		3841-4095            Private Use
			0x0F01-0x0FFF
		4096-65535           available for assignment
			0x1000-0xFFFF
	*/

}

/*
OPCODE          A four bit field that specifies kind of query in this
                message.  This value is set by the originator of a query
                and copied into the response.  The values are:

                0               a standard query (QUERY)

                1               an inverse query (IQUERY)

                2               a server status request (STATUS)

                3-15            reserved for future use
 */
public enum OPCode
{
	Query		= 0,		// a standard query (QUERY)
	IQUERY		= 1,		// OpCode Retired (previously IQUERY - No further [RFC3425]
							// assignment of this code available)
	Status		= 2,		// a server status request (STATUS) RFC1035
	RESERVED3	= 3,		// IANA

	Notify		= 4,		// RFC1996
	Update		= 5,		// RFC2136

	RESERVED6	= 6,
	RESERVED7	= 7,
	RESERVED8	= 8,
	RESERVED9	= 9,
	RESERVED10	= 10,
	RESERVED11	= 11,
	RESERVED12	= 12,
	RESERVED13	= 13,
	RESERVED14	= 14,
	RESERVED15	= 15,
}

public enum TransportType
{
	Udp,
	Tcp
}

/// <summary>
/// IPSECKEY
/// </summary>
public enum GatewayType : byte
{
	NoGateway = 0,
	IPv4 = 1,
	IPv6 = 2,
	WireFormatDomainName = 3
}