using MailSharp.DNS;

namespace MailSharp.DNS;

public sealed class Request
{
	private const int MaxUdpSize = 512;
	private readonly byte[] buffer = new byte[MaxUdpSize];
	private int length = 12; // start na header

	public DnsHeader Header { get; }

	public Request()
	{
        Header = new DnsHeader(buffer)
        {
            OPCODE = OPCode.Query,
            RD = true
        };
    }

	public void AddQuestion(QuestionRecord question)
	{
		// Eerst droog simuleren
		int nameBytes = DnsWriter.MeasureDomainName(question.Name);
		int needed = nameBytes + 4; // + DnsQType + DnsQClass

		if (length + needed > MaxUdpSize)
			throw new InvalidOperationException(
				$"DNS UDP packet would exceed 512 bytes ({length + needed} > {MaxUdpSize})");

		int written = question.WriteTo(buffer.AsSpan(length));
		length += written;
		Header.QDCount++;
	}

	public Memory<byte> Data => buffer.AsMemory(0, length);
}
