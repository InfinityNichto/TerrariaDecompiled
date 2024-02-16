using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Numerics;

[Intrinsic]
public struct Quaternion : IEquatable<Quaternion>
{
	public float X;

	public float Y;

	public float Z;

	public float W;

	public static Quaternion Identity => new Quaternion(0f, 0f, 0f, 1f);

	public readonly bool IsIdentity => this == Identity;

	public Quaternion(float x, float y, float z, float w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public Quaternion(Vector3 vectorPart, float scalarPart)
	{
		X = vectorPart.X;
		Y = vectorPart.Y;
		Z = vectorPart.Z;
		W = scalarPart;
	}

	public static Quaternion operator +(Quaternion value1, Quaternion value2)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Quaternion result);
		result.X = value1.X + value2.X;
		result.Y = value1.Y + value2.Y;
		result.Z = value1.Z + value2.Z;
		result.W = value1.W + value2.W;
		return result;
	}

	public static Quaternion operator /(Quaternion value1, Quaternion value2)
	{
		float x = value1.X;
		float y = value1.Y;
		float z = value1.Z;
		float w = value1.W;
		float num = value2.X * value2.X + value2.Y * value2.Y + value2.Z * value2.Z + value2.W * value2.W;
		float num2 = 1f / num;
		float num3 = (0f - value2.X) * num2;
		float num4 = (0f - value2.Y) * num2;
		float num5 = (0f - value2.Z) * num2;
		float num6 = value2.W * num2;
		float num7 = y * num5 - z * num4;
		float num8 = z * num3 - x * num5;
		float num9 = x * num4 - y * num3;
		float num10 = x * num3 + y * num4 + z * num5;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Quaternion result);
		result.X = x * num6 + num3 * w + num7;
		result.Y = y * num6 + num4 * w + num8;
		result.Z = z * num6 + num5 * w + num9;
		result.W = w * num6 - num10;
		return result;
	}

	public static bool operator ==(Quaternion value1, Quaternion value2)
	{
		if (value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z)
		{
			return value1.W == value2.W;
		}
		return false;
	}

	public static bool operator !=(Quaternion value1, Quaternion value2)
	{
		return !(value1 == value2);
	}

	public static Quaternion operator *(Quaternion value1, Quaternion value2)
	{
		float x = value1.X;
		float y = value1.Y;
		float z = value1.Z;
		float w = value1.W;
		float x2 = value2.X;
		float y2 = value2.Y;
		float z2 = value2.Z;
		float w2 = value2.W;
		float num = y * z2 - z * y2;
		float num2 = z * x2 - x * z2;
		float num3 = x * y2 - y * x2;
		float num4 = x * x2 + y * y2 + z * z2;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Quaternion result);
		result.X = x * w2 + x2 * w + num;
		result.Y = y * w2 + y2 * w + num2;
		result.Z = z * w2 + z2 * w + num3;
		result.W = w * w2 - num4;
		return result;
	}

	public static Quaternion operator *(Quaternion value1, float value2)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Quaternion result);
		result.X = value1.X * value2;
		result.Y = value1.Y * value2;
		result.Z = value1.Z * value2;
		result.W = value1.W * value2;
		return result;
	}

	public static Quaternion operator -(Quaternion value1, Quaternion value2)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Quaternion result);
		result.X = value1.X - value2.X;
		result.Y = value1.Y - value2.Y;
		result.Z = value1.Z - value2.Z;
		result.W = value1.W - value2.W;
		return result;
	}

	public static Quaternion operator -(Quaternion value)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Quaternion result);
		result.X = 0f - value.X;
		result.Y = 0f - value.Y;
		result.Z = 0f - value.Z;
		result.W = 0f - value.W;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion Add(Quaternion value1, Quaternion value2)
	{
		return value1 + value2;
	}

	public static Quaternion Concatenate(Quaternion value1, Quaternion value2)
	{
		float x = value2.X;
		float y = value2.Y;
		float z = value2.Z;
		float w = value2.W;
		float x2 = value1.X;
		float y2 = value1.Y;
		float z2 = value1.Z;
		float w2 = value1.W;
		float num = y * z2 - z * y2;
		float num2 = z * x2 - x * z2;
		float num3 = x * y2 - y * x2;
		float num4 = x * x2 + y * y2 + z * z2;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Quaternion result);
		result.X = x * w2 + x2 * w + num;
		result.Y = y * w2 + y2 * w + num2;
		result.Z = z * w2 + z2 * w + num3;
		result.W = w * w2 - num4;
		return result;
	}

	public static Quaternion Conjugate(Quaternion value)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Quaternion result);
		result.X = 0f - value.X;
		result.Y = 0f - value.Y;
		result.Z = 0f - value.Z;
		result.W = value.W;
		return result;
	}

	public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
	{
		float x = angle * 0.5f;
		float num = MathF.Sin(x);
		float w = MathF.Cos(x);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Quaternion result);
		result.X = axis.X * num;
		result.Y = axis.Y * num;
		result.Z = axis.Z * num;
		result.W = w;
		return result;
	}

	public static Quaternion CreateFromRotationMatrix(Matrix4x4 matrix)
	{
		float num = matrix.M11 + matrix.M22 + matrix.M33;
		Quaternion result = default(Quaternion);
		if (num > 0f)
		{
			float num2 = MathF.Sqrt(num + 1f);
			result.W = num2 * 0.5f;
			num2 = 0.5f / num2;
			result.X = (matrix.M23 - matrix.M32) * num2;
			result.Y = (matrix.M31 - matrix.M13) * num2;
			result.Z = (matrix.M12 - matrix.M21) * num2;
		}
		else if (matrix.M11 >= matrix.M22 && matrix.M11 >= matrix.M33)
		{
			float num3 = MathF.Sqrt(1f + matrix.M11 - matrix.M22 - matrix.M33);
			float num4 = 0.5f / num3;
			result.X = 0.5f * num3;
			result.Y = (matrix.M12 + matrix.M21) * num4;
			result.Z = (matrix.M13 + matrix.M31) * num4;
			result.W = (matrix.M23 - matrix.M32) * num4;
		}
		else if (matrix.M22 > matrix.M33)
		{
			float num5 = MathF.Sqrt(1f + matrix.M22 - matrix.M11 - matrix.M33);
			float num6 = 0.5f / num5;
			result.X = (matrix.M21 + matrix.M12) * num6;
			result.Y = 0.5f * num5;
			result.Z = (matrix.M32 + matrix.M23) * num6;
			result.W = (matrix.M31 - matrix.M13) * num6;
		}
		else
		{
			float num7 = MathF.Sqrt(1f + matrix.M33 - matrix.M11 - matrix.M22);
			float num8 = 0.5f / num7;
			result.X = (matrix.M31 + matrix.M13) * num8;
			result.Y = (matrix.M32 + matrix.M23) * num8;
			result.Z = 0.5f * num7;
			result.W = (matrix.M12 - matrix.M21) * num8;
		}
		return result;
	}

	public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
	{
		float x = roll * 0.5f;
		float num = MathF.Sin(x);
		float num2 = MathF.Cos(x);
		float x2 = pitch * 0.5f;
		float num3 = MathF.Sin(x2);
		float num4 = MathF.Cos(x2);
		float x3 = yaw * 0.5f;
		float num5 = MathF.Sin(x3);
		float num6 = MathF.Cos(x3);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Quaternion result);
		result.X = num6 * num3 * num2 + num5 * num4 * num;
		result.Y = num5 * num4 * num2 - num6 * num3 * num;
		result.Z = num6 * num4 * num - num5 * num3 * num2;
		result.W = num6 * num4 * num2 + num5 * num3 * num;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion Divide(Quaternion value1, Quaternion value2)
	{
		return value1 / value2;
	}

	public static float Dot(Quaternion quaternion1, Quaternion quaternion2)
	{
		return quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W;
	}

	public static Quaternion Inverse(Quaternion value)
	{
		float num = value.X * value.X + value.Y * value.Y + value.Z * value.Z + value.W * value.W;
		float num2 = 1f / num;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Quaternion result);
		result.X = (0f - value.X) * num2;
		result.Y = (0f - value.Y) * num2;
		result.Z = (0f - value.Z) * num2;
		result.W = value.W * num2;
		return result;
	}

	public static Quaternion Lerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
	{
		float num = 1f - amount;
		Quaternion result = default(Quaternion);
		float num2 = quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W;
		if (num2 >= 0f)
		{
			result.X = num * quaternion1.X + amount * quaternion2.X;
			result.Y = num * quaternion1.Y + amount * quaternion2.Y;
			result.Z = num * quaternion1.Z + amount * quaternion2.Z;
			result.W = num * quaternion1.W + amount * quaternion2.W;
		}
		else
		{
			result.X = num * quaternion1.X - amount * quaternion2.X;
			result.Y = num * quaternion1.Y - amount * quaternion2.Y;
			result.Z = num * quaternion1.Z - amount * quaternion2.Z;
			result.W = num * quaternion1.W - amount * quaternion2.W;
		}
		float x = result.X * result.X + result.Y * result.Y + result.Z * result.Z + result.W * result.W;
		float num3 = 1f / MathF.Sqrt(x);
		result.X *= num3;
		result.Y *= num3;
		result.Z *= num3;
		result.W *= num3;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion Multiply(Quaternion value1, Quaternion value2)
	{
		return value1 * value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion Multiply(Quaternion value1, float value2)
	{
		return value1 * value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion Negate(Quaternion value)
	{
		return -value;
	}

	public static Quaternion Normalize(Quaternion value)
	{
		float x = value.X * value.X + value.Y * value.Y + value.Z * value.Z + value.W * value.W;
		float num = 1f / MathF.Sqrt(x);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Quaternion result);
		result.X = value.X * num;
		result.Y = value.Y * num;
		result.Z = value.Z * num;
		result.W = value.W * num;
		return result;
	}

	public static Quaternion Slerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
	{
		float num = quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W;
		bool flag = false;
		if (num < 0f)
		{
			flag = true;
			num = 0f - num;
		}
		float num2;
		float num3;
		if (num > 0.999999f)
		{
			num2 = 1f - amount;
			num3 = (flag ? (0f - amount) : amount);
		}
		else
		{
			float num4 = MathF.Acos(num);
			float num5 = 1f / MathF.Sin(num4);
			num2 = MathF.Sin((1f - amount) * num4) * num5;
			num3 = (flag ? ((0f - MathF.Sin(amount * num4)) * num5) : (MathF.Sin(amount * num4) * num5));
		}
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Quaternion result);
		result.X = num2 * quaternion1.X + num3 * quaternion2.X;
		result.Y = num2 * quaternion1.Y + num3 * quaternion2.Y;
		result.Z = num2 * quaternion1.Z + num3 * quaternion2.Z;
		result.W = num2 * quaternion1.W + num3 * quaternion2.W;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion Subtract(Quaternion value1, Quaternion value2)
	{
		return value1 - value2;
	}

	public override readonly bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Quaternion other)
		{
			return Equals(other);
		}
		return false;
	}

	public readonly bool Equals(Quaternion other)
	{
		return this == other;
	}

	public override readonly int GetHashCode()
	{
		return X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode() + W.GetHashCode();
	}

	public readonly float Length()
	{
		float x = LengthSquared();
		return MathF.Sqrt(x);
	}

	public readonly float LengthSquared()
	{
		return X * X + Y * Y + Z * Z + W * W;
	}

	public override readonly string ToString()
	{
		return $"{{X:{X} Y:{Y} Z:{Z} W:{W}}}";
	}
}
