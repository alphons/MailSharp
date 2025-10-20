using MailSharp.Common;
using MailSharp.Common.Services;
using System.Net.Sockets;

namespace MailSharp.POP3.Session;

public class Pop3Session
{
	private readonly TcpClient client;
	private readonly IConfiguration configuration;
	private readonly bool useTls;
	private readonly AuthenticationService authService;
	private readonly MailboxService mailboxService;
	private readonly ILogger<Pop3Session> logger;
	private Stream? stream;

	public Pop3Session(
		TcpClient client,
		IConfiguration configuration,
		bool useTls,
		AuthenticationService authService,
		MailboxService mailboxService,
		ILogger<Pop3Session> logger)
	{
		this.client = client;
		this.configuration = configuration;
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
			await SendResponseAsync("+OK POP3 server ready", cancellationToken);

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
				if (parts.Length == 0)
				{
					await SendResponseAsync("-ERR Invalid command", cancellationToken);
					continue;
				}

				switch (parts[0].ToUpper())
				{
					case "USER":
						if (parts.Length != 2)
						{
							await SendResponseAsync("-ERR USER requires username", cancellationToken);
							continue;
						}
						username = parts[1];
						await SendResponseAsync("+OK Username accepted", cancellationToken);
						break;

					case "PASS":
						if (parts.Length != 2 || username == null)
						{
							await SendResponseAsync("-ERR PASS requires prior USER command", cancellationToken);
							continue;
						}
						isAuthenticated = await authService.AuthenticateAsync(username, parts[1], cancellationToken);
						await SendResponseAsync(
							isAuthenticated ? "+OK Authentication successful" : "-ERR Authentication failed",
							cancellationToken);
						break;

					case "LIST":
						if (!isAuthenticated)
						{
							await SendResponseAsync("-ERR Authentication required", cancellationToken);
							continue;
						}
						var messages = await mailboxService.GetMessagesAsync(username!, cancellationToken);
						await SendResponseAsync("+OK " + messages.Count + " messages", cancellationToken);
						for (int i = 0; i < messages.Count; i++)
						{
							await SendResponseAsync($"{i + 1} {messages[i].Length}", cancellationToken);
						}
						await SendResponseAsync(".", cancellationToken);
						break;

					case "QUIT":
						await SendResponseAsync("+OK POP3 server signing off", cancellationToken);
						return;

					default:
						await SendResponseAsync("-ERR Unknown command", cancellationToken);
						break;
				}
			}
		}
		catch (Exception ex)
		{
			var eventIdConfig = configuration.GetSection("Pop3EventIds:SessionError").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing Pop3EventIds:SessionError");
			logger.LogError(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				ex,
				configuration["Pop3LogMessages:SessionError"],
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
