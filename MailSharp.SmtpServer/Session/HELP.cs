using System.Threading.Tasks;

namespace MailSharp.SmtpServer.Session;

public partial class SmtpSession
{
	// Handle HELP command
	private async Task HandleHelpAsync(string[] parts, string line)
	{
		await writer!.WriteLineAsync(configuration["SmtpResponses:Help"]);
	}
}
