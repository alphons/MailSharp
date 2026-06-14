using System.Text.Json;

namespace MailSharp.WebManager.Services;

public class DomainConfig
{
	public string            Id          { get; set; } = Guid.NewGuid().ToString();
	public string            Name        { get; set; } = string.Empty;
	public bool              Enabled     { get; set; } = true;
	public List<string>      Aliases     { get; set; } = [];
	public List<DomainUser>  Users       { get; set; } = [];
	public List<UserAlias>   UserAliases { get; set; } = [];
	public List<EmailList>   EmailLists  { get; set; } = [];
	public DomainLimits      Limits      { get; set; } = new();
	public DkimConfig        Dkim        { get; set; } = new();
	public string            CatchAll    { get; set; } = string.Empty;
}

public class DomainUser
{
	public string Id         { get; set; } = Guid.NewGuid().ToString();
	public string Username   { get; set; } = string.Empty;   // local part only
	public string Password   { get; set; } = string.Empty;
	public long   MaxSizeMb  { get; set; }
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
			d.Users       = patch.Users;
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

	public bool PatchGeneral(string id, string name, bool enabled, string catchAll)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			if (d is null) return;
			d.Name     = Norm(name);
			d.Enabled  = enabled;
			d.CatchAll = catchAll.Trim();
			found = true;
		});
		return found;
	}

	public bool PatchLimits(string id, DomainLimits limits)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			if (d is null) return;
			d.Limits = limits;
			found = true;
		});
		return found;
	}

	public bool PatchDkim(string id, DkimConfig dkim)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			if (d is null) return;
			d.Dkim = dkim;
			found = true;
		});
		return found;
	}

	public bool AddAlias(string id, string alias)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			if (d is null) return;
			var n = Norm(alias);
			if (!d.Aliases.Contains(n)) d.Aliases.Add(n);
			found = true;
		});
		return found;
	}

	public bool RemoveAlias(string id, string alias)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			if (d is null) return;
			d.Aliases.Remove(Norm(alias));
			found = true;
		});
		return found;
	}

	public bool AddUser(string id, DomainUser user)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			if (d is null) return;
			d.Users.Add(user);
			found = true;
		});
		return found;
	}

	public bool UpdateUser(string id, string userId, DomainUser patch)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			var u = d?.Users.FirstOrDefault(x => x.Id == userId);
			if (u is null) return;
			u.Username  = patch.Username.Trim().ToLowerInvariant();
			u.Password  = patch.Password;
			u.MaxSizeMb = patch.MaxSizeMb;
			found = true;
		});
		return found;
	}

	public bool DeleteUser(string id, string userId)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			if (d is null) return;
			var u = d.Users.FirstOrDefault(x => x.Id == userId);
			if (u is null) return;
			d.Users.Remove(u);
			found = true;
		});
		return found;
	}

	public bool AddUserAlias(string id, UserAlias alias)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			if (d is null) return;
			d.UserAliases.RemoveAll(a => a.Alias == alias.Alias);
			d.UserAliases.Add(alias);
			found = true;
		});
		return found;
	}

	public bool RemoveUserAlias(string id, string alias)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			if (d is null) return;
			d.UserAliases.RemoveAll(a => a.Alias == alias);
			found = true;
		});
		return found;
	}

	public bool AddEmailList(string id, EmailList emailList)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			if (d is null) return;
			d.EmailLists.Add(emailList);
			found = true;
		});
		return found;
	}

	public bool RemoveEmailList(string id, string listId)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			if (d is null) return;
			d.EmailLists.RemoveAll(l => l.Id == listId);
			found = true;
		});
		return found;
	}

	public bool AddListMember(string id, string listId, string member)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			var l = d?.EmailLists.FirstOrDefault(x => x.Id == listId);
			if (l is null) return;
			var m = member.Trim().ToLowerInvariant();
			if (!l.Members.Contains(m)) l.Members.Add(m);
			found = true;
		});
		return found;
	}

	public bool RemoveListMember(string id, string listId, string member)
	{
		bool found = false;
		Mutate(list =>
		{
			var d = list.FirstOrDefault(x => x.Id == id);
			var l = d?.EmailLists.FirstOrDefault(x => x.Id == listId);
			if (l is null) return;
			l.Members.Remove(member.Trim().ToLowerInvariant());
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
