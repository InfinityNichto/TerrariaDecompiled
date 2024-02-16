using System.Buffers;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal;

internal static class EncodingHelper
{
	internal static bool TestOnly_LightUpEnabled
	{
		get
		{
			return true;
		}
		set
		{
		}
	}

	public unsafe static string DecodeUtf8(byte* bytes, int byteCount, byte[]? prefix, MetadataStringDecoder utf8Decoder)
	{
		if (prefix != null)
		{
			return DecodeUtf8Prefixed(bytes, byteCount, prefix, utf8Decoder);
		}
		if (byteCount == 0)
		{
			return string.Empty;
		}
		return utf8Decoder.GetString(bytes, byteCount);
	}

	private unsafe static string DecodeUtf8Prefixed(byte* bytes, int byteCount, byte[] prefix, MetadataStringDecoder utf8Decoder)
	{
		int num = byteCount + prefix.Length;
		if (num == 0)
		{
			return string.Empty;
		}
		byte[] array = ArrayPool<byte>.Shared.Rent(num);
		prefix.CopyTo(array, 0);
		Marshal.Copy((IntPtr)bytes, array, prefix.Length, byteCount);
		string @string;
		fixed (byte* bytes2 = &array[0])
		{
			@string = utf8Decoder.GetString(bytes2, num);
		}
		ArrayPool<byte>.Shared.Return(array);
		return @string;
	}
}
