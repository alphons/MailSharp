using Microsoft.Extensions.Configuration;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MailSharp.SmtpServer.Session;

public enum SmtpState
{
	Initial,
	HeloReceived,
	MailFromReceived,
	RcptToReceived,
	DataStarted,
	TlsStarted
}

// Class to manage individual client session


public partial class SmtpSession
{
	private void InitializeHandlers()
	{
		commandHandlers.Add("HELO", HandleHeloAsync);
		commandHandlers.Add("EHLO", HandleEhloAsync);
		commandHandlers.Add("MAIL", HandleMailAsync);
		commandHandlers.Add("RCPT", HandleRcptAsync);
		commandHandlers.Add("DATA", HandleDataAsync);
		commandHandlers.Add(".", HandleDataEndAsync);
		commandHandlers.Add("QUIT", HandleQuitAsync);
		commandHandlers.Add("NOOP", HandleNoopAsync);
		commandHandlers.Add("RSET", HandleRsetAsync);
		commandHandlers.Add("VRFY", HandleVrfyAsync);
		commandHandlers.Add("EXPN", HandleExpnAsync);
		commandHandlers.Add("HELP", HandleHelpAsync);
		commandHandlers.Add("AUTH", HandleAuthAsync);
		commandHandlers.Add("STARTTLS", HandleStartTlsAsync);
	}

	private readonly TcpClient client;
	private readonly IConfiguration configuration;
	private readonly bool startTls;
	private readonly bool useTls;
	private SmtpState state;
	private string? mailFrom;
	private readonly List<string> rcptTo = [];
	private readonly StringBuilder data = new();
	private StreamWriter writer;
	private StreamReader reader;
	private Stream stream;
	private readonly Dictionary<string, Func<string[], string, Task>> commandHandlers = [];

	public SmtpSession(TcpClient client, IConfiguration configuration, bool startTls, bool useTls)

	{
		this.client = client;
		this.configuration = configuration;
		this.startTls = startTls;
		this.useTls = useTls;
		this.state = useTls ? SmtpState.TlsStarted : SmtpState.Initial;
		this.stream = client.GetStream();
		if (useTls)
		{
			string certPath = configuration["SmtpSettings:CertificatePath"] ?? throw new InvalidOperationException("CertificatePath not configured");
			string certPassword = configuration["SmtpSettings:CertificatePassword"] ?? string.Empty;
			X509Certificate2 certificate = new(certPath, certPassword);
			SslStream sslStream = new(stream, false);
			sslStream.AuthenticateAsServer(certificate, false, System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13, false);
			this.stream = sslStream;
		}
		this.reader = new StreamReader(stream, Encoding.ASCII);
		this.writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

		InitializeHandlers();
	}

	// Process client session
	public async Task ProcessAsync(CancellationToken cancellationToken)
	{
		await writer.WriteLineAsync(configuration["SmtpResponses:Ready"]);

		int timeoutSeconds = configuration.GetValue<int>("SmtpSettings:CommandTimeoutSeconds");

		InitializeHandlers();

		while (client.Connected && !cancellationToken.IsCancellationRequested)
		{
			using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

			try
			{
				string? line = await reader.ReadLineAsync(linkedCts.Token);
				if (line == null) break;

				string[] parts = line.Split(' ');
				string command = parts[0].ToUpper();

				if (commandHandlers.TryGetValue(command, out var handler))
				{
					await handler(parts, line);
				}
				else if (state == SmtpState.DataStarted)
				{
					data.AppendLine(line);
				}
				else
				{
					await writer.WriteLineAsync(configuration["SmtpResponses:CommandNotRecognized"]);
				}

				if (command == "QUIT")
				{
					break;
				}
			}
			catch (OperationCanceledException)
			{
				if (timeoutCts.Token.IsCancellationRequested)
				{
					await writer.WriteLineAsync(configuration["SmtpResponses:Timeout"]);
				}
				else
				{
					await writer.WriteLineAsync(configuration["SmtpResponses:Shutdown"]);
				}
				break;
			}
		}
		client.Close();
		writer.Dispose();
		reader.Dispose();
		stream.Dispose();
		client.Dispose();
	}
}

