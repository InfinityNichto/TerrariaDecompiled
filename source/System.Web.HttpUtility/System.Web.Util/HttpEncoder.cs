using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace System.Web.Util;

internal static class HttpEncoder
{
	private sealed class UrlDecoder
	{
		private readonly int _bufferSize;

		private int _numChars;

		private readonly char[] _charBuffer;

		private int _numBytes;

		private byte[] _byteBuffer;

		private readonly Encoding _encoding;

		private void FlushBytes()
		{
			if (_numBytes > 0)
			{
				_numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
				_numBytes = 0;
			}
		}

		internal UrlDecoder(int bufferSize, Encoding encoding)
		{
			_bufferSize = bufferSize;
			_encoding = encoding;
			_charBuffer = new char[bufferSize];
		}

		internal void AddChar(char ch)
		{
			if (_numBytes > 0)
			{
				FlushBytes();
			}
			_charBuffer[_numChars++] = ch;
		}

		internal void AddByte(byte b)
		{
			if (_byteBuffer == null)
			{
				_byteBuffer = new byte[_bufferSize];
			}
			_byteBuffer[_numBytes++] = b;
		}

		internal string GetString()
		{
			if (_numBytes > 0)
			{
				FlushBytes();
			}
			if (_numChars <= 0)
			{
				return "";
			}
			return new string(_charBuffer, 0, _numChars);
		}
	}

