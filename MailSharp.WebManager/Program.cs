using MailSharp.IMAP.Extensions;
using MailSharp.POP3.Extensions;
using MailSharp.SMTP.Extensions;
using MailSharp.WebManager.Extensions;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
	ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddMvcCore().WithMultiParameterModelBinding();

builder.Services.AddRazorUnderRoot();

builder.Services.AddAuthenticationAndAddAuthorization();

builder.Host.UseWindowsService();

builder.Services.AddLogging(logging => logging.AddConsole());

builder.Services.AddSmtpServices();

builder.Services.AddPop3Services();

builder.Services.AddImapServices();

var app = builder.Build();

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseAuthenticationAndAddAuthorization();

app.MapDefaultControllerRoute();

app.MapRazorPages();

await app.RunAsync();
