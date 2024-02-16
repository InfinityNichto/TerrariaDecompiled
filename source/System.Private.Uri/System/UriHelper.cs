using System.Runtime.InteropServices;
using System.Text;

namespace System;

internal static class UriHelper
{
	internal static readonly Encoding s_noFallbackCharUTF8 = Encoding.GetEncoding(Encoding.UTF8.CodePage, new EncoderReplacementFallback(""), new DecoderReplacementFallback(""));

	internal static readonly char[] s_WSchars = new char[4] { ' ', '\n', '\r', '\t' };

	internal static ReadOnlySpan<bool> UnreservedReservedTable => new bool[128]
	{
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, true, false, true, true, false, true, true,
		true, true, true, true, true, true, true, true, true, true,
		true, true, true, true, true, true, true, true, true, true,
		false, true, false, true, true, true, true, true, true, true,
		true, true, true, true, true, true, true, true, true, true,
		true, true, true, true, true, true, true, true, true, true,
		true, true, false, true, false, true, false, true, true, true,
		true, true, true, true, true, true, true, true, true, true,
		true, true, true, true, true, true, true, true, true, true,
		true, true, true, false, false, false, true, false
	};

	internal static ReadOnlySpan<bool> UnreservedTable => new bool[128]
	{
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, true, true, false, true, true,
		true, true, true, true, true, true, true, true, false, false,
		false, false, false, false, false, true, true, true, true, true,
		true, true, true, true, true, true, true, true, true, true,
		true, true, true, true, true, true, true, true, true, true,
		true, false, false, false, false, true, false, true, true, true,
		true, true, true, true, true, true, true, true, true, true,
		true, true, true, true, true, true, true, true, true, true,
		true, true, true, false, false, false, true, false
	};

	internal unsafe static bool TestForSubPath(char* selfPtr, int selfLength, char* otherPtr, int otherLength, bool ignoreCase)
	{
		int i = 0;
		bool flag = true;
		for (; i < selfLength && i < otherLength; i++)
		{
			char c = selfPtr[i];
			char c2 = otherPtr[i];
			switch (c)
			{
			case '#':
			case '?':
				return true;
			case '/':
				if (c2 != '/')
				{
					return false;
				}
				if (!flag)
				{
					return false;
				}
				flag = true;
				continue;
			default:
				if (c2 == '?' || c2 == '#')
				{
					break;
				}
				if (!ignoreCase)
				{
					if (c != c2)
					{
						flag = false;
					}
				}
				else if (char.ToLowerInvariant(c) != char.ToLowerInvariant(c2))
				{
					flag = false;
				}
				continue;
			}
			break;
		}
		for (; i < selfLength; i++)
		{
			char c;
			if ((c = selfPtr[i]) != '?')
			{
				switch (c)
				{
				case '#':
					break;
				case '/':
					return false;
				default:
					continue;
				}
			}
			return true;
		}
		return true;
	}

	internal static string EscapeString(string stringToEscape, bool checkExistingEscaped, ReadOnlySpan<bool> unreserved, char forceEscape1 = '\0', char forceEscape2 = '\0')
	{
		if (stringToEscape == null)
		{
			throw new ArgumentNullException("stringToEscape");
		}
		if (stringToEscape.Length == 0)
		{
			return string.Empty;
		}
		ReadOnlySpan<bool> readOnlySpan = default(Span<bool>);
		if ((forceEscape1 | forceEscape2) == 0)
		{
			readOnlySpan = unreserved;
		}
		else
		{
			Span<bool> span = stackalloc bool[128];
			unreserved.CopyTo(span);
			span[forceEscape1] = false;
			span[forceEscape2] = false;
			readOnlySpan = span;
		}
		int i;
		for (i = 0; i < stringToEscape.Length; i++)
		{
			char index;
			if ((index = stringToEscape[i]) > '\u007f')
			{
				break;
			}
			if (!readOnlySpan[index])
			{
				break;
			}
		}
		if (i == stringToEscape.Length)
		{
			return stringToEscape;
		}
		Span<char> initialBuffer = stackalloc char[512];
		System.Text.ValueStringBuilder vsb = new System.Text.ValueStringBuilder(initialBuffer);
		vsb.Append(stringToEscape.AsSpan(0, i));
		EscapeStringToBuilder(stringToEscape.AsSpan(i), ref vsb, readOnlySpan, checkExistingEscaped);
		return vsb.ToString();
	}

