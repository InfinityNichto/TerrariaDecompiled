using System.Runtime.InteropServices;

namespace System.IO.Compression;

internal static class Crc32Helper
{
	public unsafe static uint UpdateCrc32(uint crc32, byte[] buffer, int offset, int length)
	{
		fixed (byte* buffer2 = &buffer[offset])
		{
			return global::Interop.zlib.crc32(crc32, buffer2, length);
		}
	}

	public unsafe static uint UpdateCrc32(uint crc32, ReadOnlySpan<byte> buffer)
	{
		fixed (byte* buffer2 = &MemoryMarshal.GetReference(buffer))
		{
			return global::Interop.zlib.crc32(crc32, buffer2, buffer.Length);
		}
	}
}
