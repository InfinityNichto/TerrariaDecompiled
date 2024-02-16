using System;
using System.Runtime.InteropServices;

namespace Internal.Cryptography.Pal.Native;

internal struct CRYPT_BIT_BLOB
{
	public int cbData;

	public unsafe byte* pbData;

	public int cUnusedBits;

	public unsafe byte[] ToByteArray()
	{
		if (cbData == 0)
		{
			return Array.Empty<byte>();
		}
		byte[] array = new byte[cbData];
		Marshal.Copy((IntPtr)pbData, array, 0, cbData);
		return array;
	}
}
