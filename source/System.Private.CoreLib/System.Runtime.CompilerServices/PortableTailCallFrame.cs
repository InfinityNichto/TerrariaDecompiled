namespace System.Runtime.CompilerServices;

internal struct PortableTailCallFrame
{
	public IntPtr TailCallAwareReturnAddress;

	public unsafe delegate*<IntPtr, IntPtr, PortableTailCallFrame*, void> NextCall;
}
