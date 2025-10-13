using MailSharp.SmtpServer.Server;
using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.Build();

SmtpServer server = new(configuration);

Console.CancelKeyPress += async (s, e) =>
{
	e.Cancel = true; // Prevent immediate termination
	await server.StopAsync();
};

await server.StartAsync();