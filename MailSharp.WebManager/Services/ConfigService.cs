using System.Text.Json;
using System.Text.Json.Nodes;

namespace MailSharp.WebManager.Services;

public class ConfigService(IWebHostEnvironment env)
{
	private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

	private string OverridePath => Path.Combine(env.ContentRootPath, "mailsharp.override.json");

	// ── Read ────────────────────────────────────────────────

	public SmtpConfigDto    GetSmtp()    => MakeSmtpDto(BuildMerged());
	public Pop3ConfigDto    GetPop3()    => MakePop3Dto(BuildMerged());
	public ImapConfigDto    GetImap()    => MakeImapDto(BuildMerged());
	public DmarcConfigDto   GetDmarc()   => MakeDmarcDto(BuildMerged());
	public MailboxConfigDto GetMailbox() => MakeMailboxDto(BuildMerged());

	private IConfiguration BuildMerged()
	{
		return new ConfigurationBuilder()
			.SetBasePath(env.ContentRootPath)
			.AddJsonFile("appsettings.json", optional: true)
			.AddJsonFile("mailsharp.override.json", optional: true)
			.Build();
	}

	private static SmtpConfigDto MakeSmtpDto(IConfiguration c) => new()
	{
		EmlStoragePath        = c["SmtpSettings:EmlStoragePath"] ?? string.Empty,
		MaxMessageSize        = c.GetValue<int>("SmtpSettings:MaxMessageSize"),
		CommandTimeoutSeconds = c.GetValue<int>("SmtpSettings:CommandTimeoutSeconds"),
		BackLog               = c.GetValue<int>("SmtpSettings:BackLog"),
		EnableAuth            = c.GetValue<bool>("SmtpSettings:EnableAuth"),
		EnableStartTls        = c.GetValue<bool>("SmtpSettings:EnableStartTls"),
		EnableVrfy            = c.GetValue<bool>("SmtpSettings:EnableVrfy"),
		EnableExpn            = c.GetValue<bool>("SmtpSettings:EnableExpn"),
		RequireDkim           = c.GetValue<bool>("SmtpSettings:RequireDkim"),
		CertificatePath       = c["SmtpSettings:CertificatePath"] ?? string.Empty,
		CertificatePassword   = c["SmtpSettings:CertificatePassword"] ?? string.Empty,
		UserStorePath         = c["SmtpSettings:UserStorePath"] ?? string.Empty,
		DnsResolvers          = c.GetSection("SmtpSettings:DnsResolvers").Get<List<string>>() ?? [],
		LocalDomains          = c.GetSection("SmtpSettings:LocalDomains").Get<List<string>>() ?? [],
		MaxConnections        = c.GetValue<int>("SmtpSettings:MaxConnections"),
		WelcomeMessage        = c["SmtpSettings:WelcomeMessage"] ?? string.Empty,
		RelayEnabled          = c.GetValue<bool>("SmtpSettings:RelayEnabled"),
		RelayQueuePath        = c["SmtpSettings:RelayQueuePath"] ?? string.Empty,
		RelayUseTls           = c.GetValue<bool>("SmtpSettings:RelayUseTls"),
		RelayTimeoutSeconds   = c.GetValue<int>("SmtpSettings:RelayTimeoutSeconds"),
		RelayRequiresAuth     = c.GetValue<bool>("SmtpSettings:RelayRequiresAuth"),
		RelayUsername         = c["SmtpSettings:RelayUsername"] ?? string.Empty,
		RelayPassword         = c["SmtpSettings:RelayPassword"] ?? string.Empty,
		Ports                 = c.GetSection("SmtpSettings:Ports").Get<List<PortConfigDto>>() ?? []
	};

	private static Pop3ConfigDto MakePop3Dto(IConfiguration c) => new()
	{
		CertificatePath     = c["Pop3Settings:CertificatePath"] ?? string.Empty,
		CertificatePassword = c["Pop3Settings:CertificatePassword"] ?? string.Empty,
		Ports               = c.GetSection("Pop3Settings:Ports").Get<List<PortConfigDto>>() ?? [],
		MaxConnections      = c.GetValue<int>("Pop3Settings:MaxConnections"),
		WelcomeMessage      = c["Pop3Settings:WelcomeMessage"] ?? string.Empty
	};

