using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq;

internal sealed class SystemCore_EnumerableDebugView<T>
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly IEnumerable<T> _enumerable;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items
	{
		get
		{
			T[] array = _enumerable.ToArray();
			if (array.Length == 0)
			{
				throw new SystemCore_EnumerableDebugViewEmptyException();
			}
			return array;
		}
	}

	public SystemCore_EnumerableDebugView(IEnumerable<T> enumerable)
	{
		if (enumerable == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.enumerable);
		}
		_enumerable = enumerable;
	}
}
internal sealed class SystemCore_EnumerableDebugView
{
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private readonly IEnumerable _enumerable;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public object[] Items
	{
		get
		{
			List<object> list = new List<object>();
			foreach (object item in _enumerable)
			{
				list.Add(item);
			}
			if (list.Count == 0)
			{
				throw new SystemCore_EnumerableDebugViewEmptyException();
			}
			return list.ToArray();
		}
	}

	public SystemCore_EnumerableDebugView(IEnumerable enumerable)
	{
		if (enumerable == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.enumerable);
		}
		_enumerable = enumerable;
	}
}
