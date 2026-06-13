using MailSharp.WebManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.WebManager.Controllers;

[Authorize(Roles = "Administrator")]
[Route("~/api/[controller]")]
[ApiController]
public class DomainController(DomainService domainService) : ControllerBase
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
}

public class CreateDomainDto
{
	public string Name { get; set; } = string.Empty;
}
