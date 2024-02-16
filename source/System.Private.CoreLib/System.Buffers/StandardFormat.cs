using System.Diagnostics.CodeAnalysis;

namespace System.Buffers;

public readonly struct StandardFormat : IEquatable<StandardFormat>
{
	public const byte NoPrecision = byte.MaxValue;

	public const byte MaxPrecision = 99;

	private readonly byte _format;

	private readonly byte _precision;

	public char Symbol => (char)_format;

	public byte Precision => _precision;

	public bool HasPrecision => _precision != byte.MaxValue;

	public bool IsDefault
	{
		get
		{
			if (_format == 0)
			{
				return _precision == 0;
			}
			return false;
		}
	}

	public StandardFormat(char symbol, byte precision = byte.MaxValue)
	{
		if (precision != byte.MaxValue && precision > 99)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_PrecisionTooLarge();
		}
		if (symbol != (byte)symbol)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_SymbolDoesNotFit();
		}
		_format = (byte)symbol;
		_precision = precision;
	}

	public static implicit operator StandardFormat(char symbol)
	{
		return new StandardFormat(symbol);
	}

	public static StandardFormat Parse(ReadOnlySpan<char> format)
	{
		ParseHelper(format, out var standardFormat, throws: true);
		return standardFormat;
	}

	public static StandardFormat Parse(string? format)
	{
		if (format != null)
		{
			return Parse(format.AsSpan());
		}
		return default(StandardFormat);
	}

	public static bool TryParse(ReadOnlySpan<char> format, out StandardFormat result)
	{
		return ParseHelper(format, out result);
	}

	private static bool ParseHelper(ReadOnlySpan<char> format, out StandardFormat standardFormat, bool throws = false)
	{
		standardFormat = default(StandardFormat);
		if (format.Length == 0)
		{
			return true;
		}
		char symbol = format[0];
		byte precision;
		if (format.Length == 1)
		{
			precision = byte.MaxValue;
		}
		else
		{
			uint num = 0u;
			for (int i = 1; i < format.Length; i++)
			{
				uint num2 = (uint)(format[i] - 48);
				if (num2 > 9)
				{
					if (!throws)
					{
						return false;
					}
					throw new FormatException(SR.Format(SR.Argument_CannotParsePrecision, (byte)99));
				}
				num = num * 10 + num2;
				if (num > 99)
				{
					if (!throws)
					{
						return false;
					}
					throw new FormatException(SR.Format(SR.Argument_PrecisionTooLarge, (byte)99));
				}
			}
			precision = (byte)num;
		}
		standardFormat = new StandardFormat(symbol, precision);
		return true;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is StandardFormat other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _format.GetHashCode() ^ _precision.GetHashCode();
	}

	public bool Equals(StandardFormat other)
	{
		if (_format == other._format)
		{
			return _precision == other._precision;
		}
		return false;
	}

	public override string ToString()
	{
		Span<char> destination = stackalloc char[3];
		return new string(destination[..Format(destination)]);
	}

	internal int Format(Span<char> destination)
	{
		int num = 0;
		char symbol = Symbol;
		if (symbol != 0 && destination.Length == 3)
		{
			destination[0] = symbol;
			num = 1;
			uint num2 = Precision;
			if (num2 != 255)
			{
				if (num2 >= 10)
				{
					uint num3;
					(num3, num2) = Math.DivRem(num2, 10u);
					destination[1] = (char)(48 + num3 % 10);
					num = 2;
				}
				destination[num] = (char)(48 + num2);
				num++;
			}
		}
		return num;
	}

	public static bool operator ==(StandardFormat left, StandardFormat right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(StandardFormat left, StandardFormat right)
	{
		return !left.Equals(right);
	}
}
