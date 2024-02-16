namespace System.Threading;

internal class _IOCompletionCallback
{
	private readonly IOCompletionCallback _ioCompletionCallback;

	private readonly ExecutionContext _executionContext;

	private uint _errorCode;

	private uint _numBytes;

	private unsafe NativeOverlapped* _pNativeOverlapped;

	internal static ContextCallback _ccb = IOCompletionCallback_Context;

	internal _IOCompletionCallback(IOCompletionCallback ioCompletionCallback, ExecutionContext executionContext)
	{
		_ioCompletionCallback = ioCompletionCallback;
		_executionContext = executionContext;
	}

	internal unsafe static void IOCompletionCallback_Context(object state)
	{
		_IOCompletionCallback iOCompletionCallback = (_IOCompletionCallback)state;
		iOCompletionCallback._ioCompletionCallback(iOCompletionCallback._errorCode, iOCompletionCallback._numBytes, iOCompletionCallback._pNativeOverlapped);
	}

	internal unsafe static void PerformIOCompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* pNativeOverlapped)
	{
		do
		{
			OverlappedData overlappedFromNative = OverlappedData.GetOverlappedFromNative(pNativeOverlapped);
			if (overlappedFromNative._callback is IOCompletionCallback iOCompletionCallback)
			{
				iOCompletionCallback(errorCode, numBytes, pNativeOverlapped);
			}
			else
			{
				_IOCompletionCallback iOCompletionCallback2 = (_IOCompletionCallback)overlappedFromNative._callback;
				iOCompletionCallback2._errorCode = errorCode;
				iOCompletionCallback2._numBytes = numBytes;
				iOCompletionCallback2._pNativeOverlapped = pNativeOverlapped;
				ExecutionContext.RunInternal(iOCompletionCallback2._executionContext, _ccb, iOCompletionCallback2);
			}
			OverlappedData.CheckVMForIOPacket(out pNativeOverlapped, out errorCode, out numBytes);
		}
		while (pNativeOverlapped != null);
	}
}
