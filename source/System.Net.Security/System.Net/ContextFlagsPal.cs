namespace System.Net;

[Flags]
internal enum ContextFlagsPal
{
	None = 0,
	Delegate = 1,
	MutualAuth = 2,
	ReplayDetect = 4,
	SequenceDetect = 8,
	Confidentiality = 0x10,
	UseSessionKey = 0x20,
	AllocateMemory = 0x100,
	Connection = 0x800,
	InitExtendedError = 0x4000,
	AcceptExtendedError = 0x8000,
	InitStream = 0x8000,
	AcceptStream = 0x10000,
	InitIntegrity = 0x10000,
	AcceptIntegrity = 0x20000,
	InitManualCredValidation = 0x80000,
	InitUseSuppliedCreds = 0x80,
	InitIdentify = 0x20000,
	AcceptIdentify = 0x80000,
	ProxyBindings = 0x4000000,
	AllowMissingBindings = 0x10000000,
	UnverifiedTargetName = 0x20000000
}