	private static ImapConfigDto MakeImapDto(IConfiguration c) => new()
	{
		CertificatePath      = c["ImapSettings:CertificatePath"] ?? string.Empty,
		CertificatePassword  = c["ImapSettings:CertificatePassword"] ?? string.Empty,
		Ports                = c.GetSection("ImapSettings:Ports").Get<List<PortConfigDto>>() ?? [],
		MaxConnections       = c.GetValue<int>("ImapSettings:MaxConnections"),
		WelcomeMessage       = c["ImapSettings:WelcomeMessage"] ?? string.Empty,
		PublicFolderName     = c["ImapSettings:PublicFolderName"] ?? "# Public",
		EnableSort           = c.GetValue<bool>("ImapSettings:EnableSort"),
		EnableQuota          = c.GetValue<bool>("ImapSettings:EnableQuota"),
		EnableIdle           = c.GetValue<bool>("ImapSettings:EnableIdle"),
		EnableAcl            = c.GetValue<bool>("ImapSettings:EnableAcl"),
		HierarchyDelimiter   = c["ImapSettings:HierarchyDelimiter"] ?? "."
	};

	private static DmarcConfigDto MakeDmarcDto(IConfiguration c) => new()
	{
		FailOpen     = c.GetValue<bool>("DmarcSettings:FailOpen"),
		RequireDmarc = c.GetValue<bool>("DmarcSettings:RequireDmarc")
	};

	private static MailboxConfigDto MakeMailboxDto(IConfiguration c) => new()
	{
		StoragePath = c["MailboxSettings:StoragePath"] ?? string.Empty
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

	// ── Startup initialisation ──────────────────────────────

	public void EnsureOverrideInitialized()
	{
		var root   = ReadOverride();
		bool dirty = false;

		// Build an appsettings-only config so we always get the canonical defaults,
		// even when the override file already exists but contains empty strings from
		// a previous failed save.
		var src = new ConfigurationBuilder()
			.SetBasePath(env.ContentRootPath)
			.AddJsonFile("appsettings.json", optional: false)
			.Build();

		void TrySeed(string section, string? probe, Func<IConfiguration, object> factory)
		{
			bool empty = probe is null
				? !root.ContainsKey(section)
				: string.IsNullOrEmpty(root[section]?[probe]?.GetValue<string>());
			if (empty)
			{
				root[section] = JsonNode.Parse(JsonSerializer.Serialize(factory(src), Pretty))!;
				dirty = true;
			}
		}

		TrySeed("SmtpSettings",    "EmlStoragePath", c => MakeSmtpDto(c));
		TrySeed("Pop3Settings",    "CertificatePath", c => MakePop3Dto(c));
		TrySeed("ImapSettings",    "CertificatePath", c => MakeImapDto(c));
		TrySeed("DmarcSettings",   null,              c => MakeDmarcDto(c));
		TrySeed("MailboxSettings", "StoragePath",     c => MakeMailboxDto(c));

		if (dirty) File.WriteAllText(OverridePath, root.ToJsonString(Pretty));
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
	public int          MaxConnections        { get; set; }
	public string       WelcomeMessage        { get; set; } = string.Empty;
	public bool         RelayEnabled          { get; set; }
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
	public int    MaxConnections      { get; set; }
	public string WelcomeMessage      { get; set; } = string.Empty;
}

public class ImapConfigDto
{
	public string CertificatePath     { get; set; } = string.Empty;
	public string CertificatePassword { get; set; } = string.Empty;
	public List<PortConfigDto> Ports  { get; set; } = [];
	public int    MaxConnections      { get; set; }
	public string WelcomeMessage      { get; set; } = string.Empty;
	public string PublicFolderName    { get; set; } = "# Public";
	public bool   EnableSort          { get; set; }
	public bool   EnableQuota         { get; set; }
	public bool   EnableIdle          { get; set; }
	public bool   EnableAcl           { get; set; }
	public string HierarchyDelimiter  { get; set; } = ".";
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