	private static void AppendCharAsUnicodeJavaScript(StringBuilder builder, char c)
	{
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, builder);
		handler.AppendLiteral("\\u");
		handler.AppendFormatted((int)c, "x4");
		builder.Append(ref handler);
	}

	private static bool CharRequiresJavaScriptEncoding(char c)
	{
		if (c >= ' ' && c != '"' && c != '\\' && c != '\'' && c != '<' && c != '>' && c != '&' && c != '\u0085' && c != '\u2028')
		{
			return c == '\u2029';
		}
		return true;
	}

	[return: NotNullIfNotNull("value")]
	internal static string HtmlAttributeEncode(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		int num = IndexOfHtmlAttributeEncodingChars(value, 0);
		if (num == -1)
		{
			return value;
		}
		StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		HtmlAttributeEncode(value, stringWriter);
		return stringWriter.ToString();
	}

	internal static void HtmlAttributeEncode(string value, TextWriter output)
	{
		if (value != null)
		{
			if (output == null)
			{
				throw new ArgumentNullException("output");
			}
			HtmlAttributeEncodeInternal(value, output);
		}
	}

	private static void HtmlAttributeEncodeInternal(string s, TextWriter output)
	{
		int num = IndexOfHtmlAttributeEncodingChars(s, 0);
		if (num == -1)
		{
			output.Write(s);
			return;
		}
		output.Write(s.AsSpan(0, num));
		ReadOnlySpan<char> readOnlySpan = s.AsSpan(num);
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			if (c <= '<')
			{
				switch (c)
				{
				case '<':
					output.Write("&lt;");
					break;
				case '"':
					output.Write("&quot;");
					break;
				case '\'':
					output.Write("&#39;");
					break;
				case '&':
					output.Write("&amp;");
					break;
				default:
					output.Write(c);
					break;
				}
			}
			else
			{
				output.Write(c);
			}
		}
	}

	[return: NotNullIfNotNull("value")]
	internal static string HtmlDecode(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			return WebUtility.HtmlDecode(value);
		}
		return value;
	}

	internal static void HtmlDecode(string value, TextWriter output)
	{
		if (output == null)
		{
			throw new ArgumentNullException("output");
		}
		output.Write(WebUtility.HtmlDecode(value));
	}

	[return: NotNullIfNotNull("value")]
	internal static string HtmlEncode(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			return WebUtility.HtmlEncode(value);
		}
		return value;
	}

	internal static void HtmlEncode(string value, TextWriter output)
	{
		if (output == null)
		{
			throw new ArgumentNullException("output");
		}
		output.Write(WebUtility.HtmlEncode(value));
	}

	private static int IndexOfHtmlAttributeEncodingChars(string s, int startPos)
	{
		ReadOnlySpan<char> readOnlySpan = s.AsSpan(startPos);
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			switch (readOnlySpan[i])
			{
			case '"':
			case '&':
			case '\'':
			case '<':
				return startPos + i;
			}
		}
		return -1;
	}

	private static bool IsNonAsciiByte(byte b)
	{
		if (b < 127)
		{
			return b < 32;
		}
		return true;
	}

	internal static string JavaScriptStringEncode(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = null;
		int startIndex = 0;
		int num = 0;
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			if (CharRequiresJavaScriptEncoding(c))
			{
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(value.Length + 5);
				}
				if (num > 0)
				{
					stringBuilder.Append(value, startIndex, num);
				}
				startIndex = i + 1;
				num = 0;
				switch (c)
				{
				case '\r':
					stringBuilder.Append("\\r");
					break;
				case '\t':
					stringBuilder.Append("\\t");
					break;
				case '"':
					stringBuilder.Append("\\\"");
					break;
				case '\\':
					stringBuilder.Append("\\\\");
					break;
				case '\n':
					stringBuilder.Append("\\n");
					break;
				case '\b':
					stringBuilder.Append("\\b");
					break;
				case '\f':
					stringBuilder.Append("\\f");
					break;
				default:
					AppendCharAsUnicodeJavaScript(stringBuilder, c);
					break;
				}
			}
			else
			{
				num++;
			}
		}
		if (stringBuilder == null)
		{
			return value;
		}
		if (num > 0)
		{
			stringBuilder.Append(value, startIndex, num);
		}
		return stringBuilder.ToString();
	}

	[return: NotNullIfNotNull("bytes")]
	internal static byte[] UrlDecode(byte[] bytes, int offset, int count)
	{
		if (!ValidateUrlEncodingParameters(bytes, offset, count))
		{
			return null;
		}
		int num = 0;
		byte[] array = new byte[count];
		for (int i = 0; i < count; i++)
		{
			int num2 = offset + i;
			byte b = bytes[num2];
			switch (b)
			{
			case 43:
				b = 32;
				break;
			case 37:
				if (i < count - 2)
				{
					int num3 = System.HexConverter.FromChar(bytes[num2 + 1]);
					int num4 = System.HexConverter.FromChar(bytes[num2 + 2]);
					if ((num3 | num4) != 255)
					{
						b = (byte)((num3 << 4) | num4);
						i += 2;
					}
				}
				break;
			}
			array[num++] = b;
		}
		if (num < array.Length)
		{
			byte[] array2 = new byte[num];
			Array.Copy(array, array2, num);
			array = array2;
		}
		return array;
	}

	[return: NotNullIfNotNull("bytes")]
	internal static string UrlDecode(byte[] bytes, int offset, int count, Encoding encoding)
	{
		if (!ValidateUrlEncodingParameters(bytes, offset, count))
		{
			return null;
		}
		UrlDecoder urlDecoder = new UrlDecoder(count, encoding);
		for (int i = 0; i < count; i++)
		{
			int num = offset + i;
			byte b = bytes[num];
			switch (b)
			{
			case 43:
				b = 32;
				break;
			case 37:
				if (i >= count - 2)
				{
					break;
				}
				if (bytes[num + 1] == 117 && i < count - 5)
				{
					int num2 = System.HexConverter.FromChar(bytes[num + 2]);
					int num3 = System.HexConverter.FromChar(bytes[num + 3]);
					int num4 = System.HexConverter.FromChar(bytes[num + 4]);
					int num5 = System.HexConverter.FromChar(bytes[num + 5]);
					if ((num2 | num3 | num4 | num5) != 255)
					{
						char ch = (char)((num2 << 12) | (num3 << 8) | (num4 << 4) | num5);
						i += 5;
						urlDecoder.AddChar(ch);
						continue;
					}
				}
				else
				{
					int num6 = System.HexConverter.FromChar(bytes[num + 1]);
					int num7 = System.HexConverter.FromChar(bytes[num + 2]);
					if ((num6 | num7) != 255)
					{
						b = (byte)((num6 << 4) | num7);
						i += 2;
					}
				}
				break;
			}
			urlDecoder.AddByte(b);
		}
		return Utf16StringValidator.ValidateString(urlDecoder.GetString());
	}

	[return: NotNullIfNotNull("value")]
	internal static string UrlDecode(string value, Encoding encoding)
	{
		if (value == null)
		{
			return null;
		}
		int length = value.Length;
		UrlDecoder urlDecoder = new UrlDecoder(length, encoding);
		for (int i = 0; i < length; i++)
		{
			char c = value[i];
			switch (c)
			{
			case '+':
				c = ' ';
				break;
			case '%':
				if (i >= length - 2)
				{
					break;
				}
				if (value[i + 1] == 'u' && i < length - 5)
				{
					int num = System.HexConverter.FromChar(value[i + 2]);
					int num2 = System.HexConverter.FromChar(value[i + 3]);
					int num3 = System.HexConverter.FromChar(value[i + 4]);
					int num4 = System.HexConverter.FromChar(value[i + 5]);
					if ((num | num2 | num3 | num4) != 255)
					{
						c = (char)((num << 12) | (num2 << 8) | (num3 << 4) | num4);
						i += 5;
						urlDecoder.AddChar(c);
						continue;
					}
				}
				else
				{
					int num5 = System.HexConverter.FromChar(value[i + 1]);
					int num6 = System.HexConverter.FromChar(value[i + 2]);
					if ((num5 | num6) != 255)
					{
						byte b = (byte)((num5 << 4) | num6);
						i += 2;
						urlDecoder.AddByte(b);
						continue;
					}
				}
				break;
			}
			if ((c & 0xFF80) == 0)
			{
				urlDecoder.AddByte((byte)c);
			}
			else
			{
				urlDecoder.AddChar(c);
			}
		}
		return Utf16StringValidator.ValidateString(urlDecoder.GetString());
	}

	[return: NotNullIfNotNull("bytes")]
	internal static byte[] UrlEncode(byte[] bytes, int offset, int count, bool alwaysCreateNewReturnValue)
	{
		byte[] array = UrlEncode(bytes, offset, count);
		if (!alwaysCreateNewReturnValue || array == null || array != bytes)
		{
			return array;
		}
		return (byte[])array.Clone();
	}

	[return: NotNullIfNotNull("bytes")]
	private static byte[] UrlEncode(byte[] bytes, int offset, int count)
	{
		if (!ValidateUrlEncodingParameters(bytes, offset, count))
		{
			return null;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < count; i++)
		{
			char c = (char)bytes[offset + i];
			if (c == ' ')
			{
				num++;
			}
			else if (!HttpEncoderUtility.IsUrlSafeChar(c))
			{
				num2++;
			}
		}
		if (num == 0 && num2 == 0)
		{
			if (offset == 0 && bytes.Length == count)
			{
				return bytes;
			}
			byte[] array = new byte[count];
			Buffer.BlockCopy(bytes, offset, array, 0, count);
			return array;
		}
		byte[] array2 = new byte[count + num2 * 2];
		int num3 = 0;
		for (int j = 0; j < count; j++)
		{
			byte b = bytes[offset + j];
			char c2 = (char)b;
			if (HttpEncoderUtility.IsUrlSafeChar(c2))
			{
				array2[num3++] = b;
				continue;
			}
			if (c2 == ' ')
			{
				array2[num3++] = 43;
				continue;
			}
			array2[num3++] = 37;
			array2[num3++] = (byte)System.HexConverter.ToCharLower(b >> 4);
			array2[num3++] = (byte)System.HexConverter.ToCharLower(b);
		}
		return array2;
	}

	private static string UrlEncodeNonAscii(string str, Encoding e)
	{
		byte[] bytes = e.GetBytes(str);
		byte[] bytes2 = UrlEncodeNonAscii(bytes, 0, bytes.Length);
		return Encoding.ASCII.GetString(bytes2);
	}

	private static byte[] UrlEncodeNonAscii(byte[] bytes, int offset, int count)
	{
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			if (IsNonAsciiByte(bytes[offset + i]))
			{
				num++;
			}
		}
		if (num == 0)
		{
			return bytes;
		}
		byte[] array = new byte[count + num * 2];
		int num2 = 0;
		for (int j = 0; j < count; j++)
		{
			byte b = bytes[offset + j];
			if (IsNonAsciiByte(b))
			{
				array[num2++] = 37;
				array[num2++] = (byte)System.HexConverter.ToCharLower(b >> 4);
				array[num2++] = (byte)System.HexConverter.ToCharLower(b);
			}
			else
			{
				array[num2++] = b;
			}
		}
		return array;
	}

	[Obsolete("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncode(*).")]
	[return: NotNullIfNotNull("value")]
	internal static string UrlEncodeUnicode(string value)
	{
		if (value == null)
		{
			return null;
		}
		int length = value.Length;
		StringBuilder stringBuilder = new StringBuilder(length);
		for (int i = 0; i < length; i++)
		{
			char c = value[i];
			if ((c & 0xFF80) == 0)
			{
				if (HttpEncoderUtility.IsUrlSafeChar(c))
				{
					stringBuilder.Append(c);
					continue;
				}
				if (c == ' ')
				{
					stringBuilder.Append('+');
					continue;
				}
				stringBuilder.Append('%');
				stringBuilder.Append(System.HexConverter.ToCharLower((int)c >> 4));
				stringBuilder.Append(System.HexConverter.ToCharLower(c));
			}
			else
			{
				stringBuilder.Append("%u");
				stringBuilder.Append(System.HexConverter.ToCharLower((int)c >> 12));
				stringBuilder.Append(System.HexConverter.ToCharLower((int)c >> 8));
				stringBuilder.Append(System.HexConverter.ToCharLower((int)c >> 4));
				stringBuilder.Append(System.HexConverter.ToCharLower(c));
			}
		}
		return stringBuilder.ToString();
	}

	[return: NotNullIfNotNull("value")]
	internal static string UrlPathEncode(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		if (!UriUtil.TrySplitUriForPathEncode(value, out var schemeAndAuthority, out var path, out var queryAndFragment))
		{
			schemeAndAuthority = null;
			path = value;
			queryAndFragment = null;
		}
		return schemeAndAuthority + UrlPathEncodeImpl(path) + queryAndFragment;
	}

	private static string UrlPathEncodeImpl(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		int num = value.IndexOf('?');
		if (num >= 0)
		{
			return UrlPathEncodeImpl(value.Substring(0, num)) + value.AsSpan(num);
		}
		return HttpEncoderUtility.UrlEncodeSpaces(UrlEncodeNonAscii(value, Encoding.UTF8));
	}

	private static bool ValidateUrlEncodingParameters([NotNullWhen(true)] byte[] bytes, int offset, int count)
	{
		if (bytes == null && count == 0)
		{
			return false;
		}
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes");
		}
		if (offset < 0 || offset > bytes.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0 || offset + count > bytes.Length)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		return true;
	}
}
