using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

public sealed class BrotliStream : Stream
{
	private BrotliEncoder _encoder;

	private BrotliDecoder _decoder;

	private int _bufferOffset;

	private int _bufferCount;

	private Stream _stream;

	private byte[] _buffer;

	private readonly bool _leaveOpen;

	private readonly CompressionMode _mode;

	private int _activeAsyncOperation;

	public Stream BaseStream => _stream;

	public override bool CanRead
	{
		get
		{
			if (_mode == CompressionMode.Decompress && _stream != null)
			{
				return _stream.CanRead;
			}
			return false;
		}
	}

	public override bool CanWrite
	{
		get
		{
			if (_mode == CompressionMode.Compress && _stream != null)
			{
				return _stream.CanWrite;
			}
			return false;
		}
	}

	public override bool CanSeek => false;

	public override long Length
	{
		get
		{
			throw new NotSupportedException();
		}
	}

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

	private bool AsyncOperationIsActive => _activeAsyncOperation != 0;

	public BrotliStream(Stream stream, CompressionLevel compressionLevel)
		: this(stream, compressionLevel, leaveOpen: false)
	{
	}

	public BrotliStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
		: this(stream, CompressionMode.Compress, leaveOpen)
	{
		_encoder.SetQuality(BrotliUtils.GetQualityFromCompressionLevel(compressionLevel));
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		WriteCore(new ReadOnlySpan<byte>(buffer, offset, count));
	}

