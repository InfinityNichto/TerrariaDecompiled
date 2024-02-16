using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net;

internal sealed class HttpRequestStream : Stream
{
	private sealed class HttpRequestStreamAsyncResult : System.Net.LazyAsyncResult
	{
		private readonly ThreadPoolBoundHandle _boundHandle;

		internal unsafe NativeOverlapped* _pOverlapped;

		internal unsafe void* _pPinnedBuffer;

		internal uint _dataAlreadyRead;

		private unsafe static readonly IOCompletionCallback s_IOCallback = Callback;

		internal HttpRequestStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback)
			: base(asyncObject, userState, callback)
		{
		}

		internal HttpRequestStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback, uint dataAlreadyRead)
			: base(asyncObject, userState, callback)
		{
			_dataAlreadyRead = dataAlreadyRead;
		}

		internal unsafe HttpRequestStreamAsyncResult(ThreadPoolBoundHandle boundHandle, object asyncObject, object userState, AsyncCallback callback, byte[] buffer, int offset, uint size, uint dataAlreadyRead)
			: base(asyncObject, userState, callback)
		{
			_dataAlreadyRead = dataAlreadyRead;
			_boundHandle = boundHandle;
			_pOverlapped = boundHandle.AllocateNativeOverlapped(s_IOCallback, this, buffer);
			_pPinnedBuffer = (void*)Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
		}

		internal void IOCompleted(uint errorCode, uint numBytes)
		{
			IOCompleted(this, errorCode, numBytes);
		}

		private unsafe static void IOCompleted(HttpRequestStreamAsyncResult asyncResult, uint errorCode, uint numBytes)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"asyncResult: {asyncResult} errorCode:0x {errorCode:x8} numBytes: {numBytes}", "IOCompleted");
			}
			object obj = null;
			try
			{
				if (errorCode != 0 && errorCode != 38)
				{
					asyncResult.ErrorCode = (int)errorCode;
					obj = new HttpListenerException((int)errorCode);
				}
				else
				{
					obj = numBytes;
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.DumpBuffer(asyncResult, (IntPtr)asyncResult._pPinnedBuffer, (int)numBytes, "IOCompleted");
					}
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(null, $"asyncResult: {asyncResult} calling Complete()", "IOCompleted");
				}
			}
			catch (Exception ex)
			{
				obj = ex;
			}
			asyncResult.InvokeCallback(obj);
		}

		private unsafe static void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
		{
			HttpRequestStreamAsyncResult httpRequestStreamAsyncResult = (HttpRequestStreamAsyncResult)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, $"asyncResult: {httpRequestStreamAsyncResult} errorCode:0x {errorCode:x8} numBytes: {numBytes} nativeOverlapped:0x{(IntPtr)nativeOverlapped:x8}", "Callback");
			}
			IOCompleted(httpRequestStreamAsyncResult, errorCode, numBytes);
		}

		protected unsafe override void Cleanup()
		{
			base.Cleanup();
			if (_pOverlapped != null)
			{
				_boundHandle.FreeNativeOverlapped(_pOverlapped);
			}
		}
	}

	private bool _closed;

	private readonly HttpListenerContext _httpContext;

	private uint _dataChunkOffset;

	private int _dataChunkIndex;

	private bool _inOpaqueMode;

	public override bool CanSeek => false;

	public override bool CanWrite => false;

	public override bool CanRead => true;

	public override long Length
	{
		get
		{
			throw new NotSupportedException(System.SR.net_noseek);
		}
	}

	public override long Position
	{
		get
		{
			throw new NotSupportedException(System.SR.net_noseek);
		}
		set
		{
			throw new NotSupportedException(System.SR.net_noseek);
		}
	}

	internal bool Closed => _closed;

	internal bool BufferedDataChunksAvailable => _dataChunkIndex > -1;

	internal HttpListenerContext InternalHttpContext => _httpContext;

	public override int Read(byte[] buffer, int offset, int count)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "buffer.Length:" + buffer.Length + " count:" + count + " offset:" + offset, "Read");
		}
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (count == 0 || _closed)
		{
			return 0;
		}
		return ReadCore(buffer, offset, count);
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "buffer.Length:" + buffer.Length + " count:" + count + " offset:" + offset, "BeginRead");
		}
		Stream.ValidateBufferArguments(buffer, offset, count);
		return BeginReadCore(buffer, offset, count, callback, state);
	}

	public override void Flush()
	{
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException(System.SR.net_noseek);
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException(System.SR.net_noseek);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new InvalidOperationException(System.SR.net_readonlystream);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		throw new InvalidOperationException(System.SR.net_readonlystream);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		throw new InvalidOperationException(System.SR.net_readonlystream);
	}

	protected override void Dispose(bool disposing)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "_closed:" + _closed, "Dispose");
		}
		_closed = true;
		base.Dispose(disposing);
	}

	internal HttpRequestStream(HttpListenerContext httpContext)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"httpContextt:{httpContext}", ".ctor");
		}
		_httpContext = httpContext;
	}

	private unsafe int ReadCore(byte[] buffer, int offset, int size)
	{
		uint num = 0u;
		if (_dataChunkIndex != -1)
		{
			num = global::Interop.HttpApi.GetChunks(_httpContext.Request.RequestBuffer, _httpContext.Request.OriginalBlobAddress, ref _dataChunkIndex, ref _dataChunkOffset, buffer, offset, size);
		}
		if (_dataChunkIndex == -1 && num < size)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "size:" + size + " offset:" + offset, "ReadCore");
			}
			uint num2 = 0u;
			uint bytesReturned = 0u;
			offset += (int)num;
			size -= (int)num;
			if (size > 131072)
			{
				size = 131072;
			}
			fixed (byte* ptr = buffer)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Calling Interop.HttpApi.HttpReceiveRequestEntityBody", "ReadCore");
				}
				uint flags = 0u;
				if (!_inOpaqueMode)
				{
					flags = 1u;
				}
				num2 = global::Interop.HttpApi.HttpReceiveRequestEntityBody(_httpContext.RequestQueueHandle, _httpContext.RequestId, flags, ptr + offset, (uint)size, out bytesReturned, null);
				num += bytesReturned;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Call to Interop.HttpApi.HttpReceiveRequestEntityBody returned:" + num2 + " dataRead:" + num, "ReadCore");
				}
			}
			if (num2 != 0 && num2 != 38)
			{
				Exception ex = new HttpListenerException((int)num2);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(this, ex.ToString(), "ReadCore");
				}
				throw ex;
			}
			UpdateAfterRead(num2, num);
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.DumpBuffer(this, buffer, offset, (int)num, "ReadCore");
			System.Net.NetEventSource.Info(this, "returning dataRead:" + num, "ReadCore");
		}
		return (int)num;
	}

	private void UpdateAfterRead(uint statusCode, uint dataRead)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "statusCode:" + statusCode + " _closed:" + _closed, "UpdateAfterRead");
		}
		if (statusCode == 38 || dataRead == 0)
		{
			Close();
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "statusCode:" + statusCode + " _closed:" + _closed, "UpdateAfterRead");
		}
	}

	public unsafe IAsyncResult BeginReadCore(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
	{
		if (size == 0 || _closed)
		{
			HttpRequestStreamAsyncResult httpRequestStreamAsyncResult = new HttpRequestStreamAsyncResult(this, state, callback);
			httpRequestStreamAsyncResult.InvokeCallback(0u);
			return httpRequestStreamAsyncResult;
		}
		HttpRequestStreamAsyncResult httpRequestStreamAsyncResult2 = null;
		uint num = 0u;
		if (_dataChunkIndex != -1)
		{
			num = global::Interop.HttpApi.GetChunks(_httpContext.Request.RequestBuffer, _httpContext.Request.OriginalBlobAddress, ref _dataChunkIndex, ref _dataChunkOffset, buffer, offset, size);
			if (_dataChunkIndex != -1 && num == size)
			{
				httpRequestStreamAsyncResult2 = new HttpRequestStreamAsyncResult(_httpContext.RequestQueueBoundHandle, this, state, callback, buffer, offset, (uint)size, 0u);
				httpRequestStreamAsyncResult2.InvokeCallback(num);
			}
		}
		if (_dataChunkIndex == -1 && num < size)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "size:" + size + " offset:" + offset, "BeginReadCore");
			}
			uint num2 = 0u;
			offset += (int)num;
			size -= (int)num;
			if (size > 131072)
			{
				size = 131072;
			}
			httpRequestStreamAsyncResult2 = new HttpRequestStreamAsyncResult(_httpContext.RequestQueueBoundHandle, this, state, callback, buffer, offset, (uint)size, num);
			uint bytesReturned;
			try
			{
				fixed (byte* ptr = buffer)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, "Calling Interop.HttpApi.HttpReceiveRequestEntityBody", "BeginReadCore");
					}
					uint flags = 0u;
					if (!_inOpaqueMode)
					{
						flags = 1u;
					}
					num2 = global::Interop.HttpApi.HttpReceiveRequestEntityBody(_httpContext.RequestQueueHandle, _httpContext.RequestId, flags, httpRequestStreamAsyncResult2._pPinnedBuffer, (uint)size, out bytesReturned, httpRequestStreamAsyncResult2._pOverlapped);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, "Call to Interop.HttpApi.HttpReceiveRequestEntityBody returned:" + num2 + " dataRead:" + num, "BeginReadCore");
					}
				}
			}
			catch (Exception ex)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(this, ex.ToString(), "BeginReadCore");
				}
				httpRequestStreamAsyncResult2.InternalCleanup();
				throw;
			}
			if (num2 != 0 && num2 != 997)
			{
				httpRequestStreamAsyncResult2.InternalCleanup();
				if (num2 != 38)
				{
					Exception ex2 = new HttpListenerException((int)num2);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Error(this, ex2.ToString(), "BeginReadCore");
					}
					httpRequestStreamAsyncResult2.InternalCleanup();
					throw ex2;
				}
				httpRequestStreamAsyncResult2 = new HttpRequestStreamAsyncResult(this, state, callback, num);
				httpRequestStreamAsyncResult2.InvokeCallback(0u);
			}
			else if (num2 == 0 && HttpListener.SkipIOCPCallbackOnSuccess)
			{
				httpRequestStreamAsyncResult2.IOCompleted(num2, bytesReturned);
			}
		}
		return httpRequestStreamAsyncResult2;
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"asyncResult: {asyncResult}", "EndRead");
		}
		if (asyncResult == null)
		{
			throw new ArgumentNullException("asyncResult");
		}
		if (!(asyncResult is HttpRequestStreamAsyncResult httpRequestStreamAsyncResult) || httpRequestStreamAsyncResult.AsyncObject != this)
		{
			throw new ArgumentException(System.SR.net_io_invalidasyncresult, "asyncResult");
		}
		if (httpRequestStreamAsyncResult.EndCalled)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidendcall, "EndRead"));
		}
		httpRequestStreamAsyncResult.EndCalled = true;
		object obj = httpRequestStreamAsyncResult.InternalWaitForCompletion();
		if (obj is Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "Rethrowing exception:" + ex, "EndRead");
				System.Net.NetEventSource.Error(this, ex.ToString(), "EndRead");
			}
			ExceptionDispatchInfo.Throw(ex);
		}
		uint num = (uint)obj;
		UpdateAfterRead((uint)httpRequestStreamAsyncResult.ErrorCode, num);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"returnValue:{obj}", "EndRead");
		}
		return (int)(num + httpRequestStreamAsyncResult._dataAlreadyRead);
	}

	internal void SwitchToOpaqueMode()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, "SwitchToOpaqueMode");
		}
		_inOpaqueMode = true;
	}

	internal uint GetChunks(byte[] buffer, int offset, int size)
	{
		return global::Interop.HttpApi.GetChunks(_httpContext.Request.RequestBuffer, _httpContext.Request.OriginalBlobAddress, ref _dataChunkIndex, ref _dataChunkOffset, buffer, offset, size);
	}
}
