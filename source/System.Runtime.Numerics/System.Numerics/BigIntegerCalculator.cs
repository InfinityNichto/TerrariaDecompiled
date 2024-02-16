namespace System.Numerics;

internal static class BigIntegerCalculator
{
	internal struct BitsBuffer
	{
		private uint[] _bits;

		private int _length;

		public BitsBuffer(int size, uint value)
		{
			_bits = new uint[size];
			_length = ((value != 0) ? 1 : 0);
			_bits[0] = value;
		}

		public BitsBuffer(int size, uint[] value)
		{
			_bits = new uint[size];
			_length = ActualLength(value);
			Array.Copy(value, _bits, _length);
		}

		public unsafe void MultiplySelf(ref BitsBuffer value, ref BitsBuffer temp)
		{
			fixed (uint* ptr2 = _bits)
			{
				fixed (uint* ptr = value._bits)
				{
					fixed (uint* bits = temp._bits)
					{
						if (_length < value._length)
						{
							Multiply(ptr, value._length, ptr2, _length, bits, _length + value._length);
						}
						else
						{
							Multiply(ptr2, _length, ptr, value._length, bits, _length + value._length);
						}
					}
				}
			}
			Apply(ref temp, _length + value._length);
		}

		public unsafe void SquareSelf(ref BitsBuffer temp)
		{
			fixed (uint* value = _bits)
			{
				fixed (uint* bits = temp._bits)
				{
					Square(value, _length, bits, _length + _length);
				}
			}
			Apply(ref temp, _length + _length);
		}

		public void Reduce(ref FastReducer reducer)
		{
			_length = reducer.Reduce(_bits, _length);
		}

		public unsafe void Reduce(uint[] modulus)
		{
			if (_length < modulus.Length)
			{
				return;
			}
			fixed (uint* left = _bits)
			{
				fixed (uint* right = modulus)
				{
					Divide(left, _length, right, modulus.Length, null, 0);
				}
			}
			_length = ActualLength(_bits, modulus.Length);
		}

		public unsafe void Reduce(ref BitsBuffer modulus)
		{
			if (_length < modulus._length)
			{
				return;
			}
			fixed (uint* left = _bits)
			{
				fixed (uint* right = modulus._bits)
				{
					Divide(left, _length, right, modulus._length, null, 0);
				}
			}
			_length = ActualLength(_bits, modulus._length);
		}

		public void Overwrite(ulong value)
		{
			if (_length > 2)
			{
				Array.Clear(_bits, 2, _length - 2);
			}
			uint num = (uint)value;
			uint num2 = (uint)(value >> 32);
			_bits[0] = num;
			_bits[1] = num2;
			_length = ((num2 != 0) ? 2 : ((num != 0) ? 1 : 0));
		}

		public void Overwrite(uint value)
		{
			if (_length > 1)
			{
				Array.Clear(_bits, 1, _length - 1);
			}
			_bits[0] = value;
			_length = ((value != 0) ? 1 : 0);
		}

		public uint[] GetBits()
		{
			return _bits;
		}

		public int GetSize()
		{
			return _bits.Length;
		}

		public int GetLength()
		{
			return _length;
		}

		public void Refresh(int maxLength)
		{
			if (_length > maxLength)
			{
				Array.Clear(_bits, maxLength, _length - maxLength);
			}
			_length = ActualLength(_bits, maxLength);
		}

		private void Apply(ref BitsBuffer temp, int maxLength)
		{
			Array.Clear(_bits, 0, _length);
			uint[] bits = temp._bits;
			temp._bits = _bits;
			_bits = bits;
			_length = ActualLength(_bits, maxLength);
		}
	}

