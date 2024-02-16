using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Numerics;

[Intrinsic]
public struct Vector2 : IEquatable<Vector2>, IFormattable
{
	public float X;

	public float Y;

	public static Vector2 Zero
	{
		[Intrinsic]
		get
		{
			return default(Vector2);
		}
	}

	public static Vector2 One
	{
		[Intrinsic]
		get
		{
			return new Vector2(1f);
		}
	}

	public static Vector2 UnitX => new Vector2(1f, 0f);

	public static Vector2 UnitY => new Vector2(0f, 1f);

	[Intrinsic]
	public Vector2(float value)
		: this(value, value)
	{
	}

	[Intrinsic]
	public Vector2(float x, float y)
	{
		X = x;
		Y = y;
	}

	public Vector2(ReadOnlySpan<float> values)
	{
		if (values.Length < 2)
		{
			Vector.ThrowInsufficientNumberOfElementsException(2);
		}
		this = Unsafe.ReadUnaligned<Vector2>(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(values)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector2 operator +(Vector2 left, Vector2 right)
	{
		return new Vector2(left.X + right.X, left.Y + right.Y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector2 operator /(Vector2 left, Vector2 right)
	{
		return new Vector2(left.X / right.X, left.Y / right.Y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 operator /(Vector2 value1, float value2)
	{
		return value1 / new Vector2(value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator ==(Vector2 left, Vector2 right)
	{
		if (left.X == right.X)
		{
			return left.Y == right.Y;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator !=(Vector2 left, Vector2 right)
	{
		return !(left == right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector2 operator *(Vector2 left, Vector2 right)
	{
		return new Vector2(left.X * right.X, left.Y * right.Y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 operator *(Vector2 left, float right)
	{
		return left * new Vector2(right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 operator *(float left, Vector2 right)
	{
		return right * left;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector2 operator -(Vector2 left, Vector2 right)
	{
		return new Vector2(left.X - right.X, left.Y - right.Y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 operator -(Vector2 value)
	{
		return Zero - value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector2 Abs(Vector2 value)
	{
		return new Vector2(MathF.Abs(value.X), MathF.Abs(value.Y));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Add(Vector2 left, Vector2 right)
	{
		return left + right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Clamp(Vector2 value1, Vector2 min, Vector2 max)
	{
		return Min(Max(value1, min), max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Distance(Vector2 value1, Vector2 value2)
	{
		float x = DistanceSquared(value1, value2);
		return MathF.Sqrt(x);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceSquared(Vector2 value1, Vector2 value2)
	{
		Vector2 vector = value1 - value2;
		return Dot(vector, vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Divide(Vector2 left, Vector2 right)
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Divide(Vector2 left, float divisor)
	{
		return left / divisor;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static float Dot(Vector2 value1, Vector2 value2)
	{
		return value1.X * value2.X + value1.Y * value2.Y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Lerp(Vector2 value1, Vector2 value2, float amount)
	{
		return value1 * (1f - amount) + value2 * amount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector2 Max(Vector2 value1, Vector2 value2)
	{
		return new Vector2((value1.X > value2.X) ? value1.X : value2.X, (value1.Y > value2.Y) ? value1.Y : value2.Y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector2 Min(Vector2 value1, Vector2 value2)
	{
		return new Vector2((value1.X < value2.X) ? value1.X : value2.X, (value1.Y < value2.Y) ? value1.Y : value2.Y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Multiply(Vector2 left, Vector2 right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Multiply(Vector2 left, float right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Multiply(float left, Vector2 right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Negate(Vector2 value)
	{
		return -value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Normalize(Vector2 value)
	{
		return value / value.Length();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Reflect(Vector2 vector, Vector2 normal)
	{
		float num = Dot(vector, normal);
		return vector - 2f * num * normal;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector2 SquareRoot(Vector2 value)
	{
		return new Vector2(MathF.Sqrt(value.X), MathF.Sqrt(value.Y));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Subtract(Vector2 left, Vector2 right)
	{
		return left - right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Transform(Vector2 position, Matrix3x2 matrix)
	{
		return new Vector2(position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M31, position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M32);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Transform(Vector2 position, Matrix4x4 matrix)
	{
		return new Vector2(position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M41, position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M42);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 Transform(Vector2 value, Quaternion rotation)
	{
		float num = rotation.X + rotation.X;
		float num2 = rotation.Y + rotation.Y;
		float num3 = rotation.Z + rotation.Z;
		float num4 = rotation.W * num3;
		float num5 = rotation.X * num;
		float num6 = rotation.X * num2;
		float num7 = rotation.Y * num2;
		float num8 = rotation.Z * num3;
		return new Vector2(value.X * (1f - num7 - num8) + value.Y * (num6 - num4), value.X * (num6 + num4) + value.Y * (1f - num5 - num8));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 TransformNormal(Vector2 normal, Matrix3x2 matrix)
	{
		return new Vector2(normal.X * matrix.M11 + normal.Y * matrix.M21, normal.X * matrix.M12 + normal.Y * matrix.M22);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector2 TransformNormal(Vector2 normal, Matrix4x4 matrix)
	{
		return new Vector2(normal.X * matrix.M11 + normal.Y * matrix.M21, normal.X * matrix.M12 + normal.Y * matrix.M22);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public readonly void CopyTo(float[] array)
	{
		CopyTo(array, 0);
	}

	[Intrinsic]
	public readonly void CopyTo(float[] array, int index)
	{
		if (array == null)
		{
			throw new NullReferenceException(SR.Arg_NullArgumentNullRef);
		}
		if (index < 0 || index >= array.Length)
		{
			throw new ArgumentOutOfRangeException("index", SR.Format(SR.Arg_ArgumentOutOfRangeException, index));
		}
		if (array.Length - index < 2)
		{
			throw new ArgumentException(SR.Format(SR.Arg_ElementsInSourceIsGreaterThanDestination, index));
		}
		array[index] = X;
		array[index + 1] = Y;
	}

	public readonly void CopyTo(Span<float> destination)
	{
		if (destination.Length < 2)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(destination)), this);
	}

	public readonly bool TryCopyTo(Span<float> destination)
	{
		if (destination.Length < 2)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(destination)), this);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override readonly bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Vector2 other)
		{
			return Equals(other);
		}
		return false;
	}

	[Intrinsic]
	public readonly bool Equals(Vector2 other)
	{
		return this == other;
	}

	public override readonly int GetHashCode()
	{
		return HashCode.Combine(X, Y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly float Length()
	{
		float x = LengthSquared();
		return MathF.Sqrt(x);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly float LengthSquared()
	{
		return Dot(this, this);
	}

	public override readonly string ToString()
	{
		return ToString("G", CultureInfo.CurrentCulture);
	}

	public readonly string ToString(string? format)
	{
		return ToString(format, CultureInfo.CurrentCulture);
	}

	public readonly string ToString(string? format, IFormatProvider? formatProvider)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string numberGroupSeparator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		stringBuilder.Append('<');
		stringBuilder.Append(X.ToString(format, formatProvider));
		stringBuilder.Append(numberGroupSeparator);
		stringBuilder.Append(' ');
		stringBuilder.Append(Y.ToString(format, formatProvider));
		stringBuilder.Append('>');
		return stringBuilder.ToString();
	}
}
