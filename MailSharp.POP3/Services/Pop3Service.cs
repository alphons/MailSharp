using MailSharp.Common;
using MailSharp.Common.Services;
using MailSharp.POP3.Metrics;
using MailSharp.POP3.Server;
using MailSharp.POP3.Session;

namespace MailSharp.POP3.Services;

public class Pop3Service : BackgroundService
{
	private readonly Pop3Server server;
	private readonly IConfiguration configuration;
	private readonly Pop3Metrics metrics;
	private readonly ILogger<Pop3Service> logger;

	public Pop3Service(
		IConfiguration configuration,
		ILogger<Pop3Service> logger,
		ILogger<Pop3Server> serverLogger,
		ILogger<Pop3Session> sessionLogger,
		AuthenticationService authService,
		MailboxService mailboxService,
		Pop3Metrics metrics)
	{
		this.configuration = configuration;
		this.metrics = metrics;
		this.logger = logger;
		this.server = new Pop3Server(configuration, serverLogger, sessionLogger, authService, mailboxService, metrics);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			metrics.IsRunning = true;
			var eventIdConfig = configuration.GetSection("Pop3EventIds:ServiceStarting").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing Pop3EventIds:ServiceStarting");
			logger.LogInformation(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				configuration["Pop3LogMessages:ServiceStarting"]);

			await server.StartAsync();
			await Task.Delay(Timeout.Infinite, stoppingToken);
		}
		catch (Exception ex)
		{
			var eventIdConfig = configuration.GetSection("Pop3EventIds:ServiceError").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing Pop3EventIds:ServiceError");
			logger.LogError(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				ex,
				configuration["Pop3LogMessages:ServiceError"]);
			metrics.IsRunning = false;
			throw;
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		var eventIdConfig = configuration.GetSection("Pop3EventIds:ServiceStopping").Get<EventIdConfig>()
			?? throw new InvalidOperationException("Missing Pop3EventIds:ServiceStopping");
		logger.LogInformation(
			new EventId(eventIdConfig.Id, eventIdConfig.Name),
			configuration["Pop3LogMessages:ServiceStopping"]);
		metrics.IsRunning = false;
		await server.StopAsync();
		await base.StopAsync(cancellationToken);
	}
}
