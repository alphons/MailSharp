using MailSharp.Common;
using MailSharp.Common.Services;
using MailSharp.IMAP.Server;
using MailSharp.IMAP.Session;

namespace MailSharp.IMAP.Services;

public class ImapService : BackgroundService, IServerStatus
{
	private readonly ImapServer server;
	private readonly IConfiguration configuration;
	private readonly ILogger<ImapService> logger;

	public bool IsRunning { get; set; }

	public ImapService(
		IConfiguration configuration,
		ILogger<ImapService> logger,
		ILogger<ImapServer> serverLogger,
		ILogger<ImapSession> sessionLogger,
		AuthenticationService authService,
		MailboxService mailboxService)
	{
		this.configuration = configuration;
		this.logger = logger;
		this.server = new ImapServer(configuration, serverLogger, sessionLogger, authService, mailboxService);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			IsRunning = true;
			var eventIdConfig = configuration.GetSection("ImapEventIds:ServiceStarting").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing ImapEventIds:ServiceStarting");
			logger.LogInformation(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				configuration["ImapLogMessages:ServiceStarting"]);

			await server.StartAsync();
			await Task.Delay(Timeout.Infinite, stoppingToken);
		}
		catch (Exception ex)
		{
			var eventIdConfig = configuration.GetSection("ImapEventIds:ServiceError").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing ImapEventIds:ServiceError");
			logger.LogError(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				ex,
				configuration["ImapLogMessages:ServiceError"]);
			IsRunning = false;
			throw;
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		var eventIdConfig = configuration.GetSection("ImapEventIds:ServiceStopping").Get<EventIdConfig>()
			?? throw new InvalidOperationException("Missing ImapEventIds:ServiceStopping");
		logger.LogInformation(
			new EventId(eventIdConfig.Id, eventIdConfig.Name),
			configuration["ImapLogMessages:ServiceStopping"]);
		IsRunning = false;
		await server.StopAsync();
		await base.StopAsync(cancellationToken);
	}
}
