using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace MailSharp.SmtpServer.Session;

public partial class SmtpSession
{
	// Handle AUTH command
	private async Task HandleAuthAsync(string[] parts, string line)
	{
		if (!configuration.GetValue<bool>("SmtpSettings:EnableAuth"))
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:CommandNotRecognized"]);
			return;
		}

		if (state != SmtpState.HeloReceived && state != SmtpState.TlsStarted)
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:BadSequence"]);
			return;
		}

		if (requireTls && state != SmtpState.TlsStarted)
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:TlsRequired"]);
			return;
		}

		if (parts.Length < 2)
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:SyntaxError"]);
			return;
		}

		string mechanism = parts[1].ToUpper();
		if (mechanism == "PLAIN")
		{
			// Handle AUTH PLAIN
			string? credentials = parts.Length > 2 ? parts[2] : await reader!.ReadLineAsync();
			if (credentials == null)
			{
				await writer!.WriteLineAsync(configuration["SmtpResponses:AuthFailed"]);
				return;
			}

			try
			{
				string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(credentials));
				string[] credentialParts = decoded.Split('\0');
				if (credentialParts.Length != 3 || !ValidateCredentials(credentialParts[1], credentialParts[2]))
				{
					await writer!.WriteLineAsync(configuration["SmtpResponses:AuthFailed"]);
					return;
				}
				await writer!.WriteLineAsync(configuration["SmtpResponses:AuthSuccess"]);
			}
			catch
			{
				await writer!.WriteLineAsync(configuration["SmtpResponses:AuthFailed"]);
			}
		}
		else if (mechanism == "CRAM-MD5")
		{
			// Handle AUTH CRAM-MD5
			string challenge = $"<{Guid.NewGuid()}.{DateTime.UtcNow.Ticks}@{configuration["SmtpSettings:Host"]}>";
			await writer!.WriteLineAsync(string.Format(configuration["SmtpResponses:CramMd5Challenge"]!, Convert.ToBase64String(Encoding.UTF8.GetBytes(challenge))));

			string? response = await reader!.ReadLineAsync();
			if (response == null)
			{
				await writer!.WriteLineAsync(configuration["SmtpResponses:AuthFailed"]);
				return;
			}

			try
			{
				string decodedResponse = Encoding.UTF8.GetString(Convert.FromBase64String(response));
				string[] responseParts = decodedResponse.Split(' ');
				if (responseParts.Length != 2)
				{
					await writer!.WriteLineAsync(configuration["SmtpResponses:AuthFailed"]);
					return;
				}

				string username = responseParts[0];
				string clientDigest = responseParts[1];
				string? password = configuration[$"SmtpSettings:Credentials:{username}"];
				if (password == null || !ValidateCramMd5(challenge, password, clientDigest))
				{
					await writer!.WriteLineAsync(configuration["SmtpResponses:AuthFailed"]);
					return;
				}
				await writer!.WriteLineAsync(configuration["SmtpResponses:AuthSuccess"]);
			}
			catch
			{
				await writer!.WriteLineAsync(configuration["SmtpResponses:AuthFailed"]);
			}
		}
		else if (mechanism == "LOGIN")
		{
			// Handle AUTH LOGIN
			await writer!.WriteLineAsync(configuration["SmtpResponses:AuthLoginUsernamePrompt"]);
			string? usernameBase64 = await reader!.ReadLineAsync();
			if (usernameBase64 == null)
			{
				await writer!.WriteLineAsync(configuration["SmtpResponses:AuthFailed"]);
				return;
			}

			await writer!.WriteLineAsync(configuration["SmtpResponses:AuthLoginPasswordPrompt"]);
			string? passwordBase64 = await reader!.ReadLineAsync();
			if (passwordBase64 == null)
			{
				await writer!.WriteLineAsync(configuration["SmtpResponses:AuthFailed"]);
				return;
			}

			try
			{
				string username = Encoding.UTF8.GetString(Convert.FromBase64String(usernameBase64));
				string password = Encoding.UTF8.GetString(Convert.FromBase64String(passwordBase64));
				if (!ValidateCredentials(username, password))
				{
					await writer!.WriteLineAsync(configuration["SmtpResponses:AuthFailed"]);
					return;
				}
				await writer!.WriteLineAsync(configuration["SmtpResponses:AuthSuccess"]);
			}
			catch
			{
				await writer!.WriteLineAsync(configuration["SmtpResponses:AuthFailed"]);
			}
		}
		else
		{
			await writer!.WriteLineAsync(configuration["SmtpResponses:SyntaxError"]);
		}
	}

	// Validate PLAIN and LOGIN credentials (mock for testing)
	private bool ValidateCredentials(string username, string password)
	{
		string? storedPassword = configuration[$"SmtpSettings:Credentials:{username}"];
		return storedPassword != null && storedPassword == password;
	}

	// Validate CRAM-MD5 response
	private static bool ValidateCramMd5(string challenge, string password, string clientDigest)
	{
		using HMACMD5 hmac = new(Encoding.UTF8.GetBytes(password));
		byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(challenge));
		string expectedDigest = BitConverter.ToString(hash).Replace("-", "").ToLower();
		return clientDigest.Equals(expectedDigest, StringComparison.OrdinalIgnoreCase);
	}
}