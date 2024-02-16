using System.Buffers;
using System.Collections.Generic;

namespace System.Text;

public static class EncodingExtensions
{
	public static long GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, IBufferWriter<byte> writer)
	{
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (chars.Length <= 1048576)
		{
			int byteCount = encoding.GetByteCount(chars);
			Span<byte> span = writer.GetSpan(byteCount);
			int bytes = encoding.GetBytes(chars, span);
			writer.Advance(bytes);
			return bytes;
		}
		encoding.GetEncoder().Convert(chars, writer, flush: true, out var bytesUsed, out var _);
		return bytesUsed;
	}

	public static long GetBytes(this Encoding encoding, in ReadOnlySequence<char> chars, IBufferWriter<byte> writer)
	{
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (chars.IsSingleSegment)
		{
			return encoding.GetBytes(chars.FirstSpan, writer);
		}
		encoding.GetEncoder().Convert(in chars, writer, flush: true, out var bytesUsed, out var _);
		return bytesUsed;
	}

	public static int GetBytes(this Encoding encoding, in ReadOnlySequence<char> chars, Span<byte> bytes)
	{
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (chars.IsSingleSegment)
		{
			return encoding.GetBytes(chars.FirstSpan, bytes);
		}
		ReadOnlySequence<char> readOnlySequence = chars;
		int length = bytes.Length;
		Encoder encoder = encoding.GetEncoder();
		bool isSingleSegment;
		do
		{
			readOnlySequence.GetFirstSpan(out var first, out var next);
			isSingleSegment = readOnlySequence.IsSingleSegment;
			int bytes2 = encoder.GetBytes(first, bytes, isSingleSegment);
			bytes = bytes.Slice(bytes2);
			readOnlySequence = readOnlySequence.Slice(next);
		}
		while (!isSingleSegment);
		return length - bytes.Length;
	}

	public static byte[] GetBytes(this Encoding encoding, in ReadOnlySequence<char> chars)
	{
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (chars.IsSingleSegment)
		{
			ReadOnlySpan<char> firstSpan = chars.FirstSpan;
			byte[] array = new byte[encoding.GetByteCount(firstSpan)];
			encoding.GetBytes(firstSpan, array);
			return array;
		}
		Encoder encoder = encoding.GetEncoder();
		List<(byte[], int)> list = new List<(byte[], int)>();
		int num = 0;
		ReadOnlySequence<char> readOnlySequence = chars;
		bool isSingleSegment;
		do
		{
			readOnlySequence.GetFirstSpan(out var first, out var next);
			isSingleSegment = readOnlySequence.IsSingleSegment;
			int byteCount = encoder.GetByteCount(first, isSingleSegment);
			byte[] array2 = ArrayPool<byte>.Shared.Rent(byteCount);
			int bytes = encoder.GetBytes(first, array2, isSingleSegment);
			list.Add((array2, bytes));
			num += bytes;
			if (num < 0)
			{
				num = int.MaxValue;
				break;
			}
			readOnlySequence = readOnlySequence.Slice(next);
		}
		while (!isSingleSegment);
		byte[] array3 = new byte[num];
		Span<byte> destination = array3;
		foreach (var (array4, num2) in list)
		{
			array4.AsSpan(0, num2).CopyTo(destination);
			ArrayPool<byte>.Shared.Return(array4);
			destination = destination.Slice(num2);
		}
		return array3;
	}

	public static long GetChars(this Encoding encoding, ReadOnlySpan<byte> bytes, IBufferWriter<char> writer)
	{
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (bytes.Length <= 1048576)
		{
			int charCount = encoding.GetCharCount(bytes);
			Span<char> span = writer.GetSpan(charCount);
			int chars = encoding.GetChars(bytes, span);
			writer.Advance(chars);
			return chars;
		}
		encoding.GetDecoder().Convert(bytes, writer, flush: true, out var charsUsed, out var _);
		return charsUsed;
	}

	public static long GetChars(this Encoding encoding, in ReadOnlySequence<byte> bytes, IBufferWriter<char> writer)
	{
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (bytes.IsSingleSegment)
		{
			return encoding.GetChars(bytes.FirstSpan, writer);
		}
		encoding.GetDecoder().Convert(in bytes, writer, flush: true, out var charsUsed, out var _);
		return charsUsed;
	}

	public static int GetChars(this Encoding encoding, in ReadOnlySequence<byte> bytes, Span<char> chars)
	{
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (bytes.IsSingleSegment)
		{
			return encoding.GetChars(bytes.FirstSpan, chars);
		}
		ReadOnlySequence<byte> readOnlySequence = bytes;
		int length = chars.Length;
		Decoder decoder = encoding.GetDecoder();
		bool isSingleSegment;
		do
		{
			readOnlySequence.GetFirstSpan(out var first, out var next);
			isSingleSegment = readOnlySequence.IsSingleSegment;
			int chars2 = decoder.GetChars(first, chars, isSingleSegment);
			chars = chars.Slice(chars2);
			readOnlySequence = readOnlySequence.Slice(next);
		}
		while (!isSingleSegment);
		return length - chars.Length;
	}

	public static string GetString(this Encoding encoding, in ReadOnlySequence<byte> bytes)
	{
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (bytes.IsSingleSegment)
		{
			return encoding.GetString(bytes.FirstSpan);
		}
		Decoder decoder = encoding.GetDecoder();
		List<(char[], int)> list = new List<(char[], int)>();
		int num = 0;
		ReadOnlySequence<byte> readOnlySequence = bytes;
		bool isSingleSegment;
		do
		{
			readOnlySequence.GetFirstSpan(out var first, out var next);
			isSingleSegment = readOnlySequence.IsSingleSegment;
			int charCount = decoder.GetCharCount(first, isSingleSegment);
			char[] array = ArrayPool<char>.Shared.Rent(charCount);
			int chars = decoder.GetChars(first, array, isSingleSegment);
			list.Add((array, chars));
			num += chars;
			if (num < 0)
			{
				num = int.MaxValue;
				break;
			}
			readOnlySequence = readOnlySequence.Slice(next);
		}
		while (!isSingleSegment);
		return string.Create(num, list, delegate(Span<char> span, List<(char[], int)> listOfSegments)
		{
			foreach (var (array2, num2) in listOfSegments)
			{
				array2.AsSpan(0, num2).CopyTo(span);
				ArrayPool<char>.Shared.Return(array2);
				span = span.Slice(num2);
			}
		});
	}

	public static void Convert(this Encoder encoder, ReadOnlySpan<char> chars, IBufferWriter<byte> writer, bool flush, out long bytesUsed, out bool completed)
	{
		if (encoder == null)
		{
			throw new ArgumentNullException("encoder");
		}
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		long num = 0L;
		do
		{
			int sizeHint = ((chars.Length <= 1048576) ? encoder.GetByteCount(chars, flush) : encoder.GetByteCount(chars.Slice(0, 1048576), flush: false));
			Span<byte> span = writer.GetSpan(sizeHint);
			encoder.Convert(chars, span, flush, out var charsUsed, out var bytesUsed2, out completed);
			chars = chars.Slice(charsUsed);
			writer.Advance(bytesUsed2);
			num += bytesUsed2;
		}
		while (!chars.IsEmpty);
		bytesUsed = num;
	}

	public static void Convert(this Encoder encoder, in ReadOnlySequence<char> chars, IBufferWriter<byte> writer, bool flush, out long bytesUsed, out bool completed)
	{
		ReadOnlySequence<char> readOnlySequence = chars;
		long num = 0L;
		bool isSingleSegment;
		do
		{
			readOnlySequence.GetFirstSpan(out var first, out var next);
			isSingleSegment = readOnlySequence.IsSingleSegment;
			encoder.Convert(first, writer, flush && isSingleSegment, out var bytesUsed2, out completed);
			num += bytesUsed2;
			readOnlySequence = readOnlySequence.Slice(next);
		}
		while (!isSingleSegment);
		bytesUsed = num;
	}

	public static void Convert(this Decoder decoder, ReadOnlySpan<byte> bytes, IBufferWriter<char> writer, bool flush, out long charsUsed, out bool completed)
	{
		if (decoder == null)
		{
			throw new ArgumentNullException("decoder");
		}
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		long num = 0L;
		do
		{
			int sizeHint = ((bytes.Length <= 1048576) ? decoder.GetCharCount(bytes, flush) : decoder.GetCharCount(bytes.Slice(0, 1048576), flush: false));
			Span<char> span = writer.GetSpan(sizeHint);
			decoder.Convert(bytes, span, flush, out var bytesUsed, out var charsUsed2, out completed);
			bytes = bytes.Slice(bytesUsed);
			writer.Advance(charsUsed2);
			num += charsUsed2;
		}
		while (!bytes.IsEmpty);
		charsUsed = num;
	}

	public static void Convert(this Decoder decoder, in ReadOnlySequence<byte> bytes, IBufferWriter<char> writer, bool flush, out long charsUsed, out bool completed)
	{
		ReadOnlySequence<byte> readOnlySequence = bytes;
		long num = 0L;
		bool isSingleSegment;
		do
		{
			readOnlySequence.GetFirstSpan(out var first, out var next);
			isSingleSegment = readOnlySequence.IsSingleSegment;
			decoder.Convert(first, writer, flush && isSingleSegment, out var charsUsed2, out completed);
			num += charsUsed2;
			readOnlySequence = readOnlySequence.Slice(next);
		}
		while (!isSingleSegment);
		charsUsed = num;
	}
}
