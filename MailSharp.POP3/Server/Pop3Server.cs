namespace MailSharp.POP3.Server;

public class Pop3Server(IConfiguration configuration,
		ILogger<Pop3Server> logger)
{
	private CancellationTokenSource? cts;

	public async Task StartAsync()
	{
		cts = new CancellationTokenSource();

		await Task.Yield();
	}

	public async Task StopAsync()
	{
		if (cts != null)
		{
			await cts.CancelAsync();
		}
		//foreach (var (tcplistener, _, _) in listeners)
		//{
		//	tcplistener.Stop();
		//	var eventIdConfig = configuration.GetSection("SmtpEventIds:ListenerStopped").Get<EventIdConfig>()
		//		?? throw new InvalidOperationException("Missing SmtpEventIds:ListenerStopped");
		//	logger.LogInformation(new EventId(eventIdConfig.Id, eventIdConfig.Name), configuration["SmtpLogMessages:ListenerStopped"], tcplistener.LocalEndpoint);
		//	tcplistener.Dispose();
		//}
		//listeners.Clear();
	}
}