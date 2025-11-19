namespace MailSharp.SMTP.Metrics;

public sealed class SmtpMetrics
{
	private long totalConnections;
	private long activeSessions;
	private long messagesReceived;
	private long messagesRelayed;
	private long messagesRejectedSpf;
	private long messagesRejectedDkim;
	private long messagesRejectedDmarc;
	private long authSuccess;
	private long authFailed;

	public long TotalConnections => totalConnections;
	public long ActiveSessions => activeSessions;
	public long MessagesReceived => messagesReceived;
	public long MessagesRelayed => messagesRelayed;
	public long MessagesRejectedSpf => messagesRejectedSpf;
	public long MessagesRejectedDkim => messagesRejectedDkim;
	public long MessagesRejectedDmarc => messagesRejectedDmarc;
	public long AuthSuccess => authSuccess;
	public long AuthFailed => authFailed;
	public long UptimeSeconds => (long)(DateTime.UtcNow - StartTime).TotalSeconds;
	public bool IsRunning { get; set; } = false;

	public DateTime StartTime { get; } = DateTime.UtcNow;

	public void IncrementConnections() => Interlocked.Increment(ref totalConnections);
	public void IncrementActive() => Interlocked.Increment(ref activeSessions);
	public void DecrementActive() => Interlocked.Decrement(ref activeSessions);
	public void IncrementReceived() => Interlocked.Increment(ref messagesReceived);
	public void IncrementRelayed() => Interlocked.Increment(ref messagesRelayed);
	public void IncrementRejectedSpf() => Interlocked.Increment(ref messagesRejectedSpf);
	public void IncrementRejectedDkim() => Interlocked.Increment(ref messagesRejectedDkim);
	public void IncrementRejectedDmarc() => Interlocked.Increment(ref messagesRejectedDmarc);
	public void IncrementAuthSuccess() => Interlocked.Increment(ref authSuccess);
	public void IncrementAuthFailed() => Interlocked.Increment(ref authFailed);
}