	internal readonly struct FastReducer
	{
		private readonly uint[] _modulus;

		private readonly uint[] _mu;

		private readonly uint[] _q1;

		private readonly uint[] _q2;

		private readonly int _muLength;

		public FastReducer(uint[] modulus)
		{
			uint[] array = new uint[modulus.Length * 2 + 1];
			array[^1] = 1u;
			_mu = Divide(array, modulus);
			_modulus = modulus;
			_q1 = new uint[modulus.Length * 2 + 2];
			_q2 = new uint[modulus.Length * 2 + 1];
			_muLength = ActualLength(_mu);
		}

		public int Reduce(uint[] value, int length)
		{
			if (length < _modulus.Length)
			{
				return length;
			}
			int leftLength = DivMul(value, length, _mu, _muLength, _q1, _modulus.Length - 1);
			int rightLength = DivMul(_q1, leftLength, _modulus, _modulus.Length, _q2, _modulus.Length + 1);
			return SubMod(value, length, _q2, rightLength, _modulus, _modulus.Length + 1);
		}

		private unsafe static int DivMul(uint[] left, int leftLength, uint[] right, int rightLength, uint[] bits, int k)
		{
			Array.Clear(bits);
			if (leftLength > k)
			{
				leftLength -= k;
				fixed (uint* ptr2 = left)
				{
					fixed (uint* ptr = right)
					{
						fixed (uint* bits2 = bits)
						{
							if (leftLength < rightLength)
							{
								Multiply(ptr, rightLength, ptr2 + k, leftLength, bits2, leftLength + rightLength);
							}
							else
							{
								Multiply(ptr2 + k, leftLength, ptr, rightLength, bits2, leftLength + rightLength);
							}
						}
					}
				}
				return ActualLength(bits, leftLength + rightLength);
			}
			return 0;
		}

		private unsafe static int SubMod(uint[] left, int leftLength, uint[] right, int rightLength, uint[] modulus, int k)
		{
			if (leftLength > k)
			{
				leftLength = k;
			}
			if (rightLength > k)
			{
				rightLength = k;
			}
			fixed (uint* left2 = left)
			{
				fixed (uint* right2 = right)
				{
					fixed (uint* right3 = modulus)
					{
						SubtractSelf(left2, leftLength, right2, rightLength);
						leftLength = ActualLength(left, leftLength);
						while (Compare(left2, leftLength, right3, modulus.Length) >= 0)
						{
							SubtractSelf(left2, leftLength, right3, modulus.Length);
							leftLength = ActualLength(left, leftLength);
						}
					}
				}
			}
			Array.Clear(left, leftLength, left.Length - leftLength);
			return leftLength;
		}
	}

	private static int ReducerThreshold = 32;

	private static int SquareThreshold = 32;

	private static int AllocationThreshold = 256;

	private static int MultiplyThreshold = 32;

	public static uint[] Add(uint[] left, uint right)
	{
		uint[] array = new uint[left.Length + 1];
		long num = (long)left[0] + (long)right;
		array[0] = (uint)num;
		long num2 = num >> 32;
		for (int i = 1; i < left.Length; i++)
		{
			num = left[i] + num2;
			array[i] = (uint)num;
			num2 = num >> 32;
		}
		array[left.Length] = (uint)num2;
		return array;
	}

	public unsafe static uint[] Add(uint[] left, uint[] right)
	{
		uint[] array = new uint[left.Length + 1];
		fixed (uint* left2 = left)
		{
			fixed (uint* right2 = right)
			{
				fixed (uint* bits = &array[0])
				{
					Add(left2, left.Length, right2, right.Length, bits, array.Length);
				}
			}
		}
		return array;
	}

	private unsafe static void Add(uint* left, int leftLength, uint* right, int rightLength, uint* bits, int bitsLength)
	{
		int i = 0;
		long num = 0L;
		for (; i < rightLength; i++)
		{
			long num2 = left[i] + num + right[i];
			bits[i] = (uint)num2;
			num = num2 >> 32;
		}
		for (; i < leftLength; i++)
		{
			long num3 = left[i] + num;
			bits[i] = (uint)num3;
			num = num3 >> 32;
		}
		bits[i] = (uint)num;
	}

	private unsafe static void AddSelf(uint* left, int leftLength, uint* right, int rightLength)
	{
		int i = 0;
		long num = 0L;
		for (; i < rightLength; i++)
		{
			long num2 = left[i] + num + right[i];
			left[i] = (uint)num2;
			num = num2 >> 32;
		}
		while (num != 0L && i < leftLength)
		{
			long num3 = left[i] + num;
			left[i] = (uint)num3;
			num = num3 >> 32;
			i++;
		}
	}

	public static uint[] Subtract(uint[] left, uint right)
	{
		uint[] array = new uint[left.Length];
		long num = (long)left[0] - (long)right;
		array[0] = (uint)num;
		long num2 = num >> 32;
		for (int i = 1; i < left.Length; i++)
		{
			num = left[i] + num2;
			array[i] = (uint)num;
			num2 = num >> 32;
		}
		return array;
	}

	public unsafe static uint[] Subtract(uint[] left, uint[] right)
	{
		uint[] array = new uint[left.Length];
		fixed (uint* left2 = left)
		{
			fixed (uint* right2 = right)
			{
				fixed (uint* bits = array)
				{
					Subtract(left2, left.Length, right2, right.Length, bits, array.Length);
				}
			}
		}
		return array;
	}

