using System;

namespace Internal.Cryptography.Pal.Native;

[Flags]
internal enum ExpectedFormatTypeFlags
{
	CERT_QUERY_FORMAT_FLAG_BINARY = 2,
	CERT_QUERY_FORMAT_FLAG_BASE64_ENCODED = 4,
	CERT_QUERY_FORMAT_FLAG_ASN_ASCII_HEX_ENCODED = 8,
	CERT_QUERY_FORMAT_FLAG_ALL = 0xE
}
