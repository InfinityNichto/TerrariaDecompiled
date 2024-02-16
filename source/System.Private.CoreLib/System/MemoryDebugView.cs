using System.Diagnostics;

namespace System;

internal sealed class MemoryDebugView<T>
{
	private readonly ReadOnlyMemory<T> _memory;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items => _memory.ToArray();

	public MemoryDebugView(Memory<T> memory)
	{
		_memory = memory;
	}

	public MemoryDebugView(ReadOnlyMemory<T> memory)
	{
		_memory = memory;
	}
}
