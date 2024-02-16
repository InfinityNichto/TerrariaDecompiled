using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Xna.Framework.Design;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(BoundingBoxConverter))]
public struct BoundingBox : IEquatable<BoundingBox>
{
	public const int CornerCount = 8;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector3 Min;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector3 Max;

	public Vector3[] GetCorners()
	{
		return new Vector3[8]
		{
			new Vector3(Min.X, Max.Y, Max.Z),
			new Vector3(Max.X, Max.Y, Max.Z),
			new Vector3(Max.X, Min.Y, Max.Z),
			new Vector3(Min.X, Min.Y, Max.Z),
			new Vector3(Min.X, Max.Y, Min.Z),
			new Vector3(Max.X, Max.Y, Min.Z),
			new Vector3(Max.X, Min.Y, Min.Z),
			new Vector3(Min.X, Min.Y, Min.Z)
		};
	}

	public void GetCorners(Vector3[] corners)
	{
		if (corners == null)
		{
			throw new ArgumentNullException("corners");
		}
		if (corners.Length < 8)
		{
			throw new ArgumentOutOfRangeException("corners", FrameworkResources.NotEnoughCorners);
		}
		corners[0].X = Min.X;
		corners[0].Y = Max.Y;
		corners[0].Z = Max.Z;
		corners[1].X = Max.X;
		corners[1].Y = Max.Y;
		corners[1].Z = Max.Z;
		corners[2].X = Max.X;
		corners[2].Y = Min.Y;
		corners[2].Z = Max.Z;
		corners[3].X = Min.X;
		corners[3].Y = Min.Y;
		corners[3].Z = Max.Z;
		corners[4].X = Min.X;
		corners[4].Y = Max.Y;
		corners[4].Z = Min.Z;
		corners[5].X = Max.X;
		corners[5].Y = Max.Y;
		corners[5].Z = Min.Z;
		corners[6].X = Max.X;
		corners[6].Y = Min.Y;
		corners[6].Z = Min.Z;
		corners[7].X = Min.X;
		corners[7].Y = Min.Y;
		corners[7].Z = Min.Z;
	}

	public BoundingBox(Vector3 min, Vector3 max)
	{
		Min = min;
		Max = max;
	}

