using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices;

public readonly struct ArrayWithOffset
{
	private readonly object m_array;

	private readonly int m_offset;

	private readonly int m_count;

	public ArrayWithOffset(object? array, int offset)
	{
		int num = 0;
		if (array != null)
		{
			if (!(array is Array { Rank: 1 } array2) || !Marshal.IsPinnable(array2))
			{
				throw new ArgumentException(SR.ArgumentException_NotIsomorphic);
			}
			nuint num2 = array2.NativeLength * array2.GetElementSize();
			if (num2 > 2147483632)
			{
				throw new ArgumentException(SR.Argument_StructArrayTooLarge);
			}
			num = (int)num2;
		}
		if ((uint)offset > (uint)num)
		{
			throw new IndexOutOfRangeException(SR.IndexOutOfRange_ArrayWithOffset);
		}
		m_array = array;
		m_offset = offset;
		m_count = num - offset;
	}

	public object? GetArray()
	{
		return m_array;
	}

	public int GetOffset()
	{
		return m_offset;
	}

	public override int GetHashCode()
	{
		return m_count + m_offset;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ArrayWithOffset)
		{
			return Equals((ArrayWithOffset)obj);
		}
		return false;
	}

	public bool Equals(ArrayWithOffset obj)
	{
		if (obj.m_array == m_array && obj.m_offset == m_offset)
		{
			return obj.m_count == m_count;
		}
		return false;
	}

	public static bool operator ==(ArrayWithOffset a, ArrayWithOffset b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(ArrayWithOffset a, ArrayWithOffset b)
	{
		return !(a == b);
	}
}
