using System.Runtime.InteropServices;

namespace Internal;

[StructLayout(LayoutKind.Explicit, Size = 64)]
internal struct PaddedReference
{
	[FieldOffset(0)]
	public object Object;
}
