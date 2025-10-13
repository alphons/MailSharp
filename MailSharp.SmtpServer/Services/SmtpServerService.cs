using MailSharp.Smtp.Server;

namespace MailSharp.Smtp.Services;

public class SmtpServerStatus
{
	public bool IsRunning { get; set; }
}

public class SmtpServerService(IConfiguration configuration, ILogger<SmtpServer> logger, SmtpServerStatus status) : BackgroundService
{
	private readonly Server.SmtpServer server = new(logger, configuration); // ILogger doorgeven

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			status.IsRunning = true;
			logger.LogInformation("Starting SMTP server service"); // Log start
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
		logger.LogInformation("Stopping SMTP server service"); // Log stop
		status.IsRunning = false;
		await server.StopAsync();
		await base.StopAsync(cancellationToken);
	}
}