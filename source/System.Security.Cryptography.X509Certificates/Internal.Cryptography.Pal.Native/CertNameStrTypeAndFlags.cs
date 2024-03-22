using System;

namespace Internal.Cryptography.Pal.Native;

[Flags]
internal enum CertNameStrTypeAndFlags
{
	CERT_SIMPLE_NAME_STR = 1,
	CERT_OID_NAME_STR = 2,
	CERT_X500_NAME_STR = 3,
	CERT_NAME_STR_SEMICOLON_FLAG = 0x40000000,
	CERT_NAME_STR_NO_PLUS_FLAG = 0x20000000,
	CERT_NAME_STR_NO_QUOTING_FLAG = 0x10000000,
	CERT_NAME_STR_CRLF_FLAG = 0x8000000,
	CERT_NAME_STR_COMMA_FLAG = 0x4000000,
	CERT_NAME_STR_REVERSE_FLAG = 0x2000000,
	CERT_NAME_STR_DISABLE_IE4_UTF8_FLAG = 0x10000,
	CERT_NAME_STR_ENABLE_T61_UNICODE_FLAG = 0x20000,
	CERT_NAME_STR_ENABLE_UTF8_UNICODE_FLAG = 0x40000,
	CERT_NAME_STR_FORCE_UTF8_DIR_STR_FLAG = 0x80000
}