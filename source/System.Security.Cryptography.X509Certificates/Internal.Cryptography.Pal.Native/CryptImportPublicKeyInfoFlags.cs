using System;

namespace Internal.Cryptography.Pal.Native;

[Flags]
internal enum CryptImportPublicKeyInfoFlags
{
	NONE = 0,
	CRYPT_OID_INFO_PUBKEY_ENCRYPT_KEY_FLAG = 0x40000000
}
