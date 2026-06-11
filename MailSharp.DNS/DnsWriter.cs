namespace MailSharp.DNS;

public static class DnsWriter
{
	public static int WriteDomainName(Span<byte> destination, string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			destination[0] = 0;
			return 1;
		}

		ReadOnlySpan<char> chars = name.AsSpan();
		bool needsTrailingDot = chars[chars.Length - 1] != '.';

		int totalLength = chars.Length + (needsTrailingDot ? 1 : 0);
		if (totalLength > 255)
			throw new ArgumentException("DNS name too long");

		int pos = 0;
		int start = 0;

		// Loop tot einde van string (exclusief eventuele toe te voegen punt)
		int end = chars.Length;
		while (true)
		{
			int nextDot = chars.Slice(start).IndexOf('.');
			if (nextDot == -1)
				break;

			int labelLength = nextDot;
			if (labelLength > 63)
				throw new ArgumentException("DNS label too long");

			destination[pos++] = (byte)labelLength;
			for (int i = 0; i < labelLength; i++)
				destination[pos++] = (byte)chars[start + i];

			start += nextDot + 1;
		}

		// Laatste label
		int lastLabelLength = end - start;
		if (lastLabelLength > 63)
			throw new ArgumentException("DNS label too long");

		destination[pos++] = (byte)lastLabelLength;
		for (int i = 0; i < lastLabelLength; i++)
			destination[pos++] = (byte)chars[start + i];

		// Voeg punt toe als nodig
		if (needsTrailingDot)
		{
			destination[pos++] = 0; // leeg label = root
		}
		else
		{
			// Als het al eindigt met '.', dan is laatste label leeg → schrijf 0
			if (lastLabelLength == 0)
				destination[pos - 1] = 0;
			else
				destination[pos++] = 0;
		}

		return pos;
	}

	public static int MeasureDomainName(string name)
	{
		if (string.IsNullOrEmpty(name)) return 1; // alleen root label (0)

		ReadOnlySpan<char> span = name.AsSpan();
		int total = 0;
		int start = 0;

		for (int i = 0; i <= span.Length; i++)
		{
			if (i == span.Length || span[i] == '.')
			{
				int labelLen = i - start;
				if (labelLen > 63) throw new ArgumentException("Label too long");
				total += 1 + labelLen; // length byte + data
				start = i + 1;
			}
		}

		total += 1; // null terminator
		return total;
	}
}

