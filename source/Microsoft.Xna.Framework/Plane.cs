using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Xna.Framework.Design;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(PlaneConverter))]
public struct Plane : IEquatable<Plane>
{
	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector3 Normal;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float D;

	public Plane(float a, float b, float c, float d)
	{
		Normal.X = a;
		Normal.Y = b;
		Normal.Z = c;
		D = d;
	}

	public Plane(Vector3 normal, float d)
	{
		Normal = normal;
		D = d;
	}

	public Plane(Vector4 value)
	{
		Normal.X = value.X;
		Normal.Y = value.Y;
		Normal.Z = value.Z;
		D = value.W;
	}

	public Plane(Vector3 point1, Vector3 point2, Vector3 point3)
	{
		float num = point2.X - point1.X;
		float num2 = point2.Y - point1.Y;
		float num3 = point2.Z - point1.Z;
		float num4 = point3.X - point1.X;
		float num5 = point3.Y - point1.Y;
		float num6 = point3.Z - point1.Z;
		float num7 = num2 * num6 - num3 * num5;
		float num8 = num3 * num4 - num * num6;
		float num9 = num * num5 - num2 * num4;
		float num10 = num7 * num7 + num8 * num8 + num9 * num9;
		float num11 = 1f / (float)Math.Sqrt(num10);
		Normal.X = num7 * num11;
		Normal.Y = num8 * num11;
		Normal.Z = num9 * num11;
		D = 0f - (Normal.X * point1.X + Normal.Y * point1.Y + Normal.Z * point1.Z);
	}

	public bool Equals(Plane other)
	{
		if (Normal.X == other.Normal.X && Normal.Y == other.Normal.Y && Normal.Z == other.Normal.Z)
		{
			return D == other.D;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		bool result = false;
		if (obj is Plane)
		{
			result = Equals((Plane)obj);
		}
		return result;
	}

	public override int GetHashCode()
	{
		return Normal.GetHashCode() + D.GetHashCode();
	}

	public override string ToString()
	{
		CultureInfo currentCulture = CultureInfo.CurrentCulture;
		return string.Format(currentCulture, "{{Normal:{0} D:{1}}}", new object[2]
		{
			Normal.ToString(),
			D.ToString(currentCulture)
		});
	}

	public void Normalize()
	{
		float num = Normal.X * Normal.X + Normal.Y * Normal.Y + Normal.Z * Normal.Z;
		if (!(Math.Abs(num - 1f) < 1.1920929E-07f))
		{
			float num2 = 1f / (float)Math.Sqrt(num);
			Normal.X *= num2;
			Normal.Y *= num2;
			Normal.Z *= num2;
			D *= num2;
		}
	}

	public static Plane Normalize(Plane value)
	{
		float num = value.Normal.X * value.Normal.X + value.Normal.Y * value.Normal.Y + value.Normal.Z * value.Normal.Z;
		Plane result = default(Plane);
		if (Math.Abs(num - 1f) < 1.1920929E-07f)
		{
			result.Normal = value.Normal;
			result.D = value.D;
			return result;
		}
		float num2 = 1f / (float)Math.Sqrt(num);
		result.Normal.X = value.Normal.X * num2;
		result.Normal.Y = value.Normal.Y * num2;
		result.Normal.Z = value.Normal.Z * num2;
		result.D = value.D * num2;
		return result;
	}

	public static void Normalize(ref Plane value, out Plane result)
	{
		float num = value.Normal.X * value.Normal.X + value.Normal.Y * value.Normal.Y + value.Normal.Z * value.Normal.Z;
		if (Math.Abs(num - 1f) < 1.1920929E-07f)
		{
			result.Normal = value.Normal;
			result.D = value.D;
			return;
		}
		float num2 = 1f / (float)Math.Sqrt(num);
		result.Normal.X = value.Normal.X * num2;
		result.Normal.Y = value.Normal.Y * num2;
		result.Normal.Z = value.Normal.Z * num2;
		result.D = value.D * num2;
	}

	public static Plane Transform(Plane plane, Matrix matrix)
	{
		Matrix.Invert(ref matrix, out var result);
		float x = plane.Normal.X;
		float y = plane.Normal.Y;
		float z = plane.Normal.Z;
		float d = plane.D;
		Plane result2 = default(Plane);
		result2.Normal.X = x * result.M11 + y * result.M12 + z * result.M13 + d * result.M14;
		result2.Normal.Y = x * result.M21 + y * result.M22 + z * result.M23 + d * result.M24;
		result2.Normal.Z = x * result.M31 + y * result.M32 + z * result.M33 + d * result.M34;
		result2.D = x * result.M41 + y * result.M42 + z * result.M43 + d * result.M44;
		return result2;
	}

	public static void Transform(ref Plane plane, ref Matrix matrix, out Plane result)
	{
		Matrix.Invert(ref matrix, out var result2);
		float x = plane.Normal.X;
		float y = plane.Normal.Y;
		float z = plane.Normal.Z;
		float d = plane.D;
		result.Normal.X = x * result2.M11 + y * result2.M12 + z * result2.M13 + d * result2.M14;
		result.Normal.Y = x * result2.M21 + y * result2.M22 + z * result2.M23 + d * result2.M24;
		result.Normal.Z = x * result2.M31 + y * result2.M32 + z * result2.M33 + d * result2.M34;
		result.D = x * result2.M41 + y * result2.M42 + z * result2.M43 + d * result2.M44;
	}

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
		Plane result = default(Plane);
		result.Normal.X = x * num13 + y * num14 + z * num15;
		result.Normal.Y = x * num16 + y * num17 + z * num18;
		result.Normal.Z = x * num19 + y * num20 + z * num21;
		result.D = plane.D;
		return result;
	}

