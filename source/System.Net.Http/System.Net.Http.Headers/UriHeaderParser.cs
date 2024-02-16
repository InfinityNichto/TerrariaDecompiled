using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Net.Http.Headers;

internal sealed class UriHeaderParser : HttpHeaderParser
{
	private readonly UriKind _uriKind;

	internal static readonly UriHeaderParser RelativeOrAbsoluteUriParser = new UriHeaderParser(UriKind.RelativeOrAbsolute);

	private UriHeaderParser(UriKind uriKind)
		: base(supportsMultipleValues: false)
	{
		_uriKind = uriKind;
	}

	public override bool TryParseValue([NotNullWhen(true)] string value, object storeValue, ref int index, [NotNullWhen(true)] out object parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(value) || index == value.Length)
		{
			return false;
		}
		string text = value;
		if (index > 0)
		{
			text = value.Substring(index);
		}
		if (!Uri.TryCreate(text, _uriKind, out Uri result))
		{
			text = DecodeUtf8FromString(text);
			if (!Uri.TryCreate(text, _uriKind, out result))
			{
				return false;
			}
		}
		index = value.Length;
		parsedValue = result;
		return true;
	}

	internal static string DecodeUtf8FromString(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return input;
		}
		bool flag = false;
		for (int i = 0; i < input.Length; i++)
		{
			if (input[i] > 'ÿ')
			{
				return input;
			}
			if (input[i] > '\u007f')
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			byte[] array = new byte[input.Length];
			for (int j = 0; j < input.Length; j++)
			{
				if (input[j] > 'ÿ')
				{
					return input;
				}
				array[j] = (byte)input[j];
			}
			try
			{
				Encoding encoding = Encoding.GetEncoding("utf-8", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
				return encoding.GetString(array, 0, array.Length);
			}
			catch (ArgumentException)
			{
			}
		}
		return input;
	}

	public override string ToString(object value)
	{
		Uri uri = (Uri)value;
		if (uri.IsAbsoluteUri)
		{
			return uri.AbsoluteUri;
		}
		return uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
	}
}