	private unsafe static void Subtract(uint* left, int leftLength, uint* right, int rightLength, uint* bits, int bitsLength)
	{
		int i = 0;
		long num = 0L;
		for (; i < rightLength; i++)
		{
			long num2 = left[i] + num - right[i];
			bits[i] = (uint)num2;
			num = num2 >> 32;
		}
		for (; i < leftLength; i++)
		{
			long num3 = left[i] + num;
			bits[i] = (uint)num3;
			num = num3 >> 32;
		}
	}

	private unsafe static void SubtractSelf(uint* left, int leftLength, uint* right, int rightLength)
	{
		int i = 0;
		long num = 0L;
		for (; i < rightLength; i++)
		{
			long num2 = left[i] + num - right[i];
			left[i] = (uint)num2;
			num = num2 >> 32;
		}
		while (num != 0L && i < leftLength)
		{
			long num3 = left[i] + num;
			left[i] = (uint)num3;
			num = num3 >> 32;
			i++;
		}
	}

	public static int Compare(uint[] left, uint[] right)
	{
		if (left.Length < right.Length)
		{
			return -1;
		}
		if (left.Length > right.Length)
		{
			return 1;
		}
		for (int num = left.Length - 1; num >= 0; num--)
		{
			if (left[num] < right[num])
			{
				return -1;
			}
			if (left[num] > right[num])
			{
				return 1;
			}
		}
		return 0;
	}

	private unsafe static int Compare(uint* left, int leftLength, uint* right, int rightLength)
	{
		if (leftLength < rightLength)
		{
			return -1;
		}
		if (leftLength > rightLength)
		{
			return 1;
		}
		for (int num = leftLength - 1; num >= 0; num--)
		{
			if (left[num] < right[num])
			{
				return -1;
			}
			if (left[num] > right[num])
			{
				return 1;
			}
		}
		return 0;
	}

	public static uint[] Divide(uint[] left, uint right, out uint remainder)
	{
		uint[] array = new uint[left.Length];
		ulong num = 0uL;
		for (int num2 = left.Length - 1; num2 >= 0; num2--)
		{
			ulong num3 = (num << 32) | left[num2];
			ulong num4 = num3 / right;
			array[num2] = (uint)num4;
			num = num3 - num4 * right;
		}
		remainder = (uint)num;
		return array;
	}

	public static uint[] Divide(uint[] left, uint right)
	{
		uint[] array = new uint[left.Length];
		ulong num = 0uL;
		for (int num2 = left.Length - 1; num2 >= 0; num2--)
		{
			ulong num3 = (num << 32) | left[num2];
			ulong num4 = num3 / right;
			array[num2] = (uint)num4;
			num = num3 - num4 * right;
		}
		return array;
	}

	public static uint Remainder(uint[] left, uint right)
	{
		ulong num = 0uL;
		for (int num2 = left.Length - 1; num2 >= 0; num2--)
		{
			ulong num3 = (num << 32) | left[num2];
			num = num3 % right;
		}
		return (uint)num;
	}

	public unsafe static uint[] Divide(uint[] left, uint[] right, out uint[] remainder)
	{
		uint[] array = left.AsSpan().ToArray();
		uint[] array2 = new uint[left.Length - right.Length + 1];
		fixed (uint* left2 = &array[0])
		{
			fixed (uint* right2 = &right[0])
			{
				fixed (uint* bits = &array2[0])
				{
					Divide(left2, array.Length, right2, right.Length, bits, array2.Length);
				}
			}
		}
		remainder = array;
		return array2;
	}

	public unsafe static uint[] Divide(uint[] left, uint[] right)
	{
		Span<uint> destination = default(Span<uint>);
		Span<uint> span;
		if (left.Length <= 64)
		{
			span = stackalloc uint[64];
			destination = span.Slice(0, left.Length);
			span = left.AsSpan();
			span.CopyTo(destination);
		}
		else
		{
			span = left.AsSpan();
			destination = span.ToArray();
		}
		uint[] array = new uint[left.Length - right.Length + 1];
		fixed (uint* left2 = &destination[0])
		{
			fixed (uint* right2 = &right[0])
			{
				fixed (uint* bits = &array[0])
				{
					Divide(left2, destination.Length, right2, right.Length, bits, array.Length);
				}
			}
		}
		return array;
	}

	public unsafe static uint[] Remainder(uint[] left, uint[] right)
	{
		uint[] array = left.AsSpan().ToArray();
		fixed (uint* left2 = &array[0])
		{
			fixed (uint* right2 = &right[0])
			{
				Divide(left2, array.Length, right2, right.Length, null, 0);
			}
		}
		return array;
	}

