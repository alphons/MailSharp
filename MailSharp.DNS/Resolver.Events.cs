namespace MailSharp.DNS;

public partial class Resolver
{
	/// <summary>
	/// Verbose messages from internal operations
	/// </summary>
	public event VerboseEventHandler? OnVerbose;

	public delegate void VerboseEventHandler(object sender, VerboseEventArgs e);

	public sealed class VerboseEventArgs : EventArgs
	{
		public string Message { get; }

		public VerboseEventArgs(string message)
		{
			Message = message;
		}
	}

	private void Verbose(string format, params object[] args)
	{
		if (OnVerbose is null)
			return;

		OnVerbose(this, new VerboseEventArgs(string.Format(format, args)));
	}
}
