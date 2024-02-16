using System.Threading;

namespace System.Net;

internal sealed class AsyncRequestContext : RequestContextBase
{
	private unsafe NativeOverlapped* _nativeOverlapped;

	private ThreadPoolBoundHandle _boundHandle;

	private readonly ListenerAsyncResult _result;

	internal unsafe NativeOverlapped* NativeOverlapped => _nativeOverlapped;

	internal unsafe AsyncRequestContext(ThreadPoolBoundHandle boundHandle, ListenerAsyncResult result)
	{
		_result = result;
		BaseConstruction(Allocate(boundHandle, 0u));
	}

	private unsafe global::Interop.HttpApi.HTTP_REQUEST* Allocate(ThreadPoolBoundHandle boundHandle, uint size)
	{
		uint num = ((size != 0) ? size : ((base.RequestBuffer == IntPtr.Zero) ? 4096u : base.Size));
		if (_nativeOverlapped != null)
		{
			NativeOverlapped* nativeOverlapped = _nativeOverlapped;
			_nativeOverlapped = null;
			_boundHandle.FreeNativeOverlapped(nativeOverlapped);
		}
		SetBuffer(checked((int)num));
		_boundHandle = boundHandle;
		_nativeOverlapped = boundHandle.AllocateNativeOverlapped(ListenerAsyncResult.IOCallback, _result, base.RequestBuffer);
		return (global::Interop.HttpApi.HTTP_REQUEST*)base.RequestBuffer.ToPointer();
	}

	internal unsafe void Reset(ThreadPoolBoundHandle boundHandle, ulong requestId, uint size)
	{
		SetBlob(Allocate(boundHandle, size));
		base.RequestBlob->RequestId = requestId;
	}

	protected unsafe override void OnReleasePins()
	{
		if (_nativeOverlapped != null)
		{
			NativeOverlapped* nativeOverlapped = _nativeOverlapped;
			_nativeOverlapped = null;
			_boundHandle.FreeNativeOverlapped(nativeOverlapped);
		}
	}

	protected unsafe override void Dispose(bool disposing)
	{
		if (_nativeOverlapped != null && (!Environment.HasShutdownStarted || disposing))
		{
			_boundHandle.FreeNativeOverlapped(_nativeOverlapped);
		}
		base.Dispose(disposing);
	}
}
