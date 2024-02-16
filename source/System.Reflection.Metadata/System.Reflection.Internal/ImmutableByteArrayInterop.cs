using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal;

internal static class ImmutableByteArrayInterop
{
	[StructLayout(LayoutKind.Explicit)]
	private struct ByteArrayUnion
	{
		[FieldOffset(0)]
		internal byte[] UnderlyingArray;

		[FieldOffset(0)]
		internal ImmutableArray<byte> ImmutableArray;
	}

	internal static ImmutableArray<byte> DangerousCreateFromUnderlyingArray(ref byte[]? array)
	{
		byte[] underlyingArray = array;
		array = null;
		ByteArrayUnion byteArrayUnion = default(ByteArrayUnion);
		byteArrayUnion.UnderlyingArray = underlyingArray;
		return byteArrayUnion.ImmutableArray;
	}

	internal static byte[]? DangerousGetUnderlyingArray(ImmutableArray<byte> array)
	{
		ByteArrayUnion byteArrayUnion = default(ByteArrayUnion);
		byteArrayUnion.ImmutableArray = array;
		return byteArrayUnion.UnderlyingArray;
	}
}
