using System;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(ExpandableObjectConverter))]
public class BoundingFrustum : IEquatable<BoundingFrustum>
{
	public const int CornerCount = 8;

	private const int NearPlaneIndex = 0;

	private const int FarPlaneIndex = 1;

	private const int LeftPlaneIndex = 2;

	private const int RightPlaneIndex = 3;

	private const int TopPlaneIndex = 4;

	private const int BottomPlaneIndex = 5;

	private const int NumPlanes = 6;

	private Matrix matrix;

	private Plane[] planes = new Plane[6];

	internal Vector3[] cornerArray = new Vector3[8];

	private Gjk gjk;

	public Plane Near => planes[0];

	public Plane Far => planes[1];

	public Plane Left => planes[2];

	public Plane Right => planes[3];

	public Plane Top => planes[4];

	public Plane Bottom => planes[5];

	public Matrix Matrix
	{
		get
		{
			return matrix;
		}
		set
		{
			SetMatrix(ref value);
		}
	}

	public Vector3[] GetCorners()
	{
		return (Vector3[])cornerArray.Clone();
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
		cornerArray.CopyTo(corners, 0);
	}

	public bool Equals(BoundingFrustum other)
	{
		if (other == null)
		{
			return false;
		}
		return matrix == other.matrix;
	}

	public override bool Equals(object obj)
	{
		bool result = false;
		BoundingFrustum boundingFrustum = obj as BoundingFrustum;
		if (boundingFrustum != null)
		{
			result = matrix == boundingFrustum.matrix;
		}
		return result;
	}

	public override int GetHashCode()
	{
		return matrix.GetHashCode();
	}

	public override string ToString()
	{
		CultureInfo currentCulture = CultureInfo.CurrentCulture;
		return string.Format(currentCulture, "{{Near:{0} Far:{1} Left:{2} Right:{3} Top:{4} Bottom:{5}}}", Near.ToString(), Far.ToString(), Left.ToString(), Right.ToString(), Top.ToString(), Bottom.ToString());
	}

	private BoundingFrustum()
	{
	}

	public BoundingFrustum(Matrix value)
	{
		SetMatrix(ref value);
	}

	private void SetMatrix(ref Matrix value)
	{
		matrix = value;
		planes[2].Normal.X = 0f - value.M14 - value.M11;
		planes[2].Normal.Y = 0f - value.M24 - value.M21;
		planes[2].Normal.Z = 0f - value.M34 - value.M31;
		planes[2].D = 0f - value.M44 - value.M41;
		planes[3].Normal.X = 0f - value.M14 + value.M11;
		planes[3].Normal.Y = 0f - value.M24 + value.M21;
		planes[3].Normal.Z = 0f - value.M34 + value.M31;
		planes[3].D = 0f - value.M44 + value.M41;
		planes[4].Normal.X = 0f - value.M14 + value.M12;
		planes[4].Normal.Y = 0f - value.M24 + value.M22;
		planes[4].Normal.Z = 0f - value.M34 + value.M32;
		planes[4].D = 0f - value.M44 + value.M42;
		planes[5].Normal.X = 0f - value.M14 - value.M12;
		planes[5].Normal.Y = 0f - value.M24 - value.M22;
		planes[5].Normal.Z = 0f - value.M34 - value.M32;
		planes[5].D = 0f - value.M44 - value.M42;
		planes[0].Normal.X = 0f - value.M13;
		planes[0].Normal.Y = 0f - value.M23;
		planes[0].Normal.Z = 0f - value.M33;
		planes[0].D = 0f - value.M43;
		planes[1].Normal.X = 0f - value.M14 + value.M13;
		planes[1].Normal.Y = 0f - value.M24 + value.M23;
		planes[1].Normal.Z = 0f - value.M34 + value.M33;
		planes[1].D = 0f - value.M44 + value.M43;
		for (int i = 0; i < 6; i++)
		{
			float num = planes[i].Normal.Length();
			planes[i].Normal /= num;
			planes[i].D /= num;
		}
		Ray ray = ComputeIntersectionLine(ref planes[0], ref planes[2]);
		ref Vector3 reference = ref cornerArray[0];
		reference = ComputeIntersection(ref planes[4], ref ray);
		ref Vector3 reference2 = ref cornerArray[3];
		reference2 = ComputeIntersection(ref planes[5], ref ray);
		ray = ComputeIntersectionLine(ref planes[3], ref planes[0]);
		ref Vector3 reference3 = ref cornerArray[1];
		reference3 = ComputeIntersection(ref planes[4], ref ray);
		ref Vector3 reference4 = ref cornerArray[2];
		reference4 = ComputeIntersection(ref planes[5], ref ray);
		ray = ComputeIntersectionLine(ref planes[2], ref planes[1]);
		ref Vector3 reference5 = ref cornerArray[4];
		reference5 = ComputeIntersection(ref planes[4], ref ray);
		ref Vector3 reference6 = ref cornerArray[7];
		reference6 = ComputeIntersection(ref planes[5], ref ray);
		ray = ComputeIntersectionLine(ref planes[1], ref planes[3]);
		ref Vector3 reference7 = ref cornerArray[5];
		reference7 = ComputeIntersection(ref planes[4], ref ray);
		ref Vector3 reference8 = ref cornerArray[6];
		reference8 = ComputeIntersection(ref planes[5], ref ray);
	}

