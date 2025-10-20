using MailSharp.Common;
using MailSharp.SMTP.Services;

namespace MailSharp.SMTP.Extensions;

public static class SmtpServiceExtensions
{
	// Adds SMTP services to the specified IServiceCollection
	public static IServiceCollection AddSmtpServices(this IServiceCollection services)
	{
		services.AddSingleton<SmtpService>();
		services.AddHostedService(provider => provider.GetRequiredService<SmtpService>()); 
		services.AddSingleton<DkimSigner>();
		services.AddSingleton<SpfChecker>();
		services.AddSingleton<DkimVerifier>();
		services.AddSingleton<DmarcChecker>();
		return services;
	}
}