using MailSharp.WebManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.WebManager.Controllers;

[Authorize(Roles = "Administrator")]
[Route("~/api/[controller]")]
[ApiController]
public class DomainController(DomainService domainService, IConfiguration configuration) : ControllerBase
{
	[HttpGet]
	public IActionResult GetAll() => Ok(domainService.GetAll());

	[HttpGet("{id}")]
	public IActionResult GetById(string id)
	{
		var d = domainService.GetById(id);
		return d is null ? NotFound() : Ok(d);
	}

	[HttpPost]
	public IActionResult Create([FromBody] CreateDomainDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Name))
			return BadRequest("Name is required.");
		return Ok(domainService.Create(dto.Name));
	}

	[HttpDelete("{id}")]
	public IActionResult Delete(string id) =>
		domainService.Delete(id) ? Ok() : NotFound();

	// ── General ────────────────────────────────────────────────

	[HttpPut("{id}/general")]
	public IActionResult UpdateGeneral(string id, [FromBody] GeneralDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required.");
		return domainService.PatchGeneral(id, dto.Name, dto.Enabled, dto.CatchAll ?? "") ? Ok() : NotFound();
	}

	// ── Limits ─────────────────────────────────────────────────

	[HttpPut("{id}/limits")]
	public IActionResult UpdateLimits(string id, [FromBody] DomainLimits dto) =>
		domainService.PatchLimits(id, dto) ? Ok() : NotFound();

	// ── DKIM ───────────────────────────────────────────────────

	[HttpPut("{id}/dkim")]
	public IActionResult UpdateDkim(string id, [FromBody] DkimConfig dto) =>
		domainService.PatchDkim(id, dto) ? Ok() : NotFound();

	// ── Domain aliases ─────────────────────────────────────────

	[HttpPost("{id}/aliases")]
	public IActionResult AddAlias(string id, [FromBody] AliasValueDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Value)) return BadRequest("Alias is required.");
		return domainService.AddAlias(id, dto.Value) ? Ok() : NotFound();
	}

	[HttpDelete("{id}/aliases/{alias}")]
	public IActionResult RemoveAlias(string id, string alias) =>
		domainService.RemoveAlias(id, alias) ? Ok() : NotFound();

	// ── Users ──────────────────────────────────────────────────

	[HttpPost("{id}/users")]
	public IActionResult AddUser(string id, [FromBody] DomainUser dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Username)) return BadRequest("Username is required.");
		if (string.IsNullOrWhiteSpace(dto.Password)) return BadRequest("Password is required.");
		dto.Id = Guid.NewGuid().ToString();
		return domainService.AddUser(id, dto) ? Ok(dto) : NotFound();
	}

	[HttpPut("{id}/users/{userId}")]
	public IActionResult UpdateUser(string id, string userId, [FromBody] DomainUser dto) =>
		domainService.UpdateUser(id, userId, dto) ? Ok() : NotFound();

	[HttpDelete("{id}/users/{userId}")]
	public IActionResult DeleteUser(string id, string userId) =>
		domainService.DeleteUser(id, userId) ? Ok() : NotFound();

	// ── User aliases ───────────────────────────────────────────

	[HttpPost("{id}/useraliases")]
	public IActionResult AddUserAlias(string id, [FromBody] UserAlias dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Alias))  return BadRequest("Alias is required.");
		if (string.IsNullOrWhiteSpace(dto.Target)) return BadRequest("Target is required.");
		return domainService.AddUserAlias(id, dto) ? Ok() : NotFound();
	}

	[HttpDelete("{id}/useraliases/{alias}")]
	public IActionResult RemoveUserAlias(string id, string alias) =>
		domainService.RemoveUserAlias(id, alias) ? Ok() : NotFound();

	// ── Email lists ────────────────────────────────────────────

	[HttpPost("{id}/lists")]
	public IActionResult AddList(string id, [FromBody] ListNameDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required.");
		var list = new EmailList { Name = dto.Name.Trim().ToLowerInvariant() };
		return domainService.AddEmailList(id, list) ? Ok(list) : NotFound();
	}

	[HttpDelete("{id}/lists/{listId}")]
	public IActionResult RemoveList(string id, string listId) =>
		domainService.RemoveEmailList(id, listId) ? Ok() : NotFound();

	[HttpPost("{id}/lists/{listId}/members")]
	public IActionResult AddMember(string id, string listId, [FromBody] AliasValueDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Value)) return BadRequest("Member is required.");
		return domainService.AddListMember(id, listId, dto.Value) ? Ok() : NotFound();
	}

	[HttpDelete("{id}/lists/{listId}/members/{member}")]
	public IActionResult RemoveMember(string id, string listId, string member) =>
		domainService.RemoveListMember(id, listId, member) ? Ok() : NotFound();

	// ── User stats ─────────────────────────────────────────────

	[HttpGet("{id}/userstats")]
	public IActionResult UserStats(string id)
	{
		var domain = domainService.GetById(id);
		if (domain is null) return NotFound();

		var storagePath = configuration["MailboxSettings:StoragePath"];
		var stats = domain.Users.Select(u =>
		{
			var address      = $"{u.Username}@{domain.Name}";
			var mailbox      = string.IsNullOrEmpty(storagePath) ? null : Path.Combine(storagePath, address);
			var sizeMb       = 0.0;
			DateTime? lastActivity = null;

			if (mailbox != null && Directory.Exists(mailbox))
			{
				var files = Directory.GetFiles(mailbox, "*.eml", SearchOption.AllDirectories);
				sizeMb = files.Sum(f => new FileInfo(f).Length) / 1_048_576.0;
				if (files.Length > 0)
					lastActivity = files.Select(f => System.IO.File.GetLastWriteTimeUtc(f)).Max();
			}

			return new { userId = u.Id, sizeMb = Math.Round(sizeMb, 2), lastActivity };
		});

		return Ok(stats);
	}
}

public class CreateDomainDto  { public string Name { get; set; } = string.Empty; }
public class GeneralDto       { public string Name { get; set; } = string.Empty; public bool Enabled { get; set; } public string? CatchAll { get; set; } }
public class AliasValueDto    { public string Value { get; set; } = string.Empty; }
public class ListNameDto      { public string Name  { get; set; } = string.Empty; }
