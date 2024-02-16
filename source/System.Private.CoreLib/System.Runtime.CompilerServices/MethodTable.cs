using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Explicit)]
internal struct MethodTable
{
	[FieldOffset(0)]
	public ushort ComponentSize;

	[FieldOffset(0)]
	private uint Flags;

	[FieldOffset(4)]
	public uint BaseSize;

	[FieldOffset(14)]
	public ushort InterfaceCount;

	[FieldOffset(16)]
	public unsafe MethodTable* ParentMethodTable;

	[FieldOffset(48)]
	public unsafe void* ElementType;

	[FieldOffset(56)]
	public unsafe MethodTable** InterfaceMap;

	public bool HasComponentSize => (Flags & 0x80000000u) != 0;

	public bool ContainsGCPointers => (Flags & 0x1000000) != 0;

	public bool NonTrivialInterfaceCast => (Flags & 0x406C0000) != 0;

	public unsafe bool IsMultiDimensionalArray
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return BaseSize > (uint)(3 * sizeof(IntPtr));
		}
	}

	public unsafe int MultiDimensionalArrayRank
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return (int)((uint)((int)BaseSize - 3 * sizeof(IntPtr)) / 8u);
		}
	}
}
