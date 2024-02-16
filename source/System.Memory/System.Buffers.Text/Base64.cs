using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Internal.Runtime.CompilerServices;

namespace System.Buffers.Text;

public static class Base64
{
	private static ReadOnlySpan<sbyte> DecodingMap => new sbyte[256]
	{
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, 62, -1, -1, -1, 63, 52, 53,
		54, 55, 56, 57, 58, 59, 60, 61, -1, -1,
		-1, -1, -1, -1, -1, 0, 1, 2, 3, 4,
		5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
		15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
		25, -1, -1, -1, -1, -1, -1, 26, 27, 28,
		29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
		39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
		49, 50, 51, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
		-1, -1, -1, -1, -1, -1
	};

	private static ReadOnlySpan<byte> EncodingMap => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"u8;

	public unsafe static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> utf8, Span<byte> bytes, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
	{
		if (utf8.IsEmpty)
		{
			bytesConsumed = 0;
			bytesWritten = 0;
			return OperationStatus.Done;
		}
		fixed (byte* ptr = &MemoryMarshal.GetReference(utf8))
		{
			fixed (byte* ptr2 = &MemoryMarshal.GetReference(bytes))
			{
				int num = utf8.Length & -4;
				int length = bytes.Length;
				int num2 = num;
				int maxDecodedFromUtf8Length = GetMaxDecodedFromUtf8Length(num);
				if (length < maxDecodedFromUtf8Length - 2)
				{
					num2 = length / 3 * 4;
				}
				byte* srcBytes = ptr;
				byte* destBytes = ptr2;
				byte* ptr3 = ptr + (uint)num;
				byte* ptr4 = ptr + (uint)num2;
				if (num2 >= 24)
				{
					byte* ptr5 = ptr4 - 45;
					if (Avx2.IsSupported && ptr5 >= srcBytes)
					{
						Avx2Decode(ref srcBytes, ref destBytes, ptr5, num2, length, ptr, ptr2);
						if (srcBytes == ptr3)
						{
							goto IL_029d;
						}
					}
					ptr5 = ptr4 - 24;
					if (Ssse3.IsSupported && ptr5 >= srcBytes)
					{
						Ssse3Decode(ref srcBytes, ref destBytes, ptr5, num2, length, ptr, ptr2);
						if (srcBytes == ptr3)
						{
							goto IL_029d;
						}
					}
				}
				int num3 = (isFinalBlock ? 4 : 0);
				num2 = ((length < maxDecodedFromUtf8Length) ? (length / 3 * 4) : (num - num3));
				ref sbyte reference = ref MemoryMarshal.GetReference(DecodingMap);
				ptr4 = ptr + (uint)num2;
				while (true)
				{
					if (srcBytes < ptr4)
					{
						int num4 = Decode(srcBytes, ref reference);
						if (num4 >= 0)
						{
							WriteThreeLowOrderBytes(destBytes, num4);
							srcBytes += 4;
							destBytes += 3;
							continue;
						}
					}
					else
					{
						if (num2 != num - num3)
						{
							goto IL_02b3;
						}
						if (srcBytes == ptr3)
						{
							if (!isFinalBlock)
							{
								if (srcBytes == ptr + utf8.Length)
								{
									break;
								}
								bytesConsumed = (int)(srcBytes - ptr);
								bytesWritten = (int)(destBytes - ptr2);
								return OperationStatus.NeedMoreData;
							}
						}
						else
						{
							uint num5 = ptr3[-4];
							uint num6 = ptr3[-3];
							uint num7 = ptr3[-2];
							uint num8 = ptr3[-1];
							int num9 = Internal.Runtime.CompilerServices.Unsafe.Add(ref reference, (IntPtr)num5);
							int num10 = Internal.Runtime.CompilerServices.Unsafe.Add(ref reference, (IntPtr)num6);
							num9 <<= 18;
							num10 <<= 12;
							num9 |= num10;
							byte* ptr6 = ptr2 + (uint)length;
							if (num8 != 61)
							{
								int num11 = Internal.Runtime.CompilerServices.Unsafe.Add(ref reference, (IntPtr)num7);
								int num12 = Internal.Runtime.CompilerServices.Unsafe.Add(ref reference, (IntPtr)num8);
								num11 <<= 6;
								num9 |= num12;
								num9 |= num11;
								if (num9 >= 0)
								{
									if (destBytes + 3 <= ptr6)
									{
										WriteThreeLowOrderBytes(destBytes, num9);
										destBytes += 3;
										goto IL_028c;
									}
									goto IL_02b3;
								}
							}
							else if (num7 != 61)
							{
								int num13 = Internal.Runtime.CompilerServices.Unsafe.Add(ref reference, (IntPtr)num7);
								num13 <<= 6;
								num9 |= num13;
								if (num9 >= 0)
								{
									if (destBytes + 2 <= ptr6)
									{
										*destBytes = (byte)(num9 >> 16);
										destBytes[1] = (byte)(num9 >> 8);
										destBytes += 2;
										goto IL_028c;
									}
									goto IL_02b3;
								}
							}
							else if (num9 >= 0)
							{
								if (destBytes + 1 <= ptr6)
								{
									*destBytes = (byte)(num9 >> 16);
									destBytes++;
									goto IL_028c;
								}
								goto IL_02b3;
							}
						}
					}
					goto IL_02f2;
					IL_028c:
					srcBytes += 4;
					if (num == utf8.Length)
					{
						break;
					}
					goto IL_02f2;
					IL_02b3:
					if (!(num != utf8.Length && isFinalBlock))
					{
						bytesConsumed = (int)(srcBytes - ptr);
						bytesWritten = (int)(destBytes - ptr2);
						return OperationStatus.DestinationTooSmall;
					}
					goto IL_02f2;
					IL_02f2:
					bytesConsumed = (int)(srcBytes - ptr);
					bytesWritten = (int)(destBytes - ptr2);
					return OperationStatus.InvalidData;
				}
				goto IL_029d;
				IL_029d:
				bytesConsumed = (int)(srcBytes - ptr);
				bytesWritten = (int)(destBytes - ptr2);
				return OperationStatus.Done;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetMaxDecodedFromUtf8Length(int length)
	{
		if (length < 0)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException(System.ExceptionArgument.length);
		}
		return (length >> 2) * 3;
	}

	public unsafe static OperationStatus DecodeFromUtf8InPlace(Span<byte> buffer, out int bytesWritten)
	{
		if (buffer.IsEmpty)
		{
			bytesWritten = 0;
			return OperationStatus.Done;
		}
		fixed (byte* ptr = &MemoryMarshal.GetReference(buffer))
		{
			int length = buffer.Length;
			uint num = 0u;
			uint num2 = 0u;
			if (length == (length >> 2) * 4)
			{
				if (length == 0)
				{
					goto IL_0189;
				}
				ref sbyte reference = ref MemoryMarshal.GetReference(DecodingMap);
				while (num < length - 4)
				{
					int num3 = Decode(ptr + num, ref reference);
					if (num3 >= 0)
					{
						WriteThreeLowOrderBytes(ptr + num2, num3);
						num2 += 3;
						num += 4;
						continue;
					}
					goto IL_018f;
				}
				uint num4 = ptr[length - 4];
				uint num5 = ptr[length - 3];
				uint num6 = ptr[length - 2];
				uint num7 = ptr[length - 1];
				int num8 = Internal.Runtime.CompilerServices.Unsafe.Add(ref reference, (IntPtr)num4);
				int num9 = Internal.Runtime.CompilerServices.Unsafe.Add(ref reference, (IntPtr)num5);
				num8 <<= 18;
				num9 <<= 12;
				num8 |= num9;
				if (num7 != 61)
				{
					int num10 = Internal.Runtime.CompilerServices.Unsafe.Add(ref reference, (IntPtr)num6);
					int num11 = Internal.Runtime.CompilerServices.Unsafe.Add(ref reference, (IntPtr)num7);
					num10 <<= 6;
					num8 |= num11;
					num8 |= num10;
					if (num8 >= 0)
					{
						WriteThreeLowOrderBytes(ptr + num2, num8);
						num2 += 3;
						goto IL_0189;
					}
				}
				else if (num6 != 61)
				{
					int num12 = Internal.Runtime.CompilerServices.Unsafe.Add(ref reference, (IntPtr)num6);
					num12 <<= 6;
					num8 |= num12;
					if (num8 >= 0)
					{
						ptr[num2] = (byte)(num8 >> 16);
						ptr[num2 + 1] = (byte)(num8 >> 8);
						num2 += 2;
						goto IL_0189;
					}
				}
				else if (num8 >= 0)
				{
					ptr[num2] = (byte)(num8 >> 16);
					num2++;
					goto IL_0189;
				}
			}
			goto IL_018f;
			IL_018f:
			bytesWritten = (int)num2;
			return OperationStatus.InvalidData;
			IL_0189:
			bytesWritten = (int)num2;
			return OperationStatus.Done;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void Avx2Decode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
	{
		Vector256<sbyte> value = Vector256.Create(16, 16, 1, 2, 4, 8, 4, 8, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 1, 2, 4, 8, 4, 8, 16, 16, 16, 16, 16, 16, 16, 16);
		Vector256<sbyte> value2 = Vector256.Create(21, 17, 17, 17, 17, 17, 17, 17, 17, 17, 19, 26, 27, 27, 27, 26, 21, 17, 17, 17, 17, 17, 17, 17, 17, 17, 19, 26, 27, 27, 27, 26);
		Vector256<sbyte> value3 = Vector256.Create(0, 16, 19, 4, -65, -65, -71, -71, 0, 0, 0, 0, 0, 0, 0, 0, 0, 16, 19, 4, -65, -65, -71, -71, 0, 0, 0, 0, 0, 0, 0, 0);
		Vector256<sbyte> mask = Vector256.Create(2, 1, 0, 6, 5, 4, 10, 9, 8, 14, 13, 12, -1, -1, -1, -1, 2, 1, 0, 6, 5, 4, 10, 9, 8, 14, 13, 12, -1, -1, -1, -1);
		Vector256<int> control = Vector256.Create(0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 4, 0, 0, 0, 5, 0, 0, 0, 6, 0, 0, 0, -1, -1, -1, -1, -1, -1, -1, -1).AsInt32();
		Vector256<sbyte> right = Vector256.Create((sbyte)47);
		Vector256<sbyte> right2 = Vector256.Create(20971840).AsSByte();
		Vector256<short> right3 = Vector256.Create(69632).AsInt16();
		byte* ptr = srcBytes;
		byte* ptr2 = destBytes;
		do
		{
			Vector256<sbyte> vector = Avx.LoadVector256(ptr).AsSByte();
			Vector256<sbyte> vector2 = Avx2.And(Avx2.ShiftRightLogical(vector.AsInt32(), 4).AsSByte(), right);
			Vector256<sbyte> mask2 = Avx2.And(vector, right);
			Vector256<sbyte> right4 = Avx2.Shuffle(value, vector2);
			Vector256<sbyte> left = Avx2.Shuffle(value2, mask2);
			if (!Avx.TestZ(left, right4))
			{
				break;
			}
			Vector256<sbyte> left2 = Avx2.CompareEqual(vector, right);
			Vector256<sbyte> right5 = Avx2.Shuffle(value3, Avx2.Add(left2, vector2));
			vector = Avx2.Add(vector, right5);
			Vector256<short> left3 = Avx2.MultiplyAddAdjacent(vector.AsByte(), right2);
			Vector256<int> vector3 = Avx2.MultiplyAddAdjacent(left3, right3);
			vector3 = Avx2.Shuffle(vector3.AsSByte(), mask).AsInt32();
			vector = Avx2.PermuteVar8x32(vector3, control).AsSByte();
			Avx.Store(ptr2, vector.AsByte());
			ptr += 32;
			ptr2 += 24;
		}
		while (ptr <= srcEnd);
		srcBytes = ptr;
		destBytes = ptr2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void Ssse3Decode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
	{
		Vector128<sbyte> value = Vector128.Create(16, 16, 1, 2, 4, 8, 4, 8, 16, 16, 16, 16, 16, 16, 16, 16);
		Vector128<sbyte> value2 = Vector128.Create(21, 17, 17, 17, 17, 17, 17, 17, 17, 17, 19, 26, 27, 27, 27, 26);
		Vector128<sbyte> value3 = Vector128.Create(0, 16, 19, 4, -65, -65, -71, -71, 0, 0, 0, 0, 0, 0, 0, 0);
		Vector128<sbyte> mask = Vector128.Create(2, 1, 0, 6, 5, 4, 10, 9, 8, 14, 13, 12, -1, -1, -1, -1);
		Vector128<sbyte> right = Vector128.Create((sbyte)47);
		Vector128<sbyte> right2 = Vector128.Create(20971840).AsSByte();
		Vector128<short> right3 = Vector128.Create(69632).AsInt16();
		Vector128<sbyte> zero = Vector128<sbyte>.Zero;
		byte* ptr = srcBytes;
		byte* ptr2 = destBytes;
		do
		{
			Vector128<sbyte> vector = Sse2.LoadVector128(ptr).AsSByte();
			Vector128<sbyte> vector2 = Sse2.And(Sse2.ShiftRightLogical(vector.AsInt32(), 4).AsSByte(), right);
			Vector128<sbyte> mask2 = Sse2.And(vector, right);
			Vector128<sbyte> right4 = Ssse3.Shuffle(value, vector2);
			Vector128<sbyte> left = Ssse3.Shuffle(value2, mask2);
			if (Sse2.MoveMask(Sse2.CompareGreaterThan(Sse2.And(left, right4), zero)) != 0)
			{
				break;
			}
			Vector128<sbyte> left2 = Sse2.CompareEqual(vector, right);
			Vector128<sbyte> right5 = Ssse3.Shuffle(value3, Sse2.Add(left2, vector2));
			vector = Sse2.Add(vector, right5);
			Vector128<short> left3 = Ssse3.MultiplyAddAdjacent(vector.AsByte(), right2);
			Vector128<int> vector3 = Sse2.MultiplyAddAdjacent(left3, right3);
			vector = Ssse3.Shuffle(vector3.AsSByte(), mask);
			Sse2.Store(ptr2, vector.AsByte());
			ptr += 16;
			ptr2 += 12;
		}
		while (ptr <= srcEnd);
		srcBytes = ptr;
		destBytes = ptr2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static int Decode(byte* encodedBytes, ref sbyte decodingMap)
	{
		uint num = *encodedBytes;
		uint num2 = encodedBytes[1];
		uint num3 = encodedBytes[2];
		uint num4 = encodedBytes[3];
		int num5 = Internal.Runtime.CompilerServices.Unsafe.Add(ref decodingMap, (IntPtr)num);
		int num6 = Internal.Runtime.CompilerServices.Unsafe.Add(ref decodingMap, (IntPtr)num2);
		int num7 = Internal.Runtime.CompilerServices.Unsafe.Add(ref decodingMap, (IntPtr)num3);
		int num8 = Internal.Runtime.CompilerServices.Unsafe.Add(ref decodingMap, (IntPtr)num4);
		num5 <<= 18;
		num6 <<= 12;
		num7 <<= 6;
		num5 |= num8;
		num6 |= num7;
		return num5 | num6;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void WriteThreeLowOrderBytes(byte* destination, int value)
	{
		*destination = (byte)(value >> 16);
		destination[1] = (byte)(value >> 8);
		destination[2] = (byte)value;
	}

	public unsafe static OperationStatus EncodeToUtf8(ReadOnlySpan<byte> bytes, Span<byte> utf8, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
	{
		if (bytes.IsEmpty)
		{
			bytesConsumed = 0;
			bytesWritten = 0;
			return OperationStatus.Done;
		}
		fixed (byte* ptr = &MemoryMarshal.GetReference(bytes))
		{
			fixed (byte* ptr2 = &MemoryMarshal.GetReference(utf8))
			{
				int length = bytes.Length;
				int length2 = utf8.Length;
				int num = ((length > 1610612733 || length2 < GetMaxEncodedToUtf8Length(length)) ? ((length2 >> 2) * 3) : length);
				byte* srcBytes = ptr;
				byte* destBytes = ptr2;
				byte* ptr3 = ptr + (uint)length;
				byte* ptr4 = ptr + (uint)num;
				if (num >= 16)
				{
					byte* ptr5 = ptr4 - 32;
					if (Avx2.IsSupported && ptr5 >= srcBytes)
					{
						Avx2Encode(ref srcBytes, ref destBytes, ptr5, num, length2, ptr, ptr2);
						if (srcBytes == ptr3)
						{
							goto IL_0175;
						}
					}
					ptr5 = ptr4 - 16;
					if (Ssse3.IsSupported && ptr5 >= srcBytes)
					{
						Ssse3Encode(ref srcBytes, ref destBytes, ptr5, num, length2, ptr, ptr2);
						if (srcBytes == ptr3)
						{
							goto IL_0175;
						}
					}
				}
				ref byte reference = ref MemoryMarshal.GetReference(EncodingMap);
				uint num2 = 0u;
				ptr4 -= 2;
				while (srcBytes < ptr4)
				{
					num2 = Encode(srcBytes, ref reference);
					Internal.Runtime.CompilerServices.Unsafe.WriteUnaligned(destBytes, num2);
					srcBytes += 3;
					destBytes += 4;
				}
				if (ptr4 + 2 == ptr3)
				{
					if (!isFinalBlock)
					{
						if (srcBytes != ptr3)
						{
							bytesConsumed = (int)(srcBytes - ptr);
							bytesWritten = (int)(destBytes - ptr2);
							return OperationStatus.NeedMoreData;
						}
					}
					else if (srcBytes + 1 == ptr3)
					{
						num2 = EncodeAndPadTwo(srcBytes, ref reference);
						Internal.Runtime.CompilerServices.Unsafe.WriteUnaligned(destBytes, num2);
						srcBytes++;
						destBytes += 4;
					}
					else if (srcBytes + 2 == ptr3)
					{
						num2 = EncodeAndPadOne(srcBytes, ref reference);
						Internal.Runtime.CompilerServices.Unsafe.WriteUnaligned(destBytes, num2);
						srcBytes += 2;
						destBytes += 4;
					}
					goto IL_0175;
				}
				bytesConsumed = (int)(srcBytes - ptr);
				bytesWritten = (int)(destBytes - ptr2);
				return OperationStatus.DestinationTooSmall;
				IL_0175:
				bytesConsumed = (int)(srcBytes - ptr);
				bytesWritten = (int)(destBytes - ptr2);
				return OperationStatus.Done;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetMaxEncodedToUtf8Length(int length)
	{
		if ((uint)length > 1610612733u)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException(System.ExceptionArgument.length);
		}
		return (length + 2) / 3 * 4;
	}

	public unsafe static OperationStatus EncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten)
	{
		if (buffer.IsEmpty)
		{
			bytesWritten = 0;
			return OperationStatus.Done;
		}
		fixed (byte* ptr = &MemoryMarshal.GetReference(buffer))
		{
			int maxEncodedToUtf8Length = GetMaxEncodedToUtf8Length(dataLength);
			if (buffer.Length >= maxEncodedToUtf8Length)
			{
				int num = dataLength - dataLength / 3 * 3;
				uint num2 = (uint)(maxEncodedToUtf8Length - 4);
				uint num3 = (uint)(dataLength - num);
				uint num4 = 0u;
				ref byte reference = ref MemoryMarshal.GetReference(EncodingMap);
				if (num != 0)
				{
					num4 = ((num != 1) ? EncodeAndPadOne(ptr + num3, ref reference) : EncodeAndPadTwo(ptr + num3, ref reference));
					Internal.Runtime.CompilerServices.Unsafe.WriteUnaligned(ptr + num2, num4);
					num2 -= 4;
				}
				num3 -= 3;
				while ((int)num3 >= 0)
				{
					num4 = Encode(ptr + num3, ref reference);
					Internal.Runtime.CompilerServices.Unsafe.WriteUnaligned(ptr + num2, num4);
					num2 -= 4;
					num3 -= 3;
				}
				bytesWritten = maxEncodedToUtf8Length;
				return OperationStatus.Done;
			}
			bytesWritten = 0;
			return OperationStatus.DestinationTooSmall;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void Avx2Encode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
	{
		Vector256<sbyte> mask = Vector256.Create(5, 4, 6, 5, 8, 7, 9, 8, 11, 10, 12, 11, 14, 13, 15, 14, 1, 0, 2, 1, 4, 3, 5, 4, 7, 6, 8, 7, 10, 9, 11, 10);
		Vector256<sbyte> value = Vector256.Create(65, 71, -4, -4, -4, -4, -4, -4, -4, -4, -4, -4, -19, -16, 0, 0, 65, 71, -4, -4, -4, -4, -4, -4, -4, -4, -4, -4, -19, -16, 0, 0);
		Vector256<sbyte> right = Vector256.Create(264305664).AsSByte();
		Vector256<sbyte> right2 = Vector256.Create(4129776).AsSByte();
		Vector256<ushort> right3 = Vector256.Create(67108928).AsUInt16();
		Vector256<short> right4 = Vector256.Create(16777232).AsInt16();
		Vector256<byte> right5 = Vector256.Create((byte)51);
		Vector256<sbyte> right6 = Vector256.Create((sbyte)25);
		byte* ptr = srcBytes;
		byte* ptr2 = destBytes;
		Vector256<sbyte> vector = Avx.LoadVector256(ptr).AsSByte();
		vector = Avx2.PermuteVar8x32(vector.AsInt32(), Vector256.Create(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 4, 0, 0, 0, 5, 0, 0, 0, 6, 0, 0, 0).AsInt32()).AsSByte();
		ptr -= 4;
		while (true)
		{
			vector = Avx2.Shuffle(vector, mask);
			Vector256<sbyte> vector2 = Avx2.And(vector, right);
			Vector256<sbyte> vector3 = Avx2.And(vector, right2);
			Vector256<ushort> vector4 = Avx2.MultiplyHigh(vector2.AsUInt16(), right3);
			Vector256<short> vector5 = Avx2.MultiplyLow(vector3.AsInt16(), right4);
			vector = Avx2.Or(vector4.AsSByte(), vector5.AsSByte());
			Vector256<byte> vector6 = Avx2.SubtractSaturate(vector.AsByte(), right5);
			Vector256<sbyte> right7 = Avx2.CompareGreaterThan(vector, right6);
			Vector256<sbyte> mask2 = Avx2.Subtract(vector6.AsSByte(), right7);
			vector = Avx2.Add(vector, Avx2.Shuffle(value, mask2));
			Avx.Store(ptr2, vector.AsByte());
			ptr += 24;
			ptr2 += 32;
			if (ptr > srcEnd)
			{
				break;
			}
			vector = Avx.LoadVector256(ptr).AsSByte();
		}
		srcBytes = ptr + 4;
		destBytes = ptr2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static void Ssse3Encode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
	{
		Vector128<sbyte> mask = Vector128.Create(1, 0, 2, 1, 4, 3, 5, 4, 7, 6, 8, 7, 10, 9, 11, 10);
		Vector128<sbyte> value = Vector128.Create(65, 71, -4, -4, -4, -4, -4, -4, -4, -4, -4, -4, -19, -16, 0, 0);
		Vector128<sbyte> right = Vector128.Create(264305664).AsSByte();
		Vector128<sbyte> right2 = Vector128.Create(4129776).AsSByte();
		Vector128<ushort> right3 = Vector128.Create(67108928).AsUInt16();
		Vector128<short> right4 = Vector128.Create(16777232).AsInt16();
		Vector128<byte> right5 = Vector128.Create((byte)51);
		Vector128<sbyte> right6 = Vector128.Create((sbyte)25);
		byte* ptr = srcBytes;
		byte* ptr2 = destBytes;
		do
		{
			Vector128<sbyte> value2 = Sse2.LoadVector128(ptr).AsSByte();
			value2 = Ssse3.Shuffle(value2, mask);
			Vector128<sbyte> vector = Sse2.And(value2, right);
			Vector128<sbyte> vector2 = Sse2.And(value2, right2);
			Vector128<ushort> vector3 = Sse2.MultiplyHigh(vector.AsUInt16(), right3);
			Vector128<short> vector4 = Sse2.MultiplyLow(vector2.AsInt16(), right4);
			value2 = Sse2.Or(vector3.AsSByte(), vector4.AsSByte());
			Vector128<byte> vector5 = Sse2.SubtractSaturate(value2.AsByte(), right5);
			Vector128<sbyte> right7 = Sse2.CompareGreaterThan(value2, right6);
			Vector128<sbyte> mask2 = Sse2.Subtract(vector5.AsSByte(), right7);
			value2 = Sse2.Add(value2, Ssse3.Shuffle(value, mask2));
			Sse2.Store(ptr2, value2.AsByte());
			ptr += 12;
			ptr2 += 16;
		}
		while (ptr <= srcEnd);
		srcBytes = ptr;
		destBytes = ptr2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static uint Encode(byte* threeBytes, ref byte encodingMap)
	{
		uint num = *threeBytes;
		uint num2 = threeBytes[1];
		uint num3 = threeBytes[2];
		uint num4 = (num << 16) | (num2 << 8) | num3;
		uint num5 = Internal.Runtime.CompilerServices.Unsafe.Add(ref encodingMap, (IntPtr)(num4 >> 18));
		uint num6 = Internal.Runtime.CompilerServices.Unsafe.Add(ref encodingMap, (IntPtr)((num4 >> 12) & 0x3F));
		uint num7 = Internal.Runtime.CompilerServices.Unsafe.Add(ref encodingMap, (IntPtr)((num4 >> 6) & 0x3F));
		uint num8 = Internal.Runtime.CompilerServices.Unsafe.Add(ref encodingMap, (IntPtr)(num4 & 0x3F));
		if (BitConverter.IsLittleEndian)
		{
			return num5 | (num6 << 8) | (num7 << 16) | (num8 << 24);
		}
		return (num5 << 24) | (num6 << 16) | (num7 << 8) | num8;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static uint EncodeAndPadOne(byte* twoBytes, ref byte encodingMap)
	{
		uint num = *twoBytes;
		uint num2 = twoBytes[1];
		uint num3 = (num << 16) | (num2 << 8);
		uint num4 = Internal.Runtime.CompilerServices.Unsafe.Add(ref encodingMap, (IntPtr)(num3 >> 18));
		uint num5 = Internal.Runtime.CompilerServices.Unsafe.Add(ref encodingMap, (IntPtr)((num3 >> 12) & 0x3F));
		uint num6 = Internal.Runtime.CompilerServices.Unsafe.Add(ref encodingMap, (IntPtr)((num3 >> 6) & 0x3F));
		if (BitConverter.IsLittleEndian)
		{
			return num4 | (num5 << 8) | (num6 << 16) | 0x3D000000u;
		}
		return (num4 << 24) | (num5 << 16) | (num6 << 8) | 0x3Du;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe static uint EncodeAndPadTwo(byte* oneByte, ref byte encodingMap)
	{
		uint num = *oneByte;
		uint num2 = num << 8;
		uint num3 = Internal.Runtime.CompilerServices.Unsafe.Add(ref encodingMap, (IntPtr)(num2 >> 10));
		uint num4 = Internal.Runtime.CompilerServices.Unsafe.Add(ref encodingMap, (IntPtr)((num2 >> 4) & 0x3F));
		if (BitConverter.IsLittleEndian)
		{
			return num3 | (num4 << 8) | 0x3D0000u | 0x3D000000u;
		}
		return (num3 << 24) | (num4 << 16) | 0x3D00u | 0x3Du;
	}
}
