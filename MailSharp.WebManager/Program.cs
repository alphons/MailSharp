using MailSharp.WebManager.Extensions;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
	ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddMvcCore().WithMultiParameterModelBinding();

builder.Services.AddRazorUnderRoot();

builder.Services.AddControllersWithViews();

builder.Services.AddAuthenticationAndAddAuthorization();

builder.Host.UseWindowsService();

builder.Services.AddLogging(logging => logging.AddConsole());

builder.Services.AddMailSharpServices();

var app = builder.Build();

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseAuthenticationAndAddAuthorization();

app.MapDefaultControllerRoute();

app.MapRazorPages();

await app.RunAsync();
