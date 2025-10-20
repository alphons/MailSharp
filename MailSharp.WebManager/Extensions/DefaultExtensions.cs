using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Razor;
using MailSharp.Common.Extensions;

using MailSharp.IMAP.Extensions;
using MailSharp.POP3.Extensions;
using MailSharp.SMTP.Extensions;

namespace MailSharp.WebManager.Extensions;

public static class DefaultExtensions
{
	// Adds SMTP server services to the specified IServiceCollection
	public static IServiceCollection AddRazorUnderRoot(this IServiceCollection services)
	{
		services.AddRazorPages(o => o.RootDirectory = "/wwwroot");

		services.Configure<RazorViewEngineOptions>(options =>
		{
			options.ViewLocationFormats.Clear();
			options.ViewLocationFormats.Add("/wwwroot/{0}.cshtml");
			options.ViewLocationFormats.Add("/wwwroot/MasterPages/{0}.cshtml");

			options.PageViewLocationFormats.Clear();
			options.PageViewLocationFormats.Add("/wwwroot/{0}.cshtml");
			options.PageViewLocationFormats.Add("/wwwroot/MasterPages/{0}.cshtml");
		});

		return services;
	}

	public static IServiceCollection AddAuthenticationAndAddAuthorization(this IServiceCollection services)
	{
		services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/Account/Login";
		options.AccessDeniedPath = "/Account/AccessDenied";
		options.Cookie.HttpOnly = true;
		options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
		options.Cookie.SameSite = SameSiteMode.Strict;
	});

		services.AddAuthorization();

		return services;
	}


	public static IServiceCollection AddMailSharpServices(this IServiceCollection services)
	{
		services.AddCommonServices();

		services.AddSmtpServices();

		services.AddPop3Services();

		services.AddImapServices();

		return services;
	}

	public static IApplicationBuilder UseAuthenticationAndAddAuthorization(this IApplicationBuilder app)
	{
		app.UseAuthentication();
		app.UseAuthorization();
		return app;
	}
}