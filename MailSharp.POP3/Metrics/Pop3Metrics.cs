using System.Collections.Concurrent;

namespace MailSharp.POP3.Metrics;

public sealed class Pop3Metrics
{
	private long totalConnections;
	private long activeSessions;
	private long loginSuccess;
	private long loginFailed;
	private long messagesRetrieved;
	private long messagesDeleted;
	private long bytesSent;

	public long TotalConnections => totalConnections;
	public long ActiveSessions => activeSessions;
	public long LoginSuccess => loginSuccess;
	public long LoginFailed => loginFailed;
	public long MessagesRetrieved => messagesRetrieved;
	public long MessagesDeleted => messagesDeleted;
	public long BytesSent => bytesSent;
	public long UptimeSeconds => (long)(DateTime.UtcNow - StartTime).TotalSeconds;
	public bool IsRunning { get; set; } = false;
	public DateTime StartTime { get; } = DateTime.UtcNow;
	public DateTime? LastActivityAt { get; private set; }

	private readonly ConcurrentDictionary<string, long> activeUsers = new(StringComparer.OrdinalIgnoreCase);
	public IReadOnlyDictionary<string, long> TopUsers => activeUsers;

	public void IncrementConnections() => Interlocked.Increment(ref totalConnections);
	public void IncrementActive() => Interlocked.Increment(ref activeSessions);
	public void DecrementActive() => Interlocked.Decrement(ref activeSessions);
	public void IncrementLoginFailed() => Interlocked.Increment(ref loginFailed);
	public void IncrementDeleted() => Interlocked.Increment(ref messagesDeleted);

	public void LoginSucceeded(string username)
	{
		Interlocked.Increment(ref loginSuccess);
		LastActivityAt = DateTime.UtcNow;
		activeUsers.AddOrUpdate(username, 1, (_, c) => c + 1);
	}

	public void MessageRetrieved(long bytes)
	{
		Interlocked.Increment(ref messagesRetrieved);
		Interlocked.Add(ref bytesSent, bytes);
		LastActivityAt = DateTime.UtcNow;
	}
}
