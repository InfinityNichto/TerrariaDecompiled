using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization;

public sealed class IdnMapping
{
	private bool _allowUnassigned;

	private bool _useStd3AsciiRules;

	private static readonly char[] s_dotSeparators = new char[4] { '.', '。', '．', '｡' };

	public bool AllowUnassigned
	{
		get
		{
			return _allowUnassigned;
		}
		set
		{
			_allowUnassigned = value;
		}
	}

	public bool UseStd3AsciiRules
	{
		get
		{
			return _useStd3AsciiRules;
		}
		set
		{
			_useStd3AsciiRules = value;
		}
	}

	private uint IcuFlags => (AllowUnassigned ? 1u : 0u) | (UseStd3AsciiRules ? 2u : 0u);

	private uint NlsFlags => (AllowUnassigned ? 1u : 0u) | (UseStd3AsciiRules ? 2u : 0u);

	public string GetAscii(string unicode)
	{
		return GetAscii(unicode, 0);
	}

	public string GetAscii(string unicode, int index)
	{
		if (unicode == null)
		{
			throw new ArgumentNullException("unicode");
		}
		return GetAscii(unicode, index, unicode.Length - index);
	}

	public unsafe string GetAscii(string unicode, int index, int count)
	{
		if (unicode == null)
		{
			throw new ArgumentNullException("unicode");
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (index > unicode.Length)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
		if (index > unicode.Length - count)
		{
			throw new ArgumentOutOfRangeException("unicode", SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (count == 0)
		{
			throw new ArgumentException(SR.Argument_IdnBadLabelSize, "unicode");
		}
		if (unicode[index + count - 1] == '\0')
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidCharSequence, index + count - 1), "unicode");
		}
		if (GlobalizationMode.Invariant)
		{
			return GetAsciiInvariant(unicode, index, count);
		}
		fixed (char* ptr = unicode)
		{
			if (!GlobalizationMode.UseNls)
			{
				return IcuGetAsciiCore(unicode, ptr + index, count);
			}
			return NlsGetAsciiCore(unicode, ptr + index, count);
		}
	}

	public string GetUnicode(string ascii)
	{
		return GetUnicode(ascii, 0);
	}

	public string GetUnicode(string ascii, int index)
	{
		if (ascii == null)
		{
			throw new ArgumentNullException("ascii");
		}
		return GetUnicode(ascii, index, ascii.Length - index);
	}

