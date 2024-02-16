namespace System.Diagnostics;

internal sealed class ThreadInfo
{
	internal ulong _threadId;

	internal int _processId;

	internal int _basePriority;

	internal int _currentPriority;

	internal IntPtr _startAddress;

	internal ThreadState _threadState;

	internal ThreadWaitReason _threadWaitReason;
}
