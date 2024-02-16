using System.Text.Unicode;

namespace System.Text.Encodings.Web;

public abstract class JavaScriptEncoder : TextEncoder
{
	public static JavaScriptEncoder Default => DefaultJavaScriptEncoder.BasicLatinSingleton;

	public static JavaScriptEncoder UnsafeRelaxedJsonEscaping => DefaultJavaScriptEncoder.UnsafeRelaxedEscapingSingleton;

	public static JavaScriptEncoder Create(TextEncoderSettings settings)
	{
		return new DefaultJavaScriptEncoder(settings);
	}

	public static JavaScriptEncoder Create(params UnicodeRange[] allowedRanges)
	{
		return new DefaultJavaScriptEncoder(new TextEncoderSettings(allowedRanges));
	}
}
