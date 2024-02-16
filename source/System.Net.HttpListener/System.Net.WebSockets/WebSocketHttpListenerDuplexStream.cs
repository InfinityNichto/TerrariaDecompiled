using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Net.WebSockets;

internal sealed class WebSocketHttpListenerDuplexStream : Stream, WebSocketBase.IWebSocketStream
{
	internal sealed class HttpListenerAsyncEventArgs : EventArgs, IDisposable
	{
		public enum HttpListenerAsyncOperation
		{
			None,
			Receive,
			Send
		}

		private int _operating;

		private bool _disposeCalled;

		private unsafe NativeOverlapped* _ptrNativeOverlapped;

		private ThreadPoolBoundHandle _boundHandle;

		private byte[] _buffer;

		private IList<ArraySegment<byte>> _bufferList;

		private int _count;

		private int _offset;

		private int _bytesTransferred;

		private HttpListenerAsyncOperation _completedOperation;

		private global::Interop.HttpApi.HTTP_DATA_CHUNK[] _dataChunks;

		private GCHandle _dataChunksGCHandle;

		private ushort _dataChunkCount;

		private Exception _exception;

		private bool _shouldCloseOutput;

		private readonly WebSocketBase _webSocket;

		private readonly WebSocketHttpListenerDuplexStream _currentStream;

		public int BytesTransferred => _bytesTransferred;

		public byte[] Buffer => _buffer;

		public IList<ArraySegment<byte>> BufferList
		{
			get
			{
				return _bufferList;
			}
			set
			{
				_bufferList = value;
			}
		}

		public bool ShouldCloseOutput => _shouldCloseOutput;

		public int Offset => _offset;

		public int Count => _count;

		public Exception Exception => _exception;

		public ushort EntityChunkCount
		{
			get
			{
				if (_dataChunks == null)
				{
					return 0;
				}
				return _dataChunkCount;
			}
		}

		internal unsafe NativeOverlapped* NativeOverlapped => _ptrNativeOverlapped;

		public IntPtr EntityChunks
		{
			get
			{
				if (_dataChunks == null)
				{
					return IntPtr.Zero;
				}
				return Marshal.UnsafeAddrOfPinnedArrayElement(_dataChunks, 0);
			}
		}

		public WebSocketHttpListenerDuplexStream CurrentStream => _currentStream;

		private event EventHandler<HttpListenerAsyncEventArgs> m_Completed;

		public event EventHandler<HttpListenerAsyncEventArgs> Completed
		{
			add
			{
				m_Completed += value;
			}
			remove
			{
				m_Completed -= value;
			}
		}

		public HttpListenerAsyncEventArgs(WebSocketBase webSocket, WebSocketHttpListenerDuplexStream stream)
		{
			_webSocket = webSocket;
			_currentStream = stream;
		}

		private void OnCompleted(HttpListenerAsyncEventArgs e)
		{
			this.m_Completed?.Invoke(e._currentStream, e);
		}

		public void SetShouldCloseOutput()
		{
			_bufferList = null;
			_buffer = null;
			_shouldCloseOutput = true;
		}

		public void Dispose()
		{
			_disposeCalled = true;
			if (Interlocked.CompareExchange(ref _operating, 2, 0) == 0)
			{
				GC.SuppressFinalize(this);
			}
		}

		private unsafe void InitializeOverlapped(ThreadPoolBoundHandle boundHandle)
		{
			_boundHandle = boundHandle;
			_ptrNativeOverlapped = boundHandle.AllocateNativeOverlapped(CompletionPortCallback, null, null);
		}

		private unsafe void FreeOverlapped(bool checkForShutdown)
		{
			if (!checkForShutdown || !Environment.HasShutdownStarted)
			{
				if (_ptrNativeOverlapped != null)
				{
					_boundHandle.FreeNativeOverlapped(_ptrNativeOverlapped);
					_ptrNativeOverlapped = null;
				}
				if (_dataChunksGCHandle.IsAllocated)
				{
					_dataChunksGCHandle.Free();
					_dataChunks = null;
				}
			}
		}

