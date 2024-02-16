using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

public sealed class BufferedStream : Stream
{
	private Stream _stream;

	private byte[] _buffer;

	private readonly int _bufferSize;

	private int _readPos;

	private int _readLen;

	private int _writePos;

	private Task<int> _lastSyncCompletedReadTask;

	public Stream UnderlyingStream => _stream;

	public int BufferSize => _bufferSize;

	public override bool CanRead
	{
		get
		{
			if (_stream != null)
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
			if (_stream != null)
			{
				return _stream.CanWrite;
			}
			return false;
		}
	}

	public override bool CanSeek
	{
		get
		{
			if (_stream != null)
			{
				return _stream.CanSeek;
			}
			return false;
		}
	}

	public override long Length
	{
		get
		{
			EnsureNotClosed();
			if (_writePos > 0)
			{
				FlushWrite();
			}
			return _stream.Length;
		}
	}

	public override long Position
	{
		get
		{
			EnsureNotClosed();
			EnsureCanSeek();
			return _stream.Position + (_readPos - _readLen + _writePos);
		}
		set
		{
			if (value < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			Seek(value, SeekOrigin.Begin);
		}
	}

	public BufferedStream(Stream stream)
		: this(stream, 4096)
	{
	}

	public BufferedStream(Stream stream, int bufferSize)
	{
		if (stream == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.stream);
		}
		if (bufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize", SR.Format(SR.ArgumentOutOfRange_MustBePositive, "bufferSize"));
		}
		_stream = stream;
		_bufferSize = bufferSize;
		if (!_stream.CanRead && !_stream.CanWrite)
		{
			ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
		}
	}

	private void EnsureNotClosed()
	{
		if (_stream == null)
		{
			ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
		}
	}

	private void EnsureCanSeek()
	{
		if (!_stream.CanSeek)
		{
			ThrowHelper.ThrowNotSupportedException_UnseekableStream();
		}
	}

	private void EnsureCanRead()
	{
		if (!_stream.CanRead)
		{
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
	}

	private void EnsureCanWrite()
	{
		if (!_stream.CanWrite)
		{
			ThrowHelper.ThrowNotSupportedException_UnwritableStream();
		}
	}

	private void EnsureShadowBufferAllocated()
	{
		if (_buffer.Length == _bufferSize && _bufferSize < 81920)
		{
			byte[] array = new byte[Math.Min(_bufferSize + _bufferSize, 81920)];
			Buffer.BlockCopy(_buffer, 0, array, 0, _writePos);
			_buffer = array;
		}
	}

	private void EnsureBufferAllocated()
	{
		if (_buffer == null)
		{
			_buffer = new byte[_bufferSize];
		}
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing && _stream != null)
			{
				try
				{
					Flush();
					return;
				}
				finally
				{
					_stream.Dispose();
				}
			}
		}
		finally
		{
			_stream = null;
			_buffer = null;
			_writePos = 0;
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
				try
				{
					await FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					await _stream.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		finally
		{
			_stream = null;
			_buffer = null;
			_writePos = 0;
		}
	}

	public override void Flush()
	{
		EnsureNotClosed();
		if (_writePos > 0)
		{
			FlushWrite();
		}
		else if (_readPos < _readLen)
		{
			if (_stream.CanSeek)
			{
				FlushRead();
			}
			if (_stream.CanWrite)
			{
				_stream.Flush();
			}
		}
		else
		{
			if (_stream.CanWrite)
			{
				_stream.Flush();
			}
			_writePos = (_readPos = (_readLen = 0));
		}
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<int>(cancellationToken);
		}
		EnsureNotClosed();
		return FlushAsyncInternal(cancellationToken);
	}

