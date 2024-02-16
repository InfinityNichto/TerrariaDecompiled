using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Numerics;

[Serializable]
[TypeForwardedFrom("System.Numerics, Version=4.0.0.0, PublicKeyToken=b77a5c561934e089")]
public readonly struct BigInteger : ISpanFormattable, IFormattable, IComparable, IComparable<BigInteger>, IEquatable<BigInteger>
{
	private enum GetBytesMode
	{
		AllocateArray,
		Count,
		Span
	}

	internal readonly int _sign;

	internal readonly uint[] _bits;

	private static readonly BigInteger s_bnMinInt = new BigInteger(-1, new uint[1] { 2147483648u });

	private static readonly BigInteger s_bnOneInt = new BigInteger(1);

	private static readonly BigInteger s_bnZeroInt = new BigInteger(0);

	private static readonly BigInteger s_bnMinusOneInt = new BigInteger(-1);

	private static readonly byte[] s_success = Array.Empty<byte>();

	public static BigInteger Zero => s_bnZeroInt;

	public static BigInteger One => s_bnOneInt;

	public static BigInteger MinusOne => s_bnMinusOneInt;

	public bool IsPowerOfTwo
	{
		get
		{
			if (_bits == null)
			{
				if ((_sign & (_sign - 1)) == 0)
				{
					return _sign != 0;
				}
				return false;
			}
			if (_sign != 1)
			{
				return false;
			}
			int num = _bits.Length - 1;
			if ((_bits[num] & (_bits[num] - 1)) != 0)
			{
				return false;
			}
			while (--num >= 0)
			{
				if (_bits[num] != 0)
				{
					return false;
				}
			}
			return true;
		}
	}

	public bool IsZero => _sign == 0;

	public bool IsOne
	{
		get
		{
			if (_sign == 1)
			{
				return _bits == null;
			}
			return false;
		}
	}

	public bool IsEven
	{
		get
		{
			if (_bits != null)
			{
				return (_bits[0] & 1) == 0;
			}
			return (_sign & 1) == 0;
		}
	}

	public int Sign => (_sign >> 31) - (-_sign >> 31);

	public BigInteger(int value)
	{
		if (value == int.MinValue)
		{
			this = s_bnMinInt;
			return;
		}
		_sign = value;
		_bits = null;
	}

	[CLSCompliant(false)]
	public BigInteger(uint value)
	{
		if (value <= int.MaxValue)
		{
			_sign = (int)value;
			_bits = null;
		}
		else
		{
			_sign = 1;
			_bits = new uint[1];
			_bits[0] = value;
		}
	}

	public BigInteger(long value)
	{
		if (int.MinValue < value && value <= int.MaxValue)
		{
			_sign = (int)value;
			_bits = null;
			return;
		}
		if (value == int.MinValue)
		{
			this = s_bnMinInt;
			return;
		}
		ulong num = 0uL;
		if (value < 0)
		{
			num = (ulong)(-value);
			_sign = -1;
		}
		else
		{
			num = (ulong)value;
			_sign = 1;
		}
		if (num <= uint.MaxValue)
		{
			_bits = new uint[1];
			_bits[0] = (uint)num;
		}
		else
		{
			_bits = new uint[2];
			_bits[0] = (uint)num;
			_bits[1] = (uint)(num >> 32);
		}
	}

	[CLSCompliant(false)]
	public BigInteger(ulong value)
	{
		if (value <= int.MaxValue)
		{
			_sign = (int)value;
			_bits = null;
		}
		else if (value <= uint.MaxValue)
		{
			_sign = 1;
			_bits = new uint[1];
			_bits[0] = (uint)value;
		}
		else
		{
			_sign = 1;
			_bits = new uint[2];
			_bits[0] = (uint)value;
			_bits[1] = (uint)(value >> 32);
		}
	}

	public BigInteger(float value)
		: this((double)value)
	{
	}

	public BigInteger(double value)
	{
		if (!double.IsFinite(value))
		{
			if (double.IsInfinity(value))
			{
				throw new OverflowException(System.SR.Overflow_BigIntInfinity);
			}
			throw new OverflowException(System.SR.Overflow_NotANumber);
		}
		_sign = 0;
		_bits = null;
		NumericsHelpers.GetDoubleParts(value, out var sign, out var exp, out var man, out var _);
		if (man == 0L)
		{
			this = Zero;
			return;
		}
		if (exp <= 0)
		{
			if (exp <= -64)
			{
				this = Zero;
				return;
			}
			this = man >> -exp;
			if (sign < 0)
			{
				_sign = -_sign;
			}
			return;
		}
		if (exp <= 11)
		{
			this = man << exp;
			if (sign < 0)
			{
				_sign = -_sign;
			}
			return;
		}
		man <<= 11;
		exp -= 11;
		int num = (exp - 1) / 32 + 1;
		int num2 = num * 32 - exp;
		_bits = new uint[num + 2];
		_bits[num + 1] = (uint)(man >> num2 + 32);
		_bits[num] = (uint)(man >> num2);
		if (num2 > 0)
		{
			_bits[num - 1] = (uint)((int)man << 32 - num2);
		}
		_sign = sign;
	}

	public BigInteger(decimal value)
	{
		Span<int> destination = stackalloc int[4];
		decimal.GetBits(decimal.Truncate(value), destination);
		int num = 3;
		while (num > 0 && destination[num - 1] == 0)
		{
			num--;
		}
		switch (num)
		{
		case 0:
			this = s_bnZeroInt;
			return;
		case 1:
			if (destination[0] > 0)
			{
				_sign = destination[0];
				_sign *= (((destination[3] & int.MinValue) == 0) ? 1 : (-1));
				_bits = null;
				return;
			}
			break;
		}
		_bits = new uint[num];
		_bits[0] = (uint)destination[0];
		if (num > 1)
		{
			_bits[1] = (uint)destination[1];
		}
		if (num > 2)
		{
			_bits[2] = (uint)destination[2];
		}
		_sign = (((destination[3] & int.MinValue) == 0) ? 1 : (-1));
	}

	[CLSCompliant(false)]
	public BigInteger(byte[] value)
		: this(new ReadOnlySpan<byte>(value ?? throw new ArgumentNullException("value")))
	{
	}

	public BigInteger(ReadOnlySpan<byte> value, bool isUnsigned = false, bool isBigEndian = false)
	{
		int num = value.Length;
		bool flag;
		if (num > 0)
		{
			byte b = (isBigEndian ? value[0] : value[num - 1]);
			flag = (b & 0x80u) != 0 && !isUnsigned;
			if (b == 0)
			{
				if (isBigEndian)
				{
					int i;
					for (i = 1; i < num && value[i] == 0; i++)
					{
					}
					value = value.Slice(i);
					num = value.Length;
				}
				else
				{
					num -= 2;
					while (num >= 0 && value[num] == 0)
					{
						num--;
					}
					num++;
				}
			}
		}
		else
		{
			flag = false;
		}
		if (num == 0)
		{
			_sign = 0;
			_bits = null;
			return;
		}
		if (num <= 4)
		{
			_sign = (flag ? (-1) : 0);
			if (isBigEndian)
			{
				for (int j = 0; j < num; j++)
				{
					_sign = (_sign << 8) | value[j];
				}
			}
			else
			{
				for (int num2 = num - 1; num2 >= 0; num2--)
				{
					_sign = (_sign << 8) | value[num2];
				}
			}
			_bits = null;
			if (_sign < 0 && !flag)
			{
				_bits = new uint[1] { (uint)_sign };
				_sign = 1;
			}
			if (_sign == int.MinValue)
			{
				this = s_bnMinInt;
			}
			return;
		}
		int num3 = num % 4;
		int num4 = num / 4 + ((num3 != 0) ? 1 : 0);
		uint[] array = new uint[num4];
		int num5 = num - 1;
		int k;
		if (isBigEndian)
		{
			int num6 = num - 4;
			for (k = 0; k < num4 - ((num3 != 0) ? 1 : 0); k++)
			{
				for (int l = 0; l < 4; l++)
				{
					byte b2 = value[num6];
					array[k] = (array[k] << 8) | b2;
					num6++;
				}
				num6 -= 8;
			}
		}
		else
		{
			int num6 = 3;
			for (k = 0; k < num4 - ((num3 != 0) ? 1 : 0); k++)
			{
				for (int m = 0; m < 4; m++)
				{
					byte b3 = value[num6];
					array[k] = (array[k] << 8) | b3;
					num6--;
				}
				num6 += 8;
			}
		}
		if (num3 != 0)
		{
			if (flag)
			{
				array[num4 - 1] = uint.MaxValue;
			}
			if (isBigEndian)
			{
				for (int num6 = 0; num6 < num3; num6++)
				{
					byte b4 = value[num6];
					array[k] = (array[k] << 8) | b4;
				}
			}
			else
			{
				for (int num6 = num5; num6 >= num - num3; num6--)
				{
					byte b5 = value[num6];
					array[k] = (array[k] << 8) | b5;
				}
			}
		}
		if (flag)
		{
			NumericsHelpers.DangerousMakeTwosComplement(array);
			int num7 = array.Length - 1;
			while (num7 >= 0 && array[num7] == 0)
			{
				num7--;
			}
			num7++;
			if (num7 == 1)
			{
				switch (array[0])
				{
				case 1u:
					this = s_bnMinusOneInt;
					return;
				case 2147483648u:
					this = s_bnMinInt;
					return;
				}
				if ((int)array[0] > 0)
				{
					_sign = -1 * (int)array[0];
					_bits = null;
					return;
				}
			}
			if (num7 != array.Length)
			{
				_sign = -1;
				_bits = new uint[num7];
				Array.Copy(array, _bits, num7);
			}
			else
			{
				_sign = -1;
				_bits = array;
			}
		}
		else
		{
			_sign = 1;
			_bits = array;
		}
	}

	internal BigInteger(int n, uint[] rgu)
	{
		_sign = n;
		_bits = rgu;
	}

	private BigInteger(ReadOnlySpan<uint> value, uint[] valueArray, bool negative)
	{
		if (!value.IsEmpty && value[^1] == 0)
		{
			int num = value.Length - 1;
			while (num > 0 && value[num - 1] == 0)
			{
				num--;
			}
			value = value.Slice(0, num);
			valueArray = null;
		}
		if (value.IsEmpty)
		{
			this = s_bnZeroInt;
		}
		else if (value.Length == 1 && value[0] < 2147483648u)
		{
			_sign = (int)(negative ? (0 - value[0]) : value[0]);
			_bits = null;
			if (_sign == int.MinValue)
			{
				this = s_bnMinInt;
			}
		}
		else
		{
			_sign = ((!negative) ? 1 : (-1));
			_bits = valueArray ?? value.ToArray();
		}
	}

	private BigInteger(uint[] value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		int num = value.Length;
		bool flag = num > 0 && (value[num - 1] & 0x80000000u) == 2147483648u;
		while (num > 0 && value[num - 1] == 0)
		{
			num--;
		}
		switch (num)
		{
		case 0:
			this = s_bnZeroInt;
			return;
		case 1:
			if ((int)value[0] < 0 && !flag)
			{
				_bits = new uint[1];
				_bits[0] = value[0];
				_sign = 1;
			}
			else if (int.MinValue == (int)value[0])
			{
				this = s_bnMinInt;
			}
			else
			{
				_sign = (int)value[0];
				_bits = null;
			}
			return;
		}
		if (!flag)
		{
			if (num != value.Length)
			{
				_sign = 1;
				_bits = new uint[num];
				Array.Copy(value, _bits, num);
			}
			else
			{
				_sign = 1;
				_bits = value;
			}
			return;
		}
		NumericsHelpers.DangerousMakeTwosComplement(value);
		int num2 = value.Length;
		while (num2 > 0 && value[num2 - 1] == 0)
		{
			num2--;
		}
		if (num2 == 1 && (int)value[0] > 0)
		{
			if (value[0] == 1)
			{
				this = s_bnMinusOneInt;
				return;
			}
			if (value[0] == 2147483648u)
			{
				this = s_bnMinInt;
				return;
			}
			_sign = -1 * (int)value[0];
			_bits = null;
		}
		else if (num2 != value.Length)
		{
			_sign = -1;
			_bits = new uint[num2];
			Array.Copy(value, _bits, num2);
		}
		else
		{
			_sign = -1;
			_bits = value;
		}
	}

	public static BigInteger Parse(string value)
	{
		return Parse(value, NumberStyles.Integer);
	}

	public static BigInteger Parse(string value, NumberStyles style)
	{
		return Parse(value, style, NumberFormatInfo.CurrentInfo);
	}

	public static BigInteger Parse(string value, IFormatProvider? provider)
	{
		return Parse(value, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
	}

	public static BigInteger Parse(string value, NumberStyles style, IFormatProvider? provider)
	{
		return BigNumber.ParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? value, out BigInteger result)
	{
		return TryParse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? value, NumberStyles style, IFormatProvider? provider, out BigInteger result)
	{
		return BigNumber.TryParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static BigInteger Parse(ReadOnlySpan<char> value, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		return BigNumber.ParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse(ReadOnlySpan<char> value, out BigInteger result)
	{
		return TryParse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> value, NumberStyles style, IFormatProvider? provider, out BigInteger result)
	{
		return BigNumber.TryParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static int Compare(BigInteger left, BigInteger right)
	{
		return left.CompareTo(right);
	}

	public static BigInteger Abs(BigInteger value)
	{
		if (!(value >= Zero))
		{
			return -value;
		}
		return value;
	}

	public static BigInteger Add(BigInteger left, BigInteger right)
	{
		return left + right;
	}

	public static BigInteger Subtract(BigInteger left, BigInteger right)
	{
		return left - right;
	}

	public static BigInteger Multiply(BigInteger left, BigInteger right)
	{
		return left * right;
	}

	public static BigInteger Divide(BigInteger dividend, BigInteger divisor)
	{
		return dividend / divisor;
	}

	public static BigInteger Remainder(BigInteger dividend, BigInteger divisor)
	{
		return dividend % divisor;
	}

	public static BigInteger DivRem(BigInteger dividend, BigInteger divisor, out BigInteger remainder)
	{
		bool flag = dividend._bits == null;
		bool flag2 = divisor._bits == null;
		if (flag && flag2)
		{
			(int Quotient, int Remainder) tuple = Math.DivRem(dividend._sign, divisor._sign);
			BigInteger bigInteger = tuple.Quotient;
			BigInteger bigInteger2 = tuple.Remainder;
			BigInteger result = bigInteger;
			remainder = bigInteger2;
			return result;
		}
		if (flag)
		{
			remainder = dividend;
			return s_bnZeroInt;
		}
		if (flag2)
		{
			uint remainder2;
			uint[] array = BigIntegerCalculator.Divide(dividend._bits, NumericsHelpers.Abs(divisor._sign), out remainder2);
			remainder = ((dividend._sign < 0) ? (-1 * remainder2) : remainder2);
			return new BigInteger(array, array, (dividend._sign < 0) ^ (divisor._sign < 0));
		}
		if (dividend._bits.Length < divisor._bits.Length)
		{
			remainder = dividend;
			return s_bnZeroInt;
		}
		uint[] remainder3;
		uint[] array2 = BigIntegerCalculator.Divide(dividend._bits, divisor._bits, out remainder3);
		remainder = new BigInteger(remainder3, remainder3, dividend._sign < 0);
		return new BigInteger(array2, array2, (dividend._sign < 0) ^ (divisor._sign < 0));
	}

	public static BigInteger Negate(BigInteger value)
	{
		return -value;
	}

	public static double Log(BigInteger value)
	{
		return Log(value, Math.E);
	}

	public static double Log(BigInteger value, double baseValue)
	{
		if (value._sign < 0 || baseValue == 1.0)
		{
			return double.NaN;
		}
		if (baseValue == double.PositiveInfinity)
		{
			if (!value.IsOne)
			{
				return double.NaN;
			}
			return 0.0;
		}
		if (baseValue == 0.0 && !value.IsOne)
		{
			return double.NaN;
		}
		if (value._bits == null)
		{
			return Math.Log(value._sign, baseValue);
		}
		ulong num = value._bits[value._bits.Length - 1];
		ulong num2 = ((value._bits.Length > 1) ? value._bits[value._bits.Length - 2] : 0u);
		ulong num3 = ((value._bits.Length > 2) ? value._bits[value._bits.Length - 3] : 0u);
		int num4 = NumericsHelpers.CbitHighZero((uint)num);
		long num5 = (long)value._bits.Length * 32L - num4;
		ulong num6 = (num << 32 + num4) | (num2 << num4) | (num3 >> 32 - num4);
		return Math.Log(num6, baseValue) + (double)(num5 - 64) / Math.Log(baseValue, 2.0);
	}

	public static double Log10(BigInteger value)
	{
		return Log(value, 10.0);
	}

	public static BigInteger GreatestCommonDivisor(BigInteger left, BigInteger right)
	{
		bool flag = left._bits == null;
		bool flag2 = right._bits == null;
		if (flag && flag2)
		{
			return BigIntegerCalculator.Gcd(NumericsHelpers.Abs(left._sign), NumericsHelpers.Abs(right._sign));
		}
		if (flag)
		{
			if (left._sign == 0)
			{
				return new BigInteger(right._bits, null, negative: false);
			}
			return BigIntegerCalculator.Gcd(right._bits, NumericsHelpers.Abs(left._sign));
		}
		if (flag2)
		{
			if (right._sign == 0)
			{
				return new BigInteger(left._bits, null, negative: false);
			}
			return BigIntegerCalculator.Gcd(left._bits, NumericsHelpers.Abs(right._sign));
		}
		if (BigIntegerCalculator.Compare(left._bits, right._bits) < 0)
		{
			return GreatestCommonDivisor(right._bits, left._bits);
		}
		return GreatestCommonDivisor(left._bits, right._bits);
	}

	private static BigInteger GreatestCommonDivisor(uint[] leftBits, uint[] rightBits)
	{
		if (rightBits.Length == 1)
		{
			uint right = BigIntegerCalculator.Remainder(leftBits, rightBits[0]);
			return BigIntegerCalculator.Gcd(rightBits[0], right);
		}
		if (rightBits.Length == 2)
		{
			uint[] array = BigIntegerCalculator.Remainder(leftBits, rightBits);
			ulong left = ((ulong)rightBits[1] << 32) | rightBits[0];
			ulong right2 = ((ulong)array[1] << 32) | array[0];
			return BigIntegerCalculator.Gcd(left, right2);
		}
		uint[] array2 = BigIntegerCalculator.Gcd(leftBits, rightBits);
		return new BigInteger(array2, array2, negative: false);
	}

	public static BigInteger Max(BigInteger left, BigInteger right)
	{
		if (left.CompareTo(right) < 0)
		{
			return right;
		}
		return left;
	}

	public static BigInteger Min(BigInteger left, BigInteger right)
	{
		if (left.CompareTo(right) <= 0)
		{
			return left;
		}
		return right;
	}

	public static BigInteger ModPow(BigInteger value, BigInteger exponent, BigInteger modulus)
	{
		if (exponent.Sign < 0)
		{
			throw new ArgumentOutOfRangeException("exponent", System.SR.ArgumentOutOfRange_MustBeNonNeg);
		}
		bool flag = value._bits == null;
		bool flag2 = exponent._bits == null;
		if (modulus._bits == null)
		{
			uint num = ((flag && flag2) ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), NumericsHelpers.Abs(exponent._sign), NumericsHelpers.Abs(modulus._sign)) : (flag ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), exponent._bits, NumericsHelpers.Abs(modulus._sign)) : (flag2 ? BigIntegerCalculator.Pow(value._bits, NumericsHelpers.Abs(exponent._sign), NumericsHelpers.Abs(modulus._sign)) : BigIntegerCalculator.Pow(value._bits, exponent._bits, NumericsHelpers.Abs(modulus._sign)))));
			return (value._sign < 0 && !exponent.IsEven) ? (-1 * num) : num;
		}
		uint[] array = ((flag && flag2) ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), NumericsHelpers.Abs(exponent._sign), modulus._bits) : (flag ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), exponent._bits, modulus._bits) : (flag2 ? BigIntegerCalculator.Pow(value._bits, NumericsHelpers.Abs(exponent._sign), modulus._bits) : BigIntegerCalculator.Pow(value._bits, exponent._bits, modulus._bits))));
		return new BigInteger(array, array, value._sign < 0 && !exponent.IsEven);
	}

	public static BigInteger Pow(BigInteger value, int exponent)
	{
		if (exponent < 0)
		{
			throw new ArgumentOutOfRangeException("exponent", System.SR.ArgumentOutOfRange_MustBeNonNeg);
		}
		switch (exponent)
		{
		case 0:
			return s_bnOneInt;
		case 1:
			return value;
		default:
		{
			bool flag = value._bits == null;
			if (flag)
			{
				if (value._sign == 1)
				{
					return value;
				}
				if (value._sign == -1)
				{
					if ((exponent & 1) == 0)
					{
						return s_bnOneInt;
					}
					return value;
				}
				if (value._sign == 0)
				{
					return value;
				}
			}
			uint[] array = (flag ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), NumericsHelpers.Abs(exponent)) : BigIntegerCalculator.Pow(value._bits, NumericsHelpers.Abs(exponent)));
			return new BigInteger(array, array, value._sign < 0 && (exponent & 1) != 0);
		}
		}
	}

	public override int GetHashCode()
	{
		if (_bits == null)
		{
			return _sign;
		}
		int num = _sign;
		int num2 = _bits.Length;
		while (--num2 >= 0)
		{
			num = NumericsHelpers.CombineHash(num, (int)_bits[num2]);
		}
		return num;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is BigInteger))
		{
			return false;
		}
		return Equals((BigInteger)obj);
	}

	public bool Equals(long other)
	{
		if (_bits == null)
		{
			return _sign == other;
		}
		int num;
		if ((_sign ^ other) < 0 || (num = _bits.Length) > 2)
		{
			return false;
		}
		ulong num2 = (ulong)((other < 0) ? (-other) : other);
		if (num == 1)
		{
			return _bits[0] == num2;
		}
		return NumericsHelpers.MakeUlong(_bits[1], _bits[0]) == num2;
	}

	[CLSCompliant(false)]
	public bool Equals(ulong other)
	{
		if (_sign < 0)
		{
			return false;
		}
		if (_bits == null)
		{
			return (ulong)_sign == other;
		}
		int num = _bits.Length;
		if (num > 2)
		{
			return false;
		}
		if (num == 1)
		{
			return _bits[0] == other;
		}
		return NumericsHelpers.MakeUlong(_bits[1], _bits[0]) == other;
	}

	public bool Equals(BigInteger other)
	{
		if (_sign != other._sign)
		{
			return false;
		}
		if (_bits == other._bits)
		{
			return true;
		}
		if (_bits == null || other._bits == null)
		{
			return false;
		}
		int num = _bits.Length;
		if (num != other._bits.Length)
		{
			return false;
		}
		int diffLength = GetDiffLength(_bits, other._bits, num);
		return diffLength == 0;
	}

	public int CompareTo(long other)
	{
		if (_bits == null)
		{
			return ((long)_sign).CompareTo(other);
		}
		int num;
		if ((_sign ^ other) < 0 || (num = _bits.Length) > 2)
		{
			return _sign;
		}
		ulong value = (ulong)((other < 0) ? (-other) : other);
		ulong num2 = ((num == 2) ? NumericsHelpers.MakeUlong(_bits[1], _bits[0]) : _bits[0]);
		return _sign * num2.CompareTo(value);
	}

	[CLSCompliant(false)]
	public int CompareTo(ulong other)
	{
		if (_sign < 0)
		{
			return -1;
		}
		if (_bits == null)
		{
			return ((ulong)_sign).CompareTo(other);
		}
		int num = _bits.Length;
		if (num > 2)
		{
			return 1;
		}
		return ((num == 2) ? NumericsHelpers.MakeUlong(_bits[1], _bits[0]) : _bits[0]).CompareTo(other);
	}

	public int CompareTo(BigInteger other)
	{
		if ((_sign ^ other._sign) < 0)
		{
			if (_sign >= 0)
			{
				return 1;
			}
			return -1;
		}
		if (_bits == null)
		{
			if (other._bits == null)
			{
				if (_sign >= other._sign)
				{
					if (_sign <= other._sign)
					{
						return 0;
					}
					return 1;
				}
				return -1;
			}
			return -other._sign;
		}
		int num;
		int num2;
		if (other._bits == null || (num = _bits.Length) > (num2 = other._bits.Length))
		{
			return _sign;
		}
		if (num < num2)
		{
			return -_sign;
		}
		int diffLength = GetDiffLength(_bits, other._bits, num);
		if (diffLength == 0)
		{
			return 0;
		}
		if (_bits[diffLength - 1] >= other._bits[diffLength - 1])
		{
			return _sign;
		}
		return -_sign;
	}

	public int CompareTo(object? obj)
	{
		if (obj == null)
		{
			return 1;
		}
		if (!(obj is BigInteger))
		{
			throw new ArgumentException(System.SR.Argument_MustBeBigInt, "obj");
		}
		return CompareTo((BigInteger)obj);
	}

	public byte[] ToByteArray()
	{
		return ToByteArray(isUnsigned: false, isBigEndian: false);
	}

	public byte[] ToByteArray(bool isUnsigned = false, bool isBigEndian = false)
	{
		int bytesWritten = 0;
		return TryGetBytes(GetBytesMode.AllocateArray, default(Span<byte>), isUnsigned, isBigEndian, ref bytesWritten);
	}

	public bool TryWriteBytes(Span<byte> destination, out int bytesWritten, bool isUnsigned = false, bool isBigEndian = false)
	{
		bytesWritten = 0;
		if (TryGetBytes(GetBytesMode.Span, destination, isUnsigned, isBigEndian, ref bytesWritten) == null)
		{
			bytesWritten = 0;
			return false;
		}
		return true;
	}

	internal bool TryWriteOrCountBytes(Span<byte> destination, out int bytesWritten, bool isUnsigned = false, bool isBigEndian = false)
	{
		bytesWritten = 0;
		return TryGetBytes(GetBytesMode.Span, destination, isUnsigned, isBigEndian, ref bytesWritten) != null;
	}

	public int GetByteCount(bool isUnsigned = false)
	{
		int bytesWritten = 0;
		TryGetBytes(GetBytesMode.Count, default(Span<byte>), isUnsigned, isBigEndian: false, ref bytesWritten);
		return bytesWritten;
	}

	private byte[] TryGetBytes(GetBytesMode mode, Span<byte> destination, bool isUnsigned, bool isBigEndian, ref int bytesWritten)
	{
		int sign = _sign;
		if (sign == 0)
		{
			switch (mode)
			{
			case GetBytesMode.AllocateArray:
				return new byte[1];
			case GetBytesMode.Count:
				bytesWritten = 1;
				return null;
			default:
				bytesWritten = 1;
				if (destination.Length != 0)
				{
					destination[0] = 0;
					return s_success;
				}
				return null;
			}
		}
		if (isUnsigned && sign < 0)
		{
			throw new OverflowException(System.SR.Overflow_Negative_Unsigned);
		}
		int i = 0;
		uint[] bits = _bits;
		byte b;
		uint num;
		if (bits == null)
		{
			b = (byte)((sign < 0) ? 255u : 0u);
			num = (uint)sign;
		}
		else if (sign == -1)
		{
			b = byte.MaxValue;
			for (; bits[i] == 0; i++)
			{
			}
			num = ~bits[^1];
			if (bits.Length - 1 == i)
			{
				num++;
			}
		}
		else
		{
			b = 0;
			num = bits[^1];
		}
		byte b2;
		int num2;
		if ((b2 = (byte)(num >> 24)) != b)
		{
			num2 = 3;
		}
		else if ((b2 = (byte)(num >> 16)) != b)
		{
			num2 = 2;
		}
		else if ((b2 = (byte)(num >> 8)) != b)
		{
			num2 = 1;
		}
		else
		{
			b2 = (byte)num;
			num2 = 0;
		}
		bool flag = (b2 & 0x80) != (b & 0x80) && !isUnsigned;
		int num3 = num2 + 1 + (flag ? 1 : 0);
		if (bits != null)
		{
			num3 = checked(4 * (bits.Length - 1) + num3);
		}
		byte[] result;
		switch (mode)
		{
		case GetBytesMode.AllocateArray:
			destination = (result = new byte[num3]);
			break;
		case GetBytesMode.Count:
			bytesWritten = num3;
			return null;
		default:
			bytesWritten = num3;
			if (destination.Length < num3)
			{
				return null;
			}
			result = s_success;
			break;
		}
		int num4 = (isBigEndian ? (num3 - 1) : 0);
		int num5 = ((!isBigEndian) ? 1 : (-1));
		if (bits != null)
		{
			for (int j = 0; j < bits.Length - 1; j++)
			{
				uint num6 = bits[j];
				if (sign == -1)
				{
					num6 = ~num6;
					if (j <= i)
					{
						num6++;
					}
				}
				destination[num4] = (byte)num6;
				num4 += num5;
				destination[num4] = (byte)(num6 >> 8);
				num4 += num5;
				destination[num4] = (byte)(num6 >> 16);
				num4 += num5;
				destination[num4] = (byte)(num6 >> 24);
				num4 += num5;
			}
		}
		destination[num4] = (byte)num;
		if (num2 != 0)
		{
			num4 += num5;
			destination[num4] = (byte)(num >> 8);
			if (num2 != 1)
			{
				num4 += num5;
				destination[num4] = (byte)(num >> 16);
				if (num2 != 2)
				{
					num4 += num5;
					destination[num4] = (byte)(num >> 24);
				}
			}
		}
		if (flag)
		{
			num4 += num5;
			destination[num4] = b;
		}
		return result;
	}

	private ReadOnlySpan<uint> ToUInt32Span(Span<uint> scratch)
	{
		if (_bits == null && _sign == 0)
		{
			scratch[0] = 0u;
			return scratch.Slice(0, 1);
		}
		Span<uint> span = scratch;
		bool flag = true;
		uint num;
		if (_bits == null)
		{
			span[0] = (uint)_sign;
			span = span.Slice(0, 1);
			num = ((_sign < 0) ? uint.MaxValue : 0u);
		}
		else if (_sign == -1)
		{
			if (span.Length >= _bits.Length)
			{
				_bits.AsSpan().CopyTo(span);
				span = span.Slice(0, _bits.Length);
			}
			else
			{
				span = (uint[])_bits.Clone();
			}
			NumericsHelpers.DangerousMakeTwosComplement(span);
			num = uint.MaxValue;
		}
		else
		{
			span = _bits;
			num = 0u;
			flag = false;
		}
		int num2 = span.Length - 1;
		while (num2 > 0 && span[num2] == num)
		{
			num2--;
		}
		bool flag2 = (span[num2] & 0x80000000u) != (num & 0x80000000u);
		int num3 = num2 + 1 + (flag2 ? 1 : 0);
		bool flag3 = true;
		if (num3 <= scratch.Length)
		{
			scratch = scratch.Slice(0, num3);
			flag3 = !flag;
		}
		else
		{
			scratch = new uint[num3];
		}
		if (flag3)
		{
			span.Slice(0, num2 + 1).CopyTo(scratch);
		}
		if (flag2)
		{
			scratch[^1] = num;
		}
		return scratch;
	}

	public override string ToString()
	{
		return BigNumber.FormatBigInteger(this, null, NumberFormatInfo.CurrentInfo);
	}

	public string ToString(IFormatProvider? provider)
	{
		return BigNumber.FormatBigInteger(this, null, NumberFormatInfo.GetInstance(provider));
	}

	public string ToString(string? format)
	{
		return BigNumber.FormatBigInteger(this, format, NumberFormatInfo.CurrentInfo);
	}

	public string ToString(string? format, IFormatProvider? provider)
	{
		return BigNumber.FormatBigInteger(this, format, NumberFormatInfo.GetInstance(provider));
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return BigNumber.TryFormatBigInteger(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
	}

	private static BigInteger Add(uint[] leftBits, int leftSign, uint[] rightBits, int rightSign)
	{
		bool flag = leftBits == null;
		bool flag2 = rightBits == null;
		if (flag && flag2)
		{
			return (long)leftSign + (long)rightSign;
		}
		if (flag)
		{
			uint[] array = BigIntegerCalculator.Add(rightBits, NumericsHelpers.Abs(leftSign));
			return new BigInteger(array, array, leftSign < 0);
		}
		if (flag2)
		{
			uint[] array2 = BigIntegerCalculator.Add(leftBits, NumericsHelpers.Abs(rightSign));
			return new BigInteger(array2, array2, leftSign < 0);
		}
		if (leftBits.Length < rightBits.Length)
		{
			uint[] array3 = BigIntegerCalculator.Add(rightBits, leftBits);
			return new BigInteger(array3, array3, leftSign < 0);
		}
		uint[] array4 = BigIntegerCalculator.Add(leftBits, rightBits);
		return new BigInteger(array4, array4, leftSign < 0);
	}

	public static BigInteger operator -(BigInteger left, BigInteger right)
	{
		if (left._sign < 0 != right._sign < 0)
		{
			return Add(left._bits, left._sign, right._bits, -1 * right._sign);
		}
		return Subtract(left._bits, left._sign, right._bits, right._sign);
	}

	private static BigInteger Subtract(uint[] leftBits, int leftSign, uint[] rightBits, int rightSign)
	{
		bool flag = leftBits == null;
		bool flag2 = rightBits == null;
		if (flag && flag2)
		{
			return (long)leftSign - (long)rightSign;
		}
		if (flag)
		{
			uint[] array = BigIntegerCalculator.Subtract(rightBits, NumericsHelpers.Abs(leftSign));
			return new BigInteger(array, array, leftSign >= 0);
		}
		if (flag2)
		{
			uint[] array2 = BigIntegerCalculator.Subtract(leftBits, NumericsHelpers.Abs(rightSign));
			return new BigInteger(array2, array2, leftSign < 0);
		}
		if (BigIntegerCalculator.Compare(leftBits, rightBits) < 0)
		{
			uint[] array3 = BigIntegerCalculator.Subtract(rightBits, leftBits);
			return new BigInteger(array3, array3, leftSign >= 0);
		}
		uint[] array4 = BigIntegerCalculator.Subtract(leftBits, rightBits);
		return new BigInteger(array4, array4, leftSign < 0);
	}

	public static implicit operator BigInteger(byte value)
	{
		return new BigInteger(value);
	}

	[CLSCompliant(false)]
	public static implicit operator BigInteger(sbyte value)
	{
		return new BigInteger(value);
	}

	public static implicit operator BigInteger(short value)
	{
		return new BigInteger(value);
	}

	[CLSCompliant(false)]
	public static implicit operator BigInteger(ushort value)
	{
		return new BigInteger(value);
	}

	public static implicit operator BigInteger(int value)
	{
		return new BigInteger(value);
	}

	[CLSCompliant(false)]
	public static implicit operator BigInteger(uint value)
	{
		return new BigInteger(value);
	}

	public static implicit operator BigInteger(long value)
	{
		return new BigInteger(value);
	}

	[CLSCompliant(false)]
	public static implicit operator BigInteger(ulong value)
	{
		return new BigInteger(value);
	}

	public static explicit operator BigInteger(float value)
	{
		return new BigInteger(value);
	}

	public static explicit operator BigInteger(double value)
	{
		return new BigInteger(value);
	}

	public static explicit operator BigInteger(decimal value)
	{
		return new BigInteger(value);
	}

	public static explicit operator byte(BigInteger value)
	{
		return checked((byte)(int)value);
	}

	[CLSCompliant(false)]
	public static explicit operator sbyte(BigInteger value)
	{
		return checked((sbyte)(int)value);
	}

	public static explicit operator short(BigInteger value)
	{
		return checked((short)(int)value);
	}

	[CLSCompliant(false)]
	public static explicit operator ushort(BigInteger value)
	{
		return checked((ushort)(int)value);
	}

	public static explicit operator int(BigInteger value)
	{
		if (value._bits == null)
		{
			return value._sign;
		}
		if (value._bits.Length > 1)
		{
			throw new OverflowException(System.SR.Overflow_Int32);
		}
		if (value._sign > 0)
		{
			return checked((int)value._bits[0]);
		}
		if (value._bits[0] > 2147483648u)
		{
			throw new OverflowException(System.SR.Overflow_Int32);
		}
		return (int)(0 - value._bits[0]);
	}

	[CLSCompliant(false)]
	public static explicit operator uint(BigInteger value)
	{
		if (value._bits == null)
		{
			return checked((uint)value._sign);
		}
		if (value._bits.Length > 1 || value._sign < 0)
		{
			throw new OverflowException(System.SR.Overflow_UInt32);
		}
		return value._bits[0];
	}

	public static explicit operator long(BigInteger value)
	{
		if (value._bits == null)
		{
			return value._sign;
		}
		int num = value._bits.Length;
		if (num > 2)
		{
			throw new OverflowException(System.SR.Overflow_Int64);
		}
		ulong num2 = ((num <= 1) ? value._bits[0] : NumericsHelpers.MakeUlong(value._bits[1], value._bits[0]));
		long num3 = (long)((value._sign > 0) ? num2 : (0L - num2));
		if ((num3 > 0 && value._sign > 0) || (num3 < 0 && value._sign < 0))
		{
			return num3;
		}
		throw new OverflowException(System.SR.Overflow_Int64);
	}

	[CLSCompliant(false)]
	public static explicit operator ulong(BigInteger value)
	{
		if (value._bits == null)
		{
			return checked((ulong)value._sign);
		}
		int num = value._bits.Length;
		if (num > 2 || value._sign < 0)
		{
			throw new OverflowException(System.SR.Overflow_UInt64);
		}
		if (num > 1)
		{
			return NumericsHelpers.MakeUlong(value._bits[1], value._bits[0]);
		}
		return value._bits[0];
	}

	public static explicit operator float(BigInteger value)
	{
		return (float)(double)value;
	}

	public static explicit operator double(BigInteger value)
	{
		int sign = value._sign;
		uint[] bits = value._bits;
		if (bits == null)
		{
			return sign;
		}
		int num = bits.Length;
		if (num > 32)
		{
			if (sign == 1)
			{
				return double.PositiveInfinity;
			}
			return double.NegativeInfinity;
		}
		ulong num2 = bits[num - 1];
		ulong num3 = ((num > 1) ? bits[num - 2] : 0u);
		ulong num4 = ((num > 2) ? bits[num - 3] : 0u);
		int num5 = NumericsHelpers.CbitHighZero((uint)num2);
		int exp = (num - 2) * 32 - num5;
		ulong man = (num2 << 32 + num5) | (num3 << num5) | (num4 >> 32 - num5);
		return NumericsHelpers.GetDoubleFromParts(sign, exp, man);
	}

	public static explicit operator decimal(BigInteger value)
	{
		if (value._bits == null)
		{
			return value._sign;
		}
		int num = value._bits.Length;
		if (num > 3)
		{
			throw new OverflowException(System.SR.Overflow_Decimal);
		}
		int lo = 0;
		int mid = 0;
		int hi = 0;
		if (num > 2)
		{
			hi = (int)value._bits[2];
		}
		if (num > 1)
		{
			mid = (int)value._bits[1];
		}
		if (num > 0)
		{
			lo = (int)value._bits[0];
		}
		return new decimal(lo, mid, hi, value._sign < 0, 0);
	}

	public static BigInteger operator &(BigInteger left, BigInteger right)
	{
		if (left.IsZero || right.IsZero)
		{
			return Zero;
		}
		if (left._bits == null && right._bits == null)
		{
			return left._sign & right._sign;
		}
		Span<uint> scratch = stackalloc uint[32];
		ReadOnlySpan<uint> readOnlySpan = left.ToUInt32Span(scratch);
		scratch = stackalloc uint[32];
		ReadOnlySpan<uint> readOnlySpan2 = right.ToUInt32Span(scratch);
		uint[] array = new uint[Math.Max(readOnlySpan.Length, readOnlySpan2.Length)];
		uint num = ((left._sign < 0) ? uint.MaxValue : 0u);
		uint num2 = ((right._sign < 0) ? uint.MaxValue : 0u);
		for (int i = 0; i < array.Length; i++)
		{
			uint num3 = ((i < readOnlySpan.Length) ? readOnlySpan[i] : num);
			uint num4 = ((i < readOnlySpan2.Length) ? readOnlySpan2[i] : num2);
			array[i] = num3 & num4;
		}
		return new BigInteger(array);
	}

	public static BigInteger operator |(BigInteger left, BigInteger right)
	{
		if (left.IsZero)
		{
			return right;
		}
		if (right.IsZero)
		{
			return left;
		}
		if (left._bits == null && right._bits == null)
		{
			return left._sign | right._sign;
		}
		Span<uint> scratch = stackalloc uint[32];
		ReadOnlySpan<uint> readOnlySpan = left.ToUInt32Span(scratch);
		scratch = stackalloc uint[32];
		ReadOnlySpan<uint> readOnlySpan2 = right.ToUInt32Span(scratch);
		uint[] array = new uint[Math.Max(readOnlySpan.Length, readOnlySpan2.Length)];
		uint num = ((left._sign < 0) ? uint.MaxValue : 0u);
		uint num2 = ((right._sign < 0) ? uint.MaxValue : 0u);
		for (int i = 0; i < array.Length; i++)
		{
			uint num3 = ((i < readOnlySpan.Length) ? readOnlySpan[i] : num);
			uint num4 = ((i < readOnlySpan2.Length) ? readOnlySpan2[i] : num2);
			array[i] = num3 | num4;
		}
		return new BigInteger(array);
	}

	public static BigInteger operator ^(BigInteger left, BigInteger right)
	{
		if (left._bits == null && right._bits == null)
		{
			return left._sign ^ right._sign;
		}
		Span<uint> scratch = stackalloc uint[32];
		ReadOnlySpan<uint> readOnlySpan = left.ToUInt32Span(scratch);
		scratch = stackalloc uint[32];
		ReadOnlySpan<uint> readOnlySpan2 = right.ToUInt32Span(scratch);
		uint[] array = new uint[Math.Max(readOnlySpan.Length, readOnlySpan2.Length)];
		uint num = ((left._sign < 0) ? uint.MaxValue : 0u);
		uint num2 = ((right._sign < 0) ? uint.MaxValue : 0u);
		for (int i = 0; i < array.Length; i++)
		{
			uint num3 = ((i < readOnlySpan.Length) ? readOnlySpan[i] : num);
			uint num4 = ((i < readOnlySpan2.Length) ? readOnlySpan2[i] : num2);
			array[i] = num3 ^ num4;
		}
		return new BigInteger(array);
	}

	public static BigInteger operator <<(BigInteger value, int shift)
	{
		if (shift == 0)
		{
			return value;
		}
		if (shift == int.MinValue)
		{
			return value >> int.MaxValue >> 1;
		}
		if (shift < 0)
		{
			return value >> -shift;
		}
		(int Quotient, int Remainder) tuple = Math.DivRem(shift, 32);
		int item = tuple.Quotient;
		int item2 = tuple.Remainder;
		Span<uint> span = stackalloc uint[1];
		Span<uint> xd = span;
		bool partsForBitManipulation = GetPartsForBitManipulation(ref value, ref xd);
		int num = xd.Length + item + 1;
		uint[] valueArray = null;
		Span<uint> span2 = default(Span<uint>);
		if (num <= 64)
		{
			span = stackalloc uint[64];
			span2 = span.Slice(0, num);
			span = span2.Slice(0, item);
			span.Clear();
		}
		else
		{
			span2 = (valueArray = new uint[num]);
		}
		uint num2 = 0u;
		if (item2 == 0)
		{
			for (int i = 0; i < xd.Length; i++)
			{
				span2[i + item] = xd[i];
			}
		}
		else
		{
			int num3 = 32 - item2;
			for (int j = 0; j < xd.Length; j++)
			{
				uint num4 = xd[j];
				span2[j + item] = (num4 << item2) | num2;
				num2 = num4 >> num3;
			}
		}
		span2[^1] = num2;
		return new BigInteger(span2, valueArray, partsForBitManipulation);
	}

	public static BigInteger operator >>(BigInteger value, int shift)
	{
		if (shift == 0)
		{
			return value;
		}
		if (shift == int.MinValue)
		{
			return value << int.MaxValue << 1;
		}
		if (shift < 0)
		{
			return value << -shift;
		}
		(int Quotient, int Remainder) tuple = Math.DivRem(shift, 32);
		int item = tuple.Quotient;
		int item2 = tuple.Remainder;
		Span<uint> span = stackalloc uint[1];
		Span<uint> span2 = span;
		Span<uint> xd = span2;
		bool partsForBitManipulation = GetPartsForBitManipulation(ref value, ref xd);
		bool flag = false;
		if (partsForBitManipulation)
		{
			if (shift >= 32 * xd.Length)
			{
				return MinusOne;
			}
			if (xd != span2)
			{
				if (xd.Length <= 64)
				{
					span = stackalloc uint[64];
					span2 = span.Slice(0, xd.Length);
					xd.CopyTo(span2);
					xd = span2;
				}
				else
				{
					xd = xd.ToArray();
				}
			}
			NumericsHelpers.DangerousMakeTwosComplement(xd);
			flag = item2 == 0 && xd[^1] == 0;
		}
		int num = xd.Length - item + (flag ? 1 : 0);
		uint[] valueArray = null;
		Span<uint> span3 = default(Span<uint>);
		if (num > 0)
		{
			Span<uint> span4;
			if (num <= 64)
			{
				span = stackalloc uint[64];
				span4 = span.Slice(0, num);
			}
			else
			{
				span4 = (valueArray = new uint[num]);
			}
			span3 = span4;
		}
		if (item2 == 0)
		{
			for (int num2 = xd.Length - 1; num2 >= item; num2--)
			{
				span3[num2 - item] = xd[num2];
			}
		}
		else
		{
			int num3 = 32 - item2;
			uint num4 = 0u;
			for (int num5 = xd.Length - 1; num5 >= item; num5--)
			{
				uint num6 = xd[num5];
				if (partsForBitManipulation && num5 == xd.Length - 1)
				{
					span3[num5 - item] = (num6 >> item2) | (uint)(-1 << num3);
				}
				else
				{
					span3[num5 - item] = (num6 >> item2) | num4;
				}
				num4 = num6 << num3;
			}
		}
		if (partsForBitManipulation)
		{
			if (flag)
			{
				span3[^1] = uint.MaxValue;
			}
			NumericsHelpers.DangerousMakeTwosComplement(span3);
		}
		return new BigInteger(span3, valueArray, partsForBitManipulation);
	}

	public static BigInteger operator ~(BigInteger value)
	{
		return -(value + One);
	}

	public static BigInteger operator -(BigInteger value)
	{
		return new BigInteger(-value._sign, value._bits);
	}

	public static BigInteger operator +(BigInteger value)
	{
		return value;
	}

	public static BigInteger operator ++(BigInteger value)
	{
		return value + One;
	}

	public static BigInteger operator --(BigInteger value)
	{
		return value - One;
	}

	public static BigInteger operator +(BigInteger left, BigInteger right)
	{
		if (left._sign < 0 != right._sign < 0)
		{
			return Subtract(left._bits, left._sign, right._bits, -1 * right._sign);
		}
		return Add(left._bits, left._sign, right._bits, right._sign);
	}

	public static BigInteger operator *(BigInteger left, BigInteger right)
	{
		bool flag = left._bits == null;
		bool flag2 = right._bits == null;
		if (flag && flag2)
		{
			return (long)left._sign * (long)right._sign;
		}
		if (flag)
		{
			uint[] array = BigIntegerCalculator.Multiply(right._bits, NumericsHelpers.Abs(left._sign));
			return new BigInteger(array, array, (left._sign < 0) ^ (right._sign < 0));
		}
		if (flag2)
		{
			uint[] array2 = BigIntegerCalculator.Multiply(left._bits, NumericsHelpers.Abs(right._sign));
			return new BigInteger(array2, array2, (left._sign < 0) ^ (right._sign < 0));
		}
		if (left._bits == right._bits)
		{
			uint[] array3 = BigIntegerCalculator.Square(left._bits);
			return new BigInteger(array3, array3, (left._sign < 0) ^ (right._sign < 0));
		}
		if (left._bits.Length < right._bits.Length)
		{
			uint[] array4 = BigIntegerCalculator.Multiply(right._bits, left._bits);
			return new BigInteger(array4, array4, (left._sign < 0) ^ (right._sign < 0));
		}
		uint[] array5 = BigIntegerCalculator.Multiply(left._bits, right._bits);
		return new BigInteger(array5, array5, (left._sign < 0) ^ (right._sign < 0));
	}

	public static BigInteger operator /(BigInteger dividend, BigInteger divisor)
	{
		bool flag = dividend._bits == null;
		bool flag2 = divisor._bits == null;
		if (flag && flag2)
		{
			return dividend._sign / divisor._sign;
		}
		if (flag)
		{
			return s_bnZeroInt;
		}
		if (flag2)
		{
			uint[] array = BigIntegerCalculator.Divide(dividend._bits, NumericsHelpers.Abs(divisor._sign));
			return new BigInteger(array, array, (dividend._sign < 0) ^ (divisor._sign < 0));
		}
		if (dividend._bits.Length < divisor._bits.Length)
		{
			return s_bnZeroInt;
		}
		uint[] array2 = BigIntegerCalculator.Divide(dividend._bits, divisor._bits);
		return new BigInteger(array2, array2, (dividend._sign < 0) ^ (divisor._sign < 0));
	}

	public static BigInteger operator %(BigInteger dividend, BigInteger divisor)
	{
		bool flag = dividend._bits == null;
		bool flag2 = divisor._bits == null;
		if (flag && flag2)
		{
			return dividend._sign % divisor._sign;
		}
		if (flag)
		{
			return dividend;
		}
		if (flag2)
		{
			uint num = BigIntegerCalculator.Remainder(dividend._bits, NumericsHelpers.Abs(divisor._sign));
			return (dividend._sign < 0) ? (-1 * num) : num;
		}
		if (dividend._bits.Length < divisor._bits.Length)
		{
			return dividend;
		}
		uint[] array = BigIntegerCalculator.Remainder(dividend._bits, divisor._bits);
		return new BigInteger(array, array, dividend._sign < 0);
	}

	public static bool operator <(BigInteger left, BigInteger right)
	{
		return left.CompareTo(right) < 0;
	}

	public static bool operator <=(BigInteger left, BigInteger right)
	{
		return left.CompareTo(right) <= 0;
	}

	public static bool operator >(BigInteger left, BigInteger right)
	{
		return left.CompareTo(right) > 0;
	}

	public static bool operator >=(BigInteger left, BigInteger right)
	{
		return left.CompareTo(right) >= 0;
	}

	public static bool operator ==(BigInteger left, BigInteger right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(BigInteger left, BigInteger right)
	{
		return !left.Equals(right);
	}

	public static bool operator <(BigInteger left, long right)
	{
		return left.CompareTo(right) < 0;
	}

	public static bool operator <=(BigInteger left, long right)
	{
		return left.CompareTo(right) <= 0;
	}

	public static bool operator >(BigInteger left, long right)
	{
		return left.CompareTo(right) > 0;
	}

	public static bool operator >=(BigInteger left, long right)
	{
		return left.CompareTo(right) >= 0;
	}

	public static bool operator ==(BigInteger left, long right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(BigInteger left, long right)
	{
		return !left.Equals(right);
	}

	public static bool operator <(long left, BigInteger right)
	{
		return right.CompareTo(left) > 0;
	}

	public static bool operator <=(long left, BigInteger right)
	{
		return right.CompareTo(left) >= 0;
	}

	public static bool operator >(long left, BigInteger right)
	{
		return right.CompareTo(left) < 0;
	}

	public static bool operator >=(long left, BigInteger right)
	{
		return right.CompareTo(left) <= 0;
	}

	public static bool operator ==(long left, BigInteger right)
	{
		return right.Equals(left);
	}

	public static bool operator !=(long left, BigInteger right)
	{
		return !right.Equals(left);
	}

	[CLSCompliant(false)]
	public static bool operator <(BigInteger left, ulong right)
	{
		return left.CompareTo(right) < 0;
	}

	[CLSCompliant(false)]
	public static bool operator <=(BigInteger left, ulong right)
	{
		return left.CompareTo(right) <= 0;
	}

	[CLSCompliant(false)]
	public static bool operator >(BigInteger left, ulong right)
	{
		return left.CompareTo(right) > 0;
	}

	[CLSCompliant(false)]
	public static bool operator >=(BigInteger left, ulong right)
	{
		return left.CompareTo(right) >= 0;
	}

	[CLSCompliant(false)]
	public static bool operator ==(BigInteger left, ulong right)
	{
		return left.Equals(right);
	}

	[CLSCompliant(false)]
	public static bool operator !=(BigInteger left, ulong right)
	{
		return !left.Equals(right);
	}

	[CLSCompliant(false)]
	public static bool operator <(ulong left, BigInteger right)
	{
		return right.CompareTo(left) > 0;
	}

	[CLSCompliant(false)]
	public static bool operator <=(ulong left, BigInteger right)
	{
		return right.CompareTo(left) >= 0;
	}

	[CLSCompliant(false)]
	public static bool operator >(ulong left, BigInteger right)
	{
		return right.CompareTo(left) < 0;
	}

	[CLSCompliant(false)]
	public static bool operator >=(ulong left, BigInteger right)
	{
		return right.CompareTo(left) <= 0;
	}

	[CLSCompliant(false)]
	public static bool operator ==(ulong left, BigInteger right)
	{
		return right.Equals(left);
	}

	[CLSCompliant(false)]
	public static bool operator !=(ulong left, BigInteger right)
	{
		return !right.Equals(left);
	}

	public long GetBitLength()
	{
		int sign = _sign;
		uint[] bits = _bits;
		int num;
		uint num2;
		if (bits == null)
		{
			num = 1;
			num2 = (uint)((sign < 0) ? (-sign) : sign);
		}
		else
		{
			num = bits.Length;
			num2 = bits[num - 1];
		}
		long num3 = (long)num * 32L - BitOperations.LeadingZeroCount(num2);
		if (sign >= 0)
		{
			return num3;
		}
		if ((num2 & (num2 - 1)) != 0)
		{
			return num3;
		}
		for (int num4 = num - 2; num4 >= 0; num4--)
		{
			if (bits[num4] != 0)
			{
				return num3;
			}
		}
		return num3 - 1;
	}

	private static bool GetPartsForBitManipulation(ref BigInteger x, ref Span<uint> xd)
	{
		if (x._bits == null)
		{
			xd[0] = (uint)((x._sign < 0) ? (-x._sign) : x._sign);
		}
		else
		{
			xd = x._bits;
		}
		return x._sign < 0;
	}

	internal static int GetDiffLength(uint[] rgu1, uint[] rgu2, int cu)
	{
		int num = cu;
		while (--num >= 0)
		{
			if (rgu1[num] != rgu2[num])
			{
				return num + 1;
			}
		}
		return 0;
	}
}
