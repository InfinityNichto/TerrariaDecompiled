using System.IO.Compression;
using System.Runtime.InteropServices;

internal static class Interop
{
	internal static class zlib
	{
		[DllImport("System.IO.Compression.Native", EntryPoint = "CompressionNative_DeflateInit2_")]
		internal unsafe static extern ZLibNative.ErrorCode DeflateInit2_(ZLibNative.ZStream* stream, ZLibNative.CompressionLevel level, ZLibNative.CompressionMethod method, int windowBits, int memLevel, ZLibNative.CompressionStrategy strategy);

		[DllImport("System.IO.Compression.Native", EntryPoint = "CompressionNative_Deflate")]
		internal unsafe static extern ZLibNative.ErrorCode Deflate(ZLibNative.ZStream* stream, ZLibNative.FlushCode flush);

		[DllImport("System.IO.Compression.Native", EntryPoint = "CompressionNative_DeflateEnd")]
		internal unsafe static extern ZLibNative.ErrorCode DeflateEnd(ZLibNative.ZStream* stream);

		[DllImport("System.IO.Compression.Native", EntryPoint = "CompressionNative_InflateInit2_")]
		internal unsafe static extern ZLibNative.ErrorCode InflateInit2_(ZLibNative.ZStream* stream, int windowBits);

		[DllImport("System.IO.Compression.Native", EntryPoint = "CompressionNative_Inflate")]
		internal unsafe static extern ZLibNative.ErrorCode Inflate(ZLibNative.ZStream* stream, ZLibNative.FlushCode flush);

		[DllImport("System.IO.Compression.Native", EntryPoint = "CompressionNative_InflateEnd")]
		internal unsafe static extern ZLibNative.ErrorCode InflateEnd(ZLibNative.ZStream* stream);

		[DllImport("System.IO.Compression.Native", EntryPoint = "CompressionNative_Crc32")]
		internal unsafe static extern uint crc32(uint crc, byte* buffer, int len);
	}
}
