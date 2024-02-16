using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Char : IComparable, IComparable<char>, IEquatable<char>, IConvertible, ISpanFormattable, IFormattable, IBinaryInteger<char>, IBinaryNumber<char>, IBitwiseOperators<char, char, char>, INumber<char>, IAdditionOperators<char, char, char>, IAdditiveIdentity<char, char>, IComparisonOperators<char, char>, IEqualityOperators<char, char>, IDecrementOperators<char>, IDivisionOperators<char, char, char>, IIncrementOperators<char>, IModulusOperators<char, char, char>, IMultiplicativeIdentity<char, char>, IMultiplyOperators<char, char, char>, ISpanParseable<char>, IParseable<char>, ISubtractionOperators<char, char, char>, IUnaryNegationOperators<char, char>, IUnaryPlusOperators<char, char>, IShiftOperators<char, char>, IMinMaxValue<char>, IUnsignedNumber<char>
{
	private readonly char m_value;

	public const char MaxValue = '\uffff';

	public const char MinValue = '\0';

	private static ReadOnlySpan<byte> Latin1CharInfo => new byte[256]
	{
		14, 14, 14, 14, 14, 14, 14, 14, 14, 142,
		142, 142, 142, 142, 14, 14, 14, 14, 14, 14,
		14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
		14, 14, 139, 24, 24, 24, 26, 24, 24, 24,
		20, 21, 24, 25, 24, 19, 24, 24, 8, 8,
		8, 8, 8, 8, 8, 8, 8, 8, 24, 24,
		25, 25, 25, 24, 24, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 20, 24, 21, 27, 18, 27, 33, 33, 33,
		33, 33, 33, 33, 33, 33, 33, 33, 33, 33,
		33, 33, 33, 33, 33, 33, 33, 33, 33, 33,
		33, 33, 33, 20, 25, 21, 25, 14, 14, 14,
		14, 14, 14, 142, 14, 14, 14, 14, 14, 14,
		14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
		14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
		139, 24, 26, 26, 26, 26, 28, 24, 27, 28,
		4, 22, 25, 15, 28, 27, 28, 25, 10, 10,
		27, 33, 24, 24, 27, 10, 4, 23, 10, 10,
		10, 24, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 25, 64, 64, 64, 64,
		64, 64, 64, 33, 33, 33, 33, 33, 33, 33,
		33, 33, 33, 33, 33, 33, 33, 33, 33, 33,
		33, 33, 33, 33, 33, 33, 33, 25, 33, 33,
		33, 33, 33, 33, 33, 33
	};

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IAdditiveIdentity<char, char>.AdditiveIdentity => '\0';

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IMinMaxValue<char>.MinValue => '\0';

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IMinMaxValue<char>.MaxValue => '\uffff';

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IMultiplicativeIdentity<char, char>.MultiplicativeIdentity => '\u0001';

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char INumber<char>.One => '\u0001';

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char INumber<char>.Zero => '\0';

	private static bool IsLatin1(char c)
	{
		return (uint)c < (uint)Latin1CharInfo.Length;
	}

	public static bool IsAscii(char c)
	{
		return (uint)c <= 127u;
	}

	private static UnicodeCategory GetLatin1UnicodeCategory(char c)
	{
		return (UnicodeCategory)(Latin1CharInfo[c] & 0x1F);
	}

	public override int GetHashCode()
	{
		return (int)(this | ((uint)this << 16));
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is char))
		{
			return false;
		}
		return this == (char)obj;
	}

	[NonVersionable]
	public bool Equals(char obj)
	{
		return this == obj;
	}

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (!(value is char))
		{
			throw new ArgumentException(SR.Arg_MustBeChar);
		}
		return this - (char)value;
	}

	public int CompareTo(char value)
	{
		return this - value;
	}

	public override string ToString()
	{
		return ToString(this);
	}

	public string ToString(IFormatProvider? provider)
	{
		return ToString(this);
	}

	public static string ToString(char c)
	{
		return string.CreateFromChar(c);
	}

	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
	{
		if (!destination.IsEmpty)
		{
			destination[0] = this;
			charsWritten = 1;
			return true;
		}
		charsWritten = 0;
		return false;
	}

	string IFormattable.ToString(string format, IFormatProvider formatProvider)
	{
		return ToString(this);
	}

	public static char Parse(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if (s.Length != 1)
		{
			throw new FormatException(SR.Format_NeedSingleChar);
		}
		return s[0];
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out char result)
	{
		result = '\0';
		if (s == null)
		{
			return false;
		}
		if (s.Length != 1)
		{
			return false;
		}
		result = s[0];
		return true;
	}

	public static bool IsDigit(char c)
	{
		if (IsLatin1(c))
		{
			return IsInRange(c, '0', '9');
		}
		return CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.DecimalDigitNumber;
	}

	internal static bool IsInRange(char c, char min, char max)
	{
		return (uint)(c - min) <= (uint)(max - min);
	}

	private static bool IsInRange(UnicodeCategory c, UnicodeCategory min, UnicodeCategory max)
	{
		return (uint)(c - min) <= (uint)(max - min);
	}

	internal static bool CheckLetter(UnicodeCategory uc)
	{
		return IsInRange(uc, UnicodeCategory.UppercaseLetter, UnicodeCategory.OtherLetter);
	}

	public static bool IsLetter(char c)
	{
		if (IsAscii(c))
		{
			return (Latin1CharInfo[c] & 0x60) != 0;
		}
		return CheckLetter(CharUnicodeInfo.GetUnicodeCategory(c));
	}

	private static bool IsWhiteSpaceLatin1(char c)
	{
		return (Latin1CharInfo[c] & 0x80) != 0;
	}

	public static bool IsWhiteSpace(char c)
	{
		if (IsLatin1(c))
		{
			return IsWhiteSpaceLatin1(c);
		}
		return CharUnicodeInfo.GetIsWhiteSpace(c);
	}

	public static bool IsUpper(char c)
	{
		if (IsLatin1(c))
		{
			return (Latin1CharInfo[c] & 0x40) != 0;
		}
		return CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.UppercaseLetter;
	}

	public static bool IsLower(char c)
	{
		if (IsLatin1(c))
		{
			return (Latin1CharInfo[c] & 0x20) != 0;
		}
		return CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.LowercaseLetter;
	}

	internal static bool CheckPunctuation(UnicodeCategory uc)
	{
		return IsInRange(uc, UnicodeCategory.ConnectorPunctuation, UnicodeCategory.OtherPunctuation);
	}

	public static bool IsPunctuation(char c)
	{
		if (IsLatin1(c))
		{
			return CheckPunctuation(GetLatin1UnicodeCategory(c));
		}
		return CheckPunctuation(CharUnicodeInfo.GetUnicodeCategory(c));
	}

	internal static bool CheckLetterOrDigit(UnicodeCategory uc)
	{
		if (!CheckLetter(uc))
		{
			return uc == UnicodeCategory.DecimalDigitNumber;
		}
		return true;
	}

	public static bool IsLetterOrDigit(char c)
	{
		if (IsLatin1(c))
		{
			return CheckLetterOrDigit(GetLatin1UnicodeCategory(c));
		}
		return CheckLetterOrDigit(CharUnicodeInfo.GetUnicodeCategory(c));
	}

	public static char ToUpper(char c, CultureInfo culture)
	{
		if (culture == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
		}
		return culture.TextInfo.ToUpper(c);
	}

	public static char ToUpper(char c)
	{
		return CultureInfo.CurrentCulture.TextInfo.ToUpper(c);
	}

	public static char ToUpperInvariant(char c)
	{
		return TextInfo.ToUpperInvariant(c);
	}

	public static char ToLower(char c, CultureInfo culture)
	{
		if (culture == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
		}
		return culture.TextInfo.ToLower(c);
	}

	public static char ToLower(char c)
	{
		return CultureInfo.CurrentCulture.TextInfo.ToLower(c);
	}

	public static char ToLowerInvariant(char c)
	{
		return TextInfo.ToLowerInvariant(c);
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Char;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "Boolean"));
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		return this;
	}

	sbyte IConvertible.ToSByte(IFormatProvider provider)
	{
		return Convert.ToSByte(this);
	}

	byte IConvertible.ToByte(IFormatProvider provider)
	{
		return Convert.ToByte(this);
	}

	short IConvertible.ToInt16(IFormatProvider provider)
	{
		return Convert.ToInt16(this);
	}

	ushort IConvertible.ToUInt16(IFormatProvider provider)
	{
		return Convert.ToUInt16(this);
	}

	int IConvertible.ToInt32(IFormatProvider provider)
	{
		return Convert.ToInt32(this);
	}

	uint IConvertible.ToUInt32(IFormatProvider provider)
	{
		return Convert.ToUInt32(this);
	}

	long IConvertible.ToInt64(IFormatProvider provider)
	{
		return Convert.ToInt64(this);
	}

	ulong IConvertible.ToUInt64(IFormatProvider provider)
	{
		return Convert.ToUInt64(this);
	}

	float IConvertible.ToSingle(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "Single"));
	}

	double IConvertible.ToDouble(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "Double"));
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "Decimal"));
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	public static bool IsControl(char c)
	{
		return (uint)((c + 1) & -129) <= 32u;
	}

	public static bool IsControl(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return IsControl(s[index]);
	}

	public static bool IsDigit(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsLatin1(c))
		{
			return IsInRange(c, '0', '9');
		}
		return CharUnicodeInfo.GetUnicodeCategoryInternal(s, index) == UnicodeCategory.DecimalDigitNumber;
	}

	public static bool IsLetter(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsAscii(c))
		{
			return (Latin1CharInfo[c] & 0x60) != 0;
		}
		return CheckLetter(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
	}

	public static bool IsLetterOrDigit(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsLatin1(c))
		{
			return CheckLetterOrDigit(GetLatin1UnicodeCategory(c));
		}
		return CheckLetterOrDigit(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
	}

	public static bool IsLower(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsLatin1(c))
		{
			return (Latin1CharInfo[c] & 0x20) != 0;
		}
		return CharUnicodeInfo.GetUnicodeCategoryInternal(s, index) == UnicodeCategory.LowercaseLetter;
	}

	internal static bool CheckNumber(UnicodeCategory uc)
	{
		return IsInRange(uc, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.OtherNumber);
	}

	public static bool IsNumber(char c)
	{
		if (IsLatin1(c))
		{
			if (IsAscii(c))
			{
				return IsInRange(c, '0', '9');
			}
			return CheckNumber(GetLatin1UnicodeCategory(c));
		}
		return CheckNumber(CharUnicodeInfo.GetUnicodeCategory(c));
	}

	public static bool IsNumber(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsLatin1(c))
		{
			if (IsAscii(c))
			{
				return IsInRange(c, '0', '9');
			}
			return CheckNumber(GetLatin1UnicodeCategory(c));
		}
		return CheckNumber(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
	}

	public static bool IsPunctuation(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsLatin1(c))
		{
			return CheckPunctuation(GetLatin1UnicodeCategory(c));
		}
		return CheckPunctuation(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
	}

	internal static bool CheckSeparator(UnicodeCategory uc)
	{
		return IsInRange(uc, UnicodeCategory.SpaceSeparator, UnicodeCategory.ParagraphSeparator);
	}

	private static bool IsSeparatorLatin1(char c)
	{
		if (c != ' ')
		{
			return c == '\u00a0';
		}
		return true;
	}

	public static bool IsSeparator(char c)
	{
		if (IsLatin1(c))
		{
			return IsSeparatorLatin1(c);
		}
		return CheckSeparator(CharUnicodeInfo.GetUnicodeCategory(c));
	}

	public static bool IsSeparator(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsLatin1(c))
		{
			return IsSeparatorLatin1(c);
		}
		return CheckSeparator(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
	}

	public static bool IsSurrogate(char c)
	{
		return IsInRange(c, '\ud800', '\udfff');
	}

	public static bool IsSurrogate(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return IsSurrogate(s[index]);
	}

	internal static bool CheckSymbol(UnicodeCategory uc)
	{
		return IsInRange(uc, UnicodeCategory.MathSymbol, UnicodeCategory.OtherSymbol);
	}

	public static bool IsSymbol(char c)
	{
		if (IsLatin1(c))
		{
			return CheckSymbol(GetLatin1UnicodeCategory(c));
		}
		return CheckSymbol(CharUnicodeInfo.GetUnicodeCategory(c));
	}

	public static bool IsSymbol(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsLatin1(c))
		{
			return CheckSymbol(GetLatin1UnicodeCategory(c));
		}
		return CheckSymbol(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
	}

	public static bool IsUpper(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		char c = s[index];
		if (IsLatin1(c))
		{
			return (Latin1CharInfo[c] & 0x40) != 0;
		}
		return CharUnicodeInfo.GetUnicodeCategoryInternal(s, index) == UnicodeCategory.UppercaseLetter;
	}

	public static bool IsWhiteSpace(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return IsWhiteSpace(s[index]);
	}

	public static UnicodeCategory GetUnicodeCategory(char c)
	{
		if (IsLatin1(c))
		{
			return GetLatin1UnicodeCategory(c);
		}
		return CharUnicodeInfo.GetUnicodeCategory((int)c);
	}

	public static UnicodeCategory GetUnicodeCategory(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		if (IsLatin1(s[index]))
		{
			return GetLatin1UnicodeCategory(s[index]);
		}
		return CharUnicodeInfo.GetUnicodeCategoryInternal(s, index);
	}

	public static double GetNumericValue(char c)
	{
		return CharUnicodeInfo.GetNumericValue(c);
	}

	public static double GetNumericValue(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return CharUnicodeInfo.GetNumericValueInternal(s, index);
	}

	public static bool IsHighSurrogate(char c)
	{
		return IsInRange(c, '\ud800', '\udbff');
	}

	public static bool IsHighSurrogate(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return IsHighSurrogate(s[index]);
	}

	public static bool IsLowSurrogate(char c)
	{
		return IsInRange(c, '\udc00', '\udfff');
	}

	public static bool IsLowSurrogate(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return IsLowSurrogate(s[index]);
	}

	public static bool IsSurrogatePair(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if ((uint)index >= (uint)s.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		if (index + 1 < s.Length)
		{
			return IsSurrogatePair(s[index], s[index + 1]);
		}
		return false;
	}

	public static bool IsSurrogatePair(char highSurrogate, char lowSurrogate)
	{
		uint num = (uint)(highSurrogate - 55296);
		uint num2 = (uint)(lowSurrogate - 56320);
		return (num | num2) <= 1023;
	}

	public static string ConvertFromUtf32(int utf32)
	{
		if (!UnicodeUtility.IsValidUnicodeScalar((uint)utf32))
		{
			throw new ArgumentOutOfRangeException("utf32", SR.ArgumentOutOfRange_InvalidUTF32);
		}
		return Rune.UnsafeCreate((uint)utf32).ToString();
	}

	public static int ConvertToUtf32(char highSurrogate, char lowSurrogate)
	{
		uint num = (uint)(highSurrogate - 55296);
		uint num2 = (uint)(lowSurrogate - 56320);
		if ((num | num2) > 1023)
		{
			ConvertToUtf32_ThrowInvalidArgs(num);
		}
		return (int)(num << 10) + (lowSurrogate - 56320) + 65536;
	}

	[StackTraceHidden]
	private static void ConvertToUtf32_ThrowInvalidArgs(uint highSurrogateOffset)
	{
		if (highSurrogateOffset > 1023)
		{
			throw new ArgumentOutOfRangeException("highSurrogate", SR.ArgumentOutOfRange_InvalidHighSurrogate);
		}
		throw new ArgumentOutOfRangeException("lowSurrogate", SR.ArgumentOutOfRange_InvalidLowSurrogate);
	}

	public static int ConvertToUtf32(string s, int index)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		if (index < 0 || index >= s.Length)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
		}
		int num = s[index] - 55296;
		if (num >= 0 && num <= 2047)
		{
			if (num <= 1023)
			{
				if (index < s.Length - 1)
				{
					int num2 = s[index + 1] - 56320;
					if (num2 >= 0 && num2 <= 1023)
					{
						return num * 1024 + num2 + 65536;
					}
					throw new ArgumentException(SR.Format(SR.Argument_InvalidHighSurrogate, index), "s");
				}
				throw new ArgumentException(SR.Format(SR.Argument_InvalidHighSurrogate, index), "s");
			}
			throw new ArgumentException(SR.Format(SR.Argument_InvalidLowSurrogate, index), "s");
		}
		return s[index];
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IAdditionOperators<char, char, char>.operator +(char left, char right)
	{
		return (char)(left + right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IBinaryInteger<char>.LeadingZeroCount(char value)
	{
		return (char)(BitOperations.LeadingZeroCount(value) - 16);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IBinaryInteger<char>.PopCount(char value)
	{
		return (char)BitOperations.PopCount(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IBinaryInteger<char>.RotateLeft(char value, int rotateAmount)
	{
		return (char)(((uint)value << (rotateAmount & 0xF)) | (uint)((int)value >> ((16 - rotateAmount) & 0xF)));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IBinaryInteger<char>.RotateRight(char value, int rotateAmount)
	{
		return (char)((uint)((int)value >> (rotateAmount & 0xF)) | ((uint)value << ((16 - rotateAmount) & 0xF)));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IBinaryInteger<char>.TrailingZeroCount(char value)
	{
		return (char)(BitOperations.TrailingZeroCount((int)((uint)value << 16)) - 16);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IBinaryNumber<char>.IsPow2(char value)
	{
		return BitOperations.IsPow2((uint)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IBinaryNumber<char>.Log2(char value)
	{
		return (char)BitOperations.Log2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IBitwiseOperators<char, char, char>.operator &(char left, char right)
	{
		return (char)(left & right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IBitwiseOperators<char, char, char>.operator |(char left, char right)
	{
		return (char)(left | right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IBitwiseOperators<char, char, char>.operator ^(char left, char right)
	{
		return (char)(left ^ right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IBitwiseOperators<char, char, char>.operator ~(char value)
	{
		return (char)(~(uint)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<char, char>.operator <(char left, char right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<char, char>.operator <=(char left, char right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<char, char>.operator >(char left, char right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<char, char>.operator >=(char left, char right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IDecrementOperators<char>.operator --(char value)
	{
		return value = (char)(value - 1);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IDivisionOperators<char, char, char>.operator /(char left, char right)
	{
		return (char)(left / right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<char, char>.operator ==(char left, char right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<char, char>.operator !=(char left, char right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IIncrementOperators<char>.operator ++(char value)
	{
		return value = (char)(value + 1);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IModulusOperators<char, char, char>.operator %(char left, char right)
	{
		return (char)(left % right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IMultiplyOperators<char, char, char>.operator *(char left, char right)
	{
		return (char)(left * right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char INumber<char>.Abs(char value)
	{
		return value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char INumber<char>.Clamp(char value, char min, char max)
	{
		return (char)Math.Clamp(value, min, max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char INumber<char>.Create<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (char)(byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			return (char)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (char)checked((ushort)(double)(object)value);
		}
		if (typeof(TOther) == typeof(short))
		{
			return (char)checked((ushort)(short)(object)value);
		}
		if (typeof(TOther) == typeof(int))
		{
			return (char)checked((ushort)(int)(object)value);
		}
		if (typeof(TOther) == typeof(long))
		{
			return (char)checked((ushort)(long)(object)value);
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (char)checked((ushort)(nint)(IntPtr)(object)value);
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (char)checked((ushort)(sbyte)(object)value);
		}
		if (typeof(TOther) == typeof(float))
		{
			return (char)checked((ushort)(float)(object)value);
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (char)(ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			return (char)checked((ushort)(uint)(object)value);
		}
		if (typeof(TOther) == typeof(ulong))
		{
			return (char)checked((ushort)(ulong)(object)value);
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (char)checked((ushort)(nuint)(UIntPtr)(object)value);
		}
		ThrowHelper.ThrowNotSupportedException();
		return '\0';
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char INumber<char>.CreateSaturating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (char)(byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			if (!(num > 65535m))
			{
				if (!(num < 0m))
				{
					return (char)num;
				}
				return '\0';
			}
			return '\uffff';
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (!(num2 > 65535.0))
			{
				if (!(num2 < 0.0))
				{
					return (char)num2;
				}
				return '\0';
			}
			return '\uffff';
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			if (num3 >= 0)
			{
				return (char)num3;
			}
			return '\0';
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			if (num4 <= 65535)
			{
				if (num4 >= 0)
				{
					return (char)num4;
				}
				return '\0';
			}
			return '\uffff';
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (num5 <= 65535)
			{
				if (num5 >= 0)
				{
					return (char)num5;
				}
				return '\0';
			}
			return '\uffff';
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr <= 65535)
			{
				if ((nint)intPtr >= 0)
				{
					return (char)(nint)intPtr;
				}
				return '\0';
			}
			return '\uffff';
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			if (b >= 0)
			{
				return (char)b;
			}
			return '\0';
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (!(num6 > 65535f))
			{
				if (!(num6 < 0f))
				{
					return (char)num6;
				}
				return '\0';
			}
			return '\uffff';
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (char)(ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num7 = (uint)(object)value;
			if (num7 <= 65535)
			{
				return (char)num7;
			}
			return '\uffff';
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num8 = (ulong)(object)value;
			if (num8 <= 65535)
			{
				return (char)num8;
			}
			return '\uffff';
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr <= 65535)
			{
				return (char)(nuint)uIntPtr;
			}
			return '\uffff';
		}
		ThrowHelper.ThrowNotSupportedException();
		return '\0';
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char INumber<char>.CreateTruncating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (char)(byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			return (char)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (char)(double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (char)(short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (char)(int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (char)(long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (char)(nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (char)(sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (char)(float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (char)(ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			return (char)(uint)(object)value;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			return (char)(ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (char)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return '\0';
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static (char Quotient, char Remainder) INumber<char>.DivRem(char left, char right)
	{
		(ushort, ushort) tuple = Math.DivRem(left, right);
		return (Quotient: (char)tuple.Item1, Remainder: (char)tuple.Item2);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char INumber<char>.Max(char x, char y)
	{
		return (char)Math.Max(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char INumber<char>.Min(char x, char y)
	{
		return (char)Math.Min(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char INumber<char>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char INumber<char>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		if (s.Length != 1)
		{
			throw new FormatException(SR.Format_NeedSingleChar);
		}
		return s[0];
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char INumber<char>.Sign(char value)
	{
		return (char)((value != 0) ? 1u : 0u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<char>.TryCreate<TOther>(TOther value, out char result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			result = (char)(byte)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			result = (char)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			if (num < 0m || num > 65535m)
			{
				result = '\0';
				return false;
			}
			result = (char)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (num2 < 0.0 || num2 > 65535.0)
			{
				result = '\0';
				return false;
			}
			result = (char)num2;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			if (num3 < 0)
			{
				result = '\0';
				return false;
			}
			result = (char)num3;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			if (num4 < 0 || num4 > 65535)
			{
				result = '\0';
				return false;
			}
			result = (char)num4;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (num5 < 0 || num5 > 65535)
			{
				result = '\0';
				return false;
			}
			result = (char)num5;
			return true;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr < 0 || (nint)intPtr > 65535)
			{
				result = '\0';
				return false;
			}
			result = (char)(nint)intPtr;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			if (b < 0)
			{
				result = '\0';
				return false;
			}
			result = (char)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (num6 < 0f || num6 > 65535f)
			{
				result = '\0';
				return false;
			}
			result = (char)num6;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num7 = (ushort)(object)value;
			if (num7 > ushort.MaxValue)
			{
				result = '\0';
				return false;
			}
			result = (char)num7;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num8 = (uint)(object)value;
			if (num8 > 65535)
			{
				result = '\0';
				return false;
			}
			result = (char)num8;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num9 = (ulong)(object)value;
			if (num9 > 65535)
			{
				result = '\0';
				return false;
			}
			result = (char)num9;
			return true;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr > 65535)
			{
				result = '\0';
				return false;
			}
			result = (char)(nuint)uIntPtr;
			return true;
		}
		ThrowHelper.ThrowNotSupportedException();
		result = '\0';
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<char>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out char result)
	{
		return TryParse(s, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<char>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out char result)
	{
		if (s.Length != 1)
		{
			result = '\0';
			return false;
		}
		result = s[0];
		return true;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IParseable<char>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<char>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out char result)
	{
		return TryParse(s, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IShiftOperators<char, char>.operator <<(char value, int shiftAmount)
	{
		return (char)((uint)value << shiftAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IShiftOperators<char, char>.operator >>(char value, int shiftAmount)
	{
		return (char)((int)value >> shiftAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char ISpanParseable<char>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		if (s.Length != 1)
		{
			throw new FormatException(SR.Format_NeedSingleChar);
		}
		return s[0];
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<char>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out char result)
	{
		if (s.Length != 1)
		{
			result = '\0';
			return false;
		}
		result = s[0];
		return true;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char ISubtractionOperators<char, char, char>.operator -(char left, char right)
	{
		return (char)(left - right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IUnaryNegationOperators<char, char>.operator -(char value)
	{
		return (char)(0 - value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static char IUnaryPlusOperators<char, char>.operator +(char value)
	{
		return value;
	}
}