	public bool Equals(BoundingBox other)
	{
		if (Min == other.Min)
		{
			return Max == other.Max;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		bool result = false;
		if (obj is BoundingBox)
		{
			result = Equals((BoundingBox)obj);
		}
		return result;
	}

	public override int GetHashCode()
	{
		return Min.GetHashCode() + Max.GetHashCode();
	}

	public override string ToString()
	{
		CultureInfo currentCulture = CultureInfo.CurrentCulture;
		return string.Format(currentCulture, "{{Min:{0} Max:{1}}}", new object[2]
		{
			Min.ToString(),
			Max.ToString()
		});
	}

	public static BoundingBox CreateMerged(BoundingBox original, BoundingBox additional)
	{
		BoundingBox result = default(BoundingBox);
		Vector3.Min(ref original.Min, ref additional.Min, out result.Min);
		Vector3.Max(ref original.Max, ref additional.Max, out result.Max);
		return result;
	}

	public static void CreateMerged(ref BoundingBox original, ref BoundingBox additional, out BoundingBox result)
	{
		Vector3.Min(ref original.Min, ref additional.Min, out var result2);
		Vector3.Max(ref original.Max, ref additional.Max, out var result3);
		result.Min = result2;
		result.Max = result3;
	}

	public static BoundingBox CreateFromSphere(BoundingSphere sphere)
	{
		BoundingBox result = default(BoundingBox);
		result.Min.X = sphere.Center.X - sphere.Radius;
		result.Min.Y = sphere.Center.Y - sphere.Radius;
		result.Min.Z = sphere.Center.Z - sphere.Radius;
		result.Max.X = sphere.Center.X + sphere.Radius;
		result.Max.Y = sphere.Center.Y + sphere.Radius;
		result.Max.Z = sphere.Center.Z + sphere.Radius;
		return result;
	}

	public static void CreateFromSphere(ref BoundingSphere sphere, out BoundingBox result)
	{
		result.Min.X = sphere.Center.X - sphere.Radius;
		result.Min.Y = sphere.Center.Y - sphere.Radius;
		result.Min.Z = sphere.Center.Z - sphere.Radius;
		result.Max.X = sphere.Center.X + sphere.Radius;
		result.Max.Y = sphere.Center.Y + sphere.Radius;
		result.Max.Z = sphere.Center.Z + sphere.Radius;
	}

	public static BoundingBox CreateFromPoints(IEnumerable<Vector3> points)
	{
		if (points == null)
		{
			throw new ArgumentNullException();
		}
		bool flag = false;
		Vector3 value = new Vector3(float.MaxValue);
		Vector3 value2 = new Vector3(float.MinValue);
		foreach (Vector3 point in points)
		{
			Vector3 value3 = point;
			Vector3.Min(ref value, ref value3, out value);
			Vector3.Max(ref value2, ref value3, out value2);
			flag = true;
		}
		if (!flag)
		{
			throw new ArgumentException(FrameworkResources.BoundingBoxZeroPoints);
		}
		return new BoundingBox(value, value2);
	}

	public bool Intersects(BoundingBox box)
	{
		if (Max.X < box.Min.X || Min.X > box.Max.X)
		{
			return false;
		}
		if (Max.Y < box.Min.Y || Min.Y > box.Max.Y)
		{
			return false;
		}
		if (Max.Z < box.Min.Z || Min.Z > box.Max.Z)
		{
			return false;
		}
		return true;
	}

	public void Intersects(ref BoundingBox box, out bool result)
	{
		result = false;
		if (!(Max.X < box.Min.X) && !(Min.X > box.Max.X) && !(Max.Y < box.Min.Y) && !(Min.Y > box.Max.Y) && !(Max.Z < box.Min.Z) && !(Min.Z > box.Max.Z))
		{
			result = true;
		}
	}

	public bool Intersects(BoundingFrustum frustum)
	{
		if (null == frustum)
		{
			throw new ArgumentNullException("frustum", FrameworkResources.NullNotAllowed);
		}
		return frustum.Intersects(this);
	}

	public PlaneIntersectionType Intersects(Plane plane)
	{
		Vector3 vector = default(Vector3);
		vector.X = ((plane.Normal.X >= 0f) ? Min.X : Max.X);
		vector.Y = ((plane.Normal.Y >= 0f) ? Min.Y : Max.Y);
		vector.Z = ((plane.Normal.Z >= 0f) ? Min.Z : Max.Z);
		Vector3 vector2 = default(Vector3);
		vector2.X = ((plane.Normal.X >= 0f) ? Max.X : Min.X);
		vector2.Y = ((plane.Normal.Y >= 0f) ? Max.Y : Min.Y);
		vector2.Z = ((plane.Normal.Z >= 0f) ? Max.Z : Min.Z);
		float num = plane.Normal.X * vector.X + plane.Normal.Y * vector.Y + plane.Normal.Z * vector.Z;
		if (num + plane.D > 0f)
		{
			return PlaneIntersectionType.Front;
		}
		num = plane.Normal.X * vector2.X + plane.Normal.Y * vector2.Y + plane.Normal.Z * vector2.Z;
		if (num + plane.D < 0f)
		{
			return PlaneIntersectionType.Back;
		}
		return PlaneIntersectionType.Intersecting;
	}

	public void Intersects(ref Plane plane, out PlaneIntersectionType result)
	{
		Vector3 vector = default(Vector3);
		vector.X = ((plane.Normal.X >= 0f) ? Min.X : Max.X);
		vector.Y = ((plane.Normal.Y >= 0f) ? Min.Y : Max.Y);
		vector.Z = ((plane.Normal.Z >= 0f) ? Min.Z : Max.Z);
		Vector3 vector2 = default(Vector3);
		vector2.X = ((plane.Normal.X >= 0f) ? Max.X : Min.X);
		vector2.Y = ((plane.Normal.Y >= 0f) ? Max.Y : Min.Y);
		vector2.Z = ((plane.Normal.Z >= 0f) ? Max.Z : Min.Z);
		float num = plane.Normal.X * vector.X + plane.Normal.Y * vector.Y + plane.Normal.Z * vector.Z;
		if (num + plane.D > 0f)
		{
			result = PlaneIntersectionType.Front;
			return;
		}
		num = plane.Normal.X * vector2.X + plane.Normal.Y * vector2.Y + plane.Normal.Z * vector2.Z;
		if (num + plane.D < 0f)
		{
			result = PlaneIntersectionType.Back;
		}
		else
		{
			result = PlaneIntersectionType.Intersecting;
		}
	}

	public float? Intersects(Ray ray)
	{
		float num = 0f;
		float num2 = float.MaxValue;
		if (Math.Abs(ray.Direction.X) < 1E-06f)
		{
			if (ray.Position.X < Min.X || ray.Position.X > Max.X)
			{
				return null;
			}
		}
		else
		{
			float num3 = 1f / ray.Direction.X;
			float num4 = (Min.X - ray.Position.X) * num3;
			float num5 = (Max.X - ray.Position.X) * num3;
			if (num4 > num5)
			{
				float num6 = num4;
				num4 = num5;
				num5 = num6;
			}
			num = MathHelper.Max(num4, num);
			num2 = MathHelper.Min(num5, num2);
			if (num > num2)
			{
				return null;
			}
		}
		if (Math.Abs(ray.Direction.Y) < 1E-06f)
		{
			if (ray.Position.Y < Min.Y || ray.Position.Y > Max.Y)
			{
				return null;
			}
		}
		else
		{
			float num7 = 1f / ray.Direction.Y;
			float num8 = (Min.Y - ray.Position.Y) * num7;
			float num9 = (Max.Y - ray.Position.Y) * num7;
			if (num8 > num9)
			{
				float num10 = num8;
				num8 = num9;
				num9 = num10;
			}
			num = MathHelper.Max(num8, num);
			num2 = MathHelper.Min(num9, num2);
			if (num > num2)
			{
				return null;
			}
		}
		if (Math.Abs(ray.Direction.Z) < 1E-06f)
		{
			if (ray.Position.Z < Min.Z || ray.Position.Z > Max.Z)
			{
				return null;
			}
		}
		else
		{
			float num11 = 1f / ray.Direction.Z;
			float num12 = (Min.Z - ray.Position.Z) * num11;
			float num13 = (Max.Z - ray.Position.Z) * num11;
			if (num12 > num13)
			{
				float num14 = num12;
				num12 = num13;
				num13 = num14;
			}
			num = MathHelper.Max(num12, num);
			num2 = MathHelper.Min(num13, num2);
			if (num > num2)
			{
				return null;
			}
		}
		return num;
	}

	public void Intersects(ref Ray ray, out float? result)
	{
		result = null;
		float num = 0f;
		float num2 = float.MaxValue;
		if (Math.Abs(ray.Direction.X) < 1E-06f)
		{
			if (ray.Position.X < Min.X || ray.Position.X > Max.X)
			{
				return;
			}
		}
		else
		{
			float num3 = 1f / ray.Direction.X;
			float num4 = (Min.X - ray.Position.X) * num3;
			float num5 = (Max.X - ray.Position.X) * num3;
			if (num4 > num5)
			{
				float num6 = num4;
				num4 = num5;
				num5 = num6;
			}
			num = MathHelper.Max(num4, num);
			num2 = MathHelper.Min(num5, num2);
			if (num > num2)
			{
				return;
			}
		}
		if (Math.Abs(ray.Direction.Y) < 1E-06f)
		{
			if (ray.Position.Y < Min.Y || ray.Position.Y > Max.Y)
			{
				return;
			}
		}
		else
		{
			float num7 = 1f / ray.Direction.Y;
			float num8 = (Min.Y - ray.Position.Y) * num7;
			float num9 = (Max.Y - ray.Position.Y) * num7;
			if (num8 > num9)
			{
				float num10 = num8;
				num8 = num9;
				num9 = num10;
			}
			num = MathHelper.Max(num8, num);
			num2 = MathHelper.Min(num9, num2);
			if (num > num2)
			{
				return;
			}
		}
		if (Math.Abs(ray.Direction.Z) < 1E-06f)
		{
			if (ray.Position.Z < Min.Z || ray.Position.Z > Max.Z)
			{
				return;
			}
		}
		else
		{
			float num11 = 1f / ray.Direction.Z;
			float num12 = (Min.Z - ray.Position.Z) * num11;
			float num13 = (Max.Z - ray.Position.Z) * num11;
			if (num12 > num13)
			{
				float num14 = num12;
				num12 = num13;
				num13 = num14;
			}
			num = MathHelper.Max(num12, num);
			num2 = MathHelper.Min(num13, num2);
			if (num > num2)
			{
				return;
			}
		}
		result = num;
	}

	public bool Intersects(BoundingSphere sphere)
	{
		Vector3.Clamp(ref sphere.Center, ref Min, ref Max, out var result);
		Vector3.DistanceSquared(ref sphere.Center, ref result, out var result2);
		if (!(result2 > sphere.Radius * sphere.Radius))
		{
			return true;
		}
		return false;
	}

	public void Intersects(ref BoundingSphere sphere, out bool result)
	{
		Vector3.Clamp(ref sphere.Center, ref Min, ref Max, out var result2);
		Vector3.DistanceSquared(ref sphere.Center, ref result2, out var result3);
		result = !(result3 > sphere.Radius * sphere.Radius);
	}

	public ContainmentType Contains(BoundingBox box)
	{
		if (Max.X < box.Min.X || Min.X > box.Max.X)
		{
			return ContainmentType.Disjoint;
		}
		if (Max.Y < box.Min.Y || Min.Y > box.Max.Y)
		{
			return ContainmentType.Disjoint;
		}
		if (Max.Z < box.Min.Z || Min.Z > box.Max.Z)
		{
			return ContainmentType.Disjoint;
		}
		if (!(Min.X <= box.Min.X) || !(box.Max.X <= Max.X) || !(Min.Y <= box.Min.Y) || !(box.Max.Y <= Max.Y) || !(Min.Z <= box.Min.Z) || !(box.Max.Z <= Max.Z))
		{
			return ContainmentType.Intersects;
		}
		return ContainmentType.Contains;
	}

	public void Contains(ref BoundingBox box, out ContainmentType result)
	{
		result = ContainmentType.Disjoint;
		if (!(Max.X < box.Min.X) && !(Min.X > box.Max.X) && !(Max.Y < box.Min.Y) && !(Min.Y > box.Max.Y) && !(Max.Z < box.Min.Z) && !(Min.Z > box.Max.Z))
		{
			result = ((Min.X <= box.Min.X && box.Max.X <= Max.X && Min.Y <= box.Min.Y && box.Max.Y <= Max.Y && Min.Z <= box.Min.Z && box.Max.Z <= Max.Z) ? ContainmentType.Contains : ContainmentType.Intersects);
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
		Vector3[] cornerArray = frustum.cornerArray;
		foreach (Vector3 point in cornerArray)
		{
			if (Contains(point) == ContainmentType.Disjoint)
			{
				return ContainmentType.Intersects;
			}
		}
		return ContainmentType.Contains;
	}

	public ContainmentType Contains(Vector3 point)
	{
		if (!(Min.X <= point.X) || !(point.X <= Max.X) || !(Min.Y <= point.Y) || !(point.Y <= Max.Y) || !(Min.Z <= point.Z) || !(point.Z <= Max.Z))
		{
			return ContainmentType.Disjoint;
		}
		return ContainmentType.Contains;
	}

	public void Contains(ref Vector3 point, out ContainmentType result)
	{
		result = ((Min.X <= point.X && point.X <= Max.X && Min.Y <= point.Y && point.Y <= Max.Y && Min.Z <= point.Z && point.Z <= Max.Z) ? ContainmentType.Contains : ContainmentType.Disjoint);
	}

	public ContainmentType Contains(BoundingSphere sphere)
	{
		Vector3.Clamp(ref sphere.Center, ref Min, ref Max, out var result);
		Vector3.DistanceSquared(ref sphere.Center, ref result, out var result2);
		float radius = sphere.Radius;
		if (result2 > radius * radius)
		{
			return ContainmentType.Disjoint;
		}
		if (!(Min.X + radius <= sphere.Center.X) || !(sphere.Center.X <= Max.X - radius) || !(Max.X - Min.X > radius) || !(Min.Y + radius <= sphere.Center.Y) || !(sphere.Center.Y <= Max.Y - radius) || !(Max.Y - Min.Y > radius) || !(Min.Z + radius <= sphere.Center.Z) || !(sphere.Center.Z <= Max.Z - radius) || !(Max.X - Min.X > radius))
		{
			return ContainmentType.Intersects;
		}
		return ContainmentType.Contains;
	}

	public void Contains(ref BoundingSphere sphere, out ContainmentType result)
	{
		Vector3.Clamp(ref sphere.Center, ref Min, ref Max, out var result2);
		Vector3.DistanceSquared(ref sphere.Center, ref result2, out var result3);
		float radius = sphere.Radius;
		if (result3 > radius * radius)
		{
			result = ContainmentType.Disjoint;
		}
		else
		{
			result = ((Min.X + radius <= sphere.Center.X && sphere.Center.X <= Max.X - radius && Max.X - Min.X > radius && Min.Y + radius <= sphere.Center.Y && sphere.Center.Y <= Max.Y - radius && Max.Y - Min.Y > radius && Min.Z + radius <= sphere.Center.Z && sphere.Center.Z <= Max.Z - radius && Max.X - Min.X > radius) ? ContainmentType.Contains : ContainmentType.Intersects);
		}
	}

	internal void SupportMapping(ref Vector3 v, out Vector3 result)
	{
		result.X = ((v.X >= 0f) ? Max.X : Min.X);
		result.Y = ((v.Y >= 0f) ? Max.Y : Min.Y);
		result.Z = ((v.Z >= 0f) ? Max.Z : Min.Z);
	}

	public static bool operator ==(BoundingBox a, BoundingBox b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(BoundingBox a, BoundingBox b)
	{
		if (!(a.Min != b.Min))
		{
			return a.Max != b.Max;
		}
		return true;
	}
}
