using System.Threading;

namespace System.Net;

internal sealed class ListenerAsyncResult : System.Net.LazyAsyncResult
{
	private unsafe static readonly IOCompletionCallback s_ioCallback = WaitCallback;

	private AsyncRequestContext _requestContext;

	internal static IOCompletionCallback IOCallback => s_ioCallback;

	internal ListenerAsyncResult(HttpListenerSession session, object userState, AsyncCallback callback)
		: base(session, userState, callback)
	{
		_requestContext = new AsyncRequestContext(session.RequestQueueBoundHandle, this);
	}

	private unsafe static void IOCompleted(ListenerAsyncResult asyncResult, uint errorCode, uint numBytes)
	{
		object obj = null;
		try
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"errorCode:[{errorCode}] numBytes:[{numBytes}]", "IOCompleted");
			}
			if (errorCode != 0 && errorCode != 234)
			{
				asyncResult.ErrorCode = (int)errorCode;
				obj = new HttpListenerException((int)errorCode);
			}
			else
			{
				HttpListenerSession httpListenerSession = asyncResult.AsyncObject as HttpListenerSession;
				if (errorCode == 0)
				{
					bool stoleBlob = false;
					try
					{
						if (HttpListener.ValidateRequest(httpListenerSession, asyncResult._requestContext))
						{
							obj = httpListenerSession.Listener.HandleAuthentication(httpListenerSession, asyncResult._requestContext, out stoleBlob);
						}
					}
					finally
					{
						if (stoleBlob)
						{
							asyncResult._requestContext = ((obj == null) ? new AsyncRequestContext(httpListenerSession.RequestQueueBoundHandle, asyncResult) : null);
						}
						else
						{
							asyncResult._requestContext.Reset(httpListenerSession.RequestQueueBoundHandle, 0uL, 0u);
						}
					}
				}
				else
				{
					asyncResult._requestContext.Reset(httpListenerSession.RequestQueueBoundHandle, asyncResult._requestContext.RequestBlob->RequestId, numBytes);
				}
				if (obj == null)
				{
					uint num = asyncResult.QueueBeginGetContext();
					if (num != 0 && num != 997)
					{
						obj = new HttpListenerException((int)num);
					}
				}
				if (obj == null)
				{
					return;
				}
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, "Calling Complete()", "IOCompleted");
			}
		}
		catch (Exception ex) when (!System.Net.ExceptionCheck.IsFatal(ex))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"Caught exception: {ex}", "IOCompleted");
			}
			obj = ex;
		}
		asyncResult.InvokeCallback(obj);
	}

	private unsafe static void WaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
	{
		ListenerAsyncResult asyncResult = (ListenerAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
		IOCompleted(asyncResult, errorCode, numBytes);
	}

	internal unsafe uint QueueBeginGetContext()
	{
		uint num = 0u;
		while (true)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"Calling Interop.HttpApi.HttpReceiveHttpRequest RequestId: {_requestContext.RequestBlob->RequestId} Buffer: 0x{(IntPtr)_requestContext.RequestBlob:x} Size: {_requestContext.Size}", "QueueBeginGetContext");
			}
			uint num2 = 0u;
			HttpListenerSession httpListenerSession = (HttpListenerSession)base.AsyncObject;
			num = global::Interop.HttpApi.HttpReceiveHttpRequest(httpListenerSession.RequestQueueHandle, _requestContext.RequestBlob->RequestId, 1u, _requestContext.RequestBlob, _requestContext.Size, &num2, _requestContext.NativeOverlapped);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "Call to Interop.HttpApi.HttpReceiveHttpRequest returned:" + num, "QueueBeginGetContext");
			}
			if (num == 87 && _requestContext.RequestBlob->RequestId != 0L)
			{
				_requestContext.RequestBlob->RequestId = 0uL;
				continue;
			}
			switch (num)
			{
			case 234u:
				_requestContext.Reset(httpListenerSession.RequestQueueBoundHandle, _requestContext.RequestBlob->RequestId, num2);
				continue;
			case 0u:
				if (HttpListener.SkipIOCPCallbackOnSuccess)
				{
					IOCompleted(this, num, num2);
				}
				break;
			}
			break;
		}
		return num;
	}

	protected override void Cleanup()
	{
		if (_requestContext != null)
		{
			_requestContext.ReleasePins();
			_requestContext.Close();
		}
		base.Cleanup();
	}
}
