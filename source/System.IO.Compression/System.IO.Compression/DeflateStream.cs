using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

public class DeflateStream : Stream
{
	private sealed class CopyToStream : Stream
	{
		private readonly DeflateStream _deflateStream;

		private readonly Stream _destination;

		private readonly CancellationToken _cancellationToken;

		private byte[] _arrayPoolBuffer;

		public override bool CanWrite => true;

		public override bool CanRead => false;

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

		public CopyToStream(DeflateStream deflateStream, Stream destination, int bufferSize)
			: this(deflateStream, destination, bufferSize, CancellationToken.None)
		{
		}

		public CopyToStream(DeflateStream deflateStream, Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			_deflateStream = deflateStream;
			_destination = destination;
			_cancellationToken = cancellationToken;
			_arrayPoolBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
		}

		public async Task CopyFromSourceToDestinationAsync()
		{
			_deflateStream.AsyncOperationStarting();
			try
			{
				while (!_deflateStream._inflater.Finished())
				{
					int num = _deflateStream._inflater.Inflate(_arrayPoolBuffer, 0, _arrayPoolBuffer.Length);
					if (num > 0)
					{
						await _destination.WriteAsync(new ReadOnlyMemory<byte>(_arrayPoolBuffer, 0, num), _cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					}
					else if (_deflateStream._inflater.NeedsInput())
					{
						break;
					}
				}
				await _deflateStream._stream.CopyToAsync(this, _arrayPoolBuffer.Length, _cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				_deflateStream.AsyncOperationCompleting();
				ArrayPool<byte>.Shared.Return(_arrayPoolBuffer);
				_arrayPoolBuffer = null;
			}
		}

		public void CopyFromSourceToDestination()
		{
			try
			{
				while (!_deflateStream._inflater.Finished())
				{
					int num = _deflateStream._inflater.Inflate(_arrayPoolBuffer, 0, _arrayPoolBuffer.Length);
					if (num > 0)
					{
						_destination.Write(_arrayPoolBuffer, 0, num);
					}
					else if (_deflateStream._inflater.NeedsInput())
					{
						break;
					}
				}
				_deflateStream._stream.CopyTo(this, _arrayPoolBuffer.Length);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(_arrayPoolBuffer);
				_arrayPoolBuffer = null;
			}
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			_deflateStream.EnsureNotDisposed();
			if (count <= 0)
			{
				return Task.CompletedTask;
			}
			if (count > buffer.Length - offset)
			{
				return Task.FromException(new InvalidDataException(System.SR.GenericInvalidData));
			}
			return WriteAsyncCore(buffer.AsMemory(offset, count), cancellationToken).AsTask();
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
		{
			_deflateStream.EnsureNotDisposed();
			return WriteAsyncCore(buffer, cancellationToken);
		}

		private async ValueTask WriteAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
		{
			_deflateStream._inflater.SetInput(buffer);
			while (!_deflateStream._inflater.Finished())
			{
				int num = _deflateStream._inflater.Inflate(new Span<byte>(_arrayPoolBuffer));
				if (num > 0)
				{
					await _destination.WriteAsync(new ReadOnlyMemory<byte>(_arrayPoolBuffer, 0, num), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				else if (_deflateStream._inflater.NeedsInput())
				{
					break;
				}
			}
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_deflateStream.EnsureNotDisposed();
			if (count <= 0)
			{
				return;
			}
			if (count > buffer.Length - offset)
			{
				throw new InvalidDataException(System.SR.GenericInvalidData);
			}
			_deflateStream._inflater.SetInput(buffer, offset, count);
			while (!_deflateStream._inflater.Finished())
			{
				int num = _deflateStream._inflater.Inflate(new Span<byte>(_arrayPoolBuffer));
				if (num > 0)
				{
					_destination.Write(_arrayPoolBuffer, 0, num);
				}
				else if (_deflateStream._inflater.NeedsInput())
				{
					break;
				}
			}
		}

		public override void Flush()
		{
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

	private Stream _stream;

	private CompressionMode _mode;

	private bool _leaveOpen;

	private Inflater _inflater;

	private Deflater _deflater;

	private byte[] _buffer;

	private int _activeAsyncOperation;

	private bool _wroteBytes;

	public Stream BaseStream => _stream;

	public override bool CanRead
	{
		get
		{
			if (_stream == null)
			{
				return false;
			}
			if (_mode == CompressionMode.Decompress)
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
			if (_stream == null)
			{
				return false;
			}
			if (_mode == CompressionMode.Compress)
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
			throw new NotSupportedException(System.SR.NotSupported);
		}
	}

	public override long Position
	{
		get
		{
			throw new NotSupportedException(System.SR.NotSupported);
		}
		set
		{
			throw new NotSupportedException(System.SR.NotSupported);
		}
	}

	private bool InflatorIsFinished
	{
		get
		{
			if (_inflater.Finished())
			{
				if (_inflater.IsGzipStream())
				{
					return !_inflater.NeedsInput();
				}
				return true;
			}
			return false;
		}
	}

	private bool AsyncOperationIsActive => _activeAsyncOperation != 0;

	internal DeflateStream(Stream stream, CompressionMode mode, long uncompressedSize)
		: this(stream, mode, leaveOpen: false, -15, uncompressedSize)
	{
	}

	public DeflateStream(Stream stream, CompressionMode mode)
		: this(stream, mode, leaveOpen: false)
	{
	}

	public DeflateStream(Stream stream, CompressionMode mode, bool leaveOpen)
		: this(stream, mode, leaveOpen, -15, -1L)
	{
	}

	public DeflateStream(Stream stream, CompressionLevel compressionLevel)
		: this(stream, compressionLevel, leaveOpen: false)
	{
	}

	public DeflateStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
		: this(stream, compressionLevel, leaveOpen, -15)
	{
	}

	internal DeflateStream(Stream stream, CompressionMode mode, bool leaveOpen, int windowBits, long uncompressedSize = -1L)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		switch (mode)
		{
		case CompressionMode.Decompress:
			if (!stream.CanRead)
			{
				throw new ArgumentException(System.SR.NotSupported_UnreadableStream, "stream");
			}
			_inflater = new Inflater(windowBits, uncompressedSize);
			_stream = stream;
			_mode = CompressionMode.Decompress;
			_leaveOpen = leaveOpen;
			break;
		case CompressionMode.Compress:
			InitializeDeflater(stream, leaveOpen, windowBits, CompressionLevel.Optimal);
			break;
		default:
			throw new ArgumentException(System.SR.ArgumentOutOfRange_Enum, "mode");
		}
	}

	internal DeflateStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen, int windowBits)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		InitializeDeflater(stream, leaveOpen, windowBits, compressionLevel);
	}

	[MemberNotNull("_stream")]
	internal void InitializeDeflater(Stream stream, bool leaveOpen, int windowBits, CompressionLevel compressionLevel)
	{
		if (!stream.CanWrite)
		{
			throw new ArgumentException(System.SR.NotSupported_UnwritableStream, "stream");
		}
		_deflater = new Deflater(compressionLevel, windowBits);
		_stream = stream;
		_mode = CompressionMode.Compress;
		_leaveOpen = leaveOpen;
		InitializeBuffer();
	}

	[MemberNotNull("_buffer")]
	private void InitializeBuffer()
	{
		_buffer = ArrayPool<byte>.Shared.Rent(8192);
	}

	[MemberNotNull("_buffer")]
	private void EnsureBufferInitialized()
	{
		if (_buffer == null)
		{
			InitializeBuffer();
		}
	}

	public override void Flush()
	{
		EnsureNotDisposed();
		if (_mode == CompressionMode.Compress)
		{
			FlushBuffers();
		}
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
			return Core(cancellationToken);
		}
		return Task.CompletedTask;
		async Task Core(CancellationToken cancellationToken)
		{
			AsyncOperationStarting();
			try
			{
				await WriteDeflaterOutputAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				bool flushSuccessful;
				do
				{
					flushSuccessful = _deflater.Flush(_buffer, out var bytesRead);
					if (flushSuccessful)
					{
						await _stream.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, bytesRead), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					}
				}
				while (flushSuccessful);
				await _stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				AsyncOperationCompleting();
			}
		}
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException(System.SR.NotSupported);
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException(System.SR.NotSupported);
	}

	public override int ReadByte()
	{
		EnsureDecompressionMode();
		EnsureNotDisposed();
		if (!_inflater.Inflate(out var b))
		{
			return base.ReadByte();
		}
		return b;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return ReadCore(new Span<byte>(buffer, offset, count));
	}

	public override int Read(Span<byte> buffer)
	{
		if (GetType() != typeof(DeflateStream))
		{
			return base.Read(buffer);
		}
		return ReadCore(buffer);
	}

	internal int ReadCore(Span<byte> buffer)
	{
		EnsureDecompressionMode();
		EnsureNotDisposed();
		EnsureBufferInitialized();
		int num;
		do
		{
			num = _inflater.Inflate(buffer);
			if (num != 0 || InflatorIsFinished)
			{
				break;
			}
			if (_inflater.NeedsInput())
			{
				int num2 = _stream.Read(_buffer, 0, _buffer.Length);
				if (num2 <= 0)
				{
					break;
				}
				if (num2 > _buffer.Length)
				{
					ThrowGenericInvalidData();
				}
				else
				{
					_inflater.SetInput(_buffer, 0, num2);
				}
			}
		}
		while (!buffer.IsEmpty);
		return num;
	}

	private void EnsureNotDisposed()
	{
		if (_stream == null)
		{
			ThrowStreamClosedException();
		}
		static void ThrowStreamClosedException()
		{
			throw new ObjectDisposedException("DeflateStream", System.SR.ObjectDisposed_StreamClosed);
		}
	}

	private void EnsureDecompressionMode()
	{
		if (_mode != 0)
		{
			ThrowCannotReadFromDeflateStreamException();
		}
		static void ThrowCannotReadFromDeflateStreamException()
		{
			throw new InvalidOperationException(System.SR.CannotReadFromDeflateStream);
		}
	}

	private void EnsureCompressionMode()
	{
		if (_mode != CompressionMode.Compress)
		{
			ThrowCannotWriteToDeflateStreamException();
		}
		static void ThrowCannotWriteToDeflateStreamException()
		{
			throw new InvalidOperationException(System.SR.CannotWriteToDeflateStream);
		}
	}

	private static void ThrowGenericInvalidData()
	{
		throw new InvalidDataException(System.SR.GenericInvalidData);
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
	{
		return System.Threading.Tasks.TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		EnsureDecompressionMode();
		EnsureNotDisposed();
		return System.Threading.Tasks.TaskToApm.End<int>(asyncResult);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return ReadAsyncMemory(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (GetType() != typeof(DeflateStream))
		{
			return base.ReadAsync(buffer, cancellationToken);
		}
		return ReadAsyncMemory(buffer, cancellationToken);
	}

	internal ValueTask<int> ReadAsyncMemory(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		EnsureDecompressionMode();
		EnsureNoActiveAsyncOperation();
		EnsureNotDisposed();
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		EnsureBufferInitialized();
		return Core(buffer, cancellationToken);
		async ValueTask<int> Core(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			AsyncOperationStarting();
			try
			{
				int bytesRead;
				do
				{
					bytesRead = _inflater.Inflate(buffer.Span);
					if (bytesRead != 0 || InflatorIsFinished)
					{
						break;
					}
					if (_inflater.NeedsInput())
					{
						int num = await _stream.ReadAsync(new Memory<byte>(_buffer, 0, _buffer.Length), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						if (num <= 0)
						{
							break;
						}
						if (num > _buffer.Length)
						{
							ThrowGenericInvalidData();
						}
						else
						{
							_inflater.SetInput(_buffer, 0, num);
						}
					}
				}
				while (!buffer.IsEmpty);
				return bytesRead;
			}
			finally
			{
				AsyncOperationCompleting();
			}
		}
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		WriteCore(new ReadOnlySpan<byte>(buffer, offset, count));
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		if (GetType() != typeof(DeflateStream))
		{
			base.Write(buffer);
		}
		else
		{
			WriteCore(buffer);
		}
	}

	internal unsafe void WriteCore(ReadOnlySpan<byte> buffer)
	{
		EnsureCompressionMode();
		EnsureNotDisposed();
		WriteDeflaterOutput();
		fixed (byte* inputBufferPtr = &MemoryMarshal.GetReference(buffer))
		{
			_deflater.SetInput(inputBufferPtr, buffer.Length);
			WriteDeflaterOutput();
			_wroteBytes = true;
		}
	}

	private void WriteDeflaterOutput()
	{
		while (!_deflater.NeedsInput())
		{
			int deflateOutput = _deflater.GetDeflateOutput(_buffer);
			if (deflateOutput > 0)
			{
				_stream.Write(_buffer, 0, deflateOutput);
			}
		}
	}

	private void FlushBuffers()
	{
		if (_wroteBytes)
		{
			WriteDeflaterOutput();
			bool flag;
			do
			{
				flag = _deflater.Flush(_buffer, out var bytesRead);
				if (flag)
				{
					_stream.Write(_buffer, 0, bytesRead);
				}
			}
			while (flag);
		}
		_stream.Flush();
	}

	private void PurgeBuffers(bool disposing)
	{
		if (!disposing || _stream == null || _mode != CompressionMode.Compress)
		{
			return;
		}
		if (_wroteBytes)
		{
			WriteDeflaterOutput();
			bool flag;
			do
			{
				flag = _deflater.Finish(_buffer, out var bytesRead);
				if (bytesRead > 0)
				{
					_stream.Write(_buffer, 0, bytesRead);
				}
			}
			while (!flag);
		}
		else
		{
			int bytesRead2;
			while (!_deflater.Finish(_buffer, out bytesRead2))
			{
			}
		}
	}

	private async ValueTask PurgeBuffersAsync()
	{
		if (_stream == null || _mode != CompressionMode.Compress)
		{
			return;
		}
		if (_wroteBytes)
		{
			await WriteDeflaterOutputAsync(default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
			bool finished;
			do
			{
				finished = _deflater.Finish(_buffer, out var bytesRead);
				if (bytesRead > 0)
				{
					await _stream.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, bytesRead)).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			while (!finished);
		}
		else
		{
			int bytesRead2;
			while (!_deflater.Finish(_buffer, out bytesRead2))
			{
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			PurgeBuffers(disposing);
		}
		finally
		{
			try
			{
				if (disposing && !_leaveOpen)
				{
					_stream?.Dispose();
				}
			}
			finally
			{
				_stream = null;
				try
				{
					_deflater?.Dispose();
					_inflater?.Dispose();
				}
				finally
				{
					_deflater = null;
					_inflater = null;
					byte[] buffer = _buffer;
					if (buffer != null)
					{
						_buffer = null;
						if (!AsyncOperationIsActive)
						{
							ArrayPool<byte>.Shared.Return(buffer);
						}
					}
					base.Dispose(disposing);
				}
			}
		}
	}

	public override ValueTask DisposeAsync()
	{
		if (!(GetType() == typeof(DeflateStream)))
		{
			return base.DisposeAsync();
		}
		return Core();
		async ValueTask Core()
		{
			try
			{
				await PurgeBuffersAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				Stream stream = _stream;
				_stream = null;
				try
				{
					if (!_leaveOpen && stream != null)
					{
						await stream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
				}
				finally
				{
					try
					{
						_deflater?.Dispose();
						_inflater?.Dispose();
					}
					finally
					{
						_deflater = null;
						_inflater = null;
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
				}
			}
		}
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
	{
		return System.Threading.Tasks.TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		EnsureCompressionMode();
		EnsureNotDisposed();
		System.Threading.Tasks.TaskToApm.End(asyncResult);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return WriteAsyncMemory(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		if (GetType() != typeof(DeflateStream))
		{
			return base.WriteAsync(buffer, cancellationToken);
		}
		return WriteAsyncMemory(buffer, cancellationToken);
	}

	internal ValueTask WriteAsyncMemory(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		EnsureCompressionMode();
		EnsureNoActiveAsyncOperation();
		EnsureNotDisposed();
		if (!cancellationToken.IsCancellationRequested)
		{
			return Core(buffer, cancellationToken);
		}
		return ValueTask.FromCanceled(cancellationToken);
		async ValueTask Core(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
		{
			AsyncOperationStarting();
			try
			{
				await WriteDeflaterOutputAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				_deflater.SetInput(buffer);
				await WriteDeflaterOutputAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				_wroteBytes = true;
			}
			finally
			{
				AsyncOperationCompleting();
			}
		}
	}

	private async ValueTask WriteDeflaterOutputAsync(CancellationToken cancellationToken)
	{
		while (!_deflater.NeedsInput())
		{
			int deflateOutput = _deflater.GetDeflateOutput(_buffer);
			if (deflateOutput > 0)
			{
				await _stream.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, deflateOutput), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
	}

	public override void CopyTo(Stream destination, int bufferSize)
	{
		Stream.ValidateCopyToArguments(destination, bufferSize);
		EnsureNotDisposed();
		if (!CanRead)
		{
			throw new NotSupportedException();
		}
		new CopyToStream(this, destination, bufferSize).CopyFromSourceToDestination();
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		Stream.ValidateCopyToArguments(destination, bufferSize);
		EnsureNotDisposed();
		if (!CanRead)
		{
			throw new NotSupportedException();
		}
		EnsureNoActiveAsyncOperation();
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<int>(cancellationToken);
		}
		return new CopyToStream(this, destination, bufferSize, cancellationToken).CopyFromSourceToDestinationAsync();
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
