using MailSharp.WebManager.Extensions;
using MailSharp.WebManager.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
	ContentRootPath = AppContext.BaseDirectory
});

builder.Configuration.AddJsonFile(
	Path.Combine(AppContext.BaseDirectory, "mailsharp.json"),
	optional: true,
	reloadOnChange: true);

builder.Services.AddMvcCore();//.WithMultiParameterModelBinding();

builder.Services.AddRazorUnderRoot();

builder.Services.AddControllersWithViews()
	.AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddAuthenticationAndAddAuthorization();

builder.Host.UseWindowsService();

builder.Services.AddLogging(logging => logging.AddConsole());

builder.Services.AddMailSharpServices();

var app = builder.Build();

// Seed the override file from appsettings.json defaults for any section that
// is missing or was previously saved with empty values, then reload so the
// seeded values are visible immediately (reloadOnChange is async).
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<ConfigService>().EnsureOverrideInitialized();
(app.Configuration as IConfigurationRoot)?.Reload();

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseAuthenticationAndAddAuthorization();

app.MapDefaultControllerRoute();

app.MapRazorPages();

await app.RunAsync();
