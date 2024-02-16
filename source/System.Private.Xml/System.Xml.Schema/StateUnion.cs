using System.Runtime.InteropServices;

namespace System.Xml.Schema;

[StructLayout(LayoutKind.Explicit)]
internal struct StateUnion
{
	[FieldOffset(0)]
	public int State;

	[FieldOffset(0)]
	public int AllElementsRequired;

	[FieldOffset(0)]
	public int CurPosIndex;

	[FieldOffset(0)]
	public int NumberOfRunningPos;
}
