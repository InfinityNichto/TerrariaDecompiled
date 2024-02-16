using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Internal.Runtime.CompilerServices;

namespace System.Text.Unicode;

internal static class Utf8Utility
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint ExtractCharFromFirstThreeByteSequence(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		return ((value & 0x3F0000) >> 16) | ((value & 0x3F00) >> 2) | ((value & 0xF) << 12);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint ExtractCharFromFirstTwoByteSequence(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		uint num = (uint)((byte)value << 6);
		return (byte)(value >> 8) + num - 12288 - 128;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint ExtractCharsFromFourByteSequence(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		uint num = (uint)((byte)value << 8);
		num |= (value & 0x3F00) >> 6;
		num |= (value & 0x300000) >> 20;
		num |= (value & 0x3F000000) >> 8;
		num |= (value & 0xF0000) << 6;
		num -= 64;
		num -= 8192;
		num += 2048;
		return num + 3690987520u;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint ExtractFourUtf8BytesFromSurrogatePair(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		value += 64;
		uint value2 = BinaryPrimitives.ReverseEndianness(value & 0x3F0700u);
		value2 = BitOperations.RotateLeft(value2, 16);
		uint num = (value & 0xFC) << 6;
		uint num2 = (value >> 6) & 0xF0000u;
		num2 |= num;
		uint num3 = (value & 3) << 20;
		num3 |= 0x808080F0u;
		return num3 | value2 | num2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint ExtractTwoCharsPackedFromTwoAdjacentTwoByteSequences(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		return ((value & 0x3F003F00) >> 8) | ((value & 0x1F001F) << 6);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint ExtractTwoUtf8TwoByteSequencesFromTwoPackedUtf16Chars(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		return ((value >> 6) & 0x1F001F) + ((value << 8) & 0x3F003F00) + 2160099520u;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint ExtractUtf8TwoByteSequenceFromFirstUtf16Char(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		uint num = (value << 2) & 0x1F00u;
		value &= 0x3Fu;
		return BinaryPrimitives.ReverseEndianness((ushort)(num + value + 49280));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsFirstCharAscii(uint value)
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
	private static bool IsFirstCharAtLeastThreeUtf8Bytes(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if ((value & 0xF800) == 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsFirstCharSurrogate(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if (((value - 55296) & 0xF800u) != 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsFirstCharTwoUtf8Bytes(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if (((value - 128) & 0xFFFF) >= 1920)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsLowByteUtf8ContinuationByte(uint value)
	{
		return (uint)(byte)(value - 128) <= 63u;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsSecondCharAscii(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if (value >= 8388608)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsSecondCharAtLeastThreeUtf8Bytes(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if ((value & 0xF8000000u) == 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsSecondCharSurrogate(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if ((uint)((int)value - -671088640) >= 134217728u)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsSecondCharTwoUtf8Bytes(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if (!UnicodeUtility.IsInRangeInclusive(value, 8388608u, 134217727u))
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsUtf8ContinuationByte(in byte value)
	{
		return (sbyte)value < -64;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsWellFormedUtf16SurrogatePair(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if (((value - 3691042816u) & 0xFC00FC00u) != 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint ToLittleEndian(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool UInt32BeginsWithOverlongUtf8TwoByteSequence(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if ((uint)(byte)value >= 194u)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool UInt32BeginsWithUtf8FourByteMask(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if (((value - 2155905264u) & 0xC0C0C0F8u) != 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool UInt32BeginsWithUtf8ThreeByteMask(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if (((value - 8421600) & 0xC0C0F0u) != 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool UInt32BeginsWithUtf8TwoByteMask(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if (((value - 32960) & 0xC0E0u) != 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool UInt32BeginsWithValidUtf8TwoByteSequenceLittleEndian(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if (!UnicodeUtility.IsInRangeInclusive(value & 0xC0FFu, 32962u, 32991u))
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool UInt32EndsWithValidUtf8TwoByteSequenceLittleEndian(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if (!UnicodeUtility.IsInRangeInclusive(value & 0xC0FF0000u, 2160197632u, 2162098176u))
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool UInt32FirstByteIsAscii(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if ((value & 0x80u) != 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool UInt32FourthByteIsAscii(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if ((int)value < 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool UInt32SecondByteIsAscii(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if ((value & 0x8000u) != 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool UInt32ThirdByteIsAscii(uint value)
	{
		_ = BitConverter.IsLittleEndian;
		if ((value & 0x800000u) != 0)
		{
			if (!BitConverter.IsLittleEndian)
			{
			}
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteTwoUtf16CharsAsTwoUtf8ThreeByteSequences(ref byte outputBuffer, uint value)
	{
		_ = BitConverter.IsLittleEndian;
		uint num = ((value << 2) & 0x3F00u) | ((value & 0x3F) << 16);
		uint num2 = ((value >> 4) & 0xF000000u) | ((value >> 12) & 0xFu);
		Unsafe.WriteUnaligned(ref outputBuffer, num + num2 + 3766517984u);
		Unsafe.WriteUnaligned(ref Unsafe.Add(ref outputBuffer, 4), (ushort)(((value >> 22) & 0x3F) + ((value >> 8) & 0x3F00) + 32896));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteFirstUtf16CharAsUtf8ThreeByteSequence(ref byte outputBuffer, uint value)
	{
		_ = BitConverter.IsLittleEndian;
		uint num = (value << 2) & 0x3F00u;
		uint num2 = (uint)(ushort)value >> 12;
		Unsafe.WriteUnaligned(ref outputBuffer, (ushort)(num + num2 + 32992));
		Unsafe.Add(ref outputBuffer, 2) = (byte)((value & 0x3Fu) | 0xFFFFFF80u);
	}

	public unsafe static OperationStatus TranscodeToUtf16(byte* pInputBuffer, int inputLength, char* pOutputBuffer, int outputCharsRemaining, out byte* pInputBufferRemaining, out char* pOutputBufferRemaining)
	{
		nuint num = ASCIIUtility.WidenAsciiToUtf16(pInputBuffer, pOutputBuffer, (uint)Math.Min(inputLength, outputCharsRemaining));
		pInputBuffer += num;
		pOutputBuffer += num;
		if ((int)num == inputLength)
		{
			pInputBufferRemaining = pInputBuffer;
			pOutputBufferRemaining = pOutputBuffer;
			return OperationStatus.Done;
		}
		inputLength -= (int)num;
		outputCharsRemaining -= (int)num;
		if (inputLength < 4)
		{
			goto IL_0690;
		}
		byte* ptr = pInputBuffer + (uint)inputLength - 4;
		while (true)
		{
			IL_004a:
			uint num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
			while (true)
			{
				IL_0051:
				if (!ASCIIUtility.AllBytesInUInt32AreAscii(num2))
				{
					goto IL_0120;
				}
				int num4;
				uint num5;
				if (outputCharsRemaining >= 4)
				{
					ASCIIUtility.WidenFourAsciiBytesToUtf16AndWriteToBuffer(ref *pOutputBuffer, num2);
					pInputBuffer += 4;
					pOutputBuffer += 4;
					outputCharsRemaining -= 4;
					uint val = (uint)((int)(void*)Unsafe.ByteOffset(ref *pInputBuffer, ref *ptr) + 4);
					uint num3 = Math.Min(val, (uint)outputCharsRemaining) / 8;
					num4 = 0;
					while ((uint)num4 < num3)
					{
						num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
						num5 = Unsafe.ReadUnaligned<uint>(pInputBuffer + 4);
						if (ASCIIUtility.AllBytesInUInt32AreAscii(num2 | num5))
						{
							pInputBuffer += 8;
							ASCIIUtility.WidenFourAsciiBytesToUtf16AndWriteToBuffer(ref *pOutputBuffer, num2);
							ASCIIUtility.WidenFourAsciiBytesToUtf16AndWriteToBuffer(ref pOutputBuffer[4], num5);
							pOutputBuffer += 8;
							num4++;
							continue;
						}
						goto IL_00f4;
					}
					outputCharsRemaining -= 8 * num4;
					goto IL_04aa;
				}
				goto IL_04b1;
				IL_04b1:
				inputLength = (int)(void*)Unsafe.ByteOffset(ref *pInputBuffer, ref *ptr) + 4;
				goto IL_0690;
				IL_04aa:
				if (pInputBuffer <= ptr)
				{
					goto IL_004a;
				}
				goto IL_04b1;
				IL_0120:
				if (UInt32FirstByteIsAscii(num2))
				{
					if (outputCharsRemaining >= 3)
					{
						uint num6 = ToLittleEndian(num2);
						nuint num7 = 1u;
						*pOutputBuffer = (char)(byte)num6;
						if (UInt32SecondByteIsAscii(num2))
						{
							num7++;
							num6 >>= 8;
							pOutputBuffer[1] = (char)(byte)num6;
							if (UInt32ThirdByteIsAscii(num2))
							{
								num7++;
								num6 >>= 8;
								pOutputBuffer[2] = (char)(byte)num6;
							}
						}
						pInputBuffer += num7;
						pOutputBuffer += num7;
						outputCharsRemaining -= (int)num7;
					}
					else
					{
						if (outputCharsRemaining == 0)
						{
							break;
						}
						uint num8 = ToLittleEndian(num2);
						pInputBuffer++;
						*(pOutputBuffer++) = (char)(byte)num8;
						outputCharsRemaining--;
						if (UInt32SecondByteIsAscii(num2))
						{
							if (outputCharsRemaining == 0)
							{
								break;
							}
							pInputBuffer++;
							num8 >>= 8;
							*(pOutputBuffer++) = (char)(byte)num8;
							if (UInt32ThirdByteIsAscii(num2))
							{
								break;
							}
							outputCharsRemaining = 0;
						}
					}
					if (pInputBuffer > ptr)
					{
						goto IL_04b1;
					}
					num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
				}
				uint num9;
				while (UInt32BeginsWithUtf8TwoByteMask(num2))
				{
					if (!UInt32BeginsWithOverlongUtf8TwoByteSequence(num2))
					{
						while (true)
						{
							_ = BitConverter.IsLittleEndian;
							if (!UInt32EndsWithValidUtf8TwoByteSequenceLittleEndian(num2) && BitConverter.IsLittleEndian)
							{
								break;
							}
							if (outputCharsRemaining >= 2)
							{
								Unsafe.WriteUnaligned(pOutputBuffer, ExtractTwoCharsPackedFromTwoAdjacentTwoByteSequences(num2));
								pInputBuffer += 4;
								pOutputBuffer += 2;
								outputCharsRemaining -= 2;
								if (pInputBuffer <= ptr)
								{
									num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
									_ = BitConverter.IsLittleEndian;
									if (!UInt32BeginsWithValidUtf8TwoByteSequenceLittleEndian(num2))
									{
										goto IL_0051;
									}
									continue;
								}
							}
							goto IL_04b1;
						}
						num9 = ExtractCharFromFirstTwoByteSequence(num2);
						if (UInt32ThirdByteIsAscii(num2))
						{
							if (UInt32FourthByteIsAscii(num2))
							{
								goto IL_0282;
							}
							if (outputCharsRemaining >= 2)
							{
								*pOutputBuffer = (char)num9;
								char* num10 = pOutputBuffer + 1;
								uint num11 = num2;
								if (!BitConverter.IsLittleEndian)
								{
								}
								*num10 = (char)(byte)(num11 >> 16);
								pInputBuffer += 3;
								pOutputBuffer += 2;
								outputCharsRemaining -= 2;
								if (ptr >= pInputBuffer)
								{
									num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
									continue;
								}
							}
						}
						else if (outputCharsRemaining != 0)
						{
							*pOutputBuffer = (char)num9;
							pInputBuffer += 2;
							pOutputBuffer++;
							outputCharsRemaining--;
							if (ptr >= pInputBuffer)
							{
								num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
								break;
							}
						}
						goto IL_04b1;
					}
					goto IL_06a3;
				}
				if (UInt32BeginsWithUtf8ThreeByteMask(num2))
				{
					while (true)
					{
						_ = BitConverter.IsLittleEndian;
						if ((num2 & 0x200F) == 0 || ((num2 - 8205) & 0x200F) == 0)
						{
							break;
						}
						if (outputCharsRemaining == 0)
						{
							goto end_IL_0051;
						}
						_ = BitConverter.IsLittleEndian;
						if ((((int)num2 - -536870912) & -268435456) == 0 && outputCharsRemaining > 1 && (nint)(void*)Unsafe.ByteOffset(ref *pInputBuffer, ref *ptr) >= (nint)3)
						{
							uint num12 = Unsafe.ReadUnaligned<uint>(pInputBuffer + 3);
							if (UInt32BeginsWithUtf8ThreeByteMask(num12) && (num12 & 0x200Fu) != 0 && ((num12 - 8205) & 0x200Fu) != 0)
							{
								*pOutputBuffer = (char)ExtractCharFromFirstThreeByteSequence(num2);
								pOutputBuffer[1] = (char)ExtractCharFromFirstThreeByteSequence(num12);
								pInputBuffer += 6;
								pOutputBuffer += 2;
								outputCharsRemaining -= 2;
								goto IL_03ff;
							}
						}
						*pOutputBuffer = (char)ExtractCharFromFirstThreeByteSequence(num2);
						pInputBuffer += 3;
						pOutputBuffer++;
						outputCharsRemaining--;
						goto IL_03ff;
						IL_03ff:
						if (UInt32FourthByteIsAscii(num2))
						{
							if (outputCharsRemaining == 0)
							{
								goto end_IL_0051;
							}
							_ = BitConverter.IsLittleEndian;
							*pOutputBuffer = (char)(num2 >> 24);
							pInputBuffer++;
							pOutputBuffer++;
							outputCharsRemaining--;
						}
						if (pInputBuffer <= ptr)
						{
							num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
							if (!UInt32BeginsWithUtf8ThreeByteMask(num2))
							{
								goto IL_0051;
							}
							continue;
						}
						goto IL_04b1;
					}
				}
				else if (UInt32BeginsWithUtf8FourByteMask(num2))
				{
					_ = BitConverter.IsLittleEndian;
					uint value = num2 & 0xFFFFu;
					value = BitOperations.RotateRight(value, 8);
					if (UnicodeUtility.IsInRangeInclusive(value, 4026531984u, 4093640847u))
					{
						if (outputCharsRemaining < 2)
						{
							break;
						}
						Unsafe.WriteUnaligned(pOutputBuffer, ExtractCharsFromFourByteSequence(num2));
						pInputBuffer += 4;
						pOutputBuffer += 2;
						outputCharsRemaining -= 2;
						goto IL_04aa;
					}
				}
				goto IL_06a3;
				IL_0282:
				if (outputCharsRemaining >= 3)
				{
					*pOutputBuffer = (char)num9;
					_ = BitConverter.IsLittleEndian;
					num2 >>= 16;
					pOutputBuffer[1] = (char)(byte)num2;
					num2 >>= 8;
					pOutputBuffer[2] = (char)num2;
					pInputBuffer += 4;
					pOutputBuffer += 3;
					outputCharsRemaining -= 3;
					goto IL_04aa;
				}
				goto IL_04b1;
				IL_00f4:
				if (ASCIIUtility.AllBytesInUInt32AreAscii(num2))
				{
					ASCIIUtility.WidenFourAsciiBytesToUtf16AndWriteToBuffer(ref *pOutputBuffer, num2);
					num2 = num5;
					pInputBuffer += 4;
					pOutputBuffer += 4;
					outputCharsRemaining -= 4;
				}
				outputCharsRemaining -= 8 * num4;
				goto IL_0120;
				continue;
				end_IL_0051:
				break;
			}
			break;
		}
		goto IL_069f;
		IL_06a3:
		OperationStatus result = OperationStatus.InvalidData;
		goto IL_06a5;
		IL_069f:
		result = OperationStatus.DestinationTooSmall;
		goto IL_06a5;
		IL_06a5:
		pInputBufferRemaining = pInputBuffer;
		pOutputBufferRemaining = pOutputBuffer;
		return result;
		IL_0690:
		while (true)
		{
			if (inputLength > 0)
			{
				uint num13 = *pInputBuffer;
				if (num13 <= 127)
				{
					if (outputCharsRemaining != 0)
					{
						*pOutputBuffer = (char)num13;
						pInputBuffer++;
						pOutputBuffer++;
						inputLength--;
						outputCharsRemaining--;
						continue;
					}
					goto IL_069f;
				}
				num13 -= 194;
				if ((uint)(byte)num13 <= 29u)
				{
					if (inputLength < 2)
					{
						goto IL_069b;
					}
					uint num14 = pInputBuffer[1];
					if (IsLowByteUtf8ContinuationByte(num14))
					{
						if (outputCharsRemaining != 0)
						{
							uint num15 = (num13 << 6) + num14 + 128 - 128;
							*pOutputBuffer = (char)num15;
							pInputBuffer += 2;
							pOutputBuffer++;
							inputLength -= 2;
							outputCharsRemaining--;
							continue;
						}
						goto IL_069f;
					}
				}
				else if ((uint)(byte)num13 <= 45u)
				{
					if (inputLength >= 3)
					{
						uint num16 = pInputBuffer[1];
						uint num17 = pInputBuffer[2];
						if (IsLowByteUtf8ContinuationByte(num16) && IsLowByteUtf8ContinuationByte(num17))
						{
							uint num18 = (num13 << 12) + (num16 << 6);
							if (num18 >= 133120)
							{
								num18 -= 186368;
								if (num18 >= 2048)
								{
									if (outputCharsRemaining != 0)
									{
										num18 += num17;
										num18 += 55296;
										num18 -= 128;
										*pOutputBuffer = (char)num18;
										pInputBuffer += 3;
										pOutputBuffer++;
										inputLength -= 3;
										outputCharsRemaining--;
										continue;
									}
									goto IL_069f;
								}
							}
						}
					}
					else
					{
						if (inputLength < 2)
						{
							goto IL_069b;
						}
						uint num19 = pInputBuffer[1];
						if (IsLowByteUtf8ContinuationByte(num19))
						{
							uint num20 = (num13 << 6) + num19;
							if (num20 >= 2080 && !UnicodeUtility.IsInRangeInclusive(num20, 2912u, 2943u))
							{
								goto IL_069b;
							}
						}
					}
				}
				else if ((uint)(byte)num13 <= 50u)
				{
					if (inputLength < 2)
					{
						goto IL_069b;
					}
					uint num21 = pInputBuffer[1];
					if (IsLowByteUtf8ContinuationByte(num21))
					{
						uint value2 = (num13 << 6) + num21;
						if (UnicodeUtility.IsInRangeInclusive(value2, 3088u, 3343u))
						{
							if (inputLength < 3)
							{
								goto IL_069b;
							}
							if (IsLowByteUtf8ContinuationByte(pInputBuffer[2]))
							{
								if (inputLength < 4)
								{
									goto IL_069b;
								}
								if (IsLowByteUtf8ContinuationByte(pInputBuffer[3]))
								{
									goto IL_069f;
								}
							}
						}
					}
				}
				goto IL_06a3;
			}
			result = OperationStatus.Done;
			break;
			IL_069b:
			result = OperationStatus.NeedMoreData;
			break;
		}
		goto IL_06a5;
	}

	public unsafe static OperationStatus TranscodeToUtf8(char* pInputBuffer, int inputLength, byte* pOutputBuffer, int outputBytesRemaining, out char* pInputBufferRemaining, out byte* pOutputBufferRemaining)
	{
		nuint num = ASCIIUtility.NarrowUtf16ToAscii(pInputBuffer, pOutputBuffer, (uint)Math.Min(inputLength, outputBytesRemaining));
		pInputBuffer += num;
		pOutputBuffer += num;
		if ((int)num == inputLength)
		{
			pInputBufferRemaining = pInputBuffer;
			pOutputBufferRemaining = pOutputBuffer;
			return OperationStatus.Done;
		}
		inputLength -= (int)num;
		outputBytesRemaining -= (int)num;
		if (inputLength < 2)
		{
			goto IL_04f0;
		}
		char* ptr = pInputBuffer + (uint)inputLength - 2;
		Vector128<short> value;
		if (Sse41.X64.IsSupported || AdvSimd.Arm64.IsSupported)
		{
			value = Vector128.Create((short)(-128));
		}
		uint num2;
		while (true)
		{
			IL_006d:
			num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
			while (true)
			{
				IL_0074:
				if (!Utf16Utility.AllCharsInUInt32AreAscii(num2))
				{
					goto IL_02e9;
				}
				if (outputBytesRemaining < 2)
				{
					break;
				}
				uint num3 = num2 | (num2 >> 8);
				Unsafe.WriteUnaligned(pOutputBuffer, (ushort)num3);
				pInputBuffer += 2;
				pOutputBuffer += 2;
				outputBytesRemaining -= 2;
				uint num4 = (uint)((int)(ptr - pInputBuffer) + 2);
				uint num5 = (uint)Math.Min(num4, outputBytesRemaining);
				int num7;
				ulong num8;
				Vector128<short> vector;
				int num10;
				uint num11;
				if (Sse41.X64.IsSupported || AdvSimd.Arm64.IsSupported)
				{
					uint num6 = num5 / 8;
					num7 = 0;
					while ((uint)num7 < num6)
					{
						Unsafe.SkipInit<Vector128<short>>(out value);
						vector = Unsafe.ReadUnaligned<Vector128<short>>(pInputBuffer);
						if (AdvSimd.IsSupported)
						{
						}
						if (Sse41.TestZ(vector, value))
						{
							Sse2.StoreScalar((ulong*)pOutputBuffer, Sse2.PackUnsignedSaturate(vector, vector).AsUInt64());
							pInputBuffer += 8;
							pOutputBuffer += 8;
							num7++;
							continue;
						}
						goto IL_019c;
					}
					outputBytesRemaining -= 8 * num7;
					if ((num5 & 4u) != 0)
					{
						num8 = Unsafe.ReadUnaligned<ulong>(pInputBuffer);
						if (!Utf16Utility.AllCharsInUInt64AreAscii(num8))
						{
							goto IL_0213;
						}
						vector = Vector128.CreateScalarUnsafe(num8).AsInt16();
						if (AdvSimd.IsSupported)
						{
						}
						Unsafe.WriteUnaligned(pOutputBuffer, Sse2.ConvertToUInt32(Sse2.PackUnsignedSaturate(vector, vector).AsUInt32()));
						pInputBuffer += 4;
						pOutputBuffer += 4;
						outputBytesRemaining -= 4;
					}
				}
				else
				{
					uint num9 = num5 / 4;
					num10 = 0;
					while ((uint)num10 < num9)
					{
						num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
						num11 = Unsafe.ReadUnaligned<uint>(pInputBuffer + 2);
						if (Utf16Utility.AllCharsInUInt32AreAscii(num2 | num11))
						{
							Unsafe.WriteUnaligned(pOutputBuffer, (ushort)(num2 | (num2 >> 8)));
							Unsafe.WriteUnaligned(pOutputBuffer + 2, (ushort)(num11 | (num11 >> 8)));
							pInputBuffer += 4;
							pOutputBuffer += 4;
							num10++;
							continue;
						}
						goto IL_02b8;
					}
					outputBytesRemaining -= 4 * num10;
				}
				goto IL_04de;
				IL_04ae:
				if (IsWellFormedUtf16SurrogatePair(num2))
				{
					if (outputBytesRemaining >= 4)
					{
						Unsafe.WriteUnaligned(pOutputBuffer, ExtractFourUtf8BytesFromSurrogatePair(num2));
						pInputBuffer += 2;
						pOutputBuffer += 4;
						outputBytesRemaining -= 4;
						goto IL_04de;
					}
					goto IL_05a7;
				}
				goto IL_05ac;
				IL_038f:
				if (outputBytesRemaining >= 3)
				{
					_ = BitConverter.IsLittleEndian;
					num2 >>= 16;
					pOutputBuffer[2] = (byte)num2;
					pInputBuffer += 2;
					pOutputBuffer += 3;
					outputBytesRemaining -= 3;
					goto IL_04de;
				}
				pInputBuffer++;
				pOutputBuffer += 2;
				goto IL_05a7;
				IL_04de:
				if (pInputBuffer <= ptr)
				{
					goto IL_006d;
				}
				goto IL_04e5;
				IL_04e5:
				inputLength = (int)(ptr - pInputBuffer) + 2;
				goto IL_04f0;
				IL_0213:
				num2 = (uint)num8;
				if (Utf16Utility.AllCharsInUInt32AreAscii(num2))
				{
					Unsafe.WriteUnaligned(pOutputBuffer, (ushort)(num2 | (num2 >> 8)));
					pInputBuffer += 2;
					pOutputBuffer += 2;
					outputBytesRemaining -= 2;
					num2 = (uint)(num8 >> 32);
				}
				goto IL_02e9;
				IL_019c:
				outputBytesRemaining -= 8 * num7;
				num8 = ((!Sse2.X64.IsSupported) ? vector.AsUInt64().ToScalar() : Sse2.X64.ConvertToUInt64(vector.AsUInt64()));
				if (Utf16Utility.AllCharsInUInt64AreAscii(num8))
				{
					if (AdvSimd.IsSupported)
					{
					}
					Unsafe.WriteUnaligned(pOutputBuffer, Sse2.ConvertToUInt32(Sse2.PackUnsignedSaturate(vector, vector).AsUInt32()));
					pInputBuffer += 4;
					pOutputBuffer += 4;
					outputBytesRemaining -= 4;
					num8 = vector.AsUInt64().GetElement(1);
				}
				goto IL_0213;
				IL_02b8:
				outputBytesRemaining -= 4 * num10;
				if (Utf16Utility.AllCharsInUInt32AreAscii(num2))
				{
					Unsafe.WriteUnaligned(pOutputBuffer, (ushort)(num2 | (num2 >> 8)));
					pInputBuffer += 2;
					pOutputBuffer += 2;
					outputBytesRemaining -= 2;
					num2 = num11;
				}
				goto IL_02e9;
				IL_02e9:
				while (true)
				{
					if (IsFirstCharAscii(num2))
					{
						if (outputBytesRemaining == 0)
						{
							break;
						}
						_ = BitConverter.IsLittleEndian;
						*pOutputBuffer = (byte)num2;
						pInputBuffer++;
						pOutputBuffer++;
						outputBytesRemaining--;
						if (pInputBuffer > ptr)
						{
							goto IL_04e5;
						}
						num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
					}
					if (!IsFirstCharAtLeastThreeUtf8Bytes(num2))
					{
						while (IsSecondCharTwoUtf8Bytes(num2))
						{
							if (outputBytesRemaining < 4)
							{
								goto end_IL_0074;
							}
							Unsafe.WriteUnaligned(pOutputBuffer, ExtractTwoUtf8TwoByteSequencesFromTwoPackedUtf16Chars(num2));
							pInputBuffer += 2;
							pOutputBuffer += 4;
							outputBytesRemaining -= 4;
							if (pInputBuffer <= ptr)
							{
								num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
								if (!IsFirstCharTwoUtf8Bytes(num2))
								{
									goto IL_0074;
								}
								continue;
							}
							goto IL_04e5;
						}
						if (outputBytesRemaining < 2)
						{
							break;
						}
						Unsafe.WriteUnaligned(pOutputBuffer, (ushort)ExtractUtf8TwoByteSequenceFromFirstUtf16Char(num2));
						if (IsSecondCharAscii(num2))
						{
							goto IL_038f;
						}
						pInputBuffer++;
						pOutputBuffer += 2;
						outputBytesRemaining -= 2;
						if (pInputBuffer > ptr)
						{
							goto IL_04e5;
						}
						num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
					}
					while (!IsFirstCharSurrogate(num2))
					{
						if (IsSecondCharAtLeastThreeUtf8Bytes(num2) && !IsSecondCharSurrogate(num2) && outputBytesRemaining >= 6)
						{
							WriteTwoUtf16CharsAsTwoUtf8ThreeByteSequences(ref *pOutputBuffer, num2);
							pInputBuffer += 2;
							pOutputBuffer += 6;
							outputBytesRemaining -= 6;
							if (pInputBuffer <= ptr)
							{
								num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
								if (!IsFirstCharAtLeastThreeUtf8Bytes(num2))
								{
									goto IL_0074;
								}
								continue;
							}
						}
						else
						{
							if (outputBytesRemaining < 3)
							{
								goto end_IL_02e9;
							}
							WriteFirstUtf16CharAsUtf8ThreeByteSequence(ref *pOutputBuffer, num2);
							pInputBuffer++;
							pOutputBuffer += 3;
							outputBytesRemaining -= 3;
							if (!IsSecondCharAscii(num2))
							{
								goto IL_049e;
							}
							if (outputBytesRemaining == 0)
							{
								goto end_IL_02e9;
							}
							_ = BitConverter.IsLittleEndian;
							*pOutputBuffer = (byte)(num2 >> 16);
							pInputBuffer++;
							pOutputBuffer++;
							outputBytesRemaining--;
							if (pInputBuffer <= ptr)
							{
								num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
								if (!IsFirstCharAtLeastThreeUtf8Bytes(num2))
								{
									goto IL_0074;
								}
								continue;
							}
						}
						goto IL_04e5;
					}
					goto IL_04ae;
					IL_049e:
					if (pInputBuffer <= ptr)
					{
						num2 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
						continue;
					}
					goto IL_04e5;
					continue;
					end_IL_02e9:
					break;
				}
				goto IL_05a7;
				continue;
				end_IL_0074:
				break;
			}
			break;
		}
		_ = BitConverter.IsLittleEndian;
		uint num12 = num2 & 0xFFFFu;
		goto IL_050b;
		IL_05af:
		pInputBufferRemaining = pInputBuffer;
		pOutputBufferRemaining = pOutputBuffer;
		OperationStatus result;
		return result;
		IL_05a7:
		result = OperationStatus.DestinationTooSmall;
		goto IL_05af;
		IL_05ac:
		result = OperationStatus.InvalidData;
		goto IL_05af;
		IL_059d:
		result = OperationStatus.Done;
		goto IL_05af;
		IL_050b:
		if (num12 <= 127)
		{
			if (outputBytesRemaining != 0)
			{
				*pOutputBuffer = (byte)num12;
				pInputBuffer++;
				pOutputBuffer++;
				goto IL_0599;
			}
		}
		else if (num12 < 2048)
		{
			if (outputBytesRemaining >= 2)
			{
				pOutputBuffer[1] = (byte)((num12 & 0x3Fu) | 0xFFFFFF80u);
				*pOutputBuffer = (byte)((num12 >> 6) | 0xFFFFFFC0u);
				pInputBuffer++;
				pOutputBuffer += 2;
				goto IL_0599;
			}
		}
		else
		{
			if (UnicodeUtility.IsSurrogateCodePoint(num12))
			{
				if (num12 > 56319)
				{
					goto IL_05ac;
				}
				result = OperationStatus.NeedMoreData;
				goto IL_05af;
			}
			if (outputBytesRemaining >= 3)
			{
				pOutputBuffer[2] = (byte)((num12 & 0x3Fu) | 0xFFFFFF80u);
				pOutputBuffer[1] = (byte)(((num12 >> 6) & 0x3Fu) | 0xFFFFFF80u);
				*pOutputBuffer = (byte)((num12 >> 12) | 0xFFFFFFE0u);
				pInputBuffer++;
				pOutputBuffer += 3;
				goto IL_0599;
			}
		}
		goto IL_05a7;
		IL_04f0:
		if (inputLength != 0)
		{
			num12 = *pInputBuffer;
			goto IL_050b;
		}
		goto IL_059d;
		IL_0599:
		if (inputLength <= 1)
		{
			goto IL_059d;
		}
		goto IL_05a7;
	}

	public unsafe static byte* GetPointerToFirstInvalidByte(byte* pInputBuffer, int inputLength, out int utf16CodeUnitCountAdjustment, out int scalarCountAdjustment)
	{
		nuint indexOfFirstNonAsciiByte = ASCIIUtility.GetIndexOfFirstNonAsciiByte(pInputBuffer, (uint)inputLength);
		pInputBuffer += indexOfFirstNonAsciiByte;
		inputLength -= (int)indexOfFirstNonAsciiByte;
		if (inputLength == 0)
		{
			utf16CodeUnitCountAdjustment = 0;
			scalarCountAdjustment = 0;
			return pInputBuffer;
		}
		int num = 0;
		int num2 = 0;
		nuint num13;
		if (inputLength >= 4)
		{
			byte* ptr = pInputBuffer + (uint)inputLength - 4;
			while (pInputBuffer <= ptr)
			{
				uint num3 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
				while (true)
				{
					IL_0043:
					if (ASCIIUtility.AllBytesInUInt32AreAscii(num3))
					{
						pInputBuffer += 4;
						if ((nint)(void*)Unsafe.ByteOffset(ref *pInputBuffer, ref *ptr) < (nint)16)
						{
							break;
						}
						num3 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
						if (ASCIIUtility.AllBytesInUInt32AreAscii(num3))
						{
							pInputBuffer = (byte*)((nuint)(pInputBuffer + 4) & ~(nuint)3u);
							byte* ptr2 = ptr - 12;
							if (AdvSimd.Arm64.IsSupported)
							{
							}
							uint num4;
							while (true)
							{
								if (Sse2.IsSupported)
								{
									num4 = (uint)Sse2.MoveMask(Sse2.LoadVector128(pInputBuffer));
									if (num4 != 0)
									{
										break;
									}
									goto IL_00da;
								}
								if (ASCIIUtility.AllBytesInUInt32AreAscii(*(uint*)pInputBuffer | *(uint*)(pInputBuffer + 4)))
								{
									if (ASCIIUtility.AllBytesInUInt32AreAscii(*(uint*)(pInputBuffer + (nint)2 * (nint)4) | *(uint*)(pInputBuffer + (nint)3 * (nint)4)))
									{
										goto IL_00da;
									}
									pInputBuffer += 8;
								}
								num3 = *(uint*)pInputBuffer;
								if (ASCIIUtility.AllBytesInUInt32AreAscii(num3))
								{
									pInputBuffer += 4;
									num3 = *(uint*)pInputBuffer;
								}
								goto IL_011e;
								IL_00da:
								pInputBuffer += 16;
								if (pInputBuffer > ptr2)
								{
									goto end_IL_0043;
								}
							}
							nuint num5 = (nuint)BitOperations.TrailingZeroCount(num4);
							pInputBuffer += num5;
							if (pInputBuffer > ptr)
							{
								goto end_IL_0428;
							}
							num3 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
							goto IL_013d;
						}
					}
					goto IL_011e;
					IL_011e:
					uint num6 = ASCIIUtility.CountNumberOfLeadingAsciiBytesFromUInt32WithSomeNonAsciiData(num3);
					pInputBuffer += num6;
					if (ptr < pInputBuffer)
					{
						goto end_IL_0428;
					}
					num3 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
					goto IL_013d;
					IL_039e:
					pInputBuffer += 6;
					num -= 4;
					break;
					IL_013d:
					while (true)
					{
						uint num7 = num3;
						if (!BitConverter.IsLittleEndian)
						{
						}
						num3 = num7 - 32960;
						uint num8 = num3;
						if (!BitConverter.IsLittleEndian)
						{
						}
						if ((num8 & 0xC0E0u) != 0)
						{
							break;
						}
						_ = BitConverter.IsLittleEndian;
						if ((uint)(byte)num3 >= 2u)
						{
							if (!BitConverter.IsLittleEndian)
							{
							}
							while (true)
							{
								_ = BitConverter.IsLittleEndian;
								if (!UInt32EndsWithValidUtf8TwoByteSequenceLittleEndian(num3) && BitConverter.IsLittleEndian)
								{
									break;
								}
								pInputBuffer += 4;
								num -= 2;
								if (pInputBuffer > ptr)
								{
									goto end_IL_0428;
								}
								num3 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
								_ = BitConverter.IsLittleEndian;
								if (!UInt32BeginsWithValidUtf8TwoByteSequenceLittleEndian(num3))
								{
									goto IL_0043;
								}
							}
							num--;
							if (UInt32ThirdByteIsAscii(num3))
							{
								if (UInt32FourthByteIsAscii(num3))
								{
									pInputBuffer += 4;
									goto end_IL_0043;
								}
								pInputBuffer += 3;
								if (pInputBuffer > ptr)
								{
									goto end_IL_0043;
								}
								num3 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
								continue;
							}
							pInputBuffer += 2;
							goto end_IL_0043;
						}
						goto IL_0521;
					}
					uint num9 = num3;
					if (!BitConverter.IsLittleEndian)
					{
					}
					num3 = num9 - 8388640;
					uint num10 = num3;
					if (!BitConverter.IsLittleEndian)
					{
					}
					if ((num10 & 0xC0C0F0) == 0)
					{
						while (true)
						{
							_ = BitConverter.IsLittleEndian;
							if ((num3 & 0x200F) == 0 || ((num3 - 8205) & 0x200F) == 0)
							{
								break;
							}
							while (true)
							{
								IL_024a:
								_ = BitConverter.IsLittleEndian;
								nint num11 = (int)num3 >> 31;
								pInputBuffer += 4;
								pInputBuffer += num11;
								num -= 2;
								ulong num12;
								while (true)
								{
									_ = IntPtr.Size;
									_ = BitConverter.IsLittleEndian;
									if ((nint)(ptr - pInputBuffer) < 5)
									{
										break;
									}
									num12 = Unsafe.ReadUnaligned<ulong>(pInputBuffer);
									num3 = (uint)num12;
									if ((num12 & 0xC0F0C0C0F0C0C0F0uL) == 9286563722648649952uL && IsUtf8ContinuationByte(in pInputBuffer[8]))
									{
										if (((int)num12 & 0x200F) == 0 || (((int)num12 - 8205) & 0x200F) == 0)
										{
											goto end_IL_0222;
										}
										num12 >>= 24;
										if (((uint)(int)num12 & 0x200Fu) != 0 && ((uint)((int)num12 - 8205) & 0x200Fu) != 0)
										{
											num12 >>= 24;
											if (((uint)(int)num12 & 0x200Fu) != 0 && ((uint)((int)num12 - 8205) & 0x200Fu) != 0)
											{
												pInputBuffer += 9;
												num -= 6;
												continue;
											}
										}
										goto IL_024a;
									}
									goto IL_033c;
								}
								break;
								IL_033c:
								if ((num12 & 0xC0C0F0C0C0F0L) == 141291010687200L)
								{
									if (((int)num12 & 0x200F) == 0 || (((int)num12 - 8205) & 0x200F) == 0)
									{
										goto end_IL_0222;
									}
									num12 >>= 24;
									if (((int)num12 & 0x200F) == 0 || (((int)num12 - 8205) & 0x200F) == 0)
									{
										continue;
									}
									goto IL_039e;
								}
								goto IL_03ac;
							}
							if (pInputBuffer > ptr)
							{
								goto end_IL_0428;
							}
							num3 = Unsafe.ReadUnaligned<uint>(pInputBuffer);
							if (!UInt32BeginsWithUtf8ThreeByteMask(num3))
							{
								goto IL_0043;
							}
							continue;
							IL_03ac:
							if (!UInt32BeginsWithUtf8ThreeByteMask(num3))
							{
								goto IL_0043;
							}
							continue;
							end_IL_0222:
							break;
						}
					}
					else
					{
						_ = BitConverter.IsLittleEndian;
						num3 &= 0xC0C0FFFFu;
						if ((int)num3 <= -2147467265)
						{
							num3 = BitOperations.RotateRight(num3, 8);
							if (UnicodeUtility.IsInRangeInclusive(num3, 276824080u, 343932943u))
							{
								pInputBuffer += 4;
								num -= 2;
								num2--;
								break;
							}
						}
					}
					goto IL_0521;
					continue;
					end_IL_0043:
					break;
				}
				continue;
				end_IL_0428:
				break;
			}
			num13 = (nuint)((byte*)(void*)Unsafe.ByteOffset(ref *pInputBuffer, ref *ptr) + 4);
		}
		else
		{
			num13 = (uint)inputLength;
		}
		while (num13 != 0)
		{
			uint num14 = *pInputBuffer;
			if ((uint)(byte)num14 < 128u)
			{
				pInputBuffer++;
				num13--;
				continue;
			}
			if (num13 < 2)
			{
				break;
			}
			uint value = pInputBuffer[1];
			if ((uint)(byte)num14 < 224u)
			{
				if ((uint)(byte)num14 < 194u || !IsLowByteUtf8ContinuationByte(value))
				{
					break;
				}
				pInputBuffer += 2;
				num--;
				num13 -= 2;
				continue;
			}
			if (num13 < 3 || (uint)(byte)num14 >= 240u)
			{
				break;
			}
			if ((byte)num14 == 224)
			{
				if (!UnicodeUtility.IsInRangeInclusive(value, 160u, 191u))
				{
					break;
				}
			}
			else if ((byte)num14 == 237)
			{
				if (!UnicodeUtility.IsInRangeInclusive(value, 128u, 159u))
				{
					break;
				}
			}
			else if (!IsLowByteUtf8ContinuationByte(value))
			{
				break;
			}
			if (!IsUtf8ContinuationByte(in pInputBuffer[2]))
			{
				break;
			}
			pInputBuffer += 3;
			num -= 2;
			num13 -= 3;
		}
		goto IL_0521;
		IL_0521:
		utf16CodeUnitCountAdjustment = num;
		scalarCountAdjustment = num2;
		return pInputBuffer;
	}
}
