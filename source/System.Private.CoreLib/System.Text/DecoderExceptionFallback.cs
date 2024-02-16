using System.Diagnostics.CodeAnalysis;

namespace System.Text;

public sealed class DecoderExceptionFallback : DecoderFallback
{
	internal static readonly DecoderExceptionFallback s_default = new DecoderExceptionFallback();

	public override int MaxCharCount => 0;

	public override DecoderFallbackBuffer CreateFallbackBuffer()
	{
		return new DecoderExceptionFallbackBuffer();
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		return value is DecoderExceptionFallback;
	}

	public override int GetHashCode()
	{
		return 879;
	}
}
