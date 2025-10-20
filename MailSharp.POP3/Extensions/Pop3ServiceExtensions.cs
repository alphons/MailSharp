using MailSharp.POP3.Services;

namespace MailSharp.POP3.Extensions;

public static class Pop3ServiceExtensions
{
	// Adds POP3 services to the specified IServiceCollection
	public static IServiceCollection AddPop3Services(this IServiceCollection services)
	{
		services.AddHostedService<Pop3Service>();

		return services;
	}
}