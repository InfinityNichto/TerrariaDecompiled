using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Xna.Framework.Design;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(MatrixConverter))]
public struct Matrix : IEquatable<Matrix>
{
	private struct CanonicalBasis
	{
		public Vector3 Row0;

		public Vector3 Row1;

		public Vector3 Row2;
	}

	private struct VectorBasis
	{
		public unsafe Vector3* Element0;

		public unsafe Vector3* Element1;

		public unsafe Vector3* Element2;
	}

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M11;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M12;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M13;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M14;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M21;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M22;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M23;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M24;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M31;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M32;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M33;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M34;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M41;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M42;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M43;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public float M44;

	private static Matrix _identity = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f);

	public static Matrix Identity => _identity;

	public Vector3 Up
	{
		get
		{
			Vector3 result = default(Vector3);
			result.X = M21;
			result.Y = M22;
			result.Z = M23;
			return result;
		}
		set
		{
			M21 = value.X;
			M22 = value.Y;
			M23 = value.Z;
		}
	}

	public Vector3 Down
	{
		get
		{
			Vector3 result = default(Vector3);
			result.X = 0f - M21;
			result.Y = 0f - M22;
			result.Z = 0f - M23;
			return result;
		}
		set
		{
			M21 = 0f - value.X;
			M22 = 0f - value.Y;
			M23 = 0f - value.Z;
		}
	}

	public Vector3 Right
	{
		get
		{
			Vector3 result = default(Vector3);
			result.X = M11;
			result.Y = M12;
			result.Z = M13;
			return result;
		}
		set
		{
			M11 = value.X;
			M12 = value.Y;
			M13 = value.Z;
		}
	}

	public Vector3 Left
	{
		get
		{
			Vector3 result = default(Vector3);
			result.X = 0f - M11;
			result.Y = 0f - M12;
			result.Z = 0f - M13;
			return result;
		}
		set
		{
			M11 = 0f - value.X;
			M12 = 0f - value.Y;
			M13 = 0f - value.Z;
		}
	}

	public Vector3 Forward
	{
		get
		{
			Vector3 result = default(Vector3);
			result.X = 0f - M31;
			result.Y = 0f - M32;
			result.Z = 0f - M33;
			return result;
		}
		set
		{
			M31 = 0f - value.X;
			M32 = 0f - value.Y;
			M33 = 0f - value.Z;
		}
	}

	public Vector3 Backward
	{
		get
		{
			Vector3 result = default(Vector3);
			result.X = M31;
			result.Y = M32;
			result.Z = M33;
			return result;
		}
		set
		{
			M31 = value.X;
			M32 = value.Y;
			M33 = value.Z;
		}
	}

	public Vector3 Translation
	{
		get
		{
			Vector3 result = default(Vector3);
			result.X = M41;
			result.Y = M42;
			result.Z = M43;
			return result;
		}
		set
		{
			M41 = value.X;
			M42 = value.Y;
			M43 = value.Z;
		}
	}

	[SuppressMessage("Microsoft.Design", "CA1025")]
	public Matrix(float m11, float m12, float m13, float m14, float m21, float m22, float m23, float m24, float m31, float m32, float m33, float m34, float m41, float m42, float m43, float m44)
	{
		M11 = m11;
		M12 = m12;
		M13 = m13;
		M14 = m14;
		M21 = m21;
		M22 = m22;
		M23 = m23;
		M24 = m24;
		M31 = m31;
		M32 = m32;
		M33 = m33;
		M34 = m34;
		M41 = m41;
		M42 = m42;
		M43 = m43;
		M44 = m44;
	}

	public static Matrix CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3? cameraForwardVector)
	{
		Vector3 value = default(Vector3);
		value.X = objectPosition.X - cameraPosition.X;
		value.Y = objectPosition.Y - cameraPosition.Y;
		value.Z = objectPosition.Z - cameraPosition.Z;
		float num = value.LengthSquared();
		if (num < 0.0001f)
		{
			value = (cameraForwardVector.HasValue ? (-cameraForwardVector.Value) : Vector3.Forward);
		}
		else
		{
			Vector3.Multiply(ref value, 1f / (float)Math.Sqrt(num), out value);
		}
		Vector3.Cross(ref cameraUpVector, ref value, out var result);
		result.Normalize();
		Vector3.Cross(ref value, ref result, out var result2);
		Matrix result3 = default(Matrix);
		result3.M11 = result.X;
		result3.M12 = result.Y;
		result3.M13 = result.Z;
		result3.M14 = 0f;
		result3.M21 = result2.X;
		result3.M22 = result2.Y;
		result3.M23 = result2.Z;
		result3.M24 = 0f;
		result3.M31 = value.X;
		result3.M32 = value.Y;
		result3.M33 = value.Z;
		result3.M34 = 0f;
		result3.M41 = objectPosition.X;
		result3.M42 = objectPosition.Y;
		result3.M43 = objectPosition.Z;
		result3.M44 = 1f;
		return result3;
	}

	public static void CreateBillboard(ref Vector3 objectPosition, ref Vector3 cameraPosition, ref Vector3 cameraUpVector, Vector3? cameraForwardVector, out Matrix result)
	{
		Vector3 value = default(Vector3);
		value.X = objectPosition.X - cameraPosition.X;
		value.Y = objectPosition.Y - cameraPosition.Y;
		value.Z = objectPosition.Z - cameraPosition.Z;
		float num = value.LengthSquared();
		if (num < 0.0001f)
		{
			value = (cameraForwardVector.HasValue ? (-cameraForwardVector.Value) : Vector3.Forward);
		}
		else
		{
			Vector3.Multiply(ref value, 1f / (float)Math.Sqrt(num), out value);
		}
		Vector3.Cross(ref cameraUpVector, ref value, out var result2);
		result2.Normalize();
		Vector3.Cross(ref value, ref result2, out var result3);
		result.M11 = result2.X;
		result.M12 = result2.Y;
		result.M13 = result2.Z;
		result.M14 = 0f;
		result.M21 = result3.X;
		result.M22 = result3.Y;
		result.M23 = result3.Z;
		result.M24 = 0f;
		result.M31 = value.X;
		result.M32 = value.Y;
		result.M33 = value.Z;
		result.M34 = 0f;
		result.M41 = objectPosition.X;
		result.M42 = objectPosition.Y;
		result.M43 = objectPosition.Z;
		result.M44 = 1f;
	}

	public static Matrix CreateConstrainedBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 rotateAxis, Vector3? cameraForwardVector, Vector3? objectForwardVector)
	{
		Vector3 value = default(Vector3);
		value.X = objectPosition.X - cameraPosition.X;
		value.Y = objectPosition.Y - cameraPosition.Y;
		value.Z = objectPosition.Z - cameraPosition.Z;
		float num = value.LengthSquared();
		if (num < 0.0001f)
		{
			value = (cameraForwardVector.HasValue ? (-cameraForwardVector.Value) : Vector3.Forward);
		}
		else
		{
			Vector3.Multiply(ref value, 1f / (float)Math.Sqrt(num), out value);
		}
		Vector3 vector = rotateAxis;
		Vector3.Dot(ref rotateAxis, ref value, out var result);
		Vector3 result2;
		Vector3 result3;
		if (Math.Abs(result) > 0.99825466f)
		{
			if (objectForwardVector.HasValue)
			{
				result2 = objectForwardVector.Value;
				Vector3.Dot(ref rotateAxis, ref result2, out result);
				if (Math.Abs(result) > 0.99825466f)
				{
					result = rotateAxis.X * Vector3.Forward.X + rotateAxis.Y * Vector3.Forward.Y + rotateAxis.Z * Vector3.Forward.Z;
					result2 = ((Math.Abs(result) > 0.99825466f) ? Vector3.Right : Vector3.Forward);
				}
			}
			else
			{
				result = rotateAxis.X * Vector3.Forward.X + rotateAxis.Y * Vector3.Forward.Y + rotateAxis.Z * Vector3.Forward.Z;
				result2 = ((Math.Abs(result) > 0.99825466f) ? Vector3.Right : Vector3.Forward);
			}
			Vector3.Cross(ref rotateAxis, ref result2, out result3);
			result3.Normalize();
			Vector3.Cross(ref result3, ref rotateAxis, out result2);
			result2.Normalize();
		}
		else
		{
			Vector3.Cross(ref rotateAxis, ref value, out result3);
			result3.Normalize();
			Vector3.Cross(ref result3, ref vector, out result2);
			result2.Normalize();
		}
		Matrix result4 = default(Matrix);
		result4.M11 = result3.X;
		result4.M12 = result3.Y;
		result4.M13 = result3.Z;
		result4.M14 = 0f;
		result4.M21 = vector.X;
		result4.M22 = vector.Y;
		result4.M23 = vector.Z;
		result4.M24 = 0f;
		result4.M31 = result2.X;
		result4.M32 = result2.Y;
		result4.M33 = result2.Z;
		result4.M34 = 0f;
		result4.M41 = objectPosition.X;
		result4.M42 = objectPosition.Y;
		result4.M43 = objectPosition.Z;
		result4.M44 = 1f;
		return result4;
	}

	public static void CreateConstrainedBillboard(ref Vector3 objectPosition, ref Vector3 cameraPosition, ref Vector3 rotateAxis, Vector3? cameraForwardVector, Vector3? objectForwardVector, out Matrix result)
	{
		Vector3 value = default(Vector3);
		value.X = objectPosition.X - cameraPosition.X;
		value.Y = objectPosition.Y - cameraPosition.Y;
		value.Z = objectPosition.Z - cameraPosition.Z;
		float num = value.LengthSquared();
		if (num < 0.0001f)
		{
			value = (cameraForwardVector.HasValue ? (-cameraForwardVector.Value) : Vector3.Forward);
		}
		else
		{
			Vector3.Multiply(ref value, 1f / (float)Math.Sqrt(num), out value);
		}
		Vector3 vector = rotateAxis;
		Vector3.Dot(ref rotateAxis, ref value, out var result2);
		Vector3 result3;
		Vector3 result4;
		if (Math.Abs(result2) > 0.99825466f)
		{
			if (objectForwardVector.HasValue)
			{
				result3 = objectForwardVector.Value;
				Vector3.Dot(ref rotateAxis, ref result3, out result2);
				if (Math.Abs(result2) > 0.99825466f)
				{
					result2 = rotateAxis.X * Vector3.Forward.X + rotateAxis.Y * Vector3.Forward.Y + rotateAxis.Z * Vector3.Forward.Z;
					result3 = ((Math.Abs(result2) > 0.99825466f) ? Vector3.Right : Vector3.Forward);
				}
			}
			else
			{
				result2 = rotateAxis.X * Vector3.Forward.X + rotateAxis.Y * Vector3.Forward.Y + rotateAxis.Z * Vector3.Forward.Z;
				result3 = ((Math.Abs(result2) > 0.99825466f) ? Vector3.Right : Vector3.Forward);
			}
			Vector3.Cross(ref rotateAxis, ref result3, out result4);
			result4.Normalize();
			Vector3.Cross(ref result4, ref rotateAxis, out result3);
			result3.Normalize();
		}
		else
		{
			Vector3.Cross(ref rotateAxis, ref value, out result4);
			result4.Normalize();
			Vector3.Cross(ref result4, ref vector, out result3);
			result3.Normalize();
		}
		result.M11 = result4.X;
		result.M12 = result4.Y;
		result.M13 = result4.Z;
		result.M14 = 0f;
		result.M21 = vector.X;
		result.M22 = vector.Y;
		result.M23 = vector.Z;
		result.M24 = 0f;
		result.M31 = result3.X;
		result.M32 = result3.Y;
		result.M33 = result3.Z;
		result.M34 = 0f;
		result.M41 = objectPosition.X;
		result.M42 = objectPosition.Y;
		result.M43 = objectPosition.Z;
		result.M44 = 1f;
	}

	public static Matrix CreateTranslation(Vector3 position)
	{
		Matrix result = default(Matrix);
		result.M11 = 1f;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = 1f;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = 1f;
		result.M34 = 0f;
		result.M41 = position.X;
		result.M42 = position.Y;
		result.M43 = position.Z;
		result.M44 = 1f;
		return result;
	}

	public static void CreateTranslation(ref Vector3 position, out Matrix result)
	{
		result.M11 = 1f;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = 1f;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = 1f;
		result.M34 = 0f;
		result.M41 = position.X;
		result.M42 = position.Y;
		result.M43 = position.Z;
		result.M44 = 1f;
	}

	public static Matrix CreateTranslation(float xPosition, float yPosition, float zPosition)
	{
		Matrix result = default(Matrix);
		result.M11 = 1f;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = 1f;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = 1f;
		result.M34 = 0f;
		result.M41 = xPosition;
		result.M42 = yPosition;
		result.M43 = zPosition;
		result.M44 = 1f;
		return result;
	}

	public static void CreateTranslation(float xPosition, float yPosition, float zPosition, out Matrix result)
	{
		result.M11 = 1f;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = 1f;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = 1f;
		result.M34 = 0f;
		result.M41 = xPosition;
		result.M42 = yPosition;
		result.M43 = zPosition;
		result.M44 = 1f;
	}

	public static Matrix CreateScale(float xScale, float yScale, float zScale)
	{
		Matrix result = default(Matrix);
		result.M11 = xScale;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = yScale;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = zScale;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
		return result;
	}

	public static void CreateScale(float xScale, float yScale, float zScale, out Matrix result)
	{
		result.M11 = xScale;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = yScale;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = zScale;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
	}

	public static Matrix CreateScale(Vector3 scales)
	{
		float x = scales.X;
		float y = scales.Y;
		float z = scales.Z;
		Matrix result = default(Matrix);
		result.M11 = x;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = y;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = z;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
		return result;
	}

	public static void CreateScale(ref Vector3 scales, out Matrix result)
	{
		float x = scales.X;
		float y = scales.Y;
		float z = scales.Z;
		result.M11 = x;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = y;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = z;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
	}

	public static Matrix CreateScale(float scale)
	{
		Matrix result = default(Matrix);
		float num = (result.M11 = scale);
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = num;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = num;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
		return result;
	}

	public static void CreateScale(float scale, out Matrix result)
	{
		float num = (result.M11 = scale);
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = num;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = num;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
	}

	public static Matrix CreateRotationX(float radians)
	{
		float num = (float)Math.Cos(radians);
		float num2 = (float)Math.Sin(radians);
		Matrix result = default(Matrix);
		result.M11 = 1f;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = num;
		result.M23 = num2;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f - num2;
		result.M33 = num;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
		return result;
	}

	public static void CreateRotationX(float radians, out Matrix result)
	{
		float num = (float)Math.Cos(radians);
		float num2 = (float)Math.Sin(radians);
		result.M11 = 1f;
		result.M12 = 0f;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = num;
		result.M23 = num2;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f - num2;
		result.M33 = num;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
	}

	public static Matrix CreateRotationY(float radians)
	{
		float num = (float)Math.Cos(radians);
		float num2 = (float)Math.Sin(radians);
		Matrix result = default(Matrix);
		result.M11 = num;
		result.M12 = 0f;
		result.M13 = 0f - num2;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = 1f;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = num2;
		result.M32 = 0f;
		result.M33 = num;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
		return result;
	}

	public static void CreateRotationY(float radians, out Matrix result)
	{
		float num = (float)Math.Cos(radians);
		float num2 = (float)Math.Sin(radians);
		result.M11 = num;
		result.M12 = 0f;
		result.M13 = 0f - num2;
		result.M14 = 0f;
		result.M21 = 0f;
		result.M22 = 1f;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = num2;
		result.M32 = 0f;
		result.M33 = num;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
	}

	public static Matrix CreateRotationZ(float radians)
	{
		float num = (float)Math.Cos(radians);
		float num2 = (float)Math.Sin(radians);
		Matrix result = default(Matrix);
		result.M11 = num;
		result.M12 = num2;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f - num2;
		result.M22 = num;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = 1f;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
		return result;
	}

	public static void CreateRotationZ(float radians, out Matrix result)
	{
		float num = (float)Math.Cos(radians);
		float num2 = (float)Math.Sin(radians);
		result.M11 = num;
		result.M12 = num2;
		result.M13 = 0f;
		result.M14 = 0f;
		result.M21 = 0f - num2;
		result.M22 = num;
		result.M23 = 0f;
		result.M24 = 0f;
		result.M31 = 0f;
		result.M32 = 0f;
		result.M33 = 1f;
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
	}

	public static Matrix CreateFromAxisAngle(Vector3 axis, float angle)
	{
		float x = axis.X;
		float y = axis.Y;
		float z = axis.Z;
		float num = (float)Math.Sin(angle);
		float num2 = (float)Math.Cos(angle);
		float num3 = x * x;
		float num4 = y * y;
		float num5 = z * z;
		float num6 = x * y;
		float num7 = x * z;
		float num8 = y * z;
		Matrix result = default(Matrix);
		result.M11 = num3 + num2 * (1f - num3);
		result.M12 = num6 - num2 * num6 + num * z;
		result.M13 = num7 - num2 * num7 - num * y;
		result.M14 = 0f;
		result.M21 = num6 - num2 * num6 - num * z;
		result.M22 = num4 + num2 * (1f - num4);
		result.M23 = num8 - num2 * num8 + num * x;
		result.M24 = 0f;
		result.M31 = num7 - num2 * num7 + num * y;
		result.M32 = num8 - num2 * num8 - num * x;
		result.M33 = num5 + num2 * (1f - num5);
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
		return result;
	}

	public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Matrix result)
	{
		float x = axis.X;
		float y = axis.Y;
		float z = axis.Z;
		float num = (float)Math.Sin(angle);
		float num2 = (float)Math.Cos(angle);
		float num3 = x * x;
		float num4 = y * y;
		float num5 = z * z;
		float num6 = x * y;
		float num7 = x * z;
		float num8 = y * z;
		result.M11 = num3 + num2 * (1f - num3);
		result.M12 = num6 - num2 * num6 + num * z;
		result.M13 = num7 - num2 * num7 - num * y;
		result.M14 = 0f;
		result.M21 = num6 - num2 * num6 - num * z;
		result.M22 = num4 + num2 * (1f - num4);
		result.M23 = num8 - num2 * num8 + num * x;
		result.M24 = 0f;
		result.M31 = num7 - num2 * num7 + num * y;
		result.M32 = num8 - num2 * num8 - num * x;
		result.M33 = num5 + num2 * (1f - num5);
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
	}

	public static Matrix CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
	{
		if (fieldOfView <= 0f || fieldOfView >= (float)Math.PI)
		{
			throw new ArgumentOutOfRangeException("fieldOfView", string.Format(CultureInfo.CurrentCulture, FrameworkResources.OutRangeFieldOfView, new object[1] { "fieldOfView" }));
		}
		if (nearPlaneDistance <= 0f)
		{
			throw new ArgumentOutOfRangeException("nearPlaneDistance", string.Format(CultureInfo.CurrentCulture, FrameworkResources.NegativePlaneDistance, new object[1] { "nearPlaneDistance" }));
		}
		if (farPlaneDistance <= 0f)
		{
			throw new ArgumentOutOfRangeException("farPlaneDistance", string.Format(CultureInfo.CurrentCulture, FrameworkResources.NegativePlaneDistance, new object[1] { "farPlaneDistance" }));
		}
		if (nearPlaneDistance >= farPlaneDistance)
		{
			throw new ArgumentOutOfRangeException("nearPlaneDistance", FrameworkResources.OppositePlanes);
		}
		float num = 1f / (float)Math.Tan(fieldOfView * 0.5f);
		float m = num / aspectRatio;
		Matrix result = default(Matrix);
		result.M11 = m;
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = num;
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M31 = (result.M32 = 0f);
		result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		result.M34 = -1f;
		result.M41 = (result.M42 = (result.M44 = 0f));
		result.M43 = nearPlaneDistance * farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		return result;
	}

	public static void CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance, out Matrix result)
	{
		if (fieldOfView <= 0f || fieldOfView >= (float)Math.PI)
		{
			throw new ArgumentOutOfRangeException("fieldOfView", string.Format(CultureInfo.CurrentCulture, FrameworkResources.OutRangeFieldOfView, new object[1] { "fieldOfView" }));
		}
		if (nearPlaneDistance <= 0f)
		{
			throw new ArgumentOutOfRangeException("nearPlaneDistance", string.Format(CultureInfo.CurrentCulture, FrameworkResources.NegativePlaneDistance, new object[1] { "nearPlaneDistance" }));
		}
		if (farPlaneDistance <= 0f)
		{
			throw new ArgumentOutOfRangeException("farPlaneDistance", string.Format(CultureInfo.CurrentCulture, FrameworkResources.NegativePlaneDistance, new object[1] { "farPlaneDistance" }));
		}
		if (nearPlaneDistance >= farPlaneDistance)
		{
			throw new ArgumentOutOfRangeException("nearPlaneDistance", FrameworkResources.OppositePlanes);
		}
		float num = 1f / (float)Math.Tan(fieldOfView * 0.5f);
		float m = num / aspectRatio;
		result.M11 = m;
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = num;
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M31 = (result.M32 = 0f);
		result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		result.M34 = -1f;
		result.M41 = (result.M42 = (result.M44 = 0f));
		result.M43 = nearPlaneDistance * farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
	}

	public static Matrix CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance)
	{
		if (nearPlaneDistance <= 0f)
		{
			throw new ArgumentOutOfRangeException("nearPlaneDistance", string.Format(CultureInfo.CurrentCulture, FrameworkResources.NegativePlaneDistance, new object[1] { "nearPlaneDistance" }));
		}
		if (farPlaneDistance <= 0f)
		{
			throw new ArgumentOutOfRangeException("farPlaneDistance", string.Format(CultureInfo.CurrentCulture, FrameworkResources.NegativePlaneDistance, new object[1] { "farPlaneDistance" }));
		}
		if (nearPlaneDistance >= farPlaneDistance)
		{
			throw new ArgumentOutOfRangeException("nearPlaneDistance", FrameworkResources.OppositePlanes);
		}
		Matrix result = default(Matrix);
		result.M11 = 2f * nearPlaneDistance / width;
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = 2f * nearPlaneDistance / height;
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		result.M31 = (result.M32 = 0f);
		result.M34 = -1f;
		result.M41 = (result.M42 = (result.M44 = 0f));
		result.M43 = nearPlaneDistance * farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		return result;
	}

	public static void CreatePerspective(float width, float height, float nearPlaneDistance, float farPlaneDistance, out Matrix result)
	{
		if (nearPlaneDistance <= 0f)
		{
			throw new ArgumentOutOfRangeException("nearPlaneDistance", string.Format(CultureInfo.CurrentCulture, FrameworkResources.NegativePlaneDistance, new object[1] { "nearPlaneDistance" }));
		}
		if (farPlaneDistance <= 0f)
		{
			throw new ArgumentOutOfRangeException("farPlaneDistance", string.Format(CultureInfo.CurrentCulture, FrameworkResources.NegativePlaneDistance, new object[1] { "farPlaneDistance" }));
		}
		if (nearPlaneDistance >= farPlaneDistance)
		{
			throw new ArgumentOutOfRangeException("nearPlaneDistance", FrameworkResources.OppositePlanes);
		}
		result.M11 = 2f * nearPlaneDistance / width;
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = 2f * nearPlaneDistance / height;
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		result.M31 = (result.M32 = 0f);
		result.M34 = -1f;
		result.M41 = (result.M42 = (result.M44 = 0f));
		result.M43 = nearPlaneDistance * farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
	}

	public static Matrix CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance)
	{
		if (nearPlaneDistance <= 0f)
		{
			throw new ArgumentOutOfRangeException("nearPlaneDistance", string.Format(CultureInfo.CurrentCulture, FrameworkResources.NegativePlaneDistance, new object[1] { "nearPlaneDistance" }));
		}
		if (farPlaneDistance <= 0f)
		{
			throw new ArgumentOutOfRangeException("farPlaneDistance", string.Format(CultureInfo.CurrentCulture, FrameworkResources.NegativePlaneDistance, new object[1] { "farPlaneDistance" }));
		}
		if (nearPlaneDistance >= farPlaneDistance)
		{
			throw new ArgumentOutOfRangeException("nearPlaneDistance", FrameworkResources.OppositePlanes);
		}
		Matrix result = default(Matrix);
		result.M11 = 2f * nearPlaneDistance / (right - left);
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = 2f * nearPlaneDistance / (top - bottom);
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M31 = (left + right) / (right - left);
		result.M32 = (top + bottom) / (top - bottom);
		result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		result.M34 = -1f;
		result.M43 = nearPlaneDistance * farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		result.M41 = (result.M42 = (result.M44 = 0f));
		return result;
	}

	public static void CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float nearPlaneDistance, float farPlaneDistance, out Matrix result)
	{
		if (nearPlaneDistance <= 0f)
		{
			throw new ArgumentOutOfRangeException("nearPlaneDistance", string.Format(CultureInfo.CurrentCulture, FrameworkResources.NegativePlaneDistance, new object[1] { "nearPlaneDistance" }));
		}
		if (farPlaneDistance <= 0f)
		{
			throw new ArgumentOutOfRangeException("farPlaneDistance", string.Format(CultureInfo.CurrentCulture, FrameworkResources.NegativePlaneDistance, new object[1] { "farPlaneDistance" }));
		}
		if (nearPlaneDistance >= farPlaneDistance)
		{
			throw new ArgumentOutOfRangeException("nearPlaneDistance", FrameworkResources.OppositePlanes);
		}
		result.M11 = 2f * nearPlaneDistance / (right - left);
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = 2f * nearPlaneDistance / (top - bottom);
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M31 = (left + right) / (right - left);
		result.M32 = (top + bottom) / (top - bottom);
		result.M33 = farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		result.M34 = -1f;
		result.M43 = nearPlaneDistance * farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
		result.M41 = (result.M42 = (result.M44 = 0f));
	}

	public static Matrix CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane)
	{
		Matrix result = default(Matrix);
		result.M11 = 2f / width;
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = 2f / height;
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M33 = 1f / (zNearPlane - zFarPlane);
		result.M31 = (result.M32 = (result.M34 = 0f));
		result.M41 = (result.M42 = 0f);
		result.M43 = zNearPlane / (zNearPlane - zFarPlane);
		result.M44 = 1f;
		return result;
	}

	public static void CreateOrthographic(float width, float height, float zNearPlane, float zFarPlane, out Matrix result)
	{
		result.M11 = 2f / width;
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = 2f / height;
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M33 = 1f / (zNearPlane - zFarPlane);
		result.M31 = (result.M32 = (result.M34 = 0f));
		result.M41 = (result.M42 = 0f);
		result.M43 = zNearPlane / (zNearPlane - zFarPlane);
		result.M44 = 1f;
	}

	public static Matrix CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane)
	{
		Matrix result = default(Matrix);
		result.M11 = 2f / (right - left);
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = 2f / (top - bottom);
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M33 = 1f / (zNearPlane - zFarPlane);
		result.M31 = (result.M32 = (result.M34 = 0f));
		result.M41 = (left + right) / (left - right);
		result.M42 = (top + bottom) / (bottom - top);
		result.M43 = zNearPlane / (zNearPlane - zFarPlane);
		result.M44 = 1f;
		return result;
	}

	public static void CreateOrthographicOffCenter(float left, float right, float bottom, float top, float zNearPlane, float zFarPlane, out Matrix result)
	{
		result.M11 = 2f / (right - left);
		result.M12 = (result.M13 = (result.M14 = 0f));
		result.M22 = 2f / (top - bottom);
		result.M21 = (result.M23 = (result.M24 = 0f));
		result.M33 = 1f / (zNearPlane - zFarPlane);
		result.M31 = (result.M32 = (result.M34 = 0f));
		result.M41 = (left + right) / (left - right);
		result.M42 = (top + bottom) / (bottom - top);
		result.M43 = zNearPlane / (zNearPlane - zFarPlane);
		result.M44 = 1f;
	}

	public static Matrix CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
	{
		Vector3 vector = Vector3.Normalize(cameraPosition - cameraTarget);
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(cameraUpVector, vector));
		Vector3 vector3 = Vector3.Cross(vector, vector2);
		Matrix result = default(Matrix);
		result.M11 = vector2.X;
		result.M12 = vector3.X;
		result.M13 = vector.X;
		result.M14 = 0f;
		result.M21 = vector2.Y;
		result.M22 = vector3.Y;
		result.M23 = vector.Y;
		result.M24 = 0f;
		result.M31 = vector2.Z;
		result.M32 = vector3.Z;
		result.M33 = vector.Z;
		result.M34 = 0f;
		result.M41 = 0f - Vector3.Dot(vector2, cameraPosition);
		result.M42 = 0f - Vector3.Dot(vector3, cameraPosition);
		result.M43 = 0f - Vector3.Dot(vector, cameraPosition);
		result.M44 = 1f;
		return result;
	}

	public static void CreateLookAt(ref Vector3 cameraPosition, ref Vector3 cameraTarget, ref Vector3 cameraUpVector, out Matrix result)
	{
		Vector3 vector = Vector3.Normalize(cameraPosition - cameraTarget);
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(cameraUpVector, vector));
		Vector3 vector3 = Vector3.Cross(vector, vector2);
		result.M11 = vector2.X;
		result.M12 = vector3.X;
		result.M13 = vector.X;
		result.M14 = 0f;
		result.M21 = vector2.Y;
		result.M22 = vector3.Y;
		result.M23 = vector.Y;
		result.M24 = 0f;
		result.M31 = vector2.Z;
		result.M32 = vector3.Z;
		result.M33 = vector.Z;
		result.M34 = 0f;
		result.M41 = 0f - Vector3.Dot(vector2, cameraPosition);
		result.M42 = 0f - Vector3.Dot(vector3, cameraPosition);
		result.M43 = 0f - Vector3.Dot(vector, cameraPosition);
		result.M44 = 1f;
	}

	public static Matrix CreateWorld(Vector3 position, Vector3 forward, Vector3 up)
	{
		Vector3 vector = Vector3.Normalize(-forward);
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(up, vector));
		Vector3 vector3 = Vector3.Cross(vector, vector2);
		Matrix result = default(Matrix);
		result.M11 = vector2.X;
		result.M12 = vector2.Y;
		result.M13 = vector2.Z;
		result.M14 = 0f;
		result.M21 = vector3.X;
		result.M22 = vector3.Y;
		result.M23 = vector3.Z;
		result.M24 = 0f;
		result.M31 = vector.X;
		result.M32 = vector.Y;
		result.M33 = vector.Z;
		result.M34 = 0f;
		result.M41 = position.X;
		result.M42 = position.Y;
		result.M43 = position.Z;
		result.M44 = 1f;
		return result;
	}

	public static void CreateWorld(ref Vector3 position, ref Vector3 forward, ref Vector3 up, out Matrix result)
	{
		Vector3 vector = Vector3.Normalize(-forward);
		Vector3 vector2 = Vector3.Normalize(Vector3.Cross(up, vector));
		Vector3 vector3 = Vector3.Cross(vector, vector2);
		result.M11 = vector2.X;
		result.M12 = vector2.Y;
		result.M13 = vector2.Z;
		result.M14 = 0f;
		result.M21 = vector3.X;
		result.M22 = vector3.Y;
		result.M23 = vector3.Z;
		result.M24 = 0f;
		result.M31 = vector.X;
		result.M32 = vector.Y;
		result.M33 = vector.Z;
		result.M34 = 0f;
		result.M41 = position.X;
		result.M42 = position.Y;
		result.M43 = position.Z;
		result.M44 = 1f;
	}

	public static Matrix CreateFromQuaternion(Quaternion quaternion)
	{
		float num = quaternion.X * quaternion.X;
		float num2 = quaternion.Y * quaternion.Y;
		float num3 = quaternion.Z * quaternion.Z;
		float num4 = quaternion.X * quaternion.Y;
		float num5 = quaternion.Z * quaternion.W;
		float num6 = quaternion.Z * quaternion.X;
		float num7 = quaternion.Y * quaternion.W;
		float num8 = quaternion.Y * quaternion.Z;
		float num9 = quaternion.X * quaternion.W;
		Matrix result = default(Matrix);
		result.M11 = 1f - 2f * (num2 + num3);
		result.M12 = 2f * (num4 + num5);
		result.M13 = 2f * (num6 - num7);
		result.M14 = 0f;
		result.M21 = 2f * (num4 - num5);
		result.M22 = 1f - 2f * (num3 + num);
		result.M23 = 2f * (num8 + num9);
		result.M24 = 0f;
		result.M31 = 2f * (num6 + num7);
		result.M32 = 2f * (num8 - num9);
		result.M33 = 1f - 2f * (num2 + num);
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
		return result;
	}

	public static void CreateFromQuaternion(ref Quaternion quaternion, out Matrix result)
	{
		float num = quaternion.X * quaternion.X;
		float num2 = quaternion.Y * quaternion.Y;
		float num3 = quaternion.Z * quaternion.Z;
		float num4 = quaternion.X * quaternion.Y;
		float num5 = quaternion.Z * quaternion.W;
		float num6 = quaternion.Z * quaternion.X;
		float num7 = quaternion.Y * quaternion.W;
		float num8 = quaternion.Y * quaternion.Z;
		float num9 = quaternion.X * quaternion.W;
		result.M11 = 1f - 2f * (num2 + num3);
		result.M12 = 2f * (num4 + num5);
		result.M13 = 2f * (num6 - num7);
		result.M14 = 0f;
		result.M21 = 2f * (num4 - num5);
		result.M22 = 1f - 2f * (num3 + num);
		result.M23 = 2f * (num8 + num9);
		result.M24 = 0f;
		result.M31 = 2f * (num6 + num7);
		result.M32 = 2f * (num8 - num9);
		result.M33 = 1f - 2f * (num2 + num);
		result.M34 = 0f;
		result.M41 = 0f;
		result.M42 = 0f;
		result.M43 = 0f;
		result.M44 = 1f;
	}

	public static Matrix CreateFromYawPitchRoll(float yaw, float pitch, float roll)
	{
		Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll, out var result);
		CreateFromQuaternion(ref result, out var result2);
		return result2;
	}

	public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Matrix result)
	{
		Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll, out var result2);
		CreateFromQuaternion(ref result2, out result);
	}

	public static Matrix CreateShadow(Vector3 lightDirection, Plane plane)
	{
		Plane.Normalize(ref plane, out var result);
		float num = result.Normal.X * lightDirection.X + result.Normal.Y * lightDirection.Y + result.Normal.Z * lightDirection.Z;
		float num2 = 0f - result.Normal.X;
		float num3 = 0f - result.Normal.Y;
		float num4 = 0f - result.Normal.Z;
		float num5 = 0f - result.D;
		Matrix result2 = default(Matrix);
		result2.M11 = num2 * lightDirection.X + num;
		result2.M21 = num3 * lightDirection.X;
		result2.M31 = num4 * lightDirection.X;
		result2.M41 = num5 * lightDirection.X;
		result2.M12 = num2 * lightDirection.Y;
		result2.M22 = num3 * lightDirection.Y + num;
		result2.M32 = num4 * lightDirection.Y;
		result2.M42 = num5 * lightDirection.Y;
		result2.M13 = num2 * lightDirection.Z;
		result2.M23 = num3 * lightDirection.Z;
		result2.M33 = num4 * lightDirection.Z + num;
		result2.M43 = num5 * lightDirection.Z;
		result2.M14 = 0f;
		result2.M24 = 0f;
		result2.M34 = 0f;
		result2.M44 = num;
		return result2;
	}

	public static void CreateShadow(ref Vector3 lightDirection, ref Plane plane, out Matrix result)
	{
		Plane.Normalize(ref plane, out var result2);
		float num = result2.Normal.X * lightDirection.X + result2.Normal.Y * lightDirection.Y + result2.Normal.Z * lightDirection.Z;
		float num2 = 0f - result2.Normal.X;
		float num3 = 0f - result2.Normal.Y;
		float num4 = 0f - result2.Normal.Z;
		float num5 = 0f - result2.D;
		result.M11 = num2 * lightDirection.X + num;
		result.M21 = num3 * lightDirection.X;
		result.M31 = num4 * lightDirection.X;
		result.M41 = num5 * lightDirection.X;
		result.M12 = num2 * lightDirection.Y;
		result.M22 = num3 * lightDirection.Y + num;
		result.M32 = num4 * lightDirection.Y;
		result.M42 = num5 * lightDirection.Y;
		result.M13 = num2 * lightDirection.Z;
		result.M23 = num3 * lightDirection.Z;
		result.M33 = num4 * lightDirection.Z + num;
		result.M43 = num5 * lightDirection.Z;
		result.M14 = 0f;
		result.M24 = 0f;
		result.M34 = 0f;
		result.M44 = num;
	}

	public static Matrix CreateReflection(Plane value)
	{
		value.Normalize();
		float x = value.Normal.X;
		float y = value.Normal.Y;
		float z = value.Normal.Z;
		float num = -2f * x;
		float num2 = -2f * y;
		float num3 = -2f * z;
		Matrix result = default(Matrix);
		result.M11 = num * x + 1f;
		result.M12 = num2 * x;
		result.M13 = num3 * x;
		result.M14 = 0f;
		result.M21 = num * y;
		result.M22 = num2 * y + 1f;
		result.M23 = num3 * y;
		result.M24 = 0f;
		result.M31 = num * z;
		result.M32 = num2 * z;
		result.M33 = num3 * z + 1f;
		result.M34 = 0f;
		result.M41 = num * value.D;
		result.M42 = num2 * value.D;
		result.M43 = num3 * value.D;
		result.M44 = 1f;
		return result;
	}

	public static void CreateReflection(ref Plane value, out Matrix result)
	{
		Plane.Normalize(ref value, out var result2);
		value.Normalize();
		float x = result2.Normal.X;
		float y = result2.Normal.Y;
		float z = result2.Normal.Z;
		float num = -2f * x;
		float num2 = -2f * y;
		float num3 = -2f * z;
		result.M11 = num * x + 1f;
		result.M12 = num2 * x;
		result.M13 = num3 * x;
		result.M14 = 0f;
		result.M21 = num * y;
		result.M22 = num2 * y + 1f;
		result.M23 = num3 * y;
		result.M24 = 0f;
		result.M31 = num * z;
		result.M32 = num2 * z;
		result.M33 = num3 * z + 1f;
		result.M34 = 0f;
		result.M41 = num * result2.D;
		result.M42 = num2 * result2.D;
		result.M43 = num3 * result2.D;
		result.M44 = 1f;
	}

	public unsafe bool Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation)
	{
		bool result = true;
		fixed (float* ptr3 = &scale.X)
		{
			VectorBasis vectorBasis = default(VectorBasis);
			Vector3** ptr = (Vector3**)(&vectorBasis);
			Matrix matrix = Identity;
			CanonicalBasis canonicalBasis = default(CanonicalBasis);
			Vector3* ptr2 = &canonicalBasis.Row0;
			canonicalBasis.Row0 = new Vector3(1f, 0f, 0f);
			canonicalBasis.Row1 = new Vector3(0f, 1f, 0f);
			canonicalBasis.Row2 = new Vector3(0f, 0f, 1f);
			translation.X = M41;
			translation.Y = M42;
			translation.Z = M43;
			*ptr = (Vector3*)(&matrix.M11);
			ptr[1] = (Vector3*)(&matrix.M21);
			ptr[2] = (Vector3*)(&matrix.M31);
			*(*ptr) = new Vector3(M11, M12, M13);
			*ptr[1] = new Vector3(M21, M22, M23);
			*ptr[2] = new Vector3(M31, M32, M33);
			scale.X = (*ptr)->Length();
			scale.Y = ptr[1]->Length();
			scale.Z = ptr[2]->Length();
			float num = *ptr3;
			float num2 = ptr3[1];
			float num3 = ptr3[2];
			uint num4;
			uint num5;
			uint num6;
			if (num < num2)
			{
				if (num2 < num3)
				{
					num4 = 2u;
					num5 = 1u;
					num6 = 0u;
				}
				else
				{
					num4 = 1u;
					if (num < num3)
					{
						num5 = 2u;
						num6 = 0u;
					}
					else
					{
						num5 = 0u;
						num6 = 2u;
					}
				}
			}
			else if (num < num3)
			{
				num4 = 2u;
				num5 = 0u;
				num6 = 1u;
			}
			else
			{
				num4 = 0u;
				if (num2 < num3)
				{
					num5 = 2u;
					num6 = 1u;
				}
				else
				{
					num5 = 1u;
					num6 = 2u;
				}
			}
			if (ptr3[num4] < 0.0001f)
			{
				*ptr[num4] = ptr2[num4];
			}
			ptr[num4]->Normalize();
			if (ptr3[num5] < 0.0001f)
			{
				float num7 = Math.Abs(ptr[num4]->X);
				float num8 = Math.Abs(ptr[num4]->Y);
				float num9 = Math.Abs(ptr[num4]->Z);
				uint num10 = ((num7 < num8) ? ((!(num8 < num9)) ? ((!(num7 < num9)) ? 2u : 0u) : 0u) : ((num7 < num9) ? 1u : ((num8 < num9) ? 1u : 2u)));
				Vector3.Cross(ref *ptr[num5], ref *ptr[num4], out ptr2[num10]);
			}
			ptr[num5]->Normalize();
			if (ptr3[num6] < 0.0001f)
			{
				Vector3.Cross(ref *ptr[num6], ref *ptr[num4], out *ptr[num5]);
			}
			ptr[num6]->Normalize();
			float num11 = matrix.Determinant();
			if (num11 < 0f)
			{
				ptr3[num4] = 0f - ptr3[num4];
				*ptr[num4] = -(*ptr[num4]);
				num11 = 0f - num11;
			}
			num11 -= 1f;
			num11 *= num11;
			if (0.0001f < num11)
			{
				rotation = Quaternion.Identity;
				result = false;
			}
			else
			{
				Quaternion.CreateFromRotationMatrix(ref matrix, out rotation);
			}
		}
		return result;
	}

	public static Matrix Transform(Matrix value, Quaternion rotation)
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
		Matrix result = default(Matrix);
		result.M11 = value.M11 * num13 + value.M12 * num14 + value.M13 * num15;
		result.M12 = value.M11 * num16 + value.M12 * num17 + value.M13 * num18;
		result.M13 = value.M11 * num19 + value.M12 * num20 + value.M13 * num21;
		result.M14 = value.M14;
		result.M21 = value.M21 * num13 + value.M22 * num14 + value.M23 * num15;
		result.M22 = value.M21 * num16 + value.M22 * num17 + value.M23 * num18;
		result.M23 = value.M21 * num19 + value.M22 * num20 + value.M23 * num21;
		result.M24 = value.M24;
		result.M31 = value.M31 * num13 + value.M32 * num14 + value.M33 * num15;
		result.M32 = value.M31 * num16 + value.M32 * num17 + value.M33 * num18;
		result.M33 = value.M31 * num19 + value.M32 * num20 + value.M33 * num21;
		result.M34 = value.M34;
		result.M41 = value.M41 * num13 + value.M42 * num14 + value.M43 * num15;
		result.M42 = value.M41 * num16 + value.M42 * num17 + value.M43 * num18;
		result.M43 = value.M41 * num19 + value.M42 * num20 + value.M43 * num21;
		result.M44 = value.M44;
		return result;
	}

	public static void Transform(ref Matrix value, ref Quaternion rotation, out Matrix result)
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
		float m = value.M11 * num13 + value.M12 * num14 + value.M13 * num15;
		float m2 = value.M11 * num16 + value.M12 * num17 + value.M13 * num18;
		float m3 = value.M11 * num19 + value.M12 * num20 + value.M13 * num21;
		float m4 = value.M14;
		float m5 = value.M21 * num13 + value.M22 * num14 + value.M23 * num15;
		float m6 = value.M21 * num16 + value.M22 * num17 + value.M23 * num18;
		float m7 = value.M21 * num19 + value.M22 * num20 + value.M23 * num21;
		float m8 = value.M24;
		float m9 = value.M31 * num13 + value.M32 * num14 + value.M33 * num15;
		float m10 = value.M31 * num16 + value.M32 * num17 + value.M33 * num18;
		float m11 = value.M31 * num19 + value.M32 * num20 + value.M33 * num21;
		float m12 = value.M34;
		float m13 = value.M41 * num13 + value.M42 * num14 + value.M43 * num15;
		float m14 = value.M41 * num16 + value.M42 * num17 + value.M43 * num18;
		float m15 = value.M41 * num19 + value.M42 * num20 + value.M43 * num21;
		float m16 = value.M44;
		result.M11 = m;
		result.M12 = m2;
		result.M13 = m3;
		result.M14 = m4;
		result.M21 = m5;
		result.M22 = m6;
		result.M23 = m7;
		result.M24 = m8;
		result.M31 = m9;
		result.M32 = m10;
		result.M33 = m11;
		result.M34 = m12;
		result.M41 = m13;
		result.M42 = m14;
		result.M43 = m15;
		result.M44 = m16;
	}

	public override string ToString()
	{
		CultureInfo currentCulture = CultureInfo.CurrentCulture;
		return "{ " + string.Format(currentCulture, "{{M11:{0} M12:{1} M13:{2} M14:{3}}} ", M11.ToString(currentCulture), M12.ToString(currentCulture), M13.ToString(currentCulture), M14.ToString(currentCulture)) + string.Format(currentCulture, "{{M21:{0} M22:{1} M23:{2} M24:{3}}} ", M21.ToString(currentCulture), M22.ToString(currentCulture), M23.ToString(currentCulture), M24.ToString(currentCulture)) + string.Format(currentCulture, "{{M31:{0} M32:{1} M33:{2} M34:{3}}} ", M31.ToString(currentCulture), M32.ToString(currentCulture), M33.ToString(currentCulture), M34.ToString(currentCulture)) + string.Format(currentCulture, "{{M41:{0} M42:{1} M43:{2} M44:{3}}} ", M41.ToString(currentCulture), M42.ToString(currentCulture), M43.ToString(currentCulture), M44.ToString(currentCulture)) + "}";
	}

	public bool Equals(Matrix other)
	{
		if (M11 == other.M11 && M22 == other.M22 && M33 == other.M33 && M44 == other.M44 && M12 == other.M12 && M13 == other.M13 && M14 == other.M14 && M21 == other.M21 && M23 == other.M23 && M24 == other.M24 && M31 == other.M31 && M32 == other.M32 && M34 == other.M34 && M41 == other.M41 && M42 == other.M42)
		{
			return M43 == other.M43;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		bool result = false;
		if (obj is Matrix)
		{
			result = Equals((Matrix)obj);
		}
		return result;
	}

	public override int GetHashCode()
	{
		return M11.GetHashCode() + M12.GetHashCode() + M13.GetHashCode() + M14.GetHashCode() + M21.GetHashCode() + M22.GetHashCode() + M23.GetHashCode() + M24.GetHashCode() + M31.GetHashCode() + M32.GetHashCode() + M33.GetHashCode() + M34.GetHashCode() + M41.GetHashCode() + M42.GetHashCode() + M43.GetHashCode() + M44.GetHashCode();
	}

	public static Matrix Transpose(Matrix matrix)
	{
		Matrix result = default(Matrix);
		result.M11 = matrix.M11;
		result.M12 = matrix.M21;
		result.M13 = matrix.M31;
		result.M14 = matrix.M41;
		result.M21 = matrix.M12;
		result.M22 = matrix.M22;
		result.M23 = matrix.M32;
		result.M24 = matrix.M42;
		result.M31 = matrix.M13;
		result.M32 = matrix.M23;
		result.M33 = matrix.M33;
		result.M34 = matrix.M43;
		result.M41 = matrix.M14;
		result.M42 = matrix.M24;
		result.M43 = matrix.M34;
		result.M44 = matrix.M44;
		return result;
	}

	public static void Transpose(ref Matrix matrix, out Matrix result)
	{
		float m = matrix.M11;
		float m2 = matrix.M12;
		float m3 = matrix.M13;
		float m4 = matrix.M14;
		float m5 = matrix.M21;
		float m6 = matrix.M22;
		float m7 = matrix.M23;
		float m8 = matrix.M24;
		float m9 = matrix.M31;
		float m10 = matrix.M32;
		float m11 = matrix.M33;
		float m12 = matrix.M34;
		float m13 = matrix.M41;
		float m14 = matrix.M42;
		float m15 = matrix.M43;
		float m16 = matrix.M44;
		result.M11 = m;
		result.M12 = m5;
		result.M13 = m9;
		result.M14 = m13;
		result.M21 = m2;
		result.M22 = m6;
		result.M23 = m10;
		result.M24 = m14;
		result.M31 = m3;
		result.M32 = m7;
		result.M33 = m11;
		result.M34 = m15;
		result.M41 = m4;
		result.M42 = m8;
		result.M43 = m12;
		result.M44 = m16;
	}

	public float Determinant()
	{
		float m = M11;
		float m2 = M12;
		float m3 = M13;
		float m4 = M14;
		float m5 = M21;
		float m6 = M22;
		float m7 = M23;
		float m8 = M24;
		float m9 = M31;
		float m10 = M32;
		float m11 = M33;
		float m12 = M34;
		float m13 = M41;
		float m14 = M42;
		float m15 = M43;
		float m16 = M44;
		float num = m11 * m16 - m12 * m15;
		float num2 = m10 * m16 - m12 * m14;
		float num3 = m10 * m15 - m11 * m14;
		float num4 = m9 * m16 - m12 * m13;
		float num5 = m9 * m15 - m11 * m13;
		float num6 = m9 * m14 - m10 * m13;
		return m * (m6 * num - m7 * num2 + m8 * num3) - m2 * (m5 * num - m7 * num4 + m8 * num5) + m3 * (m5 * num2 - m6 * num4 + m8 * num6) - m4 * (m5 * num3 - m6 * num5 + m7 * num6);
	}

	public static Matrix Invert(Matrix matrix)
	{
		float m = matrix.M11;
		float m2 = matrix.M12;
		float m3 = matrix.M13;
		float m4 = matrix.M14;
		float m5 = matrix.M21;
		float m6 = matrix.M22;
		float m7 = matrix.M23;
		float m8 = matrix.M24;
		float m9 = matrix.M31;
		float m10 = matrix.M32;
		float m11 = matrix.M33;
		float m12 = matrix.M34;
		float m13 = matrix.M41;
		float m14 = matrix.M42;
		float m15 = matrix.M43;
		float m16 = matrix.M44;
		float num = m11 * m16 - m12 * m15;
		float num2 = m10 * m16 - m12 * m14;
		float num3 = m10 * m15 - m11 * m14;
		float num4 = m9 * m16 - m12 * m13;
		float num5 = m9 * m15 - m11 * m13;
		float num6 = m9 * m14 - m10 * m13;
		float num7 = m6 * num - m7 * num2 + m8 * num3;
		float num8 = 0f - (m5 * num - m7 * num4 + m8 * num5);
		float num9 = m5 * num2 - m6 * num4 + m8 * num6;
		float num10 = 0f - (m5 * num3 - m6 * num5 + m7 * num6);
		float num11 = 1f / (m * num7 + m2 * num8 + m3 * num9 + m4 * num10);
		Matrix result = default(Matrix);
		result.M11 = num7 * num11;
		result.M21 = num8 * num11;
		result.M31 = num9 * num11;
		result.M41 = num10 * num11;
		result.M12 = (0f - (m2 * num - m3 * num2 + m4 * num3)) * num11;
		result.M22 = (m * num - m3 * num4 + m4 * num5) * num11;
		result.M32 = (0f - (m * num2 - m2 * num4 + m4 * num6)) * num11;
		result.M42 = (m * num3 - m2 * num5 + m3 * num6) * num11;
		float num12 = m7 * m16 - m8 * m15;
		float num13 = m6 * m16 - m8 * m14;
		float num14 = m6 * m15 - m7 * m14;
		float num15 = m5 * m16 - m8 * m13;
		float num16 = m5 * m15 - m7 * m13;
		float num17 = m5 * m14 - m6 * m13;
		result.M13 = (m2 * num12 - m3 * num13 + m4 * num14) * num11;
		result.M23 = (0f - (m * num12 - m3 * num15 + m4 * num16)) * num11;
		result.M33 = (m * num13 - m2 * num15 + m4 * num17) * num11;
		result.M43 = (0f - (m * num14 - m2 * num16 + m3 * num17)) * num11;
		float num18 = m7 * m12 - m8 * m11;
		float num19 = m6 * m12 - m8 * m10;
		float num20 = m6 * m11 - m7 * m10;
		float num21 = m5 * m12 - m8 * m9;
		float num22 = m5 * m11 - m7 * m9;
		float num23 = m5 * m10 - m6 * m9;
		result.M14 = (0f - (m2 * num18 - m3 * num19 + m4 * num20)) * num11;
		result.M24 = (m * num18 - m3 * num21 + m4 * num22) * num11;
		result.M34 = (0f - (m * num19 - m2 * num21 + m4 * num23)) * num11;
		result.M44 = (m * num20 - m2 * num22 + m3 * num23) * num11;
		return result;
	}

	public static void Invert(ref Matrix matrix, out Matrix result)
	{
		float m = matrix.M11;
		float m2 = matrix.M12;
		float m3 = matrix.M13;
		float m4 = matrix.M14;
		float m5 = matrix.M21;
		float m6 = matrix.M22;
		float m7 = matrix.M23;
		float m8 = matrix.M24;
		float m9 = matrix.M31;
		float m10 = matrix.M32;
		float m11 = matrix.M33;
		float m12 = matrix.M34;
		float m13 = matrix.M41;
		float m14 = matrix.M42;
		float m15 = matrix.M43;
		float m16 = matrix.M44;
		float num = m11 * m16 - m12 * m15;
		float num2 = m10 * m16 - m12 * m14;
		float num3 = m10 * m15 - m11 * m14;
		float num4 = m9 * m16 - m12 * m13;
		float num5 = m9 * m15 - m11 * m13;
		float num6 = m9 * m14 - m10 * m13;
		float num7 = m6 * num - m7 * num2 + m8 * num3;
		float num8 = 0f - (m5 * num - m7 * num4 + m8 * num5);
		float num9 = m5 * num2 - m6 * num4 + m8 * num6;
		float num10 = 0f - (m5 * num3 - m6 * num5 + m7 * num6);
		float num11 = 1f / (m * num7 + m2 * num8 + m3 * num9 + m4 * num10);
		result.M11 = num7 * num11;
		result.M21 = num8 * num11;
		result.M31 = num9 * num11;
		result.M41 = num10 * num11;
		result.M12 = (0f - (m2 * num - m3 * num2 + m4 * num3)) * num11;
		result.M22 = (m * num - m3 * num4 + m4 * num5) * num11;
		result.M32 = (0f - (m * num2 - m2 * num4 + m4 * num6)) * num11;
		result.M42 = (m * num3 - m2 * num5 + m3 * num6) * num11;
		float num12 = m7 * m16 - m8 * m15;
		float num13 = m6 * m16 - m8 * m14;
		float num14 = m6 * m15 - m7 * m14;
		float num15 = m5 * m16 - m8 * m13;
		float num16 = m5 * m15 - m7 * m13;
		float num17 = m5 * m14 - m6 * m13;
		result.M13 = (m2 * num12 - m3 * num13 + m4 * num14) * num11;
		result.M23 = (0f - (m * num12 - m3 * num15 + m4 * num16)) * num11;
		result.M33 = (m * num13 - m2 * num15 + m4 * num17) * num11;
		result.M43 = (0f - (m * num14 - m2 * num16 + m3 * num17)) * num11;
		float num18 = m7 * m12 - m8 * m11;
		float num19 = m6 * m12 - m8 * m10;
		float num20 = m6 * m11 - m7 * m10;
		float num21 = m5 * m12 - m8 * m9;
		float num22 = m5 * m11 - m7 * m9;
		float num23 = m5 * m10 - m6 * m9;
		result.M14 = (0f - (m2 * num18 - m3 * num19 + m4 * num20)) * num11;
		result.M24 = (m * num18 - m3 * num21 + m4 * num22) * num11;
		result.M34 = (0f - (m * num19 - m2 * num21 + m4 * num23)) * num11;
		result.M44 = (m * num20 - m2 * num22 + m3 * num23) * num11;
	}

	public static Matrix Lerp(Matrix matrix1, Matrix matrix2, float amount)
	{
		Matrix result = default(Matrix);
		result.M11 = matrix1.M11 + (matrix2.M11 - matrix1.M11) * amount;
		result.M12 = matrix1.M12 + (matrix2.M12 - matrix1.M12) * amount;
		result.M13 = matrix1.M13 + (matrix2.M13 - matrix1.M13) * amount;
		result.M14 = matrix1.M14 + (matrix2.M14 - matrix1.M14) * amount;
		result.M21 = matrix1.M21 + (matrix2.M21 - matrix1.M21) * amount;
		result.M22 = matrix1.M22 + (matrix2.M22 - matrix1.M22) * amount;
		result.M23 = matrix1.M23 + (matrix2.M23 - matrix1.M23) * amount;
		result.M24 = matrix1.M24 + (matrix2.M24 - matrix1.M24) * amount;
		result.M31 = matrix1.M31 + (matrix2.M31 - matrix1.M31) * amount;
		result.M32 = matrix1.M32 + (matrix2.M32 - matrix1.M32) * amount;
		result.M33 = matrix1.M33 + (matrix2.M33 - matrix1.M33) * amount;
		result.M34 = matrix1.M34 + (matrix2.M34 - matrix1.M34) * amount;
		result.M41 = matrix1.M41 + (matrix2.M41 - matrix1.M41) * amount;
		result.M42 = matrix1.M42 + (matrix2.M42 - matrix1.M42) * amount;
		result.M43 = matrix1.M43 + (matrix2.M43 - matrix1.M43) * amount;
		result.M44 = matrix1.M44 + (matrix2.M44 - matrix1.M44) * amount;
		return result;
	}

	public static void Lerp(ref Matrix matrix1, ref Matrix matrix2, float amount, out Matrix result)
	{
		result.M11 = matrix1.M11 + (matrix2.M11 - matrix1.M11) * amount;
		result.M12 = matrix1.M12 + (matrix2.M12 - matrix1.M12) * amount;
		result.M13 = matrix1.M13 + (matrix2.M13 - matrix1.M13) * amount;
		result.M14 = matrix1.M14 + (matrix2.M14 - matrix1.M14) * amount;
		result.M21 = matrix1.M21 + (matrix2.M21 - matrix1.M21) * amount;
		result.M22 = matrix1.M22 + (matrix2.M22 - matrix1.M22) * amount;
		result.M23 = matrix1.M23 + (matrix2.M23 - matrix1.M23) * amount;
		result.M24 = matrix1.M24 + (matrix2.M24 - matrix1.M24) * amount;
		result.M31 = matrix1.M31 + (matrix2.M31 - matrix1.M31) * amount;
		result.M32 = matrix1.M32 + (matrix2.M32 - matrix1.M32) * amount;
		result.M33 = matrix1.M33 + (matrix2.M33 - matrix1.M33) * amount;
		result.M34 = matrix1.M34 + (matrix2.M34 - matrix1.M34) * amount;
		result.M41 = matrix1.M41 + (matrix2.M41 - matrix1.M41) * amount;
		result.M42 = matrix1.M42 + (matrix2.M42 - matrix1.M42) * amount;
		result.M43 = matrix1.M43 + (matrix2.M43 - matrix1.M43) * amount;
		result.M44 = matrix1.M44 + (matrix2.M44 - matrix1.M44) * amount;
	}

	public static Matrix Negate(Matrix matrix)
	{
		Matrix result = default(Matrix);
		result.M11 = 0f - matrix.M11;
		result.M12 = 0f - matrix.M12;
		result.M13 = 0f - matrix.M13;
		result.M14 = 0f - matrix.M14;
		result.M21 = 0f - matrix.M21;
		result.M22 = 0f - matrix.M22;
		result.M23 = 0f - matrix.M23;
		result.M24 = 0f - matrix.M24;
		result.M31 = 0f - matrix.M31;
		result.M32 = 0f - matrix.M32;
		result.M33 = 0f - matrix.M33;
		result.M34 = 0f - matrix.M34;
		result.M41 = 0f - matrix.M41;
		result.M42 = 0f - matrix.M42;
		result.M43 = 0f - matrix.M43;
		result.M44 = 0f - matrix.M44;
		return result;
	}

	public static void Negate(ref Matrix matrix, out Matrix result)
	{
		result.M11 = 0f - matrix.M11;
		result.M12 = 0f - matrix.M12;
		result.M13 = 0f - matrix.M13;
		result.M14 = 0f - matrix.M14;
		result.M21 = 0f - matrix.M21;
		result.M22 = 0f - matrix.M22;
		result.M23 = 0f - matrix.M23;
		result.M24 = 0f - matrix.M24;
		result.M31 = 0f - matrix.M31;
		result.M32 = 0f - matrix.M32;
		result.M33 = 0f - matrix.M33;
		result.M34 = 0f - matrix.M34;
		result.M41 = 0f - matrix.M41;
		result.M42 = 0f - matrix.M42;
		result.M43 = 0f - matrix.M43;
		result.M44 = 0f - matrix.M44;
	}

	public static Matrix Add(Matrix matrix1, Matrix matrix2)
	{
		Matrix result = default(Matrix);
		result.M11 = matrix1.M11 + matrix2.M11;
		result.M12 = matrix1.M12 + matrix2.M12;
		result.M13 = matrix1.M13 + matrix2.M13;
		result.M14 = matrix1.M14 + matrix2.M14;
		result.M21 = matrix1.M21 + matrix2.M21;
		result.M22 = matrix1.M22 + matrix2.M22;
		result.M23 = matrix1.M23 + matrix2.M23;
		result.M24 = matrix1.M24 + matrix2.M24;
		result.M31 = matrix1.M31 + matrix2.M31;
		result.M32 = matrix1.M32 + matrix2.M32;
		result.M33 = matrix1.M33 + matrix2.M33;
		result.M34 = matrix1.M34 + matrix2.M34;
		result.M41 = matrix1.M41 + matrix2.M41;
		result.M42 = matrix1.M42 + matrix2.M42;
		result.M43 = matrix1.M43 + matrix2.M43;
		result.M44 = matrix1.M44 + matrix2.M44;
		return result;
	}

	public static void Add(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
	{
		result.M11 = matrix1.M11 + matrix2.M11;
		result.M12 = matrix1.M12 + matrix2.M12;
		result.M13 = matrix1.M13 + matrix2.M13;
		result.M14 = matrix1.M14 + matrix2.M14;
		result.M21 = matrix1.M21 + matrix2.M21;
		result.M22 = matrix1.M22 + matrix2.M22;
		result.M23 = matrix1.M23 + matrix2.M23;
		result.M24 = matrix1.M24 + matrix2.M24;
		result.M31 = matrix1.M31 + matrix2.M31;
		result.M32 = matrix1.M32 + matrix2.M32;
		result.M33 = matrix1.M33 + matrix2.M33;
		result.M34 = matrix1.M34 + matrix2.M34;
		result.M41 = matrix1.M41 + matrix2.M41;
		result.M42 = matrix1.M42 + matrix2.M42;
		result.M43 = matrix1.M43 + matrix2.M43;
		result.M44 = matrix1.M44 + matrix2.M44;
	}

	public static Matrix Subtract(Matrix matrix1, Matrix matrix2)
	{
		Matrix result = default(Matrix);
		result.M11 = matrix1.M11 - matrix2.M11;
		result.M12 = matrix1.M12 - matrix2.M12;
		result.M13 = matrix1.M13 - matrix2.M13;
		result.M14 = matrix1.M14 - matrix2.M14;
		result.M21 = matrix1.M21 - matrix2.M21;
		result.M22 = matrix1.M22 - matrix2.M22;
		result.M23 = matrix1.M23 - matrix2.M23;
		result.M24 = matrix1.M24 - matrix2.M24;
		result.M31 = matrix1.M31 - matrix2.M31;
		result.M32 = matrix1.M32 - matrix2.M32;
		result.M33 = matrix1.M33 - matrix2.M33;
		result.M34 = matrix1.M34 - matrix2.M34;
		result.M41 = matrix1.M41 - matrix2.M41;
		result.M42 = matrix1.M42 - matrix2.M42;
		result.M43 = matrix1.M43 - matrix2.M43;
		result.M44 = matrix1.M44 - matrix2.M44;
		return result;
	}

	public static void Subtract(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
	{
		result.M11 = matrix1.M11 - matrix2.M11;
		result.M12 = matrix1.M12 - matrix2.M12;
		result.M13 = matrix1.M13 - matrix2.M13;
		result.M14 = matrix1.M14 - matrix2.M14;
		result.M21 = matrix1.M21 - matrix2.M21;
		result.M22 = matrix1.M22 - matrix2.M22;
		result.M23 = matrix1.M23 - matrix2.M23;
		result.M24 = matrix1.M24 - matrix2.M24;
		result.M31 = matrix1.M31 - matrix2.M31;
		result.M32 = matrix1.M32 - matrix2.M32;
		result.M33 = matrix1.M33 - matrix2.M33;
		result.M34 = matrix1.M34 - matrix2.M34;
		result.M41 = matrix1.M41 - matrix2.M41;
		result.M42 = matrix1.M42 - matrix2.M42;
		result.M43 = matrix1.M43 - matrix2.M43;
		result.M44 = matrix1.M44 - matrix2.M44;
	}

	public static Matrix Multiply(Matrix matrix1, Matrix matrix2)
	{
		Matrix result = default(Matrix);
		result.M11 = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31 + matrix1.M14 * matrix2.M41;
		result.M12 = matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32 + matrix1.M14 * matrix2.M42;
		result.M13 = matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33 + matrix1.M14 * matrix2.M43;
		result.M14 = matrix1.M11 * matrix2.M14 + matrix1.M12 * matrix2.M24 + matrix1.M13 * matrix2.M34 + matrix1.M14 * matrix2.M44;
		result.M21 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31 + matrix1.M24 * matrix2.M41;
		result.M22 = matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32 + matrix1.M24 * matrix2.M42;
		result.M23 = matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33 + matrix1.M24 * matrix2.M43;
		result.M24 = matrix1.M21 * matrix2.M14 + matrix1.M22 * matrix2.M24 + matrix1.M23 * matrix2.M34 + matrix1.M24 * matrix2.M44;
		result.M31 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31 + matrix1.M34 * matrix2.M41;
		result.M32 = matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32 + matrix1.M34 * matrix2.M42;
		result.M33 = matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33 + matrix1.M34 * matrix2.M43;
		result.M34 = matrix1.M31 * matrix2.M14 + matrix1.M32 * matrix2.M24 + matrix1.M33 * matrix2.M34 + matrix1.M34 * matrix2.M44;
		result.M41 = matrix1.M41 * matrix2.M11 + matrix1.M42 * matrix2.M21 + matrix1.M43 * matrix2.M31 + matrix1.M44 * matrix2.M41;
		result.M42 = matrix1.M41 * matrix2.M12 + matrix1.M42 * matrix2.M22 + matrix1.M43 * matrix2.M32 + matrix1.M44 * matrix2.M42;
		result.M43 = matrix1.M41 * matrix2.M13 + matrix1.M42 * matrix2.M23 + matrix1.M43 * matrix2.M33 + matrix1.M44 * matrix2.M43;
		result.M44 = matrix1.M41 * matrix2.M14 + matrix1.M42 * matrix2.M24 + matrix1.M43 * matrix2.M34 + matrix1.M44 * matrix2.M44;
		return result;
	}

	public static void Multiply(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
	{
		float m = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31 + matrix1.M14 * matrix2.M41;
		float m2 = matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32 + matrix1.M14 * matrix2.M42;
		float m3 = matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33 + matrix1.M14 * matrix2.M43;
		float m4 = matrix1.M11 * matrix2.M14 + matrix1.M12 * matrix2.M24 + matrix1.M13 * matrix2.M34 + matrix1.M14 * matrix2.M44;
		float m5 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31 + matrix1.M24 * matrix2.M41;
		float m6 = matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32 + matrix1.M24 * matrix2.M42;
		float m7 = matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33 + matrix1.M24 * matrix2.M43;
		float m8 = matrix1.M21 * matrix2.M14 + matrix1.M22 * matrix2.M24 + matrix1.M23 * matrix2.M34 + matrix1.M24 * matrix2.M44;
		float m9 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31 + matrix1.M34 * matrix2.M41;
		float m10 = matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32 + matrix1.M34 * matrix2.M42;
		float m11 = matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33 + matrix1.M34 * matrix2.M43;
		float m12 = matrix1.M31 * matrix2.M14 + matrix1.M32 * matrix2.M24 + matrix1.M33 * matrix2.M34 + matrix1.M34 * matrix2.M44;
		float m13 = matrix1.M41 * matrix2.M11 + matrix1.M42 * matrix2.M21 + matrix1.M43 * matrix2.M31 + matrix1.M44 * matrix2.M41;
		float m14 = matrix1.M41 * matrix2.M12 + matrix1.M42 * matrix2.M22 + matrix1.M43 * matrix2.M32 + matrix1.M44 * matrix2.M42;
		float m15 = matrix1.M41 * matrix2.M13 + matrix1.M42 * matrix2.M23 + matrix1.M43 * matrix2.M33 + matrix1.M44 * matrix2.M43;
		float m16 = matrix1.M41 * matrix2.M14 + matrix1.M42 * matrix2.M24 + matrix1.M43 * matrix2.M34 + matrix1.M44 * matrix2.M44;
		result.M11 = m;
		result.M12 = m2;
		result.M13 = m3;
		result.M14 = m4;
		result.M21 = m5;
		result.M22 = m6;
		result.M23 = m7;
		result.M24 = m8;
		result.M31 = m9;
		result.M32 = m10;
		result.M33 = m11;
		result.M34 = m12;
		result.M41 = m13;
		result.M42 = m14;
		result.M43 = m15;
		result.M44 = m16;
	}

	public static Matrix Multiply(Matrix matrix1, float scaleFactor)
	{
		Matrix result = default(Matrix);
		result.M11 = matrix1.M11 * scaleFactor;
		result.M12 = matrix1.M12 * scaleFactor;
		result.M13 = matrix1.M13 * scaleFactor;
		result.M14 = matrix1.M14 * scaleFactor;
		result.M21 = matrix1.M21 * scaleFactor;
		result.M22 = matrix1.M22 * scaleFactor;
		result.M23 = matrix1.M23 * scaleFactor;
		result.M24 = matrix1.M24 * scaleFactor;
		result.M31 = matrix1.M31 * scaleFactor;
		result.M32 = matrix1.M32 * scaleFactor;
		result.M33 = matrix1.M33 * scaleFactor;
		result.M34 = matrix1.M34 * scaleFactor;
		result.M41 = matrix1.M41 * scaleFactor;
		result.M42 = matrix1.M42 * scaleFactor;
		result.M43 = matrix1.M43 * scaleFactor;
		result.M44 = matrix1.M44 * scaleFactor;
		return result;
	}

	public static void Multiply(ref Matrix matrix1, float scaleFactor, out Matrix result)
	{
		result.M11 = matrix1.M11 * scaleFactor;
		result.M12 = matrix1.M12 * scaleFactor;
		result.M13 = matrix1.M13 * scaleFactor;
		result.M14 = matrix1.M14 * scaleFactor;
		result.M21 = matrix1.M21 * scaleFactor;
		result.M22 = matrix1.M22 * scaleFactor;
		result.M23 = matrix1.M23 * scaleFactor;
		result.M24 = matrix1.M24 * scaleFactor;
		result.M31 = matrix1.M31 * scaleFactor;
		result.M32 = matrix1.M32 * scaleFactor;
		result.M33 = matrix1.M33 * scaleFactor;
		result.M34 = matrix1.M34 * scaleFactor;
		result.M41 = matrix1.M41 * scaleFactor;
		result.M42 = matrix1.M42 * scaleFactor;
		result.M43 = matrix1.M43 * scaleFactor;
		result.M44 = matrix1.M44 * scaleFactor;
	}

	public static Matrix Divide(Matrix matrix1, Matrix matrix2)
	{
		Matrix result = default(Matrix);
		result.M11 = matrix1.M11 / matrix2.M11;
		result.M12 = matrix1.M12 / matrix2.M12;
		result.M13 = matrix1.M13 / matrix2.M13;
		result.M14 = matrix1.M14 / matrix2.M14;
		result.M21 = matrix1.M21 / matrix2.M21;
		result.M22 = matrix1.M22 / matrix2.M22;
		result.M23 = matrix1.M23 / matrix2.M23;
		result.M24 = matrix1.M24 / matrix2.M24;
		result.M31 = matrix1.M31 / matrix2.M31;
		result.M32 = matrix1.M32 / matrix2.M32;
		result.M33 = matrix1.M33 / matrix2.M33;
		result.M34 = matrix1.M34 / matrix2.M34;
		result.M41 = matrix1.M41 / matrix2.M41;
		result.M42 = matrix1.M42 / matrix2.M42;
		result.M43 = matrix1.M43 / matrix2.M43;
		result.M44 = matrix1.M44 / matrix2.M44;
		return result;
	}

	public static void Divide(ref Matrix matrix1, ref Matrix matrix2, out Matrix result)
	{
		result.M11 = matrix1.M11 / matrix2.M11;
		result.M12 = matrix1.M12 / matrix2.M12;
		result.M13 = matrix1.M13 / matrix2.M13;
		result.M14 = matrix1.M14 / matrix2.M14;
		result.M21 = matrix1.M21 / matrix2.M21;
		result.M22 = matrix1.M22 / matrix2.M22;
		result.M23 = matrix1.M23 / matrix2.M23;
		result.M24 = matrix1.M24 / matrix2.M24;
		result.M31 = matrix1.M31 / matrix2.M31;
		result.M32 = matrix1.M32 / matrix2.M32;
		result.M33 = matrix1.M33 / matrix2.M33;
		result.M34 = matrix1.M34 / matrix2.M34;
		result.M41 = matrix1.M41 / matrix2.M41;
		result.M42 = matrix1.M42 / matrix2.M42;
		result.M43 = matrix1.M43 / matrix2.M43;
		result.M44 = matrix1.M44 / matrix2.M44;
	}

	public static Matrix Divide(Matrix matrix1, float divider)
	{
		float num = 1f / divider;
		Matrix result = default(Matrix);
		result.M11 = matrix1.M11 * num;
		result.M12 = matrix1.M12 * num;
		result.M13 = matrix1.M13 * num;
		result.M14 = matrix1.M14 * num;
		result.M21 = matrix1.M21 * num;
		result.M22 = matrix1.M22 * num;
		result.M23 = matrix1.M23 * num;
		result.M24 = matrix1.M24 * num;
		result.M31 = matrix1.M31 * num;
		result.M32 = matrix1.M32 * num;
		result.M33 = matrix1.M33 * num;
		result.M34 = matrix1.M34 * num;
		result.M41 = matrix1.M41 * num;
		result.M42 = matrix1.M42 * num;
		result.M43 = matrix1.M43 * num;
		result.M44 = matrix1.M44 * num;
		return result;
	}

	public static void Divide(ref Matrix matrix1, float divider, out Matrix result)
	{
		float num = 1f / divider;
		result.M11 = matrix1.M11 * num;
		result.M12 = matrix1.M12 * num;
		result.M13 = matrix1.M13 * num;
		result.M14 = matrix1.M14 * num;
		result.M21 = matrix1.M21 * num;
		result.M22 = matrix1.M22 * num;
		result.M23 = matrix1.M23 * num;
		result.M24 = matrix1.M24 * num;
		result.M31 = matrix1.M31 * num;
		result.M32 = matrix1.M32 * num;
		result.M33 = matrix1.M33 * num;
		result.M34 = matrix1.M34 * num;
		result.M41 = matrix1.M41 * num;
		result.M42 = matrix1.M42 * num;
		result.M43 = matrix1.M43 * num;
		result.M44 = matrix1.M44 * num;
	}

	public static Matrix operator -(Matrix matrix1)
	{
		Matrix result = default(Matrix);
		result.M11 = 0f - matrix1.M11;
		result.M12 = 0f - matrix1.M12;
		result.M13 = 0f - matrix1.M13;
		result.M14 = 0f - matrix1.M14;
		result.M21 = 0f - matrix1.M21;
		result.M22 = 0f - matrix1.M22;
		result.M23 = 0f - matrix1.M23;
		result.M24 = 0f - matrix1.M24;
		result.M31 = 0f - matrix1.M31;
		result.M32 = 0f - matrix1.M32;
		result.M33 = 0f - matrix1.M33;
		result.M34 = 0f - matrix1.M34;
		result.M41 = 0f - matrix1.M41;
		result.M42 = 0f - matrix1.M42;
		result.M43 = 0f - matrix1.M43;
		result.M44 = 0f - matrix1.M44;
		return result;
	}

	public static bool operator ==(Matrix matrix1, Matrix matrix2)
	{
		if (matrix1.M11 == matrix2.M11 && matrix1.M22 == matrix2.M22 && matrix1.M33 == matrix2.M33 && matrix1.M44 == matrix2.M44 && matrix1.M12 == matrix2.M12 && matrix1.M13 == matrix2.M13 && matrix1.M14 == matrix2.M14 && matrix1.M21 == matrix2.M21 && matrix1.M23 == matrix2.M23 && matrix1.M24 == matrix2.M24 && matrix1.M31 == matrix2.M31 && matrix1.M32 == matrix2.M32 && matrix1.M34 == matrix2.M34 && matrix1.M41 == matrix2.M41 && matrix1.M42 == matrix2.M42)
		{
			return matrix1.M43 == matrix2.M43;
		}
		return false;
	}

	public static bool operator !=(Matrix matrix1, Matrix matrix2)
	{
		if (matrix1.M11 == matrix2.M11 && matrix1.M12 == matrix2.M12 && matrix1.M13 == matrix2.M13 && matrix1.M14 == matrix2.M14 && matrix1.M21 == matrix2.M21 && matrix1.M22 == matrix2.M22 && matrix1.M23 == matrix2.M23 && matrix1.M24 == matrix2.M24 && matrix1.M31 == matrix2.M31 && matrix1.M32 == matrix2.M32 && matrix1.M33 == matrix2.M33 && matrix1.M34 == matrix2.M34 && matrix1.M41 == matrix2.M41 && matrix1.M42 == matrix2.M42 && matrix1.M43 == matrix2.M43)
		{
			return matrix1.M44 != matrix2.M44;
		}
		return true;
	}

	public static Matrix operator +(Matrix matrix1, Matrix matrix2)
	{
		Matrix result = default(Matrix);
		result.M11 = matrix1.M11 + matrix2.M11;
		result.M12 = matrix1.M12 + matrix2.M12;
		result.M13 = matrix1.M13 + matrix2.M13;
		result.M14 = matrix1.M14 + matrix2.M14;
		result.M21 = matrix1.M21 + matrix2.M21;
		result.M22 = matrix1.M22 + matrix2.M22;
		result.M23 = matrix1.M23 + matrix2.M23;
		result.M24 = matrix1.M24 + matrix2.M24;
		result.M31 = matrix1.M31 + matrix2.M31;
		result.M32 = matrix1.M32 + matrix2.M32;
		result.M33 = matrix1.M33 + matrix2.M33;
		result.M34 = matrix1.M34 + matrix2.M34;
		result.M41 = matrix1.M41 + matrix2.M41;
		result.M42 = matrix1.M42 + matrix2.M42;
		result.M43 = matrix1.M43 + matrix2.M43;
		result.M44 = matrix1.M44 + matrix2.M44;
		return result;
	}

	public static Matrix operator -(Matrix matrix1, Matrix matrix2)
	{
		Matrix result = default(Matrix);
		result.M11 = matrix1.M11 - matrix2.M11;
		result.M12 = matrix1.M12 - matrix2.M12;
		result.M13 = matrix1.M13 - matrix2.M13;
		result.M14 = matrix1.M14 - matrix2.M14;
		result.M21 = matrix1.M21 - matrix2.M21;
		result.M22 = matrix1.M22 - matrix2.M22;
		result.M23 = matrix1.M23 - matrix2.M23;
		result.M24 = matrix1.M24 - matrix2.M24;
		result.M31 = matrix1.M31 - matrix2.M31;
		result.M32 = matrix1.M32 - matrix2.M32;
		result.M33 = matrix1.M33 - matrix2.M33;
		result.M34 = matrix1.M34 - matrix2.M34;
		result.M41 = matrix1.M41 - matrix2.M41;
		result.M42 = matrix1.M42 - matrix2.M42;
		result.M43 = matrix1.M43 - matrix2.M43;
		result.M44 = matrix1.M44 - matrix2.M44;
		return result;
	}

	public static Matrix operator *(Matrix matrix1, Matrix matrix2)
	{
		Matrix result = default(Matrix);
		result.M11 = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31 + matrix1.M14 * matrix2.M41;
		result.M12 = matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32 + matrix1.M14 * matrix2.M42;
		result.M13 = matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33 + matrix1.M14 * matrix2.M43;
		result.M14 = matrix1.M11 * matrix2.M14 + matrix1.M12 * matrix2.M24 + matrix1.M13 * matrix2.M34 + matrix1.M14 * matrix2.M44;
		result.M21 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31 + matrix1.M24 * matrix2.M41;
		result.M22 = matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32 + matrix1.M24 * matrix2.M42;
		result.M23 = matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33 + matrix1.M24 * matrix2.M43;
		result.M24 = matrix1.M21 * matrix2.M14 + matrix1.M22 * matrix2.M24 + matrix1.M23 * matrix2.M34 + matrix1.M24 * matrix2.M44;
		result.M31 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31 + matrix1.M34 * matrix2.M41;
		result.M32 = matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32 + matrix1.M34 * matrix2.M42;
		result.M33 = matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33 + matrix1.M34 * matrix2.M43;
		result.M34 = matrix1.M31 * matrix2.M14 + matrix1.M32 * matrix2.M24 + matrix1.M33 * matrix2.M34 + matrix1.M34 * matrix2.M44;
		result.M41 = matrix1.M41 * matrix2.M11 + matrix1.M42 * matrix2.M21 + matrix1.M43 * matrix2.M31 + matrix1.M44 * matrix2.M41;
		result.M42 = matrix1.M41 * matrix2.M12 + matrix1.M42 * matrix2.M22 + matrix1.M43 * matrix2.M32 + matrix1.M44 * matrix2.M42;
		result.M43 = matrix1.M41 * matrix2.M13 + matrix1.M42 * matrix2.M23 + matrix1.M43 * matrix2.M33 + matrix1.M44 * matrix2.M43;
		result.M44 = matrix1.M41 * matrix2.M14 + matrix1.M42 * matrix2.M24 + matrix1.M43 * matrix2.M34 + matrix1.M44 * matrix2.M44;
		return result;
	}

	public static Matrix operator *(Matrix matrix, float scaleFactor)
	{
		Matrix result = default(Matrix);
		result.M11 = matrix.M11 * scaleFactor;
		result.M12 = matrix.M12 * scaleFactor;
		result.M13 = matrix.M13 * scaleFactor;
		result.M14 = matrix.M14 * scaleFactor;
		result.M21 = matrix.M21 * scaleFactor;
		result.M22 = matrix.M22 * scaleFactor;
		result.M23 = matrix.M23 * scaleFactor;
		result.M24 = matrix.M24 * scaleFactor;
		result.M31 = matrix.M31 * scaleFactor;
		result.M32 = matrix.M32 * scaleFactor;
		result.M33 = matrix.M33 * scaleFactor;
		result.M34 = matrix.M34 * scaleFactor;
		result.M41 = matrix.M41 * scaleFactor;
		result.M42 = matrix.M42 * scaleFactor;
		result.M43 = matrix.M43 * scaleFactor;
		result.M44 = matrix.M44 * scaleFactor;
		return result;
	}

	public static Matrix operator *(float scaleFactor, Matrix matrix)
	{
		Matrix result = default(Matrix);
		result.M11 = matrix.M11 * scaleFactor;
		result.M12 = matrix.M12 * scaleFactor;
		result.M13 = matrix.M13 * scaleFactor;
		result.M14 = matrix.M14 * scaleFactor;
		result.M21 = matrix.M21 * scaleFactor;
		result.M22 = matrix.M22 * scaleFactor;
		result.M23 = matrix.M23 * scaleFactor;
		result.M24 = matrix.M24 * scaleFactor;
		result.M31 = matrix.M31 * scaleFactor;
		result.M32 = matrix.M32 * scaleFactor;
		result.M33 = matrix.M33 * scaleFactor;
		result.M34 = matrix.M34 * scaleFactor;
		result.M41 = matrix.M41 * scaleFactor;
		result.M42 = matrix.M42 * scaleFactor;
		result.M43 = matrix.M43 * scaleFactor;
		result.M44 = matrix.M44 * scaleFactor;
		return result;
	}

	public static Matrix operator /(Matrix matrix1, Matrix matrix2)
	{
		Matrix result = default(Matrix);
		result.M11 = matrix1.M11 / matrix2.M11;
		result.M12 = matrix1.M12 / matrix2.M12;
		result.M13 = matrix1.M13 / matrix2.M13;
		result.M14 = matrix1.M14 / matrix2.M14;
		result.M21 = matrix1.M21 / matrix2.M21;
		result.M22 = matrix1.M22 / matrix2.M22;
		result.M23 = matrix1.M23 / matrix2.M23;
		result.M24 = matrix1.M24 / matrix2.M24;
		result.M31 = matrix1.M31 / matrix2.M31;
		result.M32 = matrix1.M32 / matrix2.M32;
		result.M33 = matrix1.M33 / matrix2.M33;
		result.M34 = matrix1.M34 / matrix2.M34;
		result.M41 = matrix1.M41 / matrix2.M41;
		result.M42 = matrix1.M42 / matrix2.M42;
		result.M43 = matrix1.M43 / matrix2.M43;
		result.M44 = matrix1.M44 / matrix2.M44;
		return result;
	}

	public static Matrix operator /(Matrix matrix1, float divider)
	{
		float num = 1f / divider;
		Matrix result = default(Matrix);
		result.M11 = matrix1.M11 * num;
		result.M12 = matrix1.M12 * num;
		result.M13 = matrix1.M13 * num;
		result.M14 = matrix1.M14 * num;
		result.M21 = matrix1.M21 * num;
		result.M22 = matrix1.M22 * num;
		result.M23 = matrix1.M23 * num;
		result.M24 = matrix1.M24 * num;
		result.M31 = matrix1.M31 * num;
		result.M32 = matrix1.M32 * num;
		result.M33 = matrix1.M33 * num;
		result.M34 = matrix1.M34 * num;
		result.M41 = matrix1.M41 * num;
		result.M42 = matrix1.M42 * num;
		result.M43 = matrix1.M43 * num;
		result.M44 = matrix1.M44 * num;
		return result;
	}
}
