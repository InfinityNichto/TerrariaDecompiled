using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Internal.Runtime.CompilerServices;

namespace System.Text;

internal static class Latin1Utility
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static nuint GetIndexOfFirstNonLatin1Char(char* pBuffer, nuint bufferLength)
	{
		if (!Sse2.IsSupported)
		{
			return GetIndexOfFirstNonLatin1Char_Default(pBuffer, bufferLength);
		}
		return GetIndexOfFirstNonLatin1Char_Sse2(pBuffer, bufferLength);
	}

	private unsafe static nuint GetIndexOfFirstNonLatin1Char_Default(char* pBuffer, nuint bufferLength)
	{
		char* ptr = pBuffer;
		if (Vector.IsHardwareAccelerated && bufferLength >= (uint)(2 * Vector<ushort>.Count))
		{
			uint count = (uint)Vector<ushort>.Count;
			uint count2 = (uint)Vector<byte>.Count;
			Vector<ushort> right = new Vector<ushort>(255);
			if (Vector.LessThanOrEqualAll(Unsafe.ReadUnaligned<Vector<ushort>>(pBuffer), right))
			{
				char* ptr2 = pBuffer + bufferLength - count;
				pBuffer = (char*)((nuint)((byte*)pBuffer + count2) & ~(nuint)(count2 - 1));
				while (!Vector.GreaterThanAny(Unsafe.Read<Vector<ushort>>(pBuffer), right))
				{
					pBuffer += count;
					if (pBuffer > ptr2)
					{
						break;
					}
				}
				bufferLength -= (nuint)(nint)(pBuffer - ptr);
			}
		}
		while (true)
		{
			uint num;
			if (bufferLength >= 4)
			{
				num = Unsafe.ReadUnaligned<uint>(pBuffer);
				uint num2 = Unsafe.ReadUnaligned<uint>(pBuffer + 2);
				if (!AllCharsInUInt32AreLatin1(num | num2))
				{
					if (AllCharsInUInt32AreLatin1(num))
					{
						num = num2;
						pBuffer += 2;
					}
					goto IL_010f;
				}
				pBuffer += 4;
				bufferLength -= 4;
				continue;
			}
			if ((bufferLength & 2) != 0)
			{
				num = Unsafe.ReadUnaligned<uint>(pBuffer);
				if (!AllCharsInUInt32AreLatin1(num))
				{
					goto IL_010f;
				}
				pBuffer += 2;
			}
			if ((bufferLength & 1) != 0 && *pBuffer <= 'ÿ')
			{
				pBuffer++;
			}
			break;
			IL_010f:
			if (FirstCharInUInt32IsLatin1(num))
			{
				pBuffer++;
			}
			break;
		}
		nuint num3 = (nuint)((byte*)pBuffer - (nuint)ptr);
		return num3 / 2;
	}

	private unsafe static nuint GetIndexOfFirstNonLatin1Char_Sse2(char* pBuffer, nuint bufferLength)
	{
		if (bufferLength == 0)
		{
			return 0u;
		}
		uint num = (uint)Unsafe.SizeOf<Vector128<byte>>();
		uint num2 = num / 2;
		char* ptr = pBuffer;
		Vector128<ushort> right;
		Vector128<ushort> right2;
		Vector128<ushort> left;
		uint num3;
		if (bufferLength >= num2)
		{
			right = Vector128.Create((ushort)65280);
			right2 = Vector128.Create((ushort)32512);
			left = Sse2.LoadVector128((ushort*)pBuffer);
			num3 = (uint)Sse2.MoveMask(Sse2.AddSaturate(left, right2).AsByte());
			if ((num3 & 0xAAAA) == 0)
			{
				bufferLength <<= 1;
				if (bufferLength < 2 * num)
				{
					goto IL_013e;
				}
				pBuffer = (char*)((nuint)((byte*)pBuffer + num) & ~(nuint)(num - 1));
				bufferLength = (nuint)(bufferLength + (byte*)ptr);
				bufferLength -= (nuint)pBuffer;
				if (bufferLength < 2 * num)
				{
					goto IL_00fa;
				}
				char* ptr2 = (char*)((byte*)pBuffer + bufferLength - 2 * num);
				Vector128<ushort> vector;
				while (true)
				{
					left = Sse2.LoadAlignedVector128((ushort*)pBuffer);
					vector = Sse2.LoadAlignedVector128((ushort*)(pBuffer + num2));
					Vector128<ushort> left2 = Sse2.Or(left, vector);
					if (Sse41.IsSupported)
					{
						if (!Sse41.TestZ(left2, right))
						{
							break;
						}
					}
					else
					{
						num3 = (uint)Sse2.MoveMask(Sse2.AddSaturate(left2, right2).AsByte());
						if ((num3 & 0xAAAAu) != 0)
						{
							break;
						}
					}
					pBuffer += 2 * num2;
					if (pBuffer <= ptr2)
					{
						continue;
					}
					goto IL_00fa;
				}
				if (Sse41.IsSupported)
				{
					if (!Sse41.TestZ(left, right))
					{
						goto IL_01e6;
					}
				}
				else
				{
					num3 = (uint)Sse2.MoveMask(Sse2.AddSaturate(left, right2).AsByte());
					if ((num3 & 0xAAAAu) != 0)
					{
						goto IL_01fa;
					}
				}
				pBuffer += num2;
				left = vector;
				goto IL_01e6;
			}
			goto IL_01fa;
		}
		uint num5;
		if ((bufferLength & 4) != 0)
		{
			if (Bmi1.X64.IsSupported)
			{
				ulong num4 = Unsafe.ReadUnaligned<ulong>(pBuffer);
				if (!AllCharsInUInt64AreLatin1(num4))
				{
					num4 &= 0xFF00FF00FF00FF00uL;
					pBuffer = (char*)((byte*)pBuffer + (nuint)((nint)(Bmi1.X64.TrailingZeroCount(num4) / 8) & ~(nint)1));
					goto IL_01a1;
				}
			}
			else
			{
				num5 = Unsafe.ReadUnaligned<uint>(pBuffer);
				uint num6 = Unsafe.ReadUnaligned<uint>(pBuffer + 2);
				if (!AllCharsInUInt32AreLatin1(num5 | num6))
				{
					if (AllCharsInUInt32AreLatin1(num5))
					{
						num5 = num6;
						pBuffer += 2;
					}
					goto IL_0214;
				}
			}
			pBuffer += 4;
		}
		if ((bufferLength & 2) != 0)
		{
			num5 = Unsafe.ReadUnaligned<uint>(pBuffer);
			if (!AllCharsInUInt32AreLatin1(num5))
			{
				goto IL_0214;
			}
			pBuffer += 2;
		}
		if ((bufferLength & 1) != 0 && *pBuffer <= 'ÿ')
		{
			pBuffer++;
		}
		goto IL_01a1;
		IL_01e6:
		num3 = (uint)Sse2.MoveMask(Sse2.AddSaturate(left, right2).AsByte());
		goto IL_01fa;
		IL_0214:
		if (FirstCharInUInt32IsLatin1(num5))
		{
			pBuffer++;
		}
		goto IL_01a1;
		IL_01a1:
		return (nuint)(pBuffer - ptr);
		IL_0148:
		if (((byte)bufferLength & (num - 1)) != 0)
		{
			pBuffer = (char*)((byte*)pBuffer + (bufferLength & (num - 1)) - num);
			left = Sse2.LoadVector128((ushort*)pBuffer);
			if (Sse41.IsSupported)
			{
				if (!Sse41.TestZ(left, right))
				{
					goto IL_01e6;
				}
			}
			else
			{
				num3 = (uint)Sse2.MoveMask(Sse2.AddSaturate(left, right2).AsByte());
				if ((num3 & 0xAAAAu) != 0)
				{
					goto IL_01fa;
				}
			}
			pBuffer += num2;
		}
		goto IL_01a1;
		IL_00fa:
		if ((bufferLength & num) != 0)
		{
			left = Sse2.LoadAlignedVector128((ushort*)pBuffer);
			if (Sse41.IsSupported)
			{
				if (!Sse41.TestZ(left, right))
				{
					goto IL_01e6;
				}
			}
			else
			{
				num3 = (uint)Sse2.MoveMask(Sse2.AddSaturate(left, right2).AsByte());
				if ((num3 & 0xAAAAu) != 0)
				{
					goto IL_01fa;
				}
			}
			goto IL_013e;
		}
		goto IL_0148;
		IL_01fa:
		num3 &= 0xAAAAu;
		pBuffer = (char*)((byte*)pBuffer + (uint)BitOperations.TrailingZeroCount(num3) - 1);
		goto IL_01a1;
		IL_013e:
		pBuffer += num2;
		goto IL_0148;
	}

	public unsafe static nuint NarrowUtf16ToLatin1(char* pUtf16Buffer, byte* pLatin1Buffer, nuint elementCount)
	{
		nuint num = 0u;
		uint num2 = 0u;
		uint num3 = 0u;
		ulong num4 = 0uL;
		if (Sse2.IsSupported)
		{
			if (elementCount >= (uint)(2 * Unsafe.SizeOf<Vector128<byte>>()))
			{
				_ = IntPtr.Size;
				num4 = Unsafe.ReadUnaligned<ulong>(pUtf16Buffer);
				if (!AllCharsInUInt64AreLatin1(num4))
				{
					goto IL_0191;
				}
				num = NarrowUtf16ToLatin1_Sse2(pUtf16Buffer, pLatin1Buffer, elementCount);
			}
		}
		else if (Vector.IsHardwareAccelerated)
		{
			uint num5 = (uint)Unsafe.SizeOf<Vector<byte>>();
			if (elementCount >= 2 * num5)
			{
				_ = IntPtr.Size;
				num4 = Unsafe.ReadUnaligned<ulong>(pUtf16Buffer);
				if (!AllCharsInUInt64AreLatin1(num4))
				{
					goto IL_0191;
				}
				Vector<ushort> right = new Vector<ushort>(255);
				nuint num6 = elementCount - 2 * num5;
				do
				{
					Vector<ushort> vector = Unsafe.ReadUnaligned<Vector<ushort>>(pUtf16Buffer + num);
					Vector<ushort> vector2 = Unsafe.ReadUnaligned<Vector<ushort>>(pUtf16Buffer + num + Vector<ushort>.Count);
					if (Vector.GreaterThanAny(Vector.BitwiseOr(vector, vector2), right))
					{
						break;
					}
					Vector<byte> value = Vector.Narrow(vector, vector2);
					Unsafe.WriteUnaligned(pLatin1Buffer + num, value);
					num += num5;
				}
				while (num <= num6);
			}
		}
		nuint num7 = elementCount - num;
		if (num7 < 4)
		{
			goto IL_013a;
		}
		nuint num8 = num + num7 - 4;
		while (true)
		{
			_ = IntPtr.Size;
			num4 = Unsafe.ReadUnaligned<ulong>(pUtf16Buffer + num);
			if (!AllCharsInUInt64AreLatin1(num4))
			{
				break;
			}
			NarrowFourUtf16CharsToLatin1AndWriteToBuffer(ref pLatin1Buffer[num], num4);
			num += 4;
			if (num <= num8)
			{
				continue;
			}
			goto IL_013a;
		}
		goto IL_0191;
		IL_01ca:
		if (FirstCharInUInt32IsLatin1(num2))
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			pLatin1Buffer[num] = (byte)num2;
			num++;
		}
		goto IL_018f;
		IL_018f:
		return num;
		IL_013a:
		if (((uint)(int)num7 & 2u) != 0)
		{
			num2 = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + num);
			if (!AllCharsInUInt32AreLatin1(num2))
			{
				goto IL_01ca;
			}
			NarrowTwoUtf16CharsToLatin1AndWriteToBuffer(ref pLatin1Buffer[num], num2);
			num += 2;
		}
		if (((uint)(int)num7 & (true ? 1u : 0u)) != 0)
		{
			num2 = pUtf16Buffer[num];
			if (num2 <= 255)
			{
				pLatin1Buffer[num] = (byte)num2;
				num++;
			}
		}
		goto IL_018f;
		IL_0191:
		_ = IntPtr.Size;
		_ = BitConverter.IsLittleEndian;
		num2 = (uint)num4;
		if (AllCharsInUInt32AreLatin1(num2))
		{
			NarrowTwoUtf16CharsToLatin1AndWriteToBuffer(ref pLatin1Buffer[num], num2);
			_ = BitConverter.IsLittleEndian;
			num2 = (uint)(num4 >> 32);
			num += 2;
		}
		goto IL_01ca;
	}

	private unsafe static nuint NarrowUtf16ToLatin1_Sse2(char* pUtf16Buffer, byte* pLatin1Buffer, nuint elementCount)
	{
		uint num = (uint)Unsafe.SizeOf<Vector128<byte>>();
		nuint num2 = num - 1;
		Vector128<short> right = Vector128.Create((short)(-256));
		Vector128<ushort> right2 = Vector128.Create((ushort)32512);
		Vector128<short> vector = Sse2.LoadVector128((short*)pUtf16Buffer);
		if (Sse41.IsSupported)
		{
			if (!Sse41.TestZ(vector, right))
			{
				return 0u;
			}
		}
		else if (((uint)Sse2.MoveMask(Sse2.AddSaturate(vector.AsUInt16(), right2).AsByte()) & 0xAAAAu) != 0)
		{
			return 0u;
		}
		Vector128<byte> vector2 = Sse2.PackUnsignedSaturate(vector, vector);
		Sse2.StoreScalar((ulong*)pLatin1Buffer, vector2.AsUInt64());
		nuint num3 = num / 2;
		if (((uint)(int)pLatin1Buffer & (num / 2)) != 0)
		{
			goto IL_00ea;
		}
		vector = Sse2.LoadVector128((short*)(pUtf16Buffer + num3));
		if (Sse41.IsSupported)
		{
			if (Sse41.TestZ(vector, right))
			{
				goto IL_00cd;
			}
		}
		else if ((Sse2.MoveMask(Sse2.AddSaturate(vector.AsUInt16(), right2).AsByte()) & 0xAAAA) == 0)
		{
			goto IL_00cd;
		}
		goto IL_0186;
		IL_0186:
		return num3;
		IL_00cd:
		vector2 = Sse2.PackUnsignedSaturate(vector, vector);
		Sse2.StoreScalar((ulong*)(pLatin1Buffer + num3), vector2.AsUInt64());
		goto IL_00ea;
		IL_00ea:
		num3 = num - ((nuint)pLatin1Buffer & num2);
		nuint num4 = elementCount - num;
		do
		{
			vector = Sse2.LoadVector128((short*)(pUtf16Buffer + num3));
			Vector128<short> right3 = Sse2.LoadVector128((short*)(pUtf16Buffer + num3 + num / 2));
			Vector128<short> vector3 = Sse2.Or(vector, right3);
			if (Sse41.IsSupported)
			{
				if (Sse41.TestZ(vector3, right))
				{
					goto IL_015e;
				}
			}
			else if ((Sse2.MoveMask(Sse2.AddSaturate(vector3.AsUInt16(), right2).AsByte()) & 0xAAAA) == 0)
			{
				goto IL_015e;
			}
			if (Sse41.IsSupported)
			{
				if (!Sse41.TestZ(vector, right))
				{
					break;
				}
			}
			else if (((uint)Sse2.MoveMask(Sse2.AddSaturate(vector.AsUInt16(), right2).AsByte()) & 0xAAAAu) != 0)
			{
				break;
			}
			vector2 = Sse2.PackUnsignedSaturate(vector, vector);
			Sse2.StoreScalar((ulong*)(pLatin1Buffer + num3), vector2.AsUInt64());
			num3 += num / 2;
			break;
			IL_015e:
			vector2 = Sse2.PackUnsignedSaturate(vector, right3);
			Sse2.StoreAligned(pLatin1Buffer + num3, vector2);
			num3 += num;
		}
		while (num3 <= num4);
		goto IL_0186;
	}

	public unsafe static void WidenLatin1ToUtf16(byte* pLatin1Buffer, char* pUtf16Buffer, nuint elementCount)
	{
		if (Sse2.IsSupported)
		{
			WidenLatin1ToUtf16_Sse2(pLatin1Buffer, pUtf16Buffer, elementCount);
		}
		else
		{
			WidenLatin1ToUtf16_Fallback(pLatin1Buffer, pUtf16Buffer, elementCount);
		}
	}

	private unsafe static void WidenLatin1ToUtf16_Sse2(byte* pLatin1Buffer, char* pUtf16Buffer, nuint elementCount)
	{
		uint num = (uint)Unsafe.SizeOf<Vector128<byte>>();
		nuint num2 = num - 1;
		nuint num3 = 0u;
		Vector128<byte> zero = Vector128<byte>.Zero;
		if (elementCount >= num)
		{
			Vector128<byte> left = Sse2.LoadScalarVector128((ulong*)pLatin1Buffer).AsByte();
			Sse2.Store((byte*)pUtf16Buffer, Sse2.UnpackLow(left, zero));
			num3 = (num >> 1) - (((nuint)pUtf16Buffer >> 1) & (num2 >> 1));
			char* ptr = pUtf16Buffer + num3;
			nuint num4 = elementCount - num;
			while (num3 <= num4)
			{
				left = Sse2.LoadVector128(pLatin1Buffer + num3);
				Vector128<byte> source = Sse2.UnpackLow(left, zero);
				Sse2.StoreAligned((byte*)ptr, source);
				Vector128<byte> source2 = Sse2.UnpackHigh(left, zero);
				Sse2.StoreAligned((byte*)ptr + num, source2);
				num3 += num;
				ptr += num;
			}
		}
		uint num5 = (uint)((int)elementCount - (int)num3);
		if ((num5 & 8u) != 0)
		{
			Vector128<byte> left = Sse2.LoadScalarVector128((ulong*)(pLatin1Buffer + num3)).AsByte();
			Sse2.Store((byte*)(pUtf16Buffer + num3), Sse2.UnpackLow(left, zero));
			num3 += 8;
		}
		if ((num5 & 4u) != 0)
		{
			Vector128<byte> left = Sse2.LoadScalarVector128((uint*)(pLatin1Buffer + num3)).AsByte();
			Sse2.StoreScalar((ulong*)(pUtf16Buffer + num3), Sse2.UnpackLow(left, zero).AsUInt64());
			num3 += 4;
		}
		if ((num5 & 3) == 0)
		{
			return;
		}
		pUtf16Buffer[num3] = (char)pLatin1Buffer[num3];
		if ((num5 & 2u) != 0)
		{
			pUtf16Buffer[num3 + 1] = (char)pLatin1Buffer[num3 + 1];
			if ((num5 & (true ? 1u : 0u)) != 0)
			{
				pUtf16Buffer[num3 + 2] = (char)pLatin1Buffer[num3 + 2];
			}
		}
	}

	private unsafe static void WidenLatin1ToUtf16_Fallback(byte* pLatin1Buffer, char* pUtf16Buffer, nuint elementCount)
	{
		nuint num = 0u;
		if (Vector.IsHardwareAccelerated)
		{
			uint count = (uint)Vector<byte>.Count;
			if (elementCount >= count)
			{
				nuint num2 = elementCount - count;
				do
				{
					Vector<byte> value = Unsafe.ReadUnaligned<Vector<byte>>(pLatin1Buffer + num);
					Vector.Widen(Vector.AsVectorByte(value), out var low, out var high);
					Unsafe.WriteUnaligned(pUtf16Buffer + num, low);
					Unsafe.WriteUnaligned(pUtf16Buffer + num + Vector<ushort>.Count, high);
					num += count;
				}
				while (num <= num2);
			}
		}
		for (; num < elementCount; num++)
		{
			pUtf16Buffer[num] = (char)pLatin1Buffer[num];
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool AllCharsInUInt32AreLatin1(uint value)
	{
		return (value & 0xFF00FF00u) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool AllCharsInUInt64AreLatin1(ulong value)
	{
		return (value & 0xFF00FF00FF00FF00uL) == 0;
	}

	private static bool FirstCharInUInt32IsLatin1(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if ((value & 0xFF00u) != 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void NarrowFourUtf16CharsToLatin1AndWriteToBuffer(ref byte outputBuffer, ulong value)
	{
		if (Sse2.X64.IsSupported)
		{
			Vector128<short> vector = Sse2.X64.ConvertScalarToVector128UInt64(value).AsInt16();
			Vector128<uint> value2 = Sse2.PackUnsignedSaturate(vector, vector).AsUInt32();
			Unsafe.WriteUnaligned(ref outputBuffer, Sse2.ConvertToUInt32(value2));
			return;
		}
		_ = BitConverter.IsLittleEndian;
		outputBuffer = (byte)value;
		value >>= 16;
		Unsafe.Add(ref outputBuffer, 1) = (byte)value;
		value >>= 16;
		Unsafe.Add(ref outputBuffer, 2) = (byte)value;
		value >>= 16;
		Unsafe.Add(ref outputBuffer, 3) = (byte)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void NarrowTwoUtf16CharsToLatin1AndWriteToBuffer(ref byte outputBuffer, uint value)
	{
		_ = BitConverter.IsLittleEndian;
		outputBuffer = (byte)value;
		Unsafe.Add(ref outputBuffer, 1) = (byte)(value >> 16);
	}
}
