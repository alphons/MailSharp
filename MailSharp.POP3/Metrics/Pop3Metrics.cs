namespace MailSharp.POP3.Metrics;

public sealed class Pop3Metrics
{
	public long UptimeSeconds => (long)(DateTime.UtcNow - StartTime).TotalSeconds;
	public bool IsRunning { get; set; } = false;
	public DateTime StartTime { get; } = DateTime.UtcNow;

}