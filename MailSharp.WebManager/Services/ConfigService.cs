using System.Text.Json;
using System.Text.Json.Nodes;

namespace MailSharp.WebManager.Services;

public class ConfigService(IWebHostEnvironment env)
{
	private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

	private string OverridePath => Path.Combine(env.ContentRootPath, "mailsharp.json");

	// ── Read ────────────────────────────────────────────────

	public SmtpConfigDto       GetSmtp()     => MakeSmtpDto(BuildMerged());
	public Pop3ConfigDto       GetPop3()     => MakePop3Dto(BuildMerged());
	public ImapConfigDto       GetImap()     => MakeImapDto(BuildMerged());
	public DmarcConfigDto      GetDmarc()    => MakeDmarcDto(BuildMerged());
	public MailboxConfigDto    GetMailbox()  => MakeMailboxDto(BuildMerged());
	public List<IpGroupDto>          GetIpGroups()          => MakeIpGroupsDto(BuildMerged());
	public List<MaintenanceUserDto>  GetMaintenanceUsers()  => MakeMaintenanceUsersDto(BuildMerged());
	public GeneralSettingsDto        GetGeneral()           => MakeGeneralDto(BuildMerged());

	private IConfiguration BuildMerged()
	{
		return new ConfigurationBuilder()
			.SetBasePath(env.ContentRootPath)
			.AddJsonFile("appsettings.json", optional: true)
			.AddJsonFile("mailsharp.json", optional: true)
			.Build();
	}

	private static SmtpConfigDto MakeSmtpDto(IConfiguration c) => new()
	{
		Enabled               = c.GetValue<bool?>("SmtpSettings:Enabled") ?? true,
		EmlStoragePath        = c["SmtpSettings:EmlStoragePath"] ?? string.Empty,
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
		MaxMessageSizeKb      = c.GetValue<int>("SmtpSettings:MaxMessageSizeKb"),
		RetryCount            = c.GetValue<int>("SmtpSettings:RetryCount"),
		RetryIntervalMinutes  = c.GetValue<int>("SmtpSettings:RetryIntervalMinutes"),
		LocalHostName         = c["SmtpSettings:LocalHostName"] ?? string.Empty,
		RelayEnabled          = c.GetValue<bool>("SmtpSettings:RelayEnabled"),
		RelayHost             = c["SmtpSettings:RelayHost"] ?? string.Empty,
		RelayPort             = c.GetValue<int>("SmtpSettings:RelayPort"),
		RelayRequiresAuth     = c.GetValue<bool>("SmtpSettings:RelayRequiresAuth"),
		RelayUsername         = c["SmtpSettings:RelayUsername"] ?? string.Empty,
		RelayPassword         = c["SmtpSettings:RelayPassword"] ?? string.Empty,
		RelayConnectionSecurity = c["SmtpSettings:RelayConnectionSecurity"] ?? "None",
		AllowPlainTextAuth    = c.GetValue<bool>("SmtpSettings:AllowPlainTextAuth"),
		AllowEmptySender      = c.GetValue<bool>("SmtpSettings:AllowEmptySender"),
		AllowBadLineEndings   = c.GetValue<bool>("SmtpSettings:AllowBadLineEndings"),
		DisconnectOnTooManyInvalidCommands = c.GetValue<bool>("SmtpSettings:DisconnectOnTooManyInvalidCommands"),
		MaxInvalidCommands    = c.GetValue<int>("SmtpSettings:MaxInvalidCommands"),
		BindToLocalIp         = c["SmtpSettings:BindToLocalIp"] ?? string.Empty,
		MaxRecipientsPerBatch = c.GetValue<int>("SmtpSettings:MaxRecipientsPerBatch"),
		AddDeliveredToHeader  = c.GetValue<bool>("SmtpSettings:AddDeliveredToHeader"),
		RuleLoopLimit         = c.GetValue<int>("SmtpSettings:RuleLoopLimit"),
		MaxRecipientHosts     = c.GetValue<int>("SmtpSettings:MaxRecipientHosts"),
		RelayQueuePath        = c["SmtpSettings:RelayQueuePath"] ?? string.Empty,
		Ports                 = c.GetSection("SmtpSettings:Ports").Get<List<PortConfigDto>>() ?? []
	};

