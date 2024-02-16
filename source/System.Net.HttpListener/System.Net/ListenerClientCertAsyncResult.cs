using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net;

internal class ListenerClientCertAsyncResult : System.Net.LazyAsyncResult
{
	private ThreadPoolBoundHandle _boundHandle;

	private unsafe NativeOverlapped* _pOverlapped;

	private byte[] _backingBuffer;

	private unsafe global::Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* _memoryBlob;

	private uint _size;

	private unsafe static readonly IOCompletionCallback s_IOCallback = WaitCallback;

	internal unsafe NativeOverlapped* NativeOverlapped => _pOverlapped;

	internal unsafe global::Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* RequestBlob => _memoryBlob;

	internal ListenerClientCertAsyncResult(ThreadPoolBoundHandle boundHandle, object asyncObject, object userState, AsyncCallback callback, uint size)
		: base(asyncObject, userState, callback)
	{
		_boundHandle = boundHandle;
		Reset(size);
	}

	internal unsafe void Reset(uint size)
	{
		if (size != _size)
		{
			if (_size != 0)
			{
				_boundHandle.FreeNativeOverlapped(_pOverlapped);
			}
			_size = size;
			if (size == 0)
			{
				_pOverlapped = null;
				_memoryBlob = null;
				_backingBuffer = null;
			}
			else
			{
				_backingBuffer = new byte[checked((int)size)];
				_pOverlapped = _boundHandle.AllocateNativeOverlapped(s_IOCallback, this, _backingBuffer);
				_memoryBlob = (global::Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement(_backingBuffer, 0);
			}
		}
	}

	internal void IOCompleted(uint errorCode, uint numBytes)
	{
		IOCompleted(this, errorCode, numBytes);
	}

	private unsafe static void IOCompleted(ListenerClientCertAsyncResult asyncResult, uint errorCode, uint numBytes)
	{
		HttpListenerRequest httpListenerRequest = (HttpListenerRequest)asyncResult.AsyncObject;
		object result = null;
		try
		{
			if (errorCode == 234)
			{
				global::Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* requestBlob = asyncResult.RequestBlob;
				asyncResult.Reset(numBytes + requestBlob->CertEncodedSize);
				uint num = 0u;
				errorCode = global::Interop.HttpApi.HttpReceiveClientCertificate(httpListenerRequest.HttpListenerContext.RequestQueueHandle, httpListenerRequest._connectionId, 0u, asyncResult._memoryBlob, asyncResult._size, &num, asyncResult._pOverlapped);
				switch (errorCode)
				{
				case 0u:
					if (!HttpListener.SkipIOCPCallbackOnSuccess)
					{
						return;
					}
					break;
				case 997u:
					return;
				}
			}
			if (errorCode != 0)
			{
				asyncResult.ErrorCode = (int)errorCode;
				result = new HttpListenerException((int)errorCode);
			}
			else
			{
				global::Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* memoryBlob = asyncResult._memoryBlob;
				if (memoryBlob != null)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(null, $"pClientCertInfo:{(IntPtr)memoryBlob} pClientCertInfo->CertFlags: {memoryBlob->CertFlags} pClientCertInfo->CertEncodedSize: {memoryBlob->CertEncodedSize} pClientCertInfo->pCertEncoded: {(IntPtr)memoryBlob->pCertEncoded} pClientCertInfo->Token: {(IntPtr)memoryBlob->Token} pClientCertInfo->CertDeniedByMapper: {memoryBlob->CertDeniedByMapper}", "IOCompleted");
					}
					if (memoryBlob->pCertEncoded != null)
					{
						try
						{
							byte[] array = new byte[memoryBlob->CertEncodedSize];
							Marshal.Copy((IntPtr)memoryBlob->pCertEncoded, array, 0, array.Length);
							X509Certificate2 x509Certificate2 = (httpListenerRequest.ClientCertificate = new X509Certificate2(array));
							result = x509Certificate2;
						}
						catch (CryptographicException ex)
						{
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(null, $"HttpListenerRequest: {httpListenerRequest} caught CryptographicException: {ex}", "IOCompleted");
							}
							result = ex;
						}
						catch (SecurityException ex2)
						{
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(null, $"HttpListenerRequest: {httpListenerRequest} caught SecurityException: {ex2}", "IOCompleted");
							}
							result = ex2;
						}
					}
					httpListenerRequest.SetClientCertificateError((int)memoryBlob->CertFlags);
				}
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, "Calling Complete()", "IOCompleted");
			}
		}
		catch (Exception ex3) when (!System.Net.ExceptionCheck.IsFatal(ex3))
		{
			result = ex3;
		}
		finally
		{
			if (errorCode != 997)
			{
				httpListenerRequest.ClientCertState = ListenerClientCertState.Completed;
			}
		}
		asyncResult.InvokeCallback(result);
	}

	private unsafe static void WaitCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
	{
		ListenerClientCertAsyncResult asyncResult = (ListenerClientCertAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(null, $"errorCode:[{errorCode}] numBytes:[{numBytes}] nativeOverlapped:[{(long)nativeOverlapped}]", "WaitCallback");
		}
		IOCompleted(asyncResult, errorCode, numBytes);
	}

	protected unsafe override void Cleanup()
	{
		if (_pOverlapped != null)
		{
			_memoryBlob = null;
			_boundHandle.FreeNativeOverlapped(_pOverlapped);
			_pOverlapped = null;
			_boundHandle = null;
		}
		GC.SuppressFinalize(this);
		base.Cleanup();
	}

	unsafe ~ListenerClientCertAsyncResult()
	{
		if (_pOverlapped != null && !Environment.HasShutdownStarted)
		{
			_boundHandle.FreeNativeOverlapped(_pOverlapped);
			_pOverlapped = null;
			_boundHandle = null;
		}
	}
}
