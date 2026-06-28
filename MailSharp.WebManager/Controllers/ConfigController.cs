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

	// ── SMTP ────────────────────────────────────────────────────
	[HttpPost("smtp/general")]
	public IActionResult SaveSmtpGeneral([FromBody] SmtpGeneralDto dto)
		{ configService.SaveSmtpGeneral(dto); return Ok(); }

	[HttpPost("smtp/connections")]
	public IActionResult SaveSmtpConnections([FromBody] SmtpConnectionsDto dto)
		{ configService.SaveSmtpConnections(dto); return Ok(); }

	[HttpPost("smtp/delivery")]
	public IActionResult SaveSmtpDelivery([FromBody] SmtpDeliveryDto dto)
		{ configService.SaveSmtpDelivery(dto); return Ok(); }

	[HttpPost("smtp/relay")]
	public IActionResult SaveSmtpRelay([FromBody] SmtpRelayDto dto)
		{ configService.SaveSmtpRelay(dto); return Ok(); }

	[HttpPost("smtp/security")]
	public IActionResult SaveSmtpSecurity([FromBody] SmtpSecurityDto dto)
		{ configService.SaveSmtpSecurity(dto); return Ok(); }

	[HttpPost("smtp/limits")]
	public IActionResult SaveSmtpLimits([FromBody] SmtpLimitsDto dto)
		{ configService.SaveSmtpLimits(dto); return Ok(); }

	// ── POP3 ────────────────────────────────────────────────────
	[HttpPost("pop3/general")]
	public IActionResult SavePop3General([FromBody] Pop3GeneralDto dto)
		{ configService.SavePop3General(dto); return Ok(); }

	[HttpPost("pop3/connections")]
	public IActionResult SavePop3Connections([FromBody] Pop3ConnectionsDto dto)
		{ configService.SavePop3Connections(dto); return Ok(); }

	// ── IMAP ────────────────────────────────────────────────────
	[HttpPost("imap/general")]
	public IActionResult SaveImapGeneral([FromBody] ImapGeneralDto dto)
		{ configService.SaveImapGeneral(dto); return Ok(); }

	[HttpPost("imap/connections")]
	public IActionResult SaveImapConnections([FromBody] ImapConnectionsDto dto)
		{ configService.SaveImapConnections(dto); return Ok(); }

	[HttpPost("imap/folders")]
	public IActionResult SaveImapFolders([FromBody] ImapFoldersDto dto)
		{ configService.SaveImapFolders(dto); return Ok(); }

	[HttpPost("imap/advanced")]
	public IActionResult SaveImapAdvanced([FromBody] ImapAdvancedDto dto)
		{ configService.SaveImapAdvanced(dto); return Ok(); }

	// ── Shared ──────────────────────────────────────────────────
	[HttpPost("dmarc")]
	public IActionResult SaveDmarc([FromBody] DmarcConfigDto dto)
		{ configService.SaveDmarc(dto); return Ok(); }

	[HttpPost("mailbox")]
	public IActionResult SaveMailbox([FromBody] MailboxConfigDto dto)
		{ configService.SaveMailbox(dto); return Ok(); }

	[HttpGet("general")]
	public IActionResult GetGeneral() => Ok(configService.GetGeneral());

	[HttpPost("general")]
	public IActionResult SaveGeneral([FromBody] GeneralSettingsDto dto)
		{ configService.SaveGeneral(dto); return Ok(); }

	[HttpGet("maintenance-users")]
	public IActionResult GetMaintenanceUsers() => Ok(configService.GetMaintenanceUsers());

	[HttpPost("maintenance-users")]
	public IActionResult SaveMaintenanceUsers([FromBody] List<MaintenanceUserDto> dto)
		{ configService.SaveMaintenanceUsers(dto); return Ok(); }

	[HttpGet("ipgroups")]
	public IActionResult GetIpGroups() => Ok(configService.GetIpGroups());

	[HttpPost("ipgroups")]
	public IActionResult SaveIpGroups([FromBody] List<IpGroupDto> dto)
		{ configService.SaveIpGroups(dto); return Ok(); }
}
