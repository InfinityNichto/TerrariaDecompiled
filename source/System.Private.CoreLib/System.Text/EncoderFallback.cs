namespace System.Text;

public abstract class EncoderFallback
{
	public static EncoderFallback ReplacementFallback => EncoderReplacementFallback.s_default;

	public static EncoderFallback ExceptionFallback => EncoderExceptionFallback.s_default;

	public abstract int MaxCharCount { get; }

	public abstract EncoderFallbackBuffer CreateFallbackBuffer();
}
