namespace System.Threading;

internal sealed class ThreadPoolBoundHandleOverlapped : Overlapped
{
	private unsafe static readonly IOCompletionCallback s_completionCallback = CompletionCallback;

	private readonly IOCompletionCallback _userCallback;

	internal readonly object _userState;

	internal readonly PreAllocatedOverlapped _preAllocated;

	internal unsafe NativeOverlapped* _nativeOverlapped;

	internal ThreadPoolBoundHandle _boundHandle;

	internal bool _completed;

	public unsafe ThreadPoolBoundHandleOverlapped(IOCompletionCallback callback, object state, object pinData, PreAllocatedOverlapped preAllocated, bool flowExecutionContext)
	{
		_userCallback = callback;
		_userState = state;
		_preAllocated = preAllocated;
		_nativeOverlapped = (flowExecutionContext ? Pack(s_completionCallback, pinData) : UnsafePack(s_completionCallback, pinData));
		_nativeOverlapped->OffsetLow = 0;
		_nativeOverlapped->OffsetHigh = 0;
	}

	private unsafe static void CompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
	{
		ThreadPoolBoundHandleOverlapped threadPoolBoundHandleOverlapped = (ThreadPoolBoundHandleOverlapped)Overlapped.Unpack(nativeOverlapped);
		if (threadPoolBoundHandleOverlapped._completed)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NativeOverlappedReused);
		}
		threadPoolBoundHandleOverlapped._completed = true;
		if (threadPoolBoundHandleOverlapped._boundHandle == null)
		{
			throw new InvalidOperationException(SR.Argument_NativeOverlappedAlreadyFree);
		}
		threadPoolBoundHandleOverlapped._userCallback(errorCode, numBytes, nativeOverlapped);
	}
}
