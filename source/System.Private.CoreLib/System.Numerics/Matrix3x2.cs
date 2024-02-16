using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Numerics;

[Intrinsic]
public struct Matrix3x2 : IEquatable<Matrix3x2>
{
	private static readonly Matrix3x2 _identity = new Matrix3x2(1f, 0f, 0f, 1f, 0f, 0f);

	public float M11;

	public float M12;

	public float M21;

	public float M22;

	public float M31;

	public float M32;

	public static Matrix3x2 Identity => _identity;

	public readonly bool IsIdentity => this == Identity;

	public Vector2 Translation
	{
		readonly get
		{
			return new Vector2(M31, M32);
		}
		set
		{
			M31 = value.X;
			M32 = value.Y;
		}
	}

	public Matrix3x2(float m11, float m12, float m21, float m22, float m31, float m32)
	{
		M11 = m11;
		M12 = m12;
		M21 = m21;
		M22 = m22;
		M31 = m31;
		M32 = m32;
	}

	public static Matrix3x2 operator +(Matrix3x2 value1, Matrix3x2 value2)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Matrix3x2 result);
		result.M11 = value1.M11 + value2.M11;
		result.M12 = value1.M12 + value2.M12;
		result.M21 = value1.M21 + value2.M21;
		result.M22 = value1.M22 + value2.M22;
		result.M31 = value1.M31 + value2.M31;
		result.M32 = value1.M32 + value2.M32;
		return result;
	}

	public static bool operator ==(Matrix3x2 value1, Matrix3x2 value2)
	{
		if (value1.M11 == value2.M11 && value1.M22 == value2.M22 && value1.M12 == value2.M12 && value1.M21 == value2.M21 && value1.M31 == value2.M31)
		{
			return value1.M32 == value2.M32;
		}
		return false;
	}

	public static bool operator !=(Matrix3x2 value1, Matrix3x2 value2)
	{
		return !(value1 == value2);
	}

	public static Matrix3x2 operator *(Matrix3x2 value1, Matrix3x2 value2)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Matrix3x2 result);
		result.M11 = value1.M11 * value2.M11 + value1.M12 * value2.M21;
		result.M12 = value1.M11 * value2.M12 + value1.M12 * value2.M22;
		result.M21 = value1.M21 * value2.M11 + value1.M22 * value2.M21;
		result.M22 = value1.M21 * value2.M12 + value1.M22 * value2.M22;
		result.M31 = value1.M31 * value2.M11 + value1.M32 * value2.M21 + value2.M31;
		result.M32 = value1.M31 * value2.M12 + value1.M32 * value2.M22 + value2.M32;
		return result;
	}

	public static Matrix3x2 operator *(Matrix3x2 value1, float value2)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Matrix3x2 result);
		result.M11 = value1.M11 * value2;
		result.M12 = value1.M12 * value2;
		result.M21 = value1.M21 * value2;
		result.M22 = value1.M22 * value2;
		result.M31 = value1.M31 * value2;
		result.M32 = value1.M32 * value2;
		return result;
	}

	public static Matrix3x2 operator -(Matrix3x2 value1, Matrix3x2 value2)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Matrix3x2 result);
		result.M11 = value1.M11 - value2.M11;
		result.M12 = value1.M12 - value2.M12;
		result.M21 = value1.M21 - value2.M21;
		result.M22 = value1.M22 - value2.M22;
		result.M31 = value1.M31 - value2.M31;
		result.M32 = value1.M32 - value2.M32;
		return result;
	}

	public static Matrix3x2 operator -(Matrix3x2 value)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Matrix3x2 result);
		result.M11 = 0f - value.M11;
		result.M12 = 0f - value.M12;
		result.M21 = 0f - value.M21;
		result.M22 = 0f - value.M22;
		result.M31 = 0f - value.M31;
		result.M32 = 0f - value.M32;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix3x2 Add(Matrix3x2 value1, Matrix3x2 value2)
	{
		return value1 + value2;
	}

	public static Matrix3x2 CreateRotation(float radians)
	{
		radians = MathF.IEEERemainder(radians, (float)Math.PI * 2f);
		float num;
		float num2;
		if (radians > -1.7453294E-05f && radians < 1.7453294E-05f)
		{
			num = 1f;
			num2 = 0f;
		}
		else if (radians > 1.570779f && radians < 1.5708138f)
		{
			num = 0f;
			num2 = 1f;
		}
		else if (radians < -3.1415753f || radians > 3.1415753f)
		{
			num = -1f;
			num2 = 0f;
		}
		else if (radians > -1.5708138f && radians < -1.570779f)
		{
			num = 0f;
			num2 = -1f;
		}
		else
		{
			num = MathF.Cos(radians);
			num2 = MathF.Sin(radians);
		}
		Matrix3x2 identity = Identity;
		identity.M11 = num;
		identity.M12 = num2;
		identity.M21 = 0f - num2;
		identity.M22 = num;
		return identity;
	}

	public static Matrix3x2 CreateRotation(float radians, Vector2 centerPoint)
	{
		radians = MathF.IEEERemainder(radians, (float)Math.PI * 2f);
		float num;
		float num2;
		if (radians > -1.7453294E-05f && radians < 1.7453294E-05f)
		{
			num = 1f;
			num2 = 0f;
		}
		else if (radians > 1.570779f && radians < 1.5708138f)
		{
			num = 0f;
			num2 = 1f;
		}
		else if (radians < -3.1415753f || radians > 3.1415753f)
		{
			num = -1f;
			num2 = 0f;
		}
		else if (radians > -1.5708138f && radians < -1.570779f)
		{
			num = 0f;
			num2 = -1f;
		}
		else
		{
			num = MathF.Cos(radians);
			num2 = MathF.Sin(radians);
		}
		float m = centerPoint.X * (1f - num) + centerPoint.Y * num2;
		float m2 = centerPoint.Y * (1f - num) - centerPoint.X * num2;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Matrix3x2 result);
		result.M11 = num;
		result.M12 = num2;
		result.M21 = 0f - num2;
		result.M22 = num;
		result.M31 = m;
		result.M32 = m2;
		return result;
	}

	public static Matrix3x2 CreateScale(Vector2 scales)
	{
		Matrix3x2 identity = Identity;
		identity.M11 = scales.X;
		identity.M22 = scales.Y;
		return identity;
	}

	public static Matrix3x2 CreateScale(float xScale, float yScale)
	{
		Matrix3x2 identity = Identity;
		identity.M11 = xScale;
		identity.M22 = yScale;
		return identity;
	}

	public static Matrix3x2 CreateScale(float xScale, float yScale, Vector2 centerPoint)
	{
		Matrix3x2 identity = Identity;
		float m = centerPoint.X * (1f - xScale);
		float m2 = centerPoint.Y * (1f - yScale);
		identity.M11 = xScale;
		identity.M22 = yScale;
		identity.M31 = m;
		identity.M32 = m2;
		return identity;
	}

	public static Matrix3x2 CreateScale(Vector2 scales, Vector2 centerPoint)
	{
		Matrix3x2 identity = Identity;
		float m = centerPoint.X * (1f - scales.X);
		float m2 = centerPoint.Y * (1f - scales.Y);
		identity.M11 = scales.X;
		identity.M22 = scales.Y;
		identity.M31 = m;
		identity.M32 = m2;
		return identity;
	}

	public static Matrix3x2 CreateScale(float scale)
	{
		Matrix3x2 identity = Identity;
		identity.M11 = scale;
		identity.M22 = scale;
		return identity;
	}

	public static Matrix3x2 CreateScale(float scale, Vector2 centerPoint)
	{
		Matrix3x2 identity = Identity;
		float m = centerPoint.X * (1f - scale);
		float m2 = centerPoint.Y * (1f - scale);
		identity.M11 = scale;
		identity.M22 = scale;
		identity.M31 = m;
		identity.M32 = m2;
		return identity;
	}

	public static Matrix3x2 CreateSkew(float radiansX, float radiansY)
	{
		Matrix3x2 identity = Identity;
		float m = MathF.Tan(radiansX);
		float m2 = MathF.Tan(radiansY);
		identity.M12 = m2;
		identity.M21 = m;
		return identity;
	}

	public static Matrix3x2 CreateSkew(float radiansX, float radiansY, Vector2 centerPoint)
	{
		Matrix3x2 identity = Identity;
		float num = MathF.Tan(radiansX);
		float num2 = MathF.Tan(radiansY);
		float m = (0f - centerPoint.Y) * num;
		float m2 = (0f - centerPoint.X) * num2;
		identity.M12 = num2;
		identity.M21 = num;
		identity.M31 = m;
		identity.M32 = m2;
		return identity;
	}

	public static Matrix3x2 CreateTranslation(Vector2 position)
	{
		Matrix3x2 identity = Identity;
		identity.M31 = position.X;
		identity.M32 = position.Y;
		return identity;
	}

	public static Matrix3x2 CreateTranslation(float xPosition, float yPosition)
	{
		Matrix3x2 identity = Identity;
		identity.M31 = xPosition;
		identity.M32 = yPosition;
		return identity;
	}

	public static bool Invert(Matrix3x2 matrix, out Matrix3x2 result)
	{
		float num = matrix.M11 * matrix.M22 - matrix.M21 * matrix.M12;
		if (MathF.Abs(num) < float.Epsilon)
		{
			result = new Matrix3x2(float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, float.NaN);
			return false;
		}
		float num2 = 1f / num;
		result.M11 = matrix.M22 * num2;
		result.M12 = (0f - matrix.M12) * num2;
		result.M21 = (0f - matrix.M21) * num2;
		result.M22 = matrix.M11 * num2;
		result.M31 = (matrix.M21 * matrix.M32 - matrix.M31 * matrix.M22) * num2;
		result.M32 = (matrix.M31 * matrix.M12 - matrix.M11 * matrix.M32) * num2;
		return true;
	}

	public static Matrix3x2 Lerp(Matrix3x2 matrix1, Matrix3x2 matrix2, float amount)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Matrix3x2 result);
		result.M11 = matrix1.M11 + (matrix2.M11 - matrix1.M11) * amount;
		result.M12 = matrix1.M12 + (matrix2.M12 - matrix1.M12) * amount;
		result.M21 = matrix1.M21 + (matrix2.M21 - matrix1.M21) * amount;
		result.M22 = matrix1.M22 + (matrix2.M22 - matrix1.M22) * amount;
		result.M31 = matrix1.M31 + (matrix2.M31 - matrix1.M31) * amount;
		result.M32 = matrix1.M32 + (matrix2.M32 - matrix1.M32) * amount;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix3x2 Multiply(Matrix3x2 value1, Matrix3x2 value2)
	{
		return value1 * value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix3x2 Multiply(Matrix3x2 value1, float value2)
	{
		return value1 * value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix3x2 Negate(Matrix3x2 value)
	{
		return -value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix3x2 Subtract(Matrix3x2 value1, Matrix3x2 value2)
	{
		return value1 - value2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override readonly bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Matrix3x2 other)
		{
			return Equals(other);
		}
		return false;
	}

	public readonly bool Equals(Matrix3x2 other)
	{
		return this == other;
	}

	public readonly float GetDeterminant()
	{
		return M11 * M22 - M21 * M12;
	}

	public override readonly int GetHashCode()
	{
		return HashCode.Combine(M11, M12, M21, M22, M31, M32);
	}

	public override readonly string ToString()
	{
		return $"{{ {{M11:{M11} M12:{M12}}} {{M21:{M21} M22:{M22}}} {{M31:{M31} M32:{M32}}} }}";
	}
}
