using System.Collections.Generic;

namespace System.Threading;

internal sealed class SystemThreading_ThreadLocalDebugView<T>
{
	private readonly ThreadLocal<T> _tlocal;

	public bool IsValueCreated => _tlocal.IsValueCreated;

	public T Value => _tlocal.ValueForDebugDisplay;

	public List<T> Values => _tlocal.ValuesForDebugDisplay;

	public SystemThreading_ThreadLocalDebugView(ThreadLocal<T> tlocal)
	{
		_tlocal = tlocal;
	}
}