	private unsafe static void Divide(uint* left, int leftLength, uint* right, int rightLength, uint* bits, int bitsLength)
	{
		uint num = right[rightLength - 1];
		uint num2 = ((rightLength > 1) ? right[rightLength - 2] : 0u);
		int num3 = LeadingZeros(num);
		int num4 = 32 - num3;
		if (num3 > 0)
		{
			uint num5 = ((rightLength > 2) ? right[rightLength - 3] : 0u);
			num = (num << num3) | (num2 >> num4);
			num2 = (num2 << num3) | (num5 >> num4);
		}
		for (int num6 = leftLength; num6 >= rightLength; num6--)
		{
			int num7 = num6 - rightLength;
			uint num8 = ((num6 < leftLength) ? left[num6] : 0u);
			ulong num9 = ((ulong)num8 << 32) | left[num6 - 1];
			uint num10 = ((num6 > 1) ? left[num6 - 2] : 0u);
			if (num3 > 0)
			{
				uint num11 = ((num6 > 2) ? left[num6 - 3] : 0u);
				num9 = (num9 << num3) | (num10 >> num4);
				num10 = (num10 << num3) | (num11 >> num4);
			}
			ulong num12 = num9 / num;
			if (num12 > uint.MaxValue)
			{
				num12 = 4294967295uL;
			}
			while (DivideGuessTooBig(num12, num9, num10, num, num2))
			{
				num12--;
			}
			if (num12 != 0)
			{
				uint num13 = SubtractDivisor(left + num7, leftLength - num7, right, rightLength, num12);
				if (num13 != num8)
				{
					num13 = AddDivisor(left + num7, leftLength - num7, right, rightLength);
					num12--;
				}
			}
			if (bitsLength != 0)
			{
				bits[num7] = (uint)num12;
			}
			if (num6 < leftLength)
			{
				left[num6] = 0u;
			}
		}
	}

	private unsafe static uint AddDivisor(uint* left, int leftLength, uint* right, int rightLength)
	{
		ulong num = 0uL;
		for (int i = 0; i < rightLength; i++)
		{
			ulong num2 = left[i] + num + right[i];
			left[i] = (uint)num2;
			num = num2 >> 32;
		}
		return (uint)num;
	}

	private unsafe static uint SubtractDivisor(uint* left, int leftLength, uint* right, int rightLength, ulong q)
	{
		ulong num = 0uL;
		for (int i = 0; i < rightLength; i++)
		{
			num += right[i] * q;
			uint num2 = (uint)num;
			num >>= 32;
			if (left[i] < num2)
			{
				num++;
			}
			left[i] -= num2;
		}
		return (uint)num;
	}

	private static bool DivideGuessTooBig(ulong q, ulong valHi, uint valLo, uint divHi, uint divLo)
	{
		ulong num = divHi * q;
		ulong num2 = divLo * q;
		num += num2 >> 32;
		num2 &= 0xFFFFFFFFu;
		if (num < valHi)
		{
			return false;
		}
		if (num > valHi)
		{
			return true;
		}
		if (num2 < valLo)
		{
			return false;
		}
		if (num2 > valLo)
		{
			return true;
		}
		return false;
	}

	private static int LeadingZeros(uint value)
	{
		if (value == 0)
		{
			return 32;
		}
		int num = 0;
		if ((value & 0xFFFF0000u) == 0)
		{
			num += 16;
			value <<= 16;
		}
		if ((value & 0xFF000000u) == 0)
		{
			num += 8;
			value <<= 8;
		}
		if ((value & 0xF0000000u) == 0)
		{
			num += 4;
			value <<= 4;
		}
		if ((value & 0xC0000000u) == 0)
		{
			num += 2;
			value <<= 2;
		}
		if ((value & 0x80000000u) == 0)
		{
			num++;
		}
		return num;
	}

	public static uint Gcd(uint left, uint right)
	{
		while (right != 0)
		{
			uint num = left % right;
			left = right;
			right = num;
		}
		return left;
	}

	public static ulong Gcd(ulong left, ulong right)
	{
		while (right > uint.MaxValue)
		{
			ulong num = left % right;
			left = right;
			right = num;
		}
		if (right != 0L)
		{
			return Gcd((uint)right, (uint)(left % right));
		}
		return left;
	}

	public static uint Gcd(uint[] left, uint right)
	{
		uint right2 = Remainder(left, right);
		return Gcd(right, right2);
	}

	public static uint[] Gcd(uint[] left, uint[] right)
	{
		BitsBuffer left2 = new BitsBuffer(left.Length, left);
		BitsBuffer right2 = new BitsBuffer(right.Length, right);
		Gcd(ref left2, ref right2);
		return left2.GetBits();
	}

