using MailSharp.Common;
using MailSharp.Common.Services;
using MailSharp.POP3.Services;
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
	private readonly List<(TcpListener Listener, bool UseTls)> listeners = [];
	private CancellationTokenSource? cts;

	private class PortConfig
	{
		public int Port { get; set; }
		public bool UseTls { get; set; }
	}

	public Pop3Server(
		IConfiguration configuration,
		ILogger<Pop3Server> logger,
		ILogger<Pop3Session> sessionLogger,
		AuthenticationService authService,
		MailboxService mailboxService)
	{
		this.configuration = configuration;
		this.logger = logger;
		this.sessionLogger = sessionLogger;
		this.authService = authService;
		this.mailboxService = mailboxService;

		string host = configuration["Pop3Settings:Host"] ?? throw new InvalidOperationException("Host not configured");
		var ports = configuration.GetSection("Pop3Settings:Ports").Get<List<PortConfig>>()
			?? throw new InvalidOperationException("Ports not configured");

		foreach (var port in ports)
		{
			listeners.Add((new TcpListener(IPAddress.Parse(host), port.Port), port.UseTls));
		}
	}

	public async Task StartAsync()
	{
		cts = new CancellationTokenSource();
		await Task.WhenAll(listeners.Select(l =>
		{
			try
			{
				l.Listener.Start();
				var eventIdConfig = configuration.GetSection("Pop3EventIds:ServerStarted").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing Pop3EventIds:ServerStarted");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["Pop3LogMessages:ServerStarted"],
					l.Listener.LocalEndpoint, l.UseTls);
			}
			catch (SocketException ex)
			{
				var eventIdConfig = configuration.GetSection("Pop3EventIds:ServerStartFailed").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing Pop3EventIds:ServerStartFailed");
				logger.LogError(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					ex,
					configuration["Pop3LogMessages:ServerStartFailed"],
					((IPEndPoint)l.Listener.LocalEndpoint).Port);
			}
			return Task.Run(() => AcceptClientsAsync(l.Listener, l.UseTls, cts.Token), cts.Token);
		}));
		await StopAsync();
	}

	private async Task AcceptClientsAsync(TcpListener listener, bool useTls, CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var client = await listener.AcceptTcpClientAsync(cancellationToken);
				var eventIdConfig = configuration.GetSection("Pop3EventIds:ClientAccepted").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing Pop3EventIds:ClientAccepted");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["Pop3LogMessages:ClientAccepted"],
					client.Client.RemoteEndPoint);

				var session = new Pop3Session(client, configuration, useTls, authService, mailboxService, sessionLogger);
				_ = session.ProcessAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				var eventIdConfig = configuration.GetSection("Pop3EventIds:ListenerStopped").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing Pop3EventIds:ListenerStopped");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["Pop3LogMessages:ListenerStopped"],
					listener.LocalEndpoint);
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
					listener.LocalEndpoint);
			}
		}
	}

	public async Task StopAsync()
	{
		if (cts != null)
		{
			await cts.CancelAsync();
		}
		foreach (var (tcplistener, _) in listeners)
		{
			tcplistener.Stop();
			var eventIdConfig = configuration.GetSection("Pop3EventIds:ListenerStopped").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing Pop3EventIds:ListenerStopped");
			logger.LogInformation(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				configuration["Pop3LogMessages:ListenerStopped"],
				tcplistener.LocalEndpoint);
			tcplistener.Dispose();
		}
		listeners.Clear();
	}
}
