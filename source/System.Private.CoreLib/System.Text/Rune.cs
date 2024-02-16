using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace System.Text;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct Rune : IComparable, IComparable<Rune>, IEquatable<Rune>, ISpanFormattable, IFormattable
{
	private readonly uint _value;

	private static ReadOnlySpan<byte> AsciiCharInfo => new byte[128]
	{
		14, 14, 14, 14, 14, 14, 14, 14, 14, 142,
		142, 142, 142, 142, 14, 14, 14, 14, 14, 14,
		14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
		14, 14, 139, 24, 24, 24, 26, 24, 24, 24,
		20, 21, 24, 25, 24, 19, 24, 24, 72, 72,
		72, 72, 72, 72, 72, 72, 72, 72, 24, 24,
		25, 25, 25, 24, 24, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 64, 64, 64, 64, 64, 64, 64, 64, 64,
		64, 20, 24, 21, 27, 18, 27, 65, 65, 65,
		65, 65, 65, 65, 65, 65, 65, 65, 65, 65,
		65, 65, 65, 65, 65, 65, 65, 65, 65, 65,
		65, 65, 65, 20, 25, 21, 25, 14
	};

	private string DebuggerDisplay
	{
		get
		{
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(5, 2, invariantCulture);
			handler.AppendLiteral("U+");
			handler.AppendFormatted(_value, "X4");
			handler.AppendLiteral(" '");
			handler.AppendFormatted(IsValid(_value) ? ToString() : "\ufffd");
			handler.AppendLiteral("'");
			return string.Create(invariantCulture, ref handler);
		}
	}

	public bool IsAscii => UnicodeUtility.IsAsciiCodePoint(_value);

	public bool IsBmp => UnicodeUtility.IsBmpCodePoint(_value);

	public int Plane => UnicodeUtility.GetPlane(_value);

	public static Rune ReplacementChar => UnsafeCreate(65533u);

	public int Utf16SequenceLength => UnicodeUtility.GetUtf16SequenceLength(_value);

	public int Utf8SequenceLength => UnicodeUtility.GetUtf8SequenceLength(_value);

	public int Value => (int)_value;

	public Rune(char ch)
	{
		if (UnicodeUtility.IsSurrogateCodePoint(ch))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.ch);
		}
		_value = ch;
	}

	public Rune(char highSurrogate, char lowSurrogate)
		: this((uint)char.ConvertToUtf32(highSurrogate, lowSurrogate), unused: false)
	{
	}

	public Rune(int value)
		: this((uint)value)
	{
	}

	[CLSCompliant(false)]
	public Rune(uint value)
	{
		if (!UnicodeUtility.IsValidUnicodeScalar(value))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
		}
		_value = value;
	}

	private Rune(uint scalarValue, bool unused)
	{
		_value = scalarValue;
	}

	public static bool operator ==(Rune left, Rune right)
	{
		return left._value == right._value;
	}

	public static bool operator !=(Rune left, Rune right)
	{
		return left._value != right._value;
	}

	public static bool operator <(Rune left, Rune right)
	{
		return left._value < right._value;
	}

	public static bool operator <=(Rune left, Rune right)
	{
		return left._value <= right._value;
	}

	public static bool operator >(Rune left, Rune right)
	{
		return left._value > right._value;
	}

	public static bool operator >=(Rune left, Rune right)
	{
		return left._value >= right._value;
	}

	public static explicit operator Rune(char ch)
	{
		return new Rune(ch);
	}

	[CLSCompliant(false)]
	public static explicit operator Rune(uint value)
	{
		return new Rune(value);
	}

	public static explicit operator Rune(int value)
	{
		return new Rune(value);
	}

	private static Rune ChangeCaseCultureAware(Rune rune, TextInfo textInfo, bool toUpper)
	{
		Span<char> span = stackalloc char[2];
		Span<char> destination = stackalloc char[2];
		int length = rune.EncodeToUtf16(span);
		span = span.Slice(0, length);
		destination = destination.Slice(0, length);
		if (toUpper)
		{
			textInfo.ChangeCaseToUpper(span, destination);
		}
		else
		{
			textInfo.ChangeCaseToLower(span, destination);
		}
		if (rune.IsBmp)
		{
			return UnsafeCreate(destination[0]);
		}
		return UnsafeCreate(UnicodeUtility.GetScalarFromUtf16SurrogatePair(destination[0], destination[1]));
	}

	public int CompareTo(Rune other)
	{
		return Value - other.Value;
	}

	public static OperationStatus DecodeFromUtf16(ReadOnlySpan<char> source, out Rune result, out int charsConsumed)
	{
		if (!source.IsEmpty)
		{
			char c = source[0];
			if (TryCreate(c, out result))
			{
				charsConsumed = 1;
				return OperationStatus.Done;
			}
			if (source.Length > 1)
			{
				char lowSurrogate = source[1];
				if (TryCreate(c, lowSurrogate, out result))
				{
					charsConsumed = 2;
					return OperationStatus.Done;
				}
			}
			else if (char.IsHighSurrogate(c))
			{
				goto IL_004c;
			}
			charsConsumed = 1;
			result = ReplacementChar;
			return OperationStatus.InvalidData;
		}
		goto IL_004c;
		IL_004c:
		charsConsumed = source.Length;
		result = ReplacementChar;
		return OperationStatus.NeedMoreData;
	}

	public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, out Rune result, out int bytesConsumed)
	{
		int num = 0;
		uint num2;
		if ((uint)num < (uint)source.Length)
		{
			num2 = source[num];
			if (UnicodeUtility.IsAsciiCodePoint(num2))
			{
				goto IL_0021;
			}
			if (UnicodeUtility.IsInRangeInclusive(num2, 194u, 244u))
			{
				num2 = num2 - 194 << 6;
				num++;
				if ((uint)num >= (uint)source.Length)
				{
					goto IL_0163;
				}
				int num3 = (sbyte)source[num];
				if (num3 < -64)
				{
					num2 += (uint)num3;
					num2 += 128;
					num2 += 128;
					if (num2 < 2048)
					{
						goto IL_0021;
					}
					if (UnicodeUtility.IsInRangeInclusive(num2, 2080u, 3343u) && !UnicodeUtility.IsInRangeInclusive(num2, 2912u, 2943u) && !UnicodeUtility.IsInRangeInclusive(num2, 3072u, 3087u))
					{
						num++;
						if ((uint)num >= (uint)source.Length)
						{
							goto IL_0163;
						}
						num3 = (sbyte)source[num];
						if (num3 < -64)
						{
							num2 <<= 6;
							num2 += (uint)num3;
							num2 += 128;
							num2 -= 131072;
							if (num2 > 65535)
							{
								num++;
								if ((uint)num >= (uint)source.Length)
								{
									goto IL_0163;
								}
								num3 = (sbyte)source[num];
								if (num3 >= -64)
								{
									goto IL_0153;
								}
								num2 <<= 6;
								num2 += (uint)num3;
								num2 += 128;
								num2 -= 4194304;
							}
							goto IL_0021;
						}
					}
				}
			}
			else
			{
				num = 1;
			}
			goto IL_0153;
		}
		goto IL_0163;
		IL_0021:
		bytesConsumed = num + 1;
		result = UnsafeCreate(num2);
		return OperationStatus.Done;
		IL_0163:
		bytesConsumed = num;
		result = ReplacementChar;
		return OperationStatus.NeedMoreData;
		IL_0153:
		bytesConsumed = num;
		result = ReplacementChar;
		return OperationStatus.InvalidData;
	}

	public static OperationStatus DecodeLastFromUtf16(ReadOnlySpan<char> source, out Rune result, out int charsConsumed)
	{
		int num = source.Length - 1;
		if ((uint)num < (uint)source.Length)
		{
			char c = source[num];
			if (TryCreate(c, out result))
			{
				charsConsumed = 1;
				return OperationStatus.Done;
			}
			if (char.IsLowSurrogate(c))
			{
				num--;
				if ((uint)num < (uint)source.Length)
				{
					char highSurrogate = source[num];
					if (TryCreate(highSurrogate, c, out result))
					{
						charsConsumed = 2;
						return OperationStatus.Done;
					}
				}
				charsConsumed = 1;
				result = ReplacementChar;
				return OperationStatus.InvalidData;
			}
		}
		charsConsumed = -source.Length >>> 31;
		result = ReplacementChar;
		return OperationStatus.NeedMoreData;
	}

	public static OperationStatus DecodeLastFromUtf8(ReadOnlySpan<byte> source, out Rune value, out int bytesConsumed)
	{
		int num = source.Length - 1;
		if ((uint)num < (uint)source.Length)
		{
			uint num2 = source[num];
			if (UnicodeUtility.IsAsciiCodePoint(num2))
			{
				bytesConsumed = 1;
				value = UnsafeCreate(num2);
				return OperationStatus.Done;
			}
			if (((byte)num2 & 0x40u) != 0)
			{
				return DecodeFromUtf8(source.Slice(num), out value, out bytesConsumed);
			}
			int num3 = 3;
			OperationStatus result2;
			Rune result;
			int bytesConsumed2;
			while (true)
			{
				if (num3 > 0)
				{
					num--;
					if ((uint)num < (uint)source.Length)
					{
						if ((sbyte)source[num] < -64)
						{
							num3--;
							continue;
						}
						source = source.Slice(num);
						result2 = DecodeFromUtf8(source, out result, out bytesConsumed2);
						if (bytesConsumed2 == source.Length)
						{
							break;
						}
					}
				}
				value = ReplacementChar;
				bytesConsumed = 1;
				return OperationStatus.InvalidData;
			}
			bytesConsumed = bytesConsumed2;
			value = result;
			return result2;
		}
		value = ReplacementChar;
		bytesConsumed = 0;
		return OperationStatus.NeedMoreData;
	}

	public int EncodeToUtf16(Span<char> destination)
	{
		if (!TryEncodeToUtf16(destination, out var charsWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return charsWritten;
	}

	public int EncodeToUtf8(Span<byte> destination)
	{
		if (!TryEncodeToUtf8(destination, out var bytesWritten))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return bytesWritten;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Rune other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(Rune other)
	{
		return this == other;
	}

	public override int GetHashCode()
	{
		return Value;
	}

	public static Rune GetRuneAt(string input, int index)
	{
		int num = ReadRuneFromString(input, index);
		if (num < 0)
		{
			ThrowHelper.ThrowArgumentException_CannotExtractScalar(ExceptionArgument.index);
		}
		return UnsafeCreate((uint)num);
	}

	public static bool IsValid(int value)
	{
		return IsValid((uint)value);
	}

	[CLSCompliant(false)]
	public static bool IsValid(uint value)
	{
		return UnicodeUtility.IsValidUnicodeScalar(value);
	}

	internal static int ReadFirstRuneFromUtf16Buffer(ReadOnlySpan<char> input)
	{
		if (input.IsEmpty)
		{
			return -1;
		}
		uint num = input[0];
		if (UnicodeUtility.IsSurrogateCodePoint(num))
		{
			if (!UnicodeUtility.IsHighSurrogateCodePoint(num))
			{
				return -1;
			}
			if (input.Length <= 1)
			{
				return -1;
			}
			uint num2 = input[1];
			if (!UnicodeUtility.IsLowSurrogateCodePoint(num2))
			{
				return -1;
			}
			num = UnicodeUtility.GetScalarFromUtf16SurrogatePair(num, num2);
		}
		return (int)num;
	}

	private static int ReadRuneFromString(string input, int index)
	{
		if (input == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
		}
		if ((uint)index >= (uint)input.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexException();
		}
		uint num = input[index];
		if (UnicodeUtility.IsSurrogateCodePoint(num))
		{
			if (!UnicodeUtility.IsHighSurrogateCodePoint(num))
			{
				return -1;
			}
			index++;
			if ((uint)index >= (uint)input.Length)
			{
				return -1;
			}
			uint num2 = input[index];
			if (!UnicodeUtility.IsLowSurrogateCodePoint(num2))
			{
				return -1;
			}
			num = UnicodeUtility.GetScalarFromUtf16SurrogatePair(num, num2);
		}
		return (int)num;
	}

	public override string ToString()
	{
		if (IsBmp)
		{
			return string.CreateFromChar((char)_value);
		}
		UnicodeUtility.GetUtf16SurrogatesFromSupplementaryPlaneScalar(_value, out var highSurrogateCodePoint, out var lowSurrogateCodePoint);
		return string.CreateFromChar(highSurrogateCodePoint, lowSurrogateCodePoint);
	}

	bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
	{
		return TryEncodeToUtf16(destination, out charsWritten);
	}

	string IFormattable.ToString(string format, IFormatProvider formatProvider)
	{
		return ToString();
	}

	public static bool TryCreate(char ch, out Rune result)
	{
		if (!UnicodeUtility.IsSurrogateCodePoint(ch))
		{
			result = UnsafeCreate(ch);
			return true;
		}
		result = default(Rune);
		return false;
	}

	public static bool TryCreate(char highSurrogate, char lowSurrogate, out Rune result)
	{
		uint num = (uint)(highSurrogate - 55296);
		uint num2 = (uint)(lowSurrogate - 56320);
		if ((num | num2) <= 1023)
		{
			result = UnsafeCreate((uint)((int)(num << 10) + (lowSurrogate - 56320) + 65536));
			return true;
		}
		result = default(Rune);
		return false;
	}

	public static bool TryCreate(int value, out Rune result)
	{
		return TryCreate((uint)value, out result);
	}

	[CLSCompliant(false)]
	public static bool TryCreate(uint value, out Rune result)
	{
		if (UnicodeUtility.IsValidUnicodeScalar(value))
		{
			result = UnsafeCreate(value);
			return true;
		}
		result = default(Rune);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryEncodeToUtf16(Span<char> destination, out int charsWritten)
	{
		return TryEncodeToUtf16(this, destination, out charsWritten);
	}

	private static bool TryEncodeToUtf16(Rune value, Span<char> destination, out int charsWritten)
	{
		if (!destination.IsEmpty)
		{
			if (value.IsBmp)
			{
				destination[0] = (char)value._value;
				charsWritten = 1;
				return true;
			}
			if (1u < (uint)destination.Length)
			{
				UnicodeUtility.GetUtf16SurrogatesFromSupplementaryPlaneScalar(value._value, out destination[0], out destination[1]);
				charsWritten = 2;
				return true;
			}
		}
		charsWritten = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryEncodeToUtf8(Span<byte> destination, out int bytesWritten)
	{
		return TryEncodeToUtf8(this, destination, out bytesWritten);
	}

	private static bool TryEncodeToUtf8(Rune value, Span<byte> destination, out int bytesWritten)
	{
		if (!destination.IsEmpty)
		{
			if (value.IsAscii)
			{
				destination[0] = (byte)value._value;
				bytesWritten = 1;
				return true;
			}
			if (1u < (uint)destination.Length)
			{
				if ((long)value.Value <= 2047L)
				{
					destination[0] = (byte)(value._value + 12288 >> 6);
					destination[1] = (byte)((value._value & 0x3F) + 128);
					bytesWritten = 2;
					return true;
				}
				if (2u < (uint)destination.Length)
				{
					if ((long)value.Value <= 65535L)
					{
						destination[0] = (byte)(value._value + 917504 >> 12);
						destination[1] = (byte)(((value._value & 0xFC0) >> 6) + 128);
						destination[2] = (byte)((value._value & 0x3F) + 128);
						bytesWritten = 3;
						return true;
					}
					if (3u < (uint)destination.Length)
					{
						destination[0] = (byte)(value._value + 62914560 >> 18);
						destination[1] = (byte)(((value._value & 0x3F000) >> 12) + 128);
						destination[2] = (byte)(((value._value & 0xFC0) >> 6) + 128);
						destination[3] = (byte)((value._value & 0x3F) + 128);
						bytesWritten = 4;
						return true;
					}
				}
			}
		}
		bytesWritten = 0;
		return false;
	}

	public static bool TryGetRuneAt(string input, int index, out Rune value)
	{
		int num = ReadRuneFromString(input, index);
		if (num >= 0)
		{
			value = UnsafeCreate((uint)num);
			return true;
		}
		value = default(Rune);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Rune UnsafeCreate(uint scalarValue)
	{
		return new Rune(scalarValue, unused: false);
	}

	public static double GetNumericValue(Rune value)
	{
		if (value.IsAscii)
		{
			uint num = value._value - 48;
			if (num > 9)
			{
				return -1.0;
			}
			return num;
		}
		return CharUnicodeInfo.GetNumericValue(value.Value);
	}

	public static UnicodeCategory GetUnicodeCategory(Rune value)
	{
		if (value.IsAscii)
		{
			return (UnicodeCategory)(AsciiCharInfo[value.Value] & 0x1F);
		}
		return GetUnicodeCategoryNonAscii(value);
	}

	private static UnicodeCategory GetUnicodeCategoryNonAscii(Rune value)
	{
		return CharUnicodeInfo.GetUnicodeCategory(value.Value);
	}

	private static bool IsCategoryLetter(UnicodeCategory category)
	{
		return UnicodeUtility.IsInRangeInclusive((uint)category, 0u, 4u);
	}

	private static bool IsCategoryLetterOrDecimalDigit(UnicodeCategory category)
	{
		if (!UnicodeUtility.IsInRangeInclusive((uint)category, 0u, 4u))
		{
			return category == UnicodeCategory.DecimalDigitNumber;
		}
		return true;
	}

	private static bool IsCategoryNumber(UnicodeCategory category)
	{
		return UnicodeUtility.IsInRangeInclusive((uint)category, 8u, 10u);
	}

	private static bool IsCategoryPunctuation(UnicodeCategory category)
	{
		return UnicodeUtility.IsInRangeInclusive((uint)category, 18u, 24u);
	}

	private static bool IsCategorySeparator(UnicodeCategory category)
	{
		return UnicodeUtility.IsInRangeInclusive((uint)category, 11u, 13u);
	}

	private static bool IsCategorySymbol(UnicodeCategory category)
	{
		return UnicodeUtility.IsInRangeInclusive((uint)category, 25u, 28u);
	}

	public static bool IsControl(Rune value)
	{
		return ((value._value + 1) & 0xFFFFFF7Fu) <= 32;
	}

	public static bool IsDigit(Rune value)
	{
		if (value.IsAscii)
		{
			return UnicodeUtility.IsInRangeInclusive(value._value, 48u, 57u);
		}
		return GetUnicodeCategoryNonAscii(value) == UnicodeCategory.DecimalDigitNumber;
	}

	public static bool IsLetter(Rune value)
	{
		if (value.IsAscii)
		{
			return ((value._value - 65) & 0xFFFFFFDFu) <= 25;
		}
		return IsCategoryLetter(GetUnicodeCategoryNonAscii(value));
	}

	public static bool IsLetterOrDigit(Rune value)
	{
		if (value.IsAscii)
		{
			return (AsciiCharInfo[value.Value] & 0x40) != 0;
		}
		return IsCategoryLetterOrDecimalDigit(GetUnicodeCategoryNonAscii(value));
	}

	public static bool IsLower(Rune value)
	{
		if (value.IsAscii)
		{
			return UnicodeUtility.IsInRangeInclusive(value._value, 97u, 122u);
		}
		return GetUnicodeCategoryNonAscii(value) == UnicodeCategory.LowercaseLetter;
	}

	public static bool IsNumber(Rune value)
	{
		if (value.IsAscii)
		{
			return UnicodeUtility.IsInRangeInclusive(value._value, 48u, 57u);
		}
		return IsCategoryNumber(GetUnicodeCategoryNonAscii(value));
	}

	public static bool IsPunctuation(Rune value)
	{
		return IsCategoryPunctuation(GetUnicodeCategory(value));
	}

	public static bool IsSeparator(Rune value)
	{
		return IsCategorySeparator(GetUnicodeCategory(value));
	}

	public static bool IsSymbol(Rune value)
	{
		return IsCategorySymbol(GetUnicodeCategory(value));
	}

	public static bool IsUpper(Rune value)
	{
		if (value.IsAscii)
		{
			return UnicodeUtility.IsInRangeInclusive(value._value, 65u, 90u);
		}
		return GetUnicodeCategoryNonAscii(value) == UnicodeCategory.UppercaseLetter;
	}

	public static bool IsWhiteSpace(Rune value)
	{
		if (value.IsAscii)
		{
			return (AsciiCharInfo[value.Value] & 0x80) != 0;
		}
		if (value.IsBmp)
		{
			return CharUnicodeInfo.GetIsWhiteSpace((char)value._value);
		}
		return false;
	}

	public static Rune ToLower(Rune value, CultureInfo culture)
	{
		if (culture == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
		}
		if (GlobalizationMode.Invariant)
		{
			return ToLowerInvariant(value);
		}
		return ChangeCaseCultureAware(value, culture.TextInfo, toUpper: false);
	}

	public static Rune ToLowerInvariant(Rune value)
	{
		if (value.IsAscii)
		{
			return UnsafeCreate(Utf16Utility.ConvertAllAsciiCharsInUInt32ToLowercase(value._value));
		}
		if (GlobalizationMode.Invariant)
		{
			return UnsafeCreate(CharUnicodeInfo.ToLower(value._value));
		}
		return ChangeCaseCultureAware(value, TextInfo.Invariant, toUpper: false);
	}

	public static Rune ToUpper(Rune value, CultureInfo culture)
	{
		if (culture == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
		}
		if (GlobalizationMode.Invariant)
		{
			return ToUpperInvariant(value);
		}
		return ChangeCaseCultureAware(value, culture.TextInfo, toUpper: true);
	}

	public static Rune ToUpperInvariant(Rune value)
	{
		if (value.IsAscii)
		{
			return UnsafeCreate(Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(value._value));
		}
		if (GlobalizationMode.Invariant)
		{
			return UnsafeCreate(CharUnicodeInfo.ToUpper(value._value));
		}
		return ChangeCaseCultureAware(value, TextInfo.Invariant, toUpper: true);
	}

	int IComparable.CompareTo(object obj)
	{
		if (obj == null)
		{
			return 1;
		}
		if (obj is Rune other)
		{
			return CompareTo(other);
		}
		throw new ArgumentException(SR.Arg_MustBeRune);
	}
}
