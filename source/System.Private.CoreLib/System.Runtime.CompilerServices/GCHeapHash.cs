using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Sequential)]
internal sealed class GCHeapHash
{
	private Array _data;

	private int _count;

	private int _deletedCount;
}
