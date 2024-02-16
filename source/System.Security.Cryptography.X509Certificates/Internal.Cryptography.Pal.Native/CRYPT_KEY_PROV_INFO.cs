using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CRYPT_KEY_PROV_INFO
{
	public unsafe char* pwszContainerName;

	public unsafe char* pwszProvName;

	public int dwProvType;

	public CryptAcquireContextFlags dwFlags;

	public int cProvParam;

	public IntPtr rgProvParam;

	public int dwKeySpec;
}
