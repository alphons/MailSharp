using MailSharp.Common;
using MailSharp.Common.Services;
using MailSharp.IMAP.Metrics;
using MailSharp.IMAP.Session;
using System.Net;
using System.Net.Sockets;

namespace MailSharp.IMAP.Server;

public class ImapServer
{
	private readonly IConfiguration configuration;
	private readonly ILogger<ImapServer> logger;
	private readonly ILogger<ImapSession> sessionLogger;
	private readonly AuthenticationService authService;
	private readonly MailboxService mailboxService;
	private readonly List<ServerContext> listeners = [];
	private readonly ImapMetrics metrics;
	private CancellationTokenSource? cts;

	public ImapServer(
		IConfiguration configuration,
		ILogger<ImapServer> logger,
		ILogger<ImapSession> sessionLogger,
		AuthenticationService authService,
		MailboxService mailboxService,
		ImapMetrics metrics)
	{
		this.configuration = configuration;
		this.logger = logger;
		this.sessionLogger = sessionLogger;
		this.authService = authService;
		this.mailboxService = mailboxService;
		this.metrics = metrics;

		var ports = configuration.GetSection("ImapSettings:Ports").Get<List<PortConfig>>()
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
				var eventIdConfig = configuration.GetSection("ImapEventIds:ServerStarted").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing ImapEventIds:ServerStarted");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["ImapLogMessages:ServerStarted"],
					context.Listener.LocalEndpoint, context.Security);
			}
			catch (SocketException ex)
			{
				var eventIdConfig = configuration.GetSection("ImapEventIds:ServerStartFailed").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing ImapEventIds:ServerStartFailed");
				logger.LogError(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					ex,
					configuration["ImapLogMessages:ServerStartFailed"],
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
				var eventIdConfig = configuration.GetSection("ImapEventIds:ClientAccepted").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing ImapEventIds:ClientAccepted");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["ImapLogMessages:ClientAccepted"],
					client.Client.RemoteEndPoint);

				var session = new ImapSession(client, configuration, context.Security, authService, mailboxService, metrics, sessionLogger);
				_ = session.ProcessAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				var eventIdConfig = configuration.GetSection("ImapEventIds:ListenerStopped").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing ImapEventIds:ListenerStopped");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["ImapLogMessages:ListenerStopped"],
					context.Listener.LocalEndpoint);
				break;
			}
			catch (Exception ex)
			{
				var eventIdConfig = configuration.GetSection("ImapEventIds:ClientAcceptError").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing ImapEventIds:ClientAcceptError");
				logger.LogError(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					ex,
					configuration["ImapLogMessages:ClientAcceptError"],
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
			var eventIdConfig = configuration.GetSection("ImapEventIds:ListenerStopped").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing ImapEventIds:ListenerStopped");
			logger.LogInformation(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				configuration["ImapLogMessages:ListenerStopped"],
				context.Listener.LocalEndpoint);
			context.Listener.Dispose();
		}
		listeners.Clear();
	}
}
