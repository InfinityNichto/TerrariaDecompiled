using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics;

internal struct ObjectSequence1 : IEquatable<ObjectSequence1>, IObjectSequence
{
	public object Value1;

	public override int GetHashCode()
	{
		return Value1?.GetHashCode() ?? 0;
	}

	public bool Equals(ObjectSequence1 other)
	{
		if (Value1 != null)
		{
			return Value1.Equals(other.Value1);
		}
		return other.Value1 == null;
	}

	public override bool Equals(object obj)
	{
		if (obj is ObjectSequence1 other)
		{
			return Equals(other);
		}
		return false;
	}

	public Span<object> AsSpan()
	{
		return MemoryMarshal.CreateSpan(ref Value1, 1);
	}
}
