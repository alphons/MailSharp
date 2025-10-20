using MailSharp.IMAP.Services;

namespace MailSharp.IMAP.Extensions;

public static class ImapServiceExtensions
{
	// Adds IMAP services to the specified IServiceCollection
	public static IServiceCollection AddImapServices(this IServiceCollection services)
	{
		services.AddHostedService<ImapService>();

		return services;
	}
}