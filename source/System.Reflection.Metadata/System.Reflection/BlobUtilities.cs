using System.Collections.Immutable;
using System.Reflection.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection;

internal static class BlobUtilities
{
	public const int SizeOfSerializedDecimal = 13;

	public const int SizeOfGuid = 16;

	public unsafe static byte[] ReadBytes(byte* buffer, int byteCount)
	{
		if (byteCount == 0)
		{
			return EmptyArray<byte>.Instance;
		}
		byte[] array = new byte[byteCount];
		Marshal.Copy((IntPtr)buffer, array, 0, byteCount);
		return array;
	}

	public unsafe static ImmutableArray<byte> ReadImmutableBytes(byte* buffer, int byteCount)
	{
		byte[] array = ReadBytes(buffer, byteCount);
		return ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref array);
	}

	public unsafe static void WriteBytes(this byte[] buffer, int start, byte value, int byteCount)
	{
		fixed (byte* ptr = &buffer[0])
		{
			byte* ptr2 = ptr + start;
			for (int i = 0; i < byteCount; i++)
			{
				ptr2[i] = value;
			}
		}
	}

	public unsafe static void WriteDouble(this byte[] buffer, int start, double value)
	{
		buffer.WriteUInt64(start, *(ulong*)(&value));
	}

	public unsafe static void WriteSingle(this byte[] buffer, int start, float value)
	{
		buffer.WriteUInt32(start, *(uint*)(&value));
	}

	public static void WriteByte(this byte[] buffer, int start, byte value)
	{
		buffer[start] = value;
	}

	public unsafe static void WriteUInt16(this byte[] buffer, int start, ushort value)
	{
		fixed (byte* ptr = &buffer[start])
		{
			*ptr = (byte)value;
			ptr[1] = (byte)(value >> 8);
		}
	}

	public unsafe static void WriteUInt16BE(this byte[] buffer, int start, ushort value)
	{
		fixed (byte* ptr = &buffer[start])
		{
			*ptr = (byte)(value >> 8);
			ptr[1] = (byte)value;
		}
	}

	public unsafe static void WriteUInt32BE(this byte[] buffer, int start, uint value)
	{
		fixed (byte* ptr = &buffer[start])
		{
			*ptr = (byte)(value >> 24);
			ptr[1] = (byte)(value >> 16);
			ptr[2] = (byte)(value >> 8);
			ptr[3] = (byte)value;
		}
	}

	public unsafe static void WriteUInt32(this byte[] buffer, int start, uint value)
	{
		fixed (byte* ptr = &buffer[start])
		{
			*ptr = (byte)value;
			ptr[1] = (byte)(value >> 8);
			ptr[2] = (byte)(value >> 16);
			ptr[3] = (byte)(value >> 24);
		}
	}

	public static void WriteUInt64(this byte[] buffer, int start, ulong value)
	{
		buffer.WriteUInt32(start, (uint)value);
		buffer.WriteUInt32(start + 4, (uint)(value >> 32));
	}

	public static void WriteDecimal(this byte[] buffer, int start, decimal value)
	{
		value.GetBits(out var isNegative, out var scale, out var low, out var mid, out var high);
		buffer.WriteByte(start, (byte)(scale | (isNegative ? 128u : 0u)));
		buffer.WriteUInt32(start + 1, low);
		buffer.WriteUInt32(start + 5, mid);
		buffer.WriteUInt32(start + 9, high);
	}

	public unsafe static void WriteGuid(this byte[] buffer, int start, Guid value)
	{
		fixed (byte* ptr2 = &buffer[start])
		{
			byte* ptr = (byte*)(&value);
			uint num = *(uint*)ptr;
			*ptr2 = (byte)num;
			ptr2[1] = (byte)(num >> 8);
			ptr2[2] = (byte)(num >> 16);
			ptr2[3] = (byte)(num >> 24);
			ushort num2 = *(ushort*)(ptr + 4);
			ptr2[4] = (byte)num2;
			ptr2[5] = (byte)(num2 >> 8);
			ushort num3 = *(ushort*)(ptr + 6);
			ptr2[6] = (byte)num3;
			ptr2[7] = (byte)(num3 >> 8);
			ptr2[8] = ptr[8];
			ptr2[9] = ptr[9];
			ptr2[10] = ptr[10];
			ptr2[11] = ptr[11];
			ptr2[12] = ptr[12];
			ptr2[13] = ptr[13];
			ptr2[14] = ptr[14];
			ptr2[15] = ptr[15];
		}
	}

	public unsafe static void WriteUTF8(this byte[] buffer, int start, char* charPtr, int charCount, int byteCount, bool allowUnpairedSurrogates)
	{
		char* ptr = charPtr + charCount;
		fixed (byte* ptr2 = &buffer[0])
		{
			byte* ptr3 = ptr2 + start;
			if (byteCount == charCount)
			{
				while (charPtr < ptr)
				{
					*(ptr3++) = (byte)(*(charPtr++));
				}
				return;
			}
			while (charPtr < ptr)
			{
				char c = *(charPtr++);
				if (c < '\u0080')
				{
					*(ptr3++) = (byte)c;
					continue;
				}
				if (c < 'ࠀ')
				{
					*ptr3 = (byte)(((uint)((int)c >> 6) & 0x1Fu) | 0xC0u);
					ptr3[1] = (byte)((c & 0x3Fu) | 0x80u);
					ptr3 += 2;
					continue;
				}
				if (IsSurrogateChar(c))
				{
					if (IsHighSurrogateChar(c) && charPtr < ptr && IsLowSurrogateChar(*charPtr))
					{
						int num = c;
						int num2 = *(charPtr++);
						int num3 = (num - 55296 << 10) + num2 - 56320 + 65536;
						*ptr3 = (byte)(((uint)(num3 >> 18) & 7u) | 0xF0u);
						ptr3[1] = (byte)(((uint)(num3 >> 12) & 0x3Fu) | 0x80u);
						ptr3[2] = (byte)(((uint)(num3 >> 6) & 0x3Fu) | 0x80u);
						ptr3[3] = (byte)(((uint)num3 & 0x3Fu) | 0x80u);
						ptr3 += 4;
						continue;
					}
					if (!allowUnpairedSurrogates)
					{
						c = '\ufffd';
					}
				}
				*ptr3 = (byte)(((uint)((int)c >> 12) & 0xFu) | 0xE0u);
				ptr3[1] = (byte)(((uint)((int)c >> 6) & 0x3Fu) | 0x80u);
				ptr3[2] = (byte)((c & 0x3Fu) | 0x80u);
				ptr3 += 3;
			}
		}
	}

	internal unsafe static int GetUTF8ByteCount(string str)
	{
		fixed (char* str2 = str)
		{
			return GetUTF8ByteCount(str2, str.Length);
		}
	}

	internal unsafe static int GetUTF8ByteCount(char* str, int charCount)
	{
		char* remainder;
		return GetUTF8ByteCount(str, charCount, int.MaxValue, out remainder);
	}

	internal unsafe static int GetUTF8ByteCount(char* str, int charCount, int byteLimit, out char* remainder)
	{
		char* ptr = str + charCount;
		char* ptr2 = str;
		int num = 0;
		while (ptr2 < ptr)
		{
			char c = *(ptr2++);
			int num2;
			if (c < '\u0080')
			{
				num2 = 1;
			}
			else if (c < 'ࠀ')
			{
				num2 = 2;
			}
			else if (IsHighSurrogateChar(c) && ptr2 < ptr && IsLowSurrogateChar(*ptr2))
			{
				num2 = 4;
				ptr2++;
			}
			else
			{
				num2 = 3;
			}
			if (num + num2 > byteLimit)
			{
				ptr2 -= ((num2 < 4) ? 1 : 2);
				break;
			}
			num += num2;
		}
		remainder = ptr2;
		return num;
	}

	internal static bool IsSurrogateChar(int c)
	{
		return (uint)(c - 55296) <= 2047u;
	}

	internal static bool IsHighSurrogateChar(int c)
	{
		return (uint)(c - 55296) <= 1023u;
	}

	internal static bool IsLowSurrogateChar(int c)
	{
		return (uint)(c - 56320) <= 1023u;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ValidateRange(int bufferLength, int start, int byteCount, string byteCountParameterName)
	{
		if (start < 0 || start > bufferLength)
		{
			Throw.ArgumentOutOfRange("start");
		}
		if (byteCount < 0 || byteCount > bufferLength - start)
		{
			Throw.ArgumentOutOfRange(byteCountParameterName);
		}
	}

	internal static int GetUserStringByteLength(int characterCount)
	{
		return characterCount * 2 + 1;
	}

	internal static byte GetUserStringTrailingByte(string str)
	{
		foreach (char c in str)
		{
			if (c >= '\u007f')
			{
				return 1;
			}
			switch (c)
			{
			case '\u0001':
			case '\u0002':
			case '\u0003':
			case '\u0004':
			case '\u0005':
			case '\u0006':
			case '\a':
			case '\b':
			case '\u000e':
			case '\u000f':
			case '\u0010':
			case '\u0011':
			case '\u0012':
			case '\u0013':
			case '\u0014':
			case '\u0015':
			case '\u0016':
			case '\u0017':
			case '\u0018':
			case '\u0019':
			case '\u001a':
			case '\u001b':
			case '\u001c':
			case '\u001d':
			case '\u001e':
			case '\u001f':
			case '\'':
			case '-':
				return 1;
			}
		}
		return 0;
	}
}
