namespace System.Reflection;

[Flags]
internal enum MdSigCallingConvention : byte
{
	CallConvMask = 0xF,
	Default = 0,
	C = 1,
	StdCall = 2,
	ThisCall = 3,
	FastCall = 4,
	Vararg = 5,
	Field = 6,
	LocalSig = 7,
	Property = 8,
	Unmanaged = 9,
	GenericInst = 0xA,
	Generic = 0x10,
	HasThis = 0x20,
	ExplicitThis = 0x40
}
