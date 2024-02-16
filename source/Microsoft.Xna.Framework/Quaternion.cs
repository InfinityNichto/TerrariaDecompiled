using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Xna.Framework.Design;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(QuaternionConverter))]
public struct Quaternion : IEquatable<Quaternion>
{
	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float X;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float Y;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float Z;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float W;

	private static Quaternion _identity = new Quaternion(0f, 0f, 0f, 1f);

	public static Quaternion Identity => _identity;

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

	public override string ToString()
	{
		CultureInfo currentCulture = CultureInfo.CurrentCulture;
		return string.Format(currentCulture, "{{X:{0} Y:{1} Z:{2} W:{3}}}", X.ToString(currentCulture), Y.ToString(currentCulture), Z.ToString(currentCulture), W.ToString(currentCulture));
	}

	public bool Equals(Quaternion other)
	{
		if (X == other.X && Y == other.Y && Z == other.Z)
		{
			return W == other.W;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		bool result = false;
		if (obj is Quaternion)
		{
			result = Equals((Quaternion)obj);
		}
		return result;
	}

	public override int GetHashCode()
	{
		return X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode() + W.GetHashCode();
	}

	public float LengthSquared()
	{
		return X * X + Y * Y + Z * Z + W * W;
	}

	public float Length()
	{
		float num = X * X + Y * Y + Z * Z + W * W;
		return (float)Math.Sqrt(num);
	}

	public void Normalize()
	{
		float num = X * X + Y * Y + Z * Z + W * W;
		float num2 = 1f / (float)Math.Sqrt(num);
		X *= num2;
		Y *= num2;
		Z *= num2;
		W *= num2;
	}

	public static Quaternion Normalize(Quaternion quaternion)
	{
		float num = quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W;
		float num2 = 1f / (float)Math.Sqrt(num);
		Quaternion result = default(Quaternion);
		result.X = quaternion.X * num2;
		result.Y = quaternion.Y * num2;
		result.Z = quaternion.Z * num2;
		result.W = quaternion.W * num2;
		return result;
	}

	public static void Normalize(ref Quaternion quaternion, out Quaternion result)
	{
		float num = quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W;
		float num2 = 1f / (float)Math.Sqrt(num);
		result.X = quaternion.X * num2;
		result.Y = quaternion.Y * num2;
		result.Z = quaternion.Z * num2;
		result.W = quaternion.W * num2;
	}

	public void Conjugate()
	{
		X = 0f - X;
		Y = 0f - Y;
		Z = 0f - Z;
	}

	public static Quaternion Conjugate(Quaternion value)
	{
		Quaternion result = default(Quaternion);
		result.X = 0f - value.X;
		result.Y = 0f - value.Y;
		result.Z = 0f - value.Z;
		result.W = value.W;
		return result;
	}

	public static void Conjugate(ref Quaternion value, out Quaternion result)
	{
		result.X = 0f - value.X;
		result.Y = 0f - value.Y;
		result.Z = 0f - value.Z;
		result.W = value.W;
	}

	public static Quaternion Inverse(Quaternion quaternion)
	{
		float num = quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W;
		float num2 = 1f / num;
		Quaternion result = default(Quaternion);
		result.X = (0f - quaternion.X) * num2;
		result.Y = (0f - quaternion.Y) * num2;
		result.Z = (0f - quaternion.Z) * num2;
		result.W = quaternion.W * num2;
		return result;
	}

	public static void Inverse(ref Quaternion quaternion, out Quaternion result)
	{
		float num = quaternion.X * quaternion.X + quaternion.Y * quaternion.Y + quaternion.Z * quaternion.Z + quaternion.W * quaternion.W;
		float num2 = 1f / num;
		result.X = (0f - quaternion.X) * num2;
		result.Y = (0f - quaternion.Y) * num2;
		result.Z = (0f - quaternion.Z) * num2;
		result.W = quaternion.W * num2;
	}

	public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
	{
		float num = angle * 0.5f;
		float num2 = (float)Math.Sin(num);
		float w = (float)Math.Cos(num);
		Quaternion result = default(Quaternion);
		result.X = axis.X * num2;
		result.Y = axis.Y * num2;
		result.Z = axis.Z * num2;
		result.W = w;
		return result;
	}

	public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Quaternion result)
	{
		float num = angle * 0.5f;
		float num2 = (float)Math.Sin(num);
		float w = (float)Math.Cos(num);
		result.X = axis.X * num2;
		result.Y = axis.Y * num2;
		result.Z = axis.Z * num2;
		result.W = w;
	}

