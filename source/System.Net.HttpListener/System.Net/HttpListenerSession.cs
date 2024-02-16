using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net;

internal sealed class HttpListenerSession
{
	public readonly HttpListener Listener;

	public readonly SafeHandle RequestQueueHandle;

	private ThreadPoolBoundHandle _requestQueueBoundHandle;

	public ThreadPoolBoundHandle RequestQueueBoundHandle
	{
		get
		{
			if (_requestQueueBoundHandle == null)
			{
				lock (this)
				{
					if (_requestQueueBoundHandle == null)
					{
						_requestQueueBoundHandle = ThreadPoolBoundHandle.BindHandle(RequestQueueHandle);
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info($"ThreadPoolBoundHandle.BindHandle({RequestQueueHandle}) -> {_requestQueueBoundHandle}", null, "RequestQueueBoundHandle");
						}
					}
				}
			}
			return _requestQueueBoundHandle;
		}
	}

	public unsafe HttpListenerSession(HttpListener listener)
	{
		Listener = listener;
		HttpRequestQueueV2Handle pReqQueueHandle;
		uint num = global::Interop.HttpApi.HttpCreateRequestQueue(global::Interop.HttpApi.s_version, null, null, 0u, out pReqQueueHandle);
		if (num != 0)
		{
			throw new HttpListenerException((int)num);
		}
		if (HttpListener.SkipIOCPCallbackOnSuccess && !global::Interop.Kernel32.SetFileCompletionNotificationModes(pReqQueueHandle, global::Interop.Kernel32.FileCompletionNotificationModes.SkipCompletionPortOnSuccess | global::Interop.Kernel32.FileCompletionNotificationModes.SkipSetEventOnHandle))
		{
			throw new HttpListenerException(Marshal.GetLastPInvokeError());
		}
		RequestQueueHandle = pReqQueueHandle;
	}

	public unsafe void CloseRequestQueueHandle()
	{
		lock (this)
		{
			if (!RequestQueueHandle.IsInvalid)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info($"Dispose ThreadPoolBoundHandle: {_requestQueueBoundHandle}", null, "CloseRequestQueueHandle");
				}
				_requestQueueBoundHandle?.Dispose();
				RequestQueueHandle.Dispose();
				try
				{
					global::Interop.Kernel32.CancelIoEx(RequestQueueHandle, null);
					return;
				}
				catch (ObjectDisposedException)
				{
					return;
				}
			}
		}
	}
}
