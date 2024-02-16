using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public abstract class HttpContent : IDisposable
{
	internal sealed class LimitMemoryStream : MemoryStream
	{
		private readonly int _maxSize;

		public LimitMemoryStream(int maxSize, int capacity)
			: base(capacity)
		{
			_maxSize = maxSize;
		}

		public byte[] GetSizedBuffer()
		{
			if (!TryGetBuffer(out var buffer) || buffer.Offset != 0 || buffer.Count != buffer.Array.Length)
			{
				return ToArray();
			}
			return buffer.Array;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			CheckSize(count);
			base.Write(buffer, offset, count);
		}

		public override void WriteByte(byte value)
		{
			CheckSize(1);
			base.WriteByte(value);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			CheckSize(count);
			return base.WriteAsync(buffer, offset, count, cancellationToken);
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
		{
			CheckSize(buffer.Length);
			return base.WriteAsync(buffer, cancellationToken);
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			CheckSize(count);
			return base.BeginWrite(buffer, offset, count, callback, state);
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			base.EndWrite(asyncResult);
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			if (TryGetBuffer(out var buffer))
			{
				Stream.ValidateCopyToArguments(destination, bufferSize);
				long position = Position;
				long num = (Position = Length);
				long num2 = num - position;
				return destination.WriteAsync(buffer.Array, (int)(buffer.Offset + position), (int)num2, cancellationToken);
			}
			return base.CopyToAsync(destination, bufferSize, cancellationToken);
		}

		private void CheckSize(int countToAdd)
		{
			if (_maxSize - Length < countToAdd)
			{
				throw CreateOverCapacityException(_maxSize);
			}
		}
	}

	internal sealed class LimitArrayPoolWriteStream : Stream
	{
		private readonly int _maxBufferSize;

		private byte[] _buffer;

		private int _length;

		public override long Length => _length;

		public override bool CanWrite => true;

		public override bool CanRead => false;

		public override bool CanSeek => false;

		public override long Position
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public LimitArrayPoolWriteStream(int maxBufferSize)
			: this(maxBufferSize, 256L)
		{
		}

		public LimitArrayPoolWriteStream(int maxBufferSize, long capacity)
		{
			if (capacity < 256)
			{
				capacity = 256L;
			}
			else if (capacity > maxBufferSize)
			{
				throw CreateOverCapacityException(maxBufferSize);
			}
			_maxBufferSize = maxBufferSize;
			_buffer = ArrayPool<byte>.Shared.Rent((int)capacity);
		}

		protected override void Dispose(bool disposing)
		{
			ArrayPool<byte>.Shared.Return(_buffer);
			_buffer = null;
			base.Dispose(disposing);
		}

		public ArraySegment<byte> GetBuffer()
		{
			return new ArraySegment<byte>(_buffer, 0, _length);
		}

		public byte[] ToArray()
		{
			byte[] array = new byte[_length];
			Buffer.BlockCopy(_buffer, 0, array, 0, _length);
			return array;
		}

		private void EnsureCapacity(int value)
		{
			if ((uint)value > (uint)_maxBufferSize)
			{
				throw CreateOverCapacityException(_maxBufferSize);
			}
			if (value > _buffer.Length)
			{
				Grow(value);
			}
		}

		private void Grow(int value)
		{
			byte[] buffer = _buffer;
			_buffer = null;
			uint num = (uint)(2 * buffer.Length);
			int minimumLength = ((num > Array.MaxLength) ? Math.Max(value, Array.MaxLength) : Math.Max(value, (int)num));
			byte[] array = ArrayPool<byte>.Shared.Rent(minimumLength);
			Buffer.BlockCopy(buffer, 0, array, 0, _length);
			ArrayPool<byte>.Shared.Return(buffer);
			_buffer = array;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			EnsureCapacity(_length + count);
			Buffer.BlockCopy(buffer, offset, _buffer, _length, count);
			_length += count;
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			EnsureCapacity(_length + buffer.Length);
			buffer.CopyTo(new Span<byte>(_buffer, _length, buffer.Length));
			_length += buffer.Length;
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			Write(buffer, offset, count);
			return Task.CompletedTask;
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
		{
			Write(buffer.Span);
			return default(ValueTask);
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState)
		{
			return System.Threading.Tasks.TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			System.Threading.Tasks.TaskToApm.End(asyncResult);
		}

		public override void WriteByte(byte value)
		{
			int num = _length + 1;
			EnsureCapacity(num);
			_buffer[_length] = value;
			_length = num;
		}

		public override void Flush()
		{
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}
	}

	private HttpContentHeaders _headers;

	private MemoryStream _bufferedContent;

	private object _contentReadStream;

	private bool _disposed;

	private bool _canCalculateLength;

	internal static readonly Encoding DefaultStringEncoding = Encoding.UTF8;

	public HttpContentHeaders Headers
	{
		get
		{
			if (_headers == null)
			{
				_headers = new HttpContentHeaders(this);
			}
			return _headers;
		}
	}

	private bool IsBuffered => _bufferedContent != null;

	internal virtual bool AllowDuplex => true;

	internal bool TryGetBuffer(out ArraySegment<byte> buffer)
	{
		if (_bufferedContent != null)
		{
			return _bufferedContent.TryGetBuffer(out buffer);
		}
		buffer = default(ArraySegment<byte>);
		return false;
	}

	protected HttpContent()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, ".ctor");
		}
		_canCalculateLength = true;
	}

	public Task<string> ReadAsStringAsync()
	{
		return ReadAsStringAsync(CancellationToken.None);
	}

	public Task<string> ReadAsStringAsync(CancellationToken cancellationToken)
	{
		CheckDisposed();
		return WaitAndReturnAsync(LoadIntoBufferAsync(cancellationToken), this, (HttpContent s) => s.ReadBufferedContentAsString());
	}

	private string ReadBufferedContentAsString()
	{
		if (_bufferedContent.Length == 0L)
		{
			return string.Empty;
		}
		if (!TryGetBuffer(out var buffer))
		{
			buffer = new ArraySegment<byte>(_bufferedContent.ToArray());
		}
		return ReadBufferAsString(buffer, Headers);
	}

	internal static string ReadBufferAsString(ArraySegment<byte> buffer, HttpContentHeaders headers)
	{
		Encoding encoding = null;
		int preambleLength = -1;
		string text = headers.ContentType?.CharSet;
		if (text != null)
		{
			try
			{
				encoding = ((text.Length <= 2 || text[0] != '"' || text[text.Length - 1] != '"') ? Encoding.GetEncoding(text) : Encoding.GetEncoding(text.Substring(1, text.Length - 2)));
				preambleLength = GetPreambleLength(buffer, encoding);
			}
			catch (ArgumentException innerException)
			{
				throw new InvalidOperationException(System.SR.net_http_content_invalid_charset, innerException);
			}
		}
		if (encoding == null && !TryDetectEncoding(buffer, out encoding, out preambleLength))
		{
			encoding = DefaultStringEncoding;
			preambleLength = 0;
		}
		return encoding.GetString(buffer.Array, buffer.Offset + preambleLength, buffer.Count - preambleLength);
	}

	public Task<byte[]> ReadAsByteArrayAsync()
	{
		return ReadAsByteArrayAsync(CancellationToken.None);
	}

	public Task<byte[]> ReadAsByteArrayAsync(CancellationToken cancellationToken)
	{
		CheckDisposed();
		return WaitAndReturnAsync(LoadIntoBufferAsync(cancellationToken), this, (HttpContent s) => s.ReadBufferedContentAsByteArray());
	}

	internal byte[] ReadBufferedContentAsByteArray()
	{
		return _bufferedContent.ToArray();
	}

	public Stream ReadAsStream()
	{
		return ReadAsStream(CancellationToken.None);
	}

	public Stream ReadAsStream(CancellationToken cancellationToken)
	{
		CheckDisposed();
		ArraySegment<byte> buffer;
		if (_contentReadStream == null)
		{
			return (Stream)(_contentReadStream = (TryGetBuffer(out buffer) ? new MemoryStream(buffer.Array, buffer.Offset, buffer.Count, writable: false) : CreateContentReadStream(cancellationToken)));
		}
		if (_contentReadStream is Stream result)
		{
			return result;
		}
		throw new HttpRequestException(System.SR.net_http_content_read_as_stream_has_task);
	}

	public Task<Stream> ReadAsStreamAsync()
	{
		return ReadAsStreamAsync(CancellationToken.None);
	}

	public Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken)
	{
		CheckDisposed();
		ArraySegment<byte> buffer;
		if (_contentReadStream == null)
		{
			return (Task<Stream>)(_contentReadStream = (TryGetBuffer(out buffer) ? ((Task)Task.FromResult((Stream)new MemoryStream(buffer.Array, buffer.Offset, buffer.Count, writable: false))) : ((Task)CreateContentReadStreamAsync(cancellationToken))));
		}
		if (_contentReadStream is Task<Stream> result)
		{
			return result;
		}
		return (Task<Stream>)(_contentReadStream = Task.FromResult((Stream)_contentReadStream));
	}

	internal Stream TryReadAsStream()
	{
		CheckDisposed();
		ArraySegment<byte> buffer;
		if (_contentReadStream == null)
		{
			return (Stream)(_contentReadStream = (TryGetBuffer(out buffer) ? new MemoryStream(buffer.Array, buffer.Offset, buffer.Count, writable: false) : TryCreateContentReadStream()));
		}
		if (_contentReadStream is Stream result)
		{
			return result;
		}
		Task<Stream> task = (Task<Stream>)_contentReadStream;
		if (task.Status != TaskStatus.RanToCompletion)
		{
			return null;
		}
		return task.Result;
	}

	protected abstract Task SerializeToStreamAsync(Stream stream, TransportContext? context);

	protected virtual void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.net_http_missing_sync_implementation, GetType(), "HttpContent", "SerializeToStream"));
	}

	protected virtual Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		return SerializeToStreamAsync(stream, context);
	}

	public void CopyTo(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		CheckDisposed();
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		try
		{
			if (TryGetBuffer(out var buffer))
			{
				stream.Write(buffer.Array, buffer.Offset, buffer.Count);
			}
			else
			{
				SerializeToStream(stream, context, cancellationToken);
			}
		}
		catch (Exception ex) when (StreamCopyExceptionNeedsWrapping(ex))
		{
			throw GetStreamCopyException(ex);
		}
	}

	public Task CopyToAsync(Stream stream)
	{
		return CopyToAsync(stream, CancellationToken.None);
	}

	public Task CopyToAsync(Stream stream, CancellationToken cancellationToken)
	{
		return CopyToAsync(stream, null, cancellationToken);
	}

	public Task CopyToAsync(Stream stream, TransportContext? context)
	{
		return CopyToAsync(stream, context, CancellationToken.None);
	}

	public Task CopyToAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		CheckDisposed();
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		try
		{
			return WaitAsync(InternalCopyToAsync(stream, context, cancellationToken));
		}
		catch (Exception ex) when (StreamCopyExceptionNeedsWrapping(ex))
		{
			return Task.FromException(GetStreamCopyException(ex));
		}
		static async Task WaitAsync(ValueTask copyTask)
		{
			try
			{
				await copyTask.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception e) when (StreamCopyExceptionNeedsWrapping(e))
			{
				throw WrapStreamCopyException(e);
			}
		}
	}

	internal ValueTask InternalCopyToAsync(Stream stream, TransportContext context, CancellationToken cancellationToken)
	{
		if (TryGetBuffer(out var buffer))
		{
			return stream.WriteAsync(buffer, cancellationToken);
		}
		Task task = SerializeToStreamAsync(stream, context, cancellationToken);
		CheckTaskNotNull(task);
		return new ValueTask(task);
	}

	internal void LoadIntoBuffer(long maxBufferSize, CancellationToken cancellationToken)
	{
		CheckDisposed();
		if (!CreateTemporaryBuffer(maxBufferSize, out var tempBuffer, out var error))
		{
			return;
		}
		if (tempBuffer == null)
		{
			throw error;
		}
		CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.Register(delegate(object s)
		{
			((HttpContent)s).Dispose();
		}, this);
		try
		{
			SerializeToStream(tempBuffer, null, cancellationToken);
			tempBuffer.Seek(0L, SeekOrigin.Begin);
			_bufferedContent = tempBuffer;
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex, "LoadIntoBuffer");
			}
			if (CancellationHelper.ShouldWrapInOperationCanceledException(ex, cancellationToken))
			{
				throw CancellationHelper.CreateOperationCanceledException(ex, cancellationToken);
			}
			if (StreamCopyExceptionNeedsWrapping(ex))
			{
				throw GetStreamCopyException(ex);
			}
			throw;
		}
		finally
		{
			cancellationTokenRegistration.Dispose();
		}
	}

	public Task LoadIntoBufferAsync()
	{
		return LoadIntoBufferAsync(2147483647L);
	}

	public Task LoadIntoBufferAsync(long maxBufferSize)
	{
		return LoadIntoBufferAsync(maxBufferSize, CancellationToken.None);
	}

	internal Task LoadIntoBufferAsync(CancellationToken cancellationToken)
	{
		return LoadIntoBufferAsync(2147483647L, cancellationToken);
	}

	internal Task LoadIntoBufferAsync(long maxBufferSize, CancellationToken cancellationToken)
	{
		CheckDisposed();
		if (!CreateTemporaryBuffer(maxBufferSize, out var tempBuffer, out var error))
		{
			return Task.CompletedTask;
		}
		if (tempBuffer == null)
		{
			return Task.FromException(error);
		}
		try
		{
			Task task = SerializeToStreamAsync(tempBuffer, null, cancellationToken);
			CheckTaskNotNull(task);
			return LoadIntoBufferAsyncCore(task, tempBuffer);
		}
		catch (Exception ex) when (StreamCopyExceptionNeedsWrapping(ex))
		{
			return Task.FromException(GetStreamCopyException(ex));
		}
	}

	private async Task LoadIntoBufferAsyncCore(Task serializeToStreamTask, MemoryStream tempBuffer)
	{
		try
		{
			await serializeToStreamTask.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			tempBuffer.Dispose();
			Exception streamCopyException = GetStreamCopyException(ex);
			if (streamCopyException != ex)
			{
				throw streamCopyException;
			}
			throw;
		}
		try
		{
			tempBuffer.Seek(0L, SeekOrigin.Begin);
			_bufferedContent = tempBuffer;
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, message, "LoadIntoBufferAsyncCore");
			}
			throw;
		}
	}

	protected virtual Stream CreateContentReadStream(CancellationToken cancellationToken)
	{
		LoadIntoBuffer(2147483647L, cancellationToken);
		return _bufferedContent;
	}

	protected virtual Task<Stream> CreateContentReadStreamAsync()
	{
		return WaitAndReturnAsync(LoadIntoBufferAsync(), this, (Func<HttpContent, Stream>)((HttpContent s) => s._bufferedContent));
	}

	protected virtual Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
	{
		return CreateContentReadStreamAsync();
	}

	internal virtual Stream TryCreateContentReadStream()
	{
		return null;
	}

	protected internal abstract bool TryComputeLength(out long length);

	internal long? GetComputedOrBufferLength()
	{
		CheckDisposed();
		if (IsBuffered)
		{
			return _bufferedContent.Length;
		}
		if (_canCalculateLength)
		{
			long length = 0L;
			if (TryComputeLength(out length))
			{
				return length;
			}
			_canCalculateLength = false;
		}
		return null;
	}

	private bool CreateTemporaryBuffer(long maxBufferSize, out MemoryStream tempBuffer, out Exception error)
	{
		if (maxBufferSize > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("maxBufferSize", maxBufferSize, System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_content_buffersize_limit, int.MaxValue));
		}
		if (IsBuffered)
		{
			tempBuffer = null;
			error = null;
			return false;
		}
		tempBuffer = CreateMemoryStream(maxBufferSize, out error);
		return true;
	}

	private MemoryStream CreateMemoryStream(long maxBufferSize, out Exception error)
	{
		error = null;
		long? contentLength = Headers.ContentLength;
		if (contentLength.HasValue)
		{
			if (contentLength > maxBufferSize)
			{
				error = new HttpRequestException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_content_buffersize_exceeded, maxBufferSize));
				return null;
			}
			return new LimitMemoryStream((int)maxBufferSize, (int)contentLength.Value);
		}
		return new LimitMemoryStream((int)maxBufferSize, 0);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && !_disposed)
		{
			_disposed = true;
			if (_contentReadStream != null)
			{
				((_contentReadStream as Stream) ?? ((_contentReadStream is Task<Stream> { Status: TaskStatus.RanToCompletion } task) ? task.Result : null))?.Dispose();
				_contentReadStream = null;
			}
			if (IsBuffered)
			{
				_bufferedContent.Dispose();
			}
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void CheckDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().ToString());
		}
	}

	private void CheckTaskNotNull(Task task)
	{
		if (task == null)
		{
			InvalidOperationException ex = new InvalidOperationException(System.SR.net_http_content_no_task_returned);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex, "CheckTaskNotNull");
			}
			throw ex;
		}
	}

	internal static bool StreamCopyExceptionNeedsWrapping(Exception e)
	{
		if (!(e is IOException))
		{
			return e is ObjectDisposedException;
		}
		return true;
	}

	private static Exception GetStreamCopyException(Exception originalException)
	{
		if (!StreamCopyExceptionNeedsWrapping(originalException))
		{
			return originalException;
		}
		return WrapStreamCopyException(originalException);
	}

	internal static Exception WrapStreamCopyException(Exception e)
	{
		return new HttpRequestException(System.SR.net_http_content_stream_copy_error, e);
	}

	private static int GetPreambleLength(ArraySegment<byte> buffer, Encoding encoding)
	{
		byte[] array = buffer.Array;
		int offset = buffer.Offset;
		int count = buffer.Count;
		switch (encoding.CodePage)
		{
		case 65001:
			if (count < 3 || array[offset] != 239 || array[offset + 1] != 187 || array[offset + 2] != 191)
			{
				return 0;
			}
			return 3;
		case 12000:
			if (count < 4 || array[offset] != byte.MaxValue || array[offset + 1] != 254 || array[offset + 2] != 0 || array[offset + 3] != 0)
			{
				return 0;
			}
			return 4;
		case 1200:
			if (count < 2 || array[offset] != byte.MaxValue || array[offset + 1] != 254)
			{
				return 0;
			}
			return 2;
		case 1201:
			if (count < 2 || array[offset] != 254 || array[offset + 1] != byte.MaxValue)
			{
				return 0;
			}
			return 2;
		default:
		{
			byte[] preamble = encoding.GetPreamble();
			if (!BufferHasPrefix(buffer, preamble))
			{
				return 0;
			}
			return preamble.Length;
		}
		}
	}

	private static bool TryDetectEncoding(ArraySegment<byte> buffer, [NotNullWhen(true)] out Encoding encoding, out int preambleLength)
	{
		byte[] array = buffer.Array;
		int offset = buffer.Offset;
		int count = buffer.Count;
		if (count >= 2)
		{
			switch ((array[offset] << 8) | array[offset + 1])
			{
			case 61371:
				if (count >= 3 && array[offset + 2] == 191)
				{
					encoding = Encoding.UTF8;
					preambleLength = 3;
					return true;
				}
				break;
			case 65534:
				if (count >= 4 && array[offset + 2] == 0 && array[offset + 3] == 0)
				{
					encoding = Encoding.UTF32;
					preambleLength = 4;
				}
				else
				{
					encoding = Encoding.Unicode;
					preambleLength = 2;
				}
				return true;
			case 65279:
				encoding = Encoding.BigEndianUnicode;
				preambleLength = 2;
				return true;
			}
		}
		encoding = null;
		preambleLength = 0;
		return false;
	}

	private static bool BufferHasPrefix(ArraySegment<byte> buffer, byte[] prefix)
	{
		byte[] array = buffer.Array;
		if (prefix == null || array == null || prefix.Length > buffer.Count || prefix.Length == 0)
		{
			return false;
		}
		int num = 0;
		int num2 = buffer.Offset;
		while (num < prefix.Length)
		{
			if (prefix[num] != array[num2])
			{
				return false;
			}
			num++;
			num2++;
		}
		return true;
	}

	private static async Task<TResult> WaitAndReturnAsync<TState, TResult>(Task waitTask, TState state, Func<TState, TResult> returnFunc)
	{
		await waitTask.ConfigureAwait(continueOnCapturedContext: false);
		return returnFunc(state);
	}

	private static Exception CreateOverCapacityException(int maxBufferSize)
	{
		return new HttpRequestException(System.SR.Format(System.SR.net_http_content_buffersize_exceeded, maxBufferSize));
	}
}
