using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Net;

internal sealed class HttpResponseStream : Stream
{
	private bool _closed;

	private readonly HttpListenerContext _httpContext;

	private long _leftToWrite = long.MinValue;

	private bool _inOpaqueMode;

	private HttpResponseStreamAsyncResult _lastWrite;

	private static readonly byte[] s_chunkTerminator = new byte[5] { 48, 13, 10, 13, 10 };

	internal bool Closed => _closed;

	public override bool CanRead => false;

	public override bool CanSeek => false;

	public override bool CanWrite => true;

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

	internal HttpListenerContext InternalHttpContext => _httpContext;

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

	public override int Read(byte[] buffer, int offset, int size)
	{
		throw new InvalidOperationException(System.SR.net_writeonlystream);
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
	{
		throw new InvalidOperationException(System.SR.net_writeonlystream);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		throw new InvalidOperationException(System.SR.net_writeonlystream);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "buffer.Length:" + buffer.Length + " count:" + count + " offset:" + offset, "Write");
		}
		if (!_closed)
		{
			WriteCore(buffer, offset, count);
		}
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "buffer.Length:" + buffer.Length + " count:" + count + " offset:" + offset, "BeginWrite");
		}
		return BeginWriteCore(buffer, offset, count, callback, state);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"asyncResult:{asyncResult}", "EndWrite");
		}
		if (asyncResult == null)
		{
			throw new ArgumentNullException("asyncResult");
		}
		EndWriteCore(asyncResult);
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "_closed:" + _closed, "Dispose");
				}
				if (!_closed)
				{
					_closed = true;
					DisposeCore();
				}
			}
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	internal HttpResponseStream(HttpListenerContext httpContext)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"httpContect: {httpContext}", ".ctor");
		}
		_httpContext = httpContext;
	}

	internal global::Interop.HttpApi.HTTP_FLAGS ComputeLeftToWrite()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "_LeftToWrite:" + _leftToWrite, "ComputeLeftToWrite");
		}
		global::Interop.HttpApi.HTTP_FLAGS result = global::Interop.HttpApi.HTTP_FLAGS.NONE;
		if (!_httpContext.Response.ComputedHeaders)
		{
			result = _httpContext.Response.ComputeHeaders();
		}
		if (_leftToWrite == long.MinValue)
		{
			global::Interop.HttpApi.HTTP_VERB knownMethod = _httpContext.GetKnownMethod();
			_leftToWrite = ((knownMethod != global::Interop.HttpApi.HTTP_VERB.HttpVerbHEAD) ? _httpContext.Response.ContentLength64 : 0);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "_LeftToWrite:" + _leftToWrite, "ComputeLeftToWrite");
			}
		}
		return result;
	}

	internal void SetClosedFlag()
	{
		_closed = true;
	}

	private unsafe void WriteCore(byte[] buffer, int offset, int size)
	{
		global::Interop.HttpApi.HTTP_FLAGS hTTP_FLAGS = ComputeLeftToWrite();
		if (size == 0 && _leftToWrite != 0L)
		{
			return;
		}
		if (_leftToWrite >= 0 && size > _leftToWrite)
		{
			throw new ProtocolViolationException(System.SR.net_entitytoobig);
		}
		uint num = (uint)size;
		Microsoft.Win32.SafeHandles.SafeLocalAllocHandle safeLocalAllocHandle = null;
		IntPtr zero = IntPtr.Zero;
		bool sentHeaders = _httpContext.Response.SentHeaders;
		uint num2;
		try
		{
			if (size == 0)
			{
				num2 = _httpContext.Response.SendHeaders(null, null, hTTP_FLAGS, isWebSocketHandshake: false);
			}
			else
			{
				fixed (byte* ptr = buffer)
				{
					byte* ptr2 = ptr;
					if (_httpContext.Response.BoundaryType == BoundaryType.Chunked)
					{
						string text = size.ToString("x", CultureInfo.InvariantCulture);
						num += (uint)(text.Length + 4);
						safeLocalAllocHandle = Microsoft.Win32.SafeHandles.SafeLocalAllocHandle.LocalAlloc((int)num);
						zero = safeLocalAllocHandle.DangerousGetHandle();
						for (int i = 0; i < text.Length; i++)
						{
							Marshal.WriteByte(zero, i, (byte)text[i]);
						}
						Marshal.WriteInt16(zero, text.Length, 2573);
						Marshal.Copy(buffer, offset, zero + text.Length + 2, size);
						Marshal.WriteInt16(zero, (int)(num - 2), 2573);
						ptr2 = (byte*)(void*)zero;
						offset = 0;
					}
					global::Interop.HttpApi.HTTP_DATA_CHUNK hTTP_DATA_CHUNK = default(global::Interop.HttpApi.HTTP_DATA_CHUNK);
					hTTP_DATA_CHUNK.DataChunkType = global::Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
					hTTP_DATA_CHUNK.pBuffer = ptr2 + offset;
					hTTP_DATA_CHUNK.BufferLength = num;
					hTTP_FLAGS |= ((_leftToWrite != size) ? global::Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA : global::Interop.HttpApi.HTTP_FLAGS.NONE);
					if (!sentHeaders)
					{
						num2 = _httpContext.Response.SendHeaders(&hTTP_DATA_CHUNK, null, hTTP_FLAGS, isWebSocketHandshake: false);
					}
					else
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, "Calling Interop.HttpApi.HttpSendResponseEntityBody", "WriteCore");
						}
						num2 = global::Interop.HttpApi.HttpSendResponseEntityBody(_httpContext.RequestQueueHandle, _httpContext.RequestId, (uint)hTTP_FLAGS, 1, &hTTP_DATA_CHUNK, null, Microsoft.Win32.SafeHandles.SafeLocalAllocHandle.Zero, 0u, null, null);
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, "Call to Interop.HttpApi.HttpSendResponseEntityBody returned:" + num2, "WriteCore");
						}
						if (_httpContext.Listener.IgnoreWriteExceptions)
						{
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, "Write() suppressing error", "WriteCore");
							}
							num2 = 0u;
						}
					}
				}
			}
		}
		finally
		{
			safeLocalAllocHandle?.Close();
		}
		if (num2 != 0 && num2 != 38)
		{
			Exception ex = new HttpListenerException((int)num2);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex.ToString(), "WriteCore");
			}
			_closed = true;
			_httpContext.Abort();
			throw ex;
		}
		UpdateAfterWrite(num);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.DumpBuffer(this, buffer, offset, (int)num, "WriteCore");
		}
	}

	private unsafe IAsyncResult BeginWriteCore(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
	{
		global::Interop.HttpApi.HTTP_FLAGS hTTP_FLAGS = ComputeLeftToWrite();
		if (_closed || (size == 0 && _leftToWrite != 0L))
		{
			HttpResponseStreamAsyncResult httpResponseStreamAsyncResult = new HttpResponseStreamAsyncResult(this, state, callback);
			httpResponseStreamAsyncResult.InvokeCallback(0u);
			return httpResponseStreamAsyncResult;
		}
		if (_leftToWrite >= 0 && size > _leftToWrite)
		{
			throw new ProtocolViolationException(System.SR.net_entitytoobig);
		}
		uint numBytes = 0u;
		hTTP_FLAGS |= ((_leftToWrite != size) ? global::Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA : global::Interop.HttpApi.HTTP_FLAGS.NONE);
		bool sentHeaders = _httpContext.Response.SentHeaders;
		HttpResponseStreamAsyncResult httpResponseStreamAsyncResult2 = new HttpResponseStreamAsyncResult(this, state, callback, buffer, offset, size, _httpContext.Response.BoundaryType == BoundaryType.Chunked, sentHeaders, _httpContext.RequestQueueBoundHandle);
		UpdateAfterWrite((_httpContext.Response.BoundaryType != BoundaryType.Chunked) ? ((uint)size) : 0u);
		uint num;
		try
		{
			if (!sentHeaders)
			{
				num = _httpContext.Response.SendHeaders(null, httpResponseStreamAsyncResult2, hTTP_FLAGS, isWebSocketHandshake: false);
			}
			else
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Calling Interop.HttpApi.HttpSendResponseEntityBody", "BeginWriteCore");
				}
				num = global::Interop.HttpApi.HttpSendResponseEntityBody(_httpContext.RequestQueueHandle, _httpContext.RequestId, (uint)hTTP_FLAGS, httpResponseStreamAsyncResult2.dataChunkCount, httpResponseStreamAsyncResult2.pDataChunks, &numBytes, Microsoft.Win32.SafeHandles.SafeLocalAllocHandle.Zero, 0u, httpResponseStreamAsyncResult2._pOverlapped, null);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Call to Interop.HttpApi.HttpSendResponseEntityBody returned:" + num, "BeginWriteCore");
				}
			}
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex.ToString(), "BeginWriteCore");
			}
			httpResponseStreamAsyncResult2.InternalCleanup();
			_closed = true;
			_httpContext.Abort();
			throw;
		}
		if (num != 0 && num != 997)
		{
			httpResponseStreamAsyncResult2.InternalCleanup();
			if (!(_httpContext.Listener.IgnoreWriteExceptions && sentHeaders))
			{
				Exception ex2 = new HttpListenerException((int)num);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(this, ex2.ToString(), "BeginWriteCore");
				}
				_closed = true;
				_httpContext.Abort();
				throw ex2;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "BeginWrite() Suppressing error", "BeginWriteCore");
			}
		}
		if (num == 0 && HttpListener.SkipIOCPCallbackOnSuccess)
		{
			httpResponseStreamAsyncResult2.IOCompleted(num, numBytes);
		}
		if ((hTTP_FLAGS & global::Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA) == 0)
		{
			_lastWrite = httpResponseStreamAsyncResult2;
		}
		return httpResponseStreamAsyncResult2;
	}

	private void EndWriteCore(IAsyncResult asyncResult)
	{
		if (!(asyncResult is HttpResponseStreamAsyncResult httpResponseStreamAsyncResult) || httpResponseStreamAsyncResult.AsyncObject != this)
		{
			throw new ArgumentException(System.SR.net_io_invalidasyncresult, "asyncResult");
		}
		if (httpResponseStreamAsyncResult.EndCalled)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidendcall, "EndWrite"));
		}
		httpResponseStreamAsyncResult.EndCalled = true;
		object obj = httpResponseStreamAsyncResult.InternalWaitForCompletion();
		if (obj is Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, "Rethrowing exception:" + ex, "EndWriteCore");
			}
			_closed = true;
			_httpContext.Abort();
			ExceptionDispatchInfo.Throw(ex);
		}
	}

	private void UpdateAfterWrite(uint dataWritten)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "dataWritten:" + dataWritten + " _leftToWrite:" + _leftToWrite + " _closed:" + _closed, "UpdateAfterWrite");
		}
		if (!_inOpaqueMode)
		{
			if (_leftToWrite > 0)
			{
				_leftToWrite -= dataWritten;
			}
			if (_leftToWrite == 0L)
			{
				_closed = true;
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "dataWritten:" + dataWritten + " _leftToWrite:" + _leftToWrite + " _closed:" + _closed, "UpdateAfterWrite");
		}
	}

	private unsafe void DisposeCore()
	{
		global::Interop.HttpApi.HTTP_FLAGS hTTP_FLAGS = ComputeLeftToWrite();
		if (_leftToWrite > 0 && !_inOpaqueMode)
		{
			throw new InvalidOperationException(System.SR.net_io_notenoughbyteswritten);
		}
		bool sentHeaders = _httpContext.Response.SentHeaders;
		if (sentHeaders && _leftToWrite == 0L)
		{
			return;
		}
		uint num = 0u;
		if ((_httpContext.Response.BoundaryType == BoundaryType.Chunked || _httpContext.Response.BoundaryType == BoundaryType.None) && !string.Equals(_httpContext.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
		{
			if (_httpContext.Response.BoundaryType == BoundaryType.None)
			{
				hTTP_FLAGS |= global::Interop.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY;
			}
			fixed (byte* ptr = &s_chunkTerminator[0])
			{
				void* pBuffer = ptr;
				global::Interop.HttpApi.HTTP_DATA_CHUNK* ptr2 = null;
				if (_httpContext.Response.BoundaryType == BoundaryType.Chunked)
				{
					global::Interop.HttpApi.HTTP_DATA_CHUNK hTTP_DATA_CHUNK = default(global::Interop.HttpApi.HTTP_DATA_CHUNK);
					hTTP_DATA_CHUNK.DataChunkType = global::Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
					hTTP_DATA_CHUNK.pBuffer = (byte*)pBuffer;
					hTTP_DATA_CHUNK.BufferLength = (uint)s_chunkTerminator.Length;
					ptr2 = &hTTP_DATA_CHUNK;
				}
				if (!sentHeaders)
				{
					num = _httpContext.Response.SendHeaders(ptr2, null, hTTP_FLAGS, isWebSocketHandshake: false);
				}
				else
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, "Calling Interop.HttpApi.HttpSendResponseEntityBody", "DisposeCore");
					}
					num = global::Interop.HttpApi.HttpSendResponseEntityBody(_httpContext.RequestQueueHandle, _httpContext.RequestId, (uint)hTTP_FLAGS, (ushort)((ptr2 != null) ? 1 : 0), ptr2, null, Microsoft.Win32.SafeHandles.SafeLocalAllocHandle.Zero, 0u, null, null);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, "Call to Interop.HttpApi.HttpSendResponseEntityBody returned:" + num, "DisposeCore");
					}
					if (_httpContext.Listener.IgnoreWriteExceptions)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, "Suppressing error", "DisposeCore");
						}
						num = 0u;
					}
				}
			}
		}
		else if (!sentHeaders)
		{
			num = _httpContext.Response.SendHeaders(null, null, hTTP_FLAGS, isWebSocketHandshake: false);
		}
		if (num != 0 && num != 38)
		{
			Exception ex = new HttpListenerException((int)num);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex.ToString(), "DisposeCore");
			}
			_httpContext.Abort();
			throw ex;
		}
		_leftToWrite = 0L;
	}

	internal void SwitchToOpaqueMode()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, "SwitchToOpaqueMode");
		}
		_inOpaqueMode = true;
		_leftToWrite = long.MaxValue;
	}

	internal unsafe void CancelLastWrite(SafeHandle requestQueueHandle)
	{
		HttpResponseStreamAsyncResult lastWrite = _lastWrite;
		if (lastWrite != null && !lastWrite.IsCompleted)
		{
			global::Interop.Kernel32.CancelIoEx(requestQueueHandle, lastWrite._pOverlapped);
		}
	}
}