	public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
	{
		float num = roll * 0.5f;
		float num2 = (float)Math.Sin(num);
		float num3 = (float)Math.Cos(num);
		float num4 = pitch * 0.5f;
		float num5 = (float)Math.Sin(num4);
		float num6 = (float)Math.Cos(num4);
		float num7 = yaw * 0.5f;
		float num8 = (float)Math.Sin(num7);
		float num9 = (float)Math.Cos(num7);
		Quaternion result = default(Quaternion);
		result.X = num9 * num5 * num3 + num8 * num6 * num2;
		result.Y = num8 * num6 * num3 - num9 * num5 * num2;
		result.Z = num9 * num6 * num2 - num8 * num5 * num3;
		result.W = num9 * num6 * num3 + num8 * num5 * num2;
		return result;
	}

	public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Quaternion result)
	{
		float num = roll * 0.5f;
		float num2 = (float)Math.Sin(num);
		float num3 = (float)Math.Cos(num);
		float num4 = pitch * 0.5f;
		float num5 = (float)Math.Sin(num4);
		float num6 = (float)Math.Cos(num4);
		float num7 = yaw * 0.5f;
		float num8 = (float)Math.Sin(num7);
		float num9 = (float)Math.Cos(num7);
		result.X = num9 * num5 * num3 + num8 * num6 * num2;
		result.Y = num8 * num6 * num3 - num9 * num5 * num2;
		result.Z = num9 * num6 * num2 - num8 * num5 * num3;
		result.W = num9 * num6 * num3 + num8 * num5 * num2;
	}

	public static Quaternion CreateFromRotationMatrix(Matrix matrix)
	{
		float num = matrix.M11 + matrix.M22 + matrix.M33;
		Quaternion result = default(Quaternion);
		if (num > 0f)
		{
			float num2 = (float)Math.Sqrt(num + 1f);
			result.W = num2 * 0.5f;
			num2 = 0.5f / num2;
			result.X = (matrix.M23 - matrix.M32) * num2;
			result.Y = (matrix.M31 - matrix.M13) * num2;
			result.Z = (matrix.M12 - matrix.M21) * num2;
		}
		else if (matrix.M11 >= matrix.M22 && matrix.M11 >= matrix.M33)
		{
			float num3 = (float)Math.Sqrt(1f + matrix.M11 - matrix.M22 - matrix.M33);
			float num4 = 0.5f / num3;
			result.X = 0.5f * num3;
			result.Y = (matrix.M12 + matrix.M21) * num4;
			result.Z = (matrix.M13 + matrix.M31) * num4;
			result.W = (matrix.M23 - matrix.M32) * num4;
		}
		else if (matrix.M22 > matrix.M33)
		{
			float num5 = (float)Math.Sqrt(1f + matrix.M22 - matrix.M11 - matrix.M33);
			float num6 = 0.5f / num5;
			result.X = (matrix.M21 + matrix.M12) * num6;
			result.Y = 0.5f * num5;
			result.Z = (matrix.M32 + matrix.M23) * num6;
			result.W = (matrix.M31 - matrix.M13) * num6;
		}
		else
		{
			float num7 = (float)Math.Sqrt(1f + matrix.M33 - matrix.M11 - matrix.M22);
			float num8 = 0.5f / num7;
			result.X = (matrix.M31 + matrix.M13) * num8;
			result.Y = (matrix.M32 + matrix.M23) * num8;
			result.Z = 0.5f * num7;
			result.W = (matrix.M12 - matrix.M21) * num8;
		}
		return result;
	}

	public static void CreateFromRotationMatrix(ref Matrix matrix, out Quaternion result)
	{
		float num = matrix.M11 + matrix.M22 + matrix.M33;
		if (num > 0f)
		{
			float num2 = (float)Math.Sqrt(num + 1f);
			result.W = num2 * 0.5f;
			num2 = 0.5f / num2;
			result.X = (matrix.M23 - matrix.M32) * num2;
			result.Y = (matrix.M31 - matrix.M13) * num2;
			result.Z = (matrix.M12 - matrix.M21) * num2;
		}
		else if (matrix.M11 >= matrix.M22 && matrix.M11 >= matrix.M33)
		{
			float num3 = (float)Math.Sqrt(1f + matrix.M11 - matrix.M22 - matrix.M33);
			float num4 = 0.5f / num3;
			result.X = 0.5f * num3;
			result.Y = (matrix.M12 + matrix.M21) * num4;
			result.Z = (matrix.M13 + matrix.M31) * num4;
			result.W = (matrix.M23 - matrix.M32) * num4;
		}
		else if (matrix.M22 > matrix.M33)
		{
			float num5 = (float)Math.Sqrt(1f + matrix.M22 - matrix.M11 - matrix.M33);
			float num6 = 0.5f / num5;
			result.X = (matrix.M21 + matrix.M12) * num6;
			result.Y = 0.5f * num5;
			result.Z = (matrix.M32 + matrix.M23) * num6;
			result.W = (matrix.M31 - matrix.M13) * num6;
		}
		else
		{
			float num7 = (float)Math.Sqrt(1f + matrix.M33 - matrix.M11 - matrix.M22);
			float num8 = 0.5f / num7;
			result.X = (matrix.M31 + matrix.M13) * num8;
			result.Y = (matrix.M32 + matrix.M23) * num8;
			result.Z = 0.5f * num7;
			result.W = (matrix.M12 - matrix.M21) * num8;
		}
	}

	public static float Dot(Quaternion quaternion1, Quaternion quaternion2)
	{
		return quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W;
	}

	public static void Dot(ref Quaternion quaternion1, ref Quaternion quaternion2, out float result)
	{
		result = quaternion1.X * quaternion2.X + quaternion1.Y * quaternion2.Y + quaternion1.Z * quaternion2.Z + quaternion1.W * quaternion2.W;
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
			float num4 = (float)Math.Acos(num);
			float num5 = (float)(1.0 / Math.Sin(num4));
			num2 = (float)Math.Sin((1f - amount) * num4) * num5;
			num3 = (flag ? ((float)(0.0 - Math.Sin(amount * num4)) * num5) : ((float)Math.Sin(amount * num4) * num5));
		}
		Quaternion result = default(Quaternion);
		result.X = num2 * quaternion1.X + num3 * quaternion2.X;
		result.Y = num2 * quaternion1.Y + num3 * quaternion2.Y;
		result.Z = num2 * quaternion1.Z + num3 * quaternion2.Z;
		result.W = num2 * quaternion1.W + num3 * quaternion2.W;
		return result;
	}

	public static void Slerp(ref Quaternion quaternion1, ref Quaternion quaternion2, float amount, out Quaternion result)
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
			float num4 = (float)Math.Acos(num);
			float num5 = (float)(1.0 / Math.Sin(num4));
			num2 = (float)Math.Sin((1f - amount) * num4) * num5;
			num3 = (flag ? ((float)(0.0 - Math.Sin(amount * num4)) * num5) : ((float)Math.Sin(amount * num4) * num5));
		}
		result.X = num2 * quaternion1.X + num3 * quaternion2.X;
		result.Y = num2 * quaternion1.Y + num3 * quaternion2.Y;
		result.Z = num2 * quaternion1.Z + num3 * quaternion2.Z;
		result.W = num2 * quaternion1.W + num3 * quaternion2.W;
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
		float num3 = result.X * result.X + result.Y * result.Y + result.Z * result.Z + result.W * result.W;
		float num4 = 1f / (float)Math.Sqrt(num3);
		result.X *= num4;
		result.Y *= num4;
		result.Z *= num4;
		result.W *= num4;
		return result;
	}

	public static void Lerp(ref Quaternion quaternion1, ref Quaternion quaternion2, float amount, out Quaternion result)
	{
		float num = 1f - amount;
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
		float num3 = result.X * result.X + result.Y * result.Y + result.Z * result.Z + result.W * result.W;
		float num4 = 1f / (float)Math.Sqrt(num3);
		result.X *= num4;
		result.Y *= num4;
		result.Z *= num4;
		result.W *= num4;
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
		Quaternion result = default(Quaternion);
		result.X = x * w2 + x2 * w + num;
		result.Y = y * w2 + y2 * w + num2;
		result.Z = z * w2 + z2 * w + num3;
		result.W = w * w2 - num4;
		return result;
	}

	public static void Concatenate(ref Quaternion value1, ref Quaternion value2, out Quaternion result)
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
		result.X = x * w2 + x2 * w + num;
		result.Y = y * w2 + y2 * w + num2;
		result.Z = z * w2 + z2 * w + num3;
		result.W = w * w2 - num4;
	}

	public static Quaternion Negate(Quaternion quaternion)
	{
		Quaternion result = default(Quaternion);
		result.X = 0f - quaternion.X;
		result.Y = 0f - quaternion.Y;
		result.Z = 0f - quaternion.Z;
		result.W = 0f - quaternion.W;
		return result;
	}

	public static void Negate(ref Quaternion quaternion, out Quaternion result)
	{
		result.X = 0f - quaternion.X;
		result.Y = 0f - quaternion.Y;
		result.Z = 0f - quaternion.Z;
		result.W = 0f - quaternion.W;
	}

	public static Quaternion Add(Quaternion quaternion1, Quaternion quaternion2)
	{
		Quaternion result = default(Quaternion);
		result.X = quaternion1.X + quaternion2.X;
		result.Y = quaternion1.Y + quaternion2.Y;
		result.Z = quaternion1.Z + quaternion2.Z;
		result.W = quaternion1.W + quaternion2.W;
		return result;
	}

	public static void Add(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
	{
		result.X = quaternion1.X + quaternion2.X;
		result.Y = quaternion1.Y + quaternion2.Y;
		result.Z = quaternion1.Z + quaternion2.Z;
		result.W = quaternion1.W + quaternion2.W;
	}

	public static Quaternion Subtract(Quaternion quaternion1, Quaternion quaternion2)
	{
		Quaternion result = default(Quaternion);
		result.X = quaternion1.X - quaternion2.X;
		result.Y = quaternion1.Y - quaternion2.Y;
		result.Z = quaternion1.Z - quaternion2.Z;
		result.W = quaternion1.W - quaternion2.W;
		return result;
	}

	public static void Subtract(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
	{
		result.X = quaternion1.X - quaternion2.X;
		result.Y = quaternion1.Y - quaternion2.Y;
		result.Z = quaternion1.Z - quaternion2.Z;
		result.W = quaternion1.W - quaternion2.W;
	}

	public static Quaternion Multiply(Quaternion quaternion1, Quaternion quaternion2)
	{
		float x = quaternion1.X;
		float y = quaternion1.Y;
		float z = quaternion1.Z;
		float w = quaternion1.W;
		float x2 = quaternion2.X;
		float y2 = quaternion2.Y;
		float z2 = quaternion2.Z;
		float w2 = quaternion2.W;
		float num = y * z2 - z * y2;
		float num2 = z * x2 - x * z2;
		float num3 = x * y2 - y * x2;
		float num4 = x * x2 + y * y2 + z * z2;
		Quaternion result = default(Quaternion);
		result.X = x * w2 + x2 * w + num;
		result.Y = y * w2 + y2 * w + num2;
		result.Z = z * w2 + z2 * w + num3;
		result.W = w * w2 - num4;
		return result;
	}

	public static void Multiply(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
	{
		float x = quaternion1.X;
		float y = quaternion1.Y;
		float z = quaternion1.Z;
		float w = quaternion1.W;
		float x2 = quaternion2.X;
		float y2 = quaternion2.Y;
		float z2 = quaternion2.Z;
		float w2 = quaternion2.W;
		float num = y * z2 - z * y2;
		float num2 = z * x2 - x * z2;
		float num3 = x * y2 - y * x2;
		float num4 = x * x2 + y * y2 + z * z2;
		result.X = x * w2 + x2 * w + num;
		result.Y = y * w2 + y2 * w + num2;
		result.Z = z * w2 + z2 * w + num3;
		result.W = w * w2 - num4;
	}

	public static Quaternion Multiply(Quaternion quaternion1, float scaleFactor)
	{
		Quaternion result = default(Quaternion);
		result.X = quaternion1.X * scaleFactor;
		result.Y = quaternion1.Y * scaleFactor;
		result.Z = quaternion1.Z * scaleFactor;
		result.W = quaternion1.W * scaleFactor;
		return result;
	}

	public static void Multiply(ref Quaternion quaternion1, float scaleFactor, out Quaternion result)
	{
		result.X = quaternion1.X * scaleFactor;
		result.Y = quaternion1.Y * scaleFactor;
		result.Z = quaternion1.Z * scaleFactor;
		result.W = quaternion1.W * scaleFactor;
	}

	public static Quaternion Divide(Quaternion quaternion1, Quaternion quaternion2)
	{
		float x = quaternion1.X;
		float y = quaternion1.Y;
		float z = quaternion1.Z;
		float w = quaternion1.W;
		float num = quaternion2.X * quaternion2.X + quaternion2.Y * quaternion2.Y + quaternion2.Z * quaternion2.Z + quaternion2.W * quaternion2.W;
		float num2 = 1f / num;
		float num3 = (0f - quaternion2.X) * num2;
		float num4 = (0f - quaternion2.Y) * num2;
		float num5 = (0f - quaternion2.Z) * num2;
		float num6 = quaternion2.W * num2;
		float num7 = y * num5 - z * num4;
		float num8 = z * num3 - x * num5;
		float num9 = x * num4 - y * num3;
		float num10 = x * num3 + y * num4 + z * num5;
		Quaternion result = default(Quaternion);
		result.X = x * num6 + num3 * w + num7;
		result.Y = y * num6 + num4 * w + num8;
		result.Z = z * num6 + num5 * w + num9;
		result.W = w * num6 - num10;
		return result;
	}

	public static void Divide(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
	{
		float x = quaternion1.X;
		float y = quaternion1.Y;
		float z = quaternion1.Z;
		float w = quaternion1.W;
		float num = quaternion2.X * quaternion2.X + quaternion2.Y * quaternion2.Y + quaternion2.Z * quaternion2.Z + quaternion2.W * quaternion2.W;
		float num2 = 1f / num;
		float num3 = (0f - quaternion2.X) * num2;
		float num4 = (0f - quaternion2.Y) * num2;
		float num5 = (0f - quaternion2.Z) * num2;
		float num6 = quaternion2.W * num2;
		float num7 = y * num5 - z * num4;
		float num8 = z * num3 - x * num5;
		float num9 = x * num4 - y * num3;
		float num10 = x * num3 + y * num4 + z * num5;
		result.X = x * num6 + num3 * w + num7;
		result.Y = y * num6 + num4 * w + num8;
		result.Z = z * num6 + num5 * w + num9;
		result.W = w * num6 - num10;
	}

	public static Quaternion operator -(Quaternion quaternion)
	{
		Quaternion result = default(Quaternion);
		result.X = 0f - quaternion.X;
		result.Y = 0f - quaternion.Y;
		result.Z = 0f - quaternion.Z;
		result.W = 0f - quaternion.W;
		return result;
	}

	public static bool operator ==(Quaternion quaternion1, Quaternion quaternion2)
	{
		if (quaternion1.X == quaternion2.X && quaternion1.Y == quaternion2.Y && quaternion1.Z == quaternion2.Z)
		{
			return quaternion1.W == quaternion2.W;
		}
		return false;
	}

	public static bool operator !=(Quaternion quaternion1, Quaternion quaternion2)
	{
		if (quaternion1.X == quaternion2.X && quaternion1.Y == quaternion2.Y && quaternion1.Z == quaternion2.Z)
		{
			return quaternion1.W != quaternion2.W;
		}
		return true;
	}

	public static Quaternion operator +(Quaternion quaternion1, Quaternion quaternion2)
	{
		Quaternion result = default(Quaternion);
		result.X = quaternion1.X + quaternion2.X;
		result.Y = quaternion1.Y + quaternion2.Y;
		result.Z = quaternion1.Z + quaternion2.Z;
		result.W = quaternion1.W + quaternion2.W;
		return result;
	}

	public static Quaternion operator -(Quaternion quaternion1, Quaternion quaternion2)
	{
		Quaternion result = default(Quaternion);
		result.X = quaternion1.X - quaternion2.X;
		result.Y = quaternion1.Y - quaternion2.Y;
		result.Z = quaternion1.Z - quaternion2.Z;
		result.W = quaternion1.W - quaternion2.W;
		return result;
	}

	public static Quaternion operator *(Quaternion quaternion1, Quaternion quaternion2)
	{
		float x = quaternion1.X;
		float y = quaternion1.Y;
		float z = quaternion1.Z;
		float w = quaternion1.W;
		float x2 = quaternion2.X;
		float y2 = quaternion2.Y;
		float z2 = quaternion2.Z;
		float w2 = quaternion2.W;
		float num = y * z2 - z * y2;
		float num2 = z * x2 - x * z2;
		float num3 = x * y2 - y * x2;
		float num4 = x * x2 + y * y2 + z * z2;
		Quaternion result = default(Quaternion);
		result.X = x * w2 + x2 * w + num;
		result.Y = y * w2 + y2 * w + num2;
		result.Z = z * w2 + z2 * w + num3;
		result.W = w * w2 - num4;
		return result;
	}

	public static Quaternion operator *(Quaternion quaternion1, float scaleFactor)
	{
		Quaternion result = default(Quaternion);
		result.X = quaternion1.X * scaleFactor;
		result.Y = quaternion1.Y * scaleFactor;
		result.Z = quaternion1.Z * scaleFactor;
		result.W = quaternion1.W * scaleFactor;
		return result;
	}

	public static Quaternion operator /(Quaternion quaternion1, Quaternion quaternion2)
	{
		float x = quaternion1.X;
		float y = quaternion1.Y;
		float z = quaternion1.Z;
		float w = quaternion1.W;
		float num = quaternion2.X * quaternion2.X + quaternion2.Y * quaternion2.Y + quaternion2.Z * quaternion2.Z + quaternion2.W * quaternion2.W;
		float num2 = 1f / num;
		float num3 = (0f - quaternion2.X) * num2;
		float num4 = (0f - quaternion2.Y) * num2;
		float num5 = (0f - quaternion2.Z) * num2;
		float num6 = quaternion2.W * num2;
		float num7 = y * num5 - z * num4;
		float num8 = z * num3 - x * num5;
		float num9 = x * num4 - y * num3;
		float num10 = x * num3 + y * num4 + z * num5;
		Quaternion result = default(Quaternion);
		result.X = x * num6 + num3 * w + num7;
		result.Y = y * num6 + num4 * w + num8;
		result.Z = z * num6 + num5 * w + num9;
		result.W = w * num6 - num10;
		return result;
	}
}
