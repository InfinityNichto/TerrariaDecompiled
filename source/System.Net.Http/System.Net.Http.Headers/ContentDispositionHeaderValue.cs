using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers;

public class ContentDispositionHeaderValue : ICloneable
{
	private ObjectCollection<NameValueHeaderValue> _parameters;

	private string _dispositionType;

	public string DispositionType
	{
		get
		{
			return _dispositionType;
		}
		set
		{
			CheckDispositionTypeFormat(value, "value");
			_dispositionType = value;
		}
	}

	public ICollection<NameValueHeaderValue> Parameters => _parameters ?? (_parameters = new ObjectCollection<NameValueHeaderValue>());

	public string? Name
	{
		get
		{
			return GetName("name");
		}
		set
		{
			SetName("name", value);
		}
	}

	public string? FileName
	{
		get
		{
			return GetName("filename");
		}
		set
		{
			SetName("filename", value);
		}
	}

	public string? FileNameStar
	{
		get
		{
			return GetName("filename*");
		}
		set
		{
			SetName("filename*", value);
		}
	}

	public DateTimeOffset? CreationDate
	{
		get
		{
			return GetDate("creation-date");
		}
		set
		{
			SetDate("creation-date", value);
		}
	}

	public DateTimeOffset? ModificationDate
	{
		get
		{
			return GetDate("modification-date");
		}
		set
		{
			SetDate("modification-date", value);
		}
	}

	public DateTimeOffset? ReadDate
	{
		get
		{
			return GetDate("read-date");
		}
		set
		{
			SetDate("read-date", value);
		}
	}

	public long? Size
	{
		get
		{
			NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(_parameters, "size");
			if (nameValueHeaderValue != null)
			{
				string value = nameValueHeaderValue.Value;
				if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
				{
					return (long)result;
				}
			}
			return null;
		}
		set
		{
			NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(_parameters, "size");
			if (!value.HasValue)
			{
				if (nameValueHeaderValue != null)
				{
					_parameters.Remove(nameValueHeaderValue);
				}
				return;
			}
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			if (nameValueHeaderValue != null)
			{
				nameValueHeaderValue.Value = value.Value.ToString(CultureInfo.InvariantCulture);
				return;
			}
			string value2 = value.Value.ToString(CultureInfo.InvariantCulture);
			Parameters.Add(new NameValueHeaderValue("size", value2));
		}
	}

	internal ContentDispositionHeaderValue()
	{
	}

	protected ContentDispositionHeaderValue(ContentDispositionHeaderValue source)
	{
		_dispositionType = source._dispositionType;
		_parameters = source._parameters.Clone();
	}

