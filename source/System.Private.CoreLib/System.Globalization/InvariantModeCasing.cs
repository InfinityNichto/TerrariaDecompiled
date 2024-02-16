using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Globalization;

internal static class InvariantModeCasing
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static char ToLower(char c)
	{
		return CharUnicodeInfo.ToLower(c);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static char ToUpper(char c)
	{
		return CharUnicodeInfo.ToUpper(c);
	}

	internal static string ToLower(string s)
	{
		if (s.Length == 0)
		{
			return string.Empty;
		}
		ReadOnlySpan<char> readOnlySpan = s;
		int num = 0;
		while (num < s.Length)
		{
			if (char.IsHighSurrogate(readOnlySpan[num]) && num < s.Length - 1 && char.IsLowSurrogate(readOnlySpan[num + 1]))
			{
				SurrogateCasing.ToLower(readOnlySpan[num], readOnlySpan[num + 1], out var hr, out var lr);
				if (readOnlySpan[num] != hr || readOnlySpan[num + 1] != lr)
				{
					break;
				}
				num += 2;
			}
			else
			{
				if (ToLower(readOnlySpan[num]) != readOnlySpan[num])
				{
					break;
				}
				num++;
			}
		}
		if (num >= s.Length)
		{
			return s;
		}
		return string.Create(s.Length, (s, num), delegate(Span<char> destination, (string s, int i) state)
		{
			ReadOnlySpan<char> readOnlySpan2 = state.s;
			readOnlySpan2.Slice(0, state.i).CopyTo(destination);
			ToLower(readOnlySpan2.Slice(state.i), destination.Slice(state.i));
		});
	}

	internal static string ToUpper(string s)
	{
		if (s.Length == 0)
		{
			return string.Empty;
		}
		ReadOnlySpan<char> readOnlySpan = s;
		int num = 0;
		while (num < s.Length)
		{
			if (char.IsHighSurrogate(readOnlySpan[num]) && num < s.Length - 1 && char.IsLowSurrogate(readOnlySpan[num + 1]))
			{
				SurrogateCasing.ToUpper(readOnlySpan[num], readOnlySpan[num + 1], out var hr, out var lr);
				if (readOnlySpan[num] != hr || readOnlySpan[num + 1] != lr)
				{
					break;
				}
				num += 2;
			}
			else
			{
				if (ToUpper(readOnlySpan[num]) != readOnlySpan[num])
				{
					break;
				}
				num++;
			}
		}
		if (num >= s.Length)
		{
			return s;
		}
		return string.Create(s.Length, (s, num), delegate(Span<char> destination, (string s, int i) state)
		{
			ReadOnlySpan<char> readOnlySpan2 = state.s;
			readOnlySpan2.Slice(0, state.i).CopyTo(destination);
			ToUpper(readOnlySpan2.Slice(state.i), destination.Slice(state.i));
		});
	}

	internal static void ToUpper(ReadOnlySpan<char> source, Span<char> destination)
	{
		for (int i = 0; i < source.Length; i++)
		{
			char c = source[i];
			if (char.IsHighSurrogate(c) && i < source.Length - 1)
			{
				char c2 = source[i + 1];
				if (char.IsLowSurrogate(c2))
				{
					SurrogateCasing.ToUpper(c, c2, out var hr, out var lr);
					destination[i] = hr;
					destination[i + 1] = lr;
					i++;
					continue;
				}
			}
			destination[i] = ToUpper(c);
		}
	}

	internal static void ToLower(ReadOnlySpan<char> source, Span<char> destination)
	{
		for (int i = 0; i < source.Length; i++)
		{
			char c = source[i];
			if (char.IsHighSurrogate(c) && i < source.Length - 1)
			{
				char c2 = source[i + 1];
				if (char.IsLowSurrogate(c2))
				{
					SurrogateCasing.ToLower(c, c2, out var hr, out var lr);
					destination[i] = hr;
					destination[i + 1] = lr;
					i++;
					continue;
				}
			}
			destination[i] = ToLower(c);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static (uint, int) GetScalar(ref char source, int index, int length)
	{
		char c = source;
		if (!char.IsHighSurrogate(c) || index >= length - 1)
		{
			return (c, 1);
		}
		char c2 = Unsafe.Add(ref source, 1);
		if (!char.IsLowSurrogate(c2))
		{
			return (c, 1);
		}
		return (UnicodeUtility.GetScalarFromUtf16SurrogatePair(c, c2), 2);
	}

	internal static int CompareStringIgnoreCase(ref char strA, int lengthA, ref char strB, int lengthB)
	{
		int num = Math.Min(lengthA, lengthB);
		ref char source = ref strA;
		ref char source2 = ref strB;
		int num2 = 0;
		while (num2 < num)
		{
			var (num3, num4) = GetScalar(ref source, num2, lengthA);
			var (num5, elementOffset) = GetScalar(ref source2, num2, lengthB);
			if (num3 == num5)
			{
				num2 += num4;
				source = ref Unsafe.Add(ref source, num4);
				source2 = ref Unsafe.Add(ref source2, elementOffset);
				continue;
			}
			uint num6 = CharUnicodeInfo.ToUpper(num3);
			uint num7 = CharUnicodeInfo.ToUpper(num5);
			if (num6 == num7)
			{
				num2 += num4;
				source = ref Unsafe.Add(ref source, num4);
				source2 = ref Unsafe.Add(ref source2, elementOffset);
				continue;
			}
			return (int)(num3 - num5);
		}
		return lengthA - lengthB;
	}

	internal unsafe static int IndexOfIgnoreCase(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
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

	internal unsafe static int LastIndexOfIgnoreCase(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
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
}
