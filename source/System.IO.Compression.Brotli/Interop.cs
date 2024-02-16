using System;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static class Interop
{
	internal static class Brotli
	{
		[DllImport("System.IO.Compression.Native")]
		internal static extern SafeBrotliDecoderHandle BrotliDecoderCreateInstance(IntPtr allocFunc, IntPtr freeFunc, IntPtr opaque);

		[DllImport("System.IO.Compression.Native")]
		internal unsafe static extern int BrotliDecoderDecompressStream(SafeBrotliDecoderHandle state, ref nuint availableIn, byte** nextIn, ref nuint availableOut, byte** nextOut, out nuint totalOut);

		[DllImport("System.IO.Compression.Native")]
		internal unsafe static extern BOOL BrotliDecoderDecompress(nuint availableInput, byte* inBytes, nuint* availableOutput, byte* outBytes);

		[DllImport("System.IO.Compression.Native")]
		internal static extern void BrotliDecoderDestroyInstance(IntPtr state);

		[DllImport("System.IO.Compression.Native")]
		internal static extern BOOL BrotliDecoderIsFinished(SafeBrotliDecoderHandle state);

		[DllImport("System.IO.Compression.Native")]
		internal static extern SafeBrotliEncoderHandle BrotliEncoderCreateInstance(IntPtr allocFunc, IntPtr freeFunc, IntPtr opaque);

		[DllImport("System.IO.Compression.Native")]
		internal static extern BOOL BrotliEncoderSetParameter(SafeBrotliEncoderHandle state, BrotliEncoderParameter parameter, uint value);

		[DllImport("System.IO.Compression.Native")]
		internal unsafe static extern BOOL BrotliEncoderCompressStream(SafeBrotliEncoderHandle state, BrotliEncoderOperation op, ref nuint availableIn, byte** nextIn, ref nuint availableOut, byte** nextOut, out nuint totalOut);

		[DllImport("System.IO.Compression.Native")]
		internal static extern BOOL BrotliEncoderHasMoreOutput(SafeBrotliEncoderHandle state);

		[DllImport("System.IO.Compression.Native")]
		internal static extern void BrotliEncoderDestroyInstance(IntPtr state);

		[DllImport("System.IO.Compression.Native")]
		internal unsafe static extern BOOL BrotliEncoderCompress(int quality, int window, int v, nuint availableInput, byte* inBytes, nuint* availableOutput, byte* outBytes);
	}

	internal enum BOOL
	{
		FALSE,
		TRUE
	}
}