	private static void Gcd(ref BitsBuffer left, ref BitsBuffer right)
	{
		while (right.GetLength() > 2)
		{
			ExtractDigits(ref left, ref right, out var x, out var y);
			uint num = 1u;
			uint num2 = 0u;
			uint num3 = 0u;
			uint num4 = 1u;
			int num5 = 0;
			while (y != 0L)
			{
				ulong num6 = x / y;
				if (num6 > uint.MaxValue)
				{
					break;
				}
				ulong num7 = num + num6 * num3;
				ulong num8 = num2 + num6 * num4;
				ulong num9 = x - num6 * y;
				if (num7 > int.MaxValue || num8 > int.MaxValue || num9 < num8 || num9 + num7 > y - num3)
				{
					break;
				}
				num = (uint)num7;
				num2 = (uint)num8;
				x = num9;
				num5++;
				if (x == num2)
				{
					break;
				}
				num6 = y / x;
				if (num6 > uint.MaxValue)
				{
					break;
				}
				num7 = num4 + num6 * num2;
				num8 = num3 + num6 * num;
				num9 = y - num6 * x;
				if (num7 > int.MaxValue || num8 > int.MaxValue || num9 < num8 || num9 + num7 > x - num2)
				{
					break;
				}
				num4 = (uint)num7;
				num3 = (uint)num8;
				y = num9;
				num5++;
				if (y == num3)
				{
					break;
				}
			}
			if (num2 == 0)
			{
				left.Reduce(ref right);
				BitsBuffer bitsBuffer = left;
				left = right;
				right = bitsBuffer;
				continue;
			}
			LehmerCore(ref left, ref right, num, num2, num3, num4);
			if (num5 % 2 == 1)
			{
				BitsBuffer bitsBuffer2 = left;
				left = right;
				right = bitsBuffer2;
			}
		}
		if (right.GetLength() > 0)
		{
			left.Reduce(ref right);
			uint[] bits = right.GetBits();
			uint[] bits2 = left.GetBits();
			ulong left2 = ((ulong)bits[1] << 32) | bits[0];
			ulong right2 = ((ulong)bits2[1] << 32) | bits2[0];
			left.Overwrite(Gcd(left2, right2));
			right.Overwrite(0u);
		}
	}

	private static void ExtractDigits(ref BitsBuffer xBuffer, ref BitsBuffer yBuffer, out ulong x, out ulong y)
	{
		uint[] bits = xBuffer.GetBits();
		int length = xBuffer.GetLength();
		uint[] bits2 = yBuffer.GetBits();
		int length2 = yBuffer.GetLength();
		ulong num = bits[length - 1];
		ulong num2 = bits[length - 2];
		ulong num3 = bits[length - 3];
		ulong num4;
		ulong num5;
		ulong num6;
		switch (length - length2)
		{
		case 0:
			num4 = bits2[length2 - 1];
			num5 = bits2[length2 - 2];
			num6 = bits2[length2 - 3];
			break;
		case 1:
			num4 = 0uL;
			num5 = bits2[length2 - 1];
			num6 = bits2[length2 - 2];
			break;
		case 2:
			num4 = 0uL;
			num5 = 0uL;
			num6 = bits2[length2 - 1];
			break;
		default:
			num4 = 0uL;
			num5 = 0uL;
			num6 = 0uL;
			break;
		}
		int num7 = LeadingZeros((uint)num);
		x = ((num << 32 + num7) | (num2 << num7) | (num3 >> 32 - num7)) >> 1;
		y = ((num4 << 32 + num7) | (num5 << num7) | (num6 >> 32 - num7)) >> 1;
	}

	private static void LehmerCore(ref BitsBuffer xBuffer, ref BitsBuffer yBuffer, long a, long b, long c, long d)
	{
		uint[] bits = xBuffer.GetBits();
		uint[] bits2 = yBuffer.GetBits();
		int length = yBuffer.GetLength();
		long num = 0L;
		long num2 = 0L;
		for (int i = 0; i < length; i++)
		{
			long num3 = a * bits[i] - b * bits2[i] + num;
			long num4 = d * bits2[i] - c * bits[i] + num2;
			num = num3 >> 32;
			num2 = num4 >> 32;
			bits[i] = (uint)num3;
			bits2[i] = (uint)num4;
		}
		xBuffer.Refresh(length);
		yBuffer.Refresh(length);
	}

	public static uint[] Pow(uint value, uint power)
	{
		int size = PowBound(power, 1, 1);
		BitsBuffer value2 = new BitsBuffer(size, value);
		return PowCore(power, ref value2);
	}

	public static uint[] Pow(uint[] value, uint power)
	{
		int size = PowBound(power, value.Length, 1);
		BitsBuffer value2 = new BitsBuffer(size, value);
		return PowCore(power, ref value2);
	}

