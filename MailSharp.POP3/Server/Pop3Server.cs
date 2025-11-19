using MailSharp.Common;
using MailSharp.Common.Services;
using MailSharp.POP3.Metrics;
using MailSharp.POP3.Session;
using System.Net;
using System.Net.Sockets;

namespace MailSharp.POP3.Server;

public class Pop3Server
{
	private readonly IConfiguration configuration;
	private readonly ILogger<Pop3Server> logger;
	private readonly ILogger<Pop3Session> sessionLogger;
	private readonly AuthenticationService authService;
	private readonly MailboxService mailboxService;
	private readonly List<ServerContext> listeners = [];
	private readonly Pop3Metrics pop3Metrics;
	private CancellationTokenSource? cts;

	public Pop3Server(
		IConfiguration configuration,
		ILogger<Pop3Server> logger,
		ILogger<Pop3Session> sessionLogger,
		AuthenticationService authService,
		MailboxService mailboxService,
		Pop3Metrics pop3Metrics)
	{
		this.configuration = configuration;
		this.logger = logger;
		this.sessionLogger = sessionLogger;
		this.authService = authService;
		this.mailboxService = mailboxService;
		this.pop3Metrics = pop3Metrics;

		var ports = configuration.GetSection("Pop3Settings:Ports").Get<List<PortConfig>>()
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
				var eventIdConfig = configuration.GetSection("Pop3EventIds:ServerStarted").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing Pop3EventIds:ServerStarted");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["Pop3LogMessages:ServerStarted"],
					context.Listener.LocalEndpoint, context.Security);
			}
			catch (SocketException ex)
			{
				var eventIdConfig = configuration.GetSection("Pop3EventIds:ServerStartFailed").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing Pop3EventIds:ServerStartFailed");
				logger.LogError(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					ex,
					configuration["Pop3LogMessages:ServerStartFailed"],
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
				var eventIdConfig = configuration.GetSection("Pop3EventIds:ClientAccepted").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing Pop3EventIds:ClientAccepted");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["Pop3LogMessages:ClientAccepted"],
					client.Client.RemoteEndPoint);

				var session = new Pop3Session(client, configuration, context.Security, authService, mailboxService, sessionLogger);
				_ = session.ProcessAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				var eventIdConfig = configuration.GetSection("Pop3EventIds:ListenerStopped").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing Pop3EventIds:ListenerStopped");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["Pop3LogMessages:ListenerStopped"],
					context.Listener.LocalEndpoint);
				break;
			}
			catch (Exception ex)
			{
				var eventIdConfig = configuration.GetSection("Pop3EventIds:ClientAcceptError").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing Pop3EventIds:ClientAcceptError");
				logger.LogError(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					ex,
					configuration["Pop3LogMessages:ClientAcceptError"],
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
			var eventIdConfig = configuration.GetSection("Pop3EventIds:ListenerStopped").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing Pop3EventIds:ListenerStopped");
			logger.LogInformation(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				configuration["Pop3LogMessages:ListenerStopped"],
				context.Listener.LocalEndpoint);
			context.Listener.Dispose();
		}
		listeners.Clear();
	}
}
