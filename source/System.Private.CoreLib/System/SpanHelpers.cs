using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Internal.Runtime.CompilerServices;

namespace System;

internal static class SpanHelpers
{
	internal readonly struct ComparerComparable<T, TComparer> : IComparable<T> where TComparer : IComparer<T>
	{
		private readonly T _value;

		private readonly TComparer _comparer;

		public ComparerComparable(T value, TComparer comparer)
		{
			_value = value;
			_comparer = comparer;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int CompareTo(T other)
		{
			return _comparer.Compare(_value, other);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int BinarySearch<T, TComparable>(this ReadOnlySpan<T> span, TComparable comparable) where TComparable : IComparable<T>
	{
		if (comparable == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparable);
		}
		return BinarySearch(ref MemoryMarshal.GetReference(span), span.Length, comparable);
	}

	public static int BinarySearch<T, TComparable>(ref T spanStart, int length, TComparable comparable) where TComparable : IComparable<T>
	{
		int num = 0;
		int num2 = length - 1;
		while (num <= num2)
		{
			int num3 = num2 + num >>> 1;
			int num4 = comparable.CompareTo(Unsafe.Add(ref spanStart, num3));
			if (num4 == 0)
			{
				return num3;
			}
			if (num4 > 0)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		return ~num;
	}

	public static int IndexOf(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
	{
		if (valueLength == 0)
		{
			return 0;
		}
		byte value2 = value;
		ref byte second = ref Unsafe.Add(ref value, 1);
		int num = valueLength - 1;
		int num2 = searchSpaceLength - num;
		int num3 = 0;
		while (num2 > 0)
		{
			int num4 = IndexOf(ref Unsafe.Add(ref searchSpace, num3), value2, num2);
			if (num4 == -1)
			{
				break;
			}
			num2 -= num4;
			num3 += num4;
			if (num2 <= 0)
			{
				break;
			}
			if (SequenceEqual(ref Unsafe.Add(ref searchSpace, num3 + 1), ref second, (nuint)num))
			{
				return num3;
			}
			num2--;
			num3++;
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static bool Contains(ref byte searchSpace, byte value, int length)
	{
		nuint num = 0u;
		nuint num2 = (uint)length;
		if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
		{
			num2 = UnalignedCountVector(ref searchSpace);
		}
		while (true)
		{
			if (num2 >= 8)
			{
				num2 -= 8;
				if (value == Unsafe.AddByteOffset(ref searchSpace, num + 0) || value == Unsafe.AddByteOffset(ref searchSpace, num + 1) || value == Unsafe.AddByteOffset(ref searchSpace, num + 2) || value == Unsafe.AddByteOffset(ref searchSpace, num + 3) || value == Unsafe.AddByteOffset(ref searchSpace, num + 4) || value == Unsafe.AddByteOffset(ref searchSpace, num + 5) || value == Unsafe.AddByteOffset(ref searchSpace, num + 6) || value == Unsafe.AddByteOffset(ref searchSpace, num + 7))
				{
					break;
				}
				num += 8;
				continue;
			}
			if (num2 >= 4)
			{
				num2 -= 4;
				if (value == Unsafe.AddByteOffset(ref searchSpace, num + 0) || value == Unsafe.AddByteOffset(ref searchSpace, num + 1) || value == Unsafe.AddByteOffset(ref searchSpace, num + 2) || value == Unsafe.AddByteOffset(ref searchSpace, num + 3))
				{
					break;
				}
				num += 4;
			}
			while (num2 != 0)
			{
				num2--;
				if (value == Unsafe.AddByteOffset(ref searchSpace, num))
				{
					goto end_IL_00bd;
				}
				num++;
			}
			if (Vector.IsHardwareAccelerated && num < (uint)length)
			{
				num2 = ((uint)length - num) & (nuint)(~(Vector<byte>.Count - 1));
				Vector<byte> left = new Vector<byte>(value);
				for (; num2 > num; num += (nuint)Vector<byte>.Count)
				{
					Vector<byte> other = Vector.Equals(left, LoadVector(ref searchSpace, num));
					if (!Vector<byte>.Zero.Equals(other))
					{
						goto end_IL_00bd;
					}
				}
				if (num < (uint)length)
				{
					num2 = (uint)length - num;
					continue;
				}
			}
			return false;
			continue;
			end_IL_00bd:
			break;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public unsafe static int IndexOf(ref byte searchSpace, byte value, int length)
	{
		nuint num = 0u;
		nuint num2 = (uint)length;
		if (Sse2.IsSupported || AdvSimd.Arm64.IsSupported)
		{
			if (length >= Vector128<byte>.Count * 2)
			{
				num2 = UnalignedCountVector128(ref searchSpace);
			}
		}
		else if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
		{
			num2 = UnalignedCountVector(ref searchSpace);
		}
		while (true)
		{
			if (num2 >= 8)
			{
				num2 -= 8;
				if (value == Unsafe.AddByteOffset(ref searchSpace, num))
				{
					goto IL_033c;
				}
				if (value == Unsafe.AddByteOffset(ref searchSpace, num + 1))
				{
					goto IL_033f;
				}
				if (value == Unsafe.AddByteOffset(ref searchSpace, num + 2))
				{
					goto IL_0345;
				}
				if (value != Unsafe.AddByteOffset(ref searchSpace, num + 3))
				{
					if (value != Unsafe.AddByteOffset(ref searchSpace, num + 4))
					{
						if (value != Unsafe.AddByteOffset(ref searchSpace, num + 5))
						{
							if (value != Unsafe.AddByteOffset(ref searchSpace, num + 6))
							{
								if (value == Unsafe.AddByteOffset(ref searchSpace, num + 7))
								{
									break;
								}
								num += 8;
								continue;
							}
							return (int)(num + 6);
						}
						return (int)(num + 5);
					}
					return (int)(num + 4);
				}
				goto IL_034b;
			}
			if (num2 >= 4)
			{
				num2 -= 4;
				if (value == Unsafe.AddByteOffset(ref searchSpace, num))
				{
					goto IL_033c;
				}
				if (value == Unsafe.AddByteOffset(ref searchSpace, num + 1))
				{
					goto IL_033f;
				}
				if (value == Unsafe.AddByteOffset(ref searchSpace, num + 2))
				{
					goto IL_0345;
				}
				if (value == Unsafe.AddByteOffset(ref searchSpace, num + 3))
				{
					goto IL_034b;
				}
				num += 4;
			}
			while (num2 != 0)
			{
				num2--;
				if (value != Unsafe.AddByteOffset(ref searchSpace, num))
				{
					num++;
					continue;
				}
				goto IL_033c;
			}
			if (Avx2.IsSupported)
			{
				if (num < (uint)length)
				{
					if ((((uint)Unsafe.AsPointer(ref searchSpace) + num) & (nuint)(Vector256<byte>.Count - 1)) != 0)
					{
						Vector128<byte> left = Vector128.Create(value);
						Vector128<byte> right = LoadVector128(ref searchSpace, num);
						int num3 = Sse2.MoveMask(Sse2.CompareEqual(left, right));
						if (num3 != 0)
						{
							return (int)(num + (uint)BitOperations.TrailingZeroCount(num3));
						}
						num += (nuint)Vector128<byte>.Count;
					}
					num2 = GetByteVector256SpanLength(num, length);
					if (num2 > num)
					{
						Vector256<byte> left2 = Vector256.Create(value);
						do
						{
							Vector256<byte> right2 = LoadVector256(ref searchSpace, num);
							int num4 = Avx2.MoveMask(Avx2.CompareEqual(left2, right2));
							if (num4 == 0)
							{
								num += (nuint)Vector256<byte>.Count;
								continue;
							}
							return (int)(num + (uint)BitOperations.TrailingZeroCount(num4));
						}
						while (num2 > num);
					}
					num2 = GetByteVector128SpanLength(num, length);
					if (num2 > num)
					{
						Vector128<byte> left3 = Vector128.Create(value);
						Vector128<byte> right3 = LoadVector128(ref searchSpace, num);
						int num5 = Sse2.MoveMask(Sse2.CompareEqual(left3, right3));
						if (num5 != 0)
						{
							return (int)(num + (uint)BitOperations.TrailingZeroCount(num5));
						}
						num += (nuint)Vector128<byte>.Count;
					}
					if (num < (uint)length)
					{
						num2 = (uint)length - num;
						continue;
					}
				}
			}
			else if (Sse2.IsSupported)
			{
				if (num < (uint)length)
				{
					num2 = GetByteVector128SpanLength(num, length);
					Vector128<byte> left4 = Vector128.Create(value);
					for (; num2 > num; num += (nuint)Vector128<byte>.Count)
					{
						Vector128<byte> right4 = LoadVector128(ref searchSpace, num);
						int num6 = Sse2.MoveMask(Sse2.CompareEqual(left4, right4));
						if (num6 != 0)
						{
							return (int)(num + (uint)BitOperations.TrailingZeroCount(num6));
						}
					}
					if (num < (uint)length)
					{
						num2 = (uint)length - num;
						continue;
					}
				}
			}
			else
			{
				if (AdvSimd.Arm64.IsSupported)
				{
				}
				if (Vector.IsHardwareAccelerated && num < (uint)length)
				{
					num2 = GetByteVectorSpanLength(num, length);
					Vector<byte> left5 = new Vector<byte>(value);
					for (; num2 > num; num += (nuint)Vector<byte>.Count)
					{
						Vector<byte> vector = Vector.Equals(left5, LoadVector(ref searchSpace, num));
						if (!Vector<byte>.Zero.Equals(vector))
						{
							return (int)num + LocateFirstFoundByte(vector);
						}
					}
					if (num < (uint)length)
					{
						num2 = (uint)length - num;
						continue;
					}
				}
			}
			return -1;
			IL_0345:
			return (int)(num + 2);
			IL_033f:
			return (int)(num + 1);
			IL_034b:
			return (int)(num + 3);
			IL_033c:
			return (int)num;
		}
		return (int)(num + 7);
	}

	public static int LastIndexOf(ref byte searchSpace, int searchSpaceLength, ref byte value, int valueLength)
	{
		if (valueLength == 0)
		{
			return searchSpaceLength;
		}
		byte value2 = value;
		ref byte second = ref Unsafe.Add(ref value, 1);
		int num = valueLength - 1;
		int num2 = 0;
		while (true)
		{
			int num3 = searchSpaceLength - num2 - num;
			if (num3 <= 0)
			{
				break;
			}
			int num4 = LastIndexOf(ref searchSpace, value2, num3);
			if (num4 == -1)
			{
				break;
			}
			if (SequenceEqual(ref Unsafe.Add(ref searchSpace, num4 + 1), ref second, (uint)num))
			{
				return num4;
			}
			num2 += num3 - num4;
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static int LastIndexOf(ref byte searchSpace, byte value, int length)
	{
		nuint num = (uint)length;
		nuint num2 = (uint)length;
		if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
		{
			num2 = UnalignedCountVectorFromEnd(ref searchSpace, length);
		}
		while (true)
		{
			if (num2 >= 8)
			{
				num2 -= 8;
				num -= 8;
				if (value == Unsafe.AddByteOffset(ref searchSpace, num + 7))
				{
					break;
				}
				if (value != Unsafe.AddByteOffset(ref searchSpace, num + 6))
				{
					if (value != Unsafe.AddByteOffset(ref searchSpace, num + 5))
					{
						if (value != Unsafe.AddByteOffset(ref searchSpace, num + 4))
						{
							if (value != Unsafe.AddByteOffset(ref searchSpace, num + 3))
							{
								if (value != Unsafe.AddByteOffset(ref searchSpace, num + 2))
								{
									if (value != Unsafe.AddByteOffset(ref searchSpace, num + 1))
									{
										if (value != Unsafe.AddByteOffset(ref searchSpace, num))
										{
											continue;
										}
										goto IL_01ba;
									}
									goto IL_01bd;
								}
								goto IL_01c3;
							}
							goto IL_01c9;
						}
						return (int)(num + 4);
					}
					return (int)(num + 5);
				}
				return (int)(num + 6);
			}
			if (num2 >= 4)
			{
				num2 -= 4;
				num -= 4;
				if (value == Unsafe.AddByteOffset(ref searchSpace, num + 3))
				{
					goto IL_01c9;
				}
				if (value == Unsafe.AddByteOffset(ref searchSpace, num + 2))
				{
					goto IL_01c3;
				}
				if (value == Unsafe.AddByteOffset(ref searchSpace, num + 1))
				{
					goto IL_01bd;
				}
				if (value == Unsafe.AddByteOffset(ref searchSpace, num))
				{
					goto IL_01ba;
				}
			}
			while (num2 != 0)
			{
				num2--;
				num--;
				if (value != Unsafe.AddByteOffset(ref searchSpace, num))
				{
					continue;
				}
				goto IL_01ba;
			}
			if (Vector.IsHardwareAccelerated && num != 0)
			{
				num2 = num & (nuint)(~(Vector<byte>.Count - 1));
				Vector<byte> left = new Vector<byte>(value);
				while (num2 > (nuint)(Vector<byte>.Count - 1))
				{
					Vector<byte> vector = Vector.Equals(left, LoadVector(ref searchSpace, num - (nuint)Vector<byte>.Count));
					if (Vector<byte>.Zero.Equals(vector))
					{
						num -= (nuint)Vector<byte>.Count;
						num2 -= (nuint)Vector<byte>.Count;
						continue;
					}
					return (int)num - Vector<byte>.Count + LocateLastFoundByte(vector);
				}
				if (num != 0)
				{
					num2 = num;
					continue;
				}
			}
			return -1;
			IL_01ba:
			return (int)num;
			IL_01c3:
			return (int)(num + 2);
			IL_01c9:
			return (int)(num + 3);
			IL_01bd:
			return (int)(num + 1);
		}
		return (int)(num + 7);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static int IndexOfAny(ref byte searchSpace, byte value0, byte value1, int length)
	{
		nuint num = 0u;
		nuint num2 = (uint)length;
		if (Sse2.IsSupported || AdvSimd.Arm64.IsSupported)
		{
			nint num3 = (nint)length - (nint)Vector128<byte>.Count;
			if (num3 >= 0)
			{
				num2 = (nuint)num3;
				goto IL_0212;
			}
		}
		else if (Vector.IsHardwareAccelerated)
		{
			nint num4 = (nint)length - (nint)Vector<byte>.Count;
			if (num4 >= 0)
			{
				num2 = (nuint)num4;
				goto IL_0212;
			}
		}
		while (num2 >= 8)
		{
			num2 -= 8;
			uint num5 = Unsafe.AddByteOffset(ref searchSpace, num);
			if (value0 == num5 || value1 == num5)
			{
				goto IL_01e5;
			}
			num5 = Unsafe.AddByteOffset(ref searchSpace, num + 1);
			if (value0 == num5 || value1 == num5)
			{
				goto IL_01e8;
			}
			num5 = Unsafe.AddByteOffset(ref searchSpace, num + 2);
			if (value0 == num5 || value1 == num5)
			{
				goto IL_01ee;
			}
			num5 = Unsafe.AddByteOffset(ref searchSpace, num + 3);
			if (value0 != num5 && value1 != num5)
			{
				num5 = Unsafe.AddByteOffset(ref searchSpace, num + 4);
				if (value0 != num5 && value1 != num5)
				{
					num5 = Unsafe.AddByteOffset(ref searchSpace, num + 5);
					if (value0 != num5 && value1 != num5)
					{
						num5 = Unsafe.AddByteOffset(ref searchSpace, num + 6);
						if (value0 != num5 && value1 != num5)
						{
							num5 = Unsafe.AddByteOffset(ref searchSpace, num + 7);
							if (value0 != num5 && value1 != num5)
							{
								num += 8;
								continue;
							}
							return (int)(num + 7);
						}
						return (int)(num + 6);
					}
					return (int)(num + 5);
				}
				return (int)(num + 4);
			}
			goto IL_01f4;
		}
		if (num2 >= 4)
		{
			num2 -= 4;
			uint num5 = Unsafe.AddByteOffset(ref searchSpace, num);
			if (value0 == num5 || value1 == num5)
			{
				goto IL_01e5;
			}
			num5 = Unsafe.AddByteOffset(ref searchSpace, num + 1);
			if (value0 == num5 || value1 == num5)
			{
				goto IL_01e8;
			}
			num5 = Unsafe.AddByteOffset(ref searchSpace, num + 2);
			if (value0 == num5 || value1 == num5)
			{
				goto IL_01ee;
			}
			num5 = Unsafe.AddByteOffset(ref searchSpace, num + 3);
			if (value0 == num5 || value1 == num5)
			{
				goto IL_01f4;
			}
			num += 4;
		}
		while (num2 != 0)
		{
			uint num5 = Unsafe.AddByteOffset(ref searchSpace, num);
			if (value0 != num5 && value1 != num5)
			{
				num++;
				num2--;
				continue;
			}
			goto IL_01e5;
		}
		goto IL_01e3;
		IL_0212:
		int num6;
		if (Sse2.IsSupported)
		{
			if (Avx2.IsSupported && num2 >= (nuint)Vector128<byte>.Count)
			{
				Vector256<byte> left = Vector256.Create(value0);
				Vector256<byte> left2 = Vector256.Create(value1);
				num2 -= (nuint)Vector128<byte>.Count;
				Vector256<byte> right;
				while (num2 > num)
				{
					right = LoadVector256(ref searchSpace, num);
					num6 = Avx2.MoveMask(Avx2.Or(Avx2.CompareEqual(left, right), Avx2.CompareEqual(left2, right)));
					if (num6 == 0)
					{
						num += (nuint)Vector256<byte>.Count;
						continue;
					}
					goto IL_033c;
				}
				right = LoadVector256(ref searchSpace, num2);
				num = num2;
				num6 = Avx2.MoveMask(Avx2.Or(Avx2.CompareEqual(left, right), Avx2.CompareEqual(left2, right)));
				if (num6 == 0)
				{
					goto IL_01e3;
				}
			}
			else
			{
				Vector128<byte> left3 = Vector128.Create(value0);
				Vector128<byte> left4 = Vector128.Create(value1);
				Vector128<byte> right2;
				while (num2 > num)
				{
					right2 = LoadVector128(ref searchSpace, num);
					num6 = Sse2.MoveMask(Sse2.Or(Sse2.CompareEqual(left3, right2), Sse2.CompareEqual(left4, right2)).AsByte());
					if (num6 == 0)
					{
						num += (nuint)Vector128<byte>.Count;
						continue;
					}
					goto IL_033c;
				}
				right2 = LoadVector128(ref searchSpace, num2);
				num = num2;
				num6 = Sse2.MoveMask(Sse2.Or(Sse2.CompareEqual(left3, right2), Sse2.CompareEqual(left4, right2)));
				if (num6 == 0)
				{
					goto IL_01e3;
				}
			}
			goto IL_033c;
		}
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		if (!Vector.IsHardwareAccelerated)
		{
			goto IL_01e5;
		}
		Vector<byte> right3 = new Vector<byte>(value0);
		Vector<byte> right4 = new Vector<byte>(value1);
		Vector<byte> left5;
		while (num2 > num)
		{
			left5 = LoadVector(ref searchSpace, num);
			left5 = Vector.BitwiseOr(Vector.Equals(left5, right3), Vector.Equals(left5, right4));
			if (Vector<byte>.Zero.Equals(left5))
			{
				num += (nuint)Vector<byte>.Count;
				continue;
			}
			goto IL_03ec;
		}
		left5 = LoadVector(ref searchSpace, num2);
		num = num2;
		left5 = Vector.BitwiseOr(Vector.Equals(left5, right3), Vector.Equals(left5, right4));
		if (Vector<byte>.Zero.Equals(left5))
		{
			goto IL_01e3;
		}
		goto IL_03ec;
		IL_033c:
		num += (nuint)BitOperations.TrailingZeroCount(num6);
		goto IL_01e5;
		IL_01e3:
		return -1;
		IL_01e5:
		return (int)num;
		IL_03ec:
		num += (nuint)LocateFirstFoundByte(left5);
		goto IL_01e5;
		IL_01ee:
		return (int)(num + 2);
		IL_01e8:
		return (int)(num + 1);
		IL_01f4:
		return (int)(num + 3);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static int IndexOfAny(ref byte searchSpace, byte value0, byte value1, byte value2, int length)
	{
		nuint num = 0u;
		nuint num2 = (uint)length;
		if (Sse2.IsSupported || AdvSimd.Arm64.IsSupported)
		{
			if (length >= Vector128<byte>.Count * 2)
			{
				num2 = UnalignedCountVector128(ref searchSpace);
			}
		}
		else if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
		{
			num2 = UnalignedCountVector(ref searchSpace);
		}
		while (true)
		{
			if (num2 >= 8)
			{
				num2 -= 8;
				uint num3 = Unsafe.AddByteOffset(ref searchSpace, num);
				if (value0 == num3 || value1 == num3 || value2 == num3)
				{
					goto IL_0504;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num + 1);
				if (value0 == num3 || value1 == num3 || value2 == num3)
				{
					goto IL_0507;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num + 2);
				if (value0 == num3 || value1 == num3 || value2 == num3)
				{
					goto IL_050d;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num + 3);
				if (value0 != num3 && value1 != num3 && value2 != num3)
				{
					num3 = Unsafe.AddByteOffset(ref searchSpace, num + 4);
					if (value0 != num3 && value1 != num3 && value2 != num3)
					{
						num3 = Unsafe.AddByteOffset(ref searchSpace, num + 5);
						if (value0 != num3 && value1 != num3 && value2 != num3)
						{
							num3 = Unsafe.AddByteOffset(ref searchSpace, num + 6);
							if (value0 != num3 && value1 != num3 && value2 != num3)
							{
								num3 = Unsafe.AddByteOffset(ref searchSpace, num + 7);
								if (value0 == num3 || value1 == num3 || value2 == num3)
								{
									break;
								}
								num += 8;
								continue;
							}
							return (int)(num + 6);
						}
						return (int)(num + 5);
					}
					return (int)(num + 4);
				}
				goto IL_0513;
			}
			if (num2 >= 4)
			{
				num2 -= 4;
				uint num3 = Unsafe.AddByteOffset(ref searchSpace, num);
				if (value0 == num3 || value1 == num3 || value2 == num3)
				{
					goto IL_0504;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num + 1);
				if (value0 == num3 || value1 == num3 || value2 == num3)
				{
					goto IL_0507;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num + 2);
				if (value0 == num3 || value1 == num3 || value2 == num3)
				{
					goto IL_050d;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num + 3);
				if (value0 == num3 || value1 == num3 || value2 == num3)
				{
					goto IL_0513;
				}
				num += 4;
			}
			while (num2 != 0)
			{
				num2--;
				uint num3 = Unsafe.AddByteOffset(ref searchSpace, num);
				if (value0 != num3 && value1 != num3 && value2 != num3)
				{
					num++;
					continue;
				}
				goto IL_0504;
			}
			if (Avx2.IsSupported)
			{
				if (num < (uint)length)
				{
					num2 = GetByteVector256SpanLength(num, length);
					if (num2 > num)
					{
						Vector256<byte> left = Vector256.Create(value0);
						Vector256<byte> left2 = Vector256.Create(value1);
						Vector256<byte> left3 = Vector256.Create(value2);
						do
						{
							Vector256<byte> right = LoadVector256(ref searchSpace, num);
							Vector256<byte> left4 = Avx2.CompareEqual(left, right);
							Vector256<byte> right2 = Avx2.CompareEqual(left2, right);
							Vector256<byte> right3 = Avx2.CompareEqual(left3, right);
							int num4 = Avx2.MoveMask(Avx2.Or(Avx2.Or(left4, right2), right3));
							if (num4 == 0)
							{
								num += (nuint)Vector256<byte>.Count;
								continue;
							}
							return (int)(num + (uint)BitOperations.TrailingZeroCount(num4));
						}
						while (num2 > num);
					}
					num2 = GetByteVector128SpanLength(num, length);
					if (num2 > num)
					{
						Vector128<byte> left5 = Vector128.Create(value0);
						Vector128<byte> left6 = Vector128.Create(value1);
						Vector128<byte> left7 = Vector128.Create(value2);
						Vector128<byte> right4 = LoadVector128(ref searchSpace, num);
						Vector128<byte> left8 = Sse2.CompareEqual(left5, right4);
						Vector128<byte> right5 = Sse2.CompareEqual(left6, right4);
						Vector128<byte> right6 = Sse2.CompareEqual(left7, right4);
						int num5 = Sse2.MoveMask(Sse2.Or(Sse2.Or(left8, right5), right6));
						if (num5 != 0)
						{
							return (int)(num + (uint)BitOperations.TrailingZeroCount(num5));
						}
						num += (nuint)Vector128<byte>.Count;
					}
					if (num < (uint)length)
					{
						num2 = (uint)length - num;
						continue;
					}
				}
			}
			else if (Sse2.IsSupported)
			{
				if (num < (uint)length)
				{
					num2 = GetByteVector128SpanLength(num, length);
					Vector128<byte> left9 = Vector128.Create(value0);
					Vector128<byte> left10 = Vector128.Create(value1);
					Vector128<byte> left11 = Vector128.Create(value2);
					for (; num2 > num; num += (nuint)Vector128<byte>.Count)
					{
						Vector128<byte> right7 = LoadVector128(ref searchSpace, num);
						Vector128<byte> left12 = Sse2.CompareEqual(left9, right7);
						Vector128<byte> right8 = Sse2.CompareEqual(left10, right7);
						Vector128<byte> right9 = Sse2.CompareEqual(left11, right7);
						int num6 = Sse2.MoveMask(Sse2.Or(Sse2.Or(left12, right8), right9));
						if (num6 != 0)
						{
							return (int)(num + (uint)BitOperations.TrailingZeroCount(num6));
						}
					}
					if (num < (uint)length)
					{
						num2 = (uint)length - num;
						continue;
					}
				}
			}
			else
			{
				if (AdvSimd.Arm64.IsSupported)
				{
				}
				if (Vector.IsHardwareAccelerated && num < (uint)length)
				{
					num2 = GetByteVectorSpanLength(num, length);
					Vector<byte> right10 = new Vector<byte>(value0);
					Vector<byte> right11 = new Vector<byte>(value1);
					Vector<byte> right12 = new Vector<byte>(value2);
					for (; num2 > num; num += (nuint)Vector<byte>.Count)
					{
						Vector<byte> left13 = LoadVector(ref searchSpace, num);
						Vector<byte> vector = Vector.BitwiseOr(Vector.BitwiseOr(Vector.Equals(left13, right10), Vector.Equals(left13, right11)), Vector.Equals(left13, right12));
						if (!Vector<byte>.Zero.Equals(vector))
						{
							return (int)num + LocateFirstFoundByte(vector);
						}
					}
					if (num < (uint)length)
					{
						num2 = (uint)length - num;
						continue;
					}
				}
			}
			return -1;
			IL_0507:
			return (int)(num + 1);
			IL_0513:
			return (int)(num + 3);
			IL_050d:
			return (int)(num + 2);
			IL_0504:
			return (int)num;
		}
		return (int)(num + 7);
	}

	public static int LastIndexOfAny(ref byte searchSpace, byte value0, byte value1, int length)
	{
		nuint num = (uint)length;
		nuint num2 = (uint)length;
		if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
		{
			num2 = UnalignedCountVectorFromEnd(ref searchSpace, length);
		}
		while (true)
		{
			if (num2 >= 8)
			{
				num2 -= 8;
				num -= 8;
				uint num3 = Unsafe.AddByteOffset(ref searchSpace, num + 7);
				if (value0 == num3 || value1 == num3)
				{
					break;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num + 6);
				if (value0 != num3 && value1 != num3)
				{
					num3 = Unsafe.AddByteOffset(ref searchSpace, num + 5);
					if (value0 != num3 && value1 != num3)
					{
						num3 = Unsafe.AddByteOffset(ref searchSpace, num + 4);
						if (value0 != num3 && value1 != num3)
						{
							num3 = Unsafe.AddByteOffset(ref searchSpace, num + 3);
							if (value0 != num3 && value1 != num3)
							{
								num3 = Unsafe.AddByteOffset(ref searchSpace, num + 2);
								if (value0 != num3 && value1 != num3)
								{
									num3 = Unsafe.AddByteOffset(ref searchSpace, num + 1);
									if (value0 != num3 && value1 != num3)
									{
										num3 = Unsafe.AddByteOffset(ref searchSpace, num);
										if (value0 != num3 && value1 != num3)
										{
											continue;
										}
										goto IL_027c;
									}
									goto IL_027f;
								}
								goto IL_0285;
							}
							goto IL_028b;
						}
						return (int)(num + 4);
					}
					return (int)(num + 5);
				}
				return (int)(num + 6);
			}
			if (num2 >= 4)
			{
				num2 -= 4;
				num -= 4;
				uint num3 = Unsafe.AddByteOffset(ref searchSpace, num + 3);
				if (value0 == num3 || value1 == num3)
				{
					goto IL_028b;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num + 2);
				if (value0 == num3 || value1 == num3)
				{
					goto IL_0285;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num + 1);
				if (value0 == num3 || value1 == num3)
				{
					goto IL_027f;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num);
				if (value0 == num3 || value1 == num3)
				{
					goto IL_027c;
				}
			}
			while (num2 != 0)
			{
				num2--;
				num--;
				uint num3 = Unsafe.AddByteOffset(ref searchSpace, num);
				if (value0 != num3 && value1 != num3)
				{
					continue;
				}
				goto IL_027c;
			}
			if (Vector.IsHardwareAccelerated && num != 0)
			{
				num2 = num & (nuint)(~(Vector<byte>.Count - 1));
				Vector<byte> right = new Vector<byte>(value0);
				Vector<byte> right2 = new Vector<byte>(value1);
				while (num2 > (nuint)(Vector<byte>.Count - 1))
				{
					Vector<byte> left = LoadVector(ref searchSpace, num - (nuint)Vector<byte>.Count);
					Vector<byte> vector = Vector.BitwiseOr(Vector.Equals(left, right), Vector.Equals(left, right2));
					if (Vector<byte>.Zero.Equals(vector))
					{
						num -= (nuint)Vector<byte>.Count;
						num2 -= (nuint)Vector<byte>.Count;
						continue;
					}
					return (int)num - Vector<byte>.Count + LocateLastFoundByte(vector);
				}
				if (num != 0)
				{
					num2 = num;
					continue;
				}
			}
			return -1;
			IL_027c:
			return (int)num;
			IL_028b:
			return (int)(num + 3);
			IL_027f:
			return (int)(num + 1);
			IL_0285:
			return (int)(num + 2);
		}
		return (int)(num + 7);
	}

	public static int LastIndexOfAny(ref byte searchSpace, byte value0, byte value1, byte value2, int length)
	{
		nuint num = (uint)length;
		nuint num2 = (uint)length;
		if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
		{
			num2 = UnalignedCountVectorFromEnd(ref searchSpace, length);
		}
		while (true)
		{
			if (num2 >= 8)
			{
				num2 -= 8;
				num -= 8;
				uint num3 = Unsafe.AddByteOffset(ref searchSpace, num + 7);
				if (value0 == num3 || value1 == num3 || value2 == num3)
				{
					break;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num + 6);
				if (value0 != num3 && value1 != num3 && value2 != num3)
				{
					num3 = Unsafe.AddByteOffset(ref searchSpace, num + 5);
					if (value0 != num3 && value1 != num3 && value2 != num3)
					{
						num3 = Unsafe.AddByteOffset(ref searchSpace, num + 4);
						if (value0 != num3 && value1 != num3 && value2 != num3)
						{
							num3 = Unsafe.AddByteOffset(ref searchSpace, num + 3);
							if (value0 != num3 && value1 != num3 && value2 != num3)
							{
								num3 = Unsafe.AddByteOffset(ref searchSpace, num + 2);
								if (value0 != num3 && value1 != num3 && value2 != num3)
								{
									num3 = Unsafe.AddByteOffset(ref searchSpace, num + 1);
									if (value0 != num3 && value1 != num3 && value2 != num3)
									{
										num3 = Unsafe.AddByteOffset(ref searchSpace, num);
										if (value0 != num3 && value1 != num3 && value2 != num3)
										{
											continue;
										}
										goto IL_0310;
									}
									goto IL_0313;
								}
								goto IL_0319;
							}
							goto IL_031f;
						}
						return (int)(num + 4);
					}
					return (int)(num + 5);
				}
				return (int)(num + 6);
			}
			if (num2 >= 4)
			{
				num2 -= 4;
				num -= 4;
				uint num3 = Unsafe.AddByteOffset(ref searchSpace, num + 3);
				if (value0 == num3 || value1 == num3 || value2 == num3)
				{
					goto IL_031f;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num + 2);
				if (value0 == num3 || value1 == num3 || value2 == num3)
				{
					goto IL_0319;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num + 1);
				if (value0 == num3 || value1 == num3 || value2 == num3)
				{
					goto IL_0313;
				}
				num3 = Unsafe.AddByteOffset(ref searchSpace, num);
				if (value0 == num3 || value1 == num3 || value2 == num3)
				{
					goto IL_0310;
				}
			}
			while (num2 != 0)
			{
				num2--;
				num--;
				uint num3 = Unsafe.AddByteOffset(ref searchSpace, num);
				if (value0 != num3 && value1 != num3 && value2 != num3)
				{
					continue;
				}
				goto IL_0310;
			}
			if (Vector.IsHardwareAccelerated && num != 0)
			{
				num2 = num & (nuint)(~(Vector<byte>.Count - 1));
				Vector<byte> right = new Vector<byte>(value0);
				Vector<byte> right2 = new Vector<byte>(value1);
				Vector<byte> right3 = new Vector<byte>(value2);
				while (num2 > (nuint)(Vector<byte>.Count - 1))
				{
					Vector<byte> left = LoadVector(ref searchSpace, num - (nuint)Vector<byte>.Count);
					Vector<byte> vector = Vector.BitwiseOr(Vector.BitwiseOr(Vector.Equals(left, right), Vector.Equals(left, right2)), Vector.Equals(left, right3));
					if (Vector<byte>.Zero.Equals(vector))
					{
						num -= (nuint)Vector<byte>.Count;
						num2 -= (nuint)Vector<byte>.Count;
						continue;
					}
					return (int)num - Vector<byte>.Count + LocateLastFoundByte(vector);
				}
				if (num != 0)
				{
					num2 = num;
					continue;
				}
			}
			return -1;
			IL_0310:
			return (int)num;
			IL_031f:
			return (int)(num + 3);
			IL_0319:
			return (int)(num + 2);
			IL_0313:
			return (int)(num + 1);
		}
		return (int)(num + 7);
	}

	public unsafe static bool SequenceEqual(ref byte first, ref byte second, nuint length)
	{
		if (length < (nuint)sizeof(UIntPtr))
		{
			if (length < 4)
			{
				uint num = 0u;
				nuint num2 = length & 2;
				if (num2 != 0)
				{
					num = LoadUShort(ref first);
					num -= LoadUShort(ref second);
				}
				if ((length & 1) != 0)
				{
					num |= (uint)(Unsafe.AddByteOffset(ref first, num2) - Unsafe.AddByteOffset(ref second, num2));
				}
				return num == 0;
			}
			nuint offset = length - 4;
			uint num3 = LoadUInt(ref first) - LoadUInt(ref second);
			num3 |= LoadUInt(ref first, offset) - LoadUInt(ref second, offset);
			return num3 == 0;
		}
		if (Unsafe.AreSame(ref first, ref second))
		{
			goto IL_0087;
		}
		nuint num5;
		nuint num7;
		nuint num9;
		Vector128<byte> value2;
		Vector256<byte> value;
		if (Sse2.IsSupported)
		{
			if (Avx2.IsSupported && length >= (nuint)Vector256<byte>.Count)
			{
				nuint num4 = 0u;
				num5 = length - (nuint)Vector256<byte>.Count;
				if (num5 == 0)
				{
					goto IL_00ea;
				}
				while (true)
				{
					value = Avx2.CompareEqual(LoadVector256(ref first, num4), LoadVector256(ref second, num4));
					if (Avx2.MoveMask(value) != -1)
					{
						break;
					}
					num4 += (nuint)Vector256<byte>.Count;
					if (num5 > num4)
					{
						continue;
					}
					goto IL_00ea;
				}
			}
			else
			{
				if (length < 16)
				{
					goto IL_01fc;
				}
				nuint num6 = 0u;
				num7 = length - 16;
				if (num7 == 0)
				{
					goto IL_0161;
				}
				while (true)
				{
					value2 = Sse2.CompareEqual(LoadVector128(ref first, num6), LoadVector128(ref second, num6));
					if (Sse2.MoveMask(value2) != 65535)
					{
						break;
					}
					num6 += 16;
					if (num7 > num6)
					{
						continue;
					}
					goto IL_0161;
				}
			}
		}
		else
		{
			if (!Vector.IsHardwareAccelerated || length < (nuint)Vector<byte>.Count)
			{
				goto IL_01fc;
			}
			nuint num8 = 0u;
			num9 = length - (nuint)Vector<byte>.Count;
			if (num9 == 0)
			{
				goto IL_01dd;
			}
			while (!(LoadVector(ref first, num8) != LoadVector(ref second, num8)))
			{
				num8 += (nuint)Vector<byte>.Count;
				if (num9 > num8)
				{
					continue;
				}
				goto IL_01dd;
			}
		}
		goto IL_0290;
		IL_01dd:
		if (LoadVector(ref first, num9) == LoadVector(ref second, num9))
		{
			goto IL_0087;
		}
		goto IL_0290;
		IL_0278:
		nuint num10;
		return LoadNUInt(ref first, num10) == LoadNUInt(ref second, num10);
		IL_0161:
		value2 = Sse2.CompareEqual(LoadVector128(ref first, num7), LoadVector128(ref second, num7));
		if (Sse2.MoveMask(value2) == 65535)
		{
			goto IL_0087;
		}
		goto IL_0290;
		IL_00ea:
		value = Avx2.CompareEqual(LoadVector256(ref first, num5), LoadVector256(ref second, num5));
		if (Avx2.MoveMask(value) == -1)
		{
			goto IL_0087;
		}
		goto IL_0290;
		IL_0290:
		return false;
		IL_0087:
		return true;
		IL_01fc:
		if (Sse2.IsSupported)
		{
			nuint offset2 = length - (nuint)sizeof(UIntPtr);
			nuint num11 = LoadNUInt(ref first) - LoadNUInt(ref second);
			num11 |= LoadNUInt(ref first, offset2) - LoadNUInt(ref second, offset2);
			return num11 == 0;
		}
		nuint num12 = 0u;
		num10 = length - (nuint)sizeof(UIntPtr);
		if (num10 == 0)
		{
			goto IL_0278;
		}
		while (LoadNUInt(ref first, num12) == LoadNUInt(ref second, num12))
		{
			num12 += (nuint)sizeof(UIntPtr);
			if (num10 > num12)
			{
				continue;
			}
			goto IL_0278;
		}
		goto IL_0290;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int LocateFirstFoundByte(Vector<byte> match)
	{
		Vector<ulong> vector = Vector.AsVectorUInt64(match);
		ulong num = 0uL;
		int i;
		for (i = 0; i < Vector<ulong>.Count; i++)
		{
			num = vector[i];
			if (num != 0L)
			{
				break;
			}
		}
		return i * 8 + LocateFirstFoundByte(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public unsafe static int SequenceCompareTo(ref byte first, int firstLength, ref byte second, int secondLength)
	{
		nuint num;
		uint num6;
		nuint num3;
		nuint num2;
		if (!Unsafe.AreSame(ref first, ref second))
		{
			num = (uint)(((uint)firstLength < (uint)secondLength) ? firstLength : secondLength);
			num2 = 0u;
			num3 = num;
			if (!Avx2.IsSupported)
			{
				if (Sse2.IsSupported)
				{
					if (num3 >= (nuint)Vector128<byte>.Count)
					{
						num3 -= (nuint)Vector128<byte>.Count;
						while (true)
						{
							uint num4;
							if (num3 > num2)
							{
								num4 = (uint)Sse2.MoveMask(Sse2.CompareEqual(LoadVector128(ref first, num2), LoadVector128(ref second, num2)));
								if (num4 == 65535)
								{
									num2 += (nuint)Vector128<byte>.Count;
									continue;
								}
							}
							else
							{
								num2 = num3;
								num4 = (uint)Sse2.MoveMask(Sse2.CompareEqual(LoadVector128(ref first, num2), LoadVector128(ref second, num2)));
								if (num4 == 65535)
								{
									break;
								}
							}
							uint value = ~num4;
							num2 += (uint)BitOperations.TrailingZeroCount(value);
							return Unsafe.AddByteOffset(ref first, num2).CompareTo(Unsafe.AddByteOffset(ref second, num2));
						}
						goto IL_0277;
					}
				}
				else if (Vector.IsHardwareAccelerated && num3 > (nuint)Vector<byte>.Count)
				{
					for (num3 -= (nuint)Vector<byte>.Count; num3 > num2 && !(LoadVector(ref first, num2) != LoadVector(ref second, num2)); num2 += (nuint)Vector<byte>.Count)
					{
					}
					goto IL_0273;
				}
				goto IL_021b;
			}
			if (num3 >= (nuint)Vector256<byte>.Count)
			{
				num3 -= (nuint)Vector256<byte>.Count;
				while (true)
				{
					uint num5;
					if (num3 > num2)
					{
						num5 = (uint)Avx2.MoveMask(Avx2.CompareEqual(LoadVector256(ref first, num2), LoadVector256(ref second, num2)));
						if (num5 == uint.MaxValue)
						{
							num2 += (nuint)Vector256<byte>.Count;
							continue;
						}
					}
					else
					{
						num2 = num3;
						num5 = (uint)Avx2.MoveMask(Avx2.CompareEqual(LoadVector256(ref first, num2), LoadVector256(ref second, num2)));
						if (num5 == uint.MaxValue)
						{
							break;
						}
					}
					uint value2 = ~num5;
					num2 += (uint)BitOperations.TrailingZeroCount(value2);
					return Unsafe.AddByteOffset(ref first, num2).CompareTo(Unsafe.AddByteOffset(ref second, num2));
				}
			}
			else
			{
				if (num3 < (nuint)Vector128<byte>.Count)
				{
					goto IL_021b;
				}
				num3 -= (nuint)Vector128<byte>.Count;
				if (num3 > num2)
				{
					num6 = (uint)Sse2.MoveMask(Sse2.CompareEqual(LoadVector128(ref first, num2), LoadVector128(ref second, num2)));
					if (num6 != 65535)
					{
						goto IL_0111;
					}
				}
				num2 = num3;
				num6 = (uint)Sse2.MoveMask(Sse2.CompareEqual(LoadVector128(ref first, num2), LoadVector128(ref second, num2)));
				if (num6 != 65535)
				{
					goto IL_0111;
				}
			}
		}
		goto IL_0277;
		IL_0277:
		return firstLength - secondLength;
		IL_0273:
		for (; num > num2; num2++)
		{
			int num7 = Unsafe.AddByteOffset(ref first, num2).CompareTo(Unsafe.AddByteOffset(ref second, num2));
			if (num7 != 0)
			{
				return num7;
			}
		}
		goto IL_0277;
		IL_021b:
		if (num3 > (nuint)sizeof(UIntPtr))
		{
			for (num3 -= (nuint)sizeof(UIntPtr); num3 > num2 && LoadNUInt(ref first, num2) == LoadNUInt(ref second, num2); num2 += (nuint)sizeof(UIntPtr))
			{
			}
		}
		goto IL_0273;
		IL_0111:
		uint value3 = ~num6;
		num2 += (uint)BitOperations.TrailingZeroCount(value3);
		return Unsafe.AddByteOffset(ref first, num2).CompareTo(Unsafe.AddByteOffset(ref second, num2));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int LocateLastFoundByte(Vector<byte> match)
	{
		Vector<ulong> vector = Vector.AsVectorUInt64(match);
		ulong num = 0uL;
		int num2 = Vector<ulong>.Count - 1;
		for (int i = 0; i < Vector<ulong>.Count; i++)
		{
			num = vector[num2];
			if (num != 0L)
			{
				break;
			}
			num2--;
		}
		return num2 * 8 + LocateLastFoundByte(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int LocateFirstFoundByte(ulong match)
	{
		return BitOperations.TrailingZeroCount(match) >> 3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int LocateLastFoundByte(ulong match)
	{
		return BitOperations.Log2(match) >> 3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ushort LoadUShort(ref byte start)
	{
		return Unsafe.ReadUnaligned<ushort>(ref start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint LoadUInt(ref byte start)
	{
		return Unsafe.ReadUnaligned<uint>(ref start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint LoadUInt(ref byte start, nuint offset)
	{
		return Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref start, offset));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nuint LoadNUInt(ref byte start)
	{
		return Unsafe.ReadUnaligned<UIntPtr>(ref start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nuint LoadNUInt(ref byte start, nuint offset)
	{
		return Unsafe.ReadUnaligned<UIntPtr>(ref Unsafe.AddByteOffset(ref start, offset));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector<byte> LoadVector(ref byte start, nuint offset)
	{
		return Unsafe.ReadUnaligned<Vector<byte>>(ref Unsafe.AddByteOffset(ref start, offset));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<byte> LoadVector128(ref byte start, nuint offset)
	{
		return Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.AddByteOffset(ref start, offset));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<byte> LoadVector256(ref byte start, nuint offset)
	{
		return Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.AddByteOffset(ref start, offset));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nuint GetByteVectorSpanLength(nuint offset, int length)
	{
		return (uint)((length - (int)offset) & ~(Vector<byte>.Count - 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nuint GetByteVector128SpanLength(nuint offset, int length)
	{
		return (uint)((length - (int)offset) & ~(Vector128<byte>.Count - 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nuint GetByteVector256SpanLength(nuint offset, int length)
	{
		return (uint)((length - (int)offset) & ~(Vector256<byte>.Count - 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static nuint UnalignedCountVector(ref byte searchSpace)
	{
		nint num = (nint)Unsafe.AsPointer(ref searchSpace) & (nint)(Vector<byte>.Count - 1);
		return (nuint)((Vector<byte>.Count - num) & (Vector<byte>.Count - 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static nuint UnalignedCountVector128(ref byte searchSpace)
	{
		nint num = (nint)Unsafe.AsPointer(ref searchSpace) & (nint)(Vector128<byte>.Count - 1);
		return (uint)((Vector128<byte>.Count - num) & (Vector128<byte>.Count - 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static nuint UnalignedCountVectorFromEnd(ref byte searchSpace, int length)
	{
		nint num = (nint)Unsafe.AsPointer(ref searchSpace) & (nint)(Vector<byte>.Count - 1);
		return (uint)(((length & (Vector<byte>.Count - 1)) + num) & (Vector<byte>.Count - 1));
	}

	public static int IndexOf(ref char searchSpace, int searchSpaceLength, ref char value, int valueLength)
	{
		if (valueLength == 0)
		{
			return 0;
		}
		char value2 = value;
		ref char source = ref Unsafe.Add(ref value, 1);
		int num = valueLength - 1;
		int num2 = searchSpaceLength - num;
		int num3 = 0;
		while (num2 > 0)
		{
			int num4 = IndexOf(ref Unsafe.Add(ref searchSpace, num3), value2, num2);
			if (num4 == -1)
			{
				break;
			}
			num2 -= num4;
			num3 += num4;
			if (num2 <= 0)
			{
				break;
			}
			if (SequenceEqual(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref searchSpace, num3 + 1)), ref Unsafe.As<char, byte>(ref source), (nuint)(uint)num * (nuint)2u))
			{
				return num3;
			}
			num2--;
			num3++;
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public unsafe static int SequenceCompareTo(ref char first, int firstLength, ref char second, int secondLength)
	{
		int result = firstLength - secondLength;
		if (!Unsafe.AreSame(ref first, ref second))
		{
			nuint num = (uint)(((uint)firstLength < (uint)secondLength) ? firstLength : secondLength);
			nuint num2 = 0u;
			if (num >= (nuint)(sizeof(UIntPtr) / 2))
			{
				if (Vector.IsHardwareAccelerated && num >= (nuint)Vector<ushort>.Count)
				{
					nuint num3 = num - (nuint)Vector<ushort>.Count;
					while (!(Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref first, (nint)num2))) != Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref second, (nint)num2)))))
					{
						num2 += (nuint)Vector<ushort>.Count;
						if (num3 < num2)
						{
							break;
						}
					}
				}
				for (; num >= (nuint)((nint)num2 + (nint)(sizeof(UIntPtr) / 2)) && Unsafe.ReadUnaligned<UIntPtr>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref first, (nint)num2))) == Unsafe.ReadUnaligned<UIntPtr>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref second, (nint)num2))); num2 += (nuint)(sizeof(UIntPtr) / 2))
				{
				}
			}
			if (num >= num2 + 2 && Unsafe.ReadUnaligned<int>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref first, (nint)num2))) == Unsafe.ReadUnaligned<int>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref second, (nint)num2))))
			{
				num2 += 2;
			}
			for (; num2 < num; num2++)
			{
				int num4 = Unsafe.Add(ref first, (nint)num2).CompareTo(Unsafe.Add(ref second, (nint)num2));
				if (num4 != 0)
				{
					return num4;
				}
			}
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public unsafe static bool Contains(ref char searchSpace, char value, int length)
	{
		fixed (char* ptr = &searchSpace)
		{
			char* ptr2 = ptr;
			char* ptr3 = ptr2 + length;
			if (Vector.IsHardwareAccelerated && length >= Vector<ushort>.Count * 2)
			{
				int num = ((int)ptr2 & (Unsafe.SizeOf<Vector<ushort>>() - 1)) / 2;
				length = (Vector<ushort>.Count - num) & (Vector<ushort>.Count - 1);
			}
			while (true)
			{
				if (length >= 4)
				{
					length -= 4;
					if (value == *ptr2 || value == ptr2[1] || value == ptr2[2] || value == ptr2[3])
					{
						break;
					}
					ptr2 += 4;
					continue;
				}
				while (length > 0)
				{
					length--;
					if (value == *ptr2)
					{
						goto end_IL_0079;
					}
					ptr2++;
				}
				if (Vector.IsHardwareAccelerated && ptr2 < ptr3)
				{
					length = (int)((ptr3 - ptr2) & ~(Vector<ushort>.Count - 1));
					Vector<ushort> left = new Vector<ushort>(value);
					while (length > 0)
					{
						Vector<ushort> other = Vector.Equals(left, Unsafe.Read<Vector<ushort>>(ptr2));
						if (!Vector<ushort>.Zero.Equals(other))
						{
							goto end_IL_0079;
						}
						ptr2 += Vector<ushort>.Count;
						length -= Vector<ushort>.Count;
					}
					if (ptr2 < ptr3)
					{
						length = (int)(ptr3 - ptr2);
						continue;
					}
				}
				return false;
				continue;
				end_IL_0079:
				break;
			}
			return true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public unsafe static int IndexOf(ref char searchSpace, char value, int length)
	{
		nint num = 0;
		nint num2 = length;
		if (((int)Unsafe.AsPointer(ref searchSpace) & 1) == 0)
		{
			if (Sse2.IsSupported || AdvSimd.Arm64.IsSupported)
			{
				if (length >= Vector128<ushort>.Count * 2)
				{
					num2 = UnalignedCountVector128(ref searchSpace);
				}
			}
			else if (Vector.IsHardwareAccelerated && length >= Vector<ushort>.Count * 2)
			{
				num2 = UnalignedCountVector(ref searchSpace);
			}
		}
		while (true)
		{
			if (num2 >= 4)
			{
				ref char reference = ref Unsafe.Add(ref searchSpace, num);
				if (value == reference)
				{
					break;
				}
				if (value != Unsafe.Add(ref reference, 1))
				{
					if (value != Unsafe.Add(ref reference, 2))
					{
						if (value != Unsafe.Add(ref reference, 3))
						{
							num += 4;
							num2 -= 4;
							continue;
						}
						return (int)(num + 3);
					}
					return (int)(num + 2);
				}
				return (int)(num + 1);
			}
			while (num2 > 0)
			{
				if (value == Unsafe.Add(ref searchSpace, num))
				{
					goto end_IL_0090;
				}
				num++;
				num2--;
			}
			if (Avx2.IsSupported)
			{
				if (num < length)
				{
					if (((nuint)Unsafe.AsPointer(ref Unsafe.Add(ref searchSpace, num)) & (nuint)(Vector256<byte>.Count - 1)) != 0)
					{
						Vector128<ushort> left = Vector128.Create(value);
						Vector128<ushort> right = LoadVector128(ref searchSpace, num);
						int num3 = Sse2.MoveMask(Sse2.CompareEqual(left, right).AsByte());
						if (num3 != 0)
						{
							return (int)(num + (uint)BitOperations.TrailingZeroCount(num3) / 2u);
						}
						num += Vector128<ushort>.Count;
					}
					num2 = GetCharVector256SpanLength(num, length);
					if (num2 > 0)
					{
						Vector256<ushort> left2 = Vector256.Create(value);
						do
						{
							Vector256<ushort> right2 = LoadVector256(ref searchSpace, num);
							int num4 = Avx2.MoveMask(Avx2.CompareEqual(left2, right2).AsByte());
							if (num4 == 0)
							{
								num += Vector256<ushort>.Count;
								num2 -= Vector256<ushort>.Count;
								continue;
							}
							return (int)(num + (uint)BitOperations.TrailingZeroCount(num4) / 2u);
						}
						while (num2 > 0);
					}
					num2 = GetCharVector128SpanLength(num, length);
					if (num2 > 0)
					{
						Vector128<ushort> left3 = Vector128.Create(value);
						Vector128<ushort> right3 = LoadVector128(ref searchSpace, num);
						int num5 = Sse2.MoveMask(Sse2.CompareEqual(left3, right3).AsByte());
						if (num5 != 0)
						{
							return (int)(num + (uint)BitOperations.TrailingZeroCount(num5) / 2u);
						}
						num += Vector128<ushort>.Count;
					}
					if (num < length)
					{
						num2 = length - num;
						continue;
					}
				}
			}
			else if (Sse2.IsSupported)
			{
				if (num < length)
				{
					num2 = GetCharVector128SpanLength(num, length);
					if (num2 > 0)
					{
						Vector128<ushort> left4 = Vector128.Create(value);
						do
						{
							Vector128<ushort> right4 = LoadVector128(ref searchSpace, num);
							int num6 = Sse2.MoveMask(Sse2.CompareEqual(left4, right4).AsByte());
							if (num6 == 0)
							{
								num += Vector128<ushort>.Count;
								num2 -= Vector128<ushort>.Count;
								continue;
							}
							return (int)(num + (uint)BitOperations.TrailingZeroCount(num6) / 2u);
						}
						while (num2 > 0);
					}
					if (num < length)
					{
						num2 = length - num;
						continue;
					}
				}
			}
			else
			{
				if (AdvSimd.Arm64.IsSupported)
				{
				}
				if (Vector.IsHardwareAccelerated && num < length)
				{
					num2 = GetCharVectorSpanLength(num, length);
					if (num2 > 0)
					{
						Vector<ushort> left5 = new Vector<ushort>(value);
						do
						{
							Vector<ushort> vector = Vector.Equals(left5, LoadVector(ref searchSpace, num));
							if (Vector<ushort>.Zero.Equals(vector))
							{
								num += Vector<ushort>.Count;
								num2 -= Vector<ushort>.Count;
								continue;
							}
							return (int)(num + LocateFirstFoundChar(vector));
						}
						while (num2 > 0);
					}
					if (num < length)
					{
						num2 = length - num;
						continue;
					}
				}
			}
			return -1;
			continue;
			end_IL_0090:
			break;
		}
		return (int)num;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static int IndexOfAny(ref char searchStart, char value0, char value1, int length)
	{
		nuint num = 0u;
		nuint num2 = (uint)length;
		int num4;
		if (Sse2.IsSupported)
		{
			nint num3 = (nint)length - (nint)Vector128<ushort>.Count;
			if (num3 >= 0)
			{
				num2 = (nuint)num3;
				if (Sse2.IsSupported)
				{
					if (Avx2.IsSupported && num2 >= (nuint)Vector128<ushort>.Count)
					{
						Vector256<ushort> left = Vector256.Create(value0);
						Vector256<ushort> left2 = Vector256.Create(value1);
						num2 -= (nuint)Vector128<ushort>.Count;
						Vector256<ushort> right;
						while (num2 > num)
						{
							right = LoadVector256(ref searchStart, num);
							num4 = Avx2.MoveMask(Avx2.Or(Avx2.CompareEqual(left, right), Avx2.CompareEqual(left2, right)).AsByte());
							if (num4 == 0)
							{
								num += (nuint)Vector256<ushort>.Count;
								continue;
							}
							goto IL_0211;
						}
						right = LoadVector256(ref searchStart, num2);
						num = num2;
						num4 = Avx2.MoveMask(Avx2.Or(Avx2.CompareEqual(left, right), Avx2.CompareEqual(left2, right)).AsByte());
						if (num4 == 0)
						{
							goto IL_00c1;
						}
					}
					else
					{
						Vector128<ushort> left3 = Vector128.Create(value0);
						Vector128<ushort> left4 = Vector128.Create(value1);
						Vector128<ushort> right2;
						while (num2 > num)
						{
							right2 = LoadVector128(ref searchStart, num);
							num4 = Sse2.MoveMask(Sse2.Or(Sse2.CompareEqual(left3, right2), Sse2.CompareEqual(left4, right2)).AsByte());
							if (num4 == 0)
							{
								num += (nuint)Vector128<ushort>.Count;
								continue;
							}
							goto IL_0211;
						}
						right2 = LoadVector128(ref searchStart, num2);
						num = num2;
						num4 = Sse2.MoveMask(Sse2.Or(Sse2.CompareEqual(left3, right2), Sse2.CompareEqual(left4, right2)).AsByte());
						if (num4 == 0)
						{
							goto IL_00c1;
						}
					}
					goto IL_0211;
				}
				goto IL_0223;
			}
		}
		else if (Vector.IsHardwareAccelerated)
		{
			nint num5 = (nint)length - (nint)Vector<ushort>.Count;
			if (num5 >= 0)
			{
				num2 = (nuint)num5;
				goto IL_0223;
			}
		}
		while (num2 >= 4)
		{
			ref char reference = ref Add(ref searchStart, num);
			int num6 = reference;
			if (value0 != num6 && value1 != num6)
			{
				num6 = Unsafe.Add(ref reference, 1);
				if (value0 != num6 && value1 != num6)
				{
					num6 = Unsafe.Add(ref reference, 2);
					if (value0 != num6 && value1 != num6)
					{
						num6 = Unsafe.Add(ref reference, 3);
						if (value0 != num6 && value1 != num6)
						{
							num += 4;
							num2 -= 4;
							continue;
						}
						return (int)(num + 3);
					}
					return (int)(num + 2);
				}
				return (int)(num + 1);
			}
			goto IL_00d5;
		}
		while (num2 != 0)
		{
			int num6 = Add(ref searchStart, num);
			if (value0 != num6 && value1 != num6)
			{
				num++;
				num2--;
				continue;
			}
			goto IL_00d5;
		}
		goto IL_00c1;
		IL_02c3:
		Vector<ushort> vector;
		num += (uint)LocateFirstFoundChar(vector);
		goto IL_00d5;
		IL_00d5:
		return (int)num;
		IL_00c1:
		return -1;
		IL_0211:
		num += (nuint)(uint)BitOperations.TrailingZeroCount(num4) >> 1;
		goto IL_00d5;
		IL_0223:
		if (!Sse2.IsSupported && Vector.IsHardwareAccelerated)
		{
			Vector<ushort> right3 = new Vector<ushort>(value0);
			Vector<ushort> right4 = new Vector<ushort>(value1);
			while (num2 > num)
			{
				vector = LoadVector(ref searchStart, num);
				vector = Vector.BitwiseOr(Vector.Equals(vector, right3), Vector.Equals(vector, right4));
				if (Vector<ushort>.Zero.Equals(vector))
				{
					num += (nuint)Vector<ushort>.Count;
					continue;
				}
				goto IL_02c3;
			}
			vector = LoadVector(ref searchStart, num2);
			num = num2;
			vector = Vector.BitwiseOr(Vector.Equals(vector, right3), Vector.Equals(vector, right4));
			if (!Vector<ushort>.Zero.Equals(vector))
			{
				goto IL_02c3;
			}
		}
		goto IL_00c1;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static int IndexOfAny(ref char searchStart, char value0, char value1, char value2, int length)
	{
		nuint num = 0u;
		nuint num2 = (uint)length;
		int num4;
		if (Sse2.IsSupported)
		{
			nint num3 = (nint)length - (nint)Vector128<ushort>.Count;
			if (num3 >= 0)
			{
				num2 = (nuint)num3;
				if (Sse2.IsSupported)
				{
					if (Avx2.IsSupported && num2 >= (nuint)Vector128<ushort>.Count)
					{
						Vector256<ushort> left = Vector256.Create(value0);
						Vector256<ushort> left2 = Vector256.Create(value1);
						Vector256<ushort> left3 = Vector256.Create(value2);
						num2 -= (nuint)Vector128<ushort>.Count;
						Vector256<ushort> right;
						while (num2 > num)
						{
							right = LoadVector256(ref searchStart, num);
							num4 = Avx2.MoveMask(Avx2.Or(Avx2.Or(Avx2.CompareEqual(left, right), Avx2.CompareEqual(left2, right)), Avx2.CompareEqual(left3, right)).AsByte());
							if (num4 == 0)
							{
								num += (nuint)Vector256<ushort>.Count;
								continue;
							}
							goto IL_027f;
						}
						right = LoadVector256(ref searchStart, num2);
						num = num2;
						num4 = Avx2.MoveMask(Avx2.Or(Avx2.Or(Avx2.CompareEqual(left, right), Avx2.CompareEqual(left2, right)), Avx2.CompareEqual(left3, right)).AsByte());
						if (num4 == 0)
						{
							goto IL_00e7;
						}
					}
					else
					{
						Vector128<ushort> left4 = Vector128.Create(value0);
						Vector128<ushort> left5 = Vector128.Create(value1);
						Vector128<ushort> left6 = Vector128.Create(value2);
						Vector128<ushort> right2;
						while (num2 > num)
						{
							right2 = LoadVector128(ref searchStart, num);
							num4 = Sse2.MoveMask(Sse2.Or(Sse2.Or(Sse2.CompareEqual(left4, right2), Sse2.CompareEqual(left5, right2)), Sse2.CompareEqual(left6, right2)).AsByte());
							if (num4 == 0)
							{
								num += (nuint)Vector128<ushort>.Count;
								continue;
							}
							goto IL_027f;
						}
						right2 = LoadVector128(ref searchStart, num2);
						num = num2;
						num4 = Sse2.MoveMask(Sse2.Or(Sse2.Or(Sse2.CompareEqual(left4, right2), Sse2.CompareEqual(left5, right2)), Sse2.CompareEqual(left6, right2)).AsByte());
						if (num4 == 0)
						{
							goto IL_00e7;
						}
					}
					goto IL_027f;
				}
				goto IL_0291;
			}
		}
		else if (Vector.IsHardwareAccelerated)
		{
			nint num5 = (nint)length - (nint)Vector<ushort>.Count;
			if (num5 >= 0)
			{
				num2 = (nuint)num5;
				goto IL_0291;
			}
		}
		while (num2 >= 4)
		{
			ref char reference = ref Add(ref searchStart, num);
			int num6 = reference;
			if (value0 != num6 && value1 != num6 && value2 != num6)
			{
				num6 = Unsafe.Add(ref reference, 1);
				if (value0 != num6 && value1 != num6 && value2 != num6)
				{
					num6 = Unsafe.Add(ref reference, 2);
					if (value0 != num6 && value1 != num6 && value2 != num6)
					{
						num6 = Unsafe.Add(ref reference, 3);
						if (value0 != num6 && value1 != num6 && value2 != num6)
						{
							num += 4;
							num2 -= 4;
							continue;
						}
						return (int)(num + 3);
					}
					return (int)(num + 2);
				}
				return (int)(num + 1);
			}
			goto IL_00fb;
		}
		while (num2 != 0)
		{
			int num6 = Add(ref searchStart, num);
			if (value0 != num6 && value1 != num6 && value2 != num6)
			{
				num++;
				num2--;
				continue;
			}
			goto IL_00fb;
		}
		goto IL_00e7;
		IL_00e7:
		return -1;
		IL_00fb:
		return (int)num;
		IL_0355:
		Vector<ushort> vector;
		num += (uint)LocateFirstFoundChar(vector);
		goto IL_00fb;
		IL_0291:
		if (!Sse2.IsSupported && Vector.IsHardwareAccelerated)
		{
			Vector<ushort> right3 = new Vector<ushort>(value0);
			Vector<ushort> right4 = new Vector<ushort>(value1);
			Vector<ushort> right5 = new Vector<ushort>(value2);
			while (num2 > num)
			{
				vector = LoadVector(ref searchStart, num);
				vector = Vector.BitwiseOr(Vector.BitwiseOr(Vector.Equals(vector, right3), Vector.Equals(vector, right4)), Vector.Equals(vector, right5));
				if (Vector<ushort>.Zero.Equals(vector))
				{
					num += (nuint)Vector<ushort>.Count;
					continue;
				}
				goto IL_0355;
			}
			vector = LoadVector(ref searchStart, num2);
			num = num2;
			vector = Vector.BitwiseOr(Vector.BitwiseOr(Vector.Equals(vector, right3), Vector.Equals(vector, right4)), Vector.Equals(vector, right5));
			if (!Vector<ushort>.Zero.Equals(vector))
			{
				goto IL_0355;
			}
		}
		goto IL_00e7;
		IL_027f:
		num += (nuint)(uint)BitOperations.TrailingZeroCount(num4) >> 1;
		goto IL_00fb;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static int IndexOfAny(ref char searchStart, char value0, char value1, char value2, char value3, int length)
	{
		nuint num = 0u;
		nuint num2 = (uint)length;
		int num4;
		if (Sse2.IsSupported)
		{
			nint num3 = (nint)length - (nint)Vector128<ushort>.Count;
			if (num3 >= 0)
			{
				num2 = (nuint)num3;
				if (Sse2.IsSupported)
				{
					if (Avx2.IsSupported && num2 >= (nuint)Vector128<ushort>.Count)
					{
						Vector256<ushort> left = Vector256.Create(value0);
						Vector256<ushort> left2 = Vector256.Create(value1);
						Vector256<ushort> left3 = Vector256.Create(value2);
						Vector256<ushort> left4 = Vector256.Create(value3);
						num2 -= (nuint)Vector128<ushort>.Count;
						Vector256<ushort> right;
						while (num2 > num)
						{
							right = LoadVector256(ref searchStart, num);
							num4 = Avx2.MoveMask(Avx2.CompareEqual(left, right).AsByte());
							num4 |= Avx2.MoveMask(Avx2.CompareEqual(left2, right).AsByte());
							num4 |= Avx2.MoveMask(Avx2.CompareEqual(left3, right).AsByte());
							num4 |= Avx2.MoveMask(Avx2.CompareEqual(left4, right).AsByte());
							if (num4 == 0)
							{
								num += (nuint)Vector256<ushort>.Count;
								continue;
							}
							goto IL_036f;
						}
						right = LoadVector256(ref searchStart, num2);
						num = num2;
						num4 = Avx2.MoveMask(Avx2.CompareEqual(left, right).AsByte());
						num4 |= Avx2.MoveMask(Avx2.CompareEqual(left2, right).AsByte());
						num4 |= Avx2.MoveMask(Avx2.CompareEqual(left3, right).AsByte());
						num4 |= Avx2.MoveMask(Avx2.CompareEqual(left4, right).AsByte());
						if (num4 == 0)
						{
							goto IL_0115;
						}
					}
					else
					{
						Vector128<ushort> left5 = Vector128.Create(value0);
						Vector128<ushort> left6 = Vector128.Create(value1);
						Vector128<ushort> left7 = Vector128.Create(value2);
						Vector128<ushort> left8 = Vector128.Create(value3);
						Vector128<ushort> right2;
						while (num2 > num)
						{
							right2 = LoadVector128(ref searchStart, num);
							num4 = Sse2.MoveMask(Sse2.CompareEqual(left5, right2).AsByte());
							num4 |= Sse2.MoveMask(Sse2.CompareEqual(left6, right2).AsByte());
							num4 |= Sse2.MoveMask(Sse2.CompareEqual(left7, right2).AsByte());
							num4 |= Sse2.MoveMask(Sse2.CompareEqual(left8, right2).AsByte());
							if (num4 == 0)
							{
								num += (nuint)Vector128<ushort>.Count;
								continue;
							}
							goto IL_036f;
						}
						right2 = LoadVector128(ref searchStart, num2);
						num = num2;
						num4 = Sse2.MoveMask(Sse2.CompareEqual(left5, right2).AsByte());
						num4 |= Sse2.MoveMask(Sse2.CompareEqual(left6, right2).AsByte());
						num4 |= Sse2.MoveMask(Sse2.CompareEqual(left7, right2).AsByte());
						num4 |= Sse2.MoveMask(Sse2.CompareEqual(left8, right2).AsByte());
						if (num4 == 0)
						{
							goto IL_0115;
						}
					}
					goto IL_036f;
				}
				goto IL_0381;
			}
		}
		else if (Vector.IsHardwareAccelerated)
		{
			nint num5 = (nint)length - (nint)Vector<ushort>.Count;
			if (num5 >= 0)
			{
				num2 = (nuint)num5;
				goto IL_0381;
			}
		}
		while (num2 >= 4)
		{
			ref char reference = ref Add(ref searchStart, num);
			int num6 = reference;
			if (value0 != num6 && value1 != num6 && value2 != num6 && value3 != num6)
			{
				num6 = Unsafe.Add(ref reference, 1);
				if (value0 != num6 && value1 != num6 && value2 != num6 && value3 != num6)
				{
					num6 = Unsafe.Add(ref reference, 2);
					if (value0 != num6 && value1 != num6 && value2 != num6 && value3 != num6)
					{
						num6 = Unsafe.Add(ref reference, 3);
						if (value0 != num6 && value1 != num6 && value2 != num6 && value3 != num6)
						{
							num += 4;
							num2 -= 4;
							continue;
						}
						return (int)(num + 3);
					}
					return (int)(num + 2);
				}
				return (int)(num + 1);
			}
			goto IL_0129;
		}
		while (num2 != 0)
		{
			int num6 = Add(ref searchStart, num);
			if (value0 != num6 && value1 != num6 && value2 != num6 && value3 != num6)
			{
				num++;
				num2--;
				continue;
			}
			goto IL_0129;
		}
		goto IL_0115;
		IL_0129:
		return (int)num;
		IL_0115:
		return -1;
		IL_046a:
		Vector<ushort> vector;
		num += (uint)LocateFirstFoundChar(vector);
		goto IL_0129;
		IL_036f:
		num += (nuint)(uint)BitOperations.TrailingZeroCount(num4) >> 1;
		goto IL_0129;
		IL_0381:
		if (!Sse2.IsSupported && Vector.IsHardwareAccelerated)
		{
			Vector<ushort> right3 = new Vector<ushort>(value0);
			Vector<ushort> right4 = new Vector<ushort>(value1);
			Vector<ushort> right5 = new Vector<ushort>(value2);
			Vector<ushort> right6 = new Vector<ushort>(value3);
			while (num2 > num)
			{
				vector = LoadVector(ref searchStart, num);
				vector = Vector.BitwiseOr(Vector.BitwiseOr(Vector.BitwiseOr(Vector.Equals(vector, right3), Vector.Equals(vector, right4)), Vector.Equals(vector, right5)), Vector.Equals(vector, right6));
				if (Vector<ushort>.Zero.Equals(vector))
				{
					num += (nuint)Vector<ushort>.Count;
					continue;
				}
				goto IL_046a;
			}
			vector = LoadVector(ref searchStart, num2);
			num = num2;
			vector = Vector.BitwiseOr(Vector.BitwiseOr(Vector.BitwiseOr(Vector.Equals(vector, right3), Vector.Equals(vector, right4)), Vector.Equals(vector, right5)), Vector.Equals(vector, right6));
			if (!Vector<ushort>.Zero.Equals(vector))
			{
				goto IL_046a;
			}
		}
		goto IL_0115;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static int IndexOfAny(ref char searchStart, char value0, char value1, char value2, char value3, char value4, int length)
	{
		nuint num = 0u;
		nuint num2 = (uint)length;
		int num4;
		if (Sse2.IsSupported)
		{
			nint num3 = (nint)length - (nint)Vector128<ushort>.Count;
			if (num3 >= 0)
			{
				num2 = (nuint)num3;
				if (Sse2.IsSupported)
				{
					if (Avx2.IsSupported && num2 >= (nuint)Vector128<ushort>.Count)
					{
						Vector256<ushort> left = Vector256.Create(value0);
						Vector256<ushort> left2 = Vector256.Create(value1);
						Vector256<ushort> left3 = Vector256.Create(value2);
						Vector256<ushort> left4 = Vector256.Create(value3);
						Vector256<ushort> left5 = Vector256.Create(value4);
						num2 -= (nuint)Vector128<ushort>.Count;
						Vector256<ushort> right;
						while (num2 > num)
						{
							right = LoadVector256(ref searchStart, num);
							num4 = Avx2.MoveMask(Avx2.CompareEqual(left, right).AsByte());
							num4 |= Avx2.MoveMask(Avx2.CompareEqual(left2, right).AsByte());
							num4 |= Avx2.MoveMask(Avx2.CompareEqual(left3, right).AsByte());
							num4 |= Avx2.MoveMask(Avx2.CompareEqual(left4, right).AsByte());
							num4 |= Avx2.MoveMask(Avx2.CompareEqual(left5, right).AsByte());
							if (num4 == 0)
							{
								num += (nuint)Vector256<ushort>.Count;
								continue;
							}
							goto IL_040f;
						}
						right = LoadVector256(ref searchStart, num2);
						num = num2;
						num4 = Avx2.MoveMask(Avx2.CompareEqual(left, right).AsByte());
						num4 |= Avx2.MoveMask(Avx2.CompareEqual(left2, right).AsByte());
						num4 |= Avx2.MoveMask(Avx2.CompareEqual(left3, right).AsByte());
						num4 |= Avx2.MoveMask(Avx2.CompareEqual(left4, right).AsByte());
						num4 |= Avx2.MoveMask(Avx2.CompareEqual(left5, right).AsByte());
						if (num4 == 0)
						{
							goto IL_0134;
						}
					}
					else
					{
						Vector128<ushort> left6 = Vector128.Create(value0);
						Vector128<ushort> left7 = Vector128.Create(value1);
						Vector128<ushort> left8 = Vector128.Create(value2);
						Vector128<ushort> left9 = Vector128.Create(value3);
						Vector128<ushort> left10 = Vector128.Create(value4);
						Vector128<ushort> right2;
						while (num2 > num)
						{
							right2 = LoadVector128(ref searchStart, num);
							num4 = Sse2.MoveMask(Sse2.CompareEqual(left6, right2).AsByte());
							num4 |= Sse2.MoveMask(Sse2.CompareEqual(left7, right2).AsByte());
							num4 |= Sse2.MoveMask(Sse2.CompareEqual(left8, right2).AsByte());
							num4 |= Sse2.MoveMask(Sse2.CompareEqual(left9, right2).AsByte());
							num4 |= Sse2.MoveMask(Sse2.CompareEqual(left10, right2).AsByte());
							if (num4 == 0)
							{
								num += (nuint)Vector128<ushort>.Count;
								continue;
							}
							goto IL_040f;
						}
						right2 = LoadVector128(ref searchStart, num2);
						num = num2;
						num4 = Sse2.MoveMask(Sse2.CompareEqual(left6, right2).AsByte());
						num4 |= Sse2.MoveMask(Sse2.CompareEqual(left7, right2).AsByte());
						num4 |= Sse2.MoveMask(Sse2.CompareEqual(left8, right2).AsByte());
						num4 |= Sse2.MoveMask(Sse2.CompareEqual(left9, right2).AsByte());
						num4 |= Sse2.MoveMask(Sse2.CompareEqual(left10, right2).AsByte());
						if (num4 == 0)
						{
							goto IL_0134;
						}
					}
					goto IL_040f;
				}
				goto IL_0421;
			}
		}
		else if (Vector.IsHardwareAccelerated)
		{
			nint num5 = (nint)length - (nint)Vector<ushort>.Count;
			if (num5 >= 0)
			{
				num2 = (nuint)num5;
				goto IL_0421;
			}
		}
		while (num2 >= 4)
		{
			ref char reference = ref Add(ref searchStart, num);
			int num6 = reference;
			if (value0 != num6 && value1 != num6 && value2 != num6 && value3 != num6 && value4 != num6)
			{
				num6 = Unsafe.Add(ref reference, 1);
				if (value0 != num6 && value1 != num6 && value2 != num6 && value3 != num6 && value4 != num6)
				{
					num6 = Unsafe.Add(ref reference, 2);
					if (value0 != num6 && value1 != num6 && value2 != num6 && value3 != num6 && value4 != num6)
					{
						num6 = Unsafe.Add(ref reference, 3);
						if (value0 != num6 && value1 != num6 && value2 != num6 && value3 != num6 && value4 != num6)
						{
							num += 4;
							num2 -= 4;
							continue;
						}
						return (int)(num + 3);
					}
					return (int)(num + 2);
				}
				return (int)(num + 1);
			}
			goto IL_0148;
		}
		while (num2 != 0)
		{
			int num6 = Add(ref searchStart, num);
			if (value0 != num6 && value1 != num6 && value2 != num6 && value3 != num6 && value4 != num6)
			{
				num++;
				num2--;
				continue;
			}
			goto IL_0148;
		}
		goto IL_0134;
		IL_0148:
		return (int)num;
		IL_052f:
		Vector<ushort> vector;
		num += (uint)LocateFirstFoundChar(vector);
		goto IL_0148;
		IL_0421:
		if (!Sse2.IsSupported && Vector.IsHardwareAccelerated)
		{
			Vector<ushort> right3 = new Vector<ushort>(value0);
			Vector<ushort> right4 = new Vector<ushort>(value1);
			Vector<ushort> right5 = new Vector<ushort>(value2);
			Vector<ushort> right6 = new Vector<ushort>(value3);
			Vector<ushort> right7 = new Vector<ushort>(value4);
			while (num2 > num)
			{
				vector = LoadVector(ref searchStart, num);
				vector = Vector.BitwiseOr(Vector.BitwiseOr(Vector.BitwiseOr(Vector.BitwiseOr(Vector.Equals(vector, right3), Vector.Equals(vector, right4)), Vector.Equals(vector, right5)), Vector.Equals(vector, right6)), Vector.Equals(vector, right7));
				if (Vector<ushort>.Zero.Equals(vector))
				{
					num += (nuint)Vector<ushort>.Count;
					continue;
				}
				goto IL_052f;
			}
			vector = LoadVector(ref searchStart, num2);
			num = num2;
			vector = Vector.BitwiseOr(Vector.BitwiseOr(Vector.BitwiseOr(Vector.BitwiseOr(Vector.Equals(vector, right3), Vector.Equals(vector, right4)), Vector.Equals(vector, right5)), Vector.Equals(vector, right6)), Vector.Equals(vector, right7));
			if (!Vector<ushort>.Zero.Equals(vector))
			{
				goto IL_052f;
			}
		}
		goto IL_0134;
		IL_0134:
		return -1;
		IL_040f:
		num += (nuint)(uint)BitOperations.TrailingZeroCount(num4) >> 1;
		goto IL_0148;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public unsafe static int LastIndexOf(ref char searchSpace, char value, int length)
	{
		fixed (char* ptr = &searchSpace)
		{
			char* ptr2 = ptr + length;
			char* ptr3 = ptr;
			if (Vector.IsHardwareAccelerated && length >= Vector<ushort>.Count * 2)
			{
				length = ((int)ptr2 & (Unsafe.SizeOf<Vector<ushort>>() - 1)) / 2;
			}
			while (true)
			{
				if (length >= 4)
				{
					length -= 4;
					ptr2 -= 4;
					if (ptr2[3] == value)
					{
						break;
					}
					if (ptr2[2] != value)
					{
						if (ptr2[1] != value)
						{
							if (*ptr2 != value)
							{
								continue;
							}
							goto IL_011d;
						}
						return (int)(ptr2 - ptr3) + 1;
					}
					return (int)(ptr2 - ptr3) + 2;
				}
				while (length > 0)
				{
					length--;
					ptr2--;
					if (*ptr2 != value)
					{
						continue;
					}
					goto IL_011d;
				}
				if (Vector.IsHardwareAccelerated && ptr2 > ptr3)
				{
					length = (int)((ptr2 - ptr3) & ~(Vector<ushort>.Count - 1));
					Vector<ushort> left = new Vector<ushort>(value);
					while (length > 0)
					{
						char* ptr4 = ptr2 - Vector<ushort>.Count;
						Vector<ushort> vector = Vector.Equals(left, Unsafe.Read<Vector<ushort>>(ptr4));
						if (Vector<ushort>.Zero.Equals(vector))
						{
							ptr2 -= Vector<ushort>.Count;
							length -= Vector<ushort>.Count;
							continue;
						}
						return (int)(ptr4 - ptr3) + LocateLastFoundChar(vector);
					}
					if (ptr2 > ptr3)
					{
						length = (int)(ptr2 - ptr3);
						continue;
					}
				}
				return -1;
				IL_011d:
				return (int)(ptr2 - ptr3);
			}
			return (int)(ptr2 - ptr3) + 3;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int LocateFirstFoundChar(Vector<ushort> match)
	{
		Vector<ulong> vector = Vector.AsVectorUInt64(match);
		ulong num = 0uL;
		int i;
		for (i = 0; i < Vector<ulong>.Count; i++)
		{
			num = vector[i];
			if (num != 0L)
			{
				break;
			}
		}
		return i * 4 + LocateFirstFoundChar(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int LocateFirstFoundChar(ulong match)
	{
		return BitOperations.TrailingZeroCount(match) >> 4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int LocateLastFoundChar(Vector<ushort> match)
	{
		Vector<ulong> vector = Vector.AsVectorUInt64(match);
		ulong num = 0uL;
		int num2 = Vector<ulong>.Count - 1;
		for (int i = 0; i < Vector<ulong>.Count; i++)
		{
			num = vector[num2];
			if (num != 0L)
			{
				break;
			}
			num2--;
		}
		return num2 * 4 + LocateLastFoundChar(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int LocateLastFoundChar(ulong match)
	{
		return BitOperations.Log2(match) >> 4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector<ushort> LoadVector(ref char start, nint offset)
	{
		return Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, offset)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector<ushort> LoadVector(ref char start, nuint offset)
	{
		return Unsafe.ReadUnaligned<Vector<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, (nint)offset)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<ushort> LoadVector128(ref char start, nint offset)
	{
		return Unsafe.ReadUnaligned<Vector128<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, offset)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<ushort> LoadVector128(ref char start, nuint offset)
	{
		return Unsafe.ReadUnaligned<Vector128<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, (nint)offset)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<ushort> LoadVector256(ref char start, nint offset)
	{
		return Unsafe.ReadUnaligned<Vector256<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, offset)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<ushort> LoadVector256(ref char start, nuint offset)
	{
		return Unsafe.ReadUnaligned<Vector256<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, (nint)offset)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ref char Add(ref char start, nuint offset)
	{
		return ref Unsafe.Add(ref start, (nint)offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nint GetCharVectorSpanLength(nint offset, nint length)
	{
		return (length - offset) & ~(Vector<ushort>.Count - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nint GetCharVector128SpanLength(nint offset, nint length)
	{
		return (length - offset) & ~(Vector128<ushort>.Count - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static nint GetCharVector256SpanLength(nint offset, nint length)
	{
		return (length - offset) & ~(Vector256<ushort>.Count - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static nint UnalignedCountVector(ref char searchSpace)
	{
		return (nint)(uint)(-(int)Unsafe.AsPointer(ref searchSpace) / 2) & (nint)(Vector<ushort>.Count - 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static nint UnalignedCountVector128(ref char searchSpace)
	{
		return (nint)(uint)(-(int)Unsafe.AsPointer(ref searchSpace) / 2) & (nint)(Vector128<ushort>.Count - 1);
	}

	public static void ClearWithoutReferences(ref byte b, nuint byteLength)
	{
		if (byteLength != 0)
		{
			if (byteLength <= 768)
			{
				Unsafe.InitBlockUnaligned(ref b, 0, (uint)byteLength);
			}
			else
			{
				Buffer._ZeroMemory(ref b, byteLength);
			}
		}
	}

	public static void ClearWithReferences(ref IntPtr ip, nuint pointerSizeLength)
	{
		while (pointerSizeLength >= 8)
		{
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -1) = default(IntPtr);
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -2) = default(IntPtr);
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -3) = default(IntPtr);
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -4) = default(IntPtr);
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -5) = default(IntPtr);
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -6) = default(IntPtr);
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -7) = default(IntPtr);
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -8) = default(IntPtr);
			pointerSizeLength -= 8;
		}
		if (pointerSizeLength < 4)
		{
			if (pointerSizeLength < 2)
			{
				if (pointerSizeLength == 0)
				{
					return;
				}
				goto IL_012f;
			}
		}
		else
		{
			Unsafe.Add(ref ip, 2) = default(IntPtr);
			Unsafe.Add(ref ip, 3) = default(IntPtr);
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -3) = default(IntPtr);
			Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -2) = default(IntPtr);
		}
		Unsafe.Add(ref ip, 1) = default(IntPtr);
		Unsafe.Add(ref Unsafe.Add(ref ip, (nint)pointerSizeLength), -1) = default(IntPtr);
		goto IL_012f;
		IL_012f:
		ip = default(IntPtr);
	}

	public static void Fill<T>(ref T refData, nuint numElements, T value)
	{
		if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>() && Vector.IsHardwareAccelerated && Unsafe.SizeOf<T>() <= Vector<byte>.Count && BitOperations.IsPow2(Unsafe.SizeOf<T>()) && numElements >= (uint)(Vector<byte>.Count / Unsafe.SizeOf<T>()))
		{
			T source = value;
			Vector<byte> value2;
			if (Unsafe.SizeOf<T>() == 1)
			{
				value2 = new Vector<byte>(Unsafe.As<T, byte>(ref source));
			}
			else if (Unsafe.SizeOf<T>() == 2)
			{
				value2 = (Vector<byte>)new Vector<ushort>(Unsafe.As<T, ushort>(ref source));
			}
			else if (Unsafe.SizeOf<T>() == 4)
			{
				value2 = ((typeof(T) == typeof(float)) ? ((Vector<byte>)new Vector<float>((float)(object)source)) : ((Vector<byte>)new Vector<uint>(Unsafe.As<T, uint>(ref source))));
			}
			else if (Unsafe.SizeOf<T>() == 8)
			{
				value2 = ((typeof(T) == typeof(double)) ? ((Vector<byte>)new Vector<double>((double)(object)source)) : ((Vector<byte>)new Vector<ulong>(Unsafe.As<T, ulong>(ref source))));
			}
			else if (Unsafe.SizeOf<T>() == 16)
			{
				Vector128<byte> vector = Unsafe.As<T, Vector128<byte>>(ref source);
				if (Vector<byte>.Count == 16)
				{
					value2 = vector.AsVector();
				}
				else
				{
					if (Vector<byte>.Count != 32)
					{
						goto IL_022e;
					}
					value2 = Vector256.Create(vector, vector).AsVector();
				}
			}
			else
			{
				if (Unsafe.SizeOf<T>() != 32 || Vector<byte>.Count != 32)
				{
					goto IL_022e;
				}
				value2 = Unsafe.As<T, Vector256<byte>>(ref source).AsVector();
			}
			ref byte source2 = ref Unsafe.As<T, byte>(ref refData);
			nuint num = numElements * (nuint)Unsafe.SizeOf<T>();
			nuint num2 = num & (nuint)(2 * -Vector<byte>.Count);
			nuint num3 = 0u;
			if (numElements >= (uint)(2 * Vector<byte>.Count / Unsafe.SizeOf<T>()))
			{
				do
				{
					Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref source2, num3), value2);
					Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref source2, num3 + (nuint)Vector<byte>.Count), value2);
					num3 += (uint)(2 * Vector<byte>.Count);
				}
				while (num3 < num2);
			}
			if ((num & (nuint)Vector<byte>.Count) != 0)
			{
				Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref source2, num3), value2);
			}
			Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref source2, num - (nuint)Vector<byte>.Count), value2);
			return;
		}
		goto IL_022e;
		IL_022e:
		nuint num4 = 0u;
		if (numElements >= 8)
		{
			nuint num5 = numElements & ~(nuint)7u;
			do
			{
				Unsafe.Add(ref refData, (nint)(num4 + 0)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 1)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 2)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 3)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 4)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 5)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 6)) = value;
				Unsafe.Add(ref refData, (nint)(num4 + 7)) = value;
			}
			while ((num4 += 8) < num5);
		}
		if ((numElements & 4) != 0)
		{
			Unsafe.Add(ref refData, (nint)(num4 + 0)) = value;
			Unsafe.Add(ref refData, (nint)(num4 + 1)) = value;
			Unsafe.Add(ref refData, (nint)(num4 + 2)) = value;
			Unsafe.Add(ref refData, (nint)(num4 + 3)) = value;
			num4 += 4;
		}
		if ((numElements & 2) != 0)
		{
			Unsafe.Add(ref refData, (nint)(num4 + 0)) = value;
			Unsafe.Add(ref refData, (nint)(num4 + 1)) = value;
			num4 += 2;
		}
		if ((numElements & 1) != 0)
		{
			Unsafe.Add(ref refData, (nint)num4) = value;
		}
	}

	public static int IndexOf<T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
	{
		if (valueLength == 0)
		{
			return 0;
		}
		T value2 = value;
		ref T second = ref Unsafe.Add(ref value, 1);
		int num = valueLength - 1;
		int num2 = 0;
		while (true)
		{
			int num3 = searchSpaceLength - num2 - num;
			if (num3 <= 0)
			{
				break;
			}
			int num4 = IndexOf(ref Unsafe.Add(ref searchSpace, num2), value2, num3);
			if (num4 == -1)
			{
				break;
			}
			num2 += num4;
			if (SequenceEqual(ref Unsafe.Add(ref searchSpace, num2 + 1), ref second, num))
			{
				return num2;
			}
			num2++;
		}
		return -1;
	}

	public static bool Contains<T>(ref T searchSpace, T value, int length) where T : IEquatable<T>
	{
		nint num = 0;
		if (default(T) != null || value != null)
		{
			while (length >= 8)
			{
				length -= 8;
				if (!value.Equals(Unsafe.Add(ref searchSpace, num + 0)) && !value.Equals(Unsafe.Add(ref searchSpace, num + 1)) && !value.Equals(Unsafe.Add(ref searchSpace, num + 2)) && !value.Equals(Unsafe.Add(ref searchSpace, num + 3)) && !value.Equals(Unsafe.Add(ref searchSpace, num + 4)) && !value.Equals(Unsafe.Add(ref searchSpace, num + 5)) && !value.Equals(Unsafe.Add(ref searchSpace, num + 6)) && !value.Equals(Unsafe.Add(ref searchSpace, num + 7)))
				{
					num += 8;
					continue;
				}
				goto IL_0220;
			}
			if (length >= 4)
			{
				length -= 4;
				if (value.Equals(Unsafe.Add(ref searchSpace, num + 0)) || value.Equals(Unsafe.Add(ref searchSpace, num + 1)) || value.Equals(Unsafe.Add(ref searchSpace, num + 2)) || value.Equals(Unsafe.Add(ref searchSpace, num + 3)))
				{
					goto IL_0220;
				}
				num += 4;
			}
			while (length > 0)
			{
				length--;
				if (!value.Equals(Unsafe.Add(ref searchSpace, num)))
				{
					num++;
					continue;
				}
				goto IL_0220;
			}
		}
		else
		{
			nint num2 = length;
			num = 0;
			while (num < num2)
			{
				if (Unsafe.Add(ref searchSpace, num) != null)
				{
					num++;
					continue;
				}
				goto IL_0220;
			}
		}
		return false;
		IL_0220:
		return true;
	}

	public static int IndexOf<T>(ref T searchSpace, T value, int length) where T : IEquatable<T>
	{
		nint num = 0;
		if (default(T) != null || value != null)
		{
			while (length >= 8)
			{
				length -= 8;
				if (value.Equals(Unsafe.Add(ref searchSpace, num)))
				{
					goto IL_021a;
				}
				if (value.Equals(Unsafe.Add(ref searchSpace, num + 1)))
				{
					goto IL_021d;
				}
				if (value.Equals(Unsafe.Add(ref searchSpace, num + 2)))
				{
					goto IL_0223;
				}
				if (!value.Equals(Unsafe.Add(ref searchSpace, num + 3)))
				{
					if (!value.Equals(Unsafe.Add(ref searchSpace, num + 4)))
					{
						if (!value.Equals(Unsafe.Add(ref searchSpace, num + 5)))
						{
							if (!value.Equals(Unsafe.Add(ref searchSpace, num + 6)))
							{
								if (!value.Equals(Unsafe.Add(ref searchSpace, num + 7)))
								{
									num += 8;
									continue;
								}
								return (int)(num + 7);
							}
							return (int)(num + 6);
						}
						return (int)(num + 5);
					}
					return (int)(num + 4);
				}
				goto IL_0229;
			}
			if (length >= 4)
			{
				length -= 4;
				if (value.Equals(Unsafe.Add(ref searchSpace, num)))
				{
					goto IL_021a;
				}
				if (value.Equals(Unsafe.Add(ref searchSpace, num + 1)))
				{
					goto IL_021d;
				}
				if (value.Equals(Unsafe.Add(ref searchSpace, num + 2)))
				{
					goto IL_0223;
				}
				if (value.Equals(Unsafe.Add(ref searchSpace, num + 3)))
				{
					goto IL_0229;
				}
				num += 4;
			}
			while (length > 0)
			{
				if (!value.Equals(Unsafe.Add(ref searchSpace, num)))
				{
					num++;
					length--;
					continue;
				}
				goto IL_021a;
			}
		}
		else
		{
			nint num2 = length;
			num = 0;
			while (num < num2)
			{
				if (Unsafe.Add(ref searchSpace, num) != null)
				{
					num++;
					continue;
				}
				goto IL_021a;
			}
		}
		return -1;
		IL_021a:
		return (int)num;
		IL_021d:
		return (int)(num + 1);
		IL_0229:
		return (int)(num + 3);
		IL_0223:
		return (int)(num + 2);
	}

	public static int IndexOfAny<T>(ref T searchSpace, T value0, T value1, int length) where T : IEquatable<T>
	{
		int i = 0;
		if (default(T) != null || (value0 != null && value1 != null))
		{
			while (length - i >= 8)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0350;
				}
				other = Unsafe.Add(ref searchSpace, i + 1);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0352;
				}
				other = Unsafe.Add(ref searchSpace, i + 2);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0356;
				}
				other = Unsafe.Add(ref searchSpace, i + 3);
				if (!value0.Equals(other) && !value1.Equals(other))
				{
					other = Unsafe.Add(ref searchSpace, i + 4);
					if (!value0.Equals(other) && !value1.Equals(other))
					{
						other = Unsafe.Add(ref searchSpace, i + 5);
						if (!value0.Equals(other) && !value1.Equals(other))
						{
							other = Unsafe.Add(ref searchSpace, i + 6);
							if (!value0.Equals(other) && !value1.Equals(other))
							{
								other = Unsafe.Add(ref searchSpace, i + 7);
								if (!value0.Equals(other) && !value1.Equals(other))
								{
									i += 8;
									continue;
								}
								return i + 7;
							}
							return i + 6;
						}
						return i + 5;
					}
					return i + 4;
				}
				goto IL_035a;
			}
			if (length - i >= 4)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0350;
				}
				other = Unsafe.Add(ref searchSpace, i + 1);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0352;
				}
				other = Unsafe.Add(ref searchSpace, i + 2);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0356;
				}
				other = Unsafe.Add(ref searchSpace, i + 3);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_035a;
				}
				i += 4;
			}
			while (i < length)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (!value0.Equals(other) && !value1.Equals(other))
				{
					i++;
					continue;
				}
				goto IL_0350;
			}
		}
		else
		{
			for (i = 0; i < length; i++)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (other == null)
				{
					if (value0 != null && value1 != null)
					{
						continue;
					}
				}
				else if (!other.Equals(value0) && !other.Equals(value1))
				{
					continue;
				}
				goto IL_0350;
			}
		}
		return -1;
		IL_0352:
		return i + 1;
		IL_0350:
		return i;
		IL_0356:
		return i + 2;
		IL_035a:
		return i + 3;
	}

	public static int IndexOfAny<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : IEquatable<T>
	{
		int i = 0;
		if (default(T) != null || (value0 != null && value1 != null && value2 != null))
		{
			while (length - i >= 8)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0471;
				}
				other = Unsafe.Add(ref searchSpace, i + 1);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0473;
				}
				other = Unsafe.Add(ref searchSpace, i + 2);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0477;
				}
				other = Unsafe.Add(ref searchSpace, i + 3);
				if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
				{
					other = Unsafe.Add(ref searchSpace, i + 4);
					if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
					{
						other = Unsafe.Add(ref searchSpace, i + 5);
						if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
						{
							other = Unsafe.Add(ref searchSpace, i + 6);
							if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
							{
								other = Unsafe.Add(ref searchSpace, i + 7);
								if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
								{
									i += 8;
									continue;
								}
								return i + 7;
							}
							return i + 6;
						}
						return i + 5;
					}
					return i + 4;
				}
				goto IL_047b;
			}
			if (length - i >= 4)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0471;
				}
				other = Unsafe.Add(ref searchSpace, i + 1);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0473;
				}
				other = Unsafe.Add(ref searchSpace, i + 2);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0477;
				}
				other = Unsafe.Add(ref searchSpace, i + 3);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_047b;
				}
				i += 4;
			}
			while (i < length)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
				{
					i++;
					continue;
				}
				goto IL_0471;
			}
		}
		else
		{
			for (i = 0; i < length; i++)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				if (other == null)
				{
					if (value0 != null && value1 != null && value2 != null)
					{
						continue;
					}
				}
				else if (!other.Equals(value0) && !other.Equals(value1) && !other.Equals(value2))
				{
					continue;
				}
				goto IL_0471;
			}
		}
		return -1;
		IL_0473:
		return i + 1;
		IL_047b:
		return i + 3;
		IL_0471:
		return i;
		IL_0477:
		return i + 2;
	}

	public static int IndexOfAny<T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
	{
		if (valueLength == 0)
		{
			return -1;
		}
		if (typeof(T).IsValueType)
		{
			for (int i = 0; i < searchSpaceLength; i++)
			{
				T other = Unsafe.Add(ref searchSpace, i);
				for (int j = 0; j < valueLength; j++)
				{
					if (Unsafe.Add(ref value, j).Equals(other))
					{
						return i;
					}
				}
			}
		}
		else
		{
			for (int k = 0; k < searchSpaceLength; k++)
			{
				T val = Unsafe.Add(ref searchSpace, k);
				if (val != null)
				{
					for (int l = 0; l < valueLength; l++)
					{
						if (val.Equals(Unsafe.Add(ref value, l)))
						{
							return k;
						}
					}
					continue;
				}
				for (int m = 0; m < valueLength; m++)
				{
					if (Unsafe.Add(ref value, m) == null)
					{
						return k;
					}
				}
			}
		}
		return -1;
	}

	public static int LastIndexOf<T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
	{
		if (valueLength == 0)
		{
			return searchSpaceLength;
		}
		T value2 = value;
		ref T second = ref Unsafe.Add(ref value, 1);
		int num = valueLength - 1;
		int num2 = 0;
		while (true)
		{
			int num3 = searchSpaceLength - num2 - num;
			if (num3 <= 0)
			{
				break;
			}
			int num4 = LastIndexOf(ref searchSpace, value2, num3);
			if (num4 == -1)
			{
				break;
			}
			if (SequenceEqual(ref Unsafe.Add(ref searchSpace, num4 + 1), ref second, num))
			{
				return num4;
			}
			num2 += num3 - num4;
		}
		return -1;
	}

	public static int LastIndexOf<T>(ref T searchSpace, T value, int length) where T : IEquatable<T>
	{
		if (default(T) != null || value != null)
		{
			while (length >= 8)
			{
				length -= 8;
				if (!value.Equals(Unsafe.Add(ref searchSpace, length + 7)))
				{
					if (!value.Equals(Unsafe.Add(ref searchSpace, length + 6)))
					{
						if (!value.Equals(Unsafe.Add(ref searchSpace, length + 5)))
						{
							if (!value.Equals(Unsafe.Add(ref searchSpace, length + 4)))
							{
								if (!value.Equals(Unsafe.Add(ref searchSpace, length + 3)))
								{
									if (!value.Equals(Unsafe.Add(ref searchSpace, length + 2)))
									{
										if (!value.Equals(Unsafe.Add(ref searchSpace, length + 1)))
										{
											if (!value.Equals(Unsafe.Add(ref searchSpace, length)))
											{
												continue;
											}
											goto IL_01fe;
										}
										goto IL_0200;
									}
									goto IL_0204;
								}
								goto IL_0208;
							}
							return length + 4;
						}
						return length + 5;
					}
					return length + 6;
				}
				return length + 7;
			}
			if (length >= 4)
			{
				length -= 4;
				if (value.Equals(Unsafe.Add(ref searchSpace, length + 3)))
				{
					goto IL_0208;
				}
				if (value.Equals(Unsafe.Add(ref searchSpace, length + 2)))
				{
					goto IL_0204;
				}
				if (value.Equals(Unsafe.Add(ref searchSpace, length + 1)))
				{
					goto IL_0200;
				}
				if (value.Equals(Unsafe.Add(ref searchSpace, length)))
				{
					goto IL_01fe;
				}
			}
			while (length > 0)
			{
				length--;
				if (!value.Equals(Unsafe.Add(ref searchSpace, length)))
				{
					continue;
				}
				goto IL_01fe;
			}
		}
		else
		{
			length--;
			while (length >= 0)
			{
				if (Unsafe.Add(ref searchSpace, length) != null)
				{
					length--;
					continue;
				}
				goto IL_01fe;
			}
		}
		return -1;
		IL_0208:
		return length + 3;
		IL_0200:
		return length + 1;
		IL_01fe:
		return length;
		IL_0204:
		return length + 2;
	}

	public static int LastIndexOfAny<T>(ref T searchSpace, T value0, T value1, int length) where T : IEquatable<T>
	{
		if (default(T) != null || (value0 != null && value1 != null))
		{
			while (length >= 8)
			{
				length -= 8;
				T other = Unsafe.Add(ref searchSpace, length + 7);
				if (!value0.Equals(other) && !value1.Equals(other))
				{
					other = Unsafe.Add(ref searchSpace, length + 6);
					if (!value0.Equals(other) && !value1.Equals(other))
					{
						other = Unsafe.Add(ref searchSpace, length + 5);
						if (!value0.Equals(other) && !value1.Equals(other))
						{
							other = Unsafe.Add(ref searchSpace, length + 4);
							if (!value0.Equals(other) && !value1.Equals(other))
							{
								other = Unsafe.Add(ref searchSpace, length + 3);
								if (!value0.Equals(other) && !value1.Equals(other))
								{
									other = Unsafe.Add(ref searchSpace, length + 2);
									if (!value0.Equals(other) && !value1.Equals(other))
									{
										other = Unsafe.Add(ref searchSpace, length + 1);
										if (!value0.Equals(other) && !value1.Equals(other))
										{
											other = Unsafe.Add(ref searchSpace, length);
											if (!value0.Equals(other) && !value1.Equals(other))
											{
												continue;
											}
											goto IL_0351;
										}
										goto IL_0353;
									}
									goto IL_0357;
								}
								goto IL_035b;
							}
							return length + 4;
						}
						return length + 5;
					}
					return length + 6;
				}
				return length + 7;
			}
			if (length >= 4)
			{
				length -= 4;
				T other = Unsafe.Add(ref searchSpace, length + 3);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_035b;
				}
				other = Unsafe.Add(ref searchSpace, length + 2);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0357;
				}
				other = Unsafe.Add(ref searchSpace, length + 1);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0353;
				}
				other = Unsafe.Add(ref searchSpace, length);
				if (value0.Equals(other) || value1.Equals(other))
				{
					goto IL_0351;
				}
			}
			while (length > 0)
			{
				length--;
				T other = Unsafe.Add(ref searchSpace, length);
				if (!value0.Equals(other) && !value1.Equals(other))
				{
					continue;
				}
				goto IL_0351;
			}
		}
		else
		{
			for (length--; length >= 0; length--)
			{
				T other = Unsafe.Add(ref searchSpace, length);
				if (other == null)
				{
					if (value0 != null && value1 != null)
					{
						continue;
					}
				}
				else if (!other.Equals(value0) && !other.Equals(value1))
				{
					continue;
				}
				goto IL_0351;
			}
		}
		return -1;
		IL_035b:
		return length + 3;
		IL_0357:
		return length + 2;
		IL_0351:
		return length;
		IL_0353:
		return length + 1;
	}

	public static int LastIndexOfAny<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : IEquatable<T>
	{
		if (default(T) != null || (value0 != null && value1 != null))
		{
			while (length >= 8)
			{
				length -= 8;
				T other = Unsafe.Add(ref searchSpace, length + 7);
				if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
				{
					other = Unsafe.Add(ref searchSpace, length + 6);
					if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
					{
						other = Unsafe.Add(ref searchSpace, length + 5);
						if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
						{
							other = Unsafe.Add(ref searchSpace, length + 4);
							if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
							{
								other = Unsafe.Add(ref searchSpace, length + 3);
								if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
								{
									other = Unsafe.Add(ref searchSpace, length + 2);
									if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
									{
										other = Unsafe.Add(ref searchSpace, length + 1);
										if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
										{
											other = Unsafe.Add(ref searchSpace, length);
											if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
											{
												continue;
											}
											goto IL_047a;
										}
										goto IL_047d;
									}
									goto IL_0482;
								}
								goto IL_0487;
							}
							return length + 4;
						}
						return length + 5;
					}
					return length + 6;
				}
				return length + 7;
			}
			if (length >= 4)
			{
				length -= 4;
				T other = Unsafe.Add(ref searchSpace, length + 3);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0487;
				}
				other = Unsafe.Add(ref searchSpace, length + 2);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_0482;
				}
				other = Unsafe.Add(ref searchSpace, length + 1);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_047d;
				}
				other = Unsafe.Add(ref searchSpace, length);
				if (value0.Equals(other) || value1.Equals(other) || value2.Equals(other))
				{
					goto IL_047a;
				}
			}
			while (length > 0)
			{
				length--;
				T other = Unsafe.Add(ref searchSpace, length);
				if (!value0.Equals(other) && !value1.Equals(other) && !value2.Equals(other))
				{
					continue;
				}
				goto IL_047a;
			}
		}
		else
		{
			for (length--; length >= 0; length--)
			{
				T other = Unsafe.Add(ref searchSpace, length);
				if (other == null)
				{
					if (value0 != null && value1 != null && value2 != null)
					{
						continue;
					}
				}
				else if (!other.Equals(value0) && !other.Equals(value1) && !other.Equals(value2))
				{
					continue;
				}
				goto IL_047a;
			}
		}
		return -1;
		IL_0482:
		return length + 2;
		IL_047d:
		return length + 1;
		IL_0487:
		return length + 3;
		IL_047a:
		return length;
	}

	public static int LastIndexOfAny<T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>
	{
		if (valueLength == 0)
		{
			return -1;
		}
		if (typeof(T).IsValueType)
		{
			for (int num = searchSpaceLength - 1; num >= 0; num--)
			{
				T other = Unsafe.Add(ref searchSpace, num);
				for (int i = 0; i < valueLength; i++)
				{
					if (Unsafe.Add(ref value, i).Equals(other))
					{
						return num;
					}
				}
			}
		}
		else
		{
			for (int num2 = searchSpaceLength - 1; num2 >= 0; num2--)
			{
				T val = Unsafe.Add(ref searchSpace, num2);
				if (val != null)
				{
					for (int j = 0; j < valueLength; j++)
					{
						if (val.Equals(Unsafe.Add(ref value, j)))
						{
							return num2;
						}
					}
				}
				else
				{
					for (int k = 0; k < valueLength; k++)
					{
						if (Unsafe.Add(ref value, k) == null)
						{
							return num2;
						}
					}
				}
			}
		}
		return -1;
	}

	public static bool SequenceEqual<T>(ref T first, ref T second, int length) where T : IEquatable<T>
	{
		if (!Unsafe.AreSame(ref first, ref second))
		{
			nint num = 0;
			while (true)
			{
				if (length >= 8)
				{
					length -= 8;
					T val = Unsafe.Add(ref first, num);
					T val2 = Unsafe.Add(ref second, num);
					if (val?.Equals(val2) ?? (val2 == null))
					{
						val = Unsafe.Add(ref first, num + 1);
						val2 = Unsafe.Add(ref second, num + 1);
						if (val?.Equals(val2) ?? (val2 == null))
						{
							val = Unsafe.Add(ref first, num + 2);
							val2 = Unsafe.Add(ref second, num + 2);
							if (val?.Equals(val2) ?? (val2 == null))
							{
								val = Unsafe.Add(ref first, num + 3);
								val2 = Unsafe.Add(ref second, num + 3);
								if (val?.Equals(val2) ?? (val2 == null))
								{
									val = Unsafe.Add(ref first, num + 4);
									val2 = Unsafe.Add(ref second, num + 4);
									if (val?.Equals(val2) ?? (val2 == null))
									{
										val = Unsafe.Add(ref first, num + 5);
										val2 = Unsafe.Add(ref second, num + 5);
										if (val?.Equals(val2) ?? (val2 == null))
										{
											val = Unsafe.Add(ref first, num + 6);
											val2 = Unsafe.Add(ref second, num + 6);
											if (val?.Equals(val2) ?? (val2 == null))
											{
												val = Unsafe.Add(ref first, num + 7);
												val2 = Unsafe.Add(ref second, num + 7);
												if (val?.Equals(val2) ?? (val2 == null))
												{
													num += 8;
													continue;
												}
											}
										}
									}
								}
							}
						}
					}
				}
				else
				{
					if (length < 4)
					{
						goto IL_03b8;
					}
					length -= 4;
					T val = Unsafe.Add(ref first, num);
					T val2 = Unsafe.Add(ref second, num);
					if (val?.Equals(val2) ?? (val2 == null))
					{
						val = Unsafe.Add(ref first, num + 1);
						val2 = Unsafe.Add(ref second, num + 1);
						if (val?.Equals(val2) ?? (val2 == null))
						{
							val = Unsafe.Add(ref first, num + 2);
							val2 = Unsafe.Add(ref second, num + 2);
							if (val?.Equals(val2) ?? (val2 == null))
							{
								val = Unsafe.Add(ref first, num + 3);
								val2 = Unsafe.Add(ref second, num + 3);
								if (val?.Equals(val2) ?? (val2 == null))
								{
									num += 4;
									goto IL_03b8;
								}
							}
						}
					}
				}
				goto IL_03be;
				IL_03b8:
				while (length > 0)
				{
					T val = Unsafe.Add(ref first, num);
					T val2 = Unsafe.Add(ref second, num);
					if (val?.Equals(val2) ?? (val2 == null))
					{
						num++;
						length--;
						continue;
					}
					goto IL_03be;
				}
				break;
				IL_03be:
				return false;
			}
		}
		return true;
	}

	public static int SequenceCompareTo<T>(ref T first, int firstLength, ref T second, int secondLength) where T : IComparable<T>
	{
		int num = firstLength;
		if (num > secondLength)
		{
			num = secondLength;
		}
		for (int i = 0; i < num; i++)
		{
			T val = Unsafe.Add(ref second, i);
			int num2 = Unsafe.Add(ref first, i)?.CompareTo(val) ?? ((val != null) ? (-1) : 0);
			if (num2 != 0)
			{
				return num2;
			}
		}
		return firstLength.CompareTo(secondLength);
	}
}
