namespace System.Runtime.InteropServices;

public sealed class PosixSignalContext
{
	public PosixSignal Signal { get; internal set; }

	public bool Cancel { get; set; }

	public PosixSignalContext(PosixSignal signal)
	{
		Signal = signal;
	}
}