	public unsafe string GetUnicode(string ascii, int index, int count)
	{
		if (ascii == null)
		{
			throw new ArgumentNullException("ascii");
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (index > ascii.Length)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
		if (index > ascii.Length - count)
		{
			throw new ArgumentOutOfRangeException("ascii", SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (count > 0 && ascii[index + count - 1] == '\0')
		{
			throw new ArgumentException(SR.Argument_IdnBadPunycode, "ascii");
		}
		if (GlobalizationMode.Invariant)
		{
			return GetUnicodeInvariant(ascii, index, count);
		}
		fixed (char* ptr = ascii)
		{
			if (!GlobalizationMode.UseNls)
			{
				return IcuGetUnicodeCore(ascii, ptr + index, count);
			}
			return NlsGetUnicodeCore(ascii, ptr + index, count);
		}
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is IdnMapping idnMapping && _allowUnassigned == idnMapping._allowUnassigned)
		{
			return _useStd3AsciiRules == idnMapping._useStd3AsciiRules;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (_allowUnassigned ? 100 : 200) + (_useStd3AsciiRules ? 1000 : 2000);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static string GetStringForOutput(string originalString, char* input, int inputLength, char* output, int outputLength)
	{
		if (originalString.Length == inputLength && inputLength == outputLength && Ordinal.EqualsIgnoreCase(ref *input, ref *output, inputLength))
		{
			return originalString;
		}
		return new string(output, 0, outputLength);
	}

	private string GetAsciiInvariant(string unicode, int index, int count)
	{
		if (index > 0 || count < unicode.Length)
		{
			unicode = unicode.Substring(index, count);
		}
		if (ValidateStd3AndAscii(unicode, UseStd3AsciiRules, bCheckAscii: true))
		{
			return unicode;
		}
		if (unicode[^1] <= '\u001f')
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidCharSequence, unicode.Length - 1), "unicode");
		}
		if (UseStd3AsciiRules)
		{
			ValidateStd3AndAscii(unicode, bUseStd3: true, bCheckAscii: false);
		}
		return PunycodeEncode(unicode);
	}

	private static bool ValidateStd3AndAscii(string unicode, bool bUseStd3, bool bCheckAscii)
	{
		if (unicode.Length == 0)
		{
			throw new ArgumentException(SR.Argument_IdnBadLabelSize, "unicode");
		}
		int num = -1;
		for (int i = 0; i < unicode.Length; i++)
		{
			if (unicode[i] <= '\u001f')
			{
				throw new ArgumentException(SR.Format(SR.Argument_InvalidCharSequence, i), "unicode");
			}
			if (bCheckAscii && unicode[i] >= '\u007f')
			{
				return false;
			}
			if (IsDot(unicode[i]))
			{
				if (i == num + 1)
				{
					throw new ArgumentException(SR.Argument_IdnBadLabelSize, "unicode");
				}
				if (i - num > 64)
				{
					throw new ArgumentException(SR.Argument_IdnBadLabelSize, "unicode");
				}
				if (bUseStd3 && i > 0)
				{
					ValidateStd3(unicode[i - 1], bNextToDot: true);
				}
				num = i;
			}
			else if (bUseStd3)
			{
				ValidateStd3(unicode[i], i == num + 1);
			}
		}
		if (num == -1 && unicode.Length > 63)
		{
			throw new ArgumentException(SR.Argument_IdnBadLabelSize, "unicode");
		}
		if (unicode.Length > 255 - ((!IsDot(unicode[^1])) ? 1 : 0))
		{
			throw new ArgumentException(SR.Format(SR.Argument_IdnBadNameSize, 255 - ((!IsDot(unicode[^1])) ? 1 : 0)), "unicode");
		}
		if (bUseStd3 && !IsDot(unicode[^1]))
		{
			ValidateStd3(unicode[^1], bNextToDot: true);
		}
		return true;
	}

	private static string PunycodeEncode(string unicode)
	{
		if (unicode.Length == 0)
		{
			throw new ArgumentException(SR.Argument_IdnBadLabelSize, "unicode");
		}
		StringBuilder stringBuilder = new StringBuilder(unicode.Length);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		while (num < unicode.Length)
		{
			num = unicode.IndexOfAny(s_dotSeparators, num2);
			if (num < 0)
			{
				num = unicode.Length;
			}
			if (num == num2)
			{
				if (num == unicode.Length)
				{
					break;
				}
				throw new ArgumentException(SR.Argument_IdnBadLabelSize, "unicode");
			}
			stringBuilder.Append("xn--");
			bool flag = false;
			StrongBidiCategory bidiCategory = CharUnicodeInfo.GetBidiCategory(unicode, num2);
			if (bidiCategory == StrongBidiCategory.StrongRightToLeft)
			{
				flag = true;
				int num4 = num - 1;
				if (char.IsLowSurrogate(unicode, num4))
				{
					num4--;
				}
				bidiCategory = CharUnicodeInfo.GetBidiCategory(unicode, num4);
				if (bidiCategory != StrongBidiCategory.StrongRightToLeft)
				{
					throw new ArgumentException(SR.Argument_IdnBadBidi, "unicode");
				}
			}
			int num5 = 0;
			for (int i = num2; i < num; i++)
			{
				StrongBidiCategory bidiCategory2 = CharUnicodeInfo.GetBidiCategory(unicode, i);
				if (flag && bidiCategory2 == StrongBidiCategory.StrongLeftToRight)
				{
					throw new ArgumentException(SR.Argument_IdnBadBidi, "unicode");
				}
				if (!flag && bidiCategory2 == StrongBidiCategory.StrongRightToLeft)
				{
					throw new ArgumentException(SR.Argument_IdnBadBidi, "unicode");
				}
				if (Basic(unicode[i]))
				{
					stringBuilder.Append(EncodeBasic(unicode[i]));
					num5++;
				}
				else if (char.IsSurrogatePair(unicode, i))
				{
					i++;
				}
			}
			int num6 = num5;
			if (num6 == num - num2)
			{
				stringBuilder.Remove(num3, "xn--".Length);
			}
			else
			{
				if (unicode.Length - num2 >= "xn--".Length && unicode.Substring(num2, "xn--".Length).Equals("xn--", StringComparison.OrdinalIgnoreCase))
				{
					throw new ArgumentException(SR.Argument_IdnBadPunycode, "unicode");
				}
				int num7 = 0;
				if (num6 > 0)
				{
					stringBuilder.Append('-');
				}
				int num8 = 128;
				int num9 = 0;
				int num10 = 72;
				while (num5 < num - num2)
				{
					int num11 = 0;
					int num12 = 134217727;
					for (int j = num2; j < num; j += ((!IsSupplementary(num11)) ? 1 : 2))
					{
						num11 = char.ConvertToUtf32(unicode, j);
						if (num11 >= num8 && num11 < num12)
						{
							num12 = num11;
						}
					}
					num9 += (num12 - num8) * (num5 - num7 + 1);
					num8 = num12;
					for (int j = num2; j < num; j += ((!IsSupplementary(num11)) ? 1 : 2))
					{
						num11 = char.ConvertToUtf32(unicode, j);
						if (num11 < num8)
						{
							num9++;
						}
						if (num11 != num8)
						{
							continue;
						}
						int num13 = num9;
						int num14 = 36;
						while (true)
						{
							int num15 = ((num14 <= num10) ? 1 : ((num14 >= num10 + 26) ? 26 : (num14 - num10)));
							if (num13 < num15)
							{
								break;
							}
							stringBuilder.Append(EncodeDigit(num15 + (num13 - num15) % (36 - num15)));
							num13 = (num13 - num15) / (36 - num15);
							num14 += 36;
						}
						stringBuilder.Append(EncodeDigit(num13));
						num10 = Adapt(num9, num5 - num7 + 1, num5 == num6);
						num9 = 0;
						num5++;
						if (IsSupplementary(num12))
						{
							num5++;
							num7++;
						}
					}
					num9++;
					num8++;
				}
			}
			if (stringBuilder.Length - num3 > 63)
			{
				throw new ArgumentException(SR.Argument_IdnBadLabelSize, "unicode");
			}
			if (num != unicode.Length)
			{
				stringBuilder.Append('.');
			}
			num2 = num + 1;
			num3 = stringBuilder.Length;
		}
		if (stringBuilder.Length > 255 - ((!IsDot(unicode[^1])) ? 1 : 0))
		{
			throw new ArgumentException(SR.Format(SR.Argument_IdnBadNameSize, 255 - ((!IsDot(unicode[^1])) ? 1 : 0)), "unicode");
		}
		return stringBuilder.ToString();
	}

	private static bool IsDot(char c)
	{
		if (c != '.' && c != '。' && c != '．')
		{
			return c == '｡';
		}
		return true;
	}

	private static bool IsSupplementary(int cTest)
	{
		return cTest >= 65536;
	}

	private static bool Basic(uint cp)
	{
		return cp < 128;
	}

	private static void ValidateStd3(char c, bool bNextToDot)
	{
		if (c > ',')
		{
			switch (c)
			{
			default:
				if ((c < '[' || c > '`') && (c < '{' || c > '\u007f') && !(c == '-' && bNextToDot))
				{
					return;
				}
				break;
			case '/':
			case ':':
			case ';':
			case '<':
			case '=':
			case '>':
			case '?':
			case '@':
				break;
			}
		}
		throw new ArgumentException(SR.Format(SR.Argument_IdnBadStd3, c), "c");
	}

	private string GetUnicodeInvariant(string ascii, int index, int count)
	{
		if (index > 0 || count < ascii.Length)
		{
			ascii = ascii.Substring(index, count);
		}
		string text = PunycodeDecode(ascii);
		if (!ascii.Equals(GetAscii(text), StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentException(SR.Argument_IdnIllegalName, "ascii");
		}
		return text;
	}

	private static string PunycodeDecode(string ascii)
	{
		if (ascii.Length == 0)
		{
			throw new ArgumentException(SR.Argument_IdnBadLabelSize, "ascii");
		}
		if (ascii.Length > 255 - ((!IsDot(ascii[^1])) ? 1 : 0))
		{
			throw new ArgumentException(SR.Format(SR.Argument_IdnBadNameSize, 255 - ((!IsDot(ascii[^1])) ? 1 : 0)), "ascii");
		}
		StringBuilder stringBuilder = new StringBuilder(ascii.Length);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		while (num < ascii.Length)
		{
			num = ascii.IndexOf('.', num2);
			if (num < 0 || num > ascii.Length)
			{
				num = ascii.Length;
			}
			if (num == num2)
			{
				if (num == ascii.Length)
				{
					break;
				}
				throw new ArgumentException(SR.Argument_IdnBadLabelSize, "ascii");
			}
			if (num - num2 > 63)
			{
				throw new ArgumentException(SR.Argument_IdnBadLabelSize, "ascii");
			}
			if (ascii.Length < "xn--".Length + num2 || string.Compare(ascii, num2, "xn--", 0, "xn--".Length, StringComparison.OrdinalIgnoreCase) != 0)
			{
				stringBuilder.Append(ascii, num2, num - num2);
			}
			else
			{
				num2 += "xn--".Length;
				int num4 = ascii.LastIndexOf('-', num - 1);
				if (num4 == num - 1)
				{
					throw new ArgumentException(SR.Argument_IdnBadPunycode, "ascii");
				}
				int num5;
				if (num4 <= num2)
				{
					num5 = 0;
				}
				else
				{
					num5 = num4 - num2;
					for (int i = num2; i < num2 + num5; i++)
					{
						if (ascii[i] > '\u007f')
						{
							throw new ArgumentException(SR.Argument_IdnBadPunycode, "ascii");
						}
						stringBuilder.Append((char)((ascii[i] >= 'A' && ascii[i] <= 'Z') ? (ascii[i] - 65 + 97) : ascii[i]));
					}
				}
				int num6 = num2 + ((num5 > 0) ? (num5 + 1) : 0);
				int num7 = 128;
				int num8 = 72;
				int num9 = 0;
				int num10 = 0;
				while (num6 < num)
				{
					int num11 = num9;
					int num12 = 1;
					int num13 = 36;
					while (true)
					{
						if (num6 >= num)
						{
							throw new ArgumentException(SR.Argument_IdnBadPunycode, "ascii");
						}
						int num14 = DecodeDigit(ascii[num6++]);
						if (num14 > (134217727 - num9) / num12)
						{
							throw new ArgumentException(SR.Argument_IdnBadPunycode, "ascii");
						}
						num9 += num14 * num12;
						int num15 = ((num13 <= num8) ? 1 : ((num13 >= num8 + 26) ? 26 : (num13 - num8)));
						if (num14 < num15)
						{
							break;
						}
						if (num12 > 134217727 / (36 - num15))
						{
							throw new ArgumentException(SR.Argument_IdnBadPunycode, "ascii");
						}
						num12 *= 36 - num15;
						num13 += 36;
					}
					num8 = Adapt(num9 - num11, stringBuilder.Length - num3 - num10 + 1, num11 == 0);
					if (num9 / (stringBuilder.Length - num3 - num10 + 1) > 134217727 - num7)
					{
						throw new ArgumentException(SR.Argument_IdnBadPunycode, "ascii");
					}
					num7 += num9 / (stringBuilder.Length - num3 - num10 + 1);
					num9 %= stringBuilder.Length - num3 - num10 + 1;
					if (num7 < 0 || num7 > 1114111 || (num7 >= 55296 && num7 <= 57343))
					{
						throw new ArgumentException(SR.Argument_IdnBadPunycode, "ascii");
					}
					string value = char.ConvertFromUtf32(num7);
					int num17;
					if (num10 > 0)
					{
						int num16 = num9;
						num17 = num3;
						while (num16 > 0)
						{
							if (num17 >= stringBuilder.Length)
							{
								throw new ArgumentException(SR.Argument_IdnBadPunycode, "ascii");
							}
							if (char.IsSurrogate(stringBuilder[num17]))
							{
								num17++;
							}
							num16--;
							num17++;
						}
					}
					else
					{
						num17 = num3 + num9;
					}
					stringBuilder.Insert(num17, value);
					if (IsSupplementary(num7))
					{
						num10++;
					}
					num9++;
				}
				bool flag = false;
				StrongBidiCategory bidiCategory = CharUnicodeInfo.GetBidiCategory(stringBuilder, num3);
				if (bidiCategory == StrongBidiCategory.StrongRightToLeft)
				{
					flag = true;
				}
				for (int j = num3; j < stringBuilder.Length; j++)
				{
					if (!char.IsLowSurrogate(stringBuilder[j]))
					{
						bidiCategory = CharUnicodeInfo.GetBidiCategory(stringBuilder, j);
						if ((flag && bidiCategory == StrongBidiCategory.StrongLeftToRight) || (!flag && bidiCategory == StrongBidiCategory.StrongRightToLeft))
						{
							throw new ArgumentException(SR.Argument_IdnBadBidi, "ascii");
						}
					}
				}
				if (flag && bidiCategory != StrongBidiCategory.StrongRightToLeft)
				{
					throw new ArgumentException(SR.Argument_IdnBadBidi, "ascii");
				}
			}
			if (num - num2 > 63)
			{
				throw new ArgumentException(SR.Argument_IdnBadLabelSize, "ascii");
			}
			if (num != ascii.Length)
			{
				stringBuilder.Append('.');
			}
			num2 = num + 1;
			num3 = stringBuilder.Length;
		}
		if (stringBuilder.Length > 255 - ((!IsDot(stringBuilder[stringBuilder.Length - 1])) ? 1 : 0))
		{
			throw new ArgumentException(SR.Format(SR.Argument_IdnBadNameSize, 255 - ((!IsDot(stringBuilder[stringBuilder.Length - 1])) ? 1 : 0)), "ascii");
		}
		return stringBuilder.ToString();
	}

	private static int DecodeDigit(char cp)
	{
		if (cp >= '0' && cp <= '9')
		{
			return cp - 48 + 26;
		}
		if (cp >= 'a' && cp <= 'z')
		{
			return cp - 97;
		}
		if (cp >= 'A' && cp <= 'Z')
		{
			return cp - 65;
		}
		throw new ArgumentException(SR.Argument_IdnBadPunycode, "cp");
	}

	private static int Adapt(int delta, int numpoints, bool firsttime)
	{
		delta = (firsttime ? (delta / 700) : (delta / 2));
		delta += delta / numpoints;
		uint num = 0u;
		while (delta > 455)
		{
			delta /= 35;
			num += 36;
		}
		return (int)(num + 36 * delta / (delta + 38));
	}

	private static char EncodeBasic(char bcp)
	{
		if (HasUpperCaseFlag(bcp))
		{
			bcp = (char)(bcp + 32);
		}
		return bcp;
	}

	private static bool HasUpperCaseFlag(char punychar)
	{
		if (punychar >= 'A')
		{
			return punychar <= 'Z';
		}
		return false;
	}

	private static char EncodeDigit(int d)
	{
		if (d > 25)
		{
			return (char)(d - 26 + 48);
		}
		return (char)(d + 97);
	}

	private unsafe string IcuGetAsciiCore(string unicodeString, char* unicode, int count)
	{
		uint icuFlags = IcuFlags;
		CheckInvalidIdnCharacters(unicode, count, icuFlags, "unicode");
		int num = (int)Math.Min((long)count * 3L + 4, 512L);
		int num2;
		if (num < 512)
		{
			char* ptr = stackalloc char[num];
			num2 = Interop.Globalization.ToAscii(icuFlags, unicode, count, ptr, num);
			if (num2 > 0 && num2 <= num)
			{
				return GetStringForOutput(unicodeString, unicode, count, ptr, num2);
			}
		}
		else
		{
			num2 = Interop.Globalization.ToAscii(icuFlags, unicode, count, null, 0);
		}
		if (num2 == 0)
		{
			throw new ArgumentException(SR.Argument_IdnIllegalName, "unicode");
		}
		char[] array = new char[num2];
		fixed (char* ptr2 = &array[0])
		{
			num2 = Interop.Globalization.ToAscii(icuFlags, unicode, count, ptr2, num2);
			if (num2 == 0 || num2 > array.Length)
			{
				throw new ArgumentException(SR.Argument_IdnIllegalName, "unicode");
			}
			return GetStringForOutput(unicodeString, unicode, count, ptr2, num2);
		}
	}

	private unsafe string IcuGetUnicodeCore(string asciiString, char* ascii, int count)
	{
		uint icuFlags = IcuFlags;
		CheckInvalidIdnCharacters(ascii, count, icuFlags, "ascii");
		if (count < 512)
		{
			char* output = stackalloc char[count];
			return IcuGetUnicodeCore(asciiString, ascii, count, icuFlags, output, count, reattempt: true);
		}
		char[] array = new char[count];
		fixed (char* output2 = &array[0])
		{
			return IcuGetUnicodeCore(asciiString, ascii, count, icuFlags, output2, count, reattempt: true);
		}
	}

	private unsafe string IcuGetUnicodeCore(string asciiString, char* ascii, int count, uint flags, char* output, int outputLength, bool reattempt)
	{
		int num = Interop.Globalization.ToUnicode(flags, ascii, count, output, outputLength);
		if (num == 0)
		{
			throw new ArgumentException(SR.Argument_IdnIllegalName, "ascii");
		}
		if (num <= outputLength)
		{
			return GetStringForOutput(asciiString, ascii, count, output, num);
		}
		if (reattempt)
		{
			fixed (char* output2 = new char[num])
			{
				return IcuGetUnicodeCore(asciiString, ascii, count, flags, output2, num, reattempt: false);
			}
		}
		throw new ArgumentException(SR.Argument_IdnIllegalName, "ascii");
	}

	private unsafe static void CheckInvalidIdnCharacters(char* s, int count, uint flags, string paramName)
	{
		if ((flags & 2u) != 0)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			char c = s[i];
			if (c <= '\u001f' || c == '\u007f')
			{
				throw new ArgumentException(SR.Argument_IdnIllegalName, paramName);
			}
		}
	}

	private unsafe string NlsGetAsciiCore(string unicodeString, char* unicode, int count)
	{
		uint nlsFlags = NlsFlags;
		int num = Interop.Normaliz.IdnToAscii(nlsFlags, unicode, count, null, 0);
		if (num == 0)
		{
			ThrowForZeroLength(unicode: true);
		}
		if (num < 512)
		{
			char* output = stackalloc char[num];
			return NlsGetAsciiCore(unicodeString, unicode, count, nlsFlags, output, num);
		}
		char[] array = new char[num];
		fixed (char* output2 = &array[0])
		{
			return NlsGetAsciiCore(unicodeString, unicode, count, nlsFlags, output2, num);
		}
	}

	private unsafe static string NlsGetAsciiCore(string unicodeString, char* unicode, int count, uint flags, char* output, int outputLength)
	{
		int num = Interop.Normaliz.IdnToAscii(flags, unicode, count, output, outputLength);
		if (num == 0)
		{
			ThrowForZeroLength(unicode: true);
		}
		return GetStringForOutput(unicodeString, unicode, count, output, num);
	}

	private unsafe string NlsGetUnicodeCore(string asciiString, char* ascii, int count)
	{
		uint nlsFlags = NlsFlags;
		int num = Interop.Normaliz.IdnToUnicode(nlsFlags, ascii, count, null, 0);
		if (num == 0)
		{
			ThrowForZeroLength(unicode: false);
		}
		if (num < 512)
		{
			char* output = stackalloc char[num];
			return NlsGetUnicodeCore(asciiString, ascii, count, nlsFlags, output, num);
		}
		char[] array = new char[num];
		fixed (char* output2 = &array[0])
		{
			return NlsGetUnicodeCore(asciiString, ascii, count, nlsFlags, output2, num);
		}
	}

	private unsafe static string NlsGetUnicodeCore(string asciiString, char* ascii, int count, uint flags, char* output, int outputLength)
	{
		int num = Interop.Normaliz.IdnToUnicode(flags, ascii, count, output, outputLength);
		if (num == 0)
		{
			ThrowForZeroLength(unicode: false);
		}
		return GetStringForOutput(asciiString, ascii, count, output, num);
	}

	[DoesNotReturn]
	private static void ThrowForZeroLength(bool unicode)
	{
		int lastPInvokeError = Marshal.GetLastPInvokeError();
		throw new ArgumentException((lastPInvokeError == 123) ? SR.Argument_IdnIllegalName : (unicode ? SR.Argument_InvalidCharSequenceNoIndex : SR.Argument_IdnBadPunycode), unicode ? "unicode" : "ascii");
	}
}
