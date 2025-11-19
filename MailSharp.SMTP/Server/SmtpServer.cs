using MailSharp.Common;
using MailSharp.SMTP.Metrics;
using MailSharp.SMTP.Services;
using MailSharp.SMTP.Session;
using System.Net;
using System.Net.Sockets;

namespace MailSharp.SMTP.Server;

public class SmtpServer
{
	private readonly IConfiguration configuration;
	private readonly ILogger<SmtpServer> logger;
	private readonly ILogger<SmtpSession> sessionLogger;
	private readonly DkimSigner dkimSigner;
	private readonly SpfChecker spfChecker;
	private readonly DkimVerifier dkimVerifier;
	private readonly DmarcChecker dmarcChecker;
	private readonly List<ServerContext> listeners = [];
	private readonly SmtpMetrics metrics;
	private CancellationTokenSource? cts;

	public SmtpServer(
		IConfiguration configuration,
		ILogger<SmtpServer> logger,
		ILogger<SmtpSession> sessionLogger,
		DkimSigner dkimSigner,
		SpfChecker spfChecker,
		DkimVerifier dkimVerifier,
		DmarcChecker dmarcChecker,
		SmtpMetrics metrics)
	{
		this.configuration = configuration;
		this.logger = logger;
		this.sessionLogger = sessionLogger;
		this.dkimSigner = dkimSigner;
		this.spfChecker = spfChecker;
		this.dkimVerifier = dkimVerifier;
		this.dmarcChecker = dmarcChecker;
		this.metrics = metrics;
		var ports = configuration.GetSection("SmtpSettings:Ports").Get<List<PortConfig>>()
			?? throw new InvalidOperationException("Ports not configured");
		foreach (var port in ports)
		{
			listeners.Add(new(new TcpListener(IPAddress.Parse(port.Host), port.Port), port.Security));
		}
	}

	public async Task StartAsync()
	{
		cts = new CancellationTokenSource();
		await Task.WhenAll(listeners.Select(context =>
		{
			try
			{
				context.Listener.Start();
				var eventIdConfig = configuration.GetSection("SmtpEventIds:ServerStarted").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing SmtpEventIds:ServerStarted");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["SmtpLogMessages:ServerStarted"],
					context.Listener.LocalEndpoint, context.Security);
			}
			catch (SocketException ex)
			{
				var eventIdConfig = configuration.GetSection("SmtpEventIds:ServerStartFailed").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing SmtpEventIds:ServerStartFailed");
				logger.LogError(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					ex,
					configuration["SmtpLogMessages:ServerStartFailed"],
					((IPEndPoint)context.Listener.LocalEndpoint).Port);
			}
			return Task.Run(() => AcceptClientsAsync(context, cts.Token), cts.Token);
		}));
		await StopAsync();
	}

	private async Task AcceptClientsAsync(ServerContext context, CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var client = await context.Listener.AcceptTcpClientAsync(cancellationToken);
				var eventIdConfig = configuration.GetSection("SmtpEventIds:ClientAccepted").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing SmtpEventIds:ClientAccepted");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["SmtpLogMessages:ClientAccepted"],
					client.Client.RemoteEndPoint);
				var session = new SmtpSession(client, configuration, context.Security, dkimSigner, spfChecker, dkimVerifier, dmarcChecker, metrics, sessionLogger);
				_ = session.ProcessAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				var eventIdConfig = configuration.GetSection("SmtpEventIds:ListenerStopped").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing SmtpEventIds:ListenerStopped");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["SmtpLogMessages:ListenerStopped"],
					context.Listener.LocalEndpoint);
				break;
			}
			catch (Exception ex)
			{
				var eventIdConfig = configuration.GetSection("SmtpEventIds:ClientAcceptError").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing SmtpEventIds:ClientAcceptError");
				logger.LogError(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					ex,
					configuration["SmtpLogMessages:ClientAcceptError"],
					context.Listener.LocalEndpoint);
			}
		}
	}

	public async Task StopAsync()
	{
		if (cts != null)
		{
			await cts.CancelAsync();
		}
		foreach (var context in listeners)
		{
			context.Listener.Stop();
			var eventIdConfig = configuration.GetSection("SmtpEventIds:ListenerStopped").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing SmtpEventIds:ListenerStopped");
			logger.LogInformation(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				configuration["SmtpLogMessages:ListenerStopped"],
				context.Listener.LocalEndpoint);
			context.Listener.Dispose();
		}
		listeners.Clear();
	}
}
