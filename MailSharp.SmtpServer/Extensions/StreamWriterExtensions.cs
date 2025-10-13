namespace MailSharp.Smtp.Extensions;

public static class StreamWriterExtensions
{
	// Did someone forgot this extension method in the BCL?
	public static Task WriteLineAsync(this StreamWriter writer, string? text, CancellationToken cancellationToken) =>
		writer.WriteLineAsync(text?.AsMemory() ?? ReadOnlyMemory<char>.Empty, cancellationToken);

}
