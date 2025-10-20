using MailSharp.DataModel;

namespace MailSharp.POP3.Services;

public class Pop3Service(IConfiguration configuration,
	ILogger<Server.Pop3Server> serverLogger
	) : BackgroundService, IServerStatus

{
	public bool IsRunning { get; set; }

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{

	}
}
