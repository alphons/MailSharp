using MailSharp.SMTP.Extensions;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MailSharp.SMTP.Session;

public partial class SmtpSession
{
	// Handle STARTTLS command
	private async Task HandleStartTlsAsync(string[] parts, string line, CancellationToken ct)
	{
		if (state != SmtpState.HeloReceived)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:BadSequence"], ct);
			return;
		}

		if (state == SmtpState.TlsStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:StartTlsInvalid"], ct);
			return;
		}

		await writer.WriteLineAsync(configuration["SmtpResponses:StartTls"], ct);
		try
		{
			string certPath = configuration["SmtpSettings:CertificatePath"] ?? throw new InvalidOperationException("CertificatePath not configured");
			string certPassword = configuration["SmtpSettings:CertificatePassword"] ?? string.Empty;
			X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword);

			SslStream sslStream = new(stream, false);
			await sslStream.AuthenticateAsServerAsync(certificate, false, System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13, false);
			this.stream = sslStream;
			this.reader = new StreamReader(sslStream, Encoding.ASCII, false, -1, leaveOpen: true);
			this.writer = new StreamWriter(sslStream, Encoding.ASCII, -1, leaveOpen: true) { AutoFlush = true };

			// Reset session as per RFC 3207
			state = SmtpState.Initial;
			mailFrom = null;
			rcptTo.Clear();
			data.Clear();
		}
		catch(Exception exception)
		{
			logger.LogError(exception, "HandleStartTlsAsync failed");
			await writer.WriteLineAsync(configuration["SmtpResponses:StartTlsFailed"], ct);
			state = SmtpState.HeloReceived;
		}
	}
}
