using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics;

internal struct StringSequence3 : IEquatable<StringSequence3>, IStringSequence
{
	public string Value1;

	public string Value2;

	public string Value3;

	public StringSequence3(string value1, string value2, string value3)
	{
		Value1 = value1;
		Value2 = value2;
		Value3 = value3;
	}

	public bool Equals(StringSequence3 other)
	{
		if (Value1 == other.Value1 && Value2 == other.Value2)
		{
			return Value3 == other.Value3;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is StringSequence3 other)
		{
			return Equals(other);
		}
		return false;
	}

	public Span<string> AsSpan()
	{
		return MemoryMarshal.CreateSpan(ref Value1, 3);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Value1, Value2, Value3);
	}
}
