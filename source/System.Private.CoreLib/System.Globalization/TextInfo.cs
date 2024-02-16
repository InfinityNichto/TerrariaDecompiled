using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Unicode;
using Internal.Runtime.CompilerServices;

namespace System.Globalization;

public sealed class TextInfo : ICloneable, IDeserializationCallback
{
	private enum Tristate : byte
	{
		NotInitialized,
		False,
		True
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct ToUpperConversion
	{
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private readonly struct ToLowerConversion
	{
	}

	private string _listSeparator;

	private bool _isReadOnly;

	private readonly string _cultureName;

	private readonly CultureData _cultureData;

	private readonly string _textInfoName;

	private Tristate _isAsciiCasingSameAsInvariant;

	internal static readonly TextInfo Invariant = new TextInfo(CultureData.Invariant, readOnly: true)
	{
		_isAsciiCasingSameAsInvariant = Tristate.True
	};

	private Tristate _needsTurkishCasing;

	private IntPtr _sortHandle;

	public int ANSICodePage => _cultureData.ANSICodePage;

	public int OEMCodePage => _cultureData.OEMCodePage;

	public int MacCodePage => _cultureData.MacCodePage;

	public int EBCDICCodePage => _cultureData.EBCDICCodePage;

	public int LCID => CultureInfo.GetCultureInfo(_textInfoName).LCID;

	public string CultureName => _textInfoName;

	public bool IsReadOnly => _isReadOnly;

	public string ListSeparator
	{
		get
		{
			return _listSeparator ?? (_listSeparator = _cultureData.ListSeparator);
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			VerifyWritable();
			_listSeparator = value;
		}
	}

	private bool IsAsciiCasingSameAsInvariant
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (_isAsciiCasingSameAsInvariant == Tristate.NotInitialized)
			{
				PopulateIsAsciiCasingSameAsInvariant();
			}
			return _isAsciiCasingSameAsInvariant == Tristate.True;
		}
	}

	public bool IsRightToLeft => _cultureData.IsRightToLeft;

	private bool IsInvariant => _cultureName.Length == 0;

	internal TextInfo(CultureData cultureData)
	{
		_cultureData = cultureData;
		_cultureName = _cultureData.CultureName;
		_textInfoName = _cultureData.TextInfoName;
		if (GlobalizationMode.UseNls)
		{
			_sortHandle = CompareInfo.NlsGetSortHandle(_textInfoName);
		}
	}

	private TextInfo(CultureData cultureData, bool readOnly)
		: this(cultureData)
	{
		SetReadOnlyState(readOnly);
	}

	void IDeserializationCallback.OnDeserialization(object sender)
	{
		throw new PlatformNotSupportedException();
	}

	public object Clone()
	{
		object obj = MemberwiseClone();
		((TextInfo)obj).SetReadOnlyState(readOnly: false);
		return obj;
	}

	public static TextInfo ReadOnly(TextInfo textInfo)
	{
		if (textInfo == null)
		{
			throw new ArgumentNullException("textInfo");
		}
		if (textInfo.IsReadOnly)
		{
			return textInfo;
		}
		TextInfo textInfo2 = (TextInfo)textInfo.MemberwiseClone();
		textInfo2.SetReadOnlyState(readOnly: true);
		return textInfo2;
	}