		internal void StartOperationCommon(WebSocketHttpListenerDuplexStream currentStream, ThreadPoolBoundHandle boundHandle)
		{
			if (Interlocked.CompareExchange(ref _operating, 1, 0) != 0)
			{
				if (_disposeCalled)
				{
					throw new ObjectDisposedException(GetType().FullName);
				}
				throw new InvalidOperationException();
			}
			InitializeOverlapped(boundHandle);
			_exception = null;
			_bytesTransferred = 0;
		}

		internal void StartOperationReceive()
		{
			_completedOperation = HttpListenerAsyncOperation.Receive;
		}

		internal void StartOperationSend()
		{
			UpdateDataChunk();
			_completedOperation = HttpListenerAsyncOperation.Send;
		}

		public void SetBuffer(byte[] buffer, int offset, int count)
		{
			_buffer = buffer;
			_offset = offset;
			_count = count;
		}

		private void UpdateDataChunk()
		{
			if (_dataChunks == null)
			{
				_dataChunks = new global::Interop.HttpApi.HTTP_DATA_CHUNK[2];
				_dataChunksGCHandle = GCHandle.Alloc(_dataChunks, GCHandleType.Pinned);
				_dataChunks[0] = default(global::Interop.HttpApi.HTTP_DATA_CHUNK);
				_dataChunks[0].DataChunkType = global::Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
				_dataChunks[1] = default(global::Interop.HttpApi.HTTP_DATA_CHUNK);
				_dataChunks[1].DataChunkType = global::Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
			}
			if (_buffer != null)
			{
				UpdateDataChunk(0, _buffer, _offset, _count);
				UpdateDataChunk(1, null, 0, 0);
				_dataChunkCount = 1;
			}
			else if (_bufferList != null)
			{
				UpdateDataChunk(0, _bufferList[0].Array, _bufferList[0].Offset, _bufferList[0].Count);
				UpdateDataChunk(1, _bufferList[1].Array, _bufferList[1].Offset, _bufferList[1].Count);
				_dataChunkCount = 2;
			}
			else
			{
				_dataChunks = null;
			}
		}

		private unsafe void UpdateDataChunk(int index, byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				_dataChunks[index].pBuffer = null;
				_dataChunks[index].BufferLength = 0u;
				return;
			}
			if (_webSocket.InternalBuffer.IsInternalBuffer(buffer, offset, count))
			{
				_dataChunks[index].pBuffer = (byte*)(void*)_webSocket.InternalBuffer.ToIntPtr(offset);
			}
			else
			{
				_dataChunks[index].pBuffer = (byte*)(void*)_webSocket.InternalBuffer.ConvertPinnedSendPayloadToNative(buffer, offset, count);
			}
			_dataChunks[index].BufferLength = (uint)count;
		}

		internal void Complete()
		{
			FreeOverlapped(checkForShutdown: false);
			Interlocked.Exchange(ref _operating, 0);
			if (_disposeCalled)
			{
				Dispose();
			}
		}

		private void SetResults(Exception exception, int bytesTransferred)
		{
			_exception = exception;
			_bytesTransferred = bytesTransferred;
		}

