using MailSharp.Smtp.Services;
using Microsoft.AspNetCore.Mvc;

namespace MailSharp.WebManager.Controllers;

//public class HomeControllerOld(SmtpServerStatus status) : ControllerBase
//{
//	[HttpGet]
//	public IActionResult Index()
//	{
//		return Ok(new { SmtpRunning = status.IsRunning });
//	}
//}

public class HomeController : Controller
{
	// GET: Home/Index
	public IActionResult Index()
	{
		return View("/wwwroot/Index.cshtml");
	}
}
