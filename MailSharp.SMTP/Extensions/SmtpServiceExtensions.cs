using MailSharp.SMTP.Metrics;
using MailSharp.SMTP.Services;

namespace MailSharp.SMTP.Extensions;

public static class SmtpServiceExtensions
{
	// Adds SMTP services to the specified IServiceCollection
	public static IServiceCollection AddSmtpServices(this IServiceCollection services)
	{
		services.AddSingleton<SmtpMetrics>();
		services.AddSingleton<SmtpService>();
		services.AddHostedService(provider => provider.GetRequiredService<SmtpService>()); 
		services.AddSingleton<DkimSigner>();
		services.AddSingleton<SpfChecker>();
		services.AddSingleton<DkimVerifier>();
		services.AddSingleton<DmarcChecker>();
		services.AddHostedService<RelayService>();
		return services;
	}
}