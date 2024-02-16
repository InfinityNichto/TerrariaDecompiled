using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers;

public class NameValueHeaderValue : ICloneable
{
	private static readonly Func<NameValueHeaderValue> s_defaultNameValueCreator = CreateNameValue;

	private string _name;

	private string _value;

	public string Name => _name;

	public string? Value
	{
		get
		{
			return _value;
		}
		set
		{
			CheckValueFormat(value);
			_value = value;
		}
	}

	internal NameValueHeaderValue()
	{
	}

	public NameValueHeaderValue(string name)
		: this(name, null)
	{
	}

	public NameValueHeaderValue(string name, string? value)
	{
		CheckNameValueFormat(name, value);
		_name = name;
		_value = value;
	}

	protected internal NameValueHeaderValue(NameValueHeaderValue source)
	{
		_name = source._name;
		_value = source._value;
	}

	public override int GetHashCode()
	{
		int hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(_name);
		if (!string.IsNullOrEmpty(_value))
		{
			if (_value[0] == '"')
			{
				return hashCode ^ _value.GetHashCode();
			}
			return hashCode ^ StringComparer.OrdinalIgnoreCase.GetHashCode(_value);
		}
		return hashCode;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is NameValueHeaderValue nameValueHeaderValue))
		{
			return false;
		}
		if (!string.Equals(_name, nameValueHeaderValue._name, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (string.IsNullOrEmpty(_value))
		{
			return string.IsNullOrEmpty(nameValueHeaderValue._value);
		}
		if (_value[0] == '"')
		{
			return string.Equals(_value, nameValueHeaderValue._value, StringComparison.Ordinal);
		}
		return string.Equals(_value, nameValueHeaderValue._value, StringComparison.OrdinalIgnoreCase);
	}

	public static NameValueHeaderValue Parse(string? input)
	{
		int index = 0;
		return (NameValueHeaderValue)GenericHeaderParser.SingleValueNameValueParser.ParseValue(input, null, ref index);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out NameValueHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (GenericHeaderParser.SingleValueNameValueParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (NameValueHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	public override string ToString()
	{
		if (!string.IsNullOrEmpty(_value))
		{
			return _name + "=" + _value;
		}
		return _name;
	}

	private void AddToStringBuilder(StringBuilder sb)
	{
		if (GetType() != typeof(NameValueHeaderValue))
		{
			sb.Append(ToString());
			return;
		}
		sb.Append(_name);
		if (!string.IsNullOrEmpty(_value))
		{
			sb.Append('=');
			sb.Append(_value);
		}
	}

	internal static void ToString(ObjectCollection<NameValueHeaderValue> values, char separator, bool leadingSeparator, StringBuilder destination)
	{
		if (values == null || values.Count == 0)
		{
			return;
		}
		foreach (NameValueHeaderValue value in values)
		{
			if (leadingSeparator || destination.Length > 0)
			{
				destination.Append(separator);
				destination.Append(' ');
			}
			value.AddToStringBuilder(destination);
		}
	}

	internal static int GetHashCode(ObjectCollection<NameValueHeaderValue> values)
	{
		if (values == null || values.Count == 0)
		{
			return 0;
		}
		int num = 0;
		foreach (NameValueHeaderValue value in values)
		{
			num ^= value.GetHashCode();
		}
		return num;
	}

	internal static int GetNameValueLength(string input, int startIndex, out NameValueHeaderValue parsedValue)
	{
		return GetNameValueLength(input, startIndex, s_defaultNameValueCreator, out parsedValue);
	}

	internal static int GetNameValueLength(string input, int startIndex, Func<NameValueHeaderValue> nameValueCreator, out NameValueHeaderValue parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int tokenLength = HttpRuleParser.GetTokenLength(input, startIndex);
		if (tokenLength == 0)
		{
			return 0;
		}
		string name = input.Substring(startIndex, tokenLength);
		int num = startIndex + tokenLength;
		num += HttpRuleParser.GetWhitespaceLength(input, num);
		if (num == input.Length || input[num] != '=')
		{
			parsedValue = nameValueCreator();
			parsedValue._name = name;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			return num - startIndex;
		}
		num++;
		num += HttpRuleParser.GetWhitespaceLength(input, num);
		int valueLength = GetValueLength(input, num);
		if (valueLength == 0)
		{
			return 0;
		}
		parsedValue = nameValueCreator();
		parsedValue._name = name;
		parsedValue._value = input.Substring(num, valueLength);
		num += valueLength;
		num += HttpRuleParser.GetWhitespaceLength(input, num);
		return num - startIndex;
	}

	internal static int GetNameValueListLength(string input, int startIndex, char delimiter, ObjectCollection<NameValueHeaderValue> nameValueCollection)
	{
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int num = startIndex + HttpRuleParser.GetWhitespaceLength(input, startIndex);
		while (true)
		{
			NameValueHeaderValue parsedValue;
			int nameValueLength = GetNameValueLength(input, num, s_defaultNameValueCreator, out parsedValue);
			if (nameValueLength == 0)
			{
				return 0;
			}
			nameValueCollection.Add(parsedValue);
			num += nameValueLength;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
			if (num == input.Length || input[num] != delimiter)
			{
				break;
			}
			num++;
			num += HttpRuleParser.GetWhitespaceLength(input, num);
		}
		return num - startIndex;
	}

	internal static NameValueHeaderValue Find(ObjectCollection<NameValueHeaderValue> values, string name)
	{
		if (values == null || values.Count == 0)
		{
			return null;
		}
		foreach (NameValueHeaderValue value in values)
		{
			if (string.Equals(value.Name, name, StringComparison.OrdinalIgnoreCase))
			{
				return value;
			}
		}
		return null;
	}

	internal static int GetValueLength(string input, int startIndex)
	{
		if (startIndex >= input.Length)
		{
			return 0;
		}
		int length = HttpRuleParser.GetTokenLength(input, startIndex);
		if (length == 0 && HttpRuleParser.GetQuotedStringLength(input, startIndex, out length) != 0)
		{
			return 0;
		}
		return length;
	}

	private static void CheckNameValueFormat(string name, string value)
	{
		HeaderUtilities.CheckValidToken(name, "name");
		CheckValueFormat(value);
	}

	private static void CheckValueFormat(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return;
		}
		if (value[0] == ' ' || value[0] == '\t' || value[^1] == ' ' || value[^1] == '\t')
		{
			ThrowFormatException(value);
		}
		if (value[0] == '"')
		{
			if (HttpRuleParser.GetQuotedStringLength(value, 0, out var length) != 0 || length != value.Length)
			{
				ThrowFormatException(value);
			}
		}
		else if (HttpRuleParser.ContainsNewLine(value))
		{
			ThrowFormatException(value);
		}
		static void ThrowFormatException(string value)
		{
			throw new FormatException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_invalid_value, value));
		}
	}

	private static NameValueHeaderValue CreateNameValue()
	{
		return new NameValueHeaderValue();
	}

	object ICloneable.Clone()
	{
		return new NameValueHeaderValue(this);
	}
}
