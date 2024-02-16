using System.Formats.Asn1;

namespace System.Security.Cryptography;

internal static class KeyBlobHelpers
{
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
