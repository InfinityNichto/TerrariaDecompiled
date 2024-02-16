using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace System;

internal static class DomainNameHelper
{
	private static readonly IdnMapping s_idnMapping = new IdnMapping();

	private static readonly char[] s_UnsafeForNormalizedHost = new char[8] { '\\', '/', '?', '@', '#', ':', '[', ']' };

	internal static string ParseCanonicalName(string str, int start, int end, ref bool loopback)
	{
		string text = null;
		for (int num = end - 1; num >= start; num--)
		{
			if (str[num] >= 'A' && str[num] <= 'Z')
			{
				text = str.Substring(start, end - start).ToLowerInvariant();
				break;
			}
			if (str[num] == ':')
			{
				end = num;
			}
		}
		if (text == null)
		{
			text = str.Substring(start, end - start);
		}
		if (text == "localhost" || text == "loopback")
		{
			loopback = true;
			return "localhost";
		}
		return text;
	}

	internal unsafe static bool IsValid(char* name, int pos, ref int returnedEnd, ref bool notCanonical, bool notImplicitFile)
	{
		char* ptr = name + pos;
		char* ptr2 = ptr;
		char* ptr3;
		for (ptr3 = name + returnedEnd; ptr2 < ptr3; ptr2++)
		{
			char c = *ptr2;
			if (c > '\u007f')
			{
				return false;
			}
			if (c < 'a' && (c == '/' || c == '\\' || (notImplicitFile && (c == ':' || c == '?' || c == '#'))))
			{
				ptr3 = ptr2;
				break;
			}
		}
		if (ptr3 == ptr)
		{
			return false;
		}
		do
		{
			for (ptr2 = ptr; ptr2 < ptr3 && *ptr2 != '.'; ptr2++)
			{
			}
			if (ptr == ptr2 || ptr2 - ptr > 63 || !IsASCIILetterOrDigit(*(ptr++), ref notCanonical))
			{
				return false;
			}
			while (ptr < ptr2)
			{
				if (!IsValidDomainLabelCharacter(*(ptr++), ref notCanonical))
				{
					return false;
				}
			}
			ptr++;
		}
		while (ptr < ptr3);
		returnedEnd = (int)(ptr3 - name);
		return true;
	}

	internal unsafe static bool IsValidByIri(char* name, int pos, ref int returnedEnd, ref bool notCanonical, bool notImplicitFile)
	{
		char* ptr = name + pos;
		char* ptr2 = ptr;
		char* ptr3 = name + returnedEnd;
		int num = 0;
		for (; ptr2 < ptr3; ptr2++)
		{
			char c = *ptr2;
			if (c == '/' || c == '\\' || (notImplicitFile && (c == ':' || c == '?' || c == '#')))
			{
				ptr3 = ptr2;
				break;
			}
		}
		if (ptr3 == ptr)
		{
			return false;
		}
		do
		{
			ptr2 = ptr;
			num = 0;
			bool flag = false;
			for (; ptr2 < ptr3 && *ptr2 != '.' && *ptr2 != '。' && *ptr2 != '．' && *ptr2 != '｡'; ptr2++)
			{
				num++;
				if (*ptr2 > 'ÿ')
				{
					num++;
				}
				if (*ptr2 >= '\u00a0')
				{
					flag = true;
				}
			}
			if (ptr == ptr2 || (flag ? (num + 4) : num) > 63 || (*(ptr++) < '\u00a0' && !IsASCIILetterOrDigit(*(ptr - 1), ref notCanonical)))
			{
				return false;
			}
			while (ptr < ptr2)
			{
				if (*(ptr++) < '\u00a0' && !IsValidDomainLabelCharacter(*(ptr - 1), ref notCanonical))
				{
					return false;
				}
			}
			ptr++;
		}
		while (ptr < ptr3);
		returnedEnd = (int)(ptr3 - name);
		return true;
	}

	internal static string IdnEquivalent(string hostname)
	{
		if (hostname.Length == 0)
		{
			return hostname;
		}
		bool flag = true;
		foreach (char c in hostname)
		{
			if (c > '\u007f')
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			return hostname.ToLowerInvariant();
		}
		string unicode = UriHelper.StripBidiControlCharacters(hostname, hostname);
		try
		{
			string ascii = s_idnMapping.GetAscii(unicode);
			if (ContainsCharactersUnsafeForNormalizedHost(ascii))
			{
				throw new UriFormatException(System.SR.net_uri_BadUnicodeHostForIdn);
			}
			return ascii;
		}
		catch (ArgumentException)
		{
			throw new UriFormatException(System.SR.net_uri_BadUnicodeHostForIdn);
		}
	}

