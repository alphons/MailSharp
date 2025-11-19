using MailSharp.Common;
using MailSharp.Common.Services;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Text;

namespace MailSharp.IMAP.Session;

public class ImapSession(
	TcpClient client,
	IConfiguration configuration,
	SecurityEnum security,
	AuthenticationService authService,
	MailboxService mailboxService,
	ILogger<ImapSession> logger)
{
	private Stream? stream;
	private string? selectedFolder;

	public async Task ProcessAsync(CancellationToken cancellationToken)
	{
		try
		{
			stream = security == SecurityEnum.Tls ? await InitializeTlsStreamAsync() : client.GetStream();
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
						var folders = await mailboxService.ListFoldersAsync(username!, cancellationToken);
						foreach (var folder1 in folders)
						{
							await SendResponseAsync($"* LIST (\\NoSelect) \"/\" \"{folder1}\"", cancellationToken);
						}
						await SendResponseAsync($"{tag} OK LIST completed", cancellationToken);
						break;
					case "SELECT":
						if (!isAuthenticated)
						{
							await SendResponseAsync($"{tag} NO Authentication required", cancellationToken);
							continue;
						}
						if (parts.Length != 3)
						{
							await SendResponseAsync($"{tag} BAD SELECT requires folder name", cancellationToken);
							continue;
						}
						string folder = parts[2].Trim('"');
						var folderExists = (await mailboxService.ListFoldersAsync(username!, cancellationToken)).Contains(folder);
						if (!folderExists)
						{
							await SendResponseAsync($"{tag} NO Folder does not exist", cancellationToken);
							continue;
						}
						selectedFolder = folder;
						var messages = await mailboxService.GetMessagesAsync(username!, folder, cancellationToken);
						await SendResponseAsync($"* {messages.Count} EXISTS", cancellationToken);
						await SendResponseAsync($"{tag} OK SELECT completed", cancellationToken);
						break;
					case "CREATE":
						if (!isAuthenticated)
						{
							await SendResponseAsync($"{tag} NO Authentication required", cancellationToken);
							continue;
						}
						if (parts.Length != 3)
						{
							await SendResponseAsync($"{tag} BAD CREATE requires folder name", cancellationToken);
							continue;
						}
						string newFolder = parts[2].Trim('"');
						bool created = await mailboxService.CreateFolderAsync(username!, newFolder, cancellationToken);
						await SendResponseAsync(
							created ? $"{tag} OK CREATE completed" : $"{tag} NO Folder creation failed",
							cancellationToken);
						break;
					case "DELETE":
						if (!isAuthenticated)
						{
							await SendResponseAsync($"{tag} NO Authentication required", cancellationToken);
							continue;
						}
						if (parts.Length != 3)
						{
							await SendResponseAsync($"{tag} BAD DELETE requires folder name", cancellationToken);
							continue;
						}
						string deleteFolder = parts[2].Trim('"');
						if (deleteFolder == "INBOX")
						{
							await SendResponseAsync($"{tag} NO Cannot delete INBOX", cancellationToken);
							continue;
						}
						bool deleted = await mailboxService.DeleteFolderAsync(username!, deleteFolder, cancellationToken);
						await SendResponseAsync(
							deleted ? $"{tag} OK DELETE completed" : $"{tag} NO Folder deletion failed",
							cancellationToken);
						break;
					case "FETCH":
						if (!isAuthenticated || selectedFolder == null)
						{
							await SendResponseAsync($"{tag} NO Authentication or folder selection required", cancellationToken);
							continue;
						}
						if (parts.Length < 4)
						{
							await SendResponseAsync($"{tag} BAD FETCH requires message sequence and items", cancellationToken);
							continue;
						}
						string sequence = parts[2];
						string items = parts[3].ToUpper();
						var fetchMessages = await mailboxService.GetMessagesAsync(username!, selectedFolder, cancellationToken);
						int seqNum;
						if (!int.TryParse(sequence, out seqNum) || seqNum < 1 || seqNum > fetchMessages.Count)
						{
							await SendResponseAsync($"{tag} NO Invalid message sequence", cancellationToken);
							continue;
						}
						var message = fetchMessages[seqNum - 1];
						if (items.Contains("FLAGS"))
						{
							string flags = $"FLAGS (\\{(message.Flags.Seen ? "Seen" : "")} \\{(message.Flags.Deleted ? "Deleted" : "")} \\{(message.Flags.Flagged ? "Flagged" : "")} \\{(message.Flags.Answered ? "Answered" : "")})";
							await SendResponseAsync($"* {seqNum} FETCH ({flags})", cancellationToken);
						}
						if (items.Contains("BODY[]"))
						{
							string? content = await mailboxService.GetMessageContentAsync(username!, selectedFolder, message.Uid, cancellationToken);
							if (content != null)
							{
								await SendResponseAsync($"* {seqNum} FETCH (BODY[] {{{content.Length}}}", cancellationToken);
								await SendResponseAsync(content, cancellationToken);
								await SendResponseAsync(")", cancellationToken);
							}
						}
						await SendResponseAsync($"{tag} OK FETCH completed", cancellationToken);
						break;
					case "STORE":
						if (!isAuthenticated || selectedFolder == null)
						{
							await SendResponseAsync($"{tag} NO Authentication or folder selection required", cancellationToken);
							continue;
						}
						if (parts.Length < 5)
						{
							await SendResponseAsync($"{tag} BAD STORE requires message sequence, operation, and flags", cancellationToken);
							continue;
						}
						string storeSequence = parts[2];
						string operation = parts[3].ToUpper();
						string flagList = parts[4].Trim('(', ')');
						if (!int.TryParse(storeSequence, out int storeSeqNum) || storeSeqNum < 1 || storeSeqNum > (await mailboxService.GetMessagesAsync(username!, selectedFolder, cancellationToken)).Count)
						{
							await SendResponseAsync($"{tag} NO Invalid message sequence", cancellationToken);
							continue;
						}
						var storeMessage = (await mailboxService.GetMessagesAsync(username!, selectedFolder, cancellationToken))[storeSeqNum - 1];
						var newFlags = new MessageFlags();
						foreach (string flag in flagList.Split(' ', StringSplitOptions.RemoveEmptyEntries))
						{
							if (flag == "\\Seen") newFlags.Seen = true;
							if (flag == "\\Deleted") newFlags.Deleted = true;
							if (flag == "\\Flagged") newFlags.Flagged = true;
							if (flag == "\\Answered") newFlags.Answered = true;
						}
						bool updated = await mailboxService.SetMessageFlagsAsync(username!, selectedFolder, storeMessage.Uid, newFlags, cancellationToken);
						await SendResponseAsync(
							updated ? $"{tag} OK STORE completed" : $"{tag} NO STORE failed",
							cancellationToken);
						break;
					case "LOGOUT":
						await SendResponseAsync("* BYE IMAP4rev1 server logging out", cancellationToken);
						await SendResponseAsync($"{tag} OK LOGOUT completed", cancellationToken);
						return;
					case "STARTTLS":
						if (security !=  SecurityEnum.StartTls)
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

	// Initialize TLS stream for IMAP session
	private async Task<Stream> InitializeTlsStreamAsync()
	{
		try
		{
			string certPath = configuration["ImapSettings:CertificatePath"] ?? throw new InvalidOperationException("CertificatePath not configured");
			string certPassword = configuration["ImapSettings:CertificatePassword"] ?? string.Empty;
			X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword);
			SslStream sslStream = new(client.GetStream(), false);
			await sslStream.AuthenticateAsServerAsync(certificate, false, System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13, false);

			var eventIdConfig = configuration.GetSection("ImapEventIds:TlsInitialized").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing ImapEventIds:TlsInitialized");
			logger.LogInformation(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				configuration["ImapLogMessages:TlsInitialized"],
				client.Client.RemoteEndPoint);

			return sslStream;
		}
		catch (Exception ex)
		{
			var eventIdConfig = configuration.GetSection("ImapEventIds:TlsInitializationFailed").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing ImapEventIds:TlsInitializationFailed");
			logger.LogError(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				ex,
				configuration["ImapLogMessages:TlsInitializationFailed"],
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
