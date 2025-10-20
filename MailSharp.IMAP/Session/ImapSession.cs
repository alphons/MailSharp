using MailSharp.Common;
using MailSharp.Common.Services;
using System.Net.Sockets;

namespace MailSharp.IMAP.Session;

public class ImapSession
{
	private readonly TcpClient client;
	private readonly IConfiguration configuration;
	private readonly bool startTls;
	private readonly bool useTls;
	private readonly AuthenticationService authService;
	private readonly MailboxService mailboxService;
	private readonly ILogger<ImapSession> logger;
	private Stream? stream;

	public ImapSession(
		TcpClient client,
		IConfiguration configuration,
		bool startTls,
		bool useTls,
		AuthenticationService authService,
		MailboxService mailboxService,
		ILogger<ImapSession> logger)
	{
		this.client = client;
		this.configuration = configuration;
		this.startTls = startTls;
		this.useTls = useTls;
		this.authService = authService;
		this.mailboxService = mailboxService;
		this.logger = logger;
	}

	public async Task ProcessAsync(CancellationToken cancellationToken)
	{
		try
		{
			stream = useTls ? await InitializeTlsStreamAsync() : client.GetStream();
			await SendResponseAsync("* OK IMAP4rev1 server ready", cancellationToken);

			bool isAuthenticated = false;
			string? username = null;

			while (!cancellationToken.IsCancellationRequested)
			{
				string? command = await ReadCommandAsync(cancellationToken);
				if (string.IsNullOrEmpty(command))
				{
					break;
				}

				var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length < 2)
				{
					await SendResponseAsync($"{parts[0]} BAD Invalid command", cancellationToken);
					continue;
				}

				string tag = parts[0];
				string cmd = parts[1].ToUpper();

				switch (cmd)
				{
					case "LOGIN":
						if (parts.Length != 4)
						{
							await SendResponseAsync($"{tag} BAD LOGIN requires username and password", cancellationToken);
							continue;
						}
						username = parts[2];
						isAuthenticated = await authService.AuthenticateAsync(username, parts[3], cancellationToken);
						await SendResponseAsync(
							isAuthenticated ? $"{tag} OK LOGIN completed" : $"{tag} NO LOGIN failed",
							cancellationToken);
						break;

					case "LIST":
						if (!isAuthenticated)
						{
							await SendResponseAsync($"{tag} NO Authentication required", cancellationToken);
							continue;
						}
						var messages = await mailboxService.GetMessagesAsync(username!, cancellationToken);
						foreach (var msg in messages)
						{
							await SendResponseAsync($"* LIST (\\NoSelect) \"/\" \"{msg}\"", cancellationToken);
						}
						await SendResponseAsync($"{tag} OK LIST completed", cancellationToken);
						break;

					case "LOGOUT":
						await SendResponseAsync("* BYE IMAP4rev1 server logging out", cancellationToken);
						await SendResponseAsync($"{tag} OK LOGOUT completed", cancellationToken);
						return;

					case "STARTTLS":
						if (!startTls)
						{
							await SendResponseAsync($"{tag} NO STARTTLS not supported", cancellationToken);
							continue;
						}
						await SendResponseAsync($"{tag} OK Begin TLS negotiation", cancellationToken);
						stream = await InitializeTlsStreamAsync();
						break;

					default:
						await SendResponseAsync($"{tag} BAD Unknown command", cancellationToken);
						break;
				}
			}
		}
		catch (Exception ex)
		{
			var eventIdConfig = configuration.GetSection("ImapEventIds:SessionError").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing ImapEventIds:SessionError");
			logger.LogError(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				ex,
				configuration["ImapLogMessages:SessionError"],
				client.Client.RemoteEndPoint);
		}
		finally
		{
			stream?.Dispose();
			client.Close();
		}
	}

	private async Task<Stream> InitializeTlsStreamAsync()
	{
		// Placeholder: Implement TLS initialization
		return client.GetStream(); // Replace with actual TLS stream setup
	}

	private async Task<string?> ReadCommandAsync(CancellationToken cancellationToken)
	{
		if (stream == null)
		{
			return null;
		}

		byte[] buffer = new byte[1024];
		int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
		return System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
	}

	private async Task SendResponseAsync(string response, CancellationToken cancellationToken)
	{
		if (stream == null)
		{
			return;
		}

		byte[] data = System.Text.Encoding.ASCII.GetBytes(response + "\r\n");
		await stream.WriteAsync(data, cancellationToken);
		await stream.FlushAsync(cancellationToken);
	}
}