	private void VerifyWritable()
	{
		if (_isReadOnly)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
		}
	}

	internal void SetReadOnlyState(bool readOnly)
	{
		_isReadOnly = readOnly;
	}

	public char ToLower(char c)
	{
		if (GlobalizationMode.Invariant)
		{
			return InvariantModeCasing.ToLower(c);
		}
		if (UnicodeUtility.IsAsciiCodePoint(c) && IsAsciiCasingSameAsInvariant)
		{
			return ToLowerAsciiInvariant(c);
		}
		return ChangeCase(c, toUpper: false);
	}

	internal static char ToLowerInvariant(char c)
	{
		if (GlobalizationMode.Invariant)
		{
			return InvariantModeCasing.ToLower(c);
		}
		if (UnicodeUtility.IsAsciiCodePoint(c))
		{
			return ToLowerAsciiInvariant(c);
		}
		return Invariant.ChangeCase(c, toUpper: false);
	}

	public string ToLower(string str)
	{
		if (str == null)
		{
			throw new ArgumentNullException("str");
		}
		if (GlobalizationMode.Invariant)
		{
			return InvariantModeCasing.ToLower(str);
		}
		return ChangeCaseCommon<ToLowerConversion>(str);
	}

	private unsafe char ChangeCase(char c, bool toUpper)
	{
		char result = '\0';
		ChangeCaseCore(&c, 1, &result, 1, toUpper);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void ChangeCaseToLower(ReadOnlySpan<char> source, Span<char> destination)
	{
		ChangeCaseCommon<ToLowerConversion>(ref MemoryMarshal.GetReference(source), ref MemoryMarshal.GetReference(destination), source.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void ChangeCaseToUpper(ReadOnlySpan<char> source, Span<char> destination)
	{
		ChangeCaseCommon<ToUpperConversion>(ref MemoryMarshal.GetReference(source), ref MemoryMarshal.GetReference(destination), source.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ChangeCaseCommon<TConversion>(ReadOnlySpan<char> source, Span<char> destination) where TConversion : struct
	{
		ChangeCaseCommon<TConversion>(ref MemoryMarshal.GetReference(source), ref MemoryMarshal.GetReference(destination), source.Length);
	}

	private unsafe void ChangeCaseCommon<TConversion>(ref char source, ref char destination, int charCount) where TConversion : struct
	{
		bool flag = typeof(TConversion) == typeof(ToUpperConversion);
		if (charCount == 0)
		{
			return;
		}
		fixed (char* ptr = &source)
		{
			fixed (char* ptr2 = &destination)
			{
				nuint num = 0u;
				if (IsAsciiCasingSameAsInvariant)
				{
					if (charCount < 4)
					{
						goto IL_00e5;
					}
					nuint num2 = (uint)(charCount - 4);
					while (true)
					{
						uint value = Unsafe.ReadUnaligned<uint>(ptr + num);
						if (!Utf16Utility.AllCharsInUInt32AreAscii(value))
						{
							break;
						}
						value = (flag ? Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(value) : Utf16Utility.ConvertAllAsciiCharsInUInt32ToLowercase(value));
						Unsafe.WriteUnaligned(ptr2 + num, value);
						value = Unsafe.ReadUnaligned<uint>(ptr + num + 2);
						if (Utf16Utility.AllCharsInUInt32AreAscii(value))
						{
							value = (flag ? Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(value) : Utf16Utility.ConvertAllAsciiCharsInUInt32ToLowercase(value));
							Unsafe.WriteUnaligned(ptr2 + num + 2, value);
							num += 4;
							if (num <= num2)
							{
								continue;
							}
							goto IL_00e5;
						}
						num += 2;
						break;
					}
					goto IL_0171;
				}
				goto IL_0178;
				IL_0178:
				ChangeCaseCore(ptr + num, charCount, ptr2 + num, charCount, flag);
				return;
				IL_0171:
				charCount -= (int)num;
				goto IL_0178;
				IL_00e5:
				if (((uint)charCount & 2u) != 0)
				{
					uint value2 = Unsafe.ReadUnaligned<uint>(ptr + num);
					if (!Utf16Utility.AllCharsInUInt32AreAscii(value2))
					{
						goto IL_0171;
					}
					value2 = (flag ? Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(value2) : Utf16Utility.ConvertAllAsciiCharsInUInt32ToLowercase(value2));
					Unsafe.WriteUnaligned(ptr2 + num, value2);
					num += 2;
				}
				if (((uint)charCount & (true ? 1u : 0u)) != 0)
				{
					uint num3 = ptr[num];
					if (num3 <= 127)
					{
						num3 = (flag ? Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(num3) : Utf16Utility.ConvertAllAsciiCharsInUInt32ToLowercase(num3));
						ptr2[num] = (char)num3;
						return;
					}
					goto IL_0171;
				}
			}
		}
	}

	private unsafe string ChangeCaseCommon<TConversion>(string source) where TConversion : struct
	{
		bool flag = typeof(TConversion) == typeof(ToUpperConversion);
		if (source.Length == 0)
		{
			return string.Empty;
		}
		fixed (char* ptr = source)
		{
			nuint num = 0u;
			if (IsAsciiCasingSameAsInvariant)
			{
				if (source.Length < 2)
				{
					goto IL_0095;
				}
				nuint num2 = (uint)(source.Length - 2);
				while (true)
				{
					uint value = Unsafe.ReadUnaligned<uint>(ptr + num);
					if (!Utf16Utility.AllCharsInUInt32AreAscii(value))
					{
						break;
					}
					if (!(flag ? Utf16Utility.UInt32ContainsAnyLowercaseAsciiChar(value) : Utf16Utility.UInt32ContainsAnyUppercaseAsciiChar(value)))
					{
						num += 2;
						if (num <= num2)
						{
							continue;
						}
						goto IL_0095;
					}
					goto IL_00d1;
				}
			}
			goto IL_0121;
			IL_0121:
			string text = string.FastAllocateString(source.Length);
			if (num != 0)
			{
				Span<char> destination = new Span<char>(ref text.GetRawStringData(), text.Length);
				source.AsSpan(0, (int)num).CopyTo(destination);
			}
			fixed (char* ptr2 = text)
			{
				ChangeCaseCore(ptr + num, source.Length - (int)num, ptr2 + num, text.Length - (int)num, flag);
			}
			return text;
			IL_0095:
			if (((uint)source.Length & (true ? 1u : 0u)) != 0)
			{
				uint num3 = ptr[num];
				if (num3 > 127)
				{
					goto IL_0121;
				}
				if (flag ? (num3 - 97 <= 25) : (num3 - 65 <= 25))
				{
					goto IL_00d1;
				}
			}
			return source;
			IL_00d1:
			string text2 = string.FastAllocateString(source.Length);
			Span<char> destination2 = new Span<char>(ref text2.GetRawStringData(), text2.Length);
			source.AsSpan(0, (int)num).CopyTo(destination2);
			ChangeCaseCommon<TConversion>(source.AsSpan((int)num), destination2.Slice((int)num));
			return text2;
		}
	}

	internal unsafe static string ToLowerAsciiInvariant(string s)
	{
		if (s.Length == 0)
		{
			return string.Empty;
		}
		fixed (char* ptr = s)
		{
			int i;
			for (i = 0; i < s.Length && (uint)(ptr[i] - 65) > 25u; i++)
			{
			}
			if (i >= s.Length)
			{
				return s;
			}
			string text = string.FastAllocateString(s.Length);
			fixed (char* ptr2 = text)
			{
				for (int j = 0; j < i; j++)
				{
					ptr2[j] = ptr[j];
				}
				ptr2[i] = (char)(ptr[i] | 0x20u);
				for (i++; i < s.Length; i++)
				{
					ptr2[i] = ToLowerAsciiInvariant(ptr[i]);
				}
			}
			return text;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static char ToLowerAsciiInvariant(char c)
	{
		if (UnicodeUtility.IsInRangeInclusive(c, 65u, 90u))
		{
			c = (char)(byte)(c | 0x20u);
		}
		return c;
	}

	public char ToUpper(char c)
	{
		if (GlobalizationMode.Invariant)
		{
			return InvariantModeCasing.ToUpper(c);
		}
		if (UnicodeUtility.IsAsciiCodePoint(c) && IsAsciiCasingSameAsInvariant)
		{
			return ToUpperAsciiInvariant(c);
		}
		return ChangeCase(c, toUpper: true);
	}

	internal static char ToUpperInvariant(char c)
	{
		if (GlobalizationMode.Invariant)
		{
			return InvariantModeCasing.ToUpper(c);
		}
		if (UnicodeUtility.IsAsciiCodePoint(c))
		{
			return ToUpperAsciiInvariant(c);
		}
		return Invariant.ChangeCase(c, toUpper: true);
	}

	public string ToUpper(string str)
	{
		if (str == null)
		{
			throw new ArgumentNullException("str");
		}
		if (GlobalizationMode.Invariant)
		{
			return InvariantModeCasing.ToUpper(str);
		}
		return ChangeCaseCommon<ToUpperConversion>(str);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static char ToUpperAsciiInvariant(char c)
	{
		if (UnicodeUtility.IsInRangeInclusive(c, 97u, 122u))
		{
			c = (char)(c & 0x5Fu);
		}
		return c;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void PopulateIsAsciiCasingSameAsInvariant()
	{
		bool flag = CultureInfo.GetCultureInfo(_textInfoName).CompareInfo.Compare("abcdefghijklmnopqrstuvwxyz", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", CompareOptions.IgnoreCase) == 0;
		_isAsciiCasingSameAsInvariant = ((!flag) ? Tristate.False : Tristate.True);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is TextInfo textInfo)
		{
			return CultureName.Equals(textInfo.CultureName);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return CultureName.GetHashCode();
	}

	public override string ToString()
	{
		return "TextInfo - " + _cultureData.CultureName;
	}

	public string ToTitleCase(string str)
	{
		if (str == null)
		{
			throw new ArgumentNullException("str");
		}
		if (str.Length == 0)
		{
			return str;
		}
		StringBuilder result = new StringBuilder();
		string text = null;
		bool flag = CultureName.StartsWith("nl-", StringComparison.OrdinalIgnoreCase);
		int num;
		for (num = 0; num < str.Length; num++)
		{
			UnicodeCategory unicodeCategoryInternal = CharUnicodeInfo.GetUnicodeCategoryInternal(str, num, out var charLength);
			if (char.CheckLetter(unicodeCategoryInternal))
			{
				if (flag && num < str.Length - 1 && (str[num] == 'i' || str[num] == 'I') && (str[num + 1] == 'j' || str[num + 1] == 'J'))
				{
					result.Append("IJ");
					num += 2;
				}
				else
				{
					num = AddTitlecaseLetter(ref result, ref str, num, charLength) + 1;
				}
				int num2 = num;
				bool flag2 = unicodeCategoryInternal == UnicodeCategory.LowercaseLetter;
				while (num < str.Length)
				{
					unicodeCategoryInternal = CharUnicodeInfo.GetUnicodeCategoryInternal(str, num, out charLength);
					if (IsLetterCategory(unicodeCategoryInternal))
					{
						if (unicodeCategoryInternal == UnicodeCategory.LowercaseLetter)
						{
							flag2 = true;
						}
						num += charLength;
					}
					else if (str[num] == '\'')
					{
						num++;
						if (flag2)
						{
							if (text == null)
							{
								text = ToLower(str);
							}
							result.Append(text, num2, num - num2);
						}
						else
						{
							result.Append(str, num2, num - num2);
						}
						num2 = num;
						flag2 = true;
					}
					else
					{
						if (IsWordSeparator(unicodeCategoryInternal))
						{
							break;
						}
						num += charLength;
					}
				}
				int num3 = num - num2;
				if (num3 > 0)
				{
					if (flag2)
					{
						if (text == null)
						{
							text = ToLower(str);
						}
						result.Append(text, num2, num3);
					}
					else
					{
						result.Append(str, num2, num3);
					}
				}
				if (num < str.Length)
				{
					num = AddNonLetter(ref result, ref str, num, charLength);
				}
			}
			else
			{
				num = AddNonLetter(ref result, ref str, num, charLength);
			}
		}
		return result.ToString();
	}

	private static int AddNonLetter(ref StringBuilder result, ref string input, int inputIndex, int charLen)
	{
		if (charLen == 2)
		{
			result.Append(input[inputIndex++]);
			result.Append(input[inputIndex]);
		}
		else
		{
			result.Append(input[inputIndex]);
		}
		return inputIndex;
	}

	private int AddTitlecaseLetter(ref StringBuilder result, ref string input, int inputIndex, int charLen)
	{
		if (charLen == 2)
		{
			ReadOnlySpan<char> source = input.AsSpan(inputIndex, 2);
			if (GlobalizationMode.Invariant)
			{
				SurrogateCasing.ToUpper(source[0], source[1], out var hr, out var lr);
				result.Append(hr);
				result.Append(lr);
			}
			else
			{
				Span<char> span = stackalloc char[2];
				ChangeCaseToUpper(source, span);
				result.Append(span);
			}
			inputIndex++;
		}
		else
		{
			switch (input[inputIndex])
			{
			case 'Ǆ':
			case 'ǅ':
			case 'ǆ':
				result.Append('ǅ');
				break;
			case 'Ǉ':
			case 'ǈ':
			case 'ǉ':
				result.Append('ǈ');
				break;
			case 'Ǌ':
			case 'ǋ':
			case 'ǌ':
				result.Append('ǋ');
				break;
			case 'Ǳ':
			case 'ǲ':
			case 'ǳ':
				result.Append('ǲ');
				break;
			default:
				result.Append(GlobalizationMode.Invariant ? InvariantModeCasing.ToUpper(input[inputIndex]) : ToUpper(input[inputIndex]));
				break;
			}
		}
		return inputIndex;
	}

	private unsafe void ChangeCaseCore(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper)
	{
		if (GlobalizationMode.UseNls)
		{
			NlsChangeCase(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
		}
		else
		{
			IcuChangeCase(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
		}
	}

	private static bool IsWordSeparator(UnicodeCategory category)
	{
		return (0x1FFCF800 & (1 << (int)category)) != 0;
	}

	private static bool IsLetterCategory(UnicodeCategory uc)
	{
		if (uc != 0 && uc != UnicodeCategory.LowercaseLetter && uc != UnicodeCategory.TitlecaseLetter && uc != UnicodeCategory.ModifierLetter)
		{
			return uc == UnicodeCategory.OtherLetter;
		}
		return true;
	}

	private static bool NeedsTurkishCasing(string localeName)
	{
		return CultureInfo.GetCultureInfo(localeName).CompareInfo.Compare("ı", "I", CompareOptions.IgnoreCase) == 0;
	}

	internal unsafe void IcuChangeCase(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper)
	{
		if (IsInvariant)
		{
			Interop.Globalization.ChangeCaseInvariant(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
			return;
		}
		if (_needsTurkishCasing == Tristate.NotInitialized)
		{
			_needsTurkishCasing = ((!NeedsTurkishCasing(_textInfoName)) ? Tristate.False : Tristate.True);
		}
		if (_needsTurkishCasing == Tristate.True)
		{
			Interop.Globalization.ChangeCaseTurkish(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
		}
		else
		{
			Interop.Globalization.ChangeCase(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
		}
	}

	private unsafe void NlsChangeCase(char* pSource, int pSourceLen, char* pResult, int pResultLen, bool toUpper)
	{
		uint num = ((!IsInvariantLocale(_textInfoName)) ? 16777216u : 0u);
		if (Interop.Kernel32.LCMapStringEx((_sortHandle != IntPtr.Zero) ? null : _textInfoName, num | (toUpper ? 512u : 256u), pSource, pSourceLen, pResult, pSourceLen, null, null, _sortHandle) == 0)
		{
			throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
		}
	}

	private static bool IsInvariantLocale(string localeName)
	{
		return localeName == "";
	}
}
