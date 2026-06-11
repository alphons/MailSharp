using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace MailSharp.DNS;

public partial class Resolver
{
	public static string GetArpaFromIp(IPAddress ip)
	{
		if (ip.AddressFamily == AddressFamily.InterNetwork)
		{
			var sb = new StringBuilder("in-addr.arpa.");
			foreach (var b in ip.GetAddressBytes())
				sb.Insert(0, $"{b}.");
			return sb.ToString();
		}

		if (ip.AddressFamily == AddressFamily.InterNetworkV6)
		{
			var sb = new StringBuilder("ip6.arpa.");
			foreach (var b in ip.GetAddressBytes())
			{
				sb.Insert(0, $"{(b & 0xF):x}.{(b >> 4):x}.");
				return sb.ToString();
			}
		}

		return "?";
	}

	/// <summary>
	/// Converts an E.164 phone number to an arpa domain name (RFC 3761)
	/// Example: +31161234567 → 7.6.5.4.3.2.1.6.1.1.3.e164.arpa.
	/// </summary>
	/// <param name="strEnum">The E.164 number (with or without + and non-digits)</param>
	/// <returns>The ENUM arpa name</returns>
	public static string GetArpaFromEnum(string strEnum)
	{
		if (string.IsNullOrEmpty(strEnum))
			return "e164.arpa.";

		// Remove everything that is not a digit
		string digitsOnly = NonDigitRegex().Replace(strEnum, "");

		StringBuilder sb = new StringBuilder();
		sb.Append("e164.arpa.");

		// Insert dots from right to left
		foreach (char c in digitsOnly)
		{
			sb.Insert(0, c);
			sb.Insert(0, '.');
		}

		return sb.ToString();
	}

	[GeneratedRegex("[^0-9]")]
	private static partial Regex NonDigitRegex();


}