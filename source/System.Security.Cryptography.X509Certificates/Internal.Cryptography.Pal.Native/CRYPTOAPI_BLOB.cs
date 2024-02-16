using System;
using System.Runtime.InteropServices;

namespace Internal.Cryptography.Pal.Native;

internal struct CRYPTOAPI_BLOB
{
	public int cbData;

	public unsafe byte* pbData;

	public unsafe CRYPTOAPI_BLOB(int cbData, byte* pbData)
	{
		this.cbData = cbData;
		this.pbData = pbData;
	}

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
