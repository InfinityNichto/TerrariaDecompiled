using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Xna.Framework.Design;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(BoundingSphereConverter))]
public struct BoundingSphere : IEquatable<BoundingSphere>
{
	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector3 Center;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float Radius;

	public BoundingSphere(Vector3 center, float radius)
	{
		if (radius < 0f)
		{
			throw new ArgumentException(FrameworkResources.NegativeRadius);
		}
		Center = center;
		Radius = radius;
	}

	public bool Equals(BoundingSphere other)
	{
		if (Center == other.Center)
		{
			return Radius == other.Radius;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		bool result = false;
		if (obj is BoundingSphere)
		{
			result = Equals((BoundingSphere)obj);
		}
		return result;
	}

	public override int GetHashCode()
	{
		return Center.GetHashCode() + Radius.GetHashCode();
	}

	public override string ToString()
	{
		CultureInfo currentCulture = CultureInfo.CurrentCulture;
		return string.Format(currentCulture, "{{Center:{0} Radius:{1}}}", new object[2]
		{
			Center.ToString(),
			Radius.ToString(currentCulture)
		});
	}

	public static BoundingSphere CreateMerged(BoundingSphere original, BoundingSphere additional)
	{
		Vector3.Subtract(ref additional.Center, ref original.Center, out var result);
		float num = result.Length();
		float radius = original.Radius;
		float radius2 = additional.Radius;
		if (radius + radius2 >= num)
		{
			if (radius - radius2 >= num)
			{
				return original;
			}
			if (radius2 - radius >= num)
			{
				return additional;
			}
		}
		Vector3 vector = result * (1f / num);
		float num2 = MathHelper.Min(0f - radius, num - radius2);
		float num3 = MathHelper.Max(radius, num + radius2);
		float num4 = (num3 - num2) * 0.5f;
		BoundingSphere result2 = default(BoundingSphere);
		result2.Center = original.Center + vector * (num4 + num2);
		result2.Radius = num4;
		return result2;
	}

	public static void CreateMerged(ref BoundingSphere original, ref BoundingSphere additional, out BoundingSphere result)
	{
		Vector3.Subtract(ref additional.Center, ref original.Center, out var result2);
		float num = result2.Length();
		float radius = original.Radius;
		float radius2 = additional.Radius;
		if (radius + radius2 >= num)
		{
			if (radius - radius2 >= num)
			{
				result = original;
				return;
			}
			if (radius2 - radius >= num)
			{
				result = additional;
				return;
			}
		}
		Vector3 vector = result2 * (1f / num);
		float num2 = MathHelper.Min(0f - radius, num - radius2);
		float num3 = MathHelper.Max(radius, num + radius2);
		float num4 = (num3 - num2) * 0.5f;
		result.Center = original.Center + vector * (num4 + num2);
		result.Radius = num4;
	}

	public static BoundingSphere CreateFromBoundingBox(BoundingBox box)
	{
		BoundingSphere result = default(BoundingSphere);
		Vector3.Lerp(ref box.Min, ref box.Max, 0.5f, out result.Center);
		Vector3.Distance(ref box.Min, ref box.Max, out var result2);
		result.Radius = result2 * 0.5f;
		return result;
	}

	public static void CreateFromBoundingBox(ref BoundingBox box, out BoundingSphere result)
	{
		Vector3.Lerp(ref box.Min, ref box.Max, 0.5f, out result.Center);
		Vector3.Distance(ref box.Min, ref box.Max, out var result2);
		result.Radius = result2 * 0.5f;
	}

	public static BoundingSphere CreateFromPoints(IEnumerable<Vector3> points)
	{
		if (points == null)
		{
			throw new ArgumentNullException("points");
		}
		IEnumerator<Vector3> enumerator = points.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			throw new ArgumentException(FrameworkResources.BoundingSphereZeroPoints);
		}
		Vector3 value5;
		Vector3 value4;
		Vector3 value3;
		Vector3 value2;
		Vector3 value;
		Vector3 value6 = (value5 = (value4 = (value3 = (value2 = (value = enumerator.Current)))));
		foreach (Vector3 point in points)
		{
			if (point.X < value6.X)
			{
				value6 = point;
			}
			if (point.X > value5.X)
			{
				value5 = point;
			}
			if (point.Y < value4.Y)
			{
				value4 = point;
			}
			if (point.Y > value3.Y)
			{
				value3 = point;
			}
			if (point.Z < value2.Z)
			{
				value2 = point;
			}
			if (point.Z > value.Z)
			{
				value = point;
			}
		}
		Vector3.Distance(ref value5, ref value6, out var result);
		Vector3.Distance(ref value3, ref value4, out var result2);
		Vector3.Distance(ref value, ref value2, out var result3);
		Vector3 result4;
		float num;
		if (result > result2)
		{
			if (result > result3)
			{
				Vector3.Lerp(ref value5, ref value6, 0.5f, out result4);
				num = result * 0.5f;
			}
			else
			{
				Vector3.Lerp(ref value, ref value2, 0.5f, out result4);
				num = result3 * 0.5f;
			}
		}
		else if (result2 > result3)
		{
			Vector3.Lerp(ref value3, ref value4, 0.5f, out result4);
			num = result2 * 0.5f;
		}
		else
		{
			Vector3.Lerp(ref value, ref value2, 0.5f, out result4);
			num = result3 * 0.5f;
		}
		Vector3 vector = default(Vector3);
		foreach (Vector3 point2 in points)
		{
			vector.X = point2.X - result4.X;
			vector.Y = point2.Y - result4.Y;
			vector.Z = point2.Z - result4.Z;
			float num2 = vector.Length();
			if (num2 > num)
			{
				num = (num + num2) * 0.5f;
				result4 += (1f - num / num2) * vector;
			}
		}
		BoundingSphere result5 = default(BoundingSphere);
		result5.Center = result4;
		result5.Radius = num;
		return result5;
	}

