namespace System.Text;

public abstract class DecoderFallback
{
	public static DecoderFallback ReplacementFallback => DecoderReplacementFallback.s_default;

	public static DecoderFallback ExceptionFallback => DecoderExceptionFallback.s_default;

	public abstract int MaxCharCount { get; }

	public abstract DecoderFallbackBuffer CreateFallbackBuffer();
}