	internal static bool TryGetUnicodeEquivalent(string hostname, ref System.Text.ValueStringBuilder dest)
	{
		int num = 0;
		do
		{
			if (num != 0)
			{
				dest.Append('.');
			}
			bool flag = true;
			int i;
			for (i = num; (uint)i < (uint)hostname.Length; i++)
			{
				char c = hostname[i];
				if (c == '.')
				{
					break;
				}
				if (c > '\u007f')
				{
					flag = false;
					if (c == '。' || c == '．' || c == '｡')
					{
						break;
					}
				}
			}
			if (!flag)
			{
				try
				{
					string ascii = s_idnMapping.GetAscii(hostname, num, i - num);
					dest.Append(s_idnMapping.GetUnicode(ascii));
				}
				catch (ArgumentException)
				{
					return false;
				}
			}
			else
			{
				bool flag2 = false;
				if ((uint)(num + 3) < (uint)hostname.Length && hostname[num] == 'x' && hostname[num + 1] == 'n' && hostname[num + 2] == '-' && hostname[num + 3] == '-')
				{
					try
					{
						dest.Append(s_idnMapping.GetUnicode(hostname, num, i - num));
						flag2 = true;
					}
					catch (ArgumentException)
					{
					}
				}
				if (!flag2)
				{
					ReadOnlySpan<char> source = hostname.AsSpan(num, i - num);
					int num2 = source.ToLowerInvariant(dest.AppendSpan(source.Length));
				}
			}
			num = i + 1;
		}
		while (num < hostname.Length);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsASCIILetterOrDigit(char character, ref bool notCanonical)
	{
		switch (character)
		{
		case '0':
		case '1':
		case '2':
		case '3':
		case '4':
		case '5':
		case '6':
		case '7':
		case '8':
		case '9':
		case 'a':
		case 'b':
		case 'c':
		case 'd':
		case 'e':
		case 'f':
		case 'g':
		case 'h':
		case 'i':
		case 'j':
		case 'k':
		case 'l':
		case 'm':
		case 'n':
		case 'o':
		case 'p':
		case 'q':
		case 'r':
		case 's':
		case 't':
		case 'u':
		case 'v':
		case 'w':
		case 'x':
		case 'y':
		case 'z':
			return true;
		case 'A':
		case 'B':
		case 'C':
		case 'D':
		case 'E':
		case 'F':
		case 'G':
		case 'H':
		case 'I':
		case 'J':
		case 'K':
		case 'L':
		case 'M':
		case 'N':
		case 'O':
		case 'P':
		case 'Q':
		case 'R':
		case 'S':
		case 'T':
		case 'U':
		case 'V':
		case 'W':
		case 'X':
		case 'Y':
		case 'Z':
			notCanonical = true;
			return true;
		default:
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsValidDomainLabelCharacter(char character, ref bool notCanonical)
	{
		switch (character)
		{
		case '-':
		case '0':
		case '1':
		case '2':
		case '3':
		case '4':
		case '5':
		case '6':
		case '7':
		case '8':
		case '9':
		case '_':
		case 'a':
		case 'b':
		case 'c':
		case 'd':
		case 'e':
		case 'f':
		case 'g':
		case 'h':
		case 'i':
		case 'j':
		case 'k':
		case 'l':
		case 'm':
		case 'n':
		case 'o':
		case 'p':
		case 'q':
		case 'r':
		case 's':
		case 't':
		case 'u':
		case 'v':
		case 'w':
		case 'x':
		case 'y':
		case 'z':
			return true;
		case 'A':
		case 'B':
		case 'C':
		case 'D':
		case 'E':
		case 'F':
		case 'G':
		case 'H':
		case 'I':
		case 'J':
		case 'K':
		case 'L':
		case 'M':
		case 'N':
		case 'O':
		case 'P':
		case 'Q':
		case 'R':
		case 'S':
		case 'T':
		case 'U':
		case 'V':
		case 'W':
		case 'X':
		case 'Y':
		case 'Z':
			notCanonical = true;
			return true;
		default:
			return false;
		}
	}

	internal static bool ContainsCharactersUnsafeForNormalizedHost(string host)
	{
		return host.IndexOfAny(s_UnsafeForNormalizedHost) != -1;
	}
}
