using MailSharp.WebManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.WebManager.Controllers;

[Authorize(Roles = "Administrator")]
[Route("~/api/[controller]")]
[ApiController]
public class StatusController(ServerStatusService statusService) : ControllerBase
{
	[HttpGet("all")]
	public IActionResult StatusAll()
	{
		return Ok(new
		{
			statusService.IsSmtpRunning,
			statusService.IsImapRunning,
			statusService.IsPop3Running
		});
	}
}
