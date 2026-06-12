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
		smtp    = configService.GetSmtp(),
		pop3    = configService.GetPop3(),
		imap    = configService.GetImap(),
		dmarc   = configService.GetDmarc(),
		mailbox = configService.GetMailbox()
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
}
