using MailSharp.Smtp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();
builder.Services.AddLogging(logging => logging.AddConsole());
builder.Services.AddControllers();
builder.Services.AddHostedService<SmtpServerService>();
builder.Services.AddSingleton<SmtpServerStatus>();

var app = builder.Build();

app.MapControllers();

await app.RunAsync();
