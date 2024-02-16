using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System;

internal sealed class LazyDebugView<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>
{
	private readonly Lazy<T> _lazy;

	public bool IsValueCreated => _lazy.IsValueCreated;

	public T Value => _lazy.ValueForDebugDisplay;

	public LazyThreadSafetyMode? Mode => _lazy.Mode;

	public bool IsValueFaulted => _lazy.IsValueFaulted;

	public LazyDebugView(Lazy<T> lazy)
	{
		_lazy = lazy;
	}
}
