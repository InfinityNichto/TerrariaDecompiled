using System.Diagnostics.CodeAnalysis;

namespace System.Text;

internal sealed class InternalEncoderBestFitFallback : EncoderFallback
{
	internal BaseCodePageEncoding encoding;

	internal char[] arrayBestFit;

	public override int MaxCharCount => 1;

	internal InternalEncoderBestFitFallback(BaseCodePageEncoding _encoding)
	{
		encoding = _encoding;
	}

	public override EncoderFallbackBuffer CreateFallbackBuffer()
	{
		return new InternalEncoderBestFitFallbackBuffer(this);
	}

	public override bool Equals([NotNullWhen(true)] object value)
	{
		if (value is InternalEncoderBestFitFallback internalEncoderBestFitFallback)
		{
			return encoding.CodePage == internalEncoderBestFitFallback.encoding.CodePage;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return encoding.CodePage;
	}
}
