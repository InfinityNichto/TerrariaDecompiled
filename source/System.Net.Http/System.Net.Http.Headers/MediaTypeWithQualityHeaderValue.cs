using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers;

public sealed class MediaTypeWithQualityHeaderValue : MediaTypeHeaderValue, ICloneable
{
	public double? Quality
	{
		get
		{
			return HeaderUtilities.GetQuality((ObjectCollection<NameValueHeaderValue>)base.Parameters);
		}
		set
		{
			HeaderUtilities.SetQuality((ObjectCollection<NameValueHeaderValue>)base.Parameters, value);
		}
	}

	internal MediaTypeWithQualityHeaderValue()
	{
	}

	public MediaTypeWithQualityHeaderValue(string mediaType)
		: base(mediaType)
	{
	}

	public MediaTypeWithQualityHeaderValue(string mediaType, double quality)
		: base(mediaType)
	{
		Quality = quality;
	}

	private MediaTypeWithQualityHeaderValue(MediaTypeWithQualityHeaderValue source)
		: base(source)
	{
	}

	object ICloneable.Clone()
	{
		return new MediaTypeWithQualityHeaderValue(this);
	}

	public new static MediaTypeWithQualityHeaderValue Parse(string? input)
	{
		int index = 0;
		return (MediaTypeWithQualityHeaderValue)MediaTypeHeaderParser.SingleValueWithQualityParser.ParseValue(input, null, ref index);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out MediaTypeWithQualityHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (MediaTypeHeaderParser.SingleValueWithQualityParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (MediaTypeWithQualityHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}
}
