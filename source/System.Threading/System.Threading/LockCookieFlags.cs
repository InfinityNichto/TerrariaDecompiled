namespace System.Threading;

[Flags]
internal enum LockCookieFlags
{
	Upgrade = 0x2000,
	Release = 0x4000,
	OwnedNone = 0x10000,
	OwnedWriter = 0x20000,
	OwnedReader = 0x40000,
	Invalid = -483329
}
