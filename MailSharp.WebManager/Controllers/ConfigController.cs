using MailSharp.WebManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.WebManager.Controllers;

[Authorize(Roles = "Administrator")]
[Route("~/api/[controller]")]
[ApiController]
public class ConfigController(ConfigService configService) : ControllerBase
{
	[HttpGet]
	public IActionResult GetAll() => Ok(new
	{
		smtp     = configService.GetSmtp(),
		pop3     = configService.GetPop3(),
		imap     = configService.GetImap(),
		dmarc    = configService.GetDmarc(),
		mailbox  = configService.GetMailbox(),
		ipGroups = configService.GetIpGroups(),
		general  = configService.GetGeneral()
	});

	[HttpPost("smtp")]
	public IActionResult SaveSmtp([FromBody] SmtpConfigDto dto)
	{
		configService.SaveSmtp(dto);
		return Ok();
	}

	[HttpPost("pop3")]
	public IActionResult SavePop3([FromBody] Pop3ConfigDto dto)
	{
		configService.SavePop3(dto);
		return Ok();
	}

	[HttpPost("imap")]
	public IActionResult SaveImap([FromBody] ImapConfigDto dto)
	{
		configService.SaveImap(dto);
		return Ok();
	}

	[HttpPost("dmarc")]
	public IActionResult SaveDmarc([FromBody] DmarcConfigDto dto)
	{
		configService.SaveDmarc(dto);
		return Ok();
	}

	[HttpPost("mailbox")]
	public IActionResult SaveMailbox([FromBody] MailboxConfigDto dto)
	{
		configService.SaveMailbox(dto);
		return Ok();
	}

	[HttpGet("general")]
	public IActionResult GetGeneral() => Ok(configService.GetGeneral());

	[HttpPost("general")]
	public IActionResult SaveGeneral([FromBody] GeneralSettingsDto dto)
	{
		configService.SaveGeneral(dto);
		return Ok();
	}

	[HttpGet("maintenance-users")]
	public IActionResult GetMaintenanceUsers() => Ok(configService.GetMaintenanceUsers());

	[HttpPost("maintenance-users")]
	public IActionResult SaveMaintenanceUsers([FromBody] List<MaintenanceUserDto> dto)
	{
		configService.SaveMaintenanceUsers(dto);
		return Ok();
	}

	[HttpGet("ipgroups")]
	public IActionResult GetIpGroups() => Ok(configService.GetIpGroups());

	[HttpPost("ipgroups")]
	public IActionResult SaveIpGroups([FromBody] List<IpGroupDto> dto)
	{
		configService.SaveIpGroups(dto);
		return Ok();
	}
}
