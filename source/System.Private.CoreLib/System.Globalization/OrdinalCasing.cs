using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Internal.Runtime.CompilerServices;

namespace System.Globalization;

internal static class OrdinalCasing
{
	private static ushort[] s_noCasingPage = Array.Empty<ushort>();

	private static ushort[] s_basicLatin = new ushort[256]
	{
		0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
		10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
		20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
		30, 31, 32, 33, 34, 35, 36, 37, 38, 39,
		40, 41, 42, 43, 44, 45, 46, 47, 48, 49,
		50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
		60, 61, 62, 63, 64, 65, 66, 67, 68, 69,
		70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
		80, 81, 82, 83, 84, 85, 86, 87, 88, 89,
		90, 91, 92, 93, 94, 95, 96, 65, 66, 67,
		68, 69, 70, 71, 72, 73, 74, 75, 76, 77,
		78, 79, 80, 81, 82, 83, 84, 85, 86, 87,
		88, 89, 90, 123, 124, 125, 126, 127, 128, 129,
		130, 131, 132, 133, 134, 135, 136, 137, 138, 139,
		140, 141, 142, 143, 144, 145, 146, 147, 148, 149,
		150, 151, 152, 153, 154, 155, 156, 157, 158, 159,
		160, 161, 162, 163, 164, 165, 166, 167, 168, 169,
		170, 171, 172, 173, 174, 175, 176, 177, 178, 179,
		180, 924, 182, 183, 184, 185, 186, 187, 188, 189,
		190, 191, 192, 193, 194, 195, 196, 197, 198, 199,
		200, 201, 202, 203, 204, 205, 206, 207, 208, 209,
		210, 211, 212, 213, 214, 215, 216, 217, 218, 219,
		220, 221, 222, 223, 192, 193, 194, 195, 196, 197,
		198, 199, 200, 201, 202, 203, 204, 205, 206, 207,
		208, 209, 210, 211, 212, 213, 214, 247, 216, 217,
		218, 219, 220, 221, 222, 376
	};

	private static ushort[][] s_casingTable = InitCasingTable();

