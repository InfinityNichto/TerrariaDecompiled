namespace System.Reflection;

[Flags]
internal enum PInvokeAttributes
{
	NoMangle = 1,
	CharSetMask = 6,
	CharSetNotSpec = 0,
	CharSetAnsi = 2,
	CharSetUnicode = 4,
	CharSetAuto = 6,
	BestFitUseAssem = 0,
	BestFitEnabled = 0x10,
	BestFitDisabled = 0x20,
	BestFitMask = 0x30,
	ThrowOnUnmappableCharUseAssem = 0,
	ThrowOnUnmappableCharEnabled = 0x1000,
	ThrowOnUnmappableCharDisabled = 0x2000,
	ThrowOnUnmappableCharMask = 0x3000,
	SupportsLastError = 0x40,
	CallConvMask = 0x700,
	CallConvWinapi = 0x100,
	CallConvCdecl = 0x200,
	CallConvStdcall = 0x300,
	CallConvThiscall = 0x400,
	CallConvFastcall = 0x500,
	MaxValue = 0xFFFF
}