	private static uint[] PowCore(uint power, ref BitsBuffer value)
	{
		int size = value.GetSize();
		BitsBuffer temp = new BitsBuffer(size, 0u);
		BitsBuffer result = new BitsBuffer(size, 1u);
		PowCore(power, ref value, ref result, ref temp);
		return result.GetBits();
	}

	private static int PowBound(uint power, int valueLength, int resultLength)
	{
		checked
		{
			while (power != 0)
			{
				if ((power & 1) == 1)
				{
					resultLength += valueLength;
				}
				if (power != 1)
				{
					valueLength += valueLength;
				}
				power >>= 1;
			}
			return resultLength;
		}
	}

	private static void PowCore(uint power, ref BitsBuffer value, ref BitsBuffer result, ref BitsBuffer temp)
	{
		while (power != 0)
		{
			if ((power & 1) == 1)
			{
				result.MultiplySelf(ref value, ref temp);
			}
			if (power != 1)
			{
				value.SquareSelf(ref temp);
			}
			power >>= 1;
		}
	}

	public static uint Pow(uint value, uint power, uint modulus)
	{
		return PowCore(power, modulus, value, 1uL);
	}

	public static uint Pow(uint[] value, uint power, uint modulus)
	{
		uint num = Remainder(value, modulus);
		return PowCore(power, modulus, num, 1uL);
	}

	public static uint Pow(uint value, uint[] power, uint modulus)
	{
		return PowCore(power, modulus, value, 1uL);
	}

	public static uint Pow(uint[] value, uint[] power, uint modulus)
	{
		uint num = Remainder(value, modulus);
		return PowCore(power, modulus, num, 1uL);
	}

	private static uint PowCore(uint[] power, uint modulus, ulong value, ulong result)
	{
		for (int i = 0; i < power.Length - 1; i++)
		{
			uint num = power[i];
			for (int j = 0; j < 32; j++)
			{
				if ((num & 1) == 1)
				{
					result = result * value % modulus;
				}
				value = value * value % modulus;
				num >>= 1;
			}
		}
		return PowCore(power[^1], modulus, value, result);
	}

	private static uint PowCore(uint power, uint modulus, ulong value, ulong result)
	{
		while (power != 0)
		{
			if ((power & 1) == 1)
			{
				result = result * value % modulus;
			}
			if (power != 1)
			{
				value = value * value % modulus;
			}
			power >>= 1;
		}
		return (uint)(result % modulus);
	}

	public static uint[] Pow(uint value, uint power, uint[] modulus)
	{
		int size = modulus.Length + modulus.Length;
		BitsBuffer value2 = new BitsBuffer(size, value);
		return PowCore(power, modulus, ref value2);
	}

	public static uint[] Pow(uint[] value, uint power, uint[] modulus)
	{
		if (value.Length > modulus.Length)
		{
			value = Remainder(value, modulus);
		}
		int size = modulus.Length + modulus.Length;
		BitsBuffer value2 = new BitsBuffer(size, value);
		return PowCore(power, modulus, ref value2);
	}

	public static uint[] Pow(uint value, uint[] power, uint[] modulus)
	{
		int size = modulus.Length + modulus.Length;
		BitsBuffer value2 = new BitsBuffer(size, value);
		return PowCore(power, modulus, ref value2);
	}

	public static uint[] Pow(uint[] value, uint[] power, uint[] modulus)
	{
		if (value.Length > modulus.Length)
		{
			value = Remainder(value, modulus);
		}
		int size = modulus.Length + modulus.Length;
		BitsBuffer value2 = new BitsBuffer(size, value);
		return PowCore(power, modulus, ref value2);
	}

	private static uint[] PowCore(uint[] power, uint[] modulus, ref BitsBuffer value)
	{
		int size = value.GetSize();
		BitsBuffer temp = new BitsBuffer(size, 0u);
		BitsBuffer result = new BitsBuffer(size, 1u);
		if (modulus.Length < ReducerThreshold)
		{
			PowCore(power, modulus, ref value, ref result, ref temp);
		}
		else
		{
			FastReducer reducer = new FastReducer(modulus);
			PowCore(power, ref reducer, ref value, ref result, ref temp);
		}
		return result.GetBits();
	}

	private static uint[] PowCore(uint power, uint[] modulus, ref BitsBuffer value)
	{
		int size = value.GetSize();
		BitsBuffer temp = new BitsBuffer(size, 0u);
		BitsBuffer result = new BitsBuffer(size, 1u);
		if (modulus.Length < ReducerThreshold)
		{
			PowCore(power, modulus, ref value, ref result, ref temp);
		}
		else
		{
			FastReducer reducer = new FastReducer(modulus);
			PowCore(power, ref reducer, ref value, ref result, ref temp);
		}
		return result.GetBits();
	}

