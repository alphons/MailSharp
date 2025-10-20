using MailSharp.Common;
using MailSharp.IMAP.Services;

namespace MailSharp.IMAP.Extensions;

public static class ImapServiceExtensions
{
	// Adds IMAP services to the specified IServiceCollection
	public static IServiceCollection AddImapServices(this IServiceCollection services)
	{
		services.AddSingleton<ImapService>();
		services.AddHostedService(provider => provider.GetRequiredService<ImapService>());

		return services;
	}
}