using System;

namespace Internal.Cryptography.Pal.Native;

[Flags]
internal enum CryptAcquireContextFlags
{
	CRYPT_DELETEKEYSET = 0x10,
	CRYPT_MACHINE_KEYSET = 0x20,
	None = 0
}
