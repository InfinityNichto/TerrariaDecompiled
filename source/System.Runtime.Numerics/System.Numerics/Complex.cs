using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Numerics;

[Serializable]
[TypeForwardedFrom("System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Complex : IEquatable<Complex>, IFormattable
{
	public static readonly Complex Zero = new Complex(0.0, 0.0);

	public static readonly Complex One = new Complex(1.0, 0.0);

	public static readonly Complex ImaginaryOne = new Complex(0.0, 1.0);

	public static readonly Complex NaN = new Complex(double.NaN, double.NaN);

	public static readonly Complex Infinity = new Complex(double.PositiveInfinity, double.PositiveInfinity);

	private static readonly double s_sqrtRescaleThreshold = double.MaxValue / (Math.Sqrt(2.0) + 1.0);

	private static readonly double s_asinOverflowThreshold = Math.Sqrt(double.MaxValue) / 2.0;

	private static readonly double s_log2 = Math.Log(2.0);

	private readonly double m_real;

	private readonly double m_imaginary;

	public double Real => m_real;

	public double Imaginary => m_imaginary;

	public double Magnitude => Abs(this);

	public double Phase => Math.Atan2(m_imaginary, m_real);

	public Complex(double real, double imaginary)
	{
		m_real = real;
		m_imaginary = imaginary;
	}

	public static Complex FromPolarCoordinates(double magnitude, double phase)
	{
		return new Complex(magnitude * Math.Cos(phase), magnitude * Math.Sin(phase));
	}

	public static Complex Negate(Complex value)
	{
		return -value;
	}

	public static Complex Add(Complex left, Complex right)
	{
		return left + right;
	}

	public static Complex Add(Complex left, double right)
	{
		return left + right;
	}

	public static Complex Add(double left, Complex right)
	{
		return left + right;
	}

	public static Complex Subtract(Complex left, Complex right)
	{
		return left - right;
	}

	public static Complex Subtract(Complex left, double right)
	{
		return left - right;
	}

	public static Complex Subtract(double left, Complex right)
	{
		return left - right;
	}

	public static Complex Multiply(Complex left, Complex right)
	{
		return left * right;
	}

	public static Complex Multiply(Complex left, double right)
	{
		return left * right;
	}

	public static Complex Multiply(double left, Complex right)
	{
		return left * right;
	}

	public static Complex Divide(Complex dividend, Complex divisor)
	{
		return dividend / divisor;
	}

	public static Complex Divide(Complex dividend, double divisor)
	{
		return dividend / divisor;
	}

	public static Complex Divide(double dividend, Complex divisor)
	{
		return dividend / divisor;
	}

	public static Complex operator -(Complex value)
	{
		return new Complex(0.0 - value.m_real, 0.0 - value.m_imaginary);
	}

	public static Complex operator +(Complex left, Complex right)
	{
		return new Complex(left.m_real + right.m_real, left.m_imaginary + right.m_imaginary);
	}

	public static Complex operator +(Complex left, double right)
	{
		return new Complex(left.m_real + right, left.m_imaginary);
	}

	public static Complex operator +(double left, Complex right)
	{
		return new Complex(left + right.m_real, right.m_imaginary);
	}

	public static Complex operator -(Complex left, Complex right)
	{
		return new Complex(left.m_real - right.m_real, left.m_imaginary - right.m_imaginary);
	}

	public static Complex operator -(Complex left, double right)
	{
		return new Complex(left.m_real - right, left.m_imaginary);
	}

	public static Complex operator -(double left, Complex right)
	{
		return new Complex(left - right.m_real, 0.0 - right.m_imaginary);
	}

	public static Complex operator *(Complex left, Complex right)
	{
		double real = left.m_real * right.m_real - left.m_imaginary * right.m_imaginary;
		double imaginary = left.m_imaginary * right.m_real + left.m_real * right.m_imaginary;
		return new Complex(real, imaginary);
	}

	public static Complex operator *(Complex left, double right)
	{
		if (!double.IsFinite(left.m_real))
		{
			if (!double.IsFinite(left.m_imaginary))
			{
				return new Complex(double.NaN, double.NaN);
			}
			return new Complex(left.m_real * right, double.NaN);
		}
		if (!double.IsFinite(left.m_imaginary))
		{
			return new Complex(double.NaN, left.m_imaginary * right);
		}
		return new Complex(left.m_real * right, left.m_imaginary * right);
	}

	public static Complex operator *(double left, Complex right)
	{
		if (!double.IsFinite(right.m_real))
		{
			if (!double.IsFinite(right.m_imaginary))
			{
				return new Complex(double.NaN, double.NaN);
			}
			return new Complex(left * right.m_real, double.NaN);
		}
		if (!double.IsFinite(right.m_imaginary))
		{
			return new Complex(double.NaN, left * right.m_imaginary);
		}
		return new Complex(left * right.m_real, left * right.m_imaginary);
	}

	public static Complex operator /(Complex left, Complex right)
	{
		double real = left.m_real;
		double imaginary = left.m_imaginary;
		double real2 = right.m_real;
		double imaginary2 = right.m_imaginary;
		if (Math.Abs(imaginary2) < Math.Abs(real2))
		{
			double num = imaginary2 / real2;
			return new Complex((real + imaginary * num) / (real2 + imaginary2 * num), (imaginary - real * num) / (real2 + imaginary2 * num));
		}
		double num2 = real2 / imaginary2;
		return new Complex((imaginary + real * num2) / (imaginary2 + real2 * num2), (0.0 - real + imaginary * num2) / (imaginary2 + real2 * num2));
	}

	public static Complex operator /(Complex left, double right)
	{
		if (right == 0.0)
		{
			return new Complex(double.NaN, double.NaN);
		}
		if (!double.IsFinite(left.m_real))
		{
			if (!double.IsFinite(left.m_imaginary))
			{
				return new Complex(double.NaN, double.NaN);
			}
			return new Complex(left.m_real / right, double.NaN);
		}
		if (!double.IsFinite(left.m_imaginary))
		{
			return new Complex(double.NaN, left.m_imaginary / right);
		}
		return new Complex(left.m_real / right, left.m_imaginary / right);
	}

	public static Complex operator /(double left, Complex right)
	{
		double real = right.m_real;
		double imaginary = right.m_imaginary;
		if (Math.Abs(imaginary) < Math.Abs(real))
		{
			double num = imaginary / real;
			return new Complex(left / (real + imaginary * num), (0.0 - left) * num / (real + imaginary * num));
		}
		double num2 = real / imaginary;
		return new Complex(left * num2 / (imaginary + real * num2), (0.0 - left) / (imaginary + real * num2));
	}

	public static double Abs(Complex value)
	{
		return Hypot(value.m_real, value.m_imaginary);
	}

	private static double Hypot(double a, double b)
	{
		a = Math.Abs(a);
		b = Math.Abs(b);
		double num;
		double num2;
		if (a < b)
		{
			num = a;
			num2 = b;
		}
		else
		{
			num = b;
			num2 = a;
		}
		if (num == 0.0)
		{
			return num2;
		}
		if (double.IsPositiveInfinity(num2) && !double.IsNaN(num))
		{
			return double.PositiveInfinity;
		}
		double num3 = num / num2;
		return num2 * Math.Sqrt(1.0 + num3 * num3);
	}

	private static double Log1P(double x)
	{
		double num = 1.0 + x;
		if (num == 1.0)
		{
			return x;
		}
		if (x < 0.75)
		{
			return x * Math.Log(num) / (num - 1.0);
		}
		return Math.Log(num);
	}

	public static Complex Conjugate(Complex value)
	{
		return new Complex(value.m_real, 0.0 - value.m_imaginary);
	}

	public static Complex Reciprocal(Complex value)
	{
		if (value.m_real == 0.0 && value.m_imaginary == 0.0)
		{
			return Zero;
		}
		return One / value;
	}

	public static bool operator ==(Complex left, Complex right)
	{
		if (left.m_real == right.m_real)
		{
			return left.m_imaginary == right.m_imaginary;
		}
		return false;
	}

	public static bool operator !=(Complex left, Complex right)
	{
		if (left.m_real == right.m_real)
		{
			return left.m_imaginary != right.m_imaginary;
		}
		return true;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is Complex))
		{
			return false;
		}
		return Equals((Complex)obj);
	}

	public bool Equals(Complex value)
	{
		if (m_real.Equals(value.m_real))
		{
			return m_imaginary.Equals(value.m_imaginary);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = 99999997;
		int num2 = m_real.GetHashCode() % num;
		int hashCode = m_imaginary.GetHashCode();
		return num2 ^ hashCode;
	}

	public override string ToString()
	{
		return $"({m_real}, {m_imaginary})";
	}

	public string ToString(string? format)
	{
		return ToString(format, null);
	}

	public string ToString(IFormatProvider? provider)
	{
		return ToString(null, provider);
	}

	public string ToString(string? format, IFormatProvider? provider)
	{
		return string.Format(provider, "({0}, {1})", m_real.ToString(format, provider), m_imaginary.ToString(format, provider));
	}

	public static Complex Sin(Complex value)
	{
		double num = Math.Exp(value.m_imaginary);
		double num2 = 1.0 / num;
		double num3 = (num - num2) * 0.5;
		double num4 = (num + num2) * 0.5;
		return new Complex(Math.Sin(value.m_real) * num4, Math.Cos(value.m_real) * num3);
	}

	public static Complex Sinh(Complex value)
	{
		Complex complex = Sin(new Complex(0.0 - value.m_imaginary, value.m_real));
		return new Complex(complex.m_imaginary, 0.0 - complex.m_real);
	}

	public static Complex Asin(Complex value)
	{
		Asin_Internal(Math.Abs(value.Real), Math.Abs(value.Imaginary), out var b, out var bPrime, out var v);
		double num = ((!(bPrime < 0.0)) ? Math.Atan(bPrime) : Math.Asin(b));
		if (value.Real < 0.0)
		{
			num = 0.0 - num;
		}
		if (value.Imaginary < 0.0)
		{
			v = 0.0 - v;
		}
		return new Complex(num, v);
	}

	public static Complex Cos(Complex value)
	{
		double num = Math.Exp(value.m_imaginary);
		double num2 = 1.0 / num;
		double num3 = (num - num2) * 0.5;
		double num4 = (num + num2) * 0.5;
		return new Complex(Math.Cos(value.m_real) * num4, (0.0 - Math.Sin(value.m_real)) * num3);
	}

	public static Complex Cosh(Complex value)
	{
		return Cos(new Complex(0.0 - value.m_imaginary, value.m_real));
	}

	public static Complex Acos(Complex value)
	{
		Asin_Internal(Math.Abs(value.Real), Math.Abs(value.Imaginary), out var b, out var bPrime, out var v);
		double num = ((!(bPrime < 0.0)) ? Math.Atan(1.0 / bPrime) : Math.Acos(b));
		if (value.Real < 0.0)
		{
			num = Math.PI - num;
		}
		if (value.Imaginary > 0.0)
		{
			v = 0.0 - v;
		}
		return new Complex(num, v);
	}

	public static Complex Tan(Complex value)
	{
		double num = 2.0 * value.m_real;
		double num2 = 2.0 * value.m_imaginary;
		double num3 = Math.Exp(num2);
		double num4 = 1.0 / num3;
		double num5 = (num3 + num4) * 0.5;
		if (Math.Abs(value.m_imaginary) <= 4.0)
		{
			double num6 = (num3 - num4) * 0.5;
			double num7 = Math.Cos(num) + num5;
			return new Complex(Math.Sin(num) / num7, num6 / num7);
		}
		double num8 = 1.0 + Math.Cos(num) / num5;
		return new Complex(Math.Sin(num) / num5 / num8, Math.Tanh(num2) / num8);
	}

	public static Complex Tanh(Complex value)
	{
		Complex complex = Tan(new Complex(0.0 - value.m_imaginary, value.m_real));
		return new Complex(complex.m_imaginary, 0.0 - complex.m_real);
	}

	public static Complex Atan(Complex value)
	{
		Complex complex = new Complex(2.0, 0.0);
		return ImaginaryOne / complex * (Log(One - ImaginaryOne * value) - Log(One + ImaginaryOne * value));
	}

	private static void Asin_Internal(double x, double y, out double b, out double bPrime, out double v)
	{
		if (x > s_asinOverflowThreshold || y > s_asinOverflowThreshold)
		{
			b = -1.0;
			bPrime = x / y;
			double num;
			double num2;
			if (x < y)
			{
				num = x;
				num2 = y;
			}
			else
			{
				num = y;
				num2 = x;
			}
			double num3 = num / num2;
			v = s_log2 + Math.Log(num2) + 0.5 * Log1P(num3 * num3);
			return;
		}
		double num4 = Hypot(x + 1.0, y);
		double num5 = Hypot(x - 1.0, y);
		double num6 = (num4 + num5) * 0.5;
		b = x / num6;
		if (b > 0.75)
		{
			if (x <= 1.0)
			{
				double num7 = (y * y / (num4 + (x + 1.0)) + (num5 + (1.0 - x))) * 0.5;
				bPrime = x / Math.Sqrt((num6 + x) * num7);
			}
			else
			{
				double num8 = (1.0 / (num4 + (x + 1.0)) + 1.0 / (num5 + (x - 1.0))) * 0.5;
				bPrime = x / y / Math.Sqrt((num6 + x) * num8);
			}
		}
		else
		{
			bPrime = -1.0;
		}
		if (num6 < 1.5)
		{
			if (x < 1.0)
			{
				double num9 = (1.0 / (num4 + (x + 1.0)) + 1.0 / (num5 + (1.0 - x))) * 0.5;
				double num10 = y * y * num9;
				v = Log1P(num10 + y * Math.Sqrt(num9 * (num6 + 1.0)));
			}
			else
			{
				double num11 = (y * y / (num4 + (x + 1.0)) + (num5 + (x - 1.0))) * 0.5;
				v = Log1P(num11 + Math.Sqrt(num11 * (num6 + 1.0)));
			}
		}
		else
		{
			v = Math.Log(num6 + Math.Sqrt((num6 - 1.0) * (num6 + 1.0)));
		}
	}

	public static bool IsFinite(Complex value)
	{
		if (double.IsFinite(value.m_real))
		{
			return double.IsFinite(value.m_imaginary);
		}
		return false;
	}

	public static bool IsInfinity(Complex value)
	{
		if (!double.IsInfinity(value.m_real))
		{
			return double.IsInfinity(value.m_imaginary);
		}
		return true;
	}

	public static bool IsNaN(Complex value)
	{
		if (!IsInfinity(value))
		{
			return !IsFinite(value);
		}
		return false;
	}

	public static Complex Log(Complex value)
	{
		return new Complex(Math.Log(Abs(value)), Math.Atan2(value.m_imaginary, value.m_real));
	}

	public static Complex Log(Complex value, double baseValue)
	{
		return Log(value) / Log(baseValue);
	}

	public static Complex Log10(Complex value)
	{
		Complex value2 = Log(value);
		return Scale(value2, 0.43429448190325);
	}

	public static Complex Exp(Complex value)
	{
		double num = Math.Exp(value.m_real);
		double real = num * Math.Cos(value.m_imaginary);
		double imaginary = num * Math.Sin(value.m_imaginary);
		return new Complex(real, imaginary);
	}

	public static Complex Sqrt(Complex value)
	{
		if (value.m_imaginary == 0.0)
		{
			if (value.m_real < 0.0)
			{
				return new Complex(0.0, Math.Sqrt(0.0 - value.m_real));
			}
			return new Complex(Math.Sqrt(value.m_real), 0.0);
		}
		bool flag = false;
		double num = value.m_real;
		double num2 = value.m_imaginary;
		if (Math.Abs(num) >= s_sqrtRescaleThreshold || Math.Abs(num2) >= s_sqrtRescaleThreshold)
		{
			if (double.IsInfinity(value.m_imaginary) && !double.IsNaN(value.m_real))
			{
				return new Complex(double.PositiveInfinity, num2);
			}
			num *= 0.25;
			num2 *= 0.25;
			flag = true;
		}
		double num3;
		double num4;
		if (num >= 0.0)
		{
			num3 = Math.Sqrt((Hypot(num, num2) + num) * 0.5);
			num4 = num2 / (2.0 * num3);
		}
		else
		{
			num4 = Math.Sqrt((Hypot(num, num2) - num) * 0.5);
			if (num2 < 0.0)
			{
				num4 = 0.0 - num4;
			}
			num3 = num2 / (2.0 * num4);
		}
		if (flag)
		{
			num3 *= 2.0;
			num4 *= 2.0;
		}
		return new Complex(num3, num4);
	}

	public static Complex Pow(Complex value, Complex power)
	{
		if (power == Zero)
		{
			return One;
		}
		if (value == Zero)
		{
			return Zero;
		}
		double real = value.m_real;
		double imaginary = value.m_imaginary;
		double real2 = power.m_real;
		double imaginary2 = power.m_imaginary;
		double num = Abs(value);
		double num2 = Math.Atan2(imaginary, real);
		double num3 = real2 * num2 + imaginary2 * Math.Log(num);
		double num4 = Math.Pow(num, real2) * Math.Pow(Math.E, (0.0 - imaginary2) * num2);
		return new Complex(num4 * Math.Cos(num3), num4 * Math.Sin(num3));
	}

	public static Complex Pow(Complex value, double power)
	{
		return Pow(value, new Complex(power, 0.0));
	}

	private static Complex Scale(Complex value, double factor)
	{
		double real = factor * value.m_real;
		double imaginary = factor * value.m_imaginary;
		return new Complex(real, imaginary);
	}

	public static implicit operator Complex(short value)
	{
		return new Complex(value, 0.0);
	}

	public static implicit operator Complex(int value)
	{
		return new Complex(value, 0.0);
	}

	public static implicit operator Complex(long value)
	{
		return new Complex(value, 0.0);
	}

	[CLSCompliant(false)]
	public static implicit operator Complex(ushort value)
	{
		return new Complex((int)value, 0.0);
	}

	[CLSCompliant(false)]
	public static implicit operator Complex(uint value)
	{
		return new Complex(value, 0.0);
	}

	[CLSCompliant(false)]
	public static implicit operator Complex(ulong value)
	{
		return new Complex(value, 0.0);
	}

	[CLSCompliant(false)]
	public static implicit operator Complex(sbyte value)
	{
		return new Complex(value, 0.0);
	}

	public static implicit operator Complex(byte value)
	{
		return new Complex((int)value, 0.0);
	}

	public static implicit operator Complex(float value)
	{
		return new Complex(value, 0.0);
	}

	public static implicit operator Complex(double value)
	{
		return new Complex(value, 0.0);
	}

	public static explicit operator Complex(BigInteger value)
	{
		return new Complex((double)value, 0.0);
	}

	public static explicit operator Complex(decimal value)
	{
		return new Complex((double)value, 0.0);
	}
}
