using MailSharp.Smtp.Server;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MailSharp.Smtp.Session;

public partial class SmtpSession
{
	private readonly TcpClient client;
	private readonly IConfiguration configuration;
	private readonly bool startTls;
	private readonly bool useTls;
	private readonly ILogger<SmtpServer> logger; // Toevoegen
	private SmtpState state;
	private string? mailFrom;
	private readonly List<string> rcptTo = [];
	private readonly StringBuilder data = new();
	private StreamWriter writer;
	private StreamReader reader;
	private Stream stream;
	private readonly Dictionary<string, Func<string[], string, CancellationToken, Task>> commandHandlers = [];

	public SmtpSession(TcpClient client, IConfiguration configuration, bool startTls, bool useTls, ILogger<Server.SmtpServer> logger)
	{
		this.client = client;
		this.configuration = configuration;
		this.startTls = startTls;
		this.useTls = useTls;
		this.logger = logger; // Initialiseren
		this.state = useTls ? SmtpState.TlsStarted : SmtpState.Initial;
		this.stream = client.GetStream();
		if (this.useTls)
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
		logger.LogInformation("New SMTP session started for client {ClientEndpoint}", client.Client.RemoteEndPoint); // Log verbinding
	}

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

	public async Task ProcessAsync(CancellationToken cancellationToken)
	{
		using (client)
		using (stream)
		using (reader)
		using (writer)
		{
			await writer.WriteLineAsync(configuration["SmtpResponses:Ready"]);
			int timeoutSeconds = configuration.GetValue<int>("SmtpSettings:CommandTimeoutSeconds");

			while (client.Connected && !cancellationToken.IsCancellationRequested)
			{
				using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
				using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

				try
				{
					string? line = await reader.ReadLineAsync(linkedCts.Token);
					if (line == null)
					{
						logger.LogWarning("Client {ClientEndpoint} disconnected", client.Client.RemoteEndPoint); // Log disconnect
						return;
					}

					string[] parts = line.Split(' ');
					string command = parts[0].ToUpper();
					logger.LogInformation("Received command {Command} from {ClientEndpoint}", command, client.Client.RemoteEndPoint); // Log commando

					if (commandHandlers.TryGetValue(command, out var handler))
					{
						await handler(parts, line, linkedCts.Token);
						if (command == "QUIT")
						{
							logger.LogInformation("Session ended by QUIT command for {ClientEndpoint}", client.Client.RemoteEndPoint); // Log QUIT
							return;
						}
					}
					else if (state == SmtpState.DataStarted)
					{
						data.AppendLine(line);
					}
					else
					{
						await writer.WriteLineAsync(configuration["SmtpResponses:CommandNotRecognized"]);
						logger.LogWarning("Unrecognized command {Command} from {ClientEndpoint}", command, client.Client.RemoteEndPoint); // Log onbekend commando
					}
				}
				catch (OperationCanceledException)
				{
					await writer.WriteLineAsync(timeoutCts.Token.IsCancellationRequested
						? configuration["SmtpResponses:Timeout"]
						: configuration["SmtpResponses:Shutdown"]);
					logger.LogWarning("Session terminated due to {Reason} for {ClientEndpoint}",
						timeoutCts.Token.IsCancellationRequested ? "timeout" : "shutdown", client.Client.RemoteEndPoint); // Log timeout/shutdown
					return;
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error processing command for {ClientEndpoint}", client.Client.RemoteEndPoint); // Log fout
					await writer.WriteLineAsync(configuration["SmtpResponses:CommandNotRecognized"]);
				}
			}
		}
	}
}