	internal static void EscapeString(ReadOnlySpan<char> stringToEscape, ref System.Text.ValueStringBuilder dest, bool checkExistingEscaped, char forceEscape1 = '\0', char forceEscape2 = '\0')
	{
		ReadOnlySpan<bool> readOnlySpan = default(Span<bool>);
		if ((forceEscape1 | forceEscape2) == 0)
		{
			readOnlySpan = UnreservedReservedTable;
		}
		else
		{
			Span<bool> span = stackalloc bool[128];
			UnreservedReservedTable.CopyTo(span);
			span[forceEscape1] = false;
			span[forceEscape2] = false;
			readOnlySpan = span;
		}
		int i;
		for (i = 0; i < stringToEscape.Length; i++)
		{
			char index;
			if ((index = stringToEscape[i]) > '\u007f')
			{
				break;
			}
			if (!readOnlySpan[index])
			{
				break;
			}
		}
		if (i == stringToEscape.Length)
		{
			dest.Append(stringToEscape);
			return;
		}
		dest.Append(stringToEscape.Slice(0, i));
		ReadOnlySpan<bool> noEscape = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(readOnlySpan), readOnlySpan.Length);
		EscapeStringToBuilder(stringToEscape.Slice(i), ref dest, noEscape, checkExistingEscaped);
	}

	private static void EscapeStringToBuilder(ReadOnlySpan<char> stringToEscape, ref System.Text.ValueStringBuilder vsb, ReadOnlySpan<bool> noEscape, bool checkExistingEscaped)
	{
		Span<byte> destination = stackalloc byte[4];
		SpanRuneEnumerator spanRuneEnumerator = stringToEscape.EnumerateRunes();
		while (spanRuneEnumerator.MoveNext())
		{
			Rune current = spanRuneEnumerator.Current;
			if (!current.IsAscii)
			{
				current.TryEncodeToUtf8(destination, out var bytesWritten);
				Span<byte> span = destination.Slice(0, bytesWritten);
				for (int i = 0; i < span.Length; i++)
				{
					byte value = span[i];
					vsb.Append('%');
					System.HexConverter.ToCharsBuffer(value, vsb.AppendSpan(2));
				}
				continue;
			}
			byte b = (byte)current.Value;
			if (noEscape[b])
			{
				vsb.Append((char)b);
				continue;
			}
			if (checkExistingEscaped && b == 37)
			{
				SpanRuneEnumerator spanRuneEnumerator2 = spanRuneEnumerator;
				if (spanRuneEnumerator2.MoveNext())
				{
					Rune current2 = spanRuneEnumerator2.Current;
					if (current2.IsAscii && IsHexDigit((char)current2.Value) && spanRuneEnumerator2.MoveNext())
					{
						Rune current3 = spanRuneEnumerator2.Current;
						if (current3.IsAscii && IsHexDigit((char)current3.Value))
						{
							vsb.Append('%');
							vsb.Append((char)current2.Value);
							vsb.Append((char)current3.Value);
							spanRuneEnumerator = spanRuneEnumerator2;
							continue;
						}
					}
				}
			}
			vsb.Append('%');
			System.HexConverter.ToCharsBuffer(b, vsb.AppendSpan(2));
		}
	}

	internal unsafe static char[] UnescapeString(string input, int start, int end, char[] dest, ref int destPosition, char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser syntax, bool isQuery)
	{
		fixed (char* pStr = input)
		{
			return UnescapeString(pStr, start, end, dest, ref destPosition, rsvd1, rsvd2, rsvd3, unescapeMode, syntax, isQuery);
		}
	}

	internal unsafe static char[] UnescapeString(char* pStr, int start, int end, char[] dest, ref int destPosition, char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser syntax, bool isQuery)
	{
		System.Text.ValueStringBuilder dest2 = new System.Text.ValueStringBuilder(dest.Length);
		dest2.Append(dest.AsSpan(0, destPosition));
		UnescapeString(pStr, start, end, ref dest2, rsvd1, rsvd2, rsvd3, unescapeMode, syntax, isQuery);
		if (dest2.Length > dest.Length)
		{
			dest = dest2.AsSpan().ToArray();
		}
		else
		{
			dest2.AsSpan(destPosition).TryCopyTo(dest.AsSpan(destPosition));
		}
		destPosition = dest2.Length;
		dest2.Dispose();
		return dest;
	}

	internal unsafe static void UnescapeString(string input, int start, int end, ref System.Text.ValueStringBuilder dest, char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser syntax, bool isQuery)
	{
		fixed (char* pStr = input)
		{
			UnescapeString(pStr, start, end, ref dest, rsvd1, rsvd2, rsvd3, unescapeMode, syntax, isQuery);
		}
	}

	internal unsafe static void UnescapeString(ReadOnlySpan<char> input, ref System.Text.ValueStringBuilder dest, char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser syntax, bool isQuery)
	{
		fixed (char* pStr = &MemoryMarshal.GetReference(input))
		{
			UnescapeString(pStr, 0, input.Length, ref dest, rsvd1, rsvd2, rsvd3, unescapeMode, syntax, isQuery);
		}
	}

	internal unsafe static void UnescapeString(char* pStr, int start, int end, ref System.Text.ValueStringBuilder dest, char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser syntax, bool isQuery)
	{
		if ((unescapeMode & UnescapeMode.EscapeUnescape) == 0)
		{
			dest.Append(pStr + start, end - start);
			return;
		}
		bool flag = false;
		bool flag2 = Uri.IriParsingStatic(syntax) && (unescapeMode & UnescapeMode.EscapeUnescape) == UnescapeMode.EscapeUnescape;
		int i = start;
		while (i < end)
		{
			char c = '\0';
			for (; i < end; i++)
			{
				if ((c = pStr[i]) == '%')
				{
					if ((unescapeMode & UnescapeMode.Unescape) == 0)
					{
						flag = true;
						break;
					}
					if (i + 2 < end)
					{
						c = DecodeHexChars(pStr[i + 1], pStr[i + 2]);
						if (unescapeMode < UnescapeMode.UnescapeAll)
						{
							switch (c)
							{
							case '\uffff':
								if ((unescapeMode & UnescapeMode.Escape) == 0)
								{
									continue;
								}
								flag = true;
								break;
							case '%':
								i += 2;
								continue;
							default:
								if (c == rsvd1 || c == rsvd2 || c == rsvd3)
								{
									i += 2;
									continue;
								}
								if ((unescapeMode & UnescapeMode.V1ToStringFlag) == 0 && IsNotSafeForUnescape(c))
								{
									i += 2;
									continue;
								}
								if (flag2 && ((c <= '\u009f' && IsNotSafeForUnescape(c)) || (c > '\u009f' && !IriHelper.CheckIriUnicodeRange(c, isQuery))))
								{
									i += 2;
									continue;
								}
								break;
							}
							break;
						}
						if (c != '\uffff')
						{
							break;
						}
						if (unescapeMode >= UnescapeMode.UnescapeAllOrThrow)
						{
							throw new UriFormatException(System.SR.net_uri_BadString);
						}
					}
					else
					{
						if (unescapeMode < UnescapeMode.UnescapeAll)
						{
							flag = true;
							break;
						}
						if (unescapeMode >= UnescapeMode.UnescapeAllOrThrow)
						{
							throw new UriFormatException(System.SR.net_uri_BadString);
						}
					}
				}
				else if ((unescapeMode & (UnescapeMode.Unescape | UnescapeMode.UnescapeAll)) != (UnescapeMode.Unescape | UnescapeMode.UnescapeAll) && (unescapeMode & UnescapeMode.Escape) != 0)
				{
					if (c == rsvd1 || c == rsvd2 || c == rsvd3)
					{
						flag = true;
						break;
					}
					if ((unescapeMode & UnescapeMode.V1ToStringFlag) == 0 && (c <= '\u001f' || (c >= '\u007f' && c <= '\u009f')))
					{
						flag = true;
						break;
					}
				}
			}
			while (start < i)
			{
				dest.Append(pStr[start++]);
			}
			if (i != end)
			{
				if (flag)
				{
					EscapeAsciiChar((byte)pStr[i], ref dest);
					flag = false;
					i++;
				}
				else if (c <= '\u007f')
				{
					dest.Append(c);
					i += 3;
				}
				else
				{
					int num = PercentEncodingHelper.UnescapePercentEncodedUTF8Sequence(pStr + i, end - i, ref dest, isQuery, flag2);
					i += num;
				}
				start = i;
			}
		}
	}

	internal static void EscapeAsciiChar(byte b, ref System.Text.ValueStringBuilder to)
	{
		to.Append('%');
		System.HexConverter.ToCharsBuffer(b, to.AppendSpan(2));
	}

	internal static char DecodeHexChars(int first, int second)
	{
		int num = System.HexConverter.FromChar(first);
		int num2 = System.HexConverter.FromChar(second);
		if ((num | num2) == 255)
		{
			return '\uffff';
		}
		return (char)((num << 4) | num2);
	}

	internal static bool IsNotSafeForUnescape(char ch)
	{
		if (ch <= '\u001f' || (ch >= '\u007f' && ch <= '\u009f'))
		{
			return true;
		}
		return ";/?:@&=+$,#[]!'()*%\\#".Contains(ch);
	}

	internal static bool IsGenDelim(char ch)
	{
		if (ch != ':' && ch != '/' && ch != '?' && ch != '#' && ch != '[' && ch != ']')
		{
			return ch == '@';
		}
		return true;
	}

	internal static bool IsLWS(char ch)
	{
		if (ch <= ' ')
		{
			if (ch != ' ' && ch != '\n' && ch != '\r')
			{
				return ch == '\t';
			}
			return true;
		}
		return false;
	}

	internal static bool IsAsciiLetter(char character)
	{
		return ((uint)(character - 65) & -33) < 26;
	}

	internal static bool IsAsciiLetterOrDigit(char character)
	{
		if (((uint)(character - 65) & -33) >= 26)
		{
			return (uint)(character - 48) < 10u;
		}
		return true;
	}

	internal static bool IsHexDigit(char character)
	{
		return System.HexConverter.IsHexChar(character);
	}

	internal static bool IsBidiControlCharacter(char ch)
	{
		if (ch != '\u200e' && ch != '\u200f' && ch != '\u202a' && ch != '\u202b' && ch != '\u202c' && ch != '\u202d')
		{
			return ch == '\u202e';
		}
		return true;
	}

	internal unsafe static string StripBidiControlCharacters(ReadOnlySpan<char> strToClean, string backingString = null)
	{
		int num = 0;
		ReadOnlySpan<char> readOnlySpan = strToClean;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			char c = readOnlySpan[i];
			if ((uint)(c - 8206) <= 32u && IsBidiControlCharacter(c))
			{
				num++;
			}
		}
		if (num == 0)
		{
			return backingString ?? new string(strToClean);
		}
		if (num == strToClean.Length)
		{
			return string.Empty;
		}
		fixed (char* ptr = &MemoryMarshal.GetReference(strToClean))
		{
			return string.Create(strToClean.Length - num, ((IntPtr)ptr, strToClean.Length), delegate(Span<char> buffer, (IntPtr StrToClean, int Length) state)
			{
				ReadOnlySpan<char> readOnlySpan2 = new ReadOnlySpan<char>((void*)state.StrToClean, state.Length);
				int num2 = 0;
				ReadOnlySpan<char> readOnlySpan3 = readOnlySpan2;
				for (int j = 0; j < readOnlySpan3.Length; j++)
				{
					char c2 = readOnlySpan3[j];
					if ((uint)(c2 - 8206) > 32u || !IsBidiControlCharacter(c2))
					{
						buffer[num2++] = c2;
					}
				}
			});
		}
	}
}
