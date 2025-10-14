using MailSharp.Smtp.Extensions;
using MailSharp.Smtp.Services;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MailSharp.Smtp.Session;

public enum SmtpState
{
	Initial,
	HeloReceived,
	MailFromReceived,
	RcptToReceived,
	DataStarted,
	TlsStarted
}

public partial class SmtpSession
{
	private readonly TcpClient client;
	private readonly IConfiguration configuration;
	private readonly bool startTls;
	private readonly bool useTls;
	private readonly ILogger<SmtpSession> logger;
	private readonly long sessionId;
	private SmtpState state;
	private string? mailFrom;
	private readonly List<string> rcptTo = [];
	private readonly StringBuilder data = new();
	private StreamWriter writer;
	private StreamReader reader;
	private Stream stream;
	private readonly Dictionary<string, Func<string[], string, CancellationToken, Task>> commandHandlers = [];
	private static long nextSessionId = 0;
	private readonly DkimSigner dkimSigner;
	private readonly SpfChecker spfChecker;
	private readonly DkimVerifier dkimVerifier;

	public SmtpSession(TcpClient client, IConfiguration configuration, bool startTls, bool useTls, DkimSigner dkimSigner, SpfChecker spfChecker, DkimVerifier dkimVerifier, ILogger<SmtpSession> logger)
	{
		this.client = client;
		this.configuration = configuration;
		this.startTls = startTls;
		this.useTls = useTls;
		this.dkimSigner = dkimSigner;
		this.spfChecker = spfChecker;
		this.dkimVerifier = dkimVerifier;
		this.logger = logger;
		this.sessionId = Interlocked.Increment(ref nextSessionId);
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
		using (logger.BeginScope(new Dictionary<string, object> { ["SessionId"] = sessionId }))
		{
			var eventIdConfig = configuration.GetSection("SmtpEventIds:SessionStarted").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing SmtpEventIds:SessionStarted");
			logger.LogInformation(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:SessionStarted"], sessionId, client.Client.RemoteEndPoint);
		}
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
			await writer.WriteLineAsync(configuration["SmtpResponses:Ready"], cancellationToken);
			int timeoutSeconds = configuration.GetValue<int>("SmtpSettings:CommandTimeoutSeconds");

			using (logger.BeginScope(new Dictionary<string, object> { ["SessionId"] = sessionId }))
			{
				while (client.Connected && !cancellationToken.IsCancellationRequested)
				{
					using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
					using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

					EventIdConfig eventIdConfig;
					try
					{
						string? line = await reader.ReadLineAsync(linkedCts.Token);
						if (line == null)
						{
							eventIdConfig = configuration.GetSection("SmtpEventIds:ClientDisconnected").Get<EventIdConfig>()
								?? throw new InvalidOperationException("Missing SmtpEventIds:ClientDisconnected");
							logger.LogWarning(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:ClientDisconnected"], sessionId);
							return;
						}

						string[] parts = line.Split(' ');
						string command = parts[0].ToUpper();
						eventIdConfig = configuration.GetSection("SmtpEventIds:CommandReceived").Get<EventIdConfig>()
							?? throw new InvalidOperationException("Missing SmtpEventIds:CommandReceived");
						logger.LogInformation(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:CommandReceived"], command, sessionId);

						if (commandHandlers.TryGetValue(command, out var handler))
						{
							await handler(parts, line, linkedCts.Token);
							if (command == "QUIT")
							{
								eventIdConfig = configuration.GetSection("SmtpEventIds:SessionEndedByQuit").Get<EventIdConfig>()
									?? throw new InvalidOperationException("Missing SmtpEventIds:SessionEndedByQuit");
								logger.LogInformation(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:SessionEndedByQuit"], sessionId);
								return;
							}
						}
						else if (state == SmtpState.DataStarted)
						{
							data.AppendLine(line);
						}
						else
						{
							await writer.WriteLineAsync(configuration["SmtpResponses:CommandNotRecognized"], linkedCts.Token);
							eventIdConfig = configuration.GetSection("SmtpEventIds:UnrecognizedCommand").Get<EventIdConfig>()
								?? throw new InvalidOperationException("Missing SmtpEventIds:UnrecognizedCommand");
							logger.LogWarning(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:UnrecognizedCommand"], command, sessionId);
						}
					}
					catch (OperationCanceledException)
					{
						await writer.WriteLineAsync(timeoutCts.Token.IsCancellationRequested
							? configuration["SmtpResponses:Timeout"]
							: configuration["SmtpResponses:Shutdown"], linkedCts.Token);
						eventIdConfig = configuration.GetSection("SmtpEventIds:SessionTerminated").Get<EventIdConfig>()
							?? throw new InvalidOperationException("Missing SmtpEventIds:SessionTerminated");
						logger.LogWarning(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:SessionTerminated"],
							timeoutCts.Token.IsCancellationRequested ? "timeout" : "shutdown", sessionId);
						return;
					}
					catch (Exception ex)
					{
						eventIdConfig = configuration.GetSection("SmtpEventIds:CommandProcessingError").Get<EventIdConfig>()
							?? throw new InvalidOperationException("Missing SmtpEventIds:CommandProcessingError");
						logger.LogError(new EventId(eventIdConfig.Id, eventIdConfig.Name), ex, configuration["SmtpLogMessages:CommandProcessingError"], sessionId);
						await writer.WriteLineAsync(configuration["SmtpResponses:CommandNotRecognized"], linkedCts.Token);
					}
				}
			}
		}
	}
}
