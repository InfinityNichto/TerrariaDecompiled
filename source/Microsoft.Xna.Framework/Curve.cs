using System;
using System.ComponentModel;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(ExpandableObjectConverter))]
public class Curve
{
	private CurveLoopType preLoop;

	private CurveLoopType postLoop;

	private CurveKeyCollection keys = new CurveKeyCollection();

	public CurveLoopType PreLoop
	{
		get
		{
			return preLoop;
		}
		set
		{
			preLoop = value;
		}
	}

	public CurveLoopType PostLoop
	{
		get
		{
			return postLoop;
		}
		set
		{
			postLoop = value;
		}
	}

	public CurveKeyCollection Keys => keys;

	public bool IsConstant => keys.Count <= 1;

	public Curve Clone()
	{
		Curve curve = new Curve();
		curve.preLoop = preLoop;
		curve.postLoop = postLoop;
		curve.keys = keys.Clone();
		return curve;
	}

	public void ComputeTangent(int keyIndex, CurveTangent tangentType)
	{
		ComputeTangent(keyIndex, tangentType, tangentType);
	}

	public void ComputeTangent(int keyIndex, CurveTangent tangentInType, CurveTangent tangentOutType)
	{
		if (keys.Count <= keyIndex || keyIndex < 0)
		{
			throw new ArgumentOutOfRangeException("keyIndex");
		}
		CurveKey curveKey = Keys[keyIndex];
		float num;
		float position;
		float num2 = (num = (position = curveKey.Position));
		float num3;
		float value;
		float num4 = (num3 = (value = curveKey.Value));
		if (keyIndex > 0)
		{
			num2 = Keys[keyIndex - 1].Position;
			num4 = Keys[keyIndex - 1].Value;
		}
		if (keyIndex + 1 < keys.Count)
		{
			position = Keys[keyIndex + 1].Position;
			value = Keys[keyIndex + 1].Value;
		}
		switch (tangentInType)
		{
		case CurveTangent.Smooth:
		{
			float num5 = position - num2;
			float num6 = value - num4;
			if (Math.Abs(num6) < 1.1920929E-07f)
			{
				curveKey.TangentIn = 0f;
			}
			else
			{
				curveKey.TangentIn = num6 * Math.Abs(num2 - num) / num5;
			}
			break;
		}
		case CurveTangent.Linear:
			curveKey.TangentIn = num3 - num4;
			break;
		default:
			curveKey.TangentIn = 0f;
			break;
		}
		switch (tangentOutType)
		{
		case CurveTangent.Smooth:
		{
			float num7 = position - num2;
			float num8 = value - num4;
			if (Math.Abs(num8) < 1.1920929E-07f)
			{
				curveKey.TangentOut = 0f;
			}
			else
			{
				curveKey.TangentOut = num8 * Math.Abs(position - num) / num7;
			}
			break;
		}
		case CurveTangent.Linear:
			curveKey.TangentOut = value - num3;
			break;
		default:
			curveKey.TangentOut = 0f;
			break;
		}
	}

	public void ComputeTangents(CurveTangent tangentType)
	{
		ComputeTangents(tangentType, tangentType);
	}

	public void ComputeTangents(CurveTangent tangentInType, CurveTangent tangentOutType)
	{
		for (int i = 0; i < Keys.Count; i++)
		{
			ComputeTangent(i, tangentInType, tangentOutType);
		}
	}

	public float Evaluate(float position)
	{
		if (keys.Count == 0)
		{
			return 0f;
		}
		if (keys.Count == 1)
		{
			return keys[0].internalValue;
		}
		CurveKey curveKey = keys[0];
		CurveKey curveKey2 = keys[keys.Count - 1];
		float num = position;
		float num2 = 0f;
		if (num < curveKey.position)
		{
			if (preLoop == CurveLoopType.Constant)
			{
				return curveKey.internalValue;
			}
			if (preLoop == CurveLoopType.Linear)
			{
				return curveKey.internalValue - curveKey.tangentIn * (curveKey.position - num);
			}
			if (!keys.IsCacheAvailable)
			{
				keys.ComputeCacheValues();
			}
			float num3 = CalcCycle(num);
			float num4 = num - (curveKey.position + num3 * keys.TimeRange);
			if (preLoop == CurveLoopType.Cycle)
			{
				num = curveKey.position + num4;
			}
			else if (preLoop == CurveLoopType.CycleOffset)
			{
				num = curveKey.position + num4;
				num2 = (curveKey2.internalValue - curveKey.internalValue) * num3;
			}
			else
			{
				num = ((((uint)(int)num3 & (true ? 1u : 0u)) != 0) ? (curveKey2.position - num4) : (curveKey.position + num4));
			}
		}
		else if (curveKey2.position < num)
		{
			if (postLoop == CurveLoopType.Constant)
			{
				return curveKey2.internalValue;
			}
			if (postLoop == CurveLoopType.Linear)
			{
				return curveKey2.internalValue - curveKey2.tangentOut * (curveKey2.position - num);
			}
			if (!keys.IsCacheAvailable)
			{
				keys.ComputeCacheValues();
			}
			float num5 = CalcCycle(num);
			float num6 = num - (curveKey.position + num5 * keys.TimeRange);
			if (postLoop == CurveLoopType.Cycle)
			{
				num = curveKey.position + num6;
			}
			else if (postLoop == CurveLoopType.CycleOffset)
			{
				num = curveKey.position + num6;
				num2 = (curveKey2.internalValue - curveKey.internalValue) * num5;
			}
			else
			{
				num = ((((uint)(int)num5 & (true ? 1u : 0u)) != 0) ? (curveKey2.position - num6) : (curveKey.position + num6));
			}
		}
		CurveKey k = null;
		CurveKey k2 = null;
		num = FindSegment(num, ref k, ref k2);
		return num2 + Hermite(k, k2, num);
	}

	private float CalcCycle(float t)
	{
		float num = (t - keys[0].position) * keys.InvTimeRange;
		if (num < 0f)
		{
			num -= 1f;
		}
		int num2 = (int)num;
		return num2;
	}

	private float FindSegment(float t, ref CurveKey k0, ref CurveKey k1)
	{
		float result = t;
		k0 = keys[0];
		for (int i = 1; i < keys.Count; i++)
		{
			k1 = keys[i];
			if (k1.position >= t)
			{
				double num = k0.position;
				double num2 = k1.position;
				double num3 = t;
				double num4 = num2 - num;
				result = 0f;
				if (num4 > 1E-10)
				{
					result = (float)((num3 - num) / num4);
				}
				break;
			}
			k0 = k1;
		}
		return result;
	}

	private static float Hermite(CurveKey k0, CurveKey k1, float t)
	{
		if (k0.Continuity == CurveContinuity.Step)
		{
			if (!(t < 1f))
			{
				return k1.internalValue;
			}
			return k0.internalValue;
		}
		float num = t * t;
		float num2 = num * t;
		float internalValue = k0.internalValue;
		float internalValue2 = k1.internalValue;
		float tangentOut = k0.tangentOut;
		float tangentIn = k1.tangentIn;
		return internalValue * (2f * num2 - 3f * num + 1f) + internalValue2 * (-2f * num2 + 3f * num) + tangentOut * (num2 - 2f * num + t) + tangentIn * (num2 - num);
	}
}
