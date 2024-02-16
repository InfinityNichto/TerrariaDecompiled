using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(ExpandableObjectConverter))]
public class CurveKey : IEquatable<CurveKey>, IComparable<CurveKey>
{
	internal float position;

	internal float internalValue;

	internal float tangentOut;

	internal float tangentIn;

	internal CurveContinuity continuity;

	public float Position => position;

	public float Value
	{
		get
		{
			return internalValue;
		}
		set
		{
			internalValue = value;
		}
	}

	public float TangentIn
	{
		get
		{
			return tangentIn;
		}
		set
		{
			tangentIn = value;
		}
	}

	public float TangentOut
	{
		get
		{
			return tangentOut;
		}
		set
		{
			tangentOut = value;
		}
	}

	public CurveContinuity Continuity
	{
		get
		{
			return continuity;
		}
		set
		{
			continuity = value;
		}
	}

	public CurveKey(float position, float value)
	{
		this.position = position;
		internalValue = value;
	}

	public CurveKey(float position, float value, float tangentIn, float tangentOut)
	{
		this.position = position;
		internalValue = value;
		this.tangentIn = tangentIn;
		this.tangentOut = tangentOut;
	}

	public CurveKey(float position, float value, float tangentIn, float tangentOut, CurveContinuity continuity)
	{
		this.position = position;
		internalValue = value;
		this.tangentIn = tangentIn;
		this.tangentOut = tangentOut;
		this.continuity = continuity;
	}

	public CurveKey Clone()
	{
		return new CurveKey(position, internalValue, tangentIn, tangentOut, continuity);
	}

	public bool Equals(CurveKey other)
	{
		if (other != null && other.position == position && other.internalValue == internalValue && other.tangentIn == tangentIn && other.tangentOut == tangentOut)
		{
			return other.continuity == continuity;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		return Equals(obj as CurveKey);
	}

	public override int GetHashCode()
	{
		return position.GetHashCode() + internalValue.GetHashCode() + tangentIn.GetHashCode() + tangentOut.GetHashCode() + continuity.GetHashCode();
	}

	[SuppressMessage("Microsoft.Design", "CA1062")]
	public static bool operator ==(CurveKey a, CurveKey b)
	{
		bool flag = false;
		bool flag2 = (object)null == a;
		bool flag3 = (object)null == b;
		if (flag2 || flag3)
		{
			return flag2 == flag3;
		}
		return a.Equals(b);
	}

	public static bool operator !=(CurveKey a, CurveKey b)
	{
		bool flag = false;
		bool flag2 = a == null;
		bool flag3 = b == null;
		if (flag2 || flag3)
		{
			return flag2 != flag3;
		}
		return a.position != b.position || a.internalValue != b.internalValue || a.tangentIn != b.tangentIn || a.tangentOut != b.tangentOut || a.continuity != b.continuity;
	}

	public int CompareTo(CurveKey other)
	{
		if (position != other.position)
		{
			if (!(position < other.position))
			{
				return 1;
			}
			return -1;
		}
		return 0;
	}
}
