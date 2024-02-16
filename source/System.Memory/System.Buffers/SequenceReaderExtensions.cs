using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Buffers;

public static class SequenceReaderExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static bool TryRead<T>(this ref SequenceReader<byte> reader, out T value) where T : unmanaged
	{
		ReadOnlySpan<byte> unreadSpan = reader.UnreadSpan;
		if (unreadSpan.Length < sizeof(T))
		{
			return TryReadMultisegment<T>(ref reader, out value);
		}
		value = Internal.Runtime.CompilerServices.Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(unreadSpan));
		reader.Advance(sizeof(T));
		return true;
	}

	private unsafe static bool TryReadMultisegment<T>(ref SequenceReader<byte> reader, out T value) where T : unmanaged
	{
		T val = default(T);
		Span<byte> span = new Span<byte>(&val, sizeof(T));
		if (!reader.TryCopyTo(span))
		{
			value = default(T);
			return false;
		}
		value = Internal.Runtime.CompilerServices.Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
		reader.Advance(sizeof(T));
		return true;
	}

	public static bool TryReadLittleEndian(this ref SequenceReader<byte> reader, out short value)
	{
		if (BitConverter.IsLittleEndian)
		{
			return reader.TryRead<short>(out value);
		}
		return TryReadReverseEndianness(ref reader, out value);
	}

	public static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out short value)
	{
		if (!BitConverter.IsLittleEndian)
		{
			return reader.TryRead<short>(out value);
		}
		return TryReadReverseEndianness(ref reader, out value);
	}

	private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out short value)
	{
		if (reader.TryRead<short>(out value))
		{
			value = BinaryPrimitives.ReverseEndianness(value);
			return true;
		}
		return false;
	}

	public static bool TryReadLittleEndian(this ref SequenceReader<byte> reader, out int value)
	{
		if (BitConverter.IsLittleEndian)
		{
			return reader.TryRead<int>(out value);
		}
		return TryReadReverseEndianness(ref reader, out value);
	}

	public static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out int value)
	{
		if (!BitConverter.IsLittleEndian)
		{
			return reader.TryRead<int>(out value);
		}
		return TryReadReverseEndianness(ref reader, out value);
	}

	private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out int value)
	{
		if (reader.TryRead<int>(out value))
		{
			value = BinaryPrimitives.ReverseEndianness(value);
			return true;
		}
		return false;
	}

	public static bool TryReadLittleEndian(this ref SequenceReader<byte> reader, out long value)
	{
		if (BitConverter.IsLittleEndian)
		{
			return reader.TryRead<long>(out value);
		}
		return TryReadReverseEndianness(ref reader, out value);
	}

	public static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out long value)
	{
		if (!BitConverter.IsLittleEndian)
		{
			return reader.TryRead<long>(out value);
		}
		return TryReadReverseEndianness(ref reader, out value);
	}

	private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out long value)
	{
		if (reader.TryRead<long>(out value))
		{
			value = BinaryPrimitives.ReverseEndianness(value);
			return true;
		}
		return false;
	}
}
