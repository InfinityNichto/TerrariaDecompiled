using System.Runtime.CompilerServices;

namespace System.Data.SqlTypes;

[Flags]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public enum SqlCompareOptions
{
	None = 0,
	IgnoreCase = 1,
	IgnoreNonSpace = 2,
	IgnoreKanaType = 8,
	IgnoreWidth = 0x10,
	BinarySort = 0x8000,
	BinarySort2 = 0x4000
}
