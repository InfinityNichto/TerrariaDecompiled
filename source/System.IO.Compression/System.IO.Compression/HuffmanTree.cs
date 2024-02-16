namespace System.IO.Compression;

internal sealed class HuffmanTree
{
	private readonly int _tableBits;

	private readonly short[] _table;

	private readonly short[] _left;

	private readonly short[] _right;

	private readonly byte[] _codeLengthArray;

	private readonly int _tableMask;

	public static HuffmanTree StaticLiteralLengthTree { get; } = new HuffmanTree(GetStaticLiteralTreeLength());


	public static HuffmanTree StaticDistanceTree { get; } = new HuffmanTree(GetStaticDistanceTreeLength());


	public HuffmanTree(byte[] codeLengths)
	{
		_codeLengthArray = codeLengths;
		if (_codeLengthArray.Length == 288)
		{
			_tableBits = 9;
		}
		else
		{
			_tableBits = 7;
		}
		_tableMask = (1 << _tableBits) - 1;
		_table = new short[1 << _tableBits];
		_left = new short[2 * _codeLengthArray.Length];
		_right = new short[2 * _codeLengthArray.Length];
		CreateTable();
	}

	private static byte[] GetStaticLiteralTreeLength()
	{
		byte[] array = new byte[288];
		for (int i = 0; i <= 143; i++)
		{
			array[i] = 8;
		}
		for (int j = 144; j <= 255; j++)
		{
			array[j] = 9;
		}
		for (int k = 256; k <= 279; k++)
		{
			array[k] = 7;
		}
		for (int l = 280; l <= 287; l++)
		{
			array[l] = 8;
		}
		return array;
	}

	private static byte[] GetStaticDistanceTreeLength()
	{
		byte[] array = new byte[32];
		for (int i = 0; i < 32; i++)
		{
			array[i] = 5;
		}
		return array;
	}

	private static uint BitReverse(uint code, int length)
	{
		uint num = 0u;
		do
		{
			num |= code & 1u;
			num <<= 1;
			code >>= 1;
		}
		while (--length > 0);
		return num >> 1;
	}

	private uint[] CalculateHuffmanCode()
	{
		uint[] array = new uint[17];
		byte[] codeLengthArray = _codeLengthArray;
		foreach (int num in codeLengthArray)
		{
			array[num]++;
		}
		array[0] = 0u;
		uint[] array2 = new uint[17];
		uint num2 = 0u;
		for (int j = 1; j <= 16; j++)
		{
			num2 = (array2[j] = num2 + array[j - 1] << 1);
		}
		uint[] array3 = new uint[288];
		for (int k = 0; k < _codeLengthArray.Length; k++)
		{
			int num3 = _codeLengthArray[k];
			if (num3 > 0)
			{
				array3[k] = BitReverse(array2[num3], num3);
				array2[num3]++;
			}
		}
		return array3;
	}

	private void CreateTable()
	{
		uint[] array = CalculateHuffmanCode();
		short num = (short)_codeLengthArray.Length;
		for (int i = 0; i < _codeLengthArray.Length; i++)
		{
			int num2 = _codeLengthArray[i];
			if (num2 <= 0)
			{
				continue;
			}
			int num3 = (int)array[i];
			if (num2 <= _tableBits)
			{
				int num4 = 1 << num2;
				if (num3 >= num4)
				{
					throw new InvalidDataException(System.SR.InvalidHuffmanData);
				}
				int num5 = 1 << _tableBits - num2;
				for (int j = 0; j < num5; j++)
				{
					_table[num3] = (short)i;
					num3 += num4;
				}
				continue;
			}
			int num6 = num2 - _tableBits;
			int num7 = 1 << _tableBits;
			int num8 = num3 & ((1 << _tableBits) - 1);
			short[] array2 = _table;
			do
			{
				short num9 = array2[num8];
				if (num9 == 0)
				{
					array2[num8] = (short)(-num);
					num9 = (short)(-num);
					num++;
				}
				if (num9 > 0)
				{
					throw new InvalidDataException(System.SR.InvalidHuffmanData);
				}
				array2 = (((num3 & num7) != 0) ? _right : _left);
				num8 = -num9;
				num7 <<= 1;
				num6--;
			}
			while (num6 != 0);
			array2[num8] = (short)i;
		}
	}

	public int GetNextSymbol(InputBuffer input)
	{
		uint num = input.TryLoad16Bits();
		if (input.AvailableBits == 0)
		{
			return -1;
		}
		int num2 = _table[num & _tableMask];
		if (num2 < 0)
		{
			uint num3 = (uint)(1 << _tableBits);
			do
			{
				num2 = -num2;
				num2 = (((num & num3) != 0) ? _right[num2] : _left[num2]);
				num3 <<= 1;
			}
			while (num2 < 0);
		}
		int num4 = _codeLengthArray[num2];
		if (num4 <= 0)
		{
			throw new InvalidDataException(System.SR.InvalidHuffmanData);
		}
		if (num4 > input.AvailableBits)
		{
			return -1;
		}
		input.SkipBits(num4);
		return num2;
	}
}
