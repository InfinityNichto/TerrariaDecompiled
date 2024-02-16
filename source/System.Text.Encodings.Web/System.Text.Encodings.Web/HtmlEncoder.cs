using System.Text.Unicode;

namespace System.Text.Encodings.Web;

public abstract class HtmlEncoder : TextEncoder
{
	public static HtmlEncoder Default => DefaultHtmlEncoder.BasicLatinSingleton;

	public static HtmlEncoder Create(TextEncoderSettings settings)
	{
		return new DefaultHtmlEncoder(settings);
	}

	public static HtmlEncoder Create(params UnicodeRange[] allowedRanges)
	{
		return new DefaultHtmlEncoder(new TextEncoderSettings(allowedRanges));
	}
}
