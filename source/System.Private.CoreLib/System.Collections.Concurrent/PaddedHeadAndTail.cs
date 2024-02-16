using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Collections.Concurrent;

[StructLayout(LayoutKind.Explicit, Size = 192)]
[DebuggerDisplay("Head = {Head}, Tail = {Tail}")]
internal struct PaddedHeadAndTail
{
	[FieldOffset(64)]
	public int Head;

	[FieldOffset(128)]
	public int Tail;
}
