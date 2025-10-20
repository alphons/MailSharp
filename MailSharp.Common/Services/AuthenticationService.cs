using System.Text.Json;

namespace MailSharp.Common.Services;

public class AuthenticationService(
	IConfiguration configuration, 
	ILogger<AuthenticationService> logger)
{

	// Authenticate user credentials for POP3 or IMAP
	public async Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
	{
		EventIdConfig eventIdConfig = configuration.GetSection("AuthEventIds:AuthenticationAttempt").Get<EventIdConfig>()
			?? throw new InvalidOperationException("Missing AuthEventIds:AuthenticationAttempt");
		logger.LogInformation(
			new EventId(eventIdConfig.Id, eventIdConfig.Name),
			configuration["AuthLogMessages:AuthenticationAttempt"],
			username);

		try
		{
			string userStorePath = configuration["AuthSettings:UserStorePath"] ?? throw new InvalidOperationException("UserStorePath not configured");
			if (!File.Exists(userStorePath))
			{
				eventIdConfig = configuration.GetSection("AuthEventIds:AuthenticationFailed").Get<EventIdConfig>()
					?? throw new InvalidOperationException("Missing AuthEventIds:AuthenticationFailed");
				logger.LogWarning(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["AuthLogMessages:AuthenticationFailed"],
					username);
				return false;
			}

			string json = await File.ReadAllTextAsync(userStorePath, cancellationToken);
			var users = JsonSerializer.Deserialize<List<UserConfig>>(json) ?? throw new InvalidOperationException("Invalid user store format");
			var user = users.FirstOrDefault(u => u.Username == username && u.Password == password);

			if (user != null)
			{
				logger.LogInformation(
					new EventId(eventIdConfig.Id, eventIdConfig.Name),
					configuration["AuthLogMessages:AuthenticationSuccess"],
					username);
				return true;
			}

			var errorEventIdConfig = configuration.GetSection("AuthEventIds:AuthenticationFailed").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing AuthEventIds:AuthenticationFailed");
			logger.LogWarning(
				new EventId(errorEventIdConfig.Id, errorEventIdConfig.Name),
				configuration["AuthLogMessages:AuthenticationFailed"],
				username);
			return false;
		}
		catch (Exception ex)
		{
			var errorEventIdConfig = configuration.GetSection("AuthEventIds:AuthenticationFailed").Get<EventIdConfig>()
				?? throw new InvalidOperationException("Missing AuthEventIds:AuthenticationFailed");
			logger.LogError(
				new EventId(errorEventIdConfig.Id, errorEventIdConfig.Name),
				ex,
				configuration["AuthLogMessages:AuthenticationFailed"],
				username);
			return false;
		}
	}

}
