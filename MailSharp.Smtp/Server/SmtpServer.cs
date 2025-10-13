using MailSharp.Smtp.Session;
using System.Net;
using System.Net.Sockets;

namespace MailSharp.Smtp.Server;

public class SmtpServer
{
	private readonly IConfiguration configuration;
	private readonly ILogger<SmtpServer> logger;
	private readonly ILogger<SmtpSession> sessionLogger;
	private readonly List<(TcpListener Listener, bool StartTls, bool UseTls)> listeners = [];
	private CancellationTokenSource? cts;

	private class PortConfig
	{
		public int Port { get; set; }
		public bool StartTls { get; set; }
		public bool UseTls { get; set; }
	}

	public SmtpServer(IConfiguration configuration, ILogger<SmtpServer> logger, ILogger<SmtpSession> sessionLogger)
	{
		this.configuration = configuration;
		this.logger = logger;
		this.sessionLogger = sessionLogger;
		string host = configuration["SmtpSettings:Host"] ?? throw new InvalidOperationException("Host not configured");
		var ports = configuration.GetSection("SmtpSettings:Ports").Get<List<PortConfig>>() ?? throw new InvalidOperationException("Ports not configured");

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
				var eventIdConfig = configuration.GetSection("SmtpEventIds:ServerStarted").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing SmtpEventIds:ServerStarted");
				logger.LogInformation(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:ServerStarted"], l.Listener.LocalEndpoint, l.StartTls, l.UseTls);
			}
			catch (SocketException ex)
			{
				var eventIdConfig = configuration.GetSection("SmtpEventIds:ServerStartFailed").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing SmtpEventIds:ServerStartFailed");
				logger.LogError(new EventId(eventIdConfig.Id, eventIdConfig.Name), ex, configuration["SmtpLogMessages:ServerStartFailed"], ((IPEndPoint)l.Listener.LocalEndpoint).Port);
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
				var eventIdConfig = configuration.GetSection("SmtpEventIds:ClientAccepted").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing SmtpEventIds:ClientAccepted");
				logger.LogInformation(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:ClientAccepted"], client.Client.RemoteEndPoint);
				var session = new SmtpSession(client, configuration, startTls, useTls, sessionLogger);
				_ = session.ProcessAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				var eventIdConfig = configuration.GetSection("SmtpEventIds:ListenerStopped").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing SmtpEventIds:ListenerStopped");
				logger.LogInformation(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:ListenerStopped"], listener.LocalEndpoint);
				break;
			}
			catch (Exception ex)
			{
				var eventIdConfig = configuration.GetSection("SmtpEventIds:ClientAcceptError").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing SmtpEventIds:ClientAcceptError");
				logger.LogError(new EventId(eventIdConfig.Id, eventIdConfig.Name), ex, configuration["SmtpLogMessages:ClientAcceptError"], listener.LocalEndpoint);
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
			var eventIdConfig = configuration.GetSection("SmtpEventIds:ListenerStopped").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing SmtpEventIds:ListenerStopped");
			logger.LogInformation(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:ListenerStopped"], tcplistener.LocalEndpoint);
			tcplistener.Dispose();
		}
		listeners.Clear();
	}
}
