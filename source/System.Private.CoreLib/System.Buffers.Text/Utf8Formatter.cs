using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Buffers.Text;

public static class Utf8Formatter
{
	[StructLayout(LayoutKind.Explicit)]
	private struct DecomposedGuid
	{
		[FieldOffset(0)]
		public Guid Guid;

		[FieldOffset(0)]
		public byte Byte00;

		[FieldOffset(1)]
		public byte Byte01;

		[FieldOffset(2)]
		public byte Byte02;

		[FieldOffset(3)]
		public byte Byte03;

		[FieldOffset(4)]
		public byte Byte04;

		[FieldOffset(5)]
		public byte Byte05;

		[FieldOffset(6)]
		public byte Byte06;

		[FieldOffset(7)]
		public byte Byte07;

		[FieldOffset(8)]
		public byte Byte08;

		[FieldOffset(9)]
		public byte Byte09;

		[FieldOffset(10)]
		public byte Byte10;

		[FieldOffset(11)]
		public byte Byte11;

		[FieldOffset(12)]
		public byte Byte12;

		[FieldOffset(13)]
		public byte Byte13;

		[FieldOffset(14)]
		public byte Byte14;

		[FieldOffset(15)]
		public byte Byte15;
	}

	private static readonly uint[] s_dayAbbreviations = new uint[7] { 7238995u, 7237453u, 6649172u, 6579543u, 7694420u, 6910534u, 7627091u };

	private static readonly uint[] s_dayAbbreviationsLowercase = new uint[7] { 7239027u, 7237485u, 6649204u, 6579575u, 7694452u, 6910566u, 7627123u };

	private static readonly uint[] s_monthAbbreviations = new uint[12]
	{
		7233866u, 6448454u, 7496013u, 7499841u, 7954765u, 7238986u, 7107914u, 6780225u, 7365971u, 7627599u,
		7761742u, 6513988u
	};

	private static readonly uint[] s_monthAbbreviationsLowercase = new uint[12]
	{
		7233898u, 6448486u, 7496045u, 7499873u, 7954797u, 7239018u, 7107946u, 6780257u, 7366003u, 7627631u,
		7761774u, 6514020u
	};

	public static bool TryFormat(bool value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		char symbolOrDefault = FormattingHelpers.GetSymbolOrDefault(in format, 'G');
		if (value)
		{
			if (symbolOrDefault == 'G')
			{
				if (BinaryPrimitives.TryWriteUInt32BigEndian(destination, 1416787301u))
				{
					goto IL_0033;
				}
			}
			else
			{
				if (symbolOrDefault != 'l')
				{
					goto IL_0083;
				}
				if (BinaryPrimitives.TryWriteUInt32BigEndian(destination, 1953658213u))
				{
					goto IL_0033;
				}
			}
		}
		else if (symbolOrDefault == 'G')
		{
			if (4u < (uint)destination.Length)
			{
				BinaryPrimitives.WriteUInt32BigEndian(destination, 1180789875u);
				goto IL_006e;
			}
		}
		else
		{
			if (symbolOrDefault != 'l')
			{
				goto IL_0083;
			}
			if (4u < (uint)destination.Length)
			{
				BinaryPrimitives.WriteUInt32BigEndian(destination, 1717660787u);
				goto IL_006e;
			}
		}
		bytesWritten = 0;
		return false;
		IL_006e:
		destination[4] = 101;
		bytesWritten = 5;
		return true;
		IL_0083:
		return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
		IL_0033:
		bytesWritten = 4;
		return true;
	}

	public static bool TryFormat(DateTimeOffset value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		TimeSpan offset = Utf8Constants.NullUtcOffset;
		char c = format.Symbol;
		if (format.IsDefault)
		{
			c = 'G';
			offset = value.Offset;
		}
		return c switch
		{
			'R' => TryFormatDateTimeR(value.UtcDateTime, destination, out bytesWritten), 
			'l' => TryFormatDateTimeL(value.UtcDateTime, destination, out bytesWritten), 
			'O' => TryFormatDateTimeO(value.DateTime, value.Offset, destination, out bytesWritten), 
			'G' => TryFormatDateTimeG(value.DateTime, offset, destination, out bytesWritten), 
			_ => FormattingHelpers.TryFormatThrowFormatException(out bytesWritten), 
		};
	}

