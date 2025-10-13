using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MailSharp.SmtpServer.Services;

public class SmtpServerStatus
{
	public bool IsRunning { get; set; }
}
// SMTP background service
public class SmtpServerService(IConfiguration configuration, ILogger<SmtpServerService> logger, SmtpServerStatus status) : BackgroundService
{
	private readonly Server.SmtpServer server = new(configuration);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			status.IsRunning = true;
			await server.StartAsync();
			await Task.Delay(Timeout.Infinite, stoppingToken);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error running SMTP server");
			status.IsRunning = false;
			throw;
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		status.IsRunning = false;
		await server.StopAsync();
		await base.StopAsync(cancellationToken);
	}
}