	public static BoundingSphere CreateFromFrustum(BoundingFrustum frustum)
	{
		if (frustum == null)
		{
			throw new ArgumentNullException("frustum");
		}
		return CreateFromPoints(frustum.cornerArray);
	}

	public bool Intersects(BoundingBox box)
	{
		Vector3.Clamp(ref Center, ref box.Min, ref box.Max, out var result);
		Vector3.DistanceSquared(ref Center, ref result, out var result2);
		if (!(result2 > Radius * Radius))
		{
			return true;
		}
		return false;
	}

	public void Intersects(ref BoundingBox box, out bool result)
	{
		Vector3.Clamp(ref Center, ref box.Min, ref box.Max, out var result2);
		Vector3.DistanceSquared(ref Center, ref result2, out var result3);
		result = !(result3 > Radius * Radius);
	}

	public bool Intersects(BoundingFrustum frustum)
	{
		if (null == frustum)
		{
			throw new ArgumentNullException("frustum", FrameworkResources.NullNotAllowed);
		}
		frustum.Intersects(ref this, out var result);
		return result;
	}

	public PlaneIntersectionType Intersects(Plane plane)
	{
		return plane.Intersects(this);
	}

	public void Intersects(ref Plane plane, out PlaneIntersectionType result)
	{
		plane.Intersects(ref this, out result);
	}

	public float? Intersects(Ray ray)
	{
		return ray.Intersects(this);
	}

	public void Intersects(ref Ray ray, out float? result)
	{
		ray.Intersects(ref this, out result);
	}

	public bool Intersects(BoundingSphere sphere)
	{
		Vector3.DistanceSquared(ref Center, ref sphere.Center, out var result);
		float radius = Radius;
		float radius2 = sphere.Radius;
		if (!(radius * radius + 2f * radius * radius2 + radius2 * radius2 > result))
		{
			return false;
		}
		return true;
	}

	public void Intersects(ref BoundingSphere sphere, out bool result)
	{
		Vector3.DistanceSquared(ref Center, ref sphere.Center, out var result2);
		float radius = Radius;
		float radius2 = sphere.Radius;
		result = ((radius * radius + 2f * radius * radius2 + radius2 * radius2 > result2) ? true : false);
	}

