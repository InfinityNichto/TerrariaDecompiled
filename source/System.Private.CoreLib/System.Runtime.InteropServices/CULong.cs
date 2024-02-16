using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices;

[CLSCompliant(false)]
[Intrinsic]
public readonly struct CULong : IEquatable<CULong>
{
	private readonly uint _value;

	public nuint Value => _value;

	public CULong(uint value)
	{
		_value = value;
	}

	public CULong(nuint value)
	{
		_value = checked((uint)value);
	}

	public override bool Equals([NotNullWhen(true)] object? o)
	{
		if (o is CULong other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(CULong other)
	{
		return _value == other._value;
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
