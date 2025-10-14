using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MailSharp.Smtp.Services;

public class DkimVerifier(IConfiguration configuration)
{
	private static readonly string[] separator = ["\r\n", "\n"];

	// Verify DKIM signature of incoming email
	public async Task<bool> VerifyDkimAsync(string emlContent, string clientIp)
	{
		// Split headers and body
		int headerEnd = emlContent.IndexOf("\r\n\r\n", StringComparison.Ordinal);
		if (headerEnd == -1)
		{
			return false; // Invalid email format
		}

		string headers = emlContent[..headerEnd];
		string body = emlContent[(headerEnd + 4)..];

		// Extract DKIM-Signature header
		string[] headerLines = headers.Split("\r\n");
		string? dkimHeader = headerLines.FirstOrDefault(h => h.StartsWith("DKIM-Signature:", StringComparison.OrdinalIgnoreCase));
		if (dkimHeader == null)
		{
			return false; // No DKIM signature
		}

		// Parse DKIM-Signature fields
		var dkimFields = ParseDkimHeader(dkimHeader);
		if (!dkimFields.TryGetValue("d", out string? domain) ||
			!dkimFields.TryGetValue("s", out string? selector) ||
			!dkimFields.TryGetValue("b", out string? signatureBase64) ||
			!dkimFields.TryGetValue("bh", out string? bodyHashBase64) ||
			!dkimFields.TryGetValue("h", out string? signedHeaders))
		{
			return false; // Invalid DKIM header
		}

		// Verify body hash
		string canonicalBody = CanonicalizeBody(body);
		byte[] bodyBytes = Encoding.ASCII.GetBytes(canonicalBody);
		byte[] computedBodyHash = SHA256.HashData(bodyBytes);
		string computedBodyHashBase64 = Convert.ToBase64String(computedBodyHash);
		if (computedBodyHashBase64 != bodyHashBase64)
		{
			return false; // Body hash mismatch
		}

		// Fetch public key from DNS
		string? publicKey = await FetchDkimPublicKeyAsync(selector, domain);
		if (publicKey == null)
		{
			return false; // Public key not found
		}

		// Prepare headers for verification
		string[] signedHeaderNames = [.. signedHeaders.Split(':').Select(h => h.Trim().ToLower())];
		var headersToVerify = headerLines
			.Where(h => signedHeaderNames.Any(sh => h.StartsWith(sh + ":", StringComparison.OrdinalIgnoreCase)))
			.Reverse()
			.ToList();
		headersToVerify.Add(dkimHeader[..dkimHeader.IndexOf("; b=")]); // Include DKIM-Signature without signature
		string canonicalHeaders = string.Join("\r\n", headersToVerify);

		// Verify signature
		try
		{
			using RSA rsa = RSA.Create();
			rsa.ImportFromPem(publicKey);
			byte[] signature = Convert.FromBase64String(signatureBase64);
			byte[] headerBytes = Encoding.ASCII.GetBytes(canonicalHeaders);
			return rsa.VerifyData(headerBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		}
		catch
		{
			return false; // Signature verification failed
		}
	}

	// Canonicalize body (simple canonicalization per RFC 6376)
	private static string CanonicalizeBody(string body)
	{
		body = body.TrimEnd();
		body = string.Join("\r\n", body.Split(separator, StringSplitOptions.None));
		return body + "\r\n";
	}

	// Parse DKIM-Signature header into key-value pairs
	private static Dictionary<string, string> ParseDkimHeader(string dkimHeader)
	{
		var fields = new Dictionary<string, string>();
		string[] parts = dkimHeader["DKIM-Signature:".Length..].Split(';');
		foreach (string part in parts)
		{
			string trimmed = part.Trim();
			if (string.IsNullOrEmpty(trimmed))
				continue;
			int equalsIndex = trimmed.IndexOf('=');
			if (equalsIndex == -1)
				continue;
			string key = trimmed[..equalsIndex].Trim();
			string value = trimmed[(equalsIndex + 1)..].Trim();
			fields[key] = value;
		}
		return fields;
	}

	// Fetch DKIM public key from DNS TXT record
	private static async Task<string?> FetchDkimPublicKeyAsync(string selector, string domain)
	{
		try
		{
			string query = $"{selector}._domainkey.{domain}";
			var result = await Dns.GetHostEntryAsync(query);
			string[] txtRecords = [.. result.Aliases
				.Select(a => Dns.GetHostEntry(a).HostName)
				.Where(h => h.Contains("p="))];

			string? txtRecord = txtRecords.FirstOrDefault();
			if (txtRecord == null)
				return null;

			int keyStart = txtRecord.IndexOf("p=") + 2;
			string keyBase64 = txtRecord[keyStart..];
			byte[] keyBytes = Convert.FromBase64String(keyBase64);
			return Encoding.ASCII.GetString(keyBytes);
		}
		catch
		{
			return null;
		}
	}
}
