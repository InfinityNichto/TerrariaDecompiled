using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices;

[Intrinsic]
public readonly struct NFloat : IEquatable<NFloat>
{
	private readonly double _value;

	public double Value => _value;

	public NFloat(float value)
	{
		_value = value;
	}

	public NFloat(double value)
	{
		_value = value;
	}

	public override bool Equals([NotNullWhen(true)] object? o)
	{
		if (o is NFloat other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(NFloat other)
	{
		return _value.Equals(other._value);
	}

	public override int GetHashCode()
	{
		return _value.GetHashCode();
	}

	public override string ToString()
	{
		return _value.ToString();
	}
}
