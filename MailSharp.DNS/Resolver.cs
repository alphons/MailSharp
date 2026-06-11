using System.Reflection;

namespace MailSharp.DNS;

public partial class Resolver
{
	public static string Version =>
		Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

	private uint uniqueId = (uint)new Random().Next();
	private bool useCache = true;
	private bool recursion = true;
	private int retries = 3;
	private int timeoutSeconds = 1;
	private TransportType transportType = TransportType.Udp;

	public int TimeOut { get => timeoutSeconds; set => timeoutSeconds = value; }
	public int Retries { get => retries; set => retries = value >= 1 ? value : retries; }
	public bool Recursion { get => recursion; set => recursion = value; }
	public TransportType TransportType { get => transportType; set => transportType = value; }
	public bool UseCache { get => useCache; set { useCache = value; if (!value) recordCache.Clear(); } }

	public void ClearCache() => recordCache.Clear();

}
