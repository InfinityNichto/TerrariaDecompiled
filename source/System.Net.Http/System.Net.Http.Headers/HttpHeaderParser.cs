using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Net.Http.Headers;

internal abstract class HttpHeaderParser
{
	private readonly bool _supportsMultipleValues;

	private readonly string _separator;

	public bool SupportsMultipleValues => _supportsMultipleValues;

	public string Separator => _separator;

	public virtual IEqualityComparer Comparer => null;

	protected HttpHeaderParser(bool supportsMultipleValues)
	{
		_supportsMultipleValues = supportsMultipleValues;
		if (supportsMultipleValues)
		{
			_separator = ", ";
		}
	}

	protected HttpHeaderParser(bool supportsMultipleValues, string separator)
	{
		_supportsMultipleValues = supportsMultipleValues;
		_separator = separator;
	}

	public abstract bool TryParseValue(string value, object storeValue, ref int index, [NotNullWhen(true)] out object parsedValue);

	public object ParseValue(string value, object storeValue, ref int index)
	{
		if (!TryParseValue(value, storeValue, ref index, out var parsedValue))
		{
			throw new FormatException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_invalid_value, (value == null) ? "<null>" : value.Substring(index)));
		}
		return parsedValue;
	}

	public virtual string ToString(object value)
	{
		return value.ToString();
	}
}
