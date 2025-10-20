using MailSharp.Common.Services;

namespace MailSharp.Common;

// Message metadata for IMAP
public class Message
{
	public string Uid { get; set; } = string.Empty;
	public MessageFlags Flags { get; set; } = new();
}