	private static ReadOnlySpan<byte> s_casingTableInit => new byte[32]
	{
		0, 0, 76, 0, 55, 224, 31, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 254,
		244, 15, 255, 255, 255, 255, 254, 255, 255, 255,
		255, 200
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static char ToUpper(char c)
	{
		int num = (int)c >> 8;
		if (num == 0)
		{
			return (char)s_basicLatin[(uint)c];
		}
		ushort[] array = s_casingTable[num];
		if (array == s_noCasingPage)
		{
			return c;
		}
		if (array == null)
		{
			array = InitOrdinalCasingPage(num);
		}
		return (char)array[c & 0xFF];
	}

	internal static void ToUpperOrdinal(ReadOnlySpan<char> source, Span<char> destination)
	{
		for (int i = 0; i < source.Length; i++)
		{
			char c = source[i];
			if (c <= 'ÿ')
			{
				destination[i] = (char)s_basicLatin[(uint)c];
				continue;
			}
			if (char.IsHighSurrogate(c) && i < source.Length - 1)
			{
				char c2 = source[i + 1];
				if (char.IsLowSurrogate(c2))
				{
					SurrogateCasing.ToUpper(c, c2, out destination[i], out destination[i + 1]);
					i++;
					continue;
				}
			}
			destination[i] = ToUpper(c);
		}
	}

	internal static int CompareStringIgnoreCase(ref char strA, int lengthA, ref char strB, int lengthB)
	{
		int num = Math.Min(lengthA, lengthB);
		ref char reference = ref strA;
		ref char reference2 = ref strB;
		int num2 = 0;
		while (num2 < num)
		{
			char c = reference;
			char c2 = reference2;
			char c3 = '\0';
			if (!char.IsHighSurrogate(c) || num2 >= lengthA - 1 || !char.IsLowSurrogate(c3 = Unsafe.Add(ref reference, 1)))
			{
				if (!char.IsHighSurrogate(c2) || num2 >= lengthB - 1 || !char.IsLowSurrogate(Unsafe.Add(ref reference2, 1)))
				{
					if (c2 == c)
					{
						num2++;
						reference = ref Unsafe.Add(ref reference, 1);
						reference2 = ref Unsafe.Add(ref reference2, 1);
						continue;
					}
					char c4 = ToUpper(c);
					char c5 = ToUpper(c2);
					if (c4 == c5)
					{
						num2++;
						reference = ref Unsafe.Add(ref reference, 1);
						reference2 = ref Unsafe.Add(ref reference2, 1);
						continue;
					}
					return c - c2;
				}
				return -1;
			}
			char c6 = '\0';
			if (!char.IsHighSurrogate(c2) || num2 >= lengthB - 1 || !char.IsLowSurrogate(c6 = Unsafe.Add(ref reference2, 1)))
			{
				return 1;
			}
			if (c == c2 && c3 == c6)
			{
				num2 += 2;
				reference = ref Unsafe.Add(ref reference, 2);
				reference2 = ref Unsafe.Add(ref reference2, 2);
				continue;
			}
			uint num3 = CharUnicodeInfo.ToUpper(UnicodeUtility.GetScalarFromUtf16SurrogatePair(c, c3));
			uint num4 = CharUnicodeInfo.ToUpper(UnicodeUtility.GetScalarFromUtf16SurrogatePair(c2, c6));
			if (num3 == num4)
			{
				num2 += 2;
				reference = ref Unsafe.Add(ref reference, 2);
				reference2 = ref Unsafe.Add(ref reference2, 2);
				continue;
			}
			return (int)(num3 - num4);
		}
		return lengthA - lengthB;
	}

	internal unsafe static int IndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
	{
		fixed (char* ptr = &MemoryMarshal.GetReference(source))
		{
			fixed (char* ptr3 = &MemoryMarshal.GetReference(value))
			{
				char* ptr2 = ptr + (source.Length - value.Length);
				char* ptr4 = ptr3 + value.Length - 1;
				for (char* ptr5 = ptr; ptr5 <= ptr2; ptr5++)
				{
					char* ptr6 = ptr3;
					char* ptr7 = ptr5;
					while (ptr6 <= ptr4)
					{
						if (!char.IsHighSurrogate(*ptr6) || ptr6 == ptr4)
						{
							if (*ptr6 != *ptr7 && ToUpper(*ptr6) != ToUpper(*ptr7))
							{
								break;
							}
							ptr6++;
							ptr7++;
						}
						else if (char.IsHighSurrogate(*ptr7) && char.IsLowSurrogate(ptr7[1]) && char.IsLowSurrogate(ptr6[1]))
						{
							if (!SurrogateCasing.Equal(*ptr7, ptr7[1], *ptr6, ptr6[1]))
							{
								break;
							}
							ptr7 += 2;
							ptr6 += 2;
						}
						else
						{
							if (*ptr6 != *ptr7)
							{
								break;
							}
							ptr7++;
							ptr6++;
						}
					}
					if (ptr6 > ptr4)
					{
						return (int)(ptr5 - ptr);
					}
				}
				return -1;
			}
		}
	}

	internal unsafe static int LastIndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
	{
		fixed (char* ptr3 = &MemoryMarshal.GetReference(source))
		{
			fixed (char* ptr = &MemoryMarshal.GetReference(value))
			{
				char* ptr2 = ptr + value.Length - 1;
				for (char* ptr4 = ptr3 + (source.Length - value.Length); ptr4 >= ptr3; ptr4--)
				{
					char* ptr5 = ptr;
					char* ptr6 = ptr4;
					while (ptr5 <= ptr2)
					{
						if (!char.IsHighSurrogate(*ptr5) || ptr5 == ptr2)
						{
							if (*ptr5 != *ptr6 && ToUpper(*ptr5) != ToUpper(*ptr6))
							{
								break;
							}
							ptr5++;
							ptr6++;
						}
						else if (char.IsHighSurrogate(*ptr6) && char.IsLowSurrogate(ptr6[1]) && char.IsLowSurrogate(ptr5[1]))
						{
							if (!SurrogateCasing.Equal(*ptr6, ptr6[1], *ptr5, ptr5[1]))
							{
								break;
							}
							ptr6 += 2;
							ptr5 += 2;
						}
						else
						{
							if (*ptr5 != *ptr6)
							{
								break;
							}
							ptr6++;
							ptr5++;
						}
					}
					if (ptr5 > ptr2)
					{
						return (int)(ptr4 - ptr3);
					}
				}
				return -1;
			}
		}
	}

	private static ushort[][] InitCasingTable()
	{
		ushort[][] array = new ushort[s_casingTableInit.Length * 8][];
		for (int i = 0; i < s_casingTableInit.Length * 8; i++)
		{
			byte b = (byte)(s_casingTableInit[i / 8] >> 7 - i % 8);
			if ((b & 1) == 1)
			{
				array[i] = s_noCasingPage;
			}
		}
		array[0] = s_basicLatin;
		return array;
	}

	private unsafe static ushort[] InitOrdinalCasingPage(int pageNumber)
	{
		ushort[] array = new ushort[256];
		fixed (ushort* ptr = array)
		{
			char* pTarget = (char*)ptr;
			Interop.Globalization.InitOrdinalCasingPage(pageNumber, pTarget);
		}
		Volatile.Write(ref s_casingTable[pageNumber], array);
		return array;
	}
}
