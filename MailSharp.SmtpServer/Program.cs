using MailSharp.SmtpServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();

builder.Services.AddControllers();
builder.Services.AddHostedService<SmtpServerService>();
builder.Services.AddSingleton<SmtpServerStatus>();

var app = builder.Build();

app.MapControllers();

await app.RunAsync();
