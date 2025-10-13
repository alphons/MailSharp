using MailSharp.Smtp.Session;
using System.Net;
using System.Net.Sockets;

namespace MailSharp.Smtp.Server;

public class SmtpServer
{
	private readonly IConfiguration configuration;
	private readonly ILogger<SmtpServer> logger;
	private readonly List<(TcpListener Listener, bool StartTls, bool UseTls)> listeners = [];
	private CancellationTokenSource? cts;

	private class PortConfig
	{
		public int Port { get; set; }
		public bool StartTls { get; set; }
		public bool UseTls { get; set; }
	}

	public SmtpServer(ILogger<SmtpServer> logger, IConfiguration configuration)
	{
		this.configuration = configuration;
		this.logger = logger;
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
				logger.LogInformation("SMTP Server started on {Endpoint} (StartTls: {StartTls}, UseTls: {UseTls})", l.Listener.LocalEndpoint, l.StartTls, l.UseTls); // Log start
			}
			catch (SocketException ex)
			{
				logger.LogError(ex, "Failed to start listener on port {Port}", ((IPEndPoint)l.Listener.LocalEndpoint).Port); // Log fout
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
				logger.LogInformation("Accepted client connection from {ClientEndpoint}", client.Client.RemoteEndPoint); // Log clientverbinding
				var session = new SmtpSession(client, configuration, startTls, useTls, this.logger); // ILogger doorgeven
				_ = session.ProcessAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				logger.LogInformation("Listener on {Endpoint} stopped due to cancellation", listener.LocalEndpoint); // Log stop
				break;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error accepting client on {Endpoint}", listener.LocalEndpoint); // Log fout
			}
		}
	}

	public async Task StopAsync()
	{
		if (cts != null)
			await cts.CancelAsync();
		foreach (var (tcplistener, _, _) in listeners)
		{
			tcplistener.Stop();
			logger.LogInformation("Stopped listener on {Endpoint}", tcplistener.LocalEndpoint); // Log stop
			tcplistener.Dispose();
		}
		listeners.Clear();
	}
}