	private async Task FlushAsyncInternal(CancellationToken cancellationToken)
	{
		await EnsureAsyncActiveSemaphoreInitialized().WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			if (_writePos > 0)
			{
				await FlushWriteAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			else if (_readPos < _readLen)
			{
				if (_stream.CanSeek)
				{
					FlushRead();
				}
				if (_stream.CanWrite)
				{
					await _stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			else if (_stream.CanWrite)
			{
				await _stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		finally
		{
			_asyncActiveSemaphore.Release();
		}
	}

	private void FlushRead()
	{
		if (_readPos - _readLen != 0)
		{
			_stream.Seek(_readPos - _readLen, SeekOrigin.Current);
		}
		_readPos = 0;
		_readLen = 0;
	}

	private void ClearReadBufferBeforeWrite()
	{
		if (_readPos == _readLen)
		{
			_readPos = (_readLen = 0);
			return;
		}
		if (!_stream.CanSeek)
		{
			throw new NotSupportedException(SR.NotSupported_CannotWriteToBufferedStreamIfReadBufferCannotBeFlushed);
		}
		FlushRead();
	}

	private void FlushWrite()
	{
		_stream.Write(_buffer, 0, _writePos);
		_writePos = 0;
		_stream.Flush();
	}

	private async ValueTask FlushWriteAsync(CancellationToken cancellationToken)
	{
		await _stream.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, _writePos), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		_writePos = 0;
		await _stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private int ReadFromBuffer(byte[] buffer, int offset, int count)
	{
		int num = _readLen - _readPos;
		if (num == 0)
		{
			return 0;
		}
		if (num > count)
		{
			num = count;
		}
		Buffer.BlockCopy(_buffer, _readPos, buffer, offset, num);
		_readPos += num;
		return num;
	}

	private int ReadFromBuffer(Span<byte> destination)
	{
		int num = Math.Min(_readLen - _readPos, destination.Length);
		if (num > 0)
		{
			new ReadOnlySpan<byte>(_buffer, _readPos, num).CopyTo(destination);
			_readPos += num;
		}
		return num;
	}

	private int ReadFromBuffer(byte[] buffer, int offset, int count, out Exception error)
	{
		try
		{
			error = null;
			return ReadFromBuffer(buffer, offset, count);
		}
		catch (Exception ex)
		{
			error = ex;
			return 0;
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		EnsureNotClosed();
		EnsureCanRead();
		int num = ReadFromBuffer(buffer, offset, count);
		if (num == count)
		{
			return num;
		}
		int num2 = num;
		if (num > 0)
		{
			count -= num;
			offset += num;
		}
		_readPos = (_readLen = 0);
		if (_writePos > 0)
		{
			FlushWrite();
		}
		if (count >= _bufferSize)
		{
			return _stream.Read(buffer, offset, count) + num2;
		}
		EnsureBufferAllocated();
		_readLen = _stream.Read(_buffer, 0, _bufferSize);
		num = ReadFromBuffer(buffer, offset, count);
		return num + num2;
	}

	public override int Read(Span<byte> destination)
	{
		EnsureNotClosed();
		EnsureCanRead();
		int num = ReadFromBuffer(destination);
		if (num == destination.Length)
		{
			return num;
		}
		if (num > 0)
		{
			destination = destination.Slice(num);
		}
		_readPos = (_readLen = 0);
		if (_writePos > 0)
		{
			FlushWrite();
		}
		if (destination.Length >= _bufferSize)
		{
			return _stream.Read(destination) + num;
		}
		EnsureBufferAllocated();
		_readLen = _stream.Read(_buffer, 0, _bufferSize);
		return ReadFromBuffer(destination) + num;
	}

	private Task<int> LastSyncCompletedReadTask(int val)
	{
		Task<int> lastSyncCompletedReadTask = _lastSyncCompletedReadTask;
		if (lastSyncCompletedReadTask != null && lastSyncCompletedReadTask.Result == val)
		{
			return lastSyncCompletedReadTask;
		}
		return _lastSyncCompletedReadTask = Task.FromResult(val);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<int>(cancellationToken);
		}
		EnsureNotClosed();
		EnsureCanRead();
		int num = 0;
		SemaphoreSlim semaphoreSlim = EnsureAsyncActiveSemaphoreInitialized();
		Task task = semaphoreSlim.WaitAsync(cancellationToken);
		if (task.IsCompletedSuccessfully)
		{
			bool flag = true;
			try
			{
				num = ReadFromBuffer(buffer, offset, count, out var error);
				flag = num == count || error != null;
				if (flag)
				{
					return (error == null) ? LastSyncCompletedReadTask(num) : Task.FromException<int>(error);
				}
			}
			finally
			{
				if (flag)
				{
					semaphoreSlim.Release();
				}
			}
		}
		return ReadFromUnderlyingStreamAsync(new Memory<byte>(buffer, offset + num, count - num), cancellationToken, num, task).AsTask();
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		EnsureNotClosed();
		EnsureCanRead();
		int num = 0;
		SemaphoreSlim semaphoreSlim = EnsureAsyncActiveSemaphoreInitialized();
		Task task = semaphoreSlim.WaitAsync(cancellationToken);
		if (task.IsCompletedSuccessfully)
		{
			bool flag = true;
			try
			{
				num = ReadFromBuffer(buffer.Span);
				flag = num == buffer.Length;
				if (flag)
				{
					return new ValueTask<int>(num);
				}
			}
			finally
			{
				if (flag)
				{
					semaphoreSlim.Release();
				}
			}
		}
		return ReadFromUnderlyingStreamAsync(buffer.Slice(num), cancellationToken, num, task);
	}

	private async ValueTask<int> ReadFromUnderlyingStreamAsync(Memory<byte> buffer, CancellationToken cancellationToken, int bytesAlreadySatisfied, Task semaphoreLockTask)
	{
		await semaphoreLockTask.ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			int num = ReadFromBuffer(buffer.Span);
			if (num == buffer.Length)
			{
				return bytesAlreadySatisfied + num;
			}
			if (num > 0)
			{
				buffer = buffer.Slice(num);
				bytesAlreadySatisfied += num;
			}
			_readPos = (_readLen = 0);
			if (_writePos > 0)
			{
				await FlushWriteAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (buffer.Length >= _bufferSize)
			{
				return await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false) + bytesAlreadySatisfied;
			}
			EnsureBufferAllocated();
			_readLen = await _stream.ReadAsync(new Memory<byte>(_buffer, 0, _bufferSize), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			num = ReadFromBuffer(buffer.Span);
			return bytesAlreadySatisfied + num;
		}
		finally
		{
			_asyncActiveSemaphore.Release();
		}
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
	{
		return TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return TaskToApm.End<int>(asyncResult);
	}

	public override int ReadByte()
	{
		if (_readPos == _readLen)
		{
			return ReadByteSlow();
		}
		return _buffer[_readPos++];
	}

	private int ReadByteSlow()
	{
		EnsureNotClosed();
		EnsureCanRead();
		if (_writePos > 0)
		{
			FlushWrite();
		}
		EnsureBufferAllocated();
		_readLen = _stream.Read(_buffer, 0, _bufferSize);
		_readPos = 0;
		if (_readLen == 0)
		{
			return -1;
		}
		return _buffer[_readPos++];
	}

	private void WriteToBuffer(byte[] buffer, ref int offset, ref int count)
	{
		int num = Math.Min(_bufferSize - _writePos, count);
		if (num > 0)
		{
			EnsureBufferAllocated();
			Buffer.BlockCopy(buffer, offset, _buffer, _writePos, num);
			_writePos += num;
			count -= num;
			offset += num;
		}
	}

	private int WriteToBuffer(ReadOnlySpan<byte> buffer)
	{
		int num = Math.Min(_bufferSize - _writePos, buffer.Length);
		if (num > 0)
		{
			EnsureBufferAllocated();
			buffer.Slice(0, num).CopyTo(new Span<byte>(_buffer, _writePos, num));
			_writePos += num;
		}
		return num;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		EnsureNotClosed();
		EnsureCanWrite();
		if (_writePos == 0)
		{
			ClearReadBufferBeforeWrite();
		}
		int num;
		checked
		{
			num = _writePos + count;
			if ((uint)num + count < _bufferSize + _bufferSize)
			{
				WriteToBuffer(buffer, ref offset, ref count);
				if (_writePos >= _bufferSize)
				{
					_stream.Write(_buffer, 0, _writePos);
					_writePos = 0;
					WriteToBuffer(buffer, ref offset, ref count);
				}
				return;
			}
		}
		if (_writePos > 0)
		{
			if (num <= _bufferSize + _bufferSize && num <= 81920)
			{
				EnsureShadowBufferAllocated();
				Buffer.BlockCopy(buffer, offset, _buffer, _writePos, count);
				_stream.Write(_buffer, 0, num);
				_writePos = 0;
				return;
			}
			_stream.Write(_buffer, 0, _writePos);
			_writePos = 0;
		}
		_stream.Write(buffer, offset, count);
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		EnsureNotClosed();
		EnsureCanWrite();
		if (_writePos == 0)
		{
			ClearReadBufferBeforeWrite();
		}
		int num;
		checked
		{
			num = _writePos + buffer.Length;
			if ((uint)num + buffer.Length < _bufferSize + _bufferSize)
			{
				int start = WriteToBuffer(buffer);
				if (_writePos >= _bufferSize)
				{
					buffer = buffer.Slice(start);
					_stream.Write(_buffer, 0, _writePos);
					_writePos = 0;
					start = WriteToBuffer(buffer);
				}
				return;
			}
		}
		if (_writePos > 0)
		{
			if (num <= _bufferSize + _bufferSize && num <= 81920)
			{
				EnsureShadowBufferAllocated();
				buffer.CopyTo(new Span<byte>(_buffer, _writePos, buffer.Length));
				_stream.Write(_buffer, 0, num);
				_writePos = 0;
				return;
			}
			_stream.Write(_buffer, 0, _writePos);
			_writePos = 0;
		}
		_stream.Write(buffer);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		EnsureNotClosed();
		EnsureCanWrite();
		SemaphoreSlim semaphoreSlim = EnsureAsyncActiveSemaphoreInitialized();
		Task task = semaphoreSlim.WaitAsync(cancellationToken);
		if (task.IsCompletedSuccessfully)
		{
			bool flag = true;
			try
			{
				if (_writePos == 0)
				{
					ClearReadBufferBeforeWrite();
				}
				flag = buffer.Length < _bufferSize - _writePos;
				if (flag)
				{
					int num = WriteToBuffer(buffer.Span);
					return default(ValueTask);
				}
			}
			finally
			{
				if (flag)
				{
					semaphoreSlim.Release();
				}
			}
		}
		return WriteToUnderlyingStreamAsync(buffer, cancellationToken, task);
	}

	private async ValueTask WriteToUnderlyingStreamAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken, Task semaphoreLockTask)
	{
		await semaphoreLockTask.ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			if (_writePos == 0)
			{
				ClearReadBufferBeforeWrite();
			}
			int num;
			checked
			{
				num = _writePos + buffer.Length;
				if (num + buffer.Length < _bufferSize + _bufferSize)
				{
					buffer = buffer.Slice(WriteToBuffer(buffer.Span));
					if (_writePos >= _bufferSize)
					{
						await _stream.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, _writePos), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						_writePos = 0;
						WriteToBuffer(buffer.Span);
					}
					return;
				}
			}
			if (_writePos > 0)
			{
				if (num <= _bufferSize + _bufferSize && num <= 81920)
				{
					EnsureShadowBufferAllocated();
					buffer.Span.CopyTo(new Span<byte>(_buffer, _writePos, buffer.Length));
					await _stream.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, num), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					_writePos = 0;
					return;
				}
				await _stream.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, _writePos), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				_writePos = 0;
			}
			await _stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			_asyncActiveSemaphore.Release();
		}
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
	{
		return TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		TaskToApm.End(asyncResult);
	}

	public override void WriteByte(byte value)
	{
		if (_writePos > 0 && _writePos < _bufferSize - 1)
		{
			_buffer[_writePos++] = value;
		}
		else
		{
			WriteByteSlow(value);
		}
	}

	private void WriteByteSlow(byte value)
	{
		EnsureNotClosed();
		if (_writePos == 0)
		{
			EnsureCanWrite();
			ClearReadBufferBeforeWrite();
			EnsureBufferAllocated();
		}
		if (_writePos >= _bufferSize - 1)
		{
			FlushWrite();
		}
		_buffer[_writePos++] = value;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		EnsureNotClosed();
		EnsureCanSeek();
		if (_writePos > 0)
		{
			FlushWrite();
			return _stream.Seek(offset, origin);
		}
		if (_readLen - _readPos > 0 && origin == SeekOrigin.Current)
		{
			offset -= _readLen - _readPos;
		}
		long position = Position;
		long num = _stream.Seek(offset, origin);
		long num2 = num - (position - _readPos);
		if (0 <= num2 && num2 < _readLen)
		{
			_readPos = (int)num2;
			_stream.Seek(_readLen - _readPos, SeekOrigin.Current);
		}
		else
		{
			_readPos = (_readLen = 0);
		}
		return num;
	}

	public override void SetLength(long value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		EnsureNotClosed();
		EnsureCanSeek();
		EnsureCanWrite();
		Flush();
		_stream.SetLength(value);
	}

	public override void CopyTo(Stream destination, int bufferSize)
	{
		Stream.ValidateCopyToArguments(destination, bufferSize);
		EnsureNotClosed();
		EnsureCanRead();
		int num = _readLen - _readPos;
		if (num > 0)
		{
			destination.Write(_buffer, _readPos, num);
			_readPos = (_readLen = 0);
		}
		else if (_writePos > 0)
		{
			FlushWrite();
		}
		_stream.CopyTo(destination, bufferSize);
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		Stream.ValidateCopyToArguments(destination, bufferSize);
		EnsureNotClosed();
		EnsureCanRead();
		if (!cancellationToken.IsCancellationRequested)
		{
			return CopyToAsyncCore(destination, bufferSize, cancellationToken);
		}
		return Task.FromCanceled<int>(cancellationToken);
	}

	private async Task CopyToAsyncCore(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		await EnsureAsyncActiveSemaphoreInitialized().WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			int num = _readLen - _readPos;
			if (num > 0)
			{
				await destination.WriteAsync(new ReadOnlyMemory<byte>(_buffer, _readPos, num), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				_readPos = (_readLen = 0);
			}
			else if (_writePos > 0)
			{
				await FlushWriteAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			await _stream.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			_asyncActiveSemaphore.Release();
		}
	}
}
