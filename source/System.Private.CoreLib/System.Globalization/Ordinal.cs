using System.Text.Unicode;
using Internal.Runtime.CompilerServices;

namespace System.Globalization;

internal static class Ordinal
{
	internal static int CompareStringIgnoreCase(ref char strA, int lengthA, ref char strB, int lengthB)
	{
		int num = Math.Min(lengthA, lengthB);
		int num2 = num;
		ref char reference = ref strA;
		ref char reference2 = ref strB;
		char c = '\u007f';
		while (num != 0 && reference <= c && reference2 <= c)
		{
			if (reference == reference2 || ((reference | 0x20) == (reference2 | 0x20) && (uint)((reference | 0x20) - 97) <= 25u))
			{
				num--;
				reference = ref Unsafe.Add(ref reference, 1);
				reference2 = ref Unsafe.Add(ref reference2, 1);
				continue;
			}
			int num3 = reference;
			int num4 = reference2;
			if ((uint)(reference - 97) <= 25u)
			{
				num3 -= 32;
			}
			if ((uint)(reference2 - 97) <= 25u)
			{
				num4 -= 32;
			}
			return num3 - num4;
		}
		if (num == 0)
		{
			return lengthA - lengthB;
		}
		num2 -= num;
		return CompareStringIgnoreCaseNonAscii(ref reference, lengthA - num2, ref reference2, lengthB - num2);
	}

	internal static int CompareStringIgnoreCaseNonAscii(ref char strA, int lengthA, ref char strB, int lengthB)
	{
		if (GlobalizationMode.Invariant)
		{
			return InvariantModeCasing.CompareStringIgnoreCase(ref strA, lengthA, ref strB, lengthB);
		}
		if (GlobalizationMode.UseNls)
		{
			return CompareInfo.NlsCompareStringOrdinalIgnoreCase(ref strA, lengthA, ref strB, lengthB);
		}
		return OrdinalCasing.CompareStringIgnoreCase(ref strA, lengthA, ref strB, lengthB);
	}

	internal static bool EqualsIgnoreCase(ref char charA, ref char charB, int length)
	{
		IntPtr zero = IntPtr.Zero;
		while (true)
		{
			if ((uint)length >= 4u)
			{
				ulong num = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charA, zero)));
				ulong num2 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charB, zero)));
				ulong num3 = num | num2;
				if (!Utf16Utility.AllCharsInUInt32AreAscii((uint)((int)num3 | (int)(num3 >> 32))))
				{
					break;
				}
				if (!Utf16Utility.UInt64OrdinalIgnoreCaseAscii(num, num2))
				{
					return false;
				}
				zero += 8;
				length -= 4;
				continue;
			}
			if ((uint)length >= 2u)
			{
				uint num4 = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charA, zero)));
				uint num5 = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref charB, zero)));
				if (!Utf16Utility.AllCharsInUInt32AreAscii(num4 | num5))
				{
					break;
				}
				if (!Utf16Utility.UInt32OrdinalIgnoreCaseAscii(num4, num5))
				{
					return false;
				}
				zero += 4;
				length -= 2;
			}
			if (length != 0)
			{
				uint num6 = Unsafe.AddByteOffset(ref charA, zero);
				uint num7 = Unsafe.AddByteOffset(ref charB, zero);
				if ((num6 | num7) > 127)
				{
					break;
				}
				if (num6 == num7)
				{
					return true;
				}
				num6 |= 0x20u;
				if (num6 - 97 > 25)
				{
					return false;
				}
				if (num6 != (num7 | 0x20))
				{
					return false;
				}
				return true;
			}
			return true;
		}
		return CompareStringIgnoreCase(ref Unsafe.AddByteOffset(ref charA, zero), length, ref Unsafe.AddByteOffset(ref charB, zero), length) == 0;
	}

	internal static int IndexOf(string source, string value, int startIndex, int count, bool ignoreCase)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		if (!source.TryGetSpan(startIndex, count, out var slice))
		{
			if ((uint)startIndex > (uint)source.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			else
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
			}
		}
		int num = (ignoreCase ? IndexOfOrdinalIgnoreCase(slice, value) : slice.IndexOf(value));
		if (num < 0)
		{
			return num;
		}
		return num + startIndex;
	}

	internal static int IndexOfOrdinalIgnoreCase(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
	{
		if (value.Length == 0)
		{
			return 0;
		}
		if (value.Length > source.Length)
		{
			return -1;
		}
		if (GlobalizationMode.Invariant)
		{
			return InvariantModeCasing.IndexOfIgnoreCase(source, value);
		}
		if (GlobalizationMode.UseNls)
		{
			return CompareInfo.NlsIndexOfOrdinalCore(source, value, ignoreCase: true, fromBeginning: true);
		}
		return OrdinalCasing.IndexOf(source, value);
	}

	internal static int LastIndexOfOrdinalIgnoreCase(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
	{
		if (value.Length == 0)
		{
			return source.Length;
		}
		if (value.Length > source.Length)
		{
			return -1;
		}
		if (GlobalizationMode.Invariant)
		{
			return InvariantModeCasing.LastIndexOfIgnoreCase(source, value);
		}
		if (GlobalizationMode.UseNls)
		{
			return CompareInfo.NlsIndexOfOrdinalCore(source, value, ignoreCase: true, fromBeginning: false);
		}
		return OrdinalCasing.LastIndexOf(source, value);
	}

	internal static int ToUpperOrdinal(ReadOnlySpan<char> source, Span<char> destination)
	{
		if (source.Overlaps(destination))
		{
			throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);
		}
		if (destination.Length < source.Length)
		{
			return -1;
		}
		if (GlobalizationMode.Invariant)
		{
			InvariantModeCasing.ToUpper(source, destination);
			return source.Length;
		}
		if (GlobalizationMode.UseNls)
		{
			TextInfo.Invariant.ChangeCaseToUpper(source, destination);
			return source.Length;
		}
		OrdinalCasing.ToUpperOrdinal(source, destination);
		return source.Length;
	}
}
