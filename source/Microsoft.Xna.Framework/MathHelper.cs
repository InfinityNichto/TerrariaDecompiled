using System;

namespace Microsoft.Xna.Framework;

public static class MathHelper
{
	public const float E = (float)Math.E;

	public const float Log2E = 1.442695f;

	public const float Log10E = 0.4342945f;

	public const float Pi = (float)Math.PI;

	public const float TwoPi = (float)Math.PI * 2f;

	public const float PiOver2 = (float)Math.PI / 2f;

	public const float PiOver4 = (float)Math.PI / 4f;

	public static float ToRadians(float degrees)
	{
		return degrees * ((float)Math.PI / 180f);
	}

	public static float ToDegrees(float radians)
	{
		return radians * (180f / (float)Math.PI);
	}

	public static float Distance(float value1, float value2)
	{
		return Math.Abs(value1 - value2);
	}

	public static float Min(float value1, float value2)
	{
		return Math.Min(value1, value2);
	}

	public static float Max(float value1, float value2)
	{
		return Math.Max(value1, value2);
	}

	public static float Clamp(float value, float min, float max)
	{
		value = ((value > max) ? max : value);
		value = ((value < min) ? min : value);
		return value;
	}

	public static float Lerp(float value1, float value2, float amount)
	{
		return value1 + (value2 - value1) * amount;
	}

	public static float Barycentric(float value1, float value2, float value3, float amount1, float amount2)
	{
		return value1 + amount1 * (value2 - value1) + amount2 * (value3 - value1);
	}

	public static float SmoothStep(float value1, float value2, float amount)
	{
		float num = Clamp(amount, 0f, 1f);
		return Lerp(value1, value2, num * num * (3f - 2f * num));
	}

	public static float CatmullRom(float value1, float value2, float value3, float value4, float amount)
	{
		float num = amount * amount;
		float num2 = amount * num;
		return 0.5f * (2f * value2 + (0f - value1 + value3) * amount + (2f * value1 - 5f * value2 + 4f * value3 - value4) * num + (0f - value1 + 3f * value2 - 3f * value3 + value4) * num2);
	}

	public static float Hermite(float value1, float tangent1, float value2, float tangent2, float amount)
	{
		float num = amount * amount;
		float num2 = amount * num;
		float num3 = 2f * num2 - 3f * num + 1f;
		float num4 = -2f * num2 + 3f * num;
		float num5 = num2 - 2f * num + amount;
		float num6 = num2 - num;
		return value1 * num3 + value2 * num4 + tangent1 * num5 + tangent2 * num6;
	}

	public static float WrapAngle(float angle)
	{
		angle = (float)Math.IEEERemainder(angle, 6.2831854820251465);
		if (angle <= -(float)Math.PI)
		{
			angle += (float)Math.PI * 2f;
		}
		else if (angle > (float)Math.PI)
		{
			angle -= (float)Math.PI * 2f;
		}
		return angle;
	}
}
