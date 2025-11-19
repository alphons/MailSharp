using MailSharp.Common;
using MailSharp.SMTP.Metrics;
using MailSharp.SMTP.Server;
using MailSharp.SMTP.Session;

namespace MailSharp.SMTP.Services;

public class SmtpService(IConfiguration configuration, 
	ILogger<SmtpService> logger, 
	ILogger<SmtpServer> serverLogger, 
	ILogger<SmtpSession> sessionLogger,
	DkimSigner dkimSigner,
	SpfChecker spfChecker, 
	DkimVerifier dkimVerifier,
	DmarcChecker dmarcChecker,
	SmtpMetrics metrics) : BackgroundService
{
	private readonly SmtpServer server = new (
		configuration, 
		serverLogger, 
		sessionLogger, 
		dkimSigner, 
		spfChecker, 
		dkimVerifier, 
		dmarcChecker,
		metrics);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			metrics.IsRunning = true;
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
			metrics.IsRunning = false;
			throw;
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		var eventIdConfig = configuration.GetSection("SmtpEventIds:ServiceStopping").Get<EventIdConfig>()
			?? throw new InvalidOperationException("Missing SmtpEventIds:ServiceStopping");
		logger.LogInformation(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:ServiceStopping"]);
		metrics.IsRunning = false;
		await server.StopAsync();
		await base.StopAsync(cancellationToken);
	}
}
