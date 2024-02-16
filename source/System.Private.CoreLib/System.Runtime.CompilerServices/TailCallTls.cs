namespace System.Runtime.CompilerServices;

internal struct TailCallTls
{
	public unsafe PortableTailCallFrame* Frame;

	public IntPtr ArgBuffer;
}
