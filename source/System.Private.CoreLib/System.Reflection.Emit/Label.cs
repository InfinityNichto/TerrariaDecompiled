using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit;

public readonly struct Label : IEquatable<Label>
{
	internal readonly int m_label;

	internal Label(int label)
	{
		m_label = label;
	}

	internal int GetLabelValue()
	{
		return m_label;
	}

	public override int GetHashCode()
	{
		return m_label;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Label obj2)
		{
			return Equals(obj2);
		}
		return false;
	}

	public bool Equals(Label obj)
	{
		return obj.m_label == m_label;
	}

	public static bool operator ==(Label a, Label b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Label a, Label b)
	{
		return !(a == b);
	}
}