	private static Ray ComputeIntersectionLine(ref Plane p1, ref Plane p2)
	{
		Ray result = default(Ray);
		result.Direction = Vector3.Cross(p1.Normal, p2.Normal);
		float num = result.Direction.LengthSquared();
		result.Position = Vector3.Cross((0f - p1.D) * p2.Normal + p2.D * p1.Normal, result.Direction) / num;
		return result;
	}

	private static Vector3 ComputeIntersection(ref Plane plane, ref Ray ray)
	{
		float num = (0f - plane.D - Vector3.Dot(plane.Normal, ray.Position)) / Vector3.Dot(plane.Normal, ray.Direction);
		return ray.Position + ray.Direction * num;
	}

	public bool Intersects(BoundingBox box)
	{
		Intersects(ref box, out var result);
		return result;
	}

	public void Intersects(ref BoundingBox box, out bool result)
	{
		if (gjk == null)
		{
			gjk = new Gjk();
		}
		gjk.Reset();
		Vector3.Subtract(ref cornerArray[0], ref box.Min, out var result2);
		if (result2.LengthSquared() < 1E-05f)
		{
			Vector3.Subtract(ref cornerArray[0], ref box.Max, out result2);
		}
		float num = float.MaxValue;
		float num2 = 0f;
		result = false;
		Vector3 v = default(Vector3);
		do
		{
			v.X = 0f - result2.X;
			v.Y = 0f - result2.Y;
			v.Z = 0f - result2.Z;
			SupportMapping(ref v, out var result3);
			box.SupportMapping(ref result2, out var result4);
			Vector3.Subtract(ref result3, ref result4, out var result5);
			float num3 = result2.X * result5.X + result2.Y * result5.Y + result2.Z * result5.Z;
			if (num3 > 0f)
			{
				return;
			}
			gjk.AddSupportPoint(ref result5);
			result2 = gjk.ClosestPoint;
			float num4 = num;
			num = result2.LengthSquared();
			if (num4 - num <= 1E-05f * num4)
			{
				return;
			}
			num2 = 4E-05f * gjk.MaxLengthSquared;
		}
		while (!gjk.FullSimplex && num >= num2);
		result = true;
	}

	public bool Intersects(BoundingFrustum frustum)
	{
		if (frustum == null)
		{
			throw new ArgumentNullException("frustum");
		}
		if (gjk == null)
		{
			gjk = new Gjk();
		}
		gjk.Reset();
		Vector3.Subtract(ref cornerArray[0], ref frustum.cornerArray[0], out var result);
		if (result.LengthSquared() < 1E-05f)
		{
			Vector3.Subtract(ref cornerArray[0], ref frustum.cornerArray[1], out result);
		}
		float num = float.MaxValue;
		float num2 = 0f;
		Vector3 v = default(Vector3);
		do
		{
			v.X = 0f - result.X;
			v.Y = 0f - result.Y;
			v.Z = 0f - result.Z;
			SupportMapping(ref v, out var result2);
			frustum.SupportMapping(ref result, out var result3);
			Vector3.Subtract(ref result2, ref result3, out var result4);
			float num3 = result.X * result4.X + result.Y * result4.Y + result.Z * result4.Z;
			if (num3 > 0f)
			{
				return false;
			}
			gjk.AddSupportPoint(ref result4);
			result = gjk.ClosestPoint;
			float num4 = num;
			num = result.LengthSquared();
			num2 = 4E-05f * gjk.MaxLengthSquared;
			if (num4 - num <= 1E-05f * num4)
			{
				return false;
			}
		}
		while (!gjk.FullSimplex && num >= num2);
		return true;
	}

