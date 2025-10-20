using MailSharp.Common.Services;

namespace MailSharp.Common.Extensions;

public static class CommonServiceExtensions
{
	// Adds Authentication and Mailbox  services to the specified IServiceCollection
	public static IServiceCollection AddCommonServices(this IServiceCollection services)
	{
		services.AddSingleton<AuthenticationService>();
		services.AddSingleton<MailboxService>();
		return services;
	}
}