using System;
using System.Diagnostics;

namespace ReLogic.Utilities;

[Serializable]
[DebuggerDisplay("{DebugDisplayString,nq}")]
public struct Vector2D : IEquatable<Vector2D>
{
	public double X;

	public double Y;

	private static Vector2D zeroVector = new Vector2D(0.0, 0.0);

	private static Vector2D unitVector = new Vector2D(1.0, 1.0);

	private static Vector2D unitXVector = new Vector2D(1.0, 0.0);

	private static Vector2D unitYVector = new Vector2D(0.0, 1.0);

	public static readonly double DoubleEpsilon = Math.Pow(0.5, 53.0);

	public static Vector2D Zero => zeroVector;

	public static Vector2D One => unitVector;

	public static Vector2D UnitX => unitXVector;

	public static Vector2D UnitY => unitYVector;

	internal string DebugDisplayString => X + " " + Y;

	public Vector2D(double x, double y)
	{
		X = x;
		Y = y;
	}

	public Vector2D(double value)
	{
		X = value;
		Y = value;
	}

	public override bool Equals(object obj)
	{
		if (obj is Vector2D)
		{
			return Equals((Vector2D)obj);
		}
		return false;
	}

	public bool Equals(Vector2D other)
	{
		if (X == other.X)
		{
			return Y == other.Y;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return X.GetHashCode() + Y.GetHashCode();
	}

	public double Length()
	{
		return Math.Sqrt(X * X + Y * Y);
	}

	public double LengthSquared()
	{
		return X * X + Y * Y;
	}

	public void Normalize()
	{
		double num = 1.0 / Math.Sqrt(X * X + Y * Y);
		X *= num;
		Y *= num;
	}

	public override string ToString()
	{
		return "{X:" + X + " Y:" + Y + "}";
	}

	public static Vector2D Add(Vector2D value1, Vector2D value2)
	{
		value1.X += value2.X;
		value1.Y += value2.Y;
		return value1;
	}

	public static void Add(ref Vector2D value1, ref Vector2D value2, out Vector2D result)
	{
		result.X = value1.X + value2.X;
		result.Y = value1.Y + value2.Y;
	}

	public static Vector2D Barycentric(Vector2D value1, Vector2D value2, Vector2D value3, double amount1, double amount2)
	{
		return new Vector2D(Barycentric(value1.X, value2.X, value3.X, amount1, amount2), Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2));
	}