	public PlaneIntersectionType Intersects(Plane plane)
	{
		int num = 0;
		for (int i = 0; i < 8; i++)
		{
			Vector3.Dot(ref cornerArray[i], ref plane.Normal, out var result);
			num = ((!(result + plane.D > 0f)) ? (num | 2) : (num | 1));
			if (num == 3)
			{
				return PlaneIntersectionType.Intersecting;
			}
		}
		if (num != 1)
		{
			return PlaneIntersectionType.Back;
		}
		return PlaneIntersectionType.Front;
	}

	public void Intersects(ref Plane plane, out PlaneIntersectionType result)
	{
		int num = 0;
		for (int i = 0; i < 8; i++)
		{
			Vector3.Dot(ref cornerArray[i], ref plane.Normal, out var result2);
			num = ((!(result2 + plane.D > 0f)) ? (num | 2) : (num | 1));
			if (num == 3)
			{
				result = PlaneIntersectionType.Intersecting;
				return;
			}
		}
		result = ((num != 1) ? PlaneIntersectionType.Back : PlaneIntersectionType.Front);
	}

	public float? Intersects(Ray ray)
	{
		Intersects(ref ray, out var result);
		return result;
	}

	public void Intersects(ref Ray ray, out float? result)
	{
		Contains(ref ray.Position, out var result2);
		if (result2 == ContainmentType.Contains)
		{
			result = 0f;
			return;
		}
		float num = float.MinValue;
		float num2 = float.MaxValue;
		result = null;
		Plane[] array = planes;
		for (int i = 0; i < array.Length; i++)
		{
			Plane plane = array[i];
			Vector3 vector = plane.Normal;
			Vector3.Dot(ref ray.Direction, ref vector, out var result3);
			Vector3.Dot(ref ray.Position, ref vector, out var result4);
			result4 += plane.D;
			if (Math.Abs(result3) < 1E-05f)
			{
				if (result4 > 0f)
				{
					return;
				}
				continue;
			}
			float num3 = (0f - result4) / result3;
			if (result3 < 0f)
			{
				if (num3 > num2)
				{
					return;
				}
				if (num3 > num)
				{
					num = num3;
				}
			}
			else
			{
				if (num3 < num)
				{
					return;
				}
				if (num3 < num2)
				{
					num2 = num3;
				}
			}
		}
		float num4 = ((num >= 0f) ? num : num2);
		if (num4 >= 0f)
		{
			result = num4;
		}
	}

	public bool Intersects(BoundingSphere sphere)
	{
		Intersects(ref sphere, out var result);
		return result;
	}

	public void Intersects(ref BoundingSphere sphere, out bool result)
	{
		if (gjk == null)
		{
			gjk = new Gjk();
		}
		gjk.Reset();
		Vector3.Subtract(ref cornerArray[0], ref sphere.Center, out var result2);
		if (result2.LengthSquared() < 1E-05f)
		{
			result2 = Vector3.UnitX;
		}
		float num = float.MaxValue;
		float num2 = 0f;
		result = false;
		Vector3 v = default(Vector3);
		do
		{
			v.X = 0f - result2.X;
			v.Y = 0f - result2.Y;
			v.Z = 0f - result2.Z;
			SupportMapping(ref v, out var result3);
			sphere.SupportMapping(ref result2, out var result4);
			Vector3.Subtract(ref result3, ref result4, out var result5);
			float num3 = result2.X * result5.X + result2.Y * result5.Y + result2.Z * result5.Z;
			if (num3 > 0f)
			{
				return;
			}
			gjk.AddSupportPoint(ref result5);
			result2 = gjk.ClosestPoint;
			float num4 = num;
			num = result2.LengthSquared();
			if (num4 - num <= 1E-05f * num4)
			{
				return;
			}
			num2 = 4E-05f * gjk.MaxLengthSquared;
		}
		while (!gjk.FullSimplex && num >= num2);
		result = true;
	}