		internal void FinishOperationFailure(Exception exception, bool syncCompletion)
		{
			SetResults(exception, 0);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				string text = ((_completedOperation == HttpListenerAsyncOperation.Receive) ? "ReadAsyncFast" : "WriteAsyncFast");
				System.Net.NetEventSource.Error(_currentStream, $"{text} {exception}", "FinishOperationFailure");
			}
			Complete();
			OnCompleted(this);
		}

		internal void FinishOperationSuccess(int bytesTransferred, bool syncCompletion)
		{
			SetResults(null, bytesTransferred);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				if (_buffer != null && System.Net.NetEventSource.Log.IsEnabled())
				{
					string memberName = ((_completedOperation == HttpListenerAsyncOperation.Receive) ? "ReadAsyncFast" : "WriteAsyncFast");
					System.Net.NetEventSource.DumpBuffer(_currentStream, _buffer, _offset, bytesTransferred, memberName);
				}
				else if (_bufferList != null)
				{
					foreach (ArraySegment<byte> buffer in BufferList)
					{
						System.Net.NetEventSource.DumpBuffer(this, buffer.Array, buffer.Offset, buffer.Count, "WriteAsyncFast");
					}
				}
			}
			if (_shouldCloseOutput)
			{
				_currentStream._outputStream.SetClosedFlag();
			}
			Complete();
			OnCompleted(this);
		}

		private unsafe void CompletionPortCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
		{
			if (errorCode == 0 || errorCode == 38)
			{
				FinishOperationSuccess((int)numBytes, syncCompletion: false);
			}
			else
			{
				FinishOperationFailure(new HttpListenerException((int)errorCode), syncCompletion: false);
			}
		}
	}

	private static readonly EventHandler<HttpListenerAsyncEventArgs> s_OnReadCompleted = OnReadCompleted;

	private static readonly EventHandler<HttpListenerAsyncEventArgs> s_OnWriteCompleted = OnWriteCompleted;

	private static readonly Func<Exception, bool> s_CanHandleException = CanHandleException;

	private static readonly Action<object> s_OnCancel = OnCancel;

	private readonly HttpRequestStream _inputStream;

	private readonly HttpResponseStream _outputStream;

	private readonly HttpListenerContext _context;

	private bool _inOpaqueMode;

	private WebSocketBase _webSocket;

	private HttpListenerAsyncEventArgs _writeEventArgs;

	private HttpListenerAsyncEventArgs _readEventArgs;

	private TaskCompletionSource _writeTaskCompletionSource;

	private TaskCompletionSource<int> _readTaskCompletionSource;

	private int _cleanedUp;

	public override bool CanRead => _inputStream.CanRead;

	public override bool CanSeek => false;

	public override bool CanTimeout
	{
		get
		{
			if (_inputStream.CanTimeout)
			{
				return _outputStream.CanTimeout;
			}
			return false;
		}
	}

	public override bool CanWrite => _outputStream.CanWrite;

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

	public bool SupportsMultipleWrite => true;

	public WebSocketHttpListenerDuplexStream(HttpRequestStream inputStream, HttpResponseStream outputStream, HttpListenerContext context)
	{
		_inputStream = inputStream;
		_outputStream = outputStream;
		_context = context;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Associate(inputStream, this, ".ctor");
			System.Net.NetEventSource.Associate(outputStream, this, ".ctor");
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		return _inputStream.Read(buffer, offset, count);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		System.Net.WebSockets.WebSocketValidate.ValidateBuffer(buffer, offset, count);
		return ReadAsyncCore(buffer, offset, count, cancellationToken);
	}

	private async Task<int> ReadAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
		int result = 0;
		try
		{
			if (cancellationToken.CanBeCanceled)
			{
				cancellationTokenRegistration = cancellationToken.Register(s_OnCancel, this, useSynchronizationContext: false);
			}
			if (!_inOpaqueMode)
			{
				result = await _inputStream.ReadAsync(buffer, offset, count, cancellationToken).SuppressContextFlow();
			}
			else
			{
				_readTaskCompletionSource = new TaskCompletionSource<int>();
				_readEventArgs.SetBuffer(buffer, offset, count);
				if (!ReadAsyncFast(_readEventArgs))
				{
					if (_readEventArgs.Exception != null)
					{
						throw _readEventArgs.Exception;
					}
					result = _readEventArgs.BytesTransferred;
				}
				else
				{
					result = await _readTaskCompletionSource.Task.SuppressContextFlow();
				}
			}
		}
		catch (Exception arg)
		{
			if (s_CanHandleException(arg))
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			throw;
		}
		finally
		{
			cancellationTokenRegistration.Dispose();
		}
		return result;
	}

	private unsafe bool ReadAsyncFast(HttpListenerAsyncEventArgs eventArgs)
	{
		eventArgs.StartOperationCommon(this, _inputStream.InternalHttpContext.RequestQueueBoundHandle);
		eventArgs.StartOperationReceive();
		uint num = 0u;
		bool flag = false;
		try
		{
			if (eventArgs.Count == 0 || _inputStream.Closed)
			{
				eventArgs.FinishOperationSuccess(0, syncCompletion: true);
				return false;
			}
			uint num2 = 0u;
			int offset = eventArgs.Offset;
			int count = eventArgs.Count;
			if (_inputStream.BufferedDataChunksAvailable)
			{
				num2 = _inputStream.GetChunks(eventArgs.Buffer, eventArgs.Offset, eventArgs.Count);
				if (_inputStream.BufferedDataChunksAvailable && num2 == eventArgs.Count)
				{
					eventArgs.FinishOperationSuccess(eventArgs.Count, syncCompletion: true);
					return false;
				}
			}
			if (num2 != 0)
			{
				offset += (int)num2;
				count -= (int)num2;
				if (count > 131072)
				{
					count = 131072;
				}
				eventArgs.SetBuffer(eventArgs.Buffer, offset, count);
			}
			else if (count > 131072)
			{
				count = 131072;
				eventArgs.SetBuffer(eventArgs.Buffer, offset, count);
			}
			uint flags = 0u;
			uint bytesReturned = 0u;
			num = global::Interop.HttpApi.HttpReceiveRequestEntityBody(_inputStream.InternalHttpContext.RequestQueueHandle, _inputStream.InternalHttpContext.RequestId, flags, (void*)_webSocket.InternalBuffer.ToIntPtr(eventArgs.Offset), (uint)eventArgs.Count, out bytesReturned, eventArgs.NativeOverlapped);
			if (num != 0 && num != 997 && num != 38)
			{
				throw new HttpListenerException((int)num);
			}
			if (num == 0 && HttpListener.SkipIOCPCallbackOnSuccess)
			{
				eventArgs.FinishOperationSuccess((int)bytesReturned, syncCompletion: true);
				return false;
			}
			if (num == 38)
			{
				eventArgs.FinishOperationSuccess(0, syncCompletion: true);
				return false;
			}
			return true;
		}
		catch (Exception exception)
		{
			_readEventArgs.FinishOperationFailure(exception, syncCompletion: true);
			_outputStream.SetClosedFlag();
			_outputStream.InternalHttpContext.Abort();
			return true;
		}
	}

	public override int ReadByte()
	{
		return _inputStream.ReadByte();
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return _inputStream.BeginRead(buffer, offset, count, callback, state);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return _inputStream.EndRead(asyncResult);
	}

	public Task MultipleWriteAsync(IList<ArraySegment<byte>> sendBuffers, CancellationToken cancellationToken)
	{
		if (sendBuffers.Count == 1)
		{
			ArraySegment<byte> arraySegment = sendBuffers[0];
			return WriteAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, cancellationToken);
		}
		return MultipleWriteAsyncCore(sendBuffers, cancellationToken);
	}

	private async Task MultipleWriteAsyncCore(IList<ArraySegment<byte>> sendBuffers, CancellationToken cancellationToken)
	{
		CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
		try
		{
			if (cancellationToken.CanBeCanceled)
			{
				cancellationTokenRegistration = cancellationToken.Register(s_OnCancel, this, useSynchronizationContext: false);
			}
			_writeTaskCompletionSource = new TaskCompletionSource();
			_writeEventArgs.SetBuffer(null, 0, 0);
			_writeEventArgs.BufferList = sendBuffers;
			if (WriteAsyncFast(_writeEventArgs))
			{
				await _writeTaskCompletionSource.Task.SuppressContextFlow();
			}
		}
		catch (Exception arg)
		{
			if (s_CanHandleException(arg))
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			throw;
		}
		finally
		{
			cancellationTokenRegistration.Dispose();
		}
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		_outputStream.Write(buffer, offset, count);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		System.Net.WebSockets.WebSocketValidate.ValidateBuffer(buffer, offset, count);
		return WriteAsyncCore(buffer, offset, count, cancellationToken);
	}

	private async Task WriteAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
		try
		{
			if (cancellationToken.CanBeCanceled)
			{
				cancellationTokenRegistration = cancellationToken.Register(s_OnCancel, this, useSynchronizationContext: false);
			}
			if (!_inOpaqueMode)
			{
				await _outputStream.WriteAsync(buffer, offset, count, cancellationToken).SuppressContextFlow();
				return;
			}
			_writeTaskCompletionSource = new TaskCompletionSource();
			_writeEventArgs.BufferList = null;
			_writeEventArgs.SetBuffer(buffer, offset, count);
			if (WriteAsyncFast(_writeEventArgs))
			{
				await _writeTaskCompletionSource.Task.SuppressContextFlow();
			}
		}
		catch (Exception arg)
		{
			if (s_CanHandleException(arg))
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			throw;
		}
		finally
		{
			cancellationTokenRegistration.Dispose();
		}
	}

	private unsafe bool WriteAsyncFast(HttpListenerAsyncEventArgs eventArgs)
	{
		global::Interop.HttpApi.HTTP_FLAGS hTTP_FLAGS = global::Interop.HttpApi.HTTP_FLAGS.NONE;
		eventArgs.StartOperationCommon(this, _outputStream.InternalHttpContext.RequestQueueBoundHandle);
		eventArgs.StartOperationSend();
		bool flag = false;
		try
		{
			if (_outputStream.Closed || (eventArgs.Buffer != null && eventArgs.Count == 0))
			{
				eventArgs.FinishOperationSuccess(eventArgs.Count, syncCompletion: true);
				return false;
			}
			if (eventArgs.ShouldCloseOutput)
			{
				hTTP_FLAGS |= global::Interop.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY;
			}
			else
			{
				hTTP_FLAGS |= global::Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA;
				hTTP_FLAGS |= global::Interop.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA;
			}
			Unsafe.SkipInit(out uint bytesTransferred);
			uint num = global::Interop.HttpApi.HttpSendResponseEntityBody(_outputStream.InternalHttpContext.RequestQueueHandle, _outputStream.InternalHttpContext.RequestId, (uint)hTTP_FLAGS, eventArgs.EntityChunkCount, (global::Interop.HttpApi.HTTP_DATA_CHUNK*)(void*)eventArgs.EntityChunks, &bytesTransferred, Microsoft.Win32.SafeHandles.SafeLocalAllocHandle.Zero, 0u, eventArgs.NativeOverlapped, null);
			if (num != 0 && num != 997)
			{
				throw new HttpListenerException((int)num);
			}
			if (num == 0 && HttpListener.SkipIOCPCallbackOnSuccess)
			{
				eventArgs.FinishOperationSuccess((int)bytesTransferred, syncCompletion: true);
				return false;
			}
			return true;
		}
		catch (Exception exception)
		{
			_writeEventArgs.FinishOperationFailure(exception, syncCompletion: true);
			_outputStream.SetClosedFlag();
			_outputStream.InternalHttpContext.Abort();
			return true;
		}
	}

	public override void WriteByte(byte value)
	{
		_outputStream.WriteByte(value);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return _outputStream.BeginWrite(buffer, offset, count, callback, state);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		_outputStream.EndWrite(asyncResult);
	}

	public override void Flush()
	{
		_outputStream.Flush();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		return _outputStream.FlushAsync(cancellationToken);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException(System.SR.net_noseek);
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException(System.SR.net_noseek);
	}

	public async Task CloseNetworkConnectionAsync(CancellationToken cancellationToken)
	{
		await Task.Yield();
		CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
		try
		{
			if (cancellationToken.CanBeCanceled)
			{
				cancellationTokenRegistration = cancellationToken.Register(s_OnCancel, this, useSynchronizationContext: false);
			}
			_writeTaskCompletionSource = new TaskCompletionSource();
			_writeEventArgs.SetShouldCloseOutput();
			if (WriteAsyncFast(_writeEventArgs))
			{
				await _writeTaskCompletionSource.Task.SuppressContextFlow();
			}
		}
		catch (Exception arg)
		{
			if (!s_CanHandleException(arg))
			{
				throw;
			}
			cancellationToken.ThrowIfCancellationRequested();
		}
		finally
		{
			cancellationTokenRegistration.Dispose();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && Interlocked.Exchange(ref _cleanedUp, 1) == 0)
		{
			if (_readTaskCompletionSource != null)
			{
				_readTaskCompletionSource.TrySetCanceled();
			}
			_writeTaskCompletionSource?.TrySetCanceled();
			if (_readEventArgs != null)
			{
				_readEventArgs.Dispose();
			}
			if (_writeEventArgs != null)
			{
				_writeEventArgs.Dispose();
			}
			try
			{
				_inputStream.Close();
			}
			finally
			{
				_outputStream.Close();
			}
		}
	}

	public void Abort()
	{
		OnCancel(this);
	}

	private static bool CanHandleException(Exception error)
	{
		if (!(error is HttpListenerException) && !(error is ObjectDisposedException))
		{
			return error is IOException;
		}
		return true;
	}

	private static void OnCancel(object state)
	{
		WebSocketHttpListenerDuplexStream webSocketHttpListenerDuplexStream = state as WebSocketHttpListenerDuplexStream;
		try
		{
			webSocketHttpListenerDuplexStream._outputStream.SetClosedFlag();
			webSocketHttpListenerDuplexStream._context.Abort();
		}
		catch
		{
		}
		webSocketHttpListenerDuplexStream._readTaskCompletionSource?.TrySetCanceled();
		webSocketHttpListenerDuplexStream._writeTaskCompletionSource?.TrySetCanceled();
	}

	public void SwitchToOpaqueMode(WebSocketBase webSocket)
	{
		if (_inOpaqueMode)
		{
			throw new InvalidOperationException();
		}
		_webSocket = webSocket;
		_inOpaqueMode = true;
		_readEventArgs = new HttpListenerAsyncEventArgs(webSocket, this);
		_readEventArgs.Completed += s_OnReadCompleted;
		_writeEventArgs = new HttpListenerAsyncEventArgs(webSocket, this);
		_writeEventArgs.Completed += s_OnWriteCompleted;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Associate(this, webSocket, "SwitchToOpaqueMode");
		}
	}

	private static void OnWriteCompleted(object sender, HttpListenerAsyncEventArgs eventArgs)
	{
		WebSocketHttpListenerDuplexStream currentStream = eventArgs.CurrentStream;
		if (eventArgs.Exception != null)
		{
			currentStream._writeTaskCompletionSource.TrySetException(eventArgs.Exception);
		}
		else
		{
			currentStream._writeTaskCompletionSource.TrySetResult();
		}
	}

	private static void OnReadCompleted(object sender, HttpListenerAsyncEventArgs eventArgs)
	{
		WebSocketHttpListenerDuplexStream currentStream = eventArgs.CurrentStream;
		if (eventArgs.Exception != null)
		{
			currentStream._readTaskCompletionSource.TrySetException(eventArgs.Exception);
		}
		else
		{
			currentStream._readTaskCompletionSource.TrySetResult(eventArgs.BytesTransferred);
		}
	}
}
