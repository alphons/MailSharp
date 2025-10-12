using MailSharp.SmtpServer.Session;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;

namespace MailSharp.SmtpServer.Server;

public class SmtpServer
{
	private readonly IConfiguration configuration;
	private readonly List<(TcpListener Listener, bool RequireTls, bool UseTls)> listeners = [];
	private bool isRunning = false;

	public SmtpServer(IConfiguration configuration)
	{
		this.configuration = configuration;
		string host = configuration["SmtpSettings:Host"] ?? throw new InvalidOperationException("Host not configured in appsettings.json");
		var ports = configuration.GetSection("SmtpSettings:Ports").Get<List<PortConfig>>() ?? throw new InvalidOperationException("Ports not configured in appsettings.json");

		foreach (var portConfig in ports)
		{
			var listener = new TcpListener(IPAddress.Parse(host), portConfig.Port);
			listeners.Add((listener, portConfig.RequireTls, portConfig.UseTls));
		}
	}

	private class PortConfig
	{
		public int Port { get; set; }
		public bool RequireTls { get; set; }
		public bool UseTls { get; set; }
	}

	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		isRunning = true;
		foreach (var (listener, requireTls, useTls) in listeners)
		{
			listener.Start();
			Console.WriteLine($"SMTP Server running on {listener.LocalEndpoint} (RequireTls: {requireTls}, UseTls: {useTls})");
			_ = Task.Run(() => AcceptClientsAsync(listener, requireTls, useTls, cancellationToken), cancellationToken);
		}

		try
		{
			await Task.Delay(Timeout.Infinite, cancellationToken);
		}
		catch (OperationCanceledException)
		{
			// Handled by Stop
		}

		Stop();
	}

	private async Task AcceptClientsAsync(TcpListener listener, bool requireTls, bool useTls, CancellationToken cancellationToken)
	{
		while (isRunning && !cancellationToken.IsCancellationRequested)
		{
			try
			{
				TcpClient client = await listener.AcceptTcpClientAsync(cancellationToken);
				SmtpSession session = new(client, configuration, requireTls, useTls);
				_ = session.ProcessAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}
	}

	public void Stop()
	{
		if (isRunning)
		{
			isRunning = false;
			foreach (var (listener, _, _) in listeners)
			{
				listener.Stop();
			}
			listeners.Clear();
		}
	}
}