	private static void PowCore(uint[] power, uint[] modulus, ref BitsBuffer value, ref BitsBuffer result, ref BitsBuffer temp)
	{
		for (int i = 0; i < power.Length - 1; i++)
		{
			uint num = power[i];
			for (int j = 0; j < 32; j++)
			{
				if ((num & 1) == 1)
				{
					result.MultiplySelf(ref value, ref temp);
					result.Reduce(modulus);
				}
				value.SquareSelf(ref temp);
				value.Reduce(modulus);
				num >>= 1;
			}
		}
		PowCore(power[^1], modulus, ref value, ref result, ref temp);
	}

	private static void PowCore(uint power, uint[] modulus, ref BitsBuffer value, ref BitsBuffer result, ref BitsBuffer temp)
	{
		while (power != 0)
		{
			if ((power & 1) == 1)
			{
				result.MultiplySelf(ref value, ref temp);
				result.Reduce(modulus);
			}
			if (power != 1)
			{
				value.SquareSelf(ref temp);
				value.Reduce(modulus);
			}
			power >>= 1;
		}
	}

	private static void PowCore(uint[] power, ref FastReducer reducer, ref BitsBuffer value, ref BitsBuffer result, ref BitsBuffer temp)
	{
		for (int i = 0; i < power.Length - 1; i++)
		{
			uint num = power[i];
			for (int j = 0; j < 32; j++)
			{
				if ((num & 1) == 1)
				{
					result.MultiplySelf(ref value, ref temp);
					result.Reduce(ref reducer);
				}
				value.SquareSelf(ref temp);
				value.Reduce(ref reducer);
				num >>= 1;
			}
		}
		PowCore(power[^1], ref reducer, ref value, ref result, ref temp);
	}

	private static void PowCore(uint power, ref FastReducer reducer, ref BitsBuffer value, ref BitsBuffer result, ref BitsBuffer temp)
	{
		while (power != 0)
		{
			if ((power & 1) == 1)
			{
				result.MultiplySelf(ref value, ref temp);
				result.Reduce(ref reducer);
			}
			if (power != 1)
			{
				value.SquareSelf(ref temp);
				value.Reduce(ref reducer);
			}
			power >>= 1;
		}
	}

	private static int ActualLength(uint[] value)
	{
		return ActualLength(value, value.Length);
	}

	private static int ActualLength(uint[] value, int length)
	{
		while (length > 0 && value[length - 1] == 0)
		{
			length--;
		}
		return length;
	}

	public unsafe static uint[] Square(uint[] value)
	{
		uint[] array = new uint[value.Length + value.Length];
		fixed (uint* value2 = value)
		{
			fixed (uint* bits = array)
			{
				Square(value2, value.Length, bits, array.Length);
			}
		}
		return array;
	}

	private unsafe static void Square(uint* value, int valueLength, uint* bits, int bitsLength)
	{
		if (valueLength < SquareThreshold)
		{
			for (int i = 0; i < valueLength; i++)
			{
				ulong num = 0uL;
				for (int j = 0; j < i; j++)
				{
					ulong num2 = bits[i + j] + num;
					ulong num3 = (ulong)value[j] * (ulong)value[i];
					bits[i + j] = (uint)(num2 + (num3 << 1));
					num = num3 + (num2 >> 1) >> 31;
				}
				ulong num4 = (ulong)((long)value[i] * (long)value[i]) + num;
				bits[i + i] = (uint)num4;
				bits[i + i + 1] = (uint)(num4 >> 32);
			}
			return;
		}
		int num5 = valueLength >> 1;
		int num6 = num5 << 1;
		int num7 = num5;
		uint* ptr = value + num5;
		int num8 = valueLength - num5;
		int num9 = num6;
		uint* ptr2 = bits + num6;
		int num10 = bitsLength - num6;
		Square(value, num7, bits, num9);
		Square(ptr, num8, ptr2, num10);
		int num11 = num8 + 1;
		int num12 = num11 + num11;
		if (num12 < AllocationThreshold)
		{
			uint* ptr3 = stackalloc uint[num11];
			new Span<uint>(ptr3, num11).Clear();
			uint* ptr4 = stackalloc uint[num12];
			new Span<uint>(ptr4, num12).Clear();
			Add(ptr, num8, value, num7, ptr3, num11);
			Square(ptr3, num11, ptr4, num12);
			SubtractCore(ptr2, num10, bits, num9, ptr4, num12);
			AddSelf(bits + num5, bitsLength - num5, ptr4, num12);
			return;
		}
		fixed (uint* ptr5 = new uint[num11])
		{
			fixed (uint* ptr6 = new uint[num12])
			{
				Add(ptr, num8, value, num7, ptr5, num11);
				Square(ptr5, num11, ptr6, num12);
				SubtractCore(ptr2, num10, bits, num9, ptr6, num12);
				AddSelf(bits + num5, bitsLength - num5, ptr6, num12);
			}
		}
	}

