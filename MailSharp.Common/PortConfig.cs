namespace MailSharp.Common;

public class PortConfig
{
	public string Host { get; set; } = "127.0.01";
	public int Port { get; set; } = 25;
	public SecurityEnum Security { get; set; }
}