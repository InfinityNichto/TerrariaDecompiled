using System.Diagnostics;

namespace System.Collections.Generic;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal readonly struct Marker
{
	public int Count { get; }

	public int Index { get; }

	private string DebuggerDisplay => $"{"Index"}: {Index}, {"Count"}: {Count}";

	public Marker(int count, int index)
	{
		Count = count;
		Index = index;
	}
}