	public override void WriteByte(byte value)
	{
		WriteCore(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		WriteCore(buffer);
	}

	internal void WriteCore(ReadOnlySpan<byte> buffer, bool isFinalBlock = false)
	{
		if (_mode != CompressionMode.Compress)
		{
			throw new InvalidOperationException(System.SR.BrotliStream_Decompress_UnsupportedOperation);
		}
		EnsureNotDisposed();
		OperationStatus operationStatus = OperationStatus.DestinationTooSmall;
		Span<byte> destination = new Span<byte>(_buffer);
		while (operationStatus == OperationStatus.DestinationTooSmall)
		{
			operationStatus = _encoder.Compress(buffer, destination, out var bytesConsumed, out var bytesWritten, isFinalBlock);
			if (operationStatus == OperationStatus.InvalidData)
			{
				throw new InvalidOperationException(System.SR.BrotliStream_Compress_InvalidData);
			}
			if (bytesWritten > 0)
			{
				_stream.Write(destination.Slice(0, bytesWritten));
			}
			if (bytesConsumed > 0)
			{
				buffer = buffer.Slice(bytesConsumed);
			}
		}
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
	{
		return System.Threading.Tasks.TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		System.Threading.Tasks.TaskToApm.End(asyncResult);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_mode != CompressionMode.Compress)
		{
			throw new InvalidOperationException(System.SR.BrotliStream_Decompress_UnsupportedOperation);
		}
		EnsureNoActiveAsyncOperation();
		EnsureNotDisposed();
		if (!cancellationToken.IsCancellationRequested)
		{
			return WriteAsyncMemoryCore(buffer, cancellationToken);
		}
		return ValueTask.FromCanceled(cancellationToken);
	}

	private async ValueTask WriteAsyncMemoryCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken, bool isFinalBlock = false)
	{
		AsyncOperationStarting();
		try
		{
			OperationStatus lastResult = OperationStatus.DestinationTooSmall;
			while (lastResult == OperationStatus.DestinationTooSmall)
			{
				Memory<byte> destination = new Memory<byte>(_buffer);
				int bytesConsumed = 0;
				int bytesWritten = 0;
				lastResult = _encoder.Compress(buffer, destination, out bytesConsumed, out bytesWritten, isFinalBlock);
				if (lastResult == OperationStatus.InvalidData)
				{
					throw new InvalidOperationException(System.SR.BrotliStream_Compress_InvalidData);
				}
				if (bytesConsumed > 0)
				{
					buffer = buffer.Slice(bytesConsumed);
				}
				if (bytesWritten > 0)
				{
					await _stream.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, bytesWritten), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		finally
		{
			AsyncOperationCompleting();
		}
	}

	public override void Flush()
	{
		EnsureNotDisposed();
		if (_mode != CompressionMode.Compress || _encoder._state == null || _encoder._state.IsClosed)
		{
			return;
		}
		OperationStatus operationStatus = OperationStatus.DestinationTooSmall;
		Span<byte> destination = new Span<byte>(_buffer);
		while (operationStatus == OperationStatus.DestinationTooSmall)
		{
			operationStatus = _encoder.Flush(destination, out var bytesWritten);
			if (operationStatus == OperationStatus.InvalidData)
			{
				throw new InvalidDataException(System.SR.BrotliStream_Compress_InvalidData);
			}
			if (bytesWritten > 0)
			{
				_stream.Write(destination.Slice(0, bytesWritten));
			}
		}
		_stream.Flush();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		EnsureNoActiveAsyncOperation();
		EnsureNotDisposed();
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		if (_mode == CompressionMode.Compress)
		{
			return FlushAsyncCore(cancellationToken);
		}
		return Task.CompletedTask;
	}

	private async Task FlushAsyncCore(CancellationToken cancellationToken)
	{
		AsyncOperationStarting();
		try
		{
			if (_encoder._state == null || _encoder._state.IsClosed)
			{
				return;
			}
			OperationStatus lastResult = OperationStatus.DestinationTooSmall;
			while (lastResult == OperationStatus.DestinationTooSmall)
			{
				Memory<byte> destination = new Memory<byte>(_buffer);
				int bytesWritten = 0;
				lastResult = _encoder.Flush(destination, out bytesWritten);
				if (lastResult == OperationStatus.InvalidData)
				{
					throw new InvalidDataException(System.SR.BrotliStream_Compress_InvalidData);
				}
				if (bytesWritten > 0)
				{
					await _stream.WriteAsync(destination.Slice(0, bytesWritten), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			await _stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			AsyncOperationCompleting();
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return Read(new Span<byte>(buffer, offset, count));
	}

	public override int ReadByte()
	{
		byte reference = 0;
		if (Read(MemoryMarshal.CreateSpan(ref reference, 1)) == 0)
		{
			return -1;
		}
		return reference;
	}

	public override int Read(Span<byte> buffer)
	{
		if (_mode != 0)
		{
			throw new InvalidOperationException(System.SR.BrotliStream_Compress_UnsupportedOperation);
		}
		EnsureNotDisposed();
		int bytesWritten;
		while (!TryDecompress(buffer, out bytesWritten))
		{
			int num = _stream.Read(_buffer, _bufferCount, _buffer.Length - _bufferCount);
			if (num <= 0)
			{
				break;
			}
			_bufferCount += num;
			if (_bufferCount > _buffer.Length)
			{
				ThrowInvalidStream();
			}
		}
		return bytesWritten;
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
	{
		return System.Threading.Tasks.TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return System.Threading.Tasks.TaskToApm.End<int>(asyncResult);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_mode != 0)
		{
			throw new InvalidOperationException(System.SR.BrotliStream_Compress_UnsupportedOperation);
		}
		EnsureNoActiveAsyncOperation();
		EnsureNotDisposed();
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		return Core(buffer, cancellationToken);
		async ValueTask<int> Core(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			AsyncOperationStarting();
			try
			{
				int bytesWritten;
				while (!TryDecompress(buffer.Span, out bytesWritten))
				{
					int num = await _stream.ReadAsync(_buffer.AsMemory(_bufferCount), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					if (num <= 0)
					{
						break;
					}
					_bufferCount += num;
					if (_bufferCount > _buffer.Length)
					{
						ThrowInvalidStream();
					}
				}
				return bytesWritten;
			}
			finally
			{
				AsyncOperationCompleting();
			}
		}
	}

	private bool TryDecompress(Span<byte> destination, out int bytesWritten)
	{
		int bytesConsumed;
		OperationStatus operationStatus = _decoder.Decompress(new ReadOnlySpan<byte>(_buffer, _bufferOffset, _bufferCount), destination, out bytesConsumed, out bytesWritten);
		if (operationStatus == OperationStatus.InvalidData)
		{
			throw new InvalidOperationException(System.SR.BrotliStream_Decompress_InvalidData);
		}
		if (bytesConsumed != 0)
		{
			_bufferOffset += bytesConsumed;
			_bufferCount -= bytesConsumed;
		}
		if (bytesWritten != 0 || operationStatus == OperationStatus.Done)
		{
			return true;
		}
		if (destination.IsEmpty && _bufferCount != 0)
		{
			return true;
		}
		if (_bufferCount != 0 && _bufferOffset != 0)
		{
			new ReadOnlySpan<byte>(_buffer, _bufferOffset, _bufferCount).CopyTo(_buffer);
		}
		_bufferOffset = 0;
		return false;
	}

	private static void ThrowInvalidStream()
	{
		throw new InvalidDataException(System.SR.BrotliStream_Decompress_InvalidStream);
	}

	public BrotliStream(Stream stream, CompressionMode mode)
		: this(stream, mode, leaveOpen: false)
	{
	}

	public BrotliStream(Stream stream, CompressionMode mode, bool leaveOpen)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		switch (mode)
		{
		case CompressionMode.Compress:
			if (!stream.CanWrite)
			{
				throw new ArgumentException(System.SR.Stream_FalseCanWrite, "stream");
			}
			break;
		case CompressionMode.Decompress:
			if (!stream.CanRead)
			{
				throw new ArgumentException(System.SR.Stream_FalseCanRead, "stream");
			}
			break;
		default:
			throw new ArgumentException(System.SR.ArgumentOutOfRange_Enum, "mode");
		}
		_mode = mode;
		_stream = stream;
		_leaveOpen = leaveOpen;
		_buffer = ArrayPool<byte>.Shared.Rent(65520);
	}

	private void EnsureNotDisposed()
	{
		if (_stream == null)
		{
			throw new ObjectDisposedException(GetType().Name, System.SR.ObjectDisposed_StreamClosed);
		}
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing && _stream != null)
			{
				if (_mode == CompressionMode.Compress)
				{
					WriteCore(ReadOnlySpan<byte>.Empty, isFinalBlock: true);
				}
				if (!_leaveOpen)
				{
					_stream.Dispose();
				}
			}
		}
		finally
		{
			ReleaseStateForDispose();
			base.Dispose(disposing);
		}
	}

	public override async ValueTask DisposeAsync()
	{
		_ = 1;
		try
		{
			if (_stream != null)
			{
				if (_mode == CompressionMode.Compress)
				{
					await WriteAsyncMemoryCore(ReadOnlyMemory<byte>.Empty, CancellationToken.None, isFinalBlock: true).ConfigureAwait(continueOnCapturedContext: false);
				}
				if (!_leaveOpen)
				{
					await _stream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		finally
		{
			ReleaseStateForDispose();
		}
	}

	private void ReleaseStateForDispose()
	{
		_stream = null;
		_encoder.Dispose();
		_decoder.Dispose();
		byte[] buffer = _buffer;
		if (buffer != null)
		{
			_buffer = null;
			if (!AsyncOperationIsActive)
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	private void EnsureNoActiveAsyncOperation()
	{
		if (AsyncOperationIsActive)
		{
			ThrowInvalidBeginCall();
		}
	}

	private void AsyncOperationStarting()
	{
		if (Interlocked.Exchange(ref _activeAsyncOperation, 1) != 0)
		{
			ThrowInvalidBeginCall();
		}
	}

	private void AsyncOperationCompleting()
	{
		Volatile.Write(ref _activeAsyncOperation, 0);
	}

	private static void ThrowInvalidBeginCall()
	{
		throw new InvalidOperationException(System.SR.InvalidBeginCall);
	}
}
