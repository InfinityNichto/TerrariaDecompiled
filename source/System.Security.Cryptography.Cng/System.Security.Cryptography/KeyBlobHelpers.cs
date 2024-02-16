namespace System.Security.Cryptography;

internal static class KeyBlobHelpers
{
	internal static byte[] ToUnsignedIntegerBytes(this ReadOnlyMemory<byte> memory, int length)
	{
		if (memory.Length == length)
		{
			return memory.ToArray();
		}
		ReadOnlySpan<byte> span = memory.Span;
		if (memory.Length == length + 1 && span[0] == 0)
		{
			return span.Slice(1).ToArray();
		}
		if (span.Length > length)
		{
			throw new CryptographicException(System.SR.Cryptography_Der_Invalid_Encoding);
		}
		byte[] array = new byte[length];
		span.CopyTo(array.AsSpan(length - span.Length));
		return array;
	}

	internal static byte[] ToUnsignedIntegerBytes(this ReadOnlyMemory<byte> memory)
	{
		ReadOnlySpan<byte> span = memory.Span;
		if (span.Length > 1 && span[0] == 0)
		{
			return span.Slice(1).ToArray();
		}
		return span.ToArray();
	}
}
