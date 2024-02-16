using System.Diagnostics.CodeAnalysis;

namespace System.Text;

public sealed class EncoderExceptionFallback : EncoderFallback
{
	internal static readonly EncoderExceptionFallback s_default = new EncoderExceptionFallback();

	public override int MaxCharCount => 0;

	public override EncoderFallbackBuffer CreateFallbackBuffer()
	{
		return new EncoderExceptionFallbackBuffer();
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		return value is EncoderExceptionFallback;
	}

	public override int GetHashCode()
	{
		return 654;
	}
}
