using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System;

[NonVersionable]
internal readonly ref struct ByReference<T>
{
	private readonly IntPtr _value;

	public ref T Value
	{
		[Intrinsic]
		get
		{
			throw new PlatformNotSupportedException();
		}
	}

	[Intrinsic]
	public ByReference(ref T value)
	{
		throw new PlatformNotSupportedException();
	}
}