	public static bool TryFormat(DateTime value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return FormattingHelpers.GetSymbolOrDefault(in format, 'G') switch
		{
			'R' => TryFormatDateTimeR(value, destination, out bytesWritten), 
			'l' => TryFormatDateTimeL(value, destination, out bytesWritten), 
			'O' => TryFormatDateTimeO(value, Utf8Constants.NullUtcOffset, destination, out bytesWritten), 
			'G' => TryFormatDateTimeG(value, Utf8Constants.NullUtcOffset, destination, out bytesWritten), 
			_ => FormattingHelpers.TryFormatThrowFormatException(out bytesWritten), 
		};
	}

	private static bool TryFormatDateTimeG(DateTime value, TimeSpan offset, Span<byte> destination, out int bytesWritten)
	{
		int num = 19;
		if (offset != Utf8Constants.NullUtcOffset)
		{
			num += 7;
		}
		if (destination.Length < num)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = num;
		byte b = destination[18];
		value.GetDate(out var year, out var month, out var day);
		value.GetTime(out var hour, out var minute, out var second);
		FormattingHelpers.WriteTwoDecimalDigits((uint)month, destination);
		destination[2] = 47;
		FormattingHelpers.WriteTwoDecimalDigits((uint)day, destination, 3);
		destination[5] = 47;
		FormattingHelpers.WriteFourDecimalDigits((uint)year, destination, 6);
		destination[10] = 32;
		FormattingHelpers.WriteTwoDecimalDigits((uint)hour, destination, 11);
		destination[13] = 58;
		FormattingHelpers.WriteTwoDecimalDigits((uint)minute, destination, 14);
		destination[16] = 58;
		FormattingHelpers.WriteTwoDecimalDigits((uint)second, destination, 17);
		if (offset != Utf8Constants.NullUtcOffset)
		{
			int num2 = (int)(offset.Ticks / 600000000);
			byte b2;
			if (num2 < 0)
			{
				b2 = 45;
				num2 = -num2;
			}
			else
			{
				b2 = 43;
			}
			int result;
			int value2 = Math.DivRem(num2, 60, out result);
			FormattingHelpers.WriteTwoDecimalDigits((uint)result, destination, 24);
			destination[23] = 58;
			FormattingHelpers.WriteTwoDecimalDigits((uint)value2, destination, 21);
			destination[20] = b2;
			destination[19] = 32;
		}
		return true;
	}

	private static bool TryFormatDateTimeL(DateTime value, Span<byte> destination, out int bytesWritten)
	{
		if (28u >= (uint)destination.Length)
		{
			bytesWritten = 0;
			return false;
		}
		value.GetDate(out var year, out var month, out var day);
		value.GetTime(out var hour, out var minute, out var second);
		uint num = s_dayAbbreviationsLowercase[(int)value.DayOfWeek];
		destination[0] = (byte)num;
		num >>= 8;
		destination[1] = (byte)num;
		num >>= 8;
		destination[2] = (byte)num;
		destination[3] = 44;
		destination[4] = 32;
		FormattingHelpers.WriteTwoDecimalDigits((uint)day, destination, 5);
		destination[7] = 32;
		uint num2 = s_monthAbbreviationsLowercase[month - 1];
		destination[8] = (byte)num2;
		num2 >>= 8;
		destination[9] = (byte)num2;
		num2 >>= 8;
		destination[10] = (byte)num2;
		destination[11] = 32;
		FormattingHelpers.WriteFourDecimalDigits((uint)year, destination, 12);
		destination[16] = 32;
		FormattingHelpers.WriteTwoDecimalDigits((uint)hour, destination, 17);
		destination[19] = 58;
		FormattingHelpers.WriteTwoDecimalDigits((uint)minute, destination, 20);
		destination[22] = 58;
		FormattingHelpers.WriteTwoDecimalDigits((uint)second, destination, 23);
		destination[25] = 32;
		destination[26] = 103;
		destination[27] = 109;
		destination[28] = 116;
		bytesWritten = 29;
		return true;
	}

