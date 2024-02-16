using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

public readonly struct Index : IEquatable<Index>
{
	private readonly int _value;

	public static Index Start => new Index(0);

	public static Index End => new Index(-1);

	public int Value
	{
		get
		{
			if (_value < 0)
			{
				return ~_value;
			}
			return _value;
		}
	}

	public bool IsFromEnd => _value < 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Index(int value, bool fromEnd = false)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		if (fromEnd)
		{
			_value = ~value;
		}
		else
		{
			_value = value;
		}
	}

	private Index(int value)
	{
		_value = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Index FromStart(int value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		return new Index(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Index FromEnd(int value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		return new Index(~value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetOffset(int length)
	{
		int num = _value;
		if (IsFromEnd)
		{
			num += length + 1;
		}
		return num;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is Index)
		{
			return _value == ((Index)value)._value;
		}
		return false;
	}

	public bool Equals(Index other)
	{
		return _value == other._value;
	}

	public override int GetHashCode()
	{
		return _value;
	}

	public static implicit operator Index(int value)
	{
		return FromStart(value);
	}

	public override string ToString()
	{
		if (IsFromEnd)
		{
			return ToStringFromEnd();
		}
		return ((uint)Value).ToString();
	}

	private string ToStringFromEnd()
	{
		Span<char> span = stackalloc char[11];
		int charsWritten;
		bool flag = ((uint)Value).TryFormat(span.Slice(1), out charsWritten);
		span[0] = '^';
		return new string(span.Slice(0, charsWritten + 1));
	}
}
