using System.Net;
using System.Net.Mail;
using System.Net.Sockets;

namespace MailSharp.SMTP.Services;

public class RelayService(IConfiguration configuration, ILogger<RelayService> logger) : BackgroundService
{
	private readonly Queue<string> queue = new();

	public void Enqueue(string emlPath)
	{
		queue.Enqueue(emlPath);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		string relayQueuePath = configuration["SmtpSettings:RelayQueuePath"] ?? throw new InvalidOperationException("RelayQueuePath not configured");

		if (Directory.Exists(relayQueuePath))
		{
			foreach (string file in Directory.GetFiles(relayQueuePath, "*.eml"))
			{
				queue.Enqueue(file);
			}
		}

		while (!stoppingToken.IsCancellationRequested)
		{
			if (queue.TryDequeue(out string? emlPath) && File.Exists(emlPath))
			{
				await ProcessEmailAsync(emlPath, stoppingToken);
			}
			await Task.Delay(1000, stoppingToken);
		}
	}

	private async Task ProcessEmailAsync(string emlPath, CancellationToken ct)
	{
		try
		{
			string emlContent = await File.ReadAllTextAsync(emlPath, ct);
			string recipient = ExtractRecipient(emlContent);
			string domain = recipient[(recipient.IndexOf('@') + 1)..];

			// MX record lookup using System.Net.Dns
			IPHostEntry? dnsEntry = null;
			try
			{
				dnsEntry = await Dns.GetHostEntryAsync(domain, ct);
			}
			catch (SocketException)
			{
				logger.LogWarning("No MX records found for domain {Domain}", domain);
				return;
			}

			// Extract MX records from aliases (simplified, not ideal)
			string[] mxRecords = [.. dnsEntry.Aliases
				.Where(a => a.Contains("mail exchanger", StringComparison.OrdinalIgnoreCase))
				.Select(a => a.Split(' ').Last())];

			if (mxRecords.Length == 0)
			{
				logger.LogWarning("No valid MX records found for domain {Domain}", domain);
				return;
			}

			foreach (string mxServer in mxRecords)
			{
				try
				{
					using var client = new SmtpClient(mxServer)
					{
						EnableSsl = configuration.GetValue<bool>("SmtpSettings:RelayUseTls"),
						Timeout = configuration.GetValue<int>("SmtpSettings:RelayTimeoutSeconds") * 1000
					};

					if (configuration.GetValue<bool>("SmtpSettings:RelayRequiresAuth"))
					{
						client.Credentials = new NetworkCredential(
							configuration["SmtpSettings:RelayUsername"],
							configuration["SmtpSettings:RelayPassword"]);
					}

					await client.SendMailAsync(new MailMessage
					{
						Body = emlContent,
						To = { recipient }
					}, ct);

					await Task.Run(() => File.Delete(emlPath), ct);
					logger.LogInformation("Relayed email to {Domain} via {MxServer}", domain, mxServer);
					return;
				}
				catch (SmtpException ex)
				{
					logger.LogWarning("Failed to relay to {MxServer}: {Error}", mxServer, ex.Message);
					continue;
				}
			}

			logger.LogError("Failed to relay email to {Domain}: No reachable MX servers", domain);
		}
		catch (Exception ex)
		{
			logger.LogError("Error processing email {Path}: {Error}", emlPath, ex.Message);
		}
	}

	private static string ExtractRecipient(string emlContent)
	{
		string[] lines = emlContent.Split("\r\n");
		string? toLine = lines.FirstOrDefault(l => l.StartsWith("To:", StringComparison.OrdinalIgnoreCase));
		return toLine?[(toLine.IndexOf(':') + 1)..].Trim() ?? throw new InvalidOperationException("No recipient found");
	}
}
