namespace System;

internal enum TypeNameFormatFlags
{
	FormatBasic = 0,
	FormatNamespace = 1,
	FormatFullInst = 2,
	FormatAssembly = 4,
	FormatSignature = 8,
	FormatNoVersion = 0x10,
	FormatAngleBrackets = 0x40,
	FormatStubInfo = 0x80,
	FormatGenericParam = 0x100
}
