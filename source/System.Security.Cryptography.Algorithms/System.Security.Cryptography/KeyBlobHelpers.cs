using System.Formats.Asn1;
using System.Numerics;

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

	internal static byte[] ExportKeyParameter(this BigInteger value, int length)
	{
		byte[] array = new byte[length];
		if (value.TryWriteBytes(array, out var bytesWritten, isUnsigned: true, isBigEndian: true))
		{
			if (bytesWritten < length)
			{
				Buffer.BlockCopy(array, 0, array, length - bytesWritten, bytesWritten);
				array.AsSpan(0, length - bytesWritten).Clear();
			}
			return array;
		}
		throw new CryptographicException(System.SR.Cryptography_NotValidPublicOrPrivateKey);
	}

	internal static void WriteKeyParameterInteger(this AsnWriter writer, ReadOnlySpan<byte> integer)
	{
		if (integer[0] == 0)
		{
			int i;
			for (i = 1; i < integer.Length; i++)
			{
				if (integer[i] >= 128)
				{
					i--;
					break;
				}
				if (integer[i] != 0)
				{
					break;
				}
			}
			if (i == integer.Length)
			{
				i--;
			}
			integer = integer.Slice(i);
		}
		writer.WriteIntegerUnsigned(integer);
	}
}
