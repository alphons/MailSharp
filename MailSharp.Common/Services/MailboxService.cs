using System.Text.Json;

namespace MailSharp.Common.Services;

public class MailboxService(
	IConfiguration configuration, 
	ILogger<MailboxService> logger)
{

	// Retrieve list of messages for a user in a specific folder
	public async Task<List<Message>> GetMessagesAsync(string username, string folder, CancellationToken cancellationToken)
	{
		var eventIdConfig = configuration.GetSection("MailboxEventIds:MessagesRequested").Get<EventIdConfig>()
			?? throw new InvalidOperationException("Missing MailboxEventIds:MessagesRequested");
		logger.LogInformation(
			new EventId(eventIdConfig.Id, eventIdConfig.Name),
			configuration["MailboxLogMessages:MessagesRequested"],
			username, folder);

		try
		{
			string mailboxPath = Path.Combine(
				configuration["MailboxSettings:StoragePath"] ?? throw new InvalidOperationException("MailboxStoragePath not configured"),
				username,
				folder);
			if (!Directory.Exists(mailboxPath))
			{
				return new List<Message>();
			}

			var messages = new List<Message>();
			foreach (string file in Directory.GetFiles(mailboxPath, "*.eml"))
			{
				string messageId = Path.GetFileNameWithoutExtension(file);
				string metadataPath = Path.Combine(mailboxPath, $"{messageId}.json");
				Message message = new()
				{
					Uid = messageId,
					Flags = File.Exists(metadataPath)
						? JsonSerializer.Deserialize<MessageFlags>(await File.ReadAllTextAsync(metadataPath, cancellationToken))
						: new MessageFlags()
				};
				messages.Add(message);
			}

			return messages;
		}
		catch (Exception ex)
		{
			logger.LogError(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				ex,
				configuration["MailboxLogMessages:MessagesRetrievalFailed"],
				username, folder);
			return new List<Message>();
		}
	}

	// Delete a message for a user in a specific folder
	public async Task<bool> DeleteMessageAsync(string username, string folder, string messageId, CancellationToken cancellationToken)
	{
		var eventIdConfig = configuration.GetSection("MailboxEventIds:MessageDeletion").Get<EventIdConfig>()
			?? throw new InvalidOperationException("Missing MailboxEventIds:MessageDeletion");
		logger.LogInformation(
			new EventId(eventIdConfig.Id, eventIdConfig.Name),
			configuration["MailboxLogMessages:MessageDeletion"],
			username, messageId, folder);

		try
		{
			string mailboxPath = Path.Combine(
				configuration["MailboxSettings:StoragePath"] ?? throw new InvalidOperationException("MailboxStoragePath not configured"),
				username,
				folder);
			string filePath = Path.Combine(mailboxPath, $"{messageId}.eml");
			string metadataPath = Path.Combine(mailboxPath, $"{messageId}.json");

			if (!File.Exists(filePath))
			{
				logger.LogWarning(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["MailboxLogMessages:MessageNotFound"],
					username, messageId, folder);
				return false;
			}

			await Task.Run(() =>
			{
				File.Delete(filePath);
				if (File.Exists(metadataPath))
					File.Delete(metadataPath);
			}, cancellationToken);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogError(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				ex,
				configuration["MailboxLogMessages:MessageDeletionFailed"],
				username, messageId, folder);
			return false;
		}
	}

	// Create a new folder for a user
	public async Task<bool> CreateFolderAsync(string username, string folder, CancellationToken cancellationToken)
	{
		var eventIdConfig = configuration.GetSection("MailboxEventIds:FolderCreation").Get<EventIdConfig>()
			?? throw new InvalidOperationException("Missing MailboxEventIds:FolderCreation");
		logger.LogInformation(
			new EventId(eventIdConfig.Id, eventIdConfig.Name),
			configuration["MailboxLogMessages:FolderCreation"],
			username, folder);

		try
		{
			string mailboxPath = Path.Combine(
				configuration["MailboxSettings:StoragePath"] ?? throw new InvalidOperationException("MailboxStoragePath not configured"),
				username,
				folder);
			if (Directory.Exists(mailboxPath))
			{
				logger.LogWarning(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["MailboxLogMessages:FolderExists"],
					username, folder);
				return false;
			}

			await Task.Run(() => Directory.CreateDirectory(mailboxPath), cancellationToken);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogError(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				ex,
				configuration["MailboxLogMessages:FolderCreationFailed"],
				username, folder);
			return false;
		}
	}

	// Delete a folder for a user
	public async Task<bool> DeleteFolderAsync(string username, string folder, CancellationToken cancellationToken)
	{
		var eventIdConfig = configuration.GetSection("MailboxEventIds:FolderDeletion").Get<EventIdConfig>()
			?? throw new InvalidOperationException("Missing MailboxEventIds:FolderDeletion");
		logger.LogInformation(
			new EventId(eventIdConfig.Id, eventIdConfig.Name),
			configuration["MailboxLogMessages:FolderDeletion"],
			username, folder);

		try
		{
			string mailboxPath = Path.Combine(
				configuration["MailboxSettings:StoragePath"] ?? throw new InvalidOperationException("MailboxStoragePath not configured"),
				username,
				folder);
			if (!Directory.Exists(mailboxPath))
			{
				logger.LogWarning(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["MailboxLogMessages:FolderNotFound"],
					username, folder);
				return false;
			}

			await Task.Run(() => Directory.Delete(mailboxPath, true), cancellationToken);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogError(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				ex,
				configuration["MailboxLogMessages:FolderDeletionFailed"],
				username, folder);
			return false;
		}
	}

	// List folders for a user
	public async Task<List<string>> ListFoldersAsync(string username, CancellationToken cancellationToken)
	{
		var eventIdConfig = configuration.GetSection("MailboxEventIds:FolderList").Get<EventIdConfig>()
			?? throw new InvalidOperationException("Missing MailboxEventIds:FolderList");
		logger.LogInformation(
			new EventId(eventIdConfig.Id, eventIdConfig.Name),
			configuration["MailboxLogMessages:FolderList"],
			username);

		try
		{
			string mailboxPath = Path.Combine(
				configuration["MailboxSettings:StoragePath"] ?? throw new InvalidOperationException("MailboxStoragePath not configured"),
				username);
			if (!Directory.Exists(mailboxPath))
			{
				return new List<string> { "INBOX" }; // Default folder
			}

			var folders = Directory.GetDirectories(mailboxPath)
				.Select(d => Path.GetFileName(d))
				.ToList();
			folders.Add("INBOX"); // Ensure INBOX is always included
			return await Task.FromResult(folders);
		}
		catch (Exception ex)
		{
			logger.LogError(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				ex,
				configuration["MailboxLogMessages:FolderListFailed"],
				username);
			return new List<string> { "INBOX" };
		}
	}

	// Set flags for a message
	public async Task<bool> SetMessageFlagsAsync(string username, string folder, string messageId, MessageFlags flags, CancellationToken cancellationToken)
	{
		var eventIdConfig = configuration.GetSection("MailboxEventIds:FlagUpdate").Get<EventIdConfig>()
			?? throw new InvalidOperationException("Missing MailboxEventIds:FlagUpdate");
		logger.LogInformation(
			new EventId(eventIdConfig.Id, eventIdConfig.Name),
			configuration["MailboxLogMessages:FlagUpdate"],
			username, messageId, folder);

		try
		{
			string mailboxPath = Path.Combine(
				configuration["MailboxSettings:StoragePath"] ?? throw new InvalidOperationException("MailboxStoragePath not configured"),
				username,
				folder);
			string metadataPath = Path.Combine(mailboxPath, $"{messageId}.json");

			if (!File.Exists(Path.Combine(mailboxPath, $"{messageId}.eml")))
			{
				logger.LogWarning(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["MailboxLogMessages:MessageNotFound"],
					username, messageId, folder);
				return false;
			}

			string json = JsonSerializer.Serialize(flags);
			await File.WriteAllTextAsync(metadataPath, json, cancellationToken);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogError(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				ex,
				configuration["MailboxLogMessages:FlagUpdateFailed"],
				username, messageId, folder);
			return false;
		}
	}

	// Retrieve message content
	public async Task<string?> GetMessageContentAsync(string username, string folder, string messageId, CancellationToken cancellationToken)
	{
		var eventIdConfig = configuration.GetSection("MailboxEventIds:MessageFetch").Get<EventIdConfig>()
			?? throw new InvalidOperationException("Missing MailboxEventIds:MessageFetch");
		logger.LogInformation(
			new EventId(eventIdConfig.Id, eventIdConfig.Name),
			configuration["MailboxLogMessages:MessageFetch"],
			username, messageId, folder);

		try
		{
			string mailboxPath = Path.Combine(
				configuration["MailboxSettings:StoragePath"] ?? throw new InvalidOperationException("MailboxStoragePath not configured"),
				username,
				folder);
			string filePath = Path.Combine(mailboxPath, $"{messageId}.eml");

			if (!File.Exists(filePath))
			{
				logger.LogWarning(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["MailboxLogMessages:MessageNotFound"],
					username, messageId, folder);
				return null;
			}

			return await File.ReadAllTextAsync(filePath, cancellationToken);
		}
		catch (Exception ex)
		{
			logger.LogError(
				new EventId(eventIdConfig.Id, eventIdConfig.Name),
				ex,
				configuration["MailboxLogMessages:MessageFetchFailed"],
				username, messageId, folder);
			return null;
		}
	}
}
