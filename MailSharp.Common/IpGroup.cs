using System.Net;

namespace MailSharp.Common;

public class IpGroupAccess
{
	public bool Smtp                 { get; set; }
	public bool Pop3                 { get; set; }
	public bool Imap                 { get; set; }
	public bool RequireSslTlsForAuth { get; set; }
}

public class IpGroup
{
	public string         Name     { get; set; } = string.Empty;
	public int            Priority { get; set; }
	public string         Cidr     { get; set; } = string.Empty;
	public DateTimeOffset? Expires { get; set; }
	public IpGroupAccess  Access   { get; set; } = new();
}

public static class IpGroupMatcher
{
	public static IpGroup? Match(IEnumerable<IpGroup> groups, IPAddress clientIp)
	{
		if (clientIp.IsIPv4MappedToIPv6)
			clientIp = clientIp.MapToIPv4();

		var now = DateTimeOffset.UtcNow;
		return groups
			.Where(g => g.Expires == null || g.Expires > now)
			.Where(g => IsInCidr(clientIp, g.Cidr))
			.OrderBy(g => g.Priority)
			.FirstOrDefault();
	}

	private static bool IsInCidr(IPAddress ip, string cidr)
	{
		var slash = cidr.IndexOf('/');
		if (slash < 0) return false;
		if (!IPAddress.TryParse(cidr[..slash], out var network)) return false;
		if (!int.TryParse(cidr[(slash + 1)..], out var prefix)) return false;

		var ipBytes  = ip.GetAddressBytes();
		var netBytes = network.GetAddressBytes();
		if (ipBytes.Length != netBytes.Length) return false;

		int fullBytes = prefix / 8;
		int remainder = prefix % 8;

		for (int i = 0; i < fullBytes; i++)
			if (ipBytes[i] != netBytes[i]) return false;

		if (remainder > 0)
		{
			byte mask = (byte)(0xFF << (8 - remainder));
			if ((ipBytes[fullBytes] & mask) != (netBytes[fullBytes] & mask)) return false;
		}

		return true;
	}
}
