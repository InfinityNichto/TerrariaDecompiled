namespace System.Text;

internal sealed class EncoderLatin1BestFitFallback : EncoderFallback
{
	internal static readonly EncoderLatin1BestFitFallback SingletonInstance = new EncoderLatin1BestFitFallback();

	public override int MaxCharCount => 1;

	private EncoderLatin1BestFitFallback()
	{
	}

	public override EncoderFallbackBuffer CreateFallbackBuffer()
	{
		return new EncoderLatin1BestFitFallbackBuffer();
	}
}
