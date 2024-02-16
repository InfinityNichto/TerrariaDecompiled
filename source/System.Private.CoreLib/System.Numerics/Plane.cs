using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Numerics;

[Intrinsic]
public struct Plane : IEquatable<Plane>
{
	public Vector3 Normal;

	public float D;

	public Plane(float x, float y, float z, float d)
	{
		Normal = new Vector3(x, y, z);
		D = d;
	}

	public Plane(Vector3 normal, float d)
	{
		Normal = normal;
		D = d;
	}

	public Plane(Vector4 value)
	{
		Normal = new Vector3(value.X, value.Y, value.Z);
		D = value.W;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane CreateFromVertices(Vector3 point1, Vector3 point2, Vector3 point3)
	{
		if (Vector.IsHardwareAccelerated)
		{
			Vector3 vector = point2 - point1;
			Vector3 vector2 = point3 - point1;
			Vector3 value = Vector3.Cross(vector, vector2);
			Vector3 vector3 = Vector3.Normalize(value);
			float d = 0f - Vector3.Dot(vector3, point1);
			return new Plane(vector3, d);
		}
		float num = point2.X - point1.X;
		float num2 = point2.Y - point1.Y;
		float num3 = point2.Z - point1.Z;
		float num4 = point3.X - point1.X;
		float num5 = point3.Y - point1.Y;
		float num6 = point3.Z - point1.Z;
		float num7 = num2 * num6 - num3 * num5;
		float num8 = num3 * num4 - num * num6;
		float num9 = num * num5 - num2 * num4;
		float x = num7 * num7 + num8 * num8 + num9 * num9;
		float num10 = 1f / MathF.Sqrt(x);
		Vector3 normal = new Vector3(num7 * num10, num8 * num10, num9 * num10);
		return new Plane(normal, 0f - (normal.X * point1.X + normal.Y * point1.Y + normal.Z * point1.Z));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Dot(Plane plane, Vector4 value)
	{
		return plane.Normal.X * value.X + plane.Normal.Y * value.Y + plane.Normal.Z * value.Z + plane.D * value.W;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DotCoordinate(Plane plane, Vector3 value)
	{
		if (Vector.IsHardwareAccelerated)
		{
			return Vector3.Dot(plane.Normal, value) + plane.D;
		}
		return plane.Normal.X * value.X + plane.Normal.Y * value.Y + plane.Normal.Z * value.Z + plane.D;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float DotNormal(Plane plane, Vector3 value)
	{
		if (Vector.IsHardwareAccelerated)
		{
			return Vector3.Dot(plane.Normal, value);
		}
		return plane.Normal.X * value.X + plane.Normal.Y * value.Y + plane.Normal.Z * value.Z;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane Normalize(Plane value)
	{
		if (Vector.IsHardwareAccelerated)
		{
			float num = value.Normal.LengthSquared();
			if (MathF.Abs(num - 1f) < 1.1920929E-07f)
			{
				return value;
			}
			float num2 = MathF.Sqrt(num);
			return new Plane(value.Normal / num2, value.D / num2);
		}
		float num3 = value.Normal.X * value.Normal.X + value.Normal.Y * value.Normal.Y + value.Normal.Z * value.Normal.Z;
		if (MathF.Abs(num3 - 1f) < 1.1920929E-07f)
		{
			return value;
		}
		float num4 = 1f / MathF.Sqrt(num3);
		return new Plane(value.Normal.X * num4, value.Normal.Y * num4, value.Normal.Z * num4, value.D * num4);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane Transform(Plane plane, Matrix4x4 matrix)
	{
		Matrix4x4.Invert(matrix, out var result);
		float x = plane.Normal.X;
		float y = plane.Normal.Y;
		float z = plane.Normal.Z;
		float d = plane.D;
		return new Plane(x * result.M11 + y * result.M12 + z * result.M13 + d * result.M14, x * result.M21 + y * result.M22 + z * result.M23 + d * result.M24, x * result.M31 + y * result.M32 + z * result.M33 + d * result.M34, x * result.M41 + y * result.M42 + z * result.M43 + d * result.M44);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Plane Transform(Plane plane, Quaternion rotation)
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
		float num13 = 1f - num10 - num12;
		float num14 = num8 - num6;
		float num15 = num9 + num5;
		float num16 = num8 + num6;
		float num17 = 1f - num7 - num12;
		float num18 = num11 - num4;
		float num19 = num9 - num5;
		float num20 = num11 + num4;
		float num21 = 1f - num7 - num10;
		float x = plane.Normal.X;
		float y = plane.Normal.Y;
		float z = plane.Normal.Z;
		return new Plane(x * num13 + y * num14 + z * num15, x * num16 + y * num17 + z * num18, x * num19 + y * num20 + z * num21, plane.D);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(Plane value1, Plane value2)
	{
		if (value1.Normal.X == value2.Normal.X && value1.Normal.Y == value2.Normal.Y && value1.Normal.Z == value2.Normal.Z)
		{
			return value1.D == value2.D;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator !=(Plane value1, Plane value2)
	{
		return !(value1 == value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override readonly bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Plane other)
		{
			return Equals(other);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool Equals(Plane other)
	{
		if (Vector.IsHardwareAccelerated)
		{
			if (Normal.Equals(other.Normal))
			{
				return D == other.D;
			}
			return false;
		}
		if (Normal.X == other.Normal.X && Normal.Y == other.Normal.Y && Normal.Z == other.Normal.Z)
		{
			return D == other.D;
		}
		return false;
	}

	public override readonly int GetHashCode()
	{
		return Normal.GetHashCode() + D.GetHashCode();
	}

	public override readonly string ToString()
	{
		return $"{{Normal:{Normal} D:{D}}}";
	}
}
