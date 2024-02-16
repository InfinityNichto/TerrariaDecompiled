using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Numerics;

[Intrinsic]
public struct Vector4 : IEquatable<Vector4>, IFormattable
{
	public float X;

	public float Y;

	public float Z;

	public float W;

	public static Vector4 Zero
	{
		[Intrinsic]
		get
		{
			return default(Vector4);
		}
	}

	public static Vector4 One
	{
		[Intrinsic]
		get
		{
			return new Vector4(1f);
		}
	}

	public static Vector4 UnitX => new Vector4(1f, 0f, 0f, 0f);

	public static Vector4 UnitY => new Vector4(0f, 1f, 0f, 0f);

	public static Vector4 UnitZ => new Vector4(0f, 0f, 1f, 0f);

	public static Vector4 UnitW => new Vector4(0f, 0f, 0f, 1f);

	[Intrinsic]
	public Vector4(float value)
		: this(value, value, value, value)
	{
	}

	[Intrinsic]
	public Vector4(Vector2 value, float z, float w)
		: this(value.X, value.Y, z, w)
	{
	}

	[Intrinsic]
	public Vector4(Vector3 value, float w)
		: this(value.X, value.Y, value.Z, w)
	{
	}

	[Intrinsic]
	public Vector4(float x, float y, float z, float w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public Vector4(ReadOnlySpan<float> values)
	{
		if (values.Length < 4)
		{
			Vector.ThrowInsufficientNumberOfElementsException(4);
		}
		this = Unsafe.ReadUnaligned<Vector4>(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(values)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector4 operator +(Vector4 left, Vector4 right)
	{
		return new Vector4(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector4 operator /(Vector4 left, Vector4 right)
	{
		return new Vector4(left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 operator /(Vector4 value1, float value2)
	{
		return value1 / new Vector4(value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator ==(Vector4 left, Vector4 right)
	{
		if (left.X == right.X && left.Y == right.Y && left.Z == right.Z)
		{
			return left.W == right.W;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator !=(Vector4 left, Vector4 right)
	{
		return !(left == right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector4 operator *(Vector4 left, Vector4 right)
	{
		return new Vector4(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 operator *(Vector4 left, float right)
	{
		return left * new Vector4(right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 operator *(float left, Vector4 right)
	{
		return right * left;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector4 operator -(Vector4 left, Vector4 right)
	{
		return new Vector4(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 operator -(Vector4 value)
	{
		return Zero - value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector4 Abs(Vector4 value)
	{
		return new Vector4(MathF.Abs(value.X), MathF.Abs(value.Y), MathF.Abs(value.Z), MathF.Abs(value.W));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Add(Vector4 left, Vector4 right)
	{
		return left + right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Clamp(Vector4 value1, Vector4 min, Vector4 max)
	{
		return Min(Max(value1, min), max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Distance(Vector4 value1, Vector4 value2)
	{
		float x = DistanceSquared(value1, value2);
		return MathF.Sqrt(x);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DistanceSquared(Vector4 value1, Vector4 value2)
	{
		Vector4 vector = value1 - value2;
		return Dot(vector, vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Divide(Vector4 left, Vector4 right)
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Divide(Vector4 left, float divisor)
	{
		return left / divisor;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static float Dot(Vector4 vector1, Vector4 vector2)
	{
		return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z + vector1.W * vector2.W;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Lerp(Vector4 value1, Vector4 value2, float amount)
	{
		return value1 * (1f - amount) + value2 * amount;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector4 Max(Vector4 value1, Vector4 value2)
	{
		return new Vector4((value1.X > value2.X) ? value1.X : value2.X, (value1.Y > value2.Y) ? value1.Y : value2.Y, (value1.Z > value2.Z) ? value1.Z : value2.Z, (value1.W > value2.W) ? value1.W : value2.W);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector4 Min(Vector4 value1, Vector4 value2)
	{
		return new Vector4((value1.X < value2.X) ? value1.X : value2.X, (value1.Y < value2.Y) ? value1.Y : value2.Y, (value1.Z < value2.Z) ? value1.Z : value2.Z, (value1.W < value2.W) ? value1.W : value2.W);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Multiply(Vector4 left, Vector4 right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Multiply(Vector4 left, float right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Multiply(float left, Vector4 right)
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Negate(Vector4 value)
	{
		return -value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Normalize(Vector4 vector)
	{
		return vector / vector.Length();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector4 SquareRoot(Vector4 value)
	{
		return new Vector4(MathF.Sqrt(value.X), MathF.Sqrt(value.Y), MathF.Sqrt(value.Z), MathF.Sqrt(value.W));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Subtract(Vector4 left, Vector4 right)
	{
		return left - right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Transform(Vector2 position, Matrix4x4 matrix)
	{
		return new Vector4(position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M41, position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M42, position.X * matrix.M13 + position.Y * matrix.M23 + matrix.M43, position.X * matrix.M14 + position.Y * matrix.M24 + matrix.M44);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Transform(Vector2 value, Quaternion rotation)
	{
		float num = rotation.X + rotation.X;
		float num2 = rotation.Y + rotation.Y;
		float num3 = rotation.Z + rotation.Z;
		float num4 = rotation.W * num;
		float num5 = rotation.W * num2;
		float num6 = rotation.W * num3;
		float num7 = rotation.X * num;
		float num8 = rotation.X * num2;
		float num9 = rotation.X * num3;
		float num10 = rotation.Y * num2;
		float num11 = rotation.Y * num3;
		float num12 = rotation.Z * num3;
		return new Vector4(value.X * (1f - num10 - num12) + value.Y * (num8 - num6), value.X * (num8 + num6) + value.Y * (1f - num7 - num12), value.X * (num9 - num5) + value.Y * (num11 + num4), 1f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Transform(Vector3 position, Matrix4x4 matrix)
	{
		return new Vector4(position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31 + matrix.M41, position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32 + matrix.M42, position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33 + matrix.M43, position.X * matrix.M14 + position.Y * matrix.M24 + position.Z * matrix.M34 + matrix.M44);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Transform(Vector3 value, Quaternion rotation)
	{
		float num = rotation.X + rotation.X;
		float num2 = rotation.Y + rotation.Y;
		float num3 = rotation.Z + rotation.Z;
		float num4 = rotation.W * num;
		float num5 = rotation.W * num2;
		float num6 = rotation.W * num3;
		float num7 = rotation.X * num;
		float num8 = rotation.X * num2;
		float num9 = rotation.X * num3;
		float num10 = rotation.Y * num2;
		float num11 = rotation.Y * num3;
		float num12 = rotation.Z * num3;
		return new Vector4(value.X * (1f - num10 - num12) + value.Y * (num8 - num6) + value.Z * (num9 + num5), value.X * (num8 + num6) + value.Y * (1f - num7 - num12) + value.Z * (num11 - num4), value.X * (num9 - num5) + value.Y * (num11 + num4) + value.Z * (1f - num7 - num10), 1f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Transform(Vector4 vector, Matrix4x4 matrix)
	{
		return new Vector4(vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31 + vector.W * matrix.M41, vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32 + vector.W * matrix.M42, vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33 + vector.W * matrix.M43, vector.X * matrix.M14 + vector.Y * matrix.M24 + vector.Z * matrix.M34 + vector.W * matrix.M44);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector4 Transform(Vector4 value, Quaternion rotation)
	{
		float num = rotation.X + rotation.X;
		float num2 = rotation.Y + rotation.Y;
		float num3 = rotation.Z + rotation.Z;
		float num4 = rotation.W * num;
		float num5 = rotation.W * num2;
		float num6 = rotation.W * num3;
		float num7 = rotation.X * num;
		float num8 = rotation.X * num2;
		float num9 = rotation.X * num3;
		float num10 = rotation.Y * num2;
		float num11 = rotation.Y * num3;
		float num12 = rotation.Z * num3;
		return new Vector4(value.X * (1f - num10 - num12) + value.Y * (num8 - num6) + value.Z * (num9 + num5), value.X * (num8 + num6) + value.Y * (1f - num7 - num12) + value.Z * (num11 - num4), value.X * (num9 - num5) + value.Y * (num11 + num4) + value.Z * (1f - num7 - num10), value.W);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public readonly void CopyTo(float[] array)
	{
		CopyTo(array, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		if (array.Length - index < 4)
		{
			throw new ArgumentException(SR.Format(SR.Arg_ElementsInSourceIsGreaterThanDestination, index));
		}
		array[index] = X;
		array[index + 1] = Y;
		array[index + 2] = Z;
		array[index + 3] = W;
	}

	public readonly void CopyTo(Span<float> destination)
	{
		if (destination.Length < 4)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(destination)), this);
	}

	public readonly bool TryCopyTo(Span<float> destination)
	{
		if (destination.Length < 4)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(destination)), this);
		return true;
	}

	[Intrinsic]
	public readonly bool Equals(Vector4 other)
	{
		return this == other;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override readonly bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Vector4 other)
		{
			return Equals(other);
		}
		return false;
	}

	public override readonly int GetHashCode()
	{
		return HashCode.Combine(X, Y, Z, W);
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
		stringBuilder.Append(numberGroupSeparator);
		stringBuilder.Append(' ');
		stringBuilder.Append(Z.ToString(format, formatProvider));
		stringBuilder.Append(numberGroupSeparator);
		stringBuilder.Append(' ');
		stringBuilder.Append(W.ToString(format, formatProvider));
		stringBuilder.Append('>');
		return stringBuilder.ToString();
	}
}
