namespace MailSharp.SmtpServer.Extensions;

public static class StreamWriterExtensions
{
	public static Task WriteLineAsync(this StreamWriter writer, string? text, CancellationToken cancellationToken) =>
		writer.WriteLineAsync(text?.AsMemory() ?? ReadOnlyMemory<char>.Empty, cancellationToken);

}
