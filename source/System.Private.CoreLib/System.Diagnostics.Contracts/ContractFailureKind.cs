using System.Runtime.CompilerServices;

namespace System.Diagnostics.Contracts;

[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public enum ContractFailureKind
{
	Precondition,
	Postcondition,
	PostconditionOnException,
	Invariant,
	Assert,
	Assume
}