	private static bool TryFormatDateTimeO(DateTime value, TimeSpan offset, Span<byte> destination, out int bytesWritten)
	{
		int num = 27;
		DateTimeKind dateTimeKind = DateTimeKind.Local;
		if (offset == Utf8Constants.NullUtcOffset)
		{
			dateTimeKind = value.Kind;
			switch (dateTimeKind)
			{
			case DateTimeKind.Local:
				offset = TimeZoneInfo.Local.GetUtcOffset(value);
				num += 6;
				break;
			case DateTimeKind.Utc:
				num++;
				break;
			}
		}
		else
		{
			num += 6;
		}
		if (destination.Length < num)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = num;
		_ = ref destination[26];
		value.GetDate(out var year, out var month, out var day);
		value.GetTimePrecise(out var hour, out var minute, out var second, out var tick);
		FormattingHelpers.WriteFourDecimalDigits((uint)year, destination);
		destination[4] = 45;
		FormattingHelpers.WriteTwoDecimalDigits((uint)month, destination, 5);
		destination[7] = 45;
		FormattingHelpers.WriteTwoDecimalDigits((uint)day, destination, 8);
		destination[10] = 84;
		FormattingHelpers.WriteTwoDecimalDigits((uint)hour, destination, 11);
		destination[13] = 58;
		FormattingHelpers.WriteTwoDecimalDigits((uint)minute, destination, 14);
		destination[16] = 58;
		FormattingHelpers.WriteTwoDecimalDigits((uint)second, destination, 17);
		destination[19] = 46;
		FormattingHelpers.WriteDigits((uint)tick, destination.Slice(20, 7));
		switch (dateTimeKind)
		{
		case DateTimeKind.Local:
		{
			int num2 = (int)(offset.Ticks / 600000000);
			byte b;
			if (num2 < 0)
			{
				b = 45;
				num2 = -num2;
			}
			else
			{
				b = 43;
			}
			int result;
			int value2 = Math.DivRem(num2, 60, out result);
			FormattingHelpers.WriteTwoDecimalDigits((uint)result, destination, 31);
			destination[30] = 58;
			FormattingHelpers.WriteTwoDecimalDigits((uint)value2, destination, 28);
			destination[27] = b;
			break;
		}
		case DateTimeKind.Utc:
			destination[27] = 90;
			break;
		}
		return true;
	}

	private static bool TryFormatDateTimeR(DateTime value, Span<byte> destination, out int bytesWritten)
	{
		if (28u >= (uint)destination.Length)
		{
			bytesWritten = 0;
			return false;
		}
		value.GetDate(out var year, out var month, out var day);
		value.GetTime(out var hour, out var minute, out var second);
		uint num = s_dayAbbreviations[(int)value.DayOfWeek];
		destination[0] = (byte)num;
		num >>= 8;
		destination[1] = (byte)num;
		num >>= 8;
		destination[2] = (byte)num;
		destination[3] = 44;
		destination[4] = 32;
		FormattingHelpers.WriteTwoDecimalDigits((uint)day, destination, 5);
		destination[7] = 32;
		uint num2 = s_monthAbbreviations[month - 1];
		destination[8] = (byte)num2;
		num2 >>= 8;
		destination[9] = (byte)num2;
		num2 >>= 8;
		destination[10] = (byte)num2;
		destination[11] = 32;
		FormattingHelpers.WriteFourDecimalDigits((uint)year, destination, 12);
		destination[16] = 32;
		FormattingHelpers.WriteTwoDecimalDigits((uint)hour, destination, 17);
		destination[19] = 58;
		FormattingHelpers.WriteTwoDecimalDigits((uint)minute, destination, 20);
		destination[22] = 58;
		FormattingHelpers.WriteTwoDecimalDigits((uint)second, destination, 23);
		destination[25] = 32;
		destination[26] = 71;
		destination[27] = 77;
		destination[28] = 84;
		bytesWritten = 29;
		return true;
	}

