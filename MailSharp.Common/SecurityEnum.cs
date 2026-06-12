namespace MailSharp.Common;

public enum SecurityEnum
{
	None,
	StartTlsOptional,
	StartTls,          // StartTLS required before MAIL/AUTH/DATA
	Tls                // Implicit TLS from connection start
}
