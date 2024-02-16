using System.Runtime.CompilerServices;
using System.Text;

namespace System;

internal static class IriHelper
{
	internal static bool CheckIriUnicodeRange(char unicode, bool isQuery)
	{
		if (!IsInInclusiveRange(unicode, 160u, 55295u) && !IsInInclusiveRange(unicode, 63744u, 64975u) && !IsInInclusiveRange(unicode, 65008u, 65519u))
		{
			if (isQuery)
			{
				return IsInInclusiveRange(unicode, 57344u, 63743u);
			}
			return false;
		}
		return true;
	}

	internal static bool CheckIriUnicodeRange(char highSurr, char lowSurr, out bool isSurrogatePair, bool isQuery)
	{
		if (Rune.TryCreate(highSurr, lowSurr, out var result))
		{
			isSurrogatePair = true;
			if ((result.Value & 0xFFFF) < 65534 && (uint)(result.Value - 917504) >= 4096u)
			{
				if (!isQuery)
				{
					return result.Value < 983040;
				}
				return true;
			}
			return false;
		}
		isSurrogatePair = false;
		return false;
	}

	internal static bool CheckIriUnicodeRange(uint value, bool isQuery)
	{
		if (value <= 65535)
		{
			if (!IsInInclusiveRange(value, 160u, 55295u) && !IsInInclusiveRange(value, 63744u, 64975u) && !IsInInclusiveRange(value, 65008u, 65519u))
			{
				if (isQuery)
				{
					return IsInInclusiveRange(value, 57344u, 63743u);
				}
				return false;
			}
			return true;
		}
		if ((value & 0xFFFF) < 65534 && !IsInInclusiveRange(value, 917504u, 921599u))
		{
			if (!isQuery)
			{
				return value < 983040;
			}
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsInInclusiveRange(uint value, uint min, uint max)
	{
		return value - min <= max - min;
	}

	internal static bool CheckIsReserved(char ch, UriComponents component)
	{
		if ((UriComponents.AbsoluteUri & component) == 0)
		{
			if (component == (UriComponents)0)
			{
				return UriHelper.IsGenDelim(ch);
			}
			return false;
		}
		return ";/?:@&=+$,#[]!'()*".Contains(ch);
	}

	internal unsafe static string EscapeUnescapeIri(char* pInput, int start, int end, UriComponents component)
	{
		int num = end - start;
		System.Text.ValueStringBuilder valueStringBuilder;
		if (num <= 512)
		{
			Span<char> initialBuffer = stackalloc char[512];
			valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		}
		else
		{
			valueStringBuilder = new System.Text.ValueStringBuilder(num);
		}
		System.Text.ValueStringBuilder to = valueStringBuilder;
		Span<byte> destination = stackalloc byte[4];
		for (int i = start; i < end; i++)
		{
			char c = pInput[i];
			if (c == '%')
			{
				if (end - i > 2)
				{
					c = UriHelper.DecodeHexChars(pInput[i + 1], pInput[i + 2]);
					if (c == '\uffff' || c == '%' || CheckIsReserved(c, component) || UriHelper.IsNotSafeForUnescape(c))
					{
						to.Append(pInput[i++]);
						to.Append(pInput[i++]);
						to.Append(pInput[i]);
					}
					else if (c <= '\u007f')
					{
						to.Append(c);
						i += 2;
					}
					else
					{
						int num2 = PercentEncodingHelper.UnescapePercentEncodedUTF8Sequence(pInput + i, end - i, ref to, component == UriComponents.Query, iriParsing: true);
						i += num2 - 1;
					}
				}
				else
				{
					to.Append(pInput[i]);
				}
			}
			else if (c > '\u007f')
			{
				bool isSurrogatePair = false;
				char c2 = '\0';
				bool flag;
				if (char.IsHighSurrogate(c) && i + 1 < end)
				{
					c2 = pInput[i + 1];
					flag = CheckIriUnicodeRange(c, c2, out isSurrogatePair, component == UriComponents.Query);
				}
				else
				{
					flag = CheckIriUnicodeRange(c, component == UriComponents.Query);
				}
				if (flag)
				{
					to.Append(c);
					if (isSurrogatePair)
					{
						to.Append(c2);
					}
				}
				else
				{
					Rune result;
					if (isSurrogatePair)
					{
						result = new Rune(c, c2);
					}
					else if (!Rune.TryCreate(c, out result))
					{
						result = Rune.ReplacementChar;
					}
					Span<byte> span = destination[..result.EncodeToUtf8(destination)];
					Span<byte> span2 = span;
					for (int j = 0; j < span2.Length; j++)
					{
						byte b = span2[j];
						UriHelper.EscapeAsciiChar(b, ref to);
					}
				}
				if (isSurrogatePair)
				{
					i++;
				}
			}
			else
			{
				to.Append(pInput[i]);
			}
		}
		return to.ToString();
	}
}
