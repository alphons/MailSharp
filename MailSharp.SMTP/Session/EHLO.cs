using MailSharp.Common;
using MailSharp.SMTP.Extensions;

namespace MailSharp.SMTP.Session;

public partial class SmtpSession
{
	// Handle EHLO command
	private async Task HandleEhloAsync(string[] parts, string line, CancellationToken ct)
	{
		if (state != SmtpState.Initial && state != SmtpState.TlsStarted)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:BadSequence"], ct);
			return;
		}
		bool tlsActive = security == SecurityEnum.Tls || state == SmtpState.TlsStarted;
		state = SmtpState.HeloReceived;
		await writer.WriteLineAsync(configuration["SmtpResponses:EhloSupport"], ct);
		bool authAllowed = ipGroup != null && (!ipGroup.Access.RequireSslTlsForAuth || tlsActive);
		if (authAllowed)
			await writer.WriteLineAsync(configuration["SmtpResponses:AuthSupport"], ct);

		if (security == SecurityEnum.StartTlsOptional || security == SecurityEnum.StartTls)
			await writer.WriteLineAsync(configuration["SmtpResponses:StartTlsSupport"], ct);

		await writer.WriteLineAsync(string.Format(configuration["SmtpResponses:EhloSizeFormat"]!, configuration.GetValue<long>("SmtpSettings:MaxMessageSize")), ct);
	}
}
