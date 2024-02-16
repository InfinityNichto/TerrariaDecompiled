using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace System.Collections.Specialized;

public struct BitVector32
{
	public readonly struct Section
	{
		private readonly short _mask;

		private readonly short _offset;

		public short Mask => _mask;

		public short Offset => _offset;

		internal Section(short mask, short offset)
		{
			_mask = mask;
			_offset = offset;
		}

		public override bool Equals([NotNullWhen(true)] object? o)
		{
			if (o is Section obj)
			{
				return Equals(obj);
			}
			return false;
		}

		public bool Equals(Section obj)
		{
			if (obj._mask == _mask)
			{
				return obj._offset == _offset;
			}
			return false;
		}

		public static bool operator ==(Section a, Section b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(Section a, Section b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_mask, _offset);
		}

		public static string ToString(Section value)
		{
			return $"Section{{0x{value.Mask:x}, 0x{value.Offset:x}}}";
		}

		public override string ToString()
		{
			return ToString(this);
		}
	}

	private uint _data;

	public bool this[int bit]
	{
		get
		{
			return (_data & bit) == (uint)bit;
		}
		set
		{
			if (value)
			{
				_data |= (uint)bit;
			}
			else
			{
				_data &= (uint)(~bit);
			}
		}
	}

	public int this[Section section]
	{
		get
		{
			return (int)((_data & (uint)(section.Mask << (int)section.Offset)) >> (int)section.Offset);
		}
		set
		{
			value <<= (int)section.Offset;
			int num = (0xFFFF & section.Mask) << (int)section.Offset;
			_data = (_data & (uint)(~num)) | (uint)(value & num);
		}
	}

	public int Data => (int)_data;

	public BitVector32(int data)
	{
		_data = (uint)data;
	}

	public BitVector32(BitVector32 value)
	{
		_data = value._data;
	}

	public static int CreateMask()
	{
		return CreateMask(0);
	}

	public static int CreateMask(int previous)
	{
		return previous switch
		{
			0 => 1, 
			int.MinValue => throw new InvalidOperationException(System.SR.BitVectorFull), 
			_ => previous << 1, 
		};
	}

	public static Section CreateSection(short maxValue)
	{
		return CreateSectionHelper(maxValue, 0, 0);
	}

	public static Section CreateSection(short maxValue, Section previous)
	{
		return CreateSectionHelper(maxValue, previous.Mask, previous.Offset);
	}

	private static Section CreateSectionHelper(short maxValue, short priorMask, short priorOffset)
	{
		if (maxValue < 1)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidValue_TooSmall, "maxValue", 1), "maxValue");
		}
		short num = (short)(priorOffset + BitOperations.PopCount((ushort)priorMask));
		if (num >= 32)
		{
			throw new InvalidOperationException(System.SR.BitVectorFull);
		}
		short mask = (short)(BitOperations.RoundUpToPowerOf2((uint)((ushort)maxValue + 1)) - 1);
		return new Section(mask, num);
	}

	public override bool Equals([NotNullWhen(true)] object? o)
	{
		if (o is BitVector32 bitVector)
		{
			return _data == bitVector._data;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _data.GetHashCode();
	}

	public static string ToString(BitVector32 value)
	{
		return string.Create(45, value, delegate(Span<char> dst, BitVector32 v)
		{
			ReadOnlySpan<char> readOnlySpan = "BitVector32{";
			readOnlySpan.CopyTo(dst);
			dst[dst.Length - 1] = '}';
			int num = (int)v._data;
			dst = dst.Slice(readOnlySpan.Length, 32);
			for (int i = 0; i < dst.Length; i++)
			{
				dst[i] = (((num & 0x80000000u) != 0L) ? '1' : '0');
				num <<= 1;
			}
		});
	}

	public override string ToString()
	{
		return ToString(this);
	}
}
