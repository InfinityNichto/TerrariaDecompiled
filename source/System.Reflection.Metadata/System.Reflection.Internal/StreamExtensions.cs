using System.IO;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal;

internal static class StreamExtensions
{
	internal const int StreamCopyBufferSize = 81920;

	internal unsafe static void CopyTo(this Stream source, byte* destination, int size)
	{
		byte[] array = new byte[Math.Min(81920, size)];
		while (size > 0)
		{
			int num = Math.Min(size, array.Length);
			int num2 = source.Read(array, 0, num);
			if (num2 <= 0 || num2 > num)
			{
				throw new IOException(System.SR.UnexpectedStreamEnd);
			}
			Marshal.Copy(array, 0, (IntPtr)destination, num2);
			destination += num2;
			size -= num2;
		}
	}

	internal static int TryReadAll(this Stream stream, byte[] buffer, int offset, int count)
	{
		int i;
		int num;
		for (i = 0; i < count; i += num)
		{
			num = stream.Read(buffer, offset + i, count - i);
			if (num == 0)
			{
				break;
			}
		}
		return i;
	}

	internal static int TryReadAll(this Stream stream, Span<byte> buffer)
	{
		int i;
		int num;
		for (i = 0; i < buffer.Length; i += num)
		{
			num = stream.Read(buffer.Slice(i));
			if (num == 0)
			{
				break;
			}
		}
		return i;
	}

	internal static int GetAndValidateSize(Stream stream, int size, string streamParameterName)
	{
		long num = stream.Length - stream.Position;
		if (size < 0 || size > num)
		{
			throw new ArgumentOutOfRangeException("size");
		}
		if (size != 0)
		{
			return size;
		}
		if (num > int.MaxValue)
		{
			throw new ArgumentException(System.SR.StreamTooLarge, streamParameterName);
		}
		return (int)num;
	}
}
