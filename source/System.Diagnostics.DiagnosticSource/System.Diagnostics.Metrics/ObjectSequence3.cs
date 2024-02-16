using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics;

internal struct ObjectSequence3 : IEquatable<ObjectSequence3>, IObjectSequence
{
	public object Value1;

	public object Value2;

	public object Value3;

	public bool Equals(ObjectSequence3 other)
	{
		if (((Value1 == null) ? (other.Value1 == null) : Value1.Equals(other.Value1)) && ((Value2 == null) ? (other.Value2 == null) : Value2.Equals(other.Value2)))
		{
			if (Value3 != null)
			{
				return Value3.Equals(other.Value3);
			}
			return other.Value3 == null;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ObjectSequence3 other)
		{
			return Equals(other);
		}
		return false;
	}

	public Span<object> AsSpan()
	{
		return MemoryMarshal.CreateSpan(ref Value1, 3);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Value1, Value2, Value3);
	}
}