	private static Pop3ConfigDto MakePop3Dto(IConfiguration c) => new()
	{
		Enabled             = c.GetValue<bool?>("Pop3Settings:Enabled") ?? true,
		CertificatePath     = c["Pop3Settings:CertificatePath"] ?? string.Empty,
		CertificatePassword = c["Pop3Settings:CertificatePassword"] ?? string.Empty,
		Ports               = c.GetSection("Pop3Settings:Ports").Get<List<PortConfigDto>>() ?? [],
		MaxConnections      = c.GetValue<int>("Pop3Settings:MaxConnections"),
		WelcomeMessage      = c["Pop3Settings:WelcomeMessage"] ?? string.Empty
	};

	private static ImapConfigDto MakeImapDto(IConfiguration c) => new()
	{
		Enabled              = c.GetValue<bool?>("ImapSettings:Enabled") ?? true,
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

	private static List<IpGroupDto> MakeIpGroupsDto(IConfiguration c) =>
		c.GetSection("IpGroups").Get<List<IpGroupDto>>() ?? [];

	private static List<MaintenanceUserDto> MakeMaintenanceUsersDto(IConfiguration c) =>
		c.GetSection("MaintenanceUsers").Get<List<MaintenanceUserDto>>() ?? [];

	private static GeneralSettingsDto MakeGeneralDto(IConfiguration c) => new()
	{
		EmlStoragePath       = c["SmtpSettings:EmlStoragePath"]                                    ?? string.Empty,
		CommandTimeoutSeconds = c.GetValue<int>("SmtpSettings:CommandTimeoutSeconds"),
		BackLog              = c.GetValue<int>("SmtpSettings:BackLog"),
		DnsResolvers         = c.GetSection("SmtpSettings:DnsResolvers").Get<List<string>>()       ?? []
	};

	// ── Write ───────────────────────────────────────────────

	public void SaveSmtp(SmtpConfigDto dto)            => PatchSection("SmtpSettings", dto);
	public void SavePop3(Pop3ConfigDto dto)             => PatchSection("Pop3Settings", dto);
	public void SaveImap(ImapConfigDto dto)             => PatchSection("ImapSettings", dto);
	public void SaveDmarc(DmarcConfigDto dto)           => PatchSection("DmarcSettings", dto);
	public void SaveMailbox(MailboxConfigDto dto)       => PatchSection("MailboxSettings", dto);
	public void SaveIpGroups(List<IpGroupDto> dto)                  => PatchSection("IpGroups", dto);
	public void SaveMaintenanceUsers(List<MaintenanceUserDto> dto)  => PatchSection("MaintenanceUsers", dto);
	public void SaveGeneral(GeneralSettingsDto dto)
	{
		// stored inside SmtpSettings so SMTP session code reads the same keys
		var root    = ReadOverride();
		var smtp    = root["SmtpSettings"]?.AsObject() ?? new System.Text.Json.Nodes.JsonObject();
		smtp["EmlStoragePath"]        = dto.EmlStoragePath;
		smtp["CommandTimeoutSeconds"] = dto.CommandTimeoutSeconds;
		smtp["BackLog"]               = dto.BackLog;
		smtp["DnsResolvers"]          = System.Text.Json.Nodes.JsonNode.Parse(
			System.Text.Json.JsonSerializer.Serialize(dto.DnsResolvers, Pretty))!;
		root["SmtpSettings"] = smtp;
		File.WriteAllText(OverridePath, root.ToJsonString(Pretty));
	}

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

		if (!root.ContainsKey("MaintenanceUsers"))
		{
			var users = MakeMaintenanceUsersDto(src);
			root["MaintenanceUsers"] = JsonNode.Parse(JsonSerializer.Serialize(users, Pretty))!;
			dirty = true;
		}

		if (!root.ContainsKey("IpGroups"))
		{
			var groups = MakeIpGroupsDto(src);
			root["IpGroups"] = JsonNode.Parse(JsonSerializer.Serialize(groups, Pretty))!;
			dirty = true;
		}

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
	public bool   Enabled  { get; set; } = true;
}

public class SmtpConfigDto
{
	public bool         Enabled               { get; set; } = true;
	// General
	public string       EmlStoragePath        { get; set; } = string.Empty;
	public int          CommandTimeoutSeconds { get; set; }
	public int          BackLog               { get; set; }
	public string       CertificatePath       { get; set; } = string.Empty;
	public string       CertificatePassword   { get; set; } = string.Empty;
	public string       UserStorePath         { get; set; } = string.Empty;
	public List<string> DnsResolvers          { get; set; } = [];
	public List<string> LocalDomains          { get; set; } = [];
	// Connections
	public int          MaxConnections        { get; set; }
	// Other
	public string       WelcomeMessage        { get; set; } = string.Empty;
	public int          MaxMessageSizeKb      { get; set; }
	// Delivery
	public int          RetryCount            { get; set; }
	public int          RetryIntervalMinutes  { get; set; }
	public string       LocalHostName         { get; set; } = string.Empty;
	// Relay
	public bool         RelayEnabled          { get; set; }
	public string       RelayHost             { get; set; } = string.Empty;
	public int          RelayPort             { get; set; }
	public bool         RelayRequiresAuth     { get; set; }
	public string       RelayUsername         { get; set; } = string.Empty;
	public string       RelayPassword         { get; set; } = string.Empty;
	public string       RelayConnectionSecurity { get; set; } = "None";
	public string       RelayQueuePath        { get; set; } = string.Empty;
	// RFC compliance
	public bool         AllowPlainTextAuth    { get; set; }
	public bool         AllowEmptySender      { get; set; }
	public bool         AllowBadLineEndings   { get; set; }
	public bool         DisconnectOnTooManyInvalidCommands { get; set; }
	public int          MaxInvalidCommands    { get; set; }
	// Advanced
	public string       BindToLocalIp         { get; set; } = string.Empty;
	public int          MaxRecipientsPerBatch { get; set; }
	public bool         AddDeliveredToHeader  { get; set; }
	public int          RuleLoopLimit         { get; set; }
	public int          MaxRecipientHosts     { get; set; }
	// Auth / DKIM
	public bool         EnableAuth            { get; set; }
	public bool         EnableStartTls        { get; set; }
	public bool         EnableVrfy            { get; set; }
	public bool         EnableExpn            { get; set; }
	public bool         RequireDkim           { get; set; }
	public List<PortConfigDto> Ports          { get; set; } = [];
}

public class Pop3ConfigDto
{
	public bool   Enabled             { get; set; } = true;
	public string CertificatePath     { get; set; } = string.Empty;
	public string CertificatePassword { get; set; } = string.Empty;
	public List<PortConfigDto> Ports  { get; set; } = [];
	public int    MaxConnections      { get; set; }
	public string WelcomeMessage      { get; set; } = string.Empty;
}

public class ImapConfigDto
{
	public bool   Enabled             { get; set; } = true;
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

public class IpAccessDto
{
	public bool Smtp                { get; set; }
	public bool Pop3                { get; set; }
	public bool Imap                { get; set; }
	public bool AntiSpam            { get; set; }
	public bool AntiVirus           { get; set; }
	public bool RequireSslTlsForAuth { get; set; }
}

public class EmailFlowDto
{
	public bool Allowed     { get; set; }
	public bool RequireAuth { get; set; }
}

public class EmailFlowsDto
{
	public EmailFlowDto LocalToLocal         { get; set; } = new();
	public EmailFlowDto LocalToExternal      { get; set; } = new();
	public EmailFlowDto ExternalToLocal      { get; set; } = new();
	public EmailFlowDto ExternalToExternal   { get; set; } = new();
}

public class GeneralSettingsDto
{
	public string       EmlStoragePath        { get; set; } = string.Empty;
	public int          CommandTimeoutSeconds  { get; set; }
	public int          BackLog               { get; set; }
	public List<string> DnsResolvers          { get; set; } = [];
}

public class MaintenanceUserDto
{
	public string UserName   { get; set; } = string.Empty;
	public string Password   { get; set; } = string.Empty;
	public string Role       { get; set; } = "Unknown";
	public int    ExpireDays { get; set; } = 365;
	public bool   Enabled    { get; set; } = true;
}

public class IpGroupDto
{
	public string        Name       { get; set; } = string.Empty;
	public int           Priority   { get; set; }
	public string        Cidr       { get; set; } = string.Empty;
	public string?       Expires    { get; set; }
	public IpAccessDto   Access     { get; set; } = new();
	public EmailFlowsDto EmailFlows { get; set; } = new();
}
