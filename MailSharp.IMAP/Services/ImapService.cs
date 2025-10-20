using MailSharp.Common;
using MailSharp.IMAP.Server;

namespace MailSharp.IMAP.Services;

public class ImapService(IConfiguration configuration,
	ILogger<ImapServer> serverLogger
	) : BackgroundService, IServerStatus

{
	public bool IsRunning { get; set; }

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{

	}
}
