namespace MailSharp.IMAP.Metrics;

public sealed class ImapMetrics
{
	public long UptimeSeconds => (long)(DateTime.UtcNow - StartTime).TotalSeconds;
	public bool IsRunning { get; set; } = false;
	public DateTime StartTime { get; } = DateTime.UtcNow;

}