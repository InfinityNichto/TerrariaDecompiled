using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers;

internal sealed class ByteArrayHeaderParser : HttpHeaderParser
{
	internal static readonly ByteArrayHeaderParser Parser = new ByteArrayHeaderParser();

	private ByteArrayHeaderParser()
		: base(supportsMultipleValues: false)
	{
	}

	public override string ToString(object value)
	{
		return Convert.ToBase64String((byte[])value);
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
		try
		{
			parsedValue = Convert.FromBase64String(text);
			index = value.Length;
			return true;
		}
		catch (FormatException ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, System.SR.Format(System.SR.net_http_parser_invalid_base64_string, text, ex.Message), "TryParseValue");
			}
		}
		return false;
	}
}
