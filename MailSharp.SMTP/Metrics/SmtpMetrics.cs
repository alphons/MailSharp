using System.Collections.Concurrent;

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
	private long bytesReceived;

	public long TotalConnections => totalConnections;
	public long ActiveSessions => activeSessions;
	public long MessagesReceived => messagesReceived;
	public long MessagesRelayed => messagesRelayed;
	public long MessagesRejectedSpf => messagesRejectedSpf;
	public long MessagesRejectedDkim => messagesRejectedDkim;
	public long MessagesRejectedDmarc => messagesRejectedDmarc;
	public long MessagesRejectedTotal => messagesRejectedSpf + messagesRejectedDkim + messagesRejectedDmarc;
	public long AuthSuccess => authSuccess;
	public long AuthFailed => authFailed;
	public long BytesReceived => bytesReceived;
	public long UptimeSeconds => (long)(DateTime.UtcNow - StartTime).TotalSeconds;
	public bool IsRunning { get; set; } = false;
	public DateTime StartTime { get; } = DateTime.UtcNow;
	public DateTime? LastMessageReceivedAt { get; private set; }

	private readonly ConcurrentDictionary<string, long> senderDomains = new(StringComparer.OrdinalIgnoreCase);
	private readonly ConcurrentDictionary<string, long> recipientDomains = new(StringComparer.OrdinalIgnoreCase);

	public IReadOnlyDictionary<string, long> TopSenderDomains => senderDomains;
	public IReadOnlyDictionary<string, long> TopRecipientDomains => recipientDomains;

	public void IncrementConnections() => Interlocked.Increment(ref totalConnections);
	public void IncrementActive() => Interlocked.Increment(ref activeSessions);
	public void DecrementActive() => Interlocked.Decrement(ref activeSessions);
	public void IncrementRelayed() => Interlocked.Increment(ref messagesRelayed);
	public void IncrementRejectedSpf() => Interlocked.Increment(ref messagesRejectedSpf);
	public void IncrementRejectedDkim() => Interlocked.Increment(ref messagesRejectedDkim);
	public void IncrementRejectedDmarc() => Interlocked.Increment(ref messagesRejectedDmarc);
	public void IncrementAuthSuccess() => Interlocked.Increment(ref authSuccess);
	public void IncrementAuthFailed() => Interlocked.Increment(ref authFailed);

	public void MessageReceived(string senderDomain, IEnumerable<string> recipientDomains, long bytes)
	{
		Interlocked.Increment(ref messagesReceived);
		Interlocked.Add(ref bytesReceived, bytes);
		LastMessageReceivedAt = DateTime.UtcNow;

		senderDomains.AddOrUpdate(senderDomain, 1, (_, c) => c + 1);
		foreach (var domain in recipientDomains)
			this.recipientDomains.AddOrUpdate(domain, 1, (_, c) => c + 1);
	}
}