	public ContainmentType Contains(BoundingBox box)
	{
		if (!box.Intersects(this))
		{
			return ContainmentType.Disjoint;
		}
		float num = Radius * Radius;
		Vector3 vector = default(Vector3);
		vector.X = Center.X - box.Min.X;
		vector.Y = Center.Y - box.Max.Y;
		vector.Z = Center.Z - box.Max.Z;
		if (vector.LengthSquared() > num)
		{
			return ContainmentType.Intersects;
		}
		vector.X = Center.X - box.Max.X;
		vector.Y = Center.Y - box.Max.Y;
		vector.Z = Center.Z - box.Max.Z;
		if (vector.LengthSquared() > num)
		{
			return ContainmentType.Intersects;
		}
		vector.X = Center.X - box.Max.X;
		vector.Y = Center.Y - box.Min.Y;
		vector.Z = Center.Z - box.Max.Z;
		if (vector.LengthSquared() > num)
		{
			return ContainmentType.Intersects;
		}
		vector.X = Center.X - box.Min.X;
		vector.Y = Center.Y - box.Min.Y;
		vector.Z = Center.Z - box.Max.Z;
		if (vector.LengthSquared() > num)
		{
			return ContainmentType.Intersects;
		}
		vector.X = Center.X - box.Min.X;
		vector.Y = Center.Y - box.Max.Y;
		vector.Z = Center.Z - box.Min.Z;
		if (vector.LengthSquared() > num)
		{
			return ContainmentType.Intersects;
		}
		vector.X = Center.X - box.Max.X;
		vector.Y = Center.Y - box.Max.Y;
		vector.Z = Center.Z - box.Min.Z;
		if (vector.LengthSquared() > num)
		{
			return ContainmentType.Intersects;
		}
		vector.X = Center.X - box.Max.X;
		vector.Y = Center.Y - box.Min.Y;
		vector.Z = Center.Z - box.Min.Z;
		if (vector.LengthSquared() > num)
		{
			return ContainmentType.Intersects;
		}
		vector.X = Center.X - box.Min.X;
		vector.Y = Center.Y - box.Min.Y;
		vector.Z = Center.Z - box.Min.Z;
		if (vector.LengthSquared() > num)
		{
			return ContainmentType.Intersects;
		}
		return ContainmentType.Contains;
	}

	public void Contains(ref BoundingBox box, out ContainmentType result)
	{
		box.Intersects(ref this, out var result2);
		if (!result2)
		{
			result = ContainmentType.Disjoint;
			return;
		}
		float num = Radius * Radius;
		result = ContainmentType.Intersects;
		Vector3 vector = default(Vector3);
		vector.X = Center.X - box.Min.X;
		vector.Y = Center.Y - box.Max.Y;
		vector.Z = Center.Z - box.Max.Z;
		if (vector.LengthSquared() > num)
		{
			return;
		}
		vector.X = Center.X - box.Max.X;
		vector.Y = Center.Y - box.Max.Y;
		vector.Z = Center.Z - box.Max.Z;
		if (vector.LengthSquared() > num)
		{
			return;
		}
		vector.X = Center.X - box.Max.X;
		vector.Y = Center.Y - box.Min.Y;
		vector.Z = Center.Z - box.Max.Z;
		if (vector.LengthSquared() > num)
		{
			return;
		}
		vector.X = Center.X - box.Min.X;
		vector.Y = Center.Y - box.Min.Y;
		vector.Z = Center.Z - box.Max.Z;
		if (vector.LengthSquared() > num)
		{
			return;
		}
		vector.X = Center.X - box.Min.X;
		vector.Y = Center.Y - box.Max.Y;
		vector.Z = Center.Z - box.Min.Z;
		if (vector.LengthSquared() > num)
		{
			return;
		}
		vector.X = Center.X - box.Max.X;
		vector.Y = Center.Y - box.Max.Y;
		vector.Z = Center.Z - box.Min.Z;
		if (vector.LengthSquared() > num)
		{
			return;
		}
		vector.X = Center.X - box.Max.X;
		vector.Y = Center.Y - box.Min.Y;
		vector.Z = Center.Z - box.Min.Z;
		if (!(vector.LengthSquared() > num))
		{
			vector.X = Center.X - box.Min.X;
			vector.Y = Center.Y - box.Min.Y;
			vector.Z = Center.Z - box.Min.Z;
			if (!(vector.LengthSquared() > num))
			{
				result = ContainmentType.Contains;
			}
		}
	}

