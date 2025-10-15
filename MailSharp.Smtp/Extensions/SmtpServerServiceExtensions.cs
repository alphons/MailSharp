using MailSharp.Smtp.Services;
using MailSharp.WebManager.Controllers;

namespace MailSharp.Smtp.Extensions;

public static class SmtpServerServiceExtensions
{
	// Adds SMTP server services to the specified IServiceCollection
	public static IServiceCollection AddSmtpServerServices(this IServiceCollection services)
	{
		services.AddControllersWithViews()
			.AddApplicationPart(typeof(SmtpController).Assembly);

		services.AddHostedService<SmtpServerService>();
		services.AddSingleton<SmtpServerStatus>();
		services.AddSingleton<DkimSigner>();
		services.AddSingleton<SpfChecker>();
		services.AddSingleton<DkimVerifier>();

		return services;
	}
}