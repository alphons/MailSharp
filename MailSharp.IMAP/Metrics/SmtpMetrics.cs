using System.Collections.Concurrent;

namespace MailSharp.IMAP.Metrics;

public sealed class ImapMetrics
{
	private long totalConnections;
	private long activeSessions;
	private long loginSuccess;
	private long loginFailed;
	private long messagesFetched;
	private long bytesSent;
	private long commandsProcessed;
	private long folderSelects;

	public long TotalConnections => totalConnections;
	public long ActiveSessions => activeSessions;
	public long LoginSuccess => loginSuccess;
	public long LoginFailed => loginFailed;
	public long MessagesFetched => messagesFetched;
	public long BytesSent => bytesSent;
	public long CommandsProcessed => commandsProcessed;
	public long FolderSelects => folderSelects;
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
	public void IncrementCommands() => Interlocked.Increment(ref commandsProcessed);
	public void IncrementFolderSelects() => Interlocked.Increment(ref folderSelects);

	public void LoginSucceeded(string username)
	{
		Interlocked.Increment(ref loginSuccess);
		LastActivityAt = DateTime.UtcNow;
		activeUsers.AddOrUpdate(username, 1, (_, c) => c + 1);
	}

	public void MessageFetched(long bytes)
	{
		Interlocked.Increment(ref messagesFetched);
		Interlocked.Add(ref bytesSent, bytes);
		LastActivityAt = DateTime.UtcNow;
	}
}
