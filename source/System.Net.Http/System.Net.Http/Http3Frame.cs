namespace System.Net.Http;

internal static class Http3Frame
{
	public static bool TryReadIntegerPair(ReadOnlySpan<byte> buffer, out long a, out long b, out int bytesRead)
	{
		if (VariableLengthIntegerHelper.TryRead(buffer, out a, out var bytesRead2))
		{
			buffer = buffer.Slice(bytesRead2);
			if (VariableLengthIntegerHelper.TryRead(buffer, out b, out var bytesRead3))
			{
				bytesRead = bytesRead2 + bytesRead3;
				return true;
			}
		}
		b = 0L;
		bytesRead = 0;
		return false;
	}

	public static bool TryWriteFrameEnvelope(Http3FrameType frameType, long payloadLength, Span<byte> buffer, out int bytesWritten)
	{
		if (buffer.Length != 0)
		{
			buffer[0] = (byte)frameType;
			buffer = buffer.Slice(1);
			if (VariableLengthIntegerHelper.TryWrite(buffer, payloadLength, out var bytesWritten2))
			{
				bytesWritten = bytesWritten2 + 1;
				return true;
			}
		}
		bytesWritten = 0;
		return false;
	}
}
