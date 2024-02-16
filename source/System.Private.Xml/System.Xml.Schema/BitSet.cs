using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Schema;

internal sealed class BitSet
{
	private readonly int _count;

	private uint[] _bits;

	public int Count => _count;

	public bool this[int index] => Get(index);

	public bool IsEmpty
	{
		get
		{
			uint num = 0u;
			for (int i = 0; i < _bits.Length; i++)
			{
				num |= _bits[i];
			}
			return num == 0;
		}
	}

	private BitSet(int count, uint[] bits)
	{
		_count = count;
		_bits = bits;
	}

	public BitSet(int count)
	{
		_count = count;
		_bits = new uint[Subscript(count + 31)];
	}

	public void Clear()
	{
		int num = _bits.Length;
		int num2 = num;
		while (num2-- > 0)
		{
			_bits[num2] = 0u;
		}
	}

	public void Set(int index)
	{
		int num = Subscript(index);
		EnsureLength(num + 1);
		_bits[num] |= (uint)(1 << index);
	}

	public bool Get(int index)
	{
		bool result = false;
		if (index < _count)
		{
			int num = Subscript(index);
			result = (_bits[num] & (1 << index)) != 0;
		}
		return result;
	}

	public int NextSet(int startFrom)
	{
		int num = startFrom + 1;
		if (num == _count)
		{
			return -1;
		}
		int num2 = Subscript(num);
		num &= 0x1F;
		uint num3;
		for (num3 = _bits[num2] >> num; num3 == 0; num3 = _bits[num2])
		{
			if (++num2 == _bits.Length)
			{
				return -1;
			}
			num = 0;
		}
		while ((num3 & 1) == 0)
		{
			num3 >>= 1;
			num++;
		}
		return (num2 << 5) + num;
	}

	public void And(BitSet other)
	{
		if (this != other)
		{
			int num = _bits.Length;
			int num2 = other._bits.Length;
			int i = ((num > num2) ? num2 : num);
			int num3 = i;
			while (num3-- > 0)
			{
				_bits[num3] &= other._bits[num3];
			}
			for (; i < num; i++)
			{
				_bits[i] = 0u;
			}
		}
	}

	public void Or(BitSet other)
	{
		if (this != other)
		{
			int num = other._bits.Length;
			EnsureLength(num);
			int num2 = num;
			while (num2-- > 0)
			{
				_bits[num2] |= other._bits[num2];
			}
		}
	}

	public override int GetHashCode()
	{
		int num = 1234;
		int num2 = _bits.Length;
		while (--num2 >= 0)
		{
			num ^= (int)_bits[num2] * (num2 + 1);
		}
		return num ^ num;
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		if (obj != null)
		{
			if (this == obj)
			{
				return true;
			}
			BitSet bitSet = (BitSet)obj;
			int num = _bits.Length;
			int num2 = bitSet._bits.Length;
			int num3 = ((num > num2) ? num2 : num);
			int num4 = num3;
			while (num4-- > 0)
			{
				if (_bits[num4] != bitSet._bits[num4])
				{
					return false;
				}
			}
			if (num > num3)
			{
				int num5 = num;
				while (num5-- > num3)
				{
					if (_bits[num5] != 0)
					{
						return false;
					}
				}
			}
			else
			{
				int num6 = num2;
				while (num6-- > num3)
				{
					if (bitSet._bits[num6] != 0)
					{
						return false;
					}
				}
			}
			return true;
		}
		return false;
	}

	public BitSet Clone()
	{
		return new BitSet(_count, (uint[])_bits.Clone());
	}

	public bool Intersects(BitSet other)
	{
		int num = Math.Min(_bits.Length, other._bits.Length);
		while (--num >= 0)
		{
			if ((_bits[num] & other._bits[num]) != 0)
			{
				return true;
			}
		}
		return false;
	}

	private int Subscript(int bitIndex)
	{
		return bitIndex >> 5;
	}

	private void EnsureLength(int nRequiredLength)
	{
		if (nRequiredLength > _bits.Length)
		{
			int num = 2 * _bits.Length;
			if (num < nRequiredLength)
			{
				num = nRequiredLength;
			}
			uint[] array = new uint[num];
			Array.Copy(_bits, array, _bits.Length);
			_bits = array;
		}
	}
}
