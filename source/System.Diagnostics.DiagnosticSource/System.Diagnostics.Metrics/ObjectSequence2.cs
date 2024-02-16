using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics;

internal struct ObjectSequence2 : IEquatable<ObjectSequence2>, IObjectSequence
{
	public object Value1;

	public object Value2;

	public bool Equals(ObjectSequence2 other)
	{
		if ((Value1 == null) ? (other.Value1 == null) : Value1.Equals(other.Value1))
		{
			if (Value2 != null)
			{
				return Value2.Equals(other.Value2);
			}
			return other.Value2 == null;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ObjectSequence2 other)
		{
			return Equals(other);
		}
		return false;
	}

	public Span<object> AsSpan()
	{
		return MemoryMarshal.CreateSpan(ref Value1, 2);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Value1, Value2);
	}
}
