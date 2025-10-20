using MailSharp.Common;
using MailSharp.Common.Services;
using MailSharp.IMAP.Services;
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
	private readonly List<(TcpListener Listener, bool StartTls, bool UseTls)> listeners = [];
	private CancellationTokenSource? cts;

	private class PortConfig
	{
		public int Port { get; set; }
		public bool StartTls { get; set; }
		public bool UseTls { get; set; }
	}

	public ImapServer(
		IConfiguration configuration,
		ILogger<ImapServer> logger,
		ILogger<ImapSession> sessionLogger,
		AuthenticationService authService,
		MailboxService mailboxService)
	{
		this.configuration = configuration;
		this.logger = logger;
		this.sessionLogger = sessionLogger;
		this.authService = authService;
		this.mailboxService = mailboxService;

		string host = configuration["ImapSettings:Host"] ?? throw new InvalidOperationException("Host not configured");
		var ports = configuration.GetSection("ImapSettings:Ports").Get<List<PortConfig>>()
			?? throw new InvalidOperationException("Ports not configured");

		foreach (var port in ports)
		{
			listeners.Add((new TcpListener(IPAddress.Parse(host), port.Port), port.StartTls, port.UseTls));
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
				var eventIdConfig = configuration.GetSection("ImapEventIds:ServerStarted").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing ImapEventIds:ServerStarted");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["ImapLogMessages:ServerStarted"],
					l.Listener.LocalEndpoint, l.StartTls, l.UseTls);
			}
			catch (SocketException ex)
			{
				var eventIdConfig = configuration.GetSection("ImapEventIds:ServerStartFailed").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing ImapEventIds:ServerStartFailed");
				logger.LogError(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					ex,
					configuration["ImapLogMessages:ServerStartFailed"],
					((IPEndPoint)l.Listener.LocalEndpoint).Port);
			}
			return Task.Run(() => AcceptClientsAsync(l.Listener, l.StartTls, l.UseTls, cts.Token), cts.Token);
		}));
		await StopAsync();
	}

	private async Task AcceptClientsAsync(TcpListener listener, bool startTls, bool useTls, CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var client = await listener.AcceptTcpClientAsync(cancellationToken);
				var eventIdConfig = configuration.GetSection("ImapEventIds:ClientAccepted").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing ImapEventIds:ClientAccepted");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["ImapLogMessages:ClientAccepted"],
					client.Client.RemoteEndPoint);

				var session = new ImapSession(client, configuration, startTls, useTls, authService, mailboxService, sessionLogger);
				_ = session.ProcessAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				var eventIdConfig = configuration.GetSection("ImapEventIds:ListenerStopped").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing ImapEventIds:ListenerStopped");
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["ImapLogMessages:ListenerStopped"],
					listener.LocalEndpoint);
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
		foreach (var (tcplistener, _, _) in listeners)
		{
			tcplistener.Stop();
			var eventIdConfig = configuration.GetSection("ImapEventIds:ListenerStopped").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing ImapEventIds:ListenerStopped");
			logger.LogInformation(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				configuration["ImapLogMessages:ListenerStopped"],
				tcplistener.LocalEndpoint);
			tcplistener.Dispose();
		}
		listeners.Clear();
	}
}
