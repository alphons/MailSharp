namespace MailSharp.Smtp.Services;

public class SmtpServerStatus
{
	public bool IsRunning { get; set; }
}

public class SmtpServerService(IConfiguration configuration, ILogger<SmtpServerService> logger, ILogger<Server.SmtpServer> serverLogger, ILogger<Session.SmtpSession> sessionLogger, SmtpServerStatus status) : BackgroundService
{
	private readonly Server.SmtpServer server = new (configuration, serverLogger, sessionLogger);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			status.IsRunning = true;
			var eventIdConfig = configuration.GetSection("SmtpEventIds:ServiceStarting").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing SmtpEventIds:ServiceStarting");
			logger.LogInformation(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:ServiceStarting"]);
			await server.StartAsync();
			await Task.Delay(Timeout.Infinite, stoppingToken);
		}
		catch (Exception ex)
		{
			var eventIdConfig = configuration.GetSection("SmtpEventIds:ServiceError").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing SmtpEventIds:ServiceError");
			logger.LogError(new EventId(eventIdConfig.Id, eventIdConfig.Name), ex, configuration["SmtpLogMessages:ServiceError"]);
			status.IsRunning = false;
			throw;
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		var eventIdConfig = configuration.GetSection("SmtpEventIds:ServiceStopping").Get<EventIdConfig>()
			?? throw new InvalidOperationException("Missing SmtpEventIds:ServiceStopping");
		logger.LogInformation(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:ServiceStopping"]);
		status.IsRunning = false;
		await server.StopAsync();
		await base.StopAsync(cancellationToken);
	}
}