	public ContentDispositionHeaderValue(string dispositionType)
	{
		CheckDispositionTypeFormat(dispositionType, "dispositionType");
		_dispositionType = dispositionType;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire();
		stringBuilder.Append(_dispositionType);
		NameValueHeaderValue.ToString(_parameters, ';', leadingSeparator: true, stringBuilder);
		return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is ContentDispositionHeaderValue contentDispositionHeaderValue))
		{
			return false;
		}
		if (string.Equals(_dispositionType, contentDispositionHeaderValue._dispositionType, StringComparison.OrdinalIgnoreCase))
		{
			return HeaderUtilities.AreEqualCollections(_parameters, contentDispositionHeaderValue._parameters);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return StringComparer.OrdinalIgnoreCase.GetHashCode(_dispositionType) ^ NameValueHeaderValue.GetHashCode(_parameters);
	}

	object ICloneable.Clone()
	{
		return new ContentDispositionHeaderValue(this);
	}

	public static ContentDispositionHeaderValue Parse(string? input)
	{
		int index = 0;
		return (ContentDispositionHeaderValue)GenericHeaderParser.ContentDispositionParser.ParseValue(input, null, ref index);
	}

	public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out ContentDispositionHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (GenericHeaderParser.ContentDispositionParser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (ContentDispositionHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	internal static int GetDispositionTypeLength(string input, int startIndex, out object parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		string dispositionType;
		int dispositionTypeExpressionLength = GetDispositionTypeExpressionLength(input, startIndex, out dispositionType);
		if (dispositionTypeExpressionLength == 0)
		{
			return 0;
		}
		int num = startIndex + dispositionTypeExpressionLength;
		num += HttpRuleParser.GetWhitespaceLength(input, num);
		ContentDispositionHeaderValue contentDispositionHeaderValue = new ContentDispositionHeaderValue();
		contentDispositionHeaderValue._dispositionType = dispositionType;
		if (num < input.Length && input[num] == ';')
		{
			num++;
			int nameValueListLength = NameValueHeaderValue.GetNameValueListLength(input, num, ';', (ObjectCollection<NameValueHeaderValue>)contentDispositionHeaderValue.Parameters);
			if (nameValueListLength == 0)
			{
				return 0;
			}
			parsedValue = contentDispositionHeaderValue;
			return num + nameValueListLength - startIndex;
		}
		parsedValue = contentDispositionHeaderValue;
		return num - startIndex;
	}

	private static int GetDispositionTypeExpressionLength(string input, int startIndex, out string dispositionType)
	{
		dispositionType = null;
		int tokenLength = HttpRuleParser.GetTokenLength(input, startIndex);
		if (tokenLength == 0)
		{
			return 0;
		}
		dispositionType = input.Substring(startIndex, tokenLength);
		return tokenLength;
	}

	private static void CheckDispositionTypeFormat(string dispositionType, string parameterName)
	{
		if (string.IsNullOrEmpty(dispositionType))
		{
			throw new ArgumentException(System.SR.net_http_argument_empty_string, parameterName);
		}
		if (GetDispositionTypeExpressionLength(dispositionType, 0, out var dispositionType2) == 0 || dispositionType2.Length != dispositionType.Length)
		{
			throw new FormatException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_invalid_value, dispositionType));
		}
	}

	private DateTimeOffset? GetDate(string parameter)
	{
		NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(_parameters, parameter);
		if (nameValueHeaderValue != null)
		{
			ReadOnlySpan<char> readOnlySpan = nameValueHeaderValue.Value;
			if (IsQuoted(readOnlySpan))
			{
				readOnlySpan = readOnlySpan.Slice(1, readOnlySpan.Length - 2);
			}
			if (HttpDateParser.TryParse(readOnlySpan, out var result))
			{
				return result;
			}
		}
		return null;
	}

	private void SetDate(string parameter, DateTimeOffset? date)
	{
		NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(_parameters, parameter);
		if (!date.HasValue)
		{
			if (nameValueHeaderValue != null)
			{
				_parameters.Remove(nameValueHeaderValue);
			}
			return;
		}
		string value = "\"" + HttpDateParser.DateToString(date.Value) + "\"";
		if (nameValueHeaderValue != null)
		{
			nameValueHeaderValue.Value = value;
		}
		else
		{
			Parameters.Add(new NameValueHeaderValue(parameter, value));
		}
	}

	private string GetName(string parameter)
	{
		NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(_parameters, parameter);
		if (nameValueHeaderValue != null)
		{
			string output;
			if (parameter.EndsWith('*'))
			{
				if (TryDecode5987(nameValueHeaderValue.Value, out output))
				{
					return output;
				}
				return null;
			}
			if (TryDecodeMime(nameValueHeaderValue.Value, out output))
			{
				return output;
			}
			return nameValueHeaderValue.Value;
		}
		return null;
	}

	private void SetName(string parameter, string value)
	{
		NameValueHeaderValue nameValueHeaderValue = NameValueHeaderValue.Find(_parameters, parameter);
		if (string.IsNullOrEmpty(value))
		{
			if (nameValueHeaderValue != null)
			{
				_parameters.Remove(nameValueHeaderValue);
			}
			return;
		}
		string empty = string.Empty;
		empty = ((!parameter.EndsWith('*')) ? EncodeAndQuoteMime(value) : HeaderUtilities.Encode5987(value));
		if (nameValueHeaderValue != null)
		{
			nameValueHeaderValue.Value = empty;
		}
		else
		{
			Parameters.Add(new NameValueHeaderValue(parameter, empty));
		}
	}

	private static string EncodeAndQuoteMime(string input)
	{
		string text = input;
		bool flag = false;
		if (IsQuoted(text))
		{
			text = text.Substring(1, text.Length - 2);
			flag = true;
		}
		if (text.Contains('"'))
		{
			throw new ArgumentException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_headers_invalid_value, input));
		}
		if (HeaderUtilities.ContainsNonAscii(text))
		{
			flag = true;
			text = EncodeMime(text);
		}
		else if (!flag && HttpRuleParser.GetTokenLength(text, 0) != text.Length)
		{
			flag = true;
		}
		if (flag)
		{
			text = "\"" + text + "\"";
		}
		return text;
	}

	private static bool IsQuoted(ReadOnlySpan<char> value)
	{
		if (value.Length > 1 && value[0] == '"')
		{
			return value[value.Length - 1] == '"';
		}
		return false;
	}

	private static string EncodeMime(string input)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(input);
		string text = Convert.ToBase64String(bytes);
		return "=?utf-8?B?" + text + "?=";
	}

	private static bool TryDecodeMime(string input, [NotNullWhen(true)] out string output)
	{
		output = null;
		if (!IsQuoted(input) || input.Length < 10)
		{
			return false;
		}
		string[] array = input.Split('?');
		if (array.Length != 5 || array[0] != "\"=" || array[4] != "=\"" || array[2].ToLowerInvariant() != "b")
		{
			return false;
		}
		try
		{
			Encoding encoding = Encoding.GetEncoding(array[1]);
			byte[] array2 = Convert.FromBase64String(array[3]);
			output = encoding.GetString(array2, 0, array2.Length);
			return true;
		}
		catch (ArgumentException)
		{
		}
		catch (FormatException)
		{
		}
		return false;
	}

	private static bool TryDecode5987(string input, out string output)
	{
		output = null;
		int num = input.IndexOf('\'');
		if (num == -1)
		{
			return false;
		}
		int num2 = input.LastIndexOf('\'');
		if (num == num2 || input.IndexOf('\'', num + 1) != num2)
		{
			return false;
		}
		string name = input.Substring(0, num);
		string text = input.Substring(num2 + 1, input.Length - (num2 + 1));
		StringBuilder stringBuilder = new StringBuilder();
		try
		{
			Encoding encoding = Encoding.GetEncoding(name);
			byte[] array = new byte[text.Length];
			int num3 = 0;
			for (int i = 0; i < text.Length; i++)
			{
				if (Uri.IsHexEncoding(text, i))
				{
					array[num3++] = (byte)Uri.HexUnescape(text, ref i);
					i--;
					continue;
				}
				if (num3 > 0)
				{
					stringBuilder.Append(encoding.GetString(array, 0, num3));
					num3 = 0;
				}
				stringBuilder.Append(text[i]);
			}
			if (num3 > 0)
			{
				stringBuilder.Append(encoding.GetString(array, 0, num3));
			}
		}
		catch (ArgumentException)
		{
			return false;
		}
		output = stringBuilder.ToString();
		return true;
	}
}
