using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Xna.Framework.Design;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(Vector2Converter))]
public struct Vector2 : IEquatable<Vector2>
{
	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float X;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float Y;

	private static Vector2 _zero = default(Vector2);

	private static Vector2 _one = new Vector2(1f, 1f);

	private static Vector2 _unitX = new Vector2(1f, 0f);

	private static Vector2 _unitY = new Vector2(0f, 1f);

	public static Vector2 Zero => _zero;

	public static Vector2 One => _one;

	public static Vector2 UnitX => _unitX;

	public static Vector2 UnitY => _unitY;

	public Vector2(float x, float y)
	{
		X = x;
		Y = y;
	}

	public Vector2(float value)
	{
		X = (Y = value);
	}

	public override string ToString()
	{
		CultureInfo currentCulture = CultureInfo.CurrentCulture;
		return string.Format(currentCulture, "{{X:{0} Y:{1}}}", new object[2]
		{
			X.ToString(currentCulture),
			Y.ToString(currentCulture)
		});
	}

	public bool Equals(Vector2 other)
	{
		if (X == other.X)
		{
			return Y == other.Y;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		bool result = false;
		if (obj is Vector2)
		{
			result = Equals((Vector2)obj);
		}
		return result;
	}

	public override int GetHashCode()
	{
		return X.GetHashCode() + Y.GetHashCode();
	}

	public float Length()
	{
		float num = X * X + Y * Y;
		return (float)Math.Sqrt(num);
	}

	public float LengthSquared()
	{
		return X * X + Y * Y;
	}

	public static float Distance(Vector2 value1, Vector2 value2)
	{
		float num = value1.X - value2.X;
		float num2 = value1.Y - value2.Y;
		float num3 = num * num + num2 * num2;
		return (float)Math.Sqrt(num3);
	}

	public static void Distance(ref Vector2 value1, ref Vector2 value2, out float result)
	{
		float num = value1.X - value2.X;
		float num2 = value1.Y - value2.Y;
		float num3 = num * num + num2 * num2;
		result = (float)Math.Sqrt(num3);
	}

	public static float DistanceSquared(Vector2 value1, Vector2 value2)
	{
		float num = value1.X - value2.X;
		float num2 = value1.Y - value2.Y;
		return num * num + num2 * num2;
	}

	public static void DistanceSquared(ref Vector2 value1, ref Vector2 value2, out float result)
	{
		float num = value1.X - value2.X;
		float num2 = value1.Y - value2.Y;
		result = num * num + num2 * num2;
	}

	public static float Dot(Vector2 value1, Vector2 value2)
	{
		return value1.X * value2.X + value1.Y * value2.Y;
	}

	public static void Dot(ref Vector2 value1, ref Vector2 value2, out float result)
	{
		result = value1.X * value2.X + value1.Y * value2.Y;
	}

	public void Normalize()
	{
		float num = X * X + Y * Y;
		float num2 = 1f / (float)Math.Sqrt(num);
		X *= num2;
		Y *= num2;
	}

	public static Vector2 Normalize(Vector2 value)
	{
		float num = value.X * value.X + value.Y * value.Y;
		float num2 = 1f / (float)Math.Sqrt(num);
		Vector2 result = default(Vector2);
		result.X = value.X * num2;
		result.Y = value.Y * num2;
		return result;
	}

	public static void Normalize(ref Vector2 value, out Vector2 result)
	{
		float num = value.X * value.X + value.Y * value.Y;
		float num2 = 1f / (float)Math.Sqrt(num);
		result.X = value.X * num2;
		result.Y = value.Y * num2;
	}

	public static Vector2 Reflect(Vector2 vector, Vector2 normal)
	{
		float num = vector.X * normal.X + vector.Y * normal.Y;
		Vector2 result = default(Vector2);
		result.X = vector.X - 2f * num * normal.X;
		result.Y = vector.Y - 2f * num * normal.Y;
		return result;
	}

	public static void Reflect(ref Vector2 vector, ref Vector2 normal, out Vector2 result)
	{
		float num = vector.X * normal.X + vector.Y * normal.Y;
		result.X = vector.X - 2f * num * normal.X;
		result.Y = vector.Y - 2f * num * normal.Y;
	}

	public static Vector2 Min(Vector2 value1, Vector2 value2)
	{
		Vector2 result = default(Vector2);
		result.X = ((value1.X < value2.X) ? value1.X : value2.X);
		result.Y = ((value1.Y < value2.Y) ? value1.Y : value2.Y);
		return result;
	}

	public static void Min(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
	{
		result.X = ((value1.X < value2.X) ? value1.X : value2.X);
		result.Y = ((value1.Y < value2.Y) ? value1.Y : value2.Y);
	}

	public static Vector2 Max(Vector2 value1, Vector2 value2)
	{
		Vector2 result = default(Vector2);
		result.X = ((value1.X > value2.X) ? value1.X : value2.X);
		result.Y = ((value1.Y > value2.Y) ? value1.Y : value2.Y);
		return result;
	}

	public static void Max(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
	{
		result.X = ((value1.X > value2.X) ? value1.X : value2.X);
		result.Y = ((value1.Y > value2.Y) ? value1.Y : value2.Y);
	}

	public static Vector2 Clamp(Vector2 value1, Vector2 min, Vector2 max)
	{
		float x = value1.X;
		x = ((x > max.X) ? max.X : x);
		x = ((x < min.X) ? min.X : x);
		float y = value1.Y;
		y = ((y > max.Y) ? max.Y : y);
		y = ((y < min.Y) ? min.Y : y);
		Vector2 result = default(Vector2);
		result.X = x;
		result.Y = y;
		return result;
	}

	public static void Clamp(ref Vector2 value1, ref Vector2 min, ref Vector2 max, out Vector2 result)
	{
		float x = value1.X;
		x = ((x > max.X) ? max.X : x);
		x = ((x < min.X) ? min.X : x);
		float y = value1.Y;
		y = ((y > max.Y) ? max.Y : y);
		y = ((y < min.Y) ? min.Y : y);
		result.X = x;
		result.Y = y;
	}

	public static Vector2 Lerp(Vector2 value1, Vector2 value2, float amount)
	{
		Vector2 result = default(Vector2);
		result.X = value1.X + (value2.X - value1.X) * amount;
		result.Y = value1.Y + (value2.Y - value1.Y) * amount;
		return result;
	}

	public static void Lerp(ref Vector2 value1, ref Vector2 value2, float amount, out Vector2 result)
	{
		result.X = value1.X + (value2.X - value1.X) * amount;
		result.Y = value1.Y + (value2.Y - value1.Y) * amount;
	}

	public static Vector2 Barycentric(Vector2 value1, Vector2 value2, Vector2 value3, float amount1, float amount2)
	{
		Vector2 result = default(Vector2);
		result.X = value1.X + amount1 * (value2.X - value1.X) + amount2 * (value3.X - value1.X);
		result.Y = value1.Y + amount1 * (value2.Y - value1.Y) + amount2 * (value3.Y - value1.Y);
		return result;
	}

	public static void Barycentric(ref Vector2 value1, ref Vector2 value2, ref Vector2 value3, float amount1, float amount2, out Vector2 result)
	{
		result.X = value1.X + amount1 * (value2.X - value1.X) + amount2 * (value3.X - value1.X);
		result.Y = value1.Y + amount1 * (value2.Y - value1.Y) + amount2 * (value3.Y - value1.Y);
	}

	public static Vector2 SmoothStep(Vector2 value1, Vector2 value2, float amount)
	{
		amount = ((amount > 1f) ? 1f : ((amount < 0f) ? 0f : amount));
		amount = amount * amount * (3f - 2f * amount);
		Vector2 result = default(Vector2);
		result.X = value1.X + (value2.X - value1.X) * amount;
		result.Y = value1.Y + (value2.Y - value1.Y) * amount;
		return result;
	}

	public static void SmoothStep(ref Vector2 value1, ref Vector2 value2, float amount, out Vector2 result)
	{
		amount = ((amount > 1f) ? 1f : ((amount < 0f) ? 0f : amount));
		amount = amount * amount * (3f - 2f * amount);
		result.X = value1.X + (value2.X - value1.X) * amount;
		result.Y = value1.Y + (value2.Y - value1.Y) * amount;
	}

	public static Vector2 CatmullRom(Vector2 value1, Vector2 value2, Vector2 value3, Vector2 value4, float amount)
	{
		float num = amount * amount;
		float num2 = amount * num;
		Vector2 result = default(Vector2);
		result.X = 0.5f * (2f * value2.X + (0f - value1.X + value3.X) * amount + (2f * value1.X - 5f * value2.X + 4f * value3.X - value4.X) * num + (0f - value1.X + 3f * value2.X - 3f * value3.X + value4.X) * num2);
		result.Y = 0.5f * (2f * value2.Y + (0f - value1.Y + value3.Y) * amount + (2f * value1.Y - 5f * value2.Y + 4f * value3.Y - value4.Y) * num + (0f - value1.Y + 3f * value2.Y - 3f * value3.Y + value4.Y) * num2);
		return result;
	}

	public static void CatmullRom(ref Vector2 value1, ref Vector2 value2, ref Vector2 value3, ref Vector2 value4, float amount, out Vector2 result)
	{
		float num = amount * amount;
		float num2 = amount * num;
		result.X = 0.5f * (2f * value2.X + (0f - value1.X + value3.X) * amount + (2f * value1.X - 5f * value2.X + 4f * value3.X - value4.X) * num + (0f - value1.X + 3f * value2.X - 3f * value3.X + value4.X) * num2);
		result.Y = 0.5f * (2f * value2.Y + (0f - value1.Y + value3.Y) * amount + (2f * value1.Y - 5f * value2.Y + 4f * value3.Y - value4.Y) * num + (0f - value1.Y + 3f * value2.Y - 3f * value3.Y + value4.Y) * num2);
	}

	public static Vector2 Hermite(Vector2 value1, Vector2 tangent1, Vector2 value2, Vector2 tangent2, float amount)
	{
		float num = amount * amount;
		float num2 = amount * num;
		float num3 = 2f * num2 - 3f * num + 1f;
		float num4 = -2f * num2 + 3f * num;
		float num5 = num2 - 2f * num + amount;
		float num6 = num2 - num;
		Vector2 result = default(Vector2);
		result.X = value1.X * num3 + value2.X * num4 + tangent1.X * num5 + tangent2.X * num6;
		result.Y = value1.Y * num3 + value2.Y * num4 + tangent1.Y * num5 + tangent2.Y * num6;
		return result;
	}

	public static void Hermite(ref Vector2 value1, ref Vector2 tangent1, ref Vector2 value2, ref Vector2 tangent2, float amount, out Vector2 result)
	{
		float num = amount * amount;
		float num2 = amount * num;
		float num3 = 2f * num2 - 3f * num + 1f;
		float num4 = -2f * num2 + 3f * num;
		float num5 = num2 - 2f * num + amount;
		float num6 = num2 - num;
		result.X = value1.X * num3 + value2.X * num4 + tangent1.X * num5 + tangent2.X * num6;
		result.Y = value1.Y * num3 + value2.Y * num4 + tangent1.Y * num5 + tangent2.Y * num6;
	}

	public static Vector2 Transform(Vector2 position, Matrix matrix)
	{
		float x = position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M41;
		float y = position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M42;
		Vector2 result = default(Vector2);
		result.X = x;
		result.Y = y;
		return result;
	}

	public static void Transform(ref Vector2 position, ref Matrix matrix, out Vector2 result)
	{
		float x = position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M41;
		float y = position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M42;
		result.X = x;
		result.Y = y;
	}

	public static Vector2 TransformNormal(Vector2 normal, Matrix matrix)
	{
		float x = normal.X * matrix.M11 + normal.Y * matrix.M21;
		float y = normal.X * matrix.M12 + normal.Y * matrix.M22;
		Vector2 result = default(Vector2);
		result.X = x;
		result.Y = y;
		return result;
	}

	public static void TransformNormal(ref Vector2 normal, ref Matrix matrix, out Vector2 result)
	{
		float x = normal.X * matrix.M11 + normal.Y * matrix.M21;
		float y = normal.X * matrix.M12 + normal.Y * matrix.M22;
		result.X = x;
		result.Y = y;
	}

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
		float x = value.X * (1f - num7 - num8) + value.Y * (num6 - num4);
		float y = value.X * (num6 + num4) + value.Y * (1f - num5 - num8);
		Vector2 result = default(Vector2);
		result.X = x;
		result.Y = y;
		return result;
	}

	public static void Transform(ref Vector2 value, ref Quaternion rotation, out Vector2 result)
	{
		float num = rotation.X + rotation.X;
		float num2 = rotation.Y + rotation.Y;
		float num3 = rotation.Z + rotation.Z;
		float num4 = rotation.W * num3;
		float num5 = rotation.X * num;
		float num6 = rotation.X * num2;
		float num7 = rotation.Y * num2;
		float num8 = rotation.Z * num3;
		float x = value.X * (1f - num7 - num8) + value.Y * (num6 - num4);
		float y = value.X * (num6 + num4) + value.Y * (1f - num5 - num8);
		result.X = x;
		result.Y = y;
	}

	public static void Transform(Vector2[] sourceArray, ref Matrix matrix, Vector2[] destinationArray)
	{
		if (sourceArray == null)
		{
			throw new ArgumentNullException("sourceArray");
		}
		if (destinationArray == null)
		{
			throw new ArgumentNullException("destinationArray");
		}
		if (destinationArray.Length < sourceArray.Length)
		{
			throw new ArgumentException(FrameworkResources.NotEnoughTargetSize);
		}
		for (int i = 0; i < sourceArray.Length; i++)
		{
			float x = sourceArray[i].X;
			float y = sourceArray[i].Y;
			destinationArray[i].X = x * matrix.M11 + y * matrix.M21 + matrix.M41;
			destinationArray[i].Y = x * matrix.M12 + y * matrix.M22 + matrix.M42;
		}
	}

	[SuppressMessage("Microsoft.Usage", "CA2233")]
	public static void Transform(Vector2[] sourceArray, int sourceIndex, ref Matrix matrix, Vector2[] destinationArray, int destinationIndex, int length)
	{
		if (sourceArray == null)
		{
			throw new ArgumentNullException("sourceArray");
		}
		if (destinationArray == null)
		{
			throw new ArgumentNullException("destinationArray");
		}
		if (sourceArray.Length < (long)sourceIndex + (long)length)
		{
			throw new ArgumentException(FrameworkResources.NotEnoughSourceSize);
		}
		if (destinationArray.Length < (long)destinationIndex + (long)length)
		{
			throw new ArgumentException(FrameworkResources.NotEnoughTargetSize);
		}
		while (length > 0)
		{
			float x = sourceArray[sourceIndex].X;
			float y = sourceArray[sourceIndex].Y;
			destinationArray[destinationIndex].X = x * matrix.M11 + y * matrix.M21 + matrix.M41;
			destinationArray[destinationIndex].Y = x * matrix.M12 + y * matrix.M22 + matrix.M42;
			sourceIndex++;
			destinationIndex++;
			length--;
		}
	}

	public static void TransformNormal(Vector2[] sourceArray, ref Matrix matrix, Vector2[] destinationArray)
	{
		if (sourceArray == null)
		{
			throw new ArgumentNullException("sourceArray");
		}
		if (destinationArray == null)
		{
			throw new ArgumentNullException("destinationArray");
		}
		if (destinationArray.Length < sourceArray.Length)
		{
			throw new ArgumentException(FrameworkResources.NotEnoughTargetSize);
		}
		for (int i = 0; i < sourceArray.Length; i++)
		{
			float x = sourceArray[i].X;
			float y = sourceArray[i].Y;
			destinationArray[i].X = x * matrix.M11 + y * matrix.M21;
			destinationArray[i].Y = x * matrix.M12 + y * matrix.M22;
		}
	}

	[SuppressMessage("Microsoft.Usage", "CA2233")]
	public static void TransformNormal(Vector2[] sourceArray, int sourceIndex, ref Matrix matrix, Vector2[] destinationArray, int destinationIndex, int length)
	{
		if (sourceArray == null)
		{
			throw new ArgumentNullException("sourceArray");
		}
		if (destinationArray == null)
		{
			throw new ArgumentNullException("destinationArray");
		}
		if (sourceArray.Length < (long)sourceIndex + (long)length)
		{
			throw new ArgumentException(FrameworkResources.NotEnoughSourceSize);
		}
		if (destinationArray.Length < (long)destinationIndex + (long)length)
		{
			throw new ArgumentException(FrameworkResources.NotEnoughTargetSize);
		}
		while (length > 0)
		{
			float x = sourceArray[sourceIndex].X;
			float y = sourceArray[sourceIndex].Y;
			destinationArray[destinationIndex].X = x * matrix.M11 + y * matrix.M21;
			destinationArray[destinationIndex].Y = x * matrix.M12 + y * matrix.M22;
			sourceIndex++;
			destinationIndex++;
			length--;
		}
	}

	public static void Transform(Vector2[] sourceArray, ref Quaternion rotation, Vector2[] destinationArray)
	{
		if (sourceArray == null)
		{
			throw new ArgumentNullException("sourceArray");
		}
		if (destinationArray == null)
		{
			throw new ArgumentNullException("destinationArray");
		}
		if (destinationArray.Length < sourceArray.Length)
		{
			throw new ArgumentException(FrameworkResources.NotEnoughTargetSize);
		}
		float num = rotation.X + rotation.X;
		float num2 = rotation.Y + rotation.Y;
		float num3 = rotation.Z + rotation.Z;
		float num4 = rotation.W * num3;
		float num5 = rotation.X * num;
		float num6 = rotation.X * num2;
		float num7 = rotation.Y * num2;
		float num8 = rotation.Z * num3;
		float num9 = 1f - num7 - num8;
		float num10 = num6 - num4;
		float num11 = num6 + num4;
		float num12 = 1f - num5 - num8;
		for (int i = 0; i < sourceArray.Length; i++)
		{
			float x = sourceArray[i].X;
			float y = sourceArray[i].Y;
			destinationArray[i].X = x * num9 + y * num10;
			destinationArray[i].Y = x * num11 + y * num12;
		}
	}

	[SuppressMessage("Microsoft.Usage", "CA2233")]
	public static void Transform(Vector2[] sourceArray, int sourceIndex, ref Quaternion rotation, Vector2[] destinationArray, int destinationIndex, int length)
	{
		if (sourceArray == null)
		{
			throw new ArgumentNullException("sourceArray");
		}
		if (destinationArray == null)
		{
			throw new ArgumentNullException("destinationArray");
		}
		if (sourceArray.Length < (long)sourceIndex + (long)length)
		{
			throw new ArgumentException(FrameworkResources.NotEnoughSourceSize);
		}
		if (destinationArray.Length < (long)destinationIndex + (long)length)
		{
			throw new ArgumentException(FrameworkResources.NotEnoughTargetSize);
		}
		float num = rotation.X + rotation.X;
		float num2 = rotation.Y + rotation.Y;
		float num3 = rotation.Z + rotation.Z;
		float num4 = rotation.W * num3;
		float num5 = rotation.X * num;
		float num6 = rotation.X * num2;
		float num7 = rotation.Y * num2;
		float num8 = rotation.Z * num3;
		float num9 = 1f - num7 - num8;
		float num10 = num6 - num4;
		float num11 = num6 + num4;
		float num12 = 1f - num5 - num8;
		while (length > 0)
		{
			float x = sourceArray[sourceIndex].X;
			float y = sourceArray[sourceIndex].Y;
			destinationArray[destinationIndex].X = x * num9 + y * num10;
			destinationArray[destinationIndex].Y = x * num11 + y * num12;
			sourceIndex++;
			destinationIndex++;
			length--;
		}
	}

	public static Vector2 Negate(Vector2 value)
	{
		Vector2 result = default(Vector2);
		result.X = 0f - value.X;
		result.Y = 0f - value.Y;
		return result;
	}

	public static void Negate(ref Vector2 value, out Vector2 result)
	{
		result.X = 0f - value.X;
		result.Y = 0f - value.Y;
	}

	public static Vector2 Add(Vector2 value1, Vector2 value2)
	{
		Vector2 result = default(Vector2);
		result.X = value1.X + value2.X;
		result.Y = value1.Y + value2.Y;
		return result;
	}

	public static void Add(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
	{
		result.X = value1.X + value2.X;
		result.Y = value1.Y + value2.Y;
	}

	public static Vector2 Subtract(Vector2 value1, Vector2 value2)
	{
		Vector2 result = default(Vector2);
		result.X = value1.X - value2.X;
		result.Y = value1.Y - value2.Y;
		return result;
	}

	public static void Subtract(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
	{
		result.X = value1.X - value2.X;
		result.Y = value1.Y - value2.Y;
	}

	public static Vector2 Multiply(Vector2 value1, Vector2 value2)
	{
		Vector2 result = default(Vector2);
		result.X = value1.X * value2.X;
		result.Y = value1.Y * value2.Y;
		return result;
	}

	public static void Multiply(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
	{
		result.X = value1.X * value2.X;
		result.Y = value1.Y * value2.Y;
	}

	public static Vector2 Multiply(Vector2 value1, float scaleFactor)
	{
		Vector2 result = default(Vector2);
		result.X = value1.X * scaleFactor;
		result.Y = value1.Y * scaleFactor;
		return result;
	}

	public static void Multiply(ref Vector2 value1, float scaleFactor, out Vector2 result)
	{
		result.X = value1.X * scaleFactor;
		result.Y = value1.Y * scaleFactor;
	}

	public static Vector2 Divide(Vector2 value1, Vector2 value2)
	{
		Vector2 result = default(Vector2);
		result.X = value1.X / value2.X;
		result.Y = value1.Y / value2.Y;
		return result;
	}

	public static void Divide(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
	{
		result.X = value1.X / value2.X;
		result.Y = value1.Y / value2.Y;
	}

	public static Vector2 Divide(Vector2 value1, float divider)
	{
		float num = 1f / divider;
		Vector2 result = default(Vector2);
		result.X = value1.X * num;
		result.Y = value1.Y * num;
		return result;
	}

	public static void Divide(ref Vector2 value1, float divider, out Vector2 result)
	{
		float num = 1f / divider;
		result.X = value1.X * num;
		result.Y = value1.Y * num;
	}

	public static Vector2 operator -(Vector2 value)
	{
		Vector2 result = default(Vector2);
		result.X = 0f - value.X;
		result.Y = 0f - value.Y;
		return result;
	}

	public static bool operator ==(Vector2 value1, Vector2 value2)
	{
		if (value1.X == value2.X)
		{
			return value1.Y == value2.Y;
		}
		return false;
	}

	public static bool operator !=(Vector2 value1, Vector2 value2)
	{
		if (value1.X == value2.X)
		{
			return value1.Y != value2.Y;
		}
		return true;
	}

	public static Vector2 operator +(Vector2 value1, Vector2 value2)
	{
		Vector2 result = default(Vector2);
		result.X = value1.X + value2.X;
		result.Y = value1.Y + value2.Y;
		return result;
	}

	public static Vector2 operator -(Vector2 value1, Vector2 value2)
	{
		Vector2 result = default(Vector2);
		result.X = value1.X - value2.X;
		result.Y = value1.Y - value2.Y;
		return result;
	}

	public static Vector2 operator *(Vector2 value1, Vector2 value2)
	{
		Vector2 result = default(Vector2);
		result.X = value1.X * value2.X;
		result.Y = value1.Y * value2.Y;
		return result;
	}

	public static Vector2 operator *(Vector2 value, float scaleFactor)
	{
		Vector2 result = default(Vector2);
		result.X = value.X * scaleFactor;
		result.Y = value.Y * scaleFactor;
		return result;
	}

	public static Vector2 operator *(float scaleFactor, Vector2 value)
	{
		Vector2 result = default(Vector2);
		result.X = value.X * scaleFactor;
		result.Y = value.Y * scaleFactor;
		return result;
	}

	public static Vector2 operator /(Vector2 value1, Vector2 value2)
	{
		Vector2 result = default(Vector2);
		result.X = value1.X / value2.X;
		result.Y = value1.Y / value2.Y;
		return result;
	}

	public static Vector2 operator /(Vector2 value1, float divider)
	{
		float num = 1f / divider;
		Vector2 result = default(Vector2);
		result.X = value1.X * num;
		result.Y = value1.Y * num;
		return result;
	}
}
