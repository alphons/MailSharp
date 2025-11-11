using MailSharp.Common;
using MailSharp.Common.Services;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Text;

namespace MailSharp.POP3.Session;

public class Pop3Session(
	TcpClient client,
	IConfiguration configuration,
	SecurityEnum security,
	AuthenticationService authService,
	MailboxService mailboxService,
	ILogger<Pop3Session> logger)
{
	private Stream? stream;

	public async Task ProcessAsync(CancellationToken cancellationToken)
	{
		try
		{
			stream = security == SecurityEnum.Tls ? await InitializeTlsStreamAsync() : client.GetStream();
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
						var messages = await mailboxService.GetMessagesAsync(username!, "INBOX", cancellationToken);
						await SendResponseAsync($"+OK {messages.Count} messages", cancellationToken);
						for (int i = 0; i < messages.Count; i++)
						{
							string filePath = Path.Combine(
								configuration["MailboxSettings:StoragePath"] ?? throw new InvalidOperationException("MailboxStoragePath not configured"),
								username!,
								"INBOX",
								$"{messages[i].Uid}.eml");
							long size = File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
							await SendResponseAsync($"{i + 1} {size}", cancellationToken);
						}
						await SendResponseAsync(".", cancellationToken);
						break;
					case "RETR":
						if (!isAuthenticated)
						{
							await SendResponseAsync("-ERR Authentication required", cancellationToken);
							continue;
						}
						if (parts.Length != 2 || !int.TryParse(parts[1], out int msgNum) || msgNum < 1)
						{
							await SendResponseAsync("-ERR RETR requires valid message number", cancellationToken);
							continue;
						}
						var retrMessages = await mailboxService.GetMessagesAsync(username!, "INBOX", cancellationToken);
						if (msgNum > retrMessages.Count)
						{
							await SendResponseAsync("-ERR No such message", cancellationToken);
							continue;
						}
						string? content = await mailboxService.GetMessageContentAsync(username!, "INBOX", retrMessages[msgNum - 1].Uid, cancellationToken);
						if (content == null)
						{
							await SendResponseAsync("-ERR Message not found", cancellationToken);
							continue;
						}
						await SendResponseAsync($"+OK {content.Length} octets", cancellationToken);
						await SendResponseAsync(content, cancellationToken);
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

	// Initialize TLS stream for POP3 session
	private async Task<Stream> InitializeTlsStreamAsync()
	{
		try
		{
			string certPath = configuration["Pop3Settings:CertificatePath"] ?? throw new InvalidOperationException("CertificatePath not configured");
			string certPassword = configuration["Pop3Settings:CertificatePassword"] ?? string.Empty;
			X509Certificate2 certificate = new(certPath, certPassword);
			SslStream sslStream = new(client.GetStream(), false);
			await sslStream.AuthenticateAsServerAsync(certificate, false, System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13, false);

			var eventIdConfig = configuration.GetSection("Pop3EventIds:TlsInitialized").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing Pop3EventIds:TlsInitialized");
			logger.LogInformation(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				configuration["Pop3LogMessages:TlsInitialized"],
				client.Client.RemoteEndPoint);

			return sslStream;
		}
		catch (Exception ex)
		{
			var eventIdConfig = configuration.GetSection("Pop3EventIds:TlsInitializationFailed").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing Pop3EventIds:TlsInitializationFailed");
			logger.LogError(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				ex,
				configuration["Pop3LogMessages:TlsInitializationFailed"],
				client.Client.RemoteEndPoint);
			throw;
		}
	}

	private async Task<string?> ReadCommandAsync(CancellationToken cancellationToken)
	{
		if (stream == null)
		{
			return null;
		}
		byte[] buffer = new byte[1024];
		int bytesRead = await stream.ReadAsync(buffer, cancellationToken);
		return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
	}

	private async Task SendResponseAsync(string response, CancellationToken cancellationToken)
	{
		if (stream == null)
		{
			return;
		}
		byte[] data = Encoding.ASCII.GetBytes(response + "\r\n");
		await stream.WriteAsync(data, cancellationToken);
		await stream.FlushAsync(cancellationToken);
	}
}
