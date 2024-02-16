using System.Runtime.CompilerServices;

namespace System.Threading;

internal sealed class OverlappedData
{
	internal IAsyncResult _asyncResult;

	internal object _callback;

	internal readonly Overlapped _overlapped;

	private object _userObject;

	private unsafe readonly NativeOverlapped* _pNativeOverlapped;

	private IntPtr _eventHandle;

	private int _offsetLow;

	private int _offsetHigh;

	internal unsafe ref int OffsetLow
	{
		get
		{
			if (_pNativeOverlapped == null)
			{
				return ref _offsetLow;
			}
			return ref _pNativeOverlapped->OffsetLow;
		}
	}

	internal unsafe ref int OffsetHigh
	{
		get
		{
			if (_pNativeOverlapped == null)
			{
				return ref _offsetHigh;
			}
			return ref _pNativeOverlapped->OffsetHigh;
		}
	}

	internal unsafe ref IntPtr EventHandle
	{
		get
		{
			if (_pNativeOverlapped == null)
			{
				return ref _eventHandle;
			}
			return ref _pNativeOverlapped->EventHandle;
		}
	}

	internal OverlappedData(Overlapped overlapped)
	{
		_overlapped = overlapped;
	}

	internal unsafe NativeOverlapped* Pack(IOCompletionCallback iocb, object userData)
	{
		if (_pNativeOverlapped != null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_Overlapped_Pack);
		}
		if (iocb != null)
		{
			ExecutionContext executionContext = ExecutionContext.Capture();
			_callback = ((executionContext != null && !executionContext.IsDefault) ? ((object)new _IOCompletionCallback(iocb, executionContext)) : ((object)iocb));
		}
		else
		{
			_callback = null;
		}
		_userObject = userData;
		return AllocateNativeOverlapped();
	}

	internal unsafe NativeOverlapped* UnsafePack(IOCompletionCallback iocb, object userData)
	{
		if (_pNativeOverlapped != null)
		{
			throw new InvalidOperationException(SR.InvalidOperation_Overlapped_Pack);
		}
		_userObject = userData;
		_callback = iocb;
		return AllocateNativeOverlapped();
	}

	internal bool IsUserObject(byte[] buffer)
	{
		return _userObject == buffer;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe extern NativeOverlapped* AllocateNativeOverlapped();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern void FreeNativeOverlapped(NativeOverlapped* nativeOverlappedPtr);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern OverlappedData GetOverlappedFromNative(NativeOverlapped* nativeOverlappedPtr);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern void CheckVMForIOPacket(out NativeOverlapped* pNativeOverlapped, out uint errorCode, out uint numBytes);
}
