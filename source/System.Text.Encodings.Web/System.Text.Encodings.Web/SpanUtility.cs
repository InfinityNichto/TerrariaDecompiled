using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Encodings.Web;

internal static class SpanUtility
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsValidIndex<T>(ReadOnlySpan<T> span, int index)
	{
		if ((uint)index >= (uint)span.Length)
		{
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsValidIndex<T>(Span<T> span, int index)
	{
		if ((uint)index >= (uint)span.Length)
		{
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteBytes(Span<byte> span, byte a, byte b, byte c, byte d)
	{
		if (span.Length >= 4)
		{
			Unsafe.WriteUnaligned(value: (uint)((!BitConverter.IsLittleEndian) ? ((a << 24) | (b << 16) | (c << 8) | d) : ((d << 24) | (c << 16) | (b << 8) | a)), destination: ref MemoryMarshal.GetReference(span));
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteBytes(Span<byte> span, byte a, byte b, byte c, byte d, byte e)
	{
		if (span.Length >= 5)
		{
			uint value = (uint)((!BitConverter.IsLittleEndian) ? ((a << 24) | (b << 16) | (c << 8) | d) : ((d << 24) | (c << 16) | (b << 8) | a));
			ref byte reference = ref MemoryMarshal.GetReference(span);
			Unsafe.WriteUnaligned(ref reference, value);
			Unsafe.Add(ref reference, 4) = e;
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteBytes(Span<byte> span, byte a, byte b, byte c, byte d, byte e, byte f)
	{
		if (span.Length >= 6)
		{
			uint value;
			uint num;
			if (BitConverter.IsLittleEndian)
			{
				value = (uint)((d << 24) | (c << 16) | (b << 8) | a);
				num = (uint)((f << 8) | e);
			}
			else
			{
				value = (uint)((a << 24) | (b << 16) | (c << 8) | d);
				num = (uint)((e << 8) | f);
			}
			ref byte reference = ref MemoryMarshal.GetReference(span);
			Unsafe.WriteUnaligned(ref reference, value);
			Unsafe.WriteUnaligned(ref Unsafe.Add(ref reference, 4), (ushort)num);
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteChars(Span<char> span, char a, char b, char c, char d)
	{
		if (span.Length >= 4)
		{
			Unsafe.WriteUnaligned(value: (!BitConverter.IsLittleEndian) ? (((ulong)a << 48) | ((ulong)b << 32) | ((ulong)c << 16) | d) : (((ulong)d << 48) | ((ulong)c << 32) | ((ulong)b << 16) | a), destination: ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(span)));
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteChars(Span<char> span, char a, char b, char c, char d, char e)
	{
		if (span.Length >= 5)
		{
			ulong value = ((!BitConverter.IsLittleEndian) ? (((ulong)a << 48) | ((ulong)b << 32) | ((ulong)c << 16) | d) : (((ulong)d << 48) | ((ulong)c << 32) | ((ulong)b << 16) | a));
			ref char reference = ref MemoryMarshal.GetReference(span);
			Unsafe.WriteUnaligned(ref Unsafe.As<char, byte>(ref reference), value);
			Unsafe.Add(ref reference, 4) = e;
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteChars(Span<char> span, char a, char b, char c, char d, char e, char f)
	{
		if (span.Length >= 6)
		{
			ulong value;
			uint value2;
			if (BitConverter.IsLittleEndian)
			{
				value = ((ulong)d << 48) | ((ulong)c << 32) | ((ulong)b << 16) | a;
				value2 = ((uint)f << 16) | e;
			}
			else
			{
				value = ((ulong)a << 48) | ((ulong)b << 32) | ((ulong)c << 16) | d;
				value2 = ((uint)e << 16) | f;
			}
			ref byte reference = ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(span));
			Unsafe.WriteUnaligned(ref reference, value);
			Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref reference, (IntPtr)8), value2);
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteUInt64LittleEndian(Span<byte> span, int offset, ulong value)
	{
		if (AreValidIndexAndLength(span.Length, offset, 8))
		{
			if (!BitConverter.IsLittleEndian)
			{
				value = BinaryPrimitives.ReverseEndianness(value);
			}
			Unsafe.WriteUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)offset), value);
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool AreValidIndexAndLength(int spanRealLength, int requestedOffset, int requestedLength)
	{
		if (IntPtr.Size == 4)
		{
			if ((uint)requestedOffset > (uint)spanRealLength)
			{
				return false;
			}
			if ((uint)requestedLength > (uint)(spanRealLength - requestedOffset))
			{
				return false;
			}
		}
		else if ((ulong)(uint)spanRealLength < (ulong)((long)(uint)requestedOffset + (long)(uint)requestedLength))
		{
			return false;
		}
		return true;
	}
}