	public static void Transform(ref Plane plane, ref Quaternion rotation, out Plane result)
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
		result.Normal.X = x * num13 + y * num14 + z * num15;
		result.Normal.Y = x * num16 + y * num17 + z * num18;
		result.Normal.Z = x * num19 + y * num20 + z * num21;
		result.D = plane.D;
	}

	public float Dot(Vector4 value)
	{
		return Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z + D * value.W;
	}

	public void Dot(ref Vector4 value, out float result)
	{
		result = Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z + D * value.W;
	}

	public float DotCoordinate(Vector3 value)
	{
		return Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z + D;
	}

	public void DotCoordinate(ref Vector3 value, out float result)
	{
		result = Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z + D;
	}

	public float DotNormal(Vector3 value)
	{
		return Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z;
	}

	public void DotNormal(ref Vector3 value, out float result)
	{
		result = Normal.X * value.X + Normal.Y * value.Y + Normal.Z * value.Z;
	}

	public PlaneIntersectionType Intersects(BoundingBox box)
	{
		Vector3 vector = default(Vector3);
		vector.X = ((Normal.X >= 0f) ? box.Min.X : box.Max.X);
		vector.Y = ((Normal.Y >= 0f) ? box.Min.Y : box.Max.Y);
		vector.Z = ((Normal.Z >= 0f) ? box.Min.Z : box.Max.Z);
		Vector3 vector2 = default(Vector3);
		vector2.X = ((Normal.X >= 0f) ? box.Max.X : box.Min.X);
		vector2.Y = ((Normal.Y >= 0f) ? box.Max.Y : box.Min.Y);
		vector2.Z = ((Normal.Z >= 0f) ? box.Max.Z : box.Min.Z);
		float num = Normal.X * vector.X + Normal.Y * vector.Y + Normal.Z * vector.Z;
		if (num + D > 0f)
		{
			return PlaneIntersectionType.Front;
		}
		num = Normal.X * vector2.X + Normal.Y * vector2.Y + Normal.Z * vector2.Z;
		if (num + D < 0f)
		{
			return PlaneIntersectionType.Back;
		}
		return PlaneIntersectionType.Intersecting;
	}

	public void Intersects(ref BoundingBox box, out PlaneIntersectionType result)
	{
		Vector3 vector = default(Vector3);
		vector.X = ((Normal.X >= 0f) ? box.Min.X : box.Max.X);
		vector.Y = ((Normal.Y >= 0f) ? box.Min.Y : box.Max.Y);
		vector.Z = ((Normal.Z >= 0f) ? box.Min.Z : box.Max.Z);
		Vector3 vector2 = default(Vector3);
		vector2.X = ((Normal.X >= 0f) ? box.Max.X : box.Min.X);
		vector2.Y = ((Normal.Y >= 0f) ? box.Max.Y : box.Min.Y);
		vector2.Z = ((Normal.Z >= 0f) ? box.Max.Z : box.Min.Z);
		float num = Normal.X * vector.X + Normal.Y * vector.Y + Normal.Z * vector.Z;
		if (num + D > 0f)
		{
			result = PlaneIntersectionType.Front;
			return;
		}
		num = Normal.X * vector2.X + Normal.Y * vector2.Y + Normal.Z * vector2.Z;
		if (num + D < 0f)
		{
			result = PlaneIntersectionType.Back;
		}
		else
		{
			result = PlaneIntersectionType.Intersecting;
		}
	}

	public PlaneIntersectionType Intersects(BoundingFrustum frustum)
	{
		if (null == frustum)
		{
			throw new ArgumentNullException("frustum", FrameworkResources.NullNotAllowed);
		}
		return frustum.Intersects(this);
	}

	public PlaneIntersectionType Intersects(BoundingSphere sphere)
	{
		float num = sphere.Center.X * Normal.X + sphere.Center.Y * Normal.Y + sphere.Center.Z * Normal.Z;
		float num2 = num + D;
		if (num2 > sphere.Radius)
		{
			return PlaneIntersectionType.Front;
		}
		if (num2 < 0f - sphere.Radius)
		{
			return PlaneIntersectionType.Back;
		}
		return PlaneIntersectionType.Intersecting;
	}

	public void Intersects(ref BoundingSphere sphere, out PlaneIntersectionType result)
	{
		float num = sphere.Center.X * Normal.X + sphere.Center.Y * Normal.Y + sphere.Center.Z * Normal.Z;
		float num2 = num + D;
		if (num2 > sphere.Radius)
		{
			result = PlaneIntersectionType.Front;
		}
		else if (num2 < 0f - sphere.Radius)
		{
			result = PlaneIntersectionType.Back;
		}
		else
		{
			result = PlaneIntersectionType.Intersecting;
		}
	}

	public static bool operator ==(Plane lhs, Plane rhs)
	{
		return lhs.Equals(rhs);
	}

	public static bool operator !=(Plane lhs, Plane rhs)
	{
		if (lhs.Normal.X == rhs.Normal.X && lhs.Normal.Y == rhs.Normal.Y && lhs.Normal.Z == rhs.Normal.Z)
		{
			return lhs.D != rhs.D;
		}
		return true;
	}
}
