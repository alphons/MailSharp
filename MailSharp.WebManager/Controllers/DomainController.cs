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

	[HttpPut("{id}")]
	public IActionResult Update(string id, [FromBody] DomainConfig dto)
	{
		if (string.IsNullOrWhiteSpace(dto.Name))
			return BadRequest("Name is required.");
		return domainService.Update(id, dto) ? Ok() : NotFound();
	}

	[HttpDelete("{id}")]
	public IActionResult Delete(string id) =>
		domainService.Delete(id) ? Ok() : NotFound();

	// Returns live stats (mailbox size + last activity) for every user in a domain
	[HttpGet("{id}/userstats")]
	public IActionResult UserStats(string id)
	{
		var domain = domainService.GetById(id);
		if (domain is null) return NotFound();

		var storagePath = configuration["MailboxSettings:StoragePath"];
		var stats = domain.Users.Select(u =>
		{
			var address  = $"{u.Username}@{domain.Name}";
			var mailbox  = string.IsNullOrEmpty(storagePath) ? null : Path.Combine(storagePath, address);
			var sizeMb   = 0.0;
			DateTime? lastActivity = null;

			if (mailbox != null && Directory.Exists(mailbox))
			{
				var files = Directory.GetFiles(mailbox, "*.eml", SearchOption.AllDirectories);
				sizeMb = files.Sum(f => new FileInfo(f).Length) / 1_048_576.0;
				if (files.Length > 0)
					lastActivity = files.Select(f => System.IO.File.GetLastWriteTimeUtc(f)).Max();
			}

			return new
			{
				userId       = u.Id,
				sizeMb       = Math.Round(sizeMb, 2),
				lastActivity
			};
		});

		return Ok(stats);
	}
}

public class CreateDomainDto
{
	public string Name { get; set; } = string.Empty;
}
