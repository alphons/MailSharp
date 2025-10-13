using MailSharp.Smtp.Extensions;
using Microsoft.Extensions.Configuration;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MailSharp.Smtp.Session;

public partial class SmtpSession
{
	// Handle STARTTLS command
	private async Task HandleStartTlsAsync(string[] parts, string line, CancellationToken ct)
	{
		if (!configuration.GetValue<bool>("SmtpSettings:EnableStartTls"))
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:CommandNotRecognized"], ct);
			return;
		}

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
			X509Certificate2 certificate = new(certPath, certPassword);

			SslStream sslStream = new(stream, false);
			await sslStream.AuthenticateAsServerAsync(certificate, false, System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13, false);
			this.reader?.Dispose();
			this.writer?.Dispose();
			this.stream = sslStream;
			this.reader = new StreamReader(this.stream, Encoding.ASCII);
			this.writer = new StreamWriter(this.stream, Encoding.ASCII) { AutoFlush = true };

			// Reset session as per RFC 3207
			state = SmtpState.Initial;
			mailFrom = null;
			rcptTo.Clear();
			data.Clear();
		}
		catch
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:StartTlsFailed"], ct);
			state = SmtpState.HeloReceived;
		}
	}
}
