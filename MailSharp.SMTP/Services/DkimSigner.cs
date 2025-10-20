using System.Security.Cryptography;
using System.Text;

namespace MailSharp.SMTP.Services;

public class DkimSigner(IConfiguration configuration)
{
	private static readonly string[] separator = ["\r\n", "\n"];

	// Sign email content with DKIM
	public string SignEmail(string emlContent, string selector, string domain)
	{
		string privateKey = configuration[$"SmtpSettings:Dkim:{domain}:PrivateKey"]
			?? throw new InvalidOperationException($"DKIM private key for {domain} not configured");

		// Parse email headers and body
		int headerEnd = emlContent.IndexOf("\r\n\r\n", StringComparison.Ordinal);
		if (headerEnd == -1)
		{
			throw new InvalidOperationException("Invalid email format: no header-body separator");
		}

		string headers = emlContent[..headerEnd];
		string body = emlContent[(headerEnd + 4)..];

		// Calculate body hash (BHASH)
		string canonicalBody = CanonicalizeBody(body);
		byte[] bodyBytes = Encoding.ASCII.GetBytes(canonicalBody);
		byte[] bodyHash = SHA256.HashData(bodyBytes);
		string bodyHashBase64 = Convert.ToBase64String(bodyHash);

		// Prepare DKIM-Signature header
		string dkimHeader = GenerateDkimHeader(selector, domain, bodyHashBase64, headers);
		byte[] headerBytes = Encoding.ASCII.GetBytes(dkimHeader);

		// Sign the DKIM-Signature header
		using RSA rsa = RSA.Create();
		rsa.ImportFromPem(privateKey);
		byte[] signature = rsa.SignData(headerBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		string signatureBase64 = Convert.ToBase64String(signature);

		// Append signature to DKIM-Signature header
		dkimHeader += $"; s={signatureBase64}";

		// Insert DKIM-Signature header at the start of headers
		return $"{dkimHeader}\r\n{emlContent}";
	}

	// Canonicalize body (simple canonicalization per RFC 6376)
	private static string CanonicalizeBody(string body)
	{
		// Remove trailing empty lines and normalize line endings
		body = body.TrimEnd();
		body = string.Join("\r\n", body.Split(separator, StringSplitOptions.None));
		return body + "\r\n";
	}

	// Generate DKIM-Signature header
	private static string GenerateDkimHeader(string selector, string domain, string bodyHash, string headers)
	{
		string[] headerLines = headers.Split("\r\n");
		var signedHeaders = new[] { "from", "to", "subject", "date", "message-id" };
		var headersToSign = headerLines
			.Where(h => signedHeaders.Any(sh => h.StartsWith(sh + ":", StringComparison.OrdinalIgnoreCase)))
			.Select(h => h.ToLower())
			.Reverse()
			.ToList();

		string hValue = string.Join(":", signedHeaders);
		string canonicalHeaders = string.Join("\r\n", headersToSign);

		return $"DKIM-Signature: v=1; a=rsa-sha256; d={domain}; s={selector}; " +
			   $"c=simple/simple; q=dns/txt; t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}; " +
			   $"bh={bodyHash}; h={hValue}; b=";
	}
}
