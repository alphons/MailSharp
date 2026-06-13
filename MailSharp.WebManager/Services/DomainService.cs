using System.Text.Json;

namespace MailSharp.WebManager.Services;

public class DomainConfig
{
	public string            Id          { get; set; } = Guid.NewGuid().ToString();
	public string            Name        { get; set; } = string.Empty;
	public bool              Enabled     { get; set; } = true;
	public List<string>      Aliases     { get; set; } = [];
	public List<UserAlias>   UserAliases { get; set; } = [];
	public List<EmailList>   EmailLists  { get; set; } = [];
	public DomainLimits      Limits      { get; set; } = new();
	public DkimConfig        Dkim        { get; set; } = new();
	public string            CatchAll    { get; set; } = string.Empty;
}

public class UserAlias
{
	public string Alias  { get; set; } = string.Empty;
	public string Target { get; set; } = string.Empty;
}

public class EmailList
{
	public string       Id      { get; set; } = Guid.NewGuid().ToString();
	public string       Name    { get; set; } = string.Empty;
	public List<string> Members { get; set; } = [];
}

public class DomainLimits
{
	public long MaxMailboxSizeKb  { get; set; }
	public long AllocatedSizeKb   { get; set; }
	public long MaxMessageSizeKb  { get; set; }
	public long MaxAccountsSizeKb { get; set; }
}

public class DkimConfig
{
	public bool   Enabled       { get; set; }
	public string PrivateKeyFile{ get; set; } = string.Empty;
	public string Selector      { get; set; } = string.Empty;
	public string HeaderMethod  { get; set; } = "Relaxed";
	public string BodyMethod    { get; set; } = "Relaxed";
	public string Algorithm     { get; set; } = "SHA256";
}

public class DomainService(IWebHostEnvironment env)
{
	private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
	private static readonly SemaphoreSlim _lock = new(1, 1);

	private string FilePath => Path.Combine(env.ContentRootPath, "domains.json");

	public List<DomainConfig> GetAll()
	{
		if (!File.Exists(FilePath)) return [];
		try { return JsonSerializer.Deserialize<List<DomainConfig>>(File.ReadAllText(FilePath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? []; }
		catch { return []; }
	}

	public DomainConfig? GetById(string id) => GetAll().FirstOrDefault(d => d.Id == id);

	public DomainConfig Create(string name)
	{
		var domain = new DomainConfig { Name = Norm(name) };
		Mutate(list => list.Add(domain));
		return domain;
	}

	public bool Update(string id, DomainConfig patch)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			if (d is null) return;
			d.Name        = Norm(patch.Name);
			d.Enabled     = patch.Enabled;
			d.Aliases     = patch.Aliases.Select(Norm).Where(a => a.Length > 0).Distinct().ToList();
			d.UserAliases = patch.UserAliases;
			d.EmailLists  = patch.EmailLists;
			d.Limits      = patch.Limits;
			d.Dkim        = patch.Dkim;
			d.CatchAll    = patch.CatchAll.Trim();
			found = true;
		});
		return found;
	}

	public bool Delete(string id)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			if (d is null) return;
			list.Remove(d);
			found = true;
		});
		return found;
	}

	private static string Norm(string s) => s.Trim().ToLowerInvariant();

	private void Mutate(Action<List<DomainConfig>> action)
	{
		_lock.Wait();
		try
		{
			var list = GetAll();
			action(list);
			File.WriteAllText(FilePath, JsonSerializer.Serialize(list, Pretty));
		}
		finally { _lock.Release(); }
	}
}
