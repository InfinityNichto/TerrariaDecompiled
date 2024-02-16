using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Xna.Framework.Design;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(RayConverter))]
public struct Ray : IEquatable<Ray>
{
	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector3 Position;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector3 Direction;

	public Ray(Vector3 position, Vector3 direction)
	{
		Position = position;
		Direction = direction;
	}

	public bool Equals(Ray other)
	{
		if (Position.X == other.Position.X && Position.Y == other.Position.Y && Position.Z == other.Position.Z && Direction.X == other.Direction.X && Direction.Y == other.Direction.Y)
		{
			return Direction.Z == other.Direction.Z;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		bool result = false;
		if (obj != null && obj is Ray)
		{
			result = Equals((Ray)obj);
		}
		return result;
	}

	public override int GetHashCode()
	{
		return Position.GetHashCode() + Direction.GetHashCode();
	}

	public override string ToString()
	{
		CultureInfo currentCulture = CultureInfo.CurrentCulture;
		return string.Format(currentCulture, "{{Position:{0} Direction:{1}}}", new object[2]
		{
			Position.ToString(),
			Direction.ToString()
		});
	}

	public float? Intersects(BoundingBox box)
	{
		return box.Intersects(this);
	}

	public void Intersects(ref BoundingBox box, out float? result)
	{
		box.Intersects(ref this, out result);
	}

	public float? Intersects(BoundingFrustum frustum)
	{
		if (frustum == null)
		{
			throw new ArgumentNullException("frustum");
		}
		return frustum.Intersects(this);
	}

	public float? Intersects(Plane plane)
	{
		float num = plane.Normal.X * Direction.X + plane.Normal.Y * Direction.Y + plane.Normal.Z * Direction.Z;
		if (Math.Abs(num) < 1E-05f)
		{
			return null;
		}
		float num2 = plane.Normal.X * Position.X + plane.Normal.Y * Position.Y + plane.Normal.Z * Position.Z;
		float num3 = (0f - plane.D - num2) / num;
		if (num3 < 0f)
		{
			if (num3 < -1E-05f)
			{
				return null;
			}
			num3 = 0f;
		}
		return num3;
	}

	public void Intersects(ref Plane plane, out float? result)
	{
		float num = plane.Normal.X * Direction.X + plane.Normal.Y * Direction.Y + plane.Normal.Z * Direction.Z;
		if (Math.Abs(num) < 1E-05f)
		{
			result = null;
			return;
		}
		float num2 = plane.Normal.X * Position.X + plane.Normal.Y * Position.Y + plane.Normal.Z * Position.Z;
		float num3 = (0f - plane.D - num2) / num;
		if (num3 < 0f)
		{
			if (num3 < -1E-05f)
			{
				result = null;
				return;
			}
			result = 0f;
		}
		result = num3;
	}

	public float? Intersects(BoundingSphere sphere)
	{
		float num = sphere.Center.X - Position.X;
		float num2 = sphere.Center.Y - Position.Y;
		float num3 = sphere.Center.Z - Position.Z;
		float num4 = num * num + num2 * num2 + num3 * num3;
		float num5 = sphere.Radius * sphere.Radius;
		if (num4 <= num5)
		{
			return 0f;
		}
		float num6 = num * Direction.X + num2 * Direction.Y + num3 * Direction.Z;
		if (num6 < 0f)
		{
			return null;
		}
		float num7 = num4 - num6 * num6;
		if (num7 > num5)
		{
			return null;
		}
		float num8 = (float)Math.Sqrt(num5 - num7);
		return num6 - num8;
	}

	public void Intersects(ref BoundingSphere sphere, out float? result)
	{
		float num = sphere.Center.X - Position.X;
		float num2 = sphere.Center.Y - Position.Y;
		float num3 = sphere.Center.Z - Position.Z;
		float num4 = num * num + num2 * num2 + num3 * num3;
		float num5 = sphere.Radius * sphere.Radius;
		if (num4 <= num5)
		{
			result = 0f;
			return;
		}
		result = null;
		float num6 = num * Direction.X + num2 * Direction.Y + num3 * Direction.Z;
		if (!(num6 < 0f))
		{
			float num7 = num4 - num6 * num6;
			if (!(num7 > num5))
			{
				float num8 = (float)Math.Sqrt(num5 - num7);
				result = num6 - num8;
			}
		}
	}

	public static bool operator ==(Ray a, Ray b)
	{
		if (a.Position.X == b.Position.X && a.Position.Y == b.Position.Y && a.Position.Z == b.Position.Z && a.Direction.X == b.Direction.X && a.Direction.Y == b.Direction.Y)
		{
			return a.Direction.Z == b.Direction.Z;
		}
		return false;
	}

	public static bool operator !=(Ray a, Ray b)
	{
		if (a.Position.X == b.Position.X && a.Position.Y == b.Position.Y && a.Position.Z == b.Position.Z && a.Direction.X == b.Direction.X && a.Direction.Y == b.Direction.Y)
		{
			return a.Direction.Z != b.Direction.Z;
		}
		return true;
	}
}
