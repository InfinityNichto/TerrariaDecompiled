using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Internal.Runtime.CompilerServices;

namespace System.Text.Unicode;

internal static class Utf16Utility
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool AllCharsInUInt32AreAscii(uint value)
	{
		return (value & 0xFF80FF80u) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool AllCharsInUInt64AreAscii(ulong value)
	{
		return (value & 0xFF80FF80FF80FF80uL) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint ConvertAllAsciiCharsInUInt32ToLowercase(uint value)
	{
		uint num = value + 8388736 - 4259905;
		uint num2 = value + 8388736 - 5963867;
		uint num3 = num ^ num2;
		uint num4 = (num3 & 0x800080) >> 2;
		return value ^ num4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static uint ConvertAllAsciiCharsInUInt32ToUppercase(uint value)
	{
		uint num = value + 8388736 - 6357089;
		uint num2 = value + 8388736 - 8061051;
		uint num3 = num ^ num2;
		uint num4 = (num3 & 0x800080) >> 2;
		return value ^ num4;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool UInt32ContainsAnyLowercaseAsciiChar(uint value)
	{
		uint num = value + 8388736 - 6357089;
		uint num2 = value + 8388736 - 8061051;
		uint num3 = num ^ num2;
		return (num3 & 0x800080) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool UInt32ContainsAnyUppercaseAsciiChar(uint value)
	{
		uint num = value + 8388736 - 4259905;
		uint num2 = value + 8388736 - 5963867;
		uint num3 = num ^ num2;
		return (num3 & 0x800080) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool UInt32OrdinalIgnoreCaseAscii(uint valueA, uint valueB)
	{
		uint num = (valueA ^ valueB) << 2;
		uint num2 = valueA + 327685;
		num2 |= 0xA000A0u;
		num2 += 1703962;
		num2 |= 0xFF7FFF7Fu;
		return (num & num2) == 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool UInt64OrdinalIgnoreCaseAscii(ulong valueA, ulong valueB)
	{
		ulong num = (valueA ^ valueB) << 2;
		ulong num2 = valueA + 1407396358717445L;
		num2 |= 0xA000A000A000A0uL;
		num2 += 7318461065330714L;
		num2 |= 0xFF7FFF7FFF7FFF7FuL;
		return (num & num2) == 0;
	}

	public unsafe static char* GetPointerToFirstInvalidChar(char* pInputBuffer, int inputLength, out long utf8CodeUnitCountAdjustment, out int scalarCountAdjustment)
	{
		int num = (int)ASCIIUtility.GetIndexOfFirstNonAsciiChar(pInputBuffer, (uint)inputLength);
		pInputBuffer += (uint)num;
		inputLength -= num;
		if (inputLength == 0)
		{
			utf8CodeUnitCountAdjustment = 0L;
			scalarCountAdjustment = 0;
			return pInputBuffer;
		}
		long num2 = 0L;
		int num3 = 0;
		char* ptr = pInputBuffer + (uint)inputLength;
		if (Sse41.IsSupported)
		{
			if (inputLength >= Vector128<ushort>.Count)
			{
				Vector128<ushort> right = Vector128.Create((ushort)128);
				Vector128<ushort> vector = Vector128.Create((ushort)30720);
				Vector128<ushort> right2 = Vector128.Create((ushort)40960);
				if (!BitConverter.IsLittleEndian)
				{
				}
				Vector128<byte> vector2 = Vector128.Create(9241421688590303745uL).AsByte();
				char* ptr2 = ptr - Vector128<ushort>.Count;
				do
				{
					if (AdvSimd.Arm64.IsSupported)
					{
					}
					Vector128<ushort> vector3 = Sse2.LoadVector128((ushort*)pInputBuffer);
					pInputBuffer += Vector128<ushort>.Count;
					if (AdvSimd.Arm64.IsSupported)
					{
					}
					Vector128<ushort> left = Sse41.Min(vector3, right);
					if (AdvSimd.IsSupported)
					{
					}
					Vector128<ushort> right3 = Sse2.AddSaturate(vector3, vector);
					uint value = (uint)Sse2.MoveMask(Sse2.Or(left, right3).AsByte());
					nuint num4 = (uint)BitOperations.PopCount(value);
					if (AdvSimd.Arm64.IsSupported)
					{
					}
					value = (uint)Sse2.MoveMask(Sse2.CompareLessThan(Sse2.Add(vector3, right2).AsInt16(), vector.AsInt16()).AsByte());
					while (value != 65535)
					{
						value = ~value;
						if (AdvSimd.Arm64.IsSupported)
						{
						}
						uint num5 = (uint)Sse2.MoveMask(Sse2.ShiftRightLogical(vector3, 3).AsByte());
						uint num6 = num5 & value;
						uint num7 = (num5 ^ 0x5555u) & value;
						num7 <<= 2;
						if ((ushort)num7 == num6)
						{
							if (num7 > 65535)
							{
								num7 = (ushort)num7;
								num4 -= 2;
								pInputBuffer--;
							}
							nuint num8 = (uint)BitOperations.PopCount(num7);
							num3 -= (int)num8;
							_ = IntPtr.Size;
							num2 -= (long)num8;
							num2 -= (long)num8;
							value = 65535u;
							continue;
						}
						goto IL_01c6;
					}
					num2 += (long)num4;
					continue;
					IL_01c6:
					pInputBuffer -= Vector128<ushort>.Count;
					break;
				}
				while (pInputBuffer <= ptr2);
			}
		}
		else if (Vector.IsHardwareAccelerated && inputLength >= Vector<ushort>.Count)
		{
			Vector<ushort> right4 = new Vector<ushort>(128);
			Vector<ushort> right5 = new Vector<ushort>(1024);
			Vector<ushort> right6 = new Vector<ushort>(2048);
			Vector<ushort> vector4 = new Vector<ushort>(55296);
			char* ptr3 = ptr - Vector<ushort>.Count;
			while (true)
			{
				Vector<ushort> left2 = Unsafe.ReadUnaligned<Vector<ushort>>(pInputBuffer);
				Vector<ushort> vector5 = Vector.GreaterThanOrEqual(left2, right4);
				Vector<ushort> vector6 = Vector.GreaterThanOrEqual(left2, right6);
				Vector<UIntPtr> vector7 = (Vector<nuint>)(Vector<ushort>.Zero - vector5 - vector6);
				nuint num9 = 0u;
				for (int i = 0; i < Vector<UIntPtr>.Count; i++)
				{
					num9 += (nuint)(nint)(nuint)vector7[i];
				}
				uint num10 = (uint)num9;
				_ = IntPtr.Size;
				num10 += (uint)(int)(num9 >> 32);
				num10 = (ushort)num10 + (num10 >> 16);
				left2 -= vector4;
				Vector<ushort> vector8 = Vector.LessThan(left2, right6);
				if (vector8 != Vector<ushort>.Zero)
				{
					Vector<ushort> right7 = Vector.LessThan(left2, right5);
					Vector<ushort> vector9 = Vector.AndNot(vector8, right7);
					if (vector9[0] != 0)
					{
						break;
					}
					ushort num11 = 0;
					int num12 = 0;
					while (num12 < Vector<ushort>.Count - 1)
					{
						num11 -= right7[num12];
						if (right7[num12] == vector9[num12 + 1])
						{
							num12++;
							continue;
						}
						goto IL_03e0;
					}
					if (right7[Vector<ushort>.Count - 1] != 0)
					{
						pInputBuffer--;
						num10 -= 2;
					}
					nint num13 = num11;
					num3 -= (int)num13;
					num2 -= num13;
					num2 -= num13;
				}
				num2 += num10;
				pInputBuffer += Vector<ushort>.Count;
				if (pInputBuffer <= ptr3)
				{
					continue;
				}
				goto IL_03e0;
			}
			goto IL_03e4;
		}
		goto IL_03e0;
		IL_03e0:
		while (pInputBuffer < ptr)
		{
			uint num14 = *pInputBuffer;
			if (num14 > 127)
			{
				num2 += num14 + 129024 >> 16;
				if (UnicodeUtility.IsSurrogateCodePoint(num14))
				{
					num2 -= 2;
					if ((nuint)((byte*)ptr - (nuint)pInputBuffer) < (nuint)4u)
					{
						break;
					}
					num14 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
					uint num15 = num14;
					if (!BitConverter.IsLittleEndian)
					{
					}
					if (((num15 - 3691042816u) & 0xFC00FC00u) != 0)
					{
						break;
					}
					num3--;
					num2 += 2;
					pInputBuffer++;
				}
			}
			pInputBuffer++;
		}
		goto IL_03e4;
		IL_03e4:
		utf8CodeUnitCountAdjustment = num2;
		scalarCountAdjustment = num3;
		return pInputBuffer;
	}
}
