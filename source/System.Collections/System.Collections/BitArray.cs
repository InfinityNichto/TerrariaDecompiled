using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Collections;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class BitArray : ICollection, IEnumerable, ICloneable
{
	private sealed class BitArrayEnumeratorSimple : IEnumerator, ICloneable
	{
		private readonly BitArray _bitArray;

		private int _index;

		private readonly int _version;

		private bool _currentElement;

		public object Current
		{
			get
			{
				if ((uint)_index >= (uint)_bitArray.m_length)
				{
					throw GetInvalidOperationException(_index);
				}
				return _currentElement;
			}
		}

		internal BitArrayEnumeratorSimple(BitArray bitArray)
		{
			_bitArray = bitArray;
			_index = -1;
			_version = bitArray._version;
		}

		public object Clone()
		{
			return MemberwiseClone();
		}

		public bool MoveNext()
		{
			if (_version != _bitArray._version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			if (_index < _bitArray.m_length - 1)
			{
				_index++;
				_currentElement = _bitArray.Get(_index);
				return true;
			}
			_index = _bitArray.m_length;
			return false;
		}

		public void Reset()
		{
			if (_version != _bitArray._version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			_index = -1;
		}

		private InvalidOperationException GetInvalidOperationException(int index)
		{
			if (index == -1)
			{
				return new InvalidOperationException(System.SR.InvalidOperation_EnumNotStarted);
			}
			return new InvalidOperationException(System.SR.InvalidOperation_EnumEnded);
		}
	}

	private int[] m_array;

	private int m_length;

	private int _version;

	public bool this[int index]
	{
		get
		{
			return Get(index);
		}
		set
		{
			Set(index, value);
		}
	}

	public int Length
	{
		get
		{
			return m_length;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value", value, System.SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			int int32ArrayLengthFromBitLength = GetInt32ArrayLengthFromBitLength(value);
			if (int32ArrayLengthFromBitLength > m_array.Length || int32ArrayLengthFromBitLength + 256 < m_array.Length)
			{
				Array.Resize(ref m_array, int32ArrayLengthFromBitLength);
			}
			if (value > m_length)
			{
				int num = m_length - 1 >> 5;
				Div32Rem(m_length, out var remainder);
				if (remainder > 0)
				{
					m_array[num] &= (1 << remainder) - 1;
				}
				m_array.AsSpan(num + 1, int32ArrayLengthFromBitLength - num - 1).Clear();
			}
			m_length = value;
			_version++;
		}
	}

	public int Count => m_length;

	public object SyncRoot => this;

	public bool IsSynchronized => false;

	public bool IsReadOnly => false;

	public BitArray(int length)
		: this(length, defaultValue: false)
	{
	}

	public BitArray(int length, bool defaultValue)
	{
		if (length < 0)
		{
			throw new ArgumentOutOfRangeException("length", length, System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		m_array = new int[GetInt32ArrayLengthFromBitLength(length)];
		m_length = length;
		if (defaultValue)
		{
			Array.Fill(m_array, -1);
			Div32Rem(length, out var remainder);
			if (remainder > 0)
			{
				m_array[^1] = (1 << remainder) - 1;
			}
		}
		_version = 0;
	}

	public BitArray(byte[] bytes)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes");
		}
		if (bytes.Length > 268435455)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_ArrayTooLarge, 8), "bytes");
		}
		m_array = new int[GetInt32ArrayLengthFromByteLength(bytes.Length)];
		m_length = bytes.Length * 8;
		uint num = (uint)bytes.Length / 4u;
		ReadOnlySpan<byte> source = bytes;
		for (int i = 0; i < num; i++)
		{
			m_array[i] = BinaryPrimitives.ReadInt32LittleEndian(source);
			source = source.Slice(4);
		}
		int num2 = 0;
		switch (source.Length)
		{
		case 3:
			num2 = source[2] << 16;
			goto case 2;
		case 2:
			num2 |= source[1] << 8;
			goto case 1;
		case 1:
			m_array[num] = num2 | source[0];
			break;
		}
		_version = 0;
	}

	public unsafe BitArray(bool[] values)
	{
		if (values == null)
		{
			throw new ArgumentNullException("values");
		}
		m_array = new int[GetInt32ArrayLengthFromBitLength(values.Length)];
		m_length = values.Length;
		uint num = 0u;
		if (values.Length >= Vector256<byte>.Count)
		{
			if (Avx2.IsSupported)
			{
				Vector256<byte> zero = Vector256<byte>.Zero;
				fixed (bool* ptr = values)
				{
					for (; num + 32 <= (uint)values.Length; num += 32)
					{
						Vector256<byte> left = Avx.LoadVector256((byte*)(ptr + num));
						Vector256<byte> value = Avx2.CompareEqual(left, zero);
						int num2 = Avx2.MoveMask(value);
						m_array[num / 32] = ~num2;
					}
				}
			}
			else if (Sse2.IsSupported)
			{
				Vector128<byte> zero2 = Vector128<byte>.Zero;
				fixed (bool* ptr2 = values)
				{
					for (; num + 32 <= (uint)values.Length; num += 32)
					{
						Vector128<byte> left2 = Sse2.LoadVector128((byte*)(ptr2 + num));
						Vector128<byte> value2 = Sse2.CompareEqual(left2, zero2);
						int num3 = Sse2.MoveMask(value2);
						Vector128<byte> left3 = Sse2.LoadVector128((byte*)(ptr2 + num + Vector128<byte>.Count));
						Vector128<byte> value3 = Sse2.CompareEqual(left3, zero2);
						int num4 = Sse2.MoveMask(value3);
						m_array[num / 32] = ~((num4 << 16) | num3);
					}
				}
			}
			else if (AdvSimd.Arm64.IsSupported)
			{
				Vector128<byte> zero3 = Vector128<byte>.Zero;
				Vector128<byte> right = (BitConverter.IsLittleEndian ? Vector128.Create(9241421688590303745uL).AsByte() : Vector128.Create(72624976668147840L).AsByte());
				fixed (bool* ptr3 = values)
				{
					for (; num + 32 <= (uint)values.Length; num += 32)
					{
						Vector128<byte> left4 = AdvSimd.LoadVector128((byte*)(ptr3 + num));
						Vector128<byte> left5 = AdvSimd.CompareEqual(left4, zero3);
						Vector128<byte> vector = AdvSimd.And(left5, right);
						vector = AdvSimd.Arm64.AddPairwise(vector, vector);
						vector = AdvSimd.Arm64.AddPairwise(vector, vector);
						vector = AdvSimd.Arm64.AddPairwise(vector, vector);
						Vector128<short> left6 = vector.AsInt16();
						Vector128<byte> left7 = AdvSimd.LoadVector128((byte*)(ptr3 + num + Vector128<byte>.Count));
						Vector128<byte> left8 = AdvSimd.CompareEqual(left7, zero3);
						Vector128<byte> vector2 = AdvSimd.And(left8, right);
						vector2 = AdvSimd.Arm64.AddPairwise(vector2, vector2);
						vector2 = AdvSimd.Arm64.AddPairwise(vector2, vector2);
						vector2 = AdvSimd.Arm64.AddPairwise(vector2, vector2);
						Vector128<short> right2 = vector2.AsInt16();
						int num5 = AdvSimd.Arm64.ZipLow(left6, right2).AsInt32().ToScalar();
						if (!BitConverter.IsLittleEndian)
						{
							num5 = BinaryPrimitives.ReverseEndianness(num5);
						}
						m_array[num / 32] = ~num5;
					}
				}
			}
		}
		for (; num < (uint)values.Length; num++)
		{
			if (values[num])
			{
				int remainder;
				int num6 = Div32Rem((int)num, out remainder);
				m_array[num6] |= 1 << remainder;
			}
		}
		_version = 0;
	}

	public BitArray(int[] values)
	{
		if (values == null)
		{
			throw new ArgumentNullException("values");
		}
		if (values.Length > 67108863)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_ArrayTooLarge, 32), "values");
		}
		m_array = new int[values.Length];
		Array.Copy(values, m_array, values.Length);
		m_length = values.Length * 32;
		_version = 0;
	}

	public BitArray(BitArray bits)
	{
		if (bits == null)
		{
			throw new ArgumentNullException("bits");
		}
		int int32ArrayLengthFromBitLength = GetInt32ArrayLengthFromBitLength(bits.m_length);
		m_array = new int[int32ArrayLengthFromBitLength];
		Array.Copy(bits.m_array, m_array, int32ArrayLengthFromBitLength);
		m_length = bits.m_length;
		_version = bits._version;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Get(int index)
	{
		if ((uint)index >= (uint)m_length)
		{
			ThrowArgumentOutOfRangeException(index);
		}
		return (m_array[index >> 5] & (1 << index)) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Set(int index, bool value)
	{
		if ((uint)index >= (uint)m_length)
		{
			ThrowArgumentOutOfRangeException(index);
		}
		int num = 1 << index;
		ref int reference = ref m_array[index >> 5];
		if (value)
		{
			reference |= num;
		}
		else
		{
			reference &= ~num;
		}
		_version++;
	}

	public void SetAll(bool value)
	{
		int int32ArrayLengthFromBitLength = GetInt32ArrayLengthFromBitLength(Length);
		Span<int> span = m_array.AsSpan(0, int32ArrayLengthFromBitLength);
		if (value)
		{
			span.Fill(-1);
			Div32Rem(m_length, out var remainder);
			if (remainder > 0)
			{
				span[span.Length - 1] &= (1 << remainder) - 1;
			}
		}
		else
		{
			span.Clear();
		}
		_version++;
	}

	public unsafe BitArray And(BitArray value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		int[] array = m_array;
		int[] array2 = value.m_array;
		int int32ArrayLengthFromBitLength = GetInt32ArrayLengthFromBitLength(Length);
		if (Length != value.Length || (uint)int32ArrayLengthFromBitLength > (uint)array.Length || (uint)int32ArrayLengthFromBitLength > (uint)array2.Length)
		{
			throw new ArgumentException(System.SR.Arg_ArrayLengthsDiffer);
		}
		switch (int32ArrayLengthFromBitLength)
		{
		case 7:
			array[6] &= array2[6];
			goto case 6;
		case 6:
			array[5] &= array2[5];
			goto case 5;
		case 5:
			array[4] &= array2[4];
			goto case 4;
		case 4:
			array[3] &= array2[3];
			goto case 3;
		case 3:
			array[2] &= array2[2];
			goto case 2;
		case 2:
			array[1] &= array2[1];
			goto case 1;
		case 1:
			array[0] &= array2[0];
			break;
		default:
		{
			uint num = 0u;
			if (Avx2.IsSupported)
			{
				fixed (int* ptr = array)
				{
					fixed (int* ptr2 = array2)
					{
						for (; num < (uint)(int32ArrayLengthFromBitLength - 7); num += 8)
						{
							Vector256<int> left = Avx.LoadVector256(ptr + num);
							Vector256<int> right = Avx.LoadVector256(ptr2 + num);
							Avx.Store(ptr + num, Avx2.And(left, right));
						}
					}
				}
			}
			else if (Sse2.IsSupported)
			{
				fixed (int* ptr3 = array)
				{
					fixed (int* ptr4 = array2)
					{
						for (; num < (uint)(int32ArrayLengthFromBitLength - 3); num += 4)
						{
							Vector128<int> left2 = Sse2.LoadVector128(ptr3 + num);
							Vector128<int> right2 = Sse2.LoadVector128(ptr4 + num);
							Sse2.Store(ptr3 + num, Sse2.And(left2, right2));
						}
					}
				}
			}
			else if (AdvSimd.IsSupported)
			{
				fixed (int* ptr5 = array)
				{
					fixed (int* ptr6 = array2)
					{
						for (; num < (uint)(int32ArrayLengthFromBitLength - 3); num += 4)
						{
							Vector128<int> left3 = AdvSimd.LoadVector128(ptr5 + num);
							Vector128<int> right3 = AdvSimd.LoadVector128(ptr6 + num);
							AdvSimd.Store(ptr5 + num, AdvSimd.And(left3, right3));
						}
					}
				}
			}
			for (; num < (uint)int32ArrayLengthFromBitLength; num++)
			{
				array[num] &= array2[num];
			}
			break;
		}
		case 0:
			break;
		}
		_version++;
		return this;
	}

	public unsafe BitArray Or(BitArray value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		int[] array = m_array;
		int[] array2 = value.m_array;
		int int32ArrayLengthFromBitLength = GetInt32ArrayLengthFromBitLength(Length);
		if (Length != value.Length || (uint)int32ArrayLengthFromBitLength > (uint)array.Length || (uint)int32ArrayLengthFromBitLength > (uint)array2.Length)
		{
			throw new ArgumentException(System.SR.Arg_ArrayLengthsDiffer);
		}
		switch (int32ArrayLengthFromBitLength)
		{
		case 7:
			array[6] |= array2[6];
			goto case 6;
		case 6:
			array[5] |= array2[5];
			goto case 5;
		case 5:
			array[4] |= array2[4];
			goto case 4;
		case 4:
			array[3] |= array2[3];
			goto case 3;
		case 3:
			array[2] |= array2[2];
			goto case 2;
		case 2:
			array[1] |= array2[1];
			goto case 1;
		case 1:
			array[0] |= array2[0];
			break;
		default:
		{
			uint num = 0u;
			if (Avx2.IsSupported)
			{
				fixed (int* ptr = array)
				{
					fixed (int* ptr2 = array2)
					{
						for (; num < (uint)(int32ArrayLengthFromBitLength - 7); num += 8)
						{
							Vector256<int> left = Avx.LoadVector256(ptr + num);
							Vector256<int> right = Avx.LoadVector256(ptr2 + num);
							Avx.Store(ptr + num, Avx2.Or(left, right));
						}
					}
				}
			}
			else if (Sse2.IsSupported)
			{
				fixed (int* ptr3 = array)
				{
					fixed (int* ptr4 = array2)
					{
						for (; num < (uint)(int32ArrayLengthFromBitLength - 3); num += 4)
						{
							Vector128<int> left2 = Sse2.LoadVector128(ptr3 + num);
							Vector128<int> right2 = Sse2.LoadVector128(ptr4 + num);
							Sse2.Store(ptr3 + num, Sse2.Or(left2, right2));
						}
					}
				}
			}
			else if (AdvSimd.IsSupported)
			{
				fixed (int* ptr5 = array)
				{
					fixed (int* ptr6 = array2)
					{
						for (; num < (uint)(int32ArrayLengthFromBitLength - 3); num += 4)
						{
							Vector128<int> left3 = AdvSimd.LoadVector128(ptr5 + num);
							Vector128<int> right3 = AdvSimd.LoadVector128(ptr6 + num);
							AdvSimd.Store(ptr5 + num, AdvSimd.Or(left3, right3));
						}
					}
				}
			}
			for (; num < (uint)int32ArrayLengthFromBitLength; num++)
			{
				array[num] |= array2[num];
			}
			break;
		}
		case 0:
			break;
		}
		_version++;
		return this;
	}

	public unsafe BitArray Xor(BitArray value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		int[] array = m_array;
		int[] array2 = value.m_array;
		int int32ArrayLengthFromBitLength = GetInt32ArrayLengthFromBitLength(Length);
		if (Length != value.Length || (uint)int32ArrayLengthFromBitLength > (uint)array.Length || (uint)int32ArrayLengthFromBitLength > (uint)array2.Length)
		{
			throw new ArgumentException(System.SR.Arg_ArrayLengthsDiffer);
		}
		switch (int32ArrayLengthFromBitLength)
		{
		case 7:
			array[6] ^= array2[6];
			goto case 6;
		case 6:
			array[5] ^= array2[5];
			goto case 5;
		case 5:
			array[4] ^= array2[4];
			goto case 4;
		case 4:
			array[3] ^= array2[3];
			goto case 3;
		case 3:
			array[2] ^= array2[2];
			goto case 2;
		case 2:
			array[1] ^= array2[1];
			goto case 1;
		case 1:
			array[0] ^= array2[0];
			break;
		default:
		{
			uint num = 0u;
			if (Avx2.IsSupported)
			{
				fixed (int* ptr = m_array)
				{
					fixed (int* ptr2 = value.m_array)
					{
						for (; num < (uint)(int32ArrayLengthFromBitLength - 7); num += 8)
						{
							Vector256<int> left = Avx.LoadVector256(ptr + num);
							Vector256<int> right = Avx.LoadVector256(ptr2 + num);
							Avx.Store(ptr + num, Avx2.Xor(left, right));
						}
					}
				}
			}
			else if (Sse2.IsSupported)
			{
				fixed (int* ptr3 = array)
				{
					fixed (int* ptr4 = array2)
					{
						for (; num < (uint)(int32ArrayLengthFromBitLength - 3); num += 4)
						{
							Vector128<int> left2 = Sse2.LoadVector128(ptr3 + num);
							Vector128<int> right2 = Sse2.LoadVector128(ptr4 + num);
							Sse2.Store(ptr3 + num, Sse2.Xor(left2, right2));
						}
					}
				}
			}
			else if (AdvSimd.IsSupported)
			{
				fixed (int* ptr5 = array)
				{
					fixed (int* ptr6 = array2)
					{
						for (; num < (uint)(int32ArrayLengthFromBitLength - 3); num += 4)
						{
							Vector128<int> left3 = AdvSimd.LoadVector128(ptr5 + num);
							Vector128<int> right3 = AdvSimd.LoadVector128(ptr6 + num);
							AdvSimd.Store(ptr5 + num, AdvSimd.Xor(left3, right3));
						}
					}
				}
			}
			for (; num < (uint)int32ArrayLengthFromBitLength; num++)
			{
				array[num] ^= array2[num];
			}
			break;
		}
		case 0:
			break;
		}
		_version++;
		return this;
	}

	public unsafe BitArray Not()
	{
		int[] array = m_array;
		int int32ArrayLengthFromBitLength = GetInt32ArrayLengthFromBitLength(Length);
		switch (int32ArrayLengthFromBitLength)
		{
		case 7:
			array[6] = ~array[6];
			goto case 6;
		case 6:
			array[5] = ~array[5];
			goto case 5;
		case 5:
			array[4] = ~array[4];
			goto case 4;
		case 4:
			array[3] = ~array[3];
			goto case 3;
		case 3:
			array[2] = ~array[2];
			goto case 2;
		case 2:
			array[1] = ~array[1];
			goto case 1;
		case 1:
			array[0] = ~array[0];
			break;
		default:
		{
			uint num = 0u;
			if (Avx2.IsSupported)
			{
				Vector256<int> right = Vector256.Create(-1);
				fixed (int* ptr = array)
				{
					for (; num < (uint)(int32ArrayLengthFromBitLength - 7); num += 8)
					{
						Vector256<int> left = Avx.LoadVector256(ptr + num);
						Avx.Store(ptr + num, Avx2.Xor(left, right));
					}
				}
			}
			else if (Sse2.IsSupported)
			{
				Vector128<int> right2 = Vector128.Create(-1);
				fixed (int* ptr2 = array)
				{
					for (; num < (uint)(int32ArrayLengthFromBitLength - 3); num += 4)
					{
						Vector128<int> left2 = Sse2.LoadVector128(ptr2 + num);
						Sse2.Store(ptr2 + num, Sse2.Xor(left2, right2));
					}
				}
			}
			else if (AdvSimd.IsSupported)
			{
				fixed (int* ptr3 = array)
				{
					for (; num < (uint)(int32ArrayLengthFromBitLength - 3); num += 4)
					{
						Vector128<int> value = AdvSimd.LoadVector128(ptr3 + num);
						AdvSimd.Store(ptr3 + num, AdvSimd.Not(value));
					}
				}
			}
			for (; num < (uint)int32ArrayLengthFromBitLength; num++)
			{
				array[num] = ~array[num];
			}
			break;
		}
		case 0:
			break;
		}
		_version++;
		return this;
	}

	public BitArray RightShift(int count)
	{
		if (count <= 0)
		{
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", count, System.SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			_version++;
			return this;
		}
		int num = 0;
		int int32ArrayLengthFromBitLength = GetInt32ArrayLengthFromBitLength(m_length);
		if (count < m_length)
		{
			int remainder;
			int num2 = Div32Rem(count, out remainder);
			Div32Rem(m_length, out var remainder2);
			if (remainder == 0)
			{
				uint num3 = uint.MaxValue >> 32 - remainder2;
				m_array[int32ArrayLengthFromBitLength - 1] &= (int)num3;
				Array.Copy(m_array, num2, m_array, 0, int32ArrayLengthFromBitLength - num2);
				num = int32ArrayLengthFromBitLength - num2;
			}
			else
			{
				int num4 = int32ArrayLengthFromBitLength - 1;
				while (num2 < num4)
				{
					uint num5 = (uint)m_array[num2] >> remainder;
					int num6 = m_array[++num2] << 32 - remainder;
					m_array[num++] = num6 | (int)num5;
				}
				uint num7 = uint.MaxValue >> 32 - remainder2;
				num7 &= (uint)m_array[num2];
				m_array[num++] = (int)(num7 >> remainder);
			}
		}
		m_array.AsSpan(num, int32ArrayLengthFromBitLength - num).Clear();
		_version++;
		return this;
	}

	public BitArray LeftShift(int count)
	{
		if (count <= 0)
		{
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", count, System.SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			_version++;
			return this;
		}
		int num2;
		if (count < m_length)
		{
			int num = m_length - 1 >> 5;
			num2 = Div32Rem(count, out var remainder);
			if (remainder == 0)
			{
				Array.Copy(m_array, 0, m_array, num2, num + 1 - num2);
			}
			else
			{
				int num3 = num - num2;
				while (num3 > 0)
				{
					int num4 = m_array[num3] << remainder;
					uint num5 = (uint)m_array[--num3] >> 32 - remainder;
					m_array[num] = num4 | (int)num5;
					num--;
				}
				m_array[num] = m_array[num3] << remainder;
			}
		}
		else
		{
			num2 = GetInt32ArrayLengthFromBitLength(m_length);
		}
		m_array.AsSpan(0, num2).Clear();
		_version++;
		return this;
	}

	public unsafe void CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (array.Rank != 1)
		{
			throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported, "array");
		}
		if (array is int[] array2)
		{
			Div32Rem(m_length, out var remainder);
			if (remainder == 0)
			{
				Array.Copy(m_array, 0, array2, index, m_array.Length);
				return;
			}
			int num = m_length - 1 >> 5;
			Array.Copy(m_array, 0, array2, index, num);
			array2[index + num] = m_array[num] & ((1 << remainder) - 1);
			return;
		}
		if (array is byte[] array3)
		{
			int num2 = GetByteArrayLengthFromBitLength(m_length);
			if (array.Length - index < num2)
			{
				throw new ArgumentException(System.SR.Argument_InvalidOffLen);
			}
			uint num3 = (uint)m_length & 7u;
			if (num3 != 0)
			{
				num2--;
			}
			Span<byte> destination = array3.AsSpan(index);
			int remainder2;
			int num4 = Div4Rem(num2, out remainder2);
			for (int i = 0; i < num4; i++)
			{
				BinaryPrimitives.WriteInt32LittleEndian(destination, m_array[i]);
				destination = destination.Slice(4);
			}
			if (num3 != 0)
			{
				destination[remainder2] = (byte)((m_array[num4] >> remainder2 * 8) & ((1 << (int)num3) - 1));
			}
			switch (remainder2)
			{
			default:
				return;
			case 3:
				destination[2] = (byte)(m_array[num4] >> 16);
				goto case 2;
			case 2:
				destination[1] = (byte)(m_array[num4] >> 8);
				break;
			case 1:
				break;
			}
			destination[0] = (byte)m_array[num4];
			return;
		}
		if (array is bool[] array4)
		{
			if (array.Length - index < m_length)
			{
				throw new ArgumentException(System.SR.Argument_InvalidOffLen);
			}
			uint num5 = 0u;
			if (m_length >= 32)
			{
				Vector128<byte> vector = Vector128.Create(0L, 72340172838076673L).AsByte();
				Vector128<byte> vector2 = Vector128.Create(144680345676153346L, 217020518514230019L).AsByte();
				if (Avx2.IsSupported)
				{
					Vector256<byte> mask = Vector256.Create(vector, vector2);
					Vector256<byte> right = Vector256.Create(9241421688590303745uL).AsByte();
					Vector256<byte> right2 = Vector256.Create((byte)1);
					fixed (bool* ptr = &array4[index])
					{
						for (; num5 + 32 <= (uint)m_length; num5 += 32)
						{
							int value = m_array[num5 / 32];
							Vector256<int> vector3 = Vector256.Create(value);
							Vector256<byte> left = Avx2.Shuffle(vector3.AsByte(), mask);
							Vector256<byte> left2 = Avx2.And(left, right);
							Vector256<byte> source = Avx2.Min(left2, right2);
							Avx.Store((byte*)(ptr + num5), source);
						}
					}
				}
				else if (Ssse3.IsSupported)
				{
					Vector128<byte> mask2 = vector;
					Vector128<byte> mask3 = vector2;
					Vector128<byte> right3 = Vector128.Create((byte)1);
					Vector128<byte> right4 = (BitConverter.IsLittleEndian ? Vector128.Create(9241421688590303745uL).AsByte() : Vector128.Create(72624976668147840L).AsByte());
					fixed (bool* ptr2 = &array4[index])
					{
						for (; num5 + 32 <= (uint)m_length; num5 += 32)
						{
							int value2 = m_array[num5 / 32];
							Vector128<int> vector4 = Vector128.CreateScalarUnsafe(value2);
							Vector128<byte> left3 = Ssse3.Shuffle(vector4.AsByte(), mask2);
							Vector128<byte> left4 = Sse2.And(left3, right4);
							Vector128<byte> source2 = Sse2.Min(left4, right3);
							Sse2.Store((byte*)(ptr2 + num5), source2);
							Vector128<byte> left5 = Ssse3.Shuffle(vector4.AsByte(), mask3);
							Vector128<byte> left6 = Sse2.And(left5, right4);
							Vector128<byte> source3 = Sse2.Min(left6, right3);
							Sse2.Store((byte*)(ptr2 + num5 + Vector128<byte>.Count), source3);
						}
					}
				}
				else if (AdvSimd.IsSupported)
				{
					Vector128<byte> right5 = Vector128.Create((byte)1);
					Vector128<byte> right6 = (BitConverter.IsLittleEndian ? Vector128.Create(9241421688590303745uL).AsByte() : Vector128.Create(72624976668147840L).AsByte());
					fixed (bool* ptr3 = &array4[index])
					{
						for (; num5 + 32 <= (uint)m_length; num5 += 32)
						{
							int value3 = m_array[num5 / 32];
							if (!BitConverter.IsLittleEndian)
							{
								value3 = BinaryPrimitives.ReverseEndianness(value3);
							}
							Vector128<byte> vector5 = Vector128.Create(value3).AsByte();
							vector5 = AdvSimd.Arm64.ZipLow(vector5, vector5);
							vector5 = AdvSimd.Arm64.ZipLow(vector5, vector5);
							Vector128<byte> left7 = AdvSimd.Arm64.ZipLow(vector5, vector5);
							Vector128<byte> left8 = AdvSimd.And(left7, right6);
							Vector128<byte> source4 = AdvSimd.Min(left8, right5);
							AdvSimd.Store((byte*)(ptr3 + num5), source4);
							Vector128<byte> left9 = AdvSimd.Arm64.ZipHigh(vector5, vector5);
							Vector128<byte> left10 = AdvSimd.And(left9, right6);
							Vector128<byte> source5 = AdvSimd.Min(left10, right5);
							AdvSimd.Store((byte*)(ptr3 + num5 + Vector128<byte>.Count), source5);
						}
					}
				}
			}
			for (; num5 < (uint)m_length; num5++)
			{
				int remainder3;
				int num6 = Div32Rem((int)num5, out remainder3);
				array4[index + (int)num5] = ((m_array[num6] >> remainder3) & 1) != 0;
			}
			return;
		}
		throw new ArgumentException(System.SR.Arg_BitArrayTypeUnsupported, "array");
	}

	public object Clone()
	{
		return new BitArray(this);
	}

	public IEnumerator GetEnumerator()
	{
		return new BitArrayEnumeratorSimple(this);
	}

	private static int GetInt32ArrayLengthFromBitLength(int n)
	{
		return n - 1 + 32 >>> 5;
	}

	private static int GetInt32ArrayLengthFromByteLength(int n)
	{
		return n - 1 + 4 >>> 2;
	}

	private static int GetByteArrayLengthFromBitLength(int n)
	{
		return n - 1 + 8 >>> 3;
	}

	private static int Div32Rem(int number, out int remainder)
	{
		uint result = (uint)number / 32u;
		remainder = number & 0x1F;
		return (int)result;
	}

	private static int Div4Rem(int number, out int remainder)
	{
		uint result = (uint)number / 4u;
		remainder = number & 3;
		return (int)result;
	}

	private static void ThrowArgumentOutOfRangeException(int index)
	{
		throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_Index);
	}
}
