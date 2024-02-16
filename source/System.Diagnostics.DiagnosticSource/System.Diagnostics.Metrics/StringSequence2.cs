using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics;

internal struct StringSequence2 : IEquatable<StringSequence2>, IStringSequence
{
	public string Value1;

	public string Value2;

	public StringSequence2(string value1, string value2)
	{
		Value1 = value1;
		Value2 = value2;
	}

	public bool Equals(StringSequence2 other)
	{
		if (Value1 == other.Value1)
		{
			return Value2 == other.Value2;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is StringSequence2 other)
		{
			return Equals(other);
		}
		return false;
	}

	public Span<string> AsSpan()
	{
		return MemoryMarshal.CreateSpan(ref Value1, 2);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Value1, Value2);
	}
}
