namespace System;

internal enum DelegateBindingFlags
{
	StaticMethodOnly = 1,
	InstanceMethodOnly = 2,
	OpenDelegateOnly = 4,
	ClosedDelegateOnly = 8,
	NeverCloseOverNull = 0x10,
	CaselessMatching = 0x20,
	RelaxedSignature = 0x40
}