	public static void Barycentric(ref Vector2D value1, ref Vector2D value2, ref Vector2D value3, double amount1, double amount2, out Vector2D result)
	{
		result.X = Barycentric(value1.X, value2.X, value3.X, amount1, amount2);
		result.Y = Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2);
	}

	public static Vector2D CatmullRom(Vector2D value1, Vector2D value2, Vector2D value3, Vector2D value4, double amount)
	{
		return new Vector2D(CatmullRom(value1.X, value2.X, value3.X, value4.X, amount), CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount));
	}

	public static void CatmullRom(ref Vector2D value1, ref Vector2D value2, ref Vector2D value3, ref Vector2D value4, double amount, out Vector2D result)
	{
		result.X = CatmullRom(value1.X, value2.X, value3.X, value4.X, amount);
		result.Y = CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount);
	}

	public static Vector2D Clamp(Vector2D value1, Vector2D min, Vector2D max)
	{
		return new Vector2D(Clamp(value1.X, min.X, max.X), Clamp(value1.Y, min.Y, max.Y));
	}

	public static void Clamp(ref Vector2D value1, ref Vector2D min, ref Vector2D max, out Vector2D result)
	{
		result.X = Clamp(value1.X, min.X, max.X);
		result.Y = Clamp(value1.Y, min.Y, max.Y);
	}

	public static double Distance(Vector2D value1, Vector2D value2)
	{
		double num3 = value1.X - value2.X;
		double num2 = value1.Y - value2.Y;
		return Math.Sqrt(num3 * num3 + num2 * num2);
	}

	public static void Distance(ref Vector2D value1, ref Vector2D value2, out double result)
	{
		double num = value1.X - value2.X;
		double num2 = value1.Y - value2.Y;
		result = Math.Sqrt(num * num + num2 * num2);
	}

	public static double DistanceSquared(Vector2D value1, Vector2D value2)
	{
		double num3 = value1.X - value2.X;
		double num2 = value1.Y - value2.Y;
		return num3 * num3 + num2 * num2;
	}

	public static void DistanceSquared(ref Vector2D value1, ref Vector2D value2, out double result)
	{
		double num = value1.X - value2.X;
		double num2 = value1.Y - value2.Y;
		result = num * num + num2 * num2;
	}

	public static Vector2D Divide(Vector2D value1, Vector2D value2)
	{
		value1.X /= value2.X;
		value1.Y /= value2.Y;
		return value1;
	}

	public static void Divide(ref Vector2D value1, ref Vector2D value2, out Vector2D result)
	{
		result.X = value1.X / value2.X;
		result.Y = value1.Y / value2.Y;
	}

	public static Vector2D Divide(Vector2D value1, double divider)
	{
		double num = 1.0 / divider;
		value1.X *= num;
		value1.Y *= num;
		return value1;
	}

	public static void Divide(ref Vector2D value1, double divider, out Vector2D result)
	{
		double num = 1.0 / divider;
		result.X = value1.X * num;
		result.Y = value1.Y * num;
	}

	public static double Dot(Vector2D value1, Vector2D value2)
	{
		return value1.X * value2.X + value1.Y * value2.Y;
	}

	public static void Dot(ref Vector2D value1, ref Vector2D value2, out double result)
	{
		result = value1.X * value2.X + value1.Y * value2.Y;
	}

	public static Vector2D Hermite(Vector2D value1, Vector2D tangent1, Vector2D value2, Vector2D tangent2, double amount)
	{
		Vector2D result = default(Vector2D);
		Hermite(ref value1, ref tangent1, ref value2, ref tangent2, amount, out result);
		return result;
	}

	public static void Hermite(ref Vector2D value1, ref Vector2D tangent1, ref Vector2D value2, ref Vector2D tangent2, double amount, out Vector2D result)
	{
		result.X = Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount);
		result.Y = Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount);
	}

	public static Vector2D Lerp(Vector2D value1, Vector2D value2, double amount)
	{
		return new Vector2D(Lerp(value1.X, value2.X, amount), Lerp(value1.Y, value2.Y, amount));
	}

	public static void Lerp(ref Vector2D value1, ref Vector2D value2, double amount, out Vector2D result)
	{
		result.X = Lerp(value1.X, value2.X, amount);
		result.Y = Lerp(value1.Y, value2.Y, amount);
	}

	public static Vector2D Max(Vector2D value1, Vector2D value2)
	{
		return new Vector2D((value1.X > value2.X) ? value1.X : value2.X, (value1.Y > value2.Y) ? value1.Y : value2.Y);
	}

	public static void Max(ref Vector2D value1, ref Vector2D value2, out Vector2D result)
	{
		result.X = ((value1.X > value2.X) ? value1.X : value2.X);
		result.Y = ((value1.Y > value2.Y) ? value1.Y : value2.Y);
	}

	public static Vector2D Min(Vector2D value1, Vector2D value2)
	{
		return new Vector2D((value1.X < value2.X) ? value1.X : value2.X, (value1.Y < value2.Y) ? value1.Y : value2.Y);
	}

	public static void Min(ref Vector2D value1, ref Vector2D value2, out Vector2D result)
	{
		result.X = ((value1.X < value2.X) ? value1.X : value2.X);
		result.Y = ((value1.Y < value2.Y) ? value1.Y : value2.Y);
	}

	public static Vector2D Multiply(Vector2D value1, Vector2D value2)
	{
		value1.X *= value2.X;
		value1.Y *= value2.Y;
		return value1;
	}

	public static Vector2D Multiply(Vector2D value1, double scaleFactor)
	{
		value1.X *= scaleFactor;
		value1.Y *= scaleFactor;
		return value1;
	}

	public static void Multiply(ref Vector2D value1, double scaleFactor, out Vector2D result)
	{
		result.X = value1.X * scaleFactor;
		result.Y = value1.Y * scaleFactor;
	}

	public static void Multiply(ref Vector2D value1, ref Vector2D value2, out Vector2D result)
	{
		result.X = value1.X * value2.X;
		result.Y = value1.Y * value2.Y;
	}

	public static Vector2D Negate(Vector2D value)
	{
		value.X = 0.0 - value.X;
		value.Y = 0.0 - value.Y;
		return value;
	}

	public static void Negate(ref Vector2D value, out Vector2D result)
	{
		result.X = 0.0 - value.X;
		result.Y = 0.0 - value.Y;
	}

	public static Vector2D Normalize(Vector2D value)
	{
		double num = 1.0 / Math.Sqrt(value.X * value.X + value.Y * value.Y);
		value.X *= num;
		value.Y *= num;
		return value;
	}

	public static void Normalize(ref Vector2D value, out Vector2D result)
	{
		double num = 1.0 / Math.Sqrt(value.X * value.X + value.Y * value.Y);
		result.X = value.X * num;
		result.Y = value.Y * num;
	}

	public static Vector2D Reflect(Vector2D vector, Vector2D normal)
	{
		double num = 2.0 * (vector.X * normal.X + vector.Y * normal.Y);
		Vector2D result = default(Vector2D);
		result.X = vector.X - normal.X * num;
		result.Y = vector.Y - normal.Y * num;
		return result;
	}

	public static void Reflect(ref Vector2D vector, ref Vector2D normal, out Vector2D result)
	{
		double num = 2.0 * (vector.X * normal.X + vector.Y * normal.Y);
		result.X = vector.X - normal.X * num;
		result.Y = vector.Y - normal.Y * num;
	}

	public static Vector2D SmoothStep(Vector2D value1, Vector2D value2, double amount)
	{
		return new Vector2D(SmoothStep(value1.X, value2.X, amount), SmoothStep(value1.Y, value2.Y, amount));
	}

	public static void SmoothStep(ref Vector2D value1, ref Vector2D value2, double amount, out Vector2D result)
	{
		result.X = SmoothStep(value1.X, value2.X, amount);
		result.Y = SmoothStep(value1.Y, value2.Y, amount);
	}

	public static Vector2D Subtract(Vector2D value1, Vector2D value2)
	{
		value1.X -= value2.X;
		value1.Y -= value2.Y;
		return value1;
	}

	public static void Subtract(ref Vector2D value1, ref Vector2D value2, out Vector2D result)
	{
		result.X = value1.X - value2.X;
		result.Y = value1.Y - value2.Y;
	}

	public static Vector2D operator -(Vector2D value)
	{
		value.X = 0.0 - value.X;
		value.Y = 0.0 - value.Y;
		return value;
	}

	public static bool operator ==(Vector2D value1, Vector2D value2)
	{
		if (value1.X == value2.X)
		{
			return value1.Y == value2.Y;
		}
		return false;
	}

	public static bool operator !=(Vector2D value1, Vector2D value2)
	{
		return !(value1 == value2);
	}

	public static Vector2D operator +(Vector2D value1, Vector2D value2)
	{
		value1.X += value2.X;
		value1.Y += value2.Y;
		return value1;
	}

	public static Vector2D operator -(Vector2D value1, Vector2D value2)
	{
		value1.X -= value2.X;
		value1.Y -= value2.Y;
		return value1;
	}

	public static Vector2D operator *(Vector2D value1, Vector2D value2)
	{
		value1.X *= value2.X;
		value1.Y *= value2.Y;
		return value1;
	}

	public static Vector2D operator *(Vector2D value, double scaleFactor)
	{
		value.X *= scaleFactor;
		value.Y *= scaleFactor;
		return value;
	}

	public static Vector2D operator *(double scaleFactor, Vector2D value)
	{
		value.X *= scaleFactor;
		value.Y *= scaleFactor;
		return value;
	}

	public static Vector2D operator /(Vector2D value1, Vector2D value2)
	{
		value1.X /= value2.X;
		value1.Y /= value2.Y;
		return value1;
	}

	public static Vector2D operator /(Vector2D value1, double divider)
	{
		double num = 1.0 / divider;
		value1.X *= num;
		value1.Y *= num;
		return value1;
	}

	public static double Clamp(double value, double min, double max)
	{
		value = ((value > max) ? max : value);
		value = ((value < min) ? min : value);
		return value;
	}

	public static double Lerp(double value1, double value2, double amount)
	{
		return value1 + (value2 - value1) * amount;
	}

	public static double SmoothStep(double value1, double value2, double amount)
	{
		double amount2 = Clamp(amount, 0.0, 1.0);
		return Hermite(value1, 0.0, value2, 0.0, amount2);
	}

	public static double Hermite(double value1, double tangent1, double value2, double tangent2, double amount)
	{
		double num = amount * amount * amount;
		double num2 = amount * amount;
		if (Math.Abs(amount) <= DoubleEpsilon)
		{
			return value1;
		}
		if (amount == 1.0)
		{
			return value2;
		}
		return (2.0 * value1 - 2.0 * value2 + tangent2 + tangent1) * num + (3.0 * value2 - 3.0 * value1 - 2.0 * tangent1 - tangent2) * num2 + tangent1 * amount + value1;
	}

	public static double Barycentric(double value1, double value2, double value3, double amount1, double amount2)
	{
		return value1 + (value2 - value1) * amount1 + (value3 - value1) * amount2;
	}

	public static double CatmullRom(double value1, double value2, double value3, double value4, double amount)
	{
		double num = amount * amount;
		double num2 = num * amount;
		return 0.5 * (2.0 * value2 + (value3 - value1) * amount + (2.0 * value1 - 5.0 * value2 + 4.0 * value3 - value4) * num + (3.0 * value2 - value1 - 3.0 * value3 + value4) * num2);
	}
}