	public unsafe static bool TryFormat(decimal value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		if (format.IsDefault)
		{
			format = 'G';
		}
		switch (format.Symbol)
		{
		case 'G':
		case 'g':
		{
			if (format.Precision != byte.MaxValue)
			{
				throw new NotSupportedException(SR.Argument_GWithPrecisionNotSupported);
			}
			byte* digits3 = stackalloc byte[31];
			Number.NumberBuffer number3 = new Number.NumberBuffer(Number.NumberBufferKind.Decimal, digits3, 31);
			Number.DecimalToNumber(ref value, ref number3);
			if (number3.Digits[0] == 0)
			{
				number3.IsNegative = false;
			}
			return TryFormatDecimalG(ref number3, destination, out bytesWritten);
		}
		case 'F':
		case 'f':
		{
			byte* digits2 = stackalloc byte[31];
			Number.NumberBuffer number2 = new Number.NumberBuffer(Number.NumberBufferKind.Decimal, digits2, 31);
			Number.DecimalToNumber(ref value, ref number2);
			byte b2 = (byte)((format.Precision == byte.MaxValue) ? 2 : format.Precision);
			Number.RoundNumber(ref number2, number2.Scale + b2, isCorrectlyRounded: false);
			return TryFormatDecimalF(ref number2, destination, out bytesWritten, b2);
		}
		case 'E':
		case 'e':
		{
			byte* digits = stackalloc byte[31];
			Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.Decimal, digits, 31);
			Number.DecimalToNumber(ref value, ref number);
			byte b = (byte)((format.Precision == byte.MaxValue) ? 6 : format.Precision);
			Number.RoundNumber(ref number, b + 1, isCorrectlyRounded: false);
			return TryFormatDecimalE(ref number, destination, out bytesWritten, b, (byte)format.Symbol);
		}
		default:
			return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
		}
	}

	private static bool TryFormatDecimalE(ref Number.NumberBuffer number, Span<byte> destination, out int bytesWritten, byte precision, byte exponentSymbol)
	{
		int scale = number.Scale;
		ReadOnlySpan<byte> readOnlySpan = number.Digits;
		int num = (number.IsNegative ? 1 : 0) + 1 + ((precision != 0) ? (precision + 1) : 0) + 2 + 3;
		if (destination.Length < num)
		{
			bytesWritten = 0;
			return false;
		}
		int num2 = 0;
		int num3 = 0;
		if (number.IsNegative)
		{
			destination[num2++] = 45;
		}
		byte b = readOnlySpan[num3];
		int num4;
		if (b == 0)
		{
			destination[num2++] = 48;
			num4 = 0;
		}
		else
		{
			destination[num2++] = b;
			num3++;
			num4 = scale - 1;
		}
		if (precision > 0)
		{
			destination[num2++] = 46;
			for (int i = 0; i < precision; i++)
			{
				byte b2 = readOnlySpan[num3];
				if (b2 == 0)
				{
					while (i++ < precision)
					{
						destination[num2++] = 48;
					}
					break;
				}
				destination[num2++] = b2;
				num3++;
			}
		}
		destination[num2++] = exponentSymbol;
		if (num4 >= 0)
		{
			destination[num2++] = 43;
		}
		else
		{
			destination[num2++] = 45;
			num4 = -num4;
		}
		destination[num2++] = 48;
		destination[num2++] = (byte)(num4 / 10 + 48);
		destination[num2++] = (byte)(num4 % 10 + 48);
		bytesWritten = num;
		return true;
	}

	private static bool TryFormatDecimalF(ref Number.NumberBuffer number, Span<byte> destination, out int bytesWritten, byte precision)
	{
		int scale = number.Scale;
		ReadOnlySpan<byte> readOnlySpan = number.Digits;
		int num = (number.IsNegative ? 1 : 0) + ((scale <= 0) ? 1 : scale) + ((precision != 0) ? (precision + 1) : 0);
		if (destination.Length < num)
		{
			bytesWritten = 0;
			return false;
		}
		int i = 0;
		int num2 = 0;
		if (number.IsNegative)
		{
			destination[num2++] = 45;
		}
		if (scale <= 0)
		{
			destination[num2++] = 48;
		}
		else
		{
			for (; i < scale; i++)
			{
				byte b = readOnlySpan[i];
				if (b == 0)
				{
					int num3 = scale - i;
					for (int j = 0; j < num3; j++)
					{
						destination[num2++] = 48;
					}
					break;
				}
				destination[num2++] = b;
			}
		}
		if (precision > 0)
		{
			destination[num2++] = 46;
			int k = 0;
			if (scale < 0)
			{
				int num4 = Math.Min(precision, -scale);
				for (int l = 0; l < num4; l++)
				{
					destination[num2++] = 48;
				}
				k += num4;
			}
			for (; k < precision; k++)
			{
				byte b2 = readOnlySpan[i];
				if (b2 == 0)
				{
					while (k++ < precision)
					{
						destination[num2++] = 48;
					}
					break;
				}
				destination[num2++] = b2;
				i++;
			}
		}
		bytesWritten = num;
		return true;
	}

	private static bool TryFormatDecimalG(ref Number.NumberBuffer number, Span<byte> destination, out int bytesWritten)
	{
		int scale = number.Scale;
		ReadOnlySpan<byte> readOnlySpan = number.Digits;
		int digitsCount = number.DigitsCount;
		bool flag = scale < digitsCount;
		int num;
		if (flag)
		{
			num = digitsCount + 1;
			if (scale <= 0)
			{
				num += 1 + -scale;
			}
		}
		else
		{
			num = ((scale <= 0) ? 1 : scale);
		}
		if (number.IsNegative)
		{
			num++;
		}
		if (destination.Length < num)
		{
			bytesWritten = 0;
			return false;
		}
		int i = 0;
		int num2 = 0;
		if (number.IsNegative)
		{
			destination[num2++] = 45;
		}
		if (scale <= 0)
		{
			destination[num2++] = 48;
		}
		else
		{
			for (; i < scale; i++)
			{
				byte b = readOnlySpan[i];
				if (b == 0)
				{
					int num3 = scale - i;
					for (int j = 0; j < num3; j++)
					{
						destination[num2++] = 48;
					}
					break;
				}
				destination[num2++] = b;
			}
		}
		if (flag)
		{
			destination[num2++] = 46;
			if (scale < 0)
			{
				int num4 = -scale;
				for (int k = 0; k < num4; k++)
				{
					destination[num2++] = 48;
				}
			}
			byte b2;
			while ((b2 = readOnlySpan[i++]) != 0)
			{
				destination[num2++] = b2;
			}
		}
		bytesWritten = num;
		return true;
	}

	public static bool TryFormat(double value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormatFloatingPoint(value, destination, out bytesWritten, format);
	}

	public static bool TryFormat(float value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormatFloatingPoint(value, destination, out bytesWritten, format);
	}

	private static bool TryFormatFloatingPoint<T>(T value, Span<byte> destination, out int bytesWritten, StandardFormat format) where T : ISpanFormattable
	{
		Span<char> span = default(Span<char>);
		if (!format.IsDefault)
		{
			span = stackalloc char[3];
			span = span[..format.Format(span)];
		}
		Span<char> destination2 = stackalloc char[128];
		ReadOnlySpan<char> readOnlySpan = default(Span<char>);
		if (value.TryFormat(destination2, out var charsWritten, span, CultureInfo.InvariantCulture))
		{
			readOnlySpan = destination2.Slice(0, charsWritten);
		}
		else
		{
			if (destination.Length <= 128)
			{
				bytesWritten = 0;
				return false;
			}
			readOnlySpan = value.ToString(new string(span), CultureInfo.InvariantCulture);
		}
		if (readOnlySpan.Length > destination.Length)
		{
			bytesWritten = 0;
			return false;
		}
		try
		{
			bytesWritten = Encoding.UTF8.GetBytes(readOnlySpan, destination);
			return true;
		}
		catch
		{
			bytesWritten = 0;
			return false;
		}
	}

	public static bool TryFormat(Guid value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		int num;
		switch (FormattingHelpers.GetSymbolOrDefault(in format, 'D'))
		{
		case 'D':
			num = -2147483612;
			break;
		case 'B':
			num = -2139260122;
			break;
		case 'P':
			num = -2144786394;
			break;
		case 'N':
			num = 32;
			break;
		default:
			return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
		}
		if ((byte)num > destination.Length)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = (byte)num;
		num >>= 8;
		if ((byte)num != 0)
		{
			destination[0] = (byte)num;
			destination = destination.Slice(1);
		}
		num >>= 8;
		DecomposedGuid decomposedGuid = default(DecomposedGuid);
		decomposedGuid.Guid = value;
		_ = ref destination[8];
		_ = BitConverter.IsLittleEndian;
		HexConverter.ToBytesBuffer(decomposedGuid.Byte03, destination, 0, HexConverter.Casing.Lower);
		HexConverter.ToBytesBuffer(decomposedGuid.Byte02, destination, 2, HexConverter.Casing.Lower);
		HexConverter.ToBytesBuffer(decomposedGuid.Byte01, destination, 4, HexConverter.Casing.Lower);
		HexConverter.ToBytesBuffer(decomposedGuid.Byte00, destination, 6, HexConverter.Casing.Lower);
		if (num < 0)
		{
			destination[8] = 45;
			destination = destination.Slice(9);
		}
		else
		{
			destination = destination.Slice(8);
		}
		_ = ref destination[4];
		_ = BitConverter.IsLittleEndian;
		HexConverter.ToBytesBuffer(decomposedGuid.Byte05, destination, 0, HexConverter.Casing.Lower);
		HexConverter.ToBytesBuffer(decomposedGuid.Byte04, destination, 2, HexConverter.Casing.Lower);
		if (num < 0)
		{
			destination[4] = 45;
			destination = destination.Slice(5);
		}
		else
		{
			destination = destination.Slice(4);
		}
		_ = ref destination[4];
		_ = BitConverter.IsLittleEndian;
		HexConverter.ToBytesBuffer(decomposedGuid.Byte07, destination, 0, HexConverter.Casing.Lower);
		HexConverter.ToBytesBuffer(decomposedGuid.Byte06, destination, 2, HexConverter.Casing.Lower);
		if (num < 0)
		{
			destination[4] = 45;
			destination = destination.Slice(5);
		}
		else
		{
			destination = destination.Slice(4);
		}
		_ = ref destination[4];
		HexConverter.ToBytesBuffer(decomposedGuid.Byte08, destination, 0, HexConverter.Casing.Lower);
		HexConverter.ToBytesBuffer(decomposedGuid.Byte09, destination, 2, HexConverter.Casing.Lower);
		if (num < 0)
		{
			destination[4] = 45;
			destination = destination.Slice(5);
		}
		else
		{
			destination = destination.Slice(4);
		}
		_ = ref destination[11];
		HexConverter.ToBytesBuffer(decomposedGuid.Byte10, destination, 0, HexConverter.Casing.Lower);
		HexConverter.ToBytesBuffer(decomposedGuid.Byte11, destination, 2, HexConverter.Casing.Lower);
		HexConverter.ToBytesBuffer(decomposedGuid.Byte12, destination, 4, HexConverter.Casing.Lower);
		HexConverter.ToBytesBuffer(decomposedGuid.Byte13, destination, 6, HexConverter.Casing.Lower);
		HexConverter.ToBytesBuffer(decomposedGuid.Byte14, destination, 8, HexConverter.Casing.Lower);
		HexConverter.ToBytesBuffer(decomposedGuid.Byte15, destination, 10, HexConverter.Casing.Lower);
		if ((byte)num != 0)
		{
			destination[12] = (byte)num;
		}
		return true;
	}

	public static bool TryFormat(byte value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormatUInt64(value, destination, out bytesWritten, format);
	}

	[CLSCompliant(false)]
	public static bool TryFormat(sbyte value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormatInt64(value, 255uL, destination, out bytesWritten, format);
	}

	[CLSCompliant(false)]
	public static bool TryFormat(ushort value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormatUInt64(value, destination, out bytesWritten, format);
	}

	public static bool TryFormat(short value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormatInt64(value, 65535uL, destination, out bytesWritten, format);
	}

	[CLSCompliant(false)]
	public static bool TryFormat(uint value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormatUInt64(value, destination, out bytesWritten, format);
	}

	public static bool TryFormat(int value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormatInt64(value, 4294967295uL, destination, out bytesWritten, format);
	}

	[CLSCompliant(false)]
	public static bool TryFormat(ulong value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormatUInt64(value, destination, out bytesWritten, format);
	}

	public static bool TryFormat(long value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		return TryFormatInt64(value, ulong.MaxValue, destination, out bytesWritten, format);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryFormatInt64(long value, ulong mask, Span<byte> destination, out int bytesWritten, StandardFormat format)
	{
		if (format.IsDefault)
		{
			return TryFormatInt64Default(value, destination, out bytesWritten);
		}
		switch (format.Symbol)
		{
		case 'G':
		case 'g':
			if (format.HasPrecision)
			{
				throw new NotSupportedException(SR.Argument_GWithPrecisionNotSupported);
			}
			return TryFormatInt64D(value, format.Precision, destination, out bytesWritten);
		case 'D':
		case 'd':
			return TryFormatInt64D(value, format.Precision, destination, out bytesWritten);
		case 'N':
		case 'n':
			return TryFormatInt64N(value, format.Precision, destination, out bytesWritten);
		case 'x':
			return TryFormatUInt64X((ulong)value & mask, format.Precision, useLower: true, destination, out bytesWritten);
		case 'X':
			return TryFormatUInt64X((ulong)value & mask, format.Precision, useLower: false, destination, out bytesWritten);
		default:
			return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryFormatInt64D(long value, byte precision, Span<byte> destination, out int bytesWritten)
	{
		bool insertNegationSign = false;
		if (value < 0)
		{
			insertNegationSign = true;
			value = -value;
		}
		return TryFormatUInt64D((ulong)value, precision, destination, insertNegationSign, out bytesWritten);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryFormatInt64Default(long value, Span<byte> destination, out int bytesWritten)
	{
		if ((ulong)value < 10uL)
		{
			return TryFormatUInt32SingleDigit((uint)value, destination, out bytesWritten);
		}
		_ = IntPtr.Size;
		return TryFormatInt64MultipleDigits(value, destination, out bytesWritten);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryFormatInt64MultipleDigits(long value, Span<byte> destination, out int bytesWritten)
	{
		if (value < 0)
		{
			value = -value;
			int num = FormattingHelpers.CountDigits((ulong)value);
			if (num >= destination.Length)
			{
				bytesWritten = 0;
				return false;
			}
			destination[0] = 45;
			bytesWritten = num + 1;
			FormattingHelpers.WriteDigits((ulong)value, destination.Slice(1, num));
			return true;
		}
		return TryFormatUInt64MultipleDigits((ulong)value, destination, out bytesWritten);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryFormatInt64N(long value, byte precision, Span<byte> destination, out int bytesWritten)
	{
		bool insertNegationSign = false;
		if (value < 0)
		{
			insertNegationSign = true;
			value = -value;
		}
		return TryFormatUInt64N((ulong)value, precision, destination, insertNegationSign, out bytesWritten);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryFormatUInt64(ulong value, Span<byte> destination, out int bytesWritten, StandardFormat format)
	{
		if (format.IsDefault)
		{
			return TryFormatUInt64Default(value, destination, out bytesWritten);
		}
		switch (format.Symbol)
		{
		case 'G':
		case 'g':
			if (format.HasPrecision)
			{
				throw new NotSupportedException(SR.Argument_GWithPrecisionNotSupported);
			}
			return TryFormatUInt64D(value, format.Precision, destination, insertNegationSign: false, out bytesWritten);
		case 'D':
		case 'd':
			return TryFormatUInt64D(value, format.Precision, destination, insertNegationSign: false, out bytesWritten);
		case 'N':
		case 'n':
			return TryFormatUInt64N(value, format.Precision, destination, insertNegationSign: false, out bytesWritten);
		case 'x':
			return TryFormatUInt64X(value, format.Precision, useLower: true, destination, out bytesWritten);
		case 'X':
			return TryFormatUInt64X(value, format.Precision, useLower: false, destination, out bytesWritten);
		default:
			return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
		}
	}

	private static bool TryFormatUInt64D(ulong value, byte precision, Span<byte> destination, bool insertNegationSign, out int bytesWritten)
	{
		int num = FormattingHelpers.CountDigits(value);
		int num2 = ((precision != byte.MaxValue) ? precision : 0) - num;
		if (num2 < 0)
		{
			num2 = 0;
		}
		int num3 = num + num2;
		if (insertNegationSign)
		{
			num3++;
		}
		if (num3 > destination.Length)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = num3;
		if (insertNegationSign)
		{
			destination[0] = 45;
			destination = destination.Slice(1);
		}
		if (num2 > 0)
		{
			FormattingHelpers.FillWithAsciiZeros(destination.Slice(0, num2));
		}
		FormattingHelpers.WriteDigits(value, destination.Slice(num2, num));
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryFormatUInt64Default(ulong value, Span<byte> destination, out int bytesWritten)
	{
		if (value < 10)
		{
			return TryFormatUInt32SingleDigit((uint)value, destination, out bytesWritten);
		}
		_ = IntPtr.Size;
		return TryFormatUInt64MultipleDigits(value, destination, out bytesWritten);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryFormatUInt32SingleDigit(uint value, Span<byte> destination, out int bytesWritten)
	{
		if (destination.Length == 0)
		{
			bytesWritten = 0;
			return false;
		}
		destination[0] = (byte)(48 + value);
		bytesWritten = 1;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryFormatUInt64MultipleDigits(ulong value, Span<byte> destination, out int bytesWritten)
	{
		int num = FormattingHelpers.CountDigits(value);
		if (num > destination.Length)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = num;
		FormattingHelpers.WriteDigits(value, destination.Slice(0, num));
		return true;
	}

	private static bool TryFormatUInt64N(ulong value, byte precision, Span<byte> destination, bool insertNegationSign, out int bytesWritten)
	{
		int num = FormattingHelpers.CountDigits(value);
		int num2 = (num - 1) / 3;
		int num3 = ((precision == byte.MaxValue) ? 2 : precision);
		int num4 = num + num2;
		if (num3 > 0)
		{
			num4 += num3 + 1;
		}
		if (insertNegationSign)
		{
			num4++;
		}
		if (num4 > destination.Length)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = num4;
		if (insertNegationSign)
		{
			destination[0] = 45;
			destination = destination.Slice(1);
		}
		FormattingHelpers.WriteDigitsWithGroupSeparator(value, destination.Slice(0, num + num2));
		if (num3 > 0)
		{
			destination[num + num2] = 46;
			FormattingHelpers.FillWithAsciiZeros(destination.Slice(num + num2 + 1, num3));
		}
		return true;
	}

	private static bool TryFormatUInt64X(ulong value, byte precision, bool useLower, Span<byte> destination, out int bytesWritten)
	{
		int num = FormattingHelpers.CountHexDigits(value);
		int num2 = ((precision == byte.MaxValue) ? num : Math.Max(precision, num));
		if (destination.Length < num2)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = num2;
		if (useLower)
		{
			while ((uint)(--num2) < (uint)destination.Length)
			{
				destination[num2] = (byte)HexConverter.ToCharLower((int)value);
				value >>= 4;
			}
		}
		else
		{
			while ((uint)(--num2) < (uint)destination.Length)
			{
				destination[num2] = (byte)HexConverter.ToCharUpper((int)value);
				value >>= 4;
			}
		}
		return true;
	}

	public static bool TryFormat(TimeSpan value, Span<byte> destination, out int bytesWritten, StandardFormat format = default(StandardFormat))
	{
		char c = FormattingHelpers.GetSymbolOrDefault(in format, 'c');
		switch (c)
		{
		case 'T':
		case 't':
			c = 'c';
			break;
		default:
			return FormattingHelpers.TryFormatThrowFormatException(out bytesWritten);
		case 'G':
		case 'c':
		case 'g':
			break;
		}
		int num = 8;
		long ticks = value.Ticks;
		uint valueWithoutTrailingZeros;
		ulong num2;
		if (ticks < 0)
		{
			ticks = -ticks;
			if (ticks < 0)
			{
				valueWithoutTrailingZeros = 4775808u;
				num2 = 922337203685uL;
				goto IL_008d;
			}
		}
		(ulong Quotient, ulong Remainder) tuple = Math.DivRem((ulong)Math.Abs(value.Ticks), 10000000uL);
		num2 = tuple.Quotient;
		ulong item = tuple.Remainder;
		valueWithoutTrailingZeros = (uint)item;
		goto IL_008d;
		IL_008d:
		int num3 = 0;
		switch (c)
		{
		case 'c':
			if (valueWithoutTrailingZeros != 0)
			{
				num3 = 7;
			}
			break;
		case 'G':
			num3 = 7;
			break;
		default:
			if (valueWithoutTrailingZeros != 0)
			{
				num3 = 7 - FormattingHelpers.CountDecimalTrailingZeros(valueWithoutTrailingZeros, out valueWithoutTrailingZeros);
			}
			break;
		}
		if (num3 != 0)
		{
			num += num3 + 1;
		}
		ulong num4 = 0uL;
		ulong num5 = 0uL;
		if (num2 != 0)
		{
			(num4, num5) = Math.DivRem(num2, 60uL);
		}
		ulong num6 = 0uL;
		ulong num7 = 0uL;
		if (num4 != 0)
		{
			(num6, num7) = Math.DivRem(num4, 60uL);
		}
		uint num8 = 0u;
		uint num9 = 0u;
		if (num6 != 0)
		{
			(num8, num9) = Math.DivRem((uint)num6, 24u);
		}
		int num10 = 2;
		if (num9 < 10 && c == 'g')
		{
			num10--;
			num--;
		}
		int num11 = 0;
		if (num8 == 0)
		{
			if (c == 'G')
			{
				num += 2;
				num11 = 1;
			}
		}
		else
		{
			num11 = FormattingHelpers.CountDigits(num8);
			num += num11 + 1;
		}
		if (value.Ticks < 0)
		{
			num++;
		}
		if (destination.Length < num)
		{
			bytesWritten = 0;
			return false;
		}
		bytesWritten = num;
		int num12 = 0;
		if (value.Ticks < 0)
		{
			destination[num12++] = 45;
		}
		if (num11 > 0)
		{
			FormattingHelpers.WriteDigits(num8, destination.Slice(num12, num11));
			num12 += num11;
			destination[num12++] = (byte)((c == 'c') ? 46 : 58);
		}
		FormattingHelpers.WriteDigits(num9, destination.Slice(num12, num10));
		num12 += num10;
		destination[num12++] = 58;
		FormattingHelpers.WriteDigits((uint)num7, destination.Slice(num12, 2));
		num12 += 2;
		destination[num12++] = 58;
		FormattingHelpers.WriteDigits((uint)num5, destination.Slice(num12, 2));
		num12 += 2;
		if (num3 > 0)
		{
			destination[num12++] = 46;
			FormattingHelpers.WriteDigits(valueWithoutTrailingZeros, destination.Slice(num12, num3));
			num12 += num3;
		}
		return true;
	}
}
