using System.Runtime.CompilerServices;

namespace System.Collections;

[Obsolete("IHashCodeProvider has been deprecated. Use IEqualityComparer instead.")]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public interface IHashCodeProvider
{
	int GetHashCode(object obj);
}
