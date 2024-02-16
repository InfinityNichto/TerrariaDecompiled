using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Util;

namespace System.Web;

public sealed class HttpUtility
{
	private sealed class HttpQSCollection : NameValueCollection
	{
		internal HttpQSCollection()
			: base(StringComparer.OrdinalIgnoreCase)
		{
		}

		public override string ToString()
		{
			int count = Count;
			if (count == 0)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			string[] allKeys = AllKeys;
			for (int i = 0; i < count; i++)
			{
				string text = allKeys[i];
				string[] values = GetValues(text);
				if (values == null)
				{
					continue;
				}
				string[] array = values;
				foreach (string str in array)
				{
					if (!string.IsNullOrEmpty(text))
					{
						stringBuilder.Append(text).Append('=');
					}
					stringBuilder.Append(UrlEncode(str)).Append('&');
				}
			}
			return stringBuilder.ToString(0, stringBuilder.Length - 1);
		}
	}

	public static NameValueCollection ParseQueryString(string query)
	{
		return ParseQueryString(query, Encoding.UTF8);
	}

	public static NameValueCollection ParseQueryString(string query, Encoding encoding)
	{
		if (query == null)
		{
			throw new ArgumentNullException("query");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		HttpQSCollection httpQSCollection = new HttpQSCollection();
		int length = query.Length;
		int num = ((length > 0 && query[0] == '?') ? 1 : 0);
		if (length == num)
		{
			return httpQSCollection;
		}
		while (num <= length)
		{
			int num2 = -1;
			int num3 = -1;
			for (int i = num; i < length; i++)
			{
				if (num2 == -1 && query[i] == '=')
				{
					num2 = i + 1;
				}
				else if (query[i] == '&')
				{
					num3 = i;
					break;
				}
			}
			string name;
			if (num2 == -1)
			{
				name = null;
				num2 = num;
			}
			else
			{
				name = UrlDecode(query.Substring(num, num2 - num - 1), encoding);
			}
			if (num3 < 0)
			{
				num3 = query.Length;
			}
			num = num3 + 1;
			string value = UrlDecode(query.Substring(num2, num3 - num2), encoding);
			httpQSCollection.Add(name, value);
		}
		return httpQSCollection;
	}

	[return: NotNullIfNotNull("s")]
	public static string? HtmlDecode(string? s)
	{
		return HttpEncoder.HtmlDecode(s);
	}

	public static void HtmlDecode(string? s, TextWriter output)
	{
		HttpEncoder.HtmlDecode(s, output);
	}

	[return: NotNullIfNotNull("s")]
	public static string? HtmlEncode(string? s)
	{
		return HttpEncoder.HtmlEncode(s);
	}

	[return: NotNullIfNotNull("value")]
	public static string? HtmlEncode(object? value)
	{
		if (value != null)
		{
			return HtmlEncode(Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty);
		}
		return null;
	}

	public static void HtmlEncode(string? s, TextWriter output)
	{
		HttpEncoder.HtmlEncode(s, output);
	}

	[return: NotNullIfNotNull("s")]
	public static string? HtmlAttributeEncode(string? s)
	{
		return HttpEncoder.HtmlAttributeEncode(s);
	}

	public static void HtmlAttributeEncode(string? s, TextWriter output)
	{
		HttpEncoder.HtmlAttributeEncode(s, output);
	}

	[return: NotNullIfNotNull("str")]
	public static string? UrlEncode(string? str)
	{
		return UrlEncode(str, Encoding.UTF8);
	}

	[return: NotNullIfNotNull("str")]
	public static string? UrlPathEncode(string? str)
	{
		return HttpEncoder.UrlPathEncode(str);
	}

	[return: NotNullIfNotNull("str")]
	public static string? UrlEncode(string? str, Encoding e)
	{
		if (str != null)
		{
			return Encoding.ASCII.GetString(UrlEncodeToBytes(str, e));
		}
		return null;
	}

	[return: NotNullIfNotNull("bytes")]
	public static string? UrlEncode(byte[]? bytes)
	{
		if (bytes != null)
		{
			return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes));
		}
		return null;
	}

	[return: NotNullIfNotNull("bytes")]
	public static string? UrlEncode(byte[]? bytes, int offset, int count)
	{
		if (bytes != null)
		{
			return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes, offset, count));
		}
		return null;
	}

	[return: NotNullIfNotNull("str")]
	public static byte[]? UrlEncodeToBytes(string? str)
	{
		return UrlEncodeToBytes(str, Encoding.UTF8);
	}

	[return: NotNullIfNotNull("bytes")]
	public static byte[]? UrlEncodeToBytes(byte[]? bytes)
	{
		if (bytes != null)
		{
			return UrlEncodeToBytes(bytes, 0, bytes.Length);
		}
		return null;
	}

	[Obsolete("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncodeToBytes(String).")]
	[return: NotNullIfNotNull("str")]
	public static byte[]? UrlEncodeUnicodeToBytes(string? str)
	{
		if (str != null)
		{
			return Encoding.ASCII.GetBytes(UrlEncodeUnicode(str));
		}
		return null;
	}

	[return: NotNullIfNotNull("str")]
	public static string? UrlDecode(string? str)
	{
		return UrlDecode(str, Encoding.UTF8);
	}

	[return: NotNullIfNotNull("bytes")]
	public static string? UrlDecode(byte[]? bytes, Encoding e)
	{
		if (bytes != null)
		{
			return UrlDecode(bytes, 0, bytes.Length, e);
		}
		return null;
	}

	[return: NotNullIfNotNull("str")]
	public static byte[]? UrlDecodeToBytes(string? str)
	{
		return UrlDecodeToBytes(str, Encoding.UTF8);
	}

	[return: NotNullIfNotNull("str")]
	public static byte[]? UrlDecodeToBytes(string? str, Encoding e)
	{
		if (str != null)
		{
			return UrlDecodeToBytes(e.GetBytes(str));
		}
		return null;
	}

	[return: NotNullIfNotNull("bytes")]
	public static byte[]? UrlDecodeToBytes(byte[]? bytes)
	{
		if (bytes != null)
		{
			return UrlDecodeToBytes(bytes, 0, bytes.Length);
		}
		return null;
	}

	[return: NotNullIfNotNull("str")]
	public static byte[]? UrlEncodeToBytes(string? str, Encoding e)
	{
		if (str == null)
		{
			return null;
		}
		byte[] bytes = e.GetBytes(str);
		return HttpEncoder.UrlEncode(bytes, 0, bytes.Length, alwaysCreateNewReturnValue: false);
	}

	[return: NotNullIfNotNull("bytes")]
	public static byte[]? UrlEncodeToBytes(byte[]? bytes, int offset, int count)
	{
		return HttpEncoder.UrlEncode(bytes, offset, count, alwaysCreateNewReturnValue: true);
	}

	[Obsolete("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncode(String).")]
	[return: NotNullIfNotNull("str")]
	public static string? UrlEncodeUnicode(string? str)
	{
		return HttpEncoder.UrlEncodeUnicode(str);
	}

	[return: NotNullIfNotNull("str")]
	public static string? UrlDecode(string? str, Encoding e)
	{
		return HttpEncoder.UrlDecode(str, e);
	}

	[return: NotNullIfNotNull("bytes")]
	public static string? UrlDecode(byte[]? bytes, int offset, int count, Encoding e)
	{
		return HttpEncoder.UrlDecode(bytes, offset, count, e);
	}

	[return: NotNullIfNotNull("bytes")]
	public static byte[]? UrlDecodeToBytes(byte[]? bytes, int offset, int count)
	{
		return HttpEncoder.UrlDecode(bytes, offset, count);
	}

	public static string JavaScriptStringEncode(string? value)
	{
		return HttpEncoder.JavaScriptStringEncode(value);
	}

	public static string JavaScriptStringEncode(string? value, bool addDoubleQuotes)
	{
		string text = HttpEncoder.JavaScriptStringEncode(value);
		if (!addDoubleQuotes)
		{
			return text;
		}
		return "\"" + text + "\"";
	}
}
