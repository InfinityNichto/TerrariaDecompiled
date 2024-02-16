using System;

namespace Internal.Cryptography.Pal.Native;

[Flags]
internal enum CertSetPropertyFlags
{
	CERT_SET_PROPERTY_INHIBIT_PERSIST_FLAG = 0x40000000,
	None = 0
}
