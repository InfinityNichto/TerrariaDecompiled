using System.Runtime.InteropServices;

namespace System.Diagnostics.Metrics;

internal struct StringSequence1 : IEquatable<StringSequence1>, IStringSequence
{
	public string Value1;

	public StringSequence1(string value1)
	{
		Value1 = value1;
	}

	public override int GetHashCode()
	{
		return Value1.GetHashCode();
	}

	public bool Equals(StringSequence1 other)
	{
		return Value1 == other.Value1;
	}

	public override bool Equals(object obj)
	{
		if (obj is StringSequence1 other)
		{
			return Equals(other);
		}
		return false;
	}

	public Span<string> AsSpan()
	{
		return MemoryMarshal.CreateSpan(ref Value1, 1);
	}
}