	public ContainmentType Contains(BoundingBox box)
	{
		bool flag = false;
		Plane[] array = planes;
		foreach (Plane plane in array)
		{
			switch (box.Intersects(plane))
			{
			case PlaneIntersectionType.Front:
				return ContainmentType.Disjoint;
			case PlaneIntersectionType.Intersecting:
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return ContainmentType.Contains;
		}
		return ContainmentType.Intersects;
	}

	public void Contains(ref BoundingBox box, out ContainmentType result)
	{
		bool flag = false;
		Plane[] array = planes;
		foreach (Plane plane in array)
		{
			switch (box.Intersects(plane))
			{
			case PlaneIntersectionType.Front:
				result = ContainmentType.Disjoint;
				return;
			case PlaneIntersectionType.Intersecting:
				flag = true;
				break;
			}
		}
		result = ((!flag) ? ContainmentType.Contains : ContainmentType.Intersects);
	}

	public ContainmentType Contains(BoundingFrustum frustum)
	{
		if (frustum == null)
		{
			throw new ArgumentNullException("frustum");
		}
		ContainmentType result = ContainmentType.Disjoint;
		if (Intersects(frustum))
		{
			result = ContainmentType.Contains;
			for (int i = 0; i < cornerArray.Length; i++)
			{
				if (Contains(frustum.cornerArray[i]) == ContainmentType.Disjoint)
				{
					result = ContainmentType.Intersects;
					break;
				}
			}
		}
		return result;
	}

	public ContainmentType Contains(Vector3 point)
	{
		Plane[] array = planes;
		for (int i = 0; i < array.Length; i++)
		{
			Plane plane = array[i];
			float num = plane.Normal.X * point.X + plane.Normal.Y * point.Y + plane.Normal.Z * point.Z + plane.D;
			if (num > 1E-05f)
			{
				return ContainmentType.Disjoint;
			}
		}
		return ContainmentType.Contains;
	}

	public void Contains(ref Vector3 point, out ContainmentType result)
	{
		Plane[] array = planes;
		for (int i = 0; i < array.Length; i++)
		{
			Plane plane = array[i];
			float num = plane.Normal.X * point.X + plane.Normal.Y * point.Y + plane.Normal.Z * point.Z + plane.D;
			if (num > 1E-05f)
			{
				result = ContainmentType.Disjoint;
				return;
			}
		}
		result = ContainmentType.Contains;
	}

	public ContainmentType Contains(BoundingSphere sphere)
	{
		Vector3 center = sphere.Center;
		float radius = sphere.Radius;
		int num = 0;
		Plane[] array = planes;
		for (int i = 0; i < array.Length; i++)
		{
			Plane plane = array[i];
			float num2 = plane.Normal.X * center.X + plane.Normal.Y * center.Y + plane.Normal.Z * center.Z;
			float num3 = num2 + plane.D;
			if (num3 > radius)
			{
				return ContainmentType.Disjoint;
			}
			if (num3 < 0f - radius)
			{
				num++;
			}
		}
		if (num != 6)
		{
			return ContainmentType.Intersects;
		}
		return ContainmentType.Contains;
	}

	public void Contains(ref BoundingSphere sphere, out ContainmentType result)
	{
		Vector3 center = sphere.Center;
		float radius = sphere.Radius;
		int num = 0;
		Plane[] array = planes;
		for (int i = 0; i < array.Length; i++)
		{
			Plane plane = array[i];
			float num2 = plane.Normal.X * center.X + plane.Normal.Y * center.Y + plane.Normal.Z * center.Z;
			float num3 = num2 + plane.D;
			if (num3 > radius)
			{
				result = ContainmentType.Disjoint;
				return;
			}
			if (num3 < 0f - radius)
			{
				num++;
			}
		}
		result = ((num == 6) ? ContainmentType.Contains : ContainmentType.Intersects);
	}

	internal void SupportMapping(ref Vector3 v, out Vector3 result)
	{
		int num = 0;
		Vector3.Dot(ref cornerArray[0], ref v, out var result2);
		for (int i = 1; i < cornerArray.Length; i++)
		{
			Vector3.Dot(ref cornerArray[i], ref v, out var result3);
			if (result3 > result2)
			{
				num = i;
				result2 = result3;
			}
		}
		result = cornerArray[num];
	}

	public static bool operator ==(BoundingFrustum a, BoundingFrustum b)
	{
		return object.Equals(a, b);
	}

	public static bool operator !=(BoundingFrustum a, BoundingFrustum b)
	{
		return !object.Equals(a, b);
	}
}
