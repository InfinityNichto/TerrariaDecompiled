using System;

namespace Internal.Cryptography.Pal.Native;

[Flags]
internal enum CertNameFlags
{
	None = 0,
	CERT_NAME_ISSUER_FLAG = 1
}
