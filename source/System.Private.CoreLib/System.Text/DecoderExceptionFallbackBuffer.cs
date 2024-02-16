using System.Diagnostics.CodeAnalysis;

namespace System.Text;

public sealed class DecoderExceptionFallbackBuffer : DecoderFallbackBuffer
{
	public override int Remaining => 0;

	public override bool Fallback(byte[] bytesUnknown, int index)
	{
		Throw(bytesUnknown, index);
		return true;
	}

	public override char GetNextChar()
	{
		return '\0';
	}

	public override bool MovePrevious()
	{
		return false;
	}

	[DoesNotReturn]
	private static void Throw(byte[] bytesUnknown, int index)
	{
		if (bytesUnknown == null)
		{
			bytesUnknown = Array.Empty<byte>();
		}
		StringBuilder stringBuilder = new StringBuilder(bytesUnknown.Length * 4);
		for (int i = 0; i < bytesUnknown.Length && i < 20; i++)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, stringBuilder2);
			handler.AppendLiteral("[");
			handler.AppendFormatted(bytesUnknown[i], "X2");
			handler.AppendLiteral("]");
			stringBuilder2.Append(ref handler);
		}
		if (bytesUnknown.Length > 20)
		{
			stringBuilder.Append(" ...");
		}
		throw new DecoderFallbackException(SR.Format(SR.Argument_InvalidCodePageBytesIndex, stringBuilder, index), bytesUnknown, index);
	}
}
