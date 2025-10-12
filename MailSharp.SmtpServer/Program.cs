using MailSharp.SmtpServer.Server;
using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.Build();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
	e.Cancel = true; // Prevent immediate termination
	cts.Cancel();
};

SmtpServer server = new(configuration);
await server.StartAsync(cts.Token);