	public static uint[] Multiply(uint[] left, uint right)
	{
		int i = 0;
		ulong num = 0uL;
		uint[] array = new uint[left.Length + 1];
		for (; i < left.Length; i++)
		{
			ulong num2 = (ulong)((long)left[i] * (long)right) + num;
			array[i] = (uint)num2;
			num = num2 >> 32;
		}
		array[i] = (uint)num;
		return array;
	}

	public unsafe static uint[] Multiply(uint[] left, uint[] right)
	{
		uint[] array = new uint[left.Length + right.Length];
		fixed (uint* left2 = left)
		{
			fixed (uint* right2 = right)
			{
				fixed (uint* bits = array)
				{
					Multiply(left2, left.Length, right2, right.Length, bits, array.Length);
				}
			}
		}
		return array;
	}

	private unsafe static void Multiply(uint* left, int leftLength, uint* right, int rightLength, uint* bits, int bitsLength)
	{
		if (rightLength < MultiplyThreshold)
		{
			for (int i = 0; i < rightLength; i++)
			{
				ulong num = 0uL;
				for (int j = 0; j < leftLength; j++)
				{
					ulong num2 = bits[i + j] + num + (ulong)((long)left[j] * (long)right[i]);
					bits[i + j] = (uint)num2;
					num = num2 >> 32;
				}
				bits[i + leftLength] = (uint)num;
			}
			return;
		}
		int num3 = rightLength >> 1;
		int num4 = num3 << 1;
		int num5 = num3;
		uint* left2 = left + num3;
		int num6 = leftLength - num3;
		int rightLength2 = num3;
		uint* ptr = right + num3;
		int num7 = rightLength - num3;
		int num8 = num4;
		uint* ptr2 = bits + num4;
		int num9 = bitsLength - num4;
		Multiply(left, num5, right, rightLength2, bits, num8);
		Multiply(left2, num6, ptr, num7, ptr2, num9);
		int num10 = num6 + 1;
		int num11 = num7 + 1;
		int num12 = num10 + num11;
		if (num12 < AllocationThreshold)
		{
			uint* ptr3 = stackalloc uint[num10];
			new Span<uint>(ptr3, num10).Clear();
			uint* ptr4 = stackalloc uint[num11];
			new Span<uint>(ptr4, num11).Clear();
			uint* ptr5 = stackalloc uint[num12];
			new Span<uint>(ptr5, num12).Clear();
			Add(left2, num6, left, num5, ptr3, num10);
			Add(ptr, num7, right, rightLength2, ptr4, num11);
			Multiply(ptr3, num10, ptr4, num11, ptr5, num12);
			SubtractCore(ptr2, num9, bits, num8, ptr5, num12);
			AddSelf(bits + num3, bitsLength - num3, ptr5, num12);
			return;
		}
		fixed (uint* ptr6 = new uint[num10])
		{
			fixed (uint* ptr7 = new uint[num11])
			{
				fixed (uint* ptr8 = new uint[num12])
				{
					Add(left2, num6, left, num5, ptr6, num10);
					Add(ptr, num7, right, rightLength2, ptr7, num11);
					Multiply(ptr6, num10, ptr7, num11, ptr8, num12);
					SubtractCore(ptr2, num9, bits, num8, ptr8, num12);
					AddSelf(bits + num3, bitsLength - num3, ptr8, num12);
				}
			}
		}
	}

	private unsafe static void SubtractCore(uint* left, int leftLength, uint* right, int rightLength, uint* core, int coreLength)
	{
		int i = 0;
		long num = 0L;
		for (; i < rightLength; i++)
		{
			long num2 = core[i] + num - left[i] - right[i];
			core[i] = (uint)num2;
			num = num2 >> 32;
		}
		for (; i < leftLength; i++)
		{
			long num3 = core[i] + num - left[i];
			core[i] = (uint)num3;
			num = num3 >> 32;
		}
		while (num != 0L && i < coreLength)
		{
			long num4 = core[i] + num;
			core[i] = (uint)num4;
			num = num4 >> 32;
			i++;
		}
	}
}