	public ContainmentType Contains(BoundingFrustum frustum)
	{
		if (null == frustum)
		{
			throw new ArgumentNullException("frustum", FrameworkResources.NullNotAllowed);
		}
		if (!frustum.Intersects(this))
		{
			return ContainmentType.Disjoint;
		}
		float num = Radius * Radius;
		Vector3[] cornerArray = frustum.cornerArray;
		Vector3 vector2 = default(Vector3);
		for (int i = 0; i < cornerArray.Length; i++)
		{
			Vector3 vector = cornerArray[i];
			vector2.X = vector.X - Center.X;
			vector2.Y = vector.Y - Center.Y;
			vector2.Z = vector.Z - Center.Z;
			if (vector2.LengthSquared() > num)
			{
				return ContainmentType.Intersects;
			}
		}
		return ContainmentType.Contains;
	}

	public ContainmentType Contains(Vector3 point)
	{
		float num = Vector3.DistanceSquared(point, Center);
		if (!(num < Radius * Radius))
		{
			return ContainmentType.Disjoint;
		}
		return ContainmentType.Contains;
	}

	public void Contains(ref Vector3 point, out ContainmentType result)
	{
		Vector3.DistanceSquared(ref point, ref Center, out var result2);
		result = ((result2 < Radius * Radius) ? ContainmentType.Contains : ContainmentType.Disjoint);
	}

	public ContainmentType Contains(BoundingSphere sphere)
	{
		Vector3.Distance(ref Center, ref sphere.Center, out var result);
		float radius = Radius;
		float radius2 = sphere.Radius;
		if (!(radius + radius2 >= result))
		{
			return ContainmentType.Disjoint;
		}
		if (!(radius - radius2 >= result))
		{
			return ContainmentType.Intersects;
		}
		return ContainmentType.Contains;
	}

	public void Contains(ref BoundingSphere sphere, out ContainmentType result)
	{
		Vector3.Distance(ref Center, ref sphere.Center, out var result2);
		float radius = Radius;
		float radius2 = sphere.Radius;
		result = ((radius + radius2 >= result2) ? ((radius - radius2 >= result2) ? ContainmentType.Contains : ContainmentType.Intersects) : ContainmentType.Disjoint);
	}

	internal void SupportMapping(ref Vector3 v, out Vector3 result)
	{
		float num = v.Length();
		float num2 = Radius / num;
		result.X = Center.X + v.X * num2;
		result.Y = Center.Y + v.Y * num2;
		result.Z = Center.Z + v.Z * num2;
	}

	public BoundingSphere Transform(Matrix matrix)
	{
		BoundingSphere result = default(BoundingSphere);
		result.Center = Vector3.Transform(Center, matrix);
		float val = matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12 + matrix.M13 * matrix.M13;
		float val2 = matrix.M21 * matrix.M21 + matrix.M22 * matrix.M22 + matrix.M23 * matrix.M23;
		float val3 = matrix.M31 * matrix.M31 + matrix.M32 * matrix.M32 + matrix.M33 * matrix.M33;
		float num = Math.Max(val, Math.Max(val2, val3));
		result.Radius = Radius * (float)Math.Sqrt(num);
		return result;
	}

	public void Transform(ref Matrix matrix, out BoundingSphere result)
	{
		result.Center = Vector3.Transform(Center, matrix);
		float val = matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12 + matrix.M13 * matrix.M13;
		float val2 = matrix.M21 * matrix.M21 + matrix.M22 * matrix.M22 + matrix.M23 * matrix.M23;
		float val3 = matrix.M31 * matrix.M31 + matrix.M32 * matrix.M32 + matrix.M33 * matrix.M33;
		float num = Math.Max(val, Math.Max(val2, val3));
		result.Radius = Radius * (float)Math.Sqrt(num);
	}

	public static bool operator ==(BoundingSphere a, BoundingSphere b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(BoundingSphere a, BoundingSphere b)
	{
		if (!(a.Center != b.Center))
		{
			return a.Radius != b.Radius;
		}
		return true;
	}
}
