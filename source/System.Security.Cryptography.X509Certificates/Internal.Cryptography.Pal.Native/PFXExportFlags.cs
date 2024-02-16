using System;

namespace Internal.Cryptography.Pal.Native;

[Flags]
internal enum PFXExportFlags
{
	REPORT_NO_PRIVATE_KEY = 1,
	REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY = 2,
	EXPORT_PRIVATE_KEYS = 4,
	None = 0
}
