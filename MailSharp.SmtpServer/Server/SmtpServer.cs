using MailSharp.SmtpServer.Session;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;

namespace MailSharp.SmtpServer.Server;

public class SmtpServer
{
	private readonly IConfiguration configuration;
	private readonly List<(TcpListener Listener, bool StartTls, bool UseTls)> listeners = [];
	private CancellationTokenSource? cts;

	public SmtpServer(IConfiguration configuration)
	{
		this.configuration = configuration;
		string host = configuration["SmtpSettings:Host"] ?? throw new InvalidOperationException("Host not configured");
		var ports = configuration.GetSection("SmtpSettings:Ports").Get<List<PortConfig>>() ?? throw new InvalidOperationException("Ports not configured");

		foreach (var port in ports)
		{
			listeners.Add((new TcpListener(IPAddress.Parse(host), port.Port), port.StartTls, port.UseTls));
		}
	}

	private class PortConfig
	{
		public int Port { get; set; }
		public bool StartTls { get; set; }
		public bool UseTls { get; set; }
	}

	public async Task StartAsync()
	{
		this.cts = new CancellationTokenSource();

		await Task.WhenAll(listeners.Select(l =>
		{
			try
			{
				l.Listener.Start();
				Console.WriteLine($"SMTP Server running on {l.Listener.LocalEndpoint} (StartTls: {l.StartTls}, UseTls: {l.UseTls})");
			}
			catch (SocketException ex)
			{
				Console.WriteLine($"Failed to start listener on port {((IPEndPoint)l.Listener.LocalEndpoint).Port}: {ex.Message}");
			}
			return Task.Run(() => AcceptClientsAsync(l.Listener, l.StartTls, l.UseTls, this.cts.Token), this.cts.Token);
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
				var session = new SmtpSession(client, configuration, startTls, useTls);
				_ = session.ProcessAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}
	}

	public async Task StopAsync()
	{
		if(cts != null)
			await cts.CancelAsync();
		foreach (var (tcplistener, _, _) in listeners)
		{
			tcplistener.Stop();
			tcplistener.Dispose();
		}
		listeners.Clear();
	}
}
