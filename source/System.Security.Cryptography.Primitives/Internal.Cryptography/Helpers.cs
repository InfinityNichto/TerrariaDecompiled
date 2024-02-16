using System.Diagnostics.CodeAnalysis;

namespace Internal.Cryptography;

internal static class Helpers
{
	[return: NotNullIfNotNull("src")]
	public static byte[] CloneByteArray(this byte[] src)
	{
		if (src == null)
		{
			return null;
		}
		return (byte[])src.Clone();
	}
}
