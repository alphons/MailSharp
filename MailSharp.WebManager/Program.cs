using MailSharp.Smtp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Razor;


var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
	ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddMvcCore().WithMultiParameterModelBinding();

builder.Services.AddRazorPages(o => o.RootDirectory = "/wwwroot");

builder.Services.Configure<RazorViewEngineOptions>(options =>
{
	options.ViewLocationFormats.Clear();
	options.ViewLocationFormats.Add("/wwwroot/{0}.cshtml");
	options.ViewLocationFormats.Add("/wwwroot/MasterPages/{0}.cshtml");

	options.PageViewLocationFormats.Clear();
	options.PageViewLocationFormats.Add("/wwwroot/{0}.cshtml");
	options.PageViewLocationFormats.Add("/wwwroot/MasterPages/{0}.cshtml");
});



builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
	.AddCookie(options =>
	{
		options.LoginPath = "/Account/Login";
		options.AccessDeniedPath = "/Account/AccessDenied";
		options.Cookie.HttpOnly = true;
		options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
		options.Cookie.SameSite = SameSiteMode.Strict;
	});

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();

builder.Host.UseWindowsService();
builder.Services.AddLogging(logging => logging.AddConsole());
builder.Services.AddHostedService<SmtpServerService>();
builder.Services.AddSingleton<SmtpServerStatus>();
builder.Services.AddSingleton<DkimSigner>();
builder.Services.AddSingleton<SpfChecker>();
builder.Services.AddSingleton<DkimVerifier>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.MapRazorPages();

await app.RunAsync();
