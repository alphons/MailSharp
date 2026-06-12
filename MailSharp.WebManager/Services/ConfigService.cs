using System.Text.Json;
using System.Text.Json.Nodes;

namespace MailSharp.WebManager.Services;

public class ConfigService(IConfiguration configuration, IWebHostEnvironment env)
{
	private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

	private string OverridePath => Path.Combine(env.ContentRootPath, "mailsharp.override.json");

	// ── Read ────────────────────────────────────────────────

	public SmtpConfigDto GetSmtp() => new()
	{
		EmlStoragePath        = configuration["SmtpSettings:EmlStoragePath"] ?? string.Empty,
		MaxMessageSize        = configuration.GetValue<int>("SmtpSettings:MaxMessageSize"),
		CommandTimeoutSeconds = configuration.GetValue<int>("SmtpSettings:CommandTimeoutSeconds"),
		BackLog               = configuration.GetValue<int>("SmtpSettings:BackLog"),
		EnableAuth            = configuration.GetValue<bool>("SmtpSettings:EnableAuth"),
		EnableStartTls        = configuration.GetValue<bool>("SmtpSettings:EnableStartTls"),
		EnableVrfy            = configuration.GetValue<bool>("SmtpSettings:EnableVrfy"),
		EnableExpn            = configuration.GetValue<bool>("SmtpSettings:EnableExpn"),
		RequireDkim           = configuration.GetValue<bool>("SmtpSettings:RequireDkim"),
		CertificatePath       = configuration["SmtpSettings:CertificatePath"] ?? string.Empty,
		CertificatePassword   = configuration["SmtpSettings:CertificatePassword"] ?? string.Empty,
		UserStorePath         = configuration["SmtpSettings:UserStorePath"] ?? string.Empty,
		DnsResolvers          = configuration.GetSection("SmtpSettings:DnsResolvers").Get<List<string>>() ?? [],
		LocalDomains          = configuration.GetSection("SmtpSettings:LocalDomains").Get<List<string>>() ?? [],
		RelayQueuePath        = configuration["SmtpSettings:RelayQueuePath"] ?? string.Empty,
		RelayUseTls           = configuration.GetValue<bool>("SmtpSettings:RelayUseTls"),
		RelayTimeoutSeconds   = configuration.GetValue<int>("SmtpSettings:RelayTimeoutSeconds"),
		RelayRequiresAuth     = configuration.GetValue<bool>("SmtpSettings:RelayRequiresAuth"),
		RelayUsername         = configuration["SmtpSettings:RelayUsername"] ?? string.Empty,
		RelayPassword         = configuration["SmtpSettings:RelayPassword"] ?? string.Empty,
		Ports                 = configuration.GetSection("SmtpSettings:Ports").Get<List<PortConfigDto>>() ?? []
	};

	public Pop3ConfigDto GetPop3() => new()
	{
		CertificatePath     = configuration["Pop3Settings:CertificatePath"] ?? string.Empty,
		CertificatePassword = configuration["Pop3Settings:CertificatePassword"] ?? string.Empty,
		Ports               = configuration.GetSection("Pop3Settings:Ports").Get<List<PortConfigDto>>() ?? []
	};

	public ImapConfigDto GetImap() => new()
	{
		CertificatePath     = configuration["ImapSettings:CertificatePath"] ?? string.Empty,
		CertificatePassword = configuration["ImapSettings:CertificatePassword"] ?? string.Empty,
		Ports               = configuration.GetSection("ImapSettings:Ports").Get<List<PortConfigDto>>() ?? []
	};

	public DmarcConfigDto GetDmarc() => new()
	{
		FailOpen      = configuration.GetValue<bool>("DmarcSettings:FailOpen"),
		RequireDmarc  = configuration.GetValue<bool>("DmarcSettings:RequireDmarc")
	};

	public MailboxConfigDto GetMailbox() => new()
	{
		StoragePath = configuration["MailboxSettings:StoragePath"] ?? string.Empty
	};

	// ── Write ───────────────────────────────────────────────

	public void SaveSmtp(SmtpConfigDto dto)       => PatchSection("SmtpSettings", dto);
	public void SavePop3(Pop3ConfigDto dto)        => PatchSection("Pop3Settings", dto);
	public void SaveImap(ImapConfigDto dto)        => PatchSection("ImapSettings", dto);
	public void SaveDmarc(DmarcConfigDto dto)      => PatchSection("DmarcSettings", dto);
	public void SaveMailbox(MailboxConfigDto dto)  => PatchSection("MailboxSettings", dto);

	private void PatchSection(string key, object dto)
	{
		var root = ReadOverride();
		root[key] = JsonNode.Parse(JsonSerializer.Serialize(dto, Pretty))!;
		File.WriteAllText(OverridePath, root.ToJsonString(Pretty));
	}

	private JsonObject ReadOverride()
	{
		if (!File.Exists(OverridePath))
			return [];
		try { return JsonNode.Parse(File.ReadAllText(OverridePath))?.AsObject() ?? []; }
		catch { return []; }
	}
}

// ── DTOs ────────────────────────────────────────────────────

public class PortConfigDto
{
	public string Host     { get; set; } = string.Empty;
	public int    Port     { get; set; }
	public string Security { get; set; } = string.Empty;
}

public class SmtpConfigDto
{
	public string       EmlStoragePath        { get; set; } = string.Empty;
	public int          MaxMessageSize        { get; set; }
	public int          CommandTimeoutSeconds { get; set; }
	public int          BackLog               { get; set; }
	public bool         EnableAuth            { get; set; }
	public bool         EnableStartTls        { get; set; }
	public bool         EnableVrfy            { get; set; }
	public bool         EnableExpn            { get; set; }
	public bool         RequireDkim           { get; set; }
	public string       CertificatePath       { get; set; } = string.Empty;
	public string       CertificatePassword   { get; set; } = string.Empty;
	public string       UserStorePath         { get; set; } = string.Empty;
	public List<string> DnsResolvers          { get; set; } = [];
	public List<string> LocalDomains          { get; set; } = [];
	public string       RelayQueuePath        { get; set; } = string.Empty;
	public bool         RelayUseTls           { get; set; }
	public int          RelayTimeoutSeconds   { get; set; }
	public bool         RelayRequiresAuth     { get; set; }
	public string       RelayUsername         { get; set; } = string.Empty;
	public string       RelayPassword         { get; set; } = string.Empty;
	public List<PortConfigDto> Ports          { get; set; } = [];
}

public class Pop3ConfigDto
{
	public string CertificatePath     { get; set; } = string.Empty;
	public string CertificatePassword { get; set; } = string.Empty;
	public List<PortConfigDto> Ports  { get; set; } = [];
}

public class ImapConfigDto
{
	public string CertificatePath     { get; set; } = string.Empty;
	public string CertificatePassword { get; set; } = string.Empty;
	public List<PortConfigDto> Ports  { get; set; } = [];
}

public class DmarcConfigDto
{
	public bool FailOpen     { get; set; }
	public bool RequireDmarc { get; set; }
}

public class MailboxConfigDto
{
	public string StoragePath { get; set; } = string.Empty;
}
