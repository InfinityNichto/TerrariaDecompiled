using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Internal.Runtime.CompilerServices;

namespace System.Text;

internal static class ASCIIUtility
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool AllBytesInUInt64AreAscii(ulong value)
	{
		return (value & 0x8080808080808080uL) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool AllCharsInUInt32AreAscii(uint value)
	{
		return (value & 0xFF80FF80u) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool AllCharsInUInt64AreAscii(ulong value)
	{
		return (value & 0xFF80FF80FF80FF80uL) == 0;
	}

	private static bool FirstCharInUInt32IsAscii(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if ((value & 0xFF80u) != 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static nuint GetIndexOfFirstNonAsciiByte(byte* pBuffer, nuint bufferLength)
	{
		if (!Sse2.IsSupported)
		{
			if (AdvSimd.Arm64.IsSupported)
			{
			}
			return GetIndexOfFirstNonAsciiByte_Default(pBuffer, bufferLength);
		}
		return GetIndexOfFirstNonAsciiByte_Intrinsified(pBuffer, bufferLength);
	}

	private unsafe static nuint GetIndexOfFirstNonAsciiByte_Default(byte* pBuffer, nuint bufferLength)
	{
		byte* ptr = pBuffer;
		if (Vector.IsHardwareAccelerated && bufferLength >= (uint)(2 * Vector<sbyte>.Count))
		{
			uint count = (uint)Vector<sbyte>.Count;
			if (Vector.GreaterThanOrEqualAll(Unsafe.ReadUnaligned<Vector<sbyte>>(pBuffer), Vector<sbyte>.Zero))
			{
				byte* ptr2 = pBuffer + bufferLength - count;
				pBuffer = (byte*)((nuint)(pBuffer + count) & ~(nuint)(count - 1));
				while (!Vector.LessThanAny(Unsafe.Read<Vector<sbyte>>(pBuffer), Vector<sbyte>.Zero))
				{
					pBuffer += count;
					if (pBuffer > ptr2)
					{
						break;
					}
				}
				bufferLength -= (nuint)pBuffer;
				bufferLength = (nuint)(bufferLength + ptr);
			}
		}
		while (true)
		{
			uint num;
			if (bufferLength >= 8)
			{
				num = Unsafe.ReadUnaligned<uint>(pBuffer);
				uint num2 = Unsafe.ReadUnaligned<uint>(pBuffer + 4);
				if (!AllBytesInUInt32AreAscii(num | num2))
				{
					if (AllBytesInUInt32AreAscii(num))
					{
						num = num2;
						pBuffer += 4;
					}
					goto IL_0100;
				}
				pBuffer += 8;
				bufferLength -= 8;
				continue;
			}
			if ((bufferLength & 4) != 0)
			{
				num = Unsafe.ReadUnaligned<uint>(pBuffer);
				if (!AllBytesInUInt32AreAscii(num))
				{
					goto IL_0100;
				}
				pBuffer += 4;
			}
			if ((bufferLength & 2) != 0)
			{
				num = Unsafe.ReadUnaligned<ushort>(pBuffer);
				if (!AllBytesInUInt32AreAscii(num) && BitConverter.IsLittleEndian)
				{
					goto IL_0100;
				}
				pBuffer += 2;
			}
			if ((bufferLength & 1) != 0 && *pBuffer >= 0)
			{
				pBuffer++;
			}
			break;
			IL_0100:
			pBuffer += CountNumberOfLeadingAsciiBytesFromUInt32WithSomeNonAsciiData(num);
			break;
		}
		return (nuint)(pBuffer - (nuint)ptr);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ContainsNonAsciiByte_Sse2(uint sseMask)
	{
		return sseMask != 0;
	}

	private unsafe static nuint GetIndexOfFirstNonAsciiByte_Intrinsified(byte* pBuffer, nuint bufferLength)
	{
		uint num = (uint)Unsafe.SizeOf<Vector128<byte>>();
		nuint num2 = num - 1;
		if (!BitConverter.IsLittleEndian)
		{
		}
		Vector128<byte> vector = Vector128.Create((ushort)4097).AsByte();
		uint num3 = uint.MaxValue;
		uint num4 = uint.MaxValue;
		uint num5 = uint.MaxValue;
		uint num6 = uint.MaxValue;
		byte* ptr = pBuffer;
		if (bufferLength >= num)
		{
			if (Sse2.IsSupported)
			{
				num3 = (uint)Sse2.MoveMask(Sse2.LoadVector128(pBuffer));
				if (!ContainsNonAsciiByte_Sse2(num3))
				{
					if (bufferLength < 2 * num)
					{
						goto IL_0122;
					}
					pBuffer = (byte*)((nuint)(pBuffer + num) & ~num2);
					bufferLength = (nuint)(bufferLength + ptr);
					bufferLength -= (nuint)pBuffer;
					if (bufferLength < 2 * num)
					{
						goto IL_00ef;
					}
					byte* ptr2 = pBuffer + bufferLength - 2 * num;
					while (true)
					{
						if (Sse2.IsSupported)
						{
							Vector128<byte> value = Sse2.LoadAlignedVector128(pBuffer);
							Vector128<byte> value2 = Sse2.LoadAlignedVector128(pBuffer + num);
							num3 = (uint)Sse2.MoveMask(value);
							num4 = (uint)Sse2.MoveMask(value2);
							if (ContainsNonAsciiByte_Sse2(num3 | num4))
							{
								break;
							}
							pBuffer += 2 * num;
							if (pBuffer <= ptr2)
							{
								continue;
							}
							goto IL_00ef;
						}
						if (AdvSimd.Arm64.IsSupported)
						{
						}
						throw new PlatformNotSupportedException();
					}
					if (!Sse2.IsSupported)
					{
						if (AdvSimd.IsSupported)
						{
						}
						throw new PlatformNotSupportedException();
					}
					if (!ContainsNonAsciiByte_Sse2(num3))
					{
						pBuffer += num;
						num3 = num4;
					}
				}
				goto IL_0197;
			}
			if (AdvSimd.Arm64.IsSupported)
			{
			}
			throw new PlatformNotSupportedException();
		}
		if ((bufferLength & 8) != 0)
		{
			_ = UIntPtr.Size;
			ulong num7 = Unsafe.ReadUnaligned<ulong>(pBuffer);
			if (!AllBytesInUInt64AreAscii(num7))
			{
				num7 &= 0x8080808080808080uL;
				pBuffer += (nuint)(BitOperations.TrailingZeroCount(num7) >> 3);
				goto IL_016b;
			}
			pBuffer += 8;
		}
		if ((bufferLength & 4) != 0)
		{
			uint value3 = Unsafe.ReadUnaligned<uint>(pBuffer);
			if (!AllBytesInUInt32AreAscii(value3))
			{
				pBuffer += CountNumberOfLeadingAsciiBytesFromUInt32WithSomeNonAsciiData(value3);
				goto IL_016b;
			}
			pBuffer += 4;
		}
		if ((bufferLength & 2) != 0)
		{
			uint value3 = Unsafe.ReadUnaligned<ushort>(pBuffer);
			if (!AllBytesInUInt32AreAscii(value3))
			{
				pBuffer += (nuint)(((nint)(sbyte)value3 >> 7) + 1);
				goto IL_016b;
			}
			pBuffer += 2;
		}
		if ((bufferLength & 1) != 0 && *pBuffer >= 0)
		{
			pBuffer++;
		}
		goto IL_016b;
		IL_00ef:
		if ((bufferLength & num) == 0)
		{
			goto IL_0128;
		}
		if (Sse2.IsSupported)
		{
			num3 = (uint)Sse2.MoveMask(Sse2.LoadAlignedVector128(pBuffer));
			if (!ContainsNonAsciiByte_Sse2(num3))
			{
				goto IL_0122;
			}
			goto IL_0197;
		}
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		throw new PlatformNotSupportedException();
		IL_016b:
		return (nuint)(pBuffer - (nuint)ptr);
		IL_0122:
		pBuffer += num;
		goto IL_0128;
		IL_0197:
		if (Sse2.IsSupported)
		{
			pBuffer += (uint)BitOperations.TrailingZeroCount(num3);
			goto IL_016b;
		}
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		throw new PlatformNotSupportedException();
		IL_0128:
		if (((byte)bufferLength & num2) != 0)
		{
			pBuffer += (bufferLength & num2) - num;
			if (!Sse2.IsSupported)
			{
				if (AdvSimd.Arm64.IsSupported)
				{
				}
				throw new PlatformNotSupportedException();
			}
			num3 = (uint)Sse2.MoveMask(Sse2.LoadVector128(pBuffer));
			if (ContainsNonAsciiByte_Sse2(num3))
			{
				goto IL_0197;
			}
			pBuffer += num;
		}
		goto IL_016b;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static nuint GetIndexOfFirstNonAsciiChar(char* pBuffer, nuint bufferLength)
	{
		if (!Sse2.IsSupported)
		{
			return GetIndexOfFirstNonAsciiChar_Default(pBuffer, bufferLength);
		}
		return GetIndexOfFirstNonAsciiChar_Sse2(pBuffer, bufferLength);
	}

	private unsafe static nuint GetIndexOfFirstNonAsciiChar_Default(char* pBuffer, nuint bufferLength)
	{
		char* ptr = pBuffer;
		if (Vector.IsHardwareAccelerated && bufferLength >= (uint)(2 * Vector<ushort>.Count))
		{
			uint count = (uint)Vector<ushort>.Count;
			uint count2 = (uint)Vector<byte>.Count;
			Vector<ushort> right = new Vector<ushort>(127);
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
				if (!AllCharsInUInt32AreAscii(num | num2))
				{
					if (AllCharsInUInt32AreAscii(num))
					{
						num = num2;
						pBuffer += 2;
					}
					goto IL_0109;
				}
				pBuffer += 4;
				bufferLength -= 4;
				continue;
			}
			if ((bufferLength & 2) != 0)
			{
				num = Unsafe.ReadUnaligned<uint>(pBuffer);
				if (!AllCharsInUInt32AreAscii(num))
				{
					goto IL_0109;
				}
				pBuffer += 2;
			}
			if ((bufferLength & 1) != 0 && *pBuffer <= '\u007f')
			{
				pBuffer++;
			}
			break;
			IL_0109:
			if (FirstCharInUInt32IsAscii(num))
			{
				pBuffer++;
			}
			break;
		}
		nuint num3 = (nuint)((byte*)pBuffer - (nuint)ptr);
		return num3 / 2;
	}

	private unsafe static nuint GetIndexOfFirstNonAsciiChar_Sse2(char* pBuffer, nuint bufferLength)
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
			right = Vector128.Create((ushort)65408);
			right2 = Vector128.Create((ushort)32640);
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
		if ((bufferLength & 4) != 0)
		{
			_ = UIntPtr.Size;
			ulong num4 = Unsafe.ReadUnaligned<ulong>(pBuffer);
			if (!AllCharsInUInt64AreAscii(num4))
			{
				num4 &= 0xFF80FF80FF80FF80uL;
				pBuffer = (char*)((byte*)pBuffer + (nuint)((BitOperations.TrailingZeroCount(num4) >> 3) & ~(nint)1));
				goto IL_01a1;
			}
			pBuffer += 4;
		}
		if ((bufferLength & 2) != 0)
		{
			uint value = Unsafe.ReadUnaligned<uint>(pBuffer);
			if (!AllCharsInUInt32AreAscii(value))
			{
				if (FirstCharInUInt32IsAscii(value))
				{
					pBuffer++;
				}
				goto IL_01a1;
			}
			pBuffer += 2;
		}
		if ((bufferLength & 1) != 0 && *pBuffer <= '\u007f')
		{
			pBuffer++;
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
		IL_01e6:
		num3 = (uint)Sse2.MoveMask(Sse2.AddSaturate(left, right2).AsByte());
		goto IL_01fa;
		IL_013e:
		pBuffer += num2;
		goto IL_0148;
		IL_01a1:
		return (nuint)(pBuffer - ptr);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void NarrowFourUtf16CharsToAsciiAndWriteToBuffer(ref byte outputBuffer, ulong value)
	{
		if (Sse2.X64.IsSupported)
		{
			Vector128<short> vector = Sse2.X64.ConvertScalarToVector128UInt64(value).AsInt16();
			Vector128<uint> value2 = Sse2.PackUnsignedSaturate(vector, vector).AsUInt32();
			Unsafe.WriteUnaligned(ref outputBuffer, Sse2.ConvertToUInt32(value2));
			return;
		}
		if (AdvSimd.IsSupported)
		{
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
	private static void NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref byte outputBuffer, uint value)
	{
		_ = BitConverter.IsLittleEndian;
		outputBuffer = (byte)value;
		Unsafe.Add(ref outputBuffer, 1) = (byte)(value >> 16);
	}

	public unsafe static nuint NarrowUtf16ToAscii(char* pUtf16Buffer, byte* pAsciiBuffer, nuint elementCount)
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
				if (!AllCharsInUInt64AreAscii(num4))
				{
					goto IL_018b;
				}
				num = NarrowUtf16ToAscii_Sse2(pUtf16Buffer, pAsciiBuffer, elementCount);
			}
		}
		else if (Vector.IsHardwareAccelerated)
		{
			uint num5 = (uint)Unsafe.SizeOf<Vector<byte>>();
			if (elementCount >= 2 * num5)
			{
				_ = IntPtr.Size;
				num4 = Unsafe.ReadUnaligned<ulong>(pUtf16Buffer);
				if (!AllCharsInUInt64AreAscii(num4))
				{
					goto IL_018b;
				}
				Vector<ushort> right = new Vector<ushort>(127);
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
					Unsafe.WriteUnaligned(pAsciiBuffer + num, value);
					num += num5;
				}
				while (num <= num6);
			}
		}
		nuint num7 = elementCount - num;
		if (num7 < 4)
		{
			goto IL_0137;
		}
		nuint num8 = num + num7 - 4;
		while (true)
		{
			_ = IntPtr.Size;
			num4 = Unsafe.ReadUnaligned<ulong>(pUtf16Buffer + num);
			if (!AllCharsInUInt64AreAscii(num4))
			{
				break;
			}
			NarrowFourUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[num], num4);
			num += 4;
			if (num <= num8)
			{
				continue;
			}
			goto IL_0137;
		}
		goto IL_018b;
		IL_01c4:
		if (FirstCharInUInt32IsAscii(num2))
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			pAsciiBuffer[num] = (byte)num2;
			num++;
		}
		goto IL_0189;
		IL_0189:
		return num;
		IL_0137:
		if (((uint)(int)num7 & 2u) != 0)
		{
			num2 = Unsafe.ReadUnaligned<uint>(pUtf16Buffer + num);
			if (!AllCharsInUInt32AreAscii(num2))
			{
				goto IL_01c4;
			}
			NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[num], num2);
			num += 2;
		}
		if (((uint)(int)num7 & (true ? 1u : 0u)) != 0)
		{
			num2 = pUtf16Buffer[num];
			if (num2 <= 127)
			{
				pAsciiBuffer[num] = (byte)num2;
				num++;
			}
		}
		goto IL_0189;
		IL_018b:
		_ = IntPtr.Size;
		_ = BitConverter.IsLittleEndian;
		num2 = (uint)num4;
		if (AllCharsInUInt32AreAscii(num2))
		{
			NarrowTwoUtf16CharsToAsciiAndWriteToBuffer(ref pAsciiBuffer[num], num2);
			_ = BitConverter.IsLittleEndian;
			num2 = (uint)(num4 >> 32);
			num += 2;
		}
		goto IL_01c4;
	}

	private unsafe static nuint NarrowUtf16ToAscii_Sse2(char* pUtf16Buffer, byte* pAsciiBuffer, nuint elementCount)
	{
		uint num = (uint)Unsafe.SizeOf<Vector128<byte>>();
		nuint num2 = num - 1;
		Vector128<short> right = Vector128.Create((short)(-128));
		Vector128<ushort> right2 = Vector128.Create((ushort)32640);
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
		Sse2.StoreScalar((ulong*)pAsciiBuffer, vector2.AsUInt64());
		nuint num3 = num / 2;
		if (((uint)(int)pAsciiBuffer & (num / 2)) != 0)
		{
			goto IL_00e7;
		}
		vector = Sse2.LoadVector128((short*)(pUtf16Buffer + num3));
		if (Sse41.IsSupported)
		{
			if (Sse41.TestZ(vector, right))
			{
				goto IL_00ca;
			}
		}
		else if ((Sse2.MoveMask(Sse2.AddSaturate(vector.AsUInt16(), right2).AsByte()) & 0xAAAA) == 0)
		{
			goto IL_00ca;
		}
		goto IL_0183;
		IL_0183:
		return num3;
		IL_00ca:
		vector2 = Sse2.PackUnsignedSaturate(vector, vector);
		Sse2.StoreScalar((ulong*)(pAsciiBuffer + num3), vector2.AsUInt64());
		goto IL_00e7;
		IL_00e7:
		num3 = num - ((nuint)pAsciiBuffer & num2);
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
					goto IL_015b;
				}
			}
			else if ((Sse2.MoveMask(Sse2.AddSaturate(vector3.AsUInt16(), right2).AsByte()) & 0xAAAA) == 0)
			{
				goto IL_015b;
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
			Sse2.StoreScalar((ulong*)(pAsciiBuffer + num3), vector2.AsUInt64());
			num3 += num / 2;
			break;
			IL_015b:
			vector2 = Sse2.PackUnsignedSaturate(vector, right3);
			Sse2.StoreAligned(pAsciiBuffer + num3, vector2);
			num3 += num;
		}
		while (num3 <= num4);
		goto IL_0183;
	}

	public unsafe static nuint WidenAsciiToUtf16(byte* pAsciiBuffer, char* pUtf16Buffer, nuint elementCount)
	{
		nuint num = 0u;
		_ = BitConverter.IsLittleEndian;
		if (Sse2.IsSupported || AdvSimd.Arm64.IsSupported)
		{
			if (elementCount >= (uint)(2 * Unsafe.SizeOf<Vector128<byte>>()))
			{
				num = WidenAsciiToUtf16_Intrinsified(pAsciiBuffer, pUtf16Buffer, elementCount);
			}
		}
		else if (Vector.IsHardwareAccelerated)
		{
			uint num2 = (uint)Unsafe.SizeOf<Vector<byte>>();
			if (elementCount >= num2)
			{
				nuint num3 = elementCount - num2;
				do
				{
					Vector<sbyte> vector = Unsafe.ReadUnaligned<Vector<sbyte>>(pAsciiBuffer + num);
					if (Vector.LessThanAny(vector, Vector<sbyte>.Zero))
					{
						break;
					}
					Vector.Widen(Vector.AsVectorByte(vector), out var low, out var high);
					Unsafe.WriteUnaligned(pUtf16Buffer + num, low);
					Unsafe.WriteUnaligned(pUtf16Buffer + num + Vector<ushort>.Count, high);
					num += num2;
				}
				while (num <= num3);
			}
		}
		nuint num4 = elementCount - num;
		if (num4 < 4)
		{
			goto IL_00df;
		}
		nuint num5 = num + num4 - 4;
		uint num6;
		while (true)
		{
			num6 = Unsafe.ReadUnaligned<uint>(pAsciiBuffer + num);
			if (!AllBytesInUInt32AreAscii(num6))
			{
				break;
			}
			WidenFourAsciiBytesToUtf16AndWriteToBuffer(ref pUtf16Buffer[num], num6);
			num += 4;
			if (num <= num5)
			{
				continue;
			}
			goto IL_00df;
		}
		goto IL_014f;
		IL_014d:
		return num;
		IL_014f:
		_ = BitConverter.IsLittleEndian;
		while (((byte)num6 & 0x80) == 0)
		{
			pUtf16Buffer[num] = (char)(byte)num6;
			num++;
			num6 >>= 8;
		}
		goto IL_014d;
		IL_00df:
		if (((uint)(int)num4 & 2u) != 0)
		{
			num6 = Unsafe.ReadUnaligned<ushort>(pAsciiBuffer + num);
			if (!AllBytesInUInt32AreAscii(num6) && BitConverter.IsLittleEndian)
			{
				goto IL_014f;
			}
			_ = BitConverter.IsLittleEndian;
			pUtf16Buffer[num] = (char)(byte)num6;
			pUtf16Buffer[num + 1] = (char)(num6 >> 8);
			num += 2;
		}
		if (((uint)(int)num4 & (true ? 1u : 0u)) != 0)
		{
			num6 = pAsciiBuffer[num];
			if (((byte)num6 & 0x80) == 0)
			{
				pUtf16Buffer[num] = (char)num6;
				num++;
			}
		}
		goto IL_014d;
	}

	private unsafe static nuint WidenAsciiToUtf16_Intrinsified(byte* pAsciiBuffer, char* pUtf16Buffer, nuint elementCount)
	{
		uint num = (uint)Unsafe.SizeOf<Vector128<byte>>();
		nuint num2 = num - 1;
		if (Sse2.IsSupported)
		{
			Vector128<byte> vector = Sse2.LoadVector128(pAsciiBuffer);
			if (Sse2.MoveMask(vector) != 0)
			{
				return 0u;
			}
			Vector128<byte> zero = Vector128<byte>.Zero;
			if (Sse2.IsSupported)
			{
				Vector128<byte> source = Sse2.UnpackLow(vector, zero);
				Sse2.Store((byte*)pUtf16Buffer, source);
				nuint num3 = (num >> 1) - (((nuint)pUtf16Buffer >> 1) & (num2 >> 1));
				nuint num4 = elementCount - num;
				char* ptr = pUtf16Buffer + num3;
				while (true)
				{
					if (Sse2.IsSupported)
					{
						vector = Sse2.LoadVector128(pAsciiBuffer + num3);
						bool flag = Sse2.MoveMask(vector) != 0;
						if (!flag)
						{
							if (!Sse2.IsSupported)
							{
								if (AdvSimd.Arm64.IsSupported)
								{
								}
								throw new PlatformNotSupportedException();
							}
							Vector128<byte> source2 = Sse2.UnpackLow(vector, zero);
							Sse2.StoreAligned((byte*)ptr, source2);
							Vector128<byte> source3 = Sse2.UnpackHigh(vector, zero);
							Sse2.StoreAligned((byte*)ptr + num, source3);
							num3 += num;
							ptr += num;
							if (num3 <= num4)
							{
								continue;
							}
						}
						else if (!flag)
						{
							if (!Sse2.IsSupported)
							{
								break;
							}
							source = Sse2.UnpackLow(vector, zero);
							Sse2.StoreAligned((byte*)(pUtf16Buffer + num3), source);
							num3 += num / 2;
						}
						return num3;
					}
					if (AdvSimd.Arm64.IsSupported)
					{
					}
					throw new PlatformNotSupportedException();
				}
				if (AdvSimd.Arm64.IsSupported)
				{
				}
				throw new PlatformNotSupportedException();
			}
			if (AdvSimd.IsSupported)
			{
			}
			throw new PlatformNotSupportedException();
		}
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void WidenFourAsciiBytesToUtf16AndWriteToBuffer(ref char outputBuffer, uint value)
	{
		if (Sse2.X64.IsSupported)
		{
			Vector128<byte> left = Sse2.ConvertScalarToVector128UInt32(value).AsByte();
			Vector128<ulong> value2 = Sse2.UnpackLow(left, Vector128<byte>.Zero).AsUInt64();
			Unsafe.WriteUnaligned(ref Unsafe.As<char, byte>(ref outputBuffer), Sse2.X64.ConvertToUInt64(value2));
			return;
		}
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		_ = BitConverter.IsLittleEndian;
		outputBuffer = (char)(byte)value;
		value >>= 8;
		Unsafe.Add(ref outputBuffer, 1) = (char)(byte)value;
		value >>= 8;
		Unsafe.Add(ref outputBuffer, 2) = (char)(byte)value;
		value >>= 8;
		Unsafe.Add(ref outputBuffer, 3) = (char)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool AllBytesInUInt32AreAscii(uint value)
	{
		return (value & 0x80808080u) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint CountNumberOfLeadingAsciiBytesFromUInt32WithSomeNonAsciiData(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		return (uint)BitOperations.TrailingZeroCount(value & 0x80808080u) >> 3;
	}
}
