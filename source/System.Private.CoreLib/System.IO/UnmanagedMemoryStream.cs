using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

public class UnmanagedMemoryStream : Stream
{
	private SafeBuffer _buffer;

	private unsafe byte* _mem;

	private long _length;

	private long _capacity;

	private long _position;

	private long _offset;

	private FileAccess _access;

	private bool _isOpen;

	private Task<int> _lastReadTask;

	public override bool CanRead
	{
		get
		{
			if (_isOpen)
			{
				return (_access & FileAccess.Read) != 0;
			}
			return false;
		}
	}

	public override bool CanSeek => _isOpen;

	public override bool CanWrite
	{
		get
		{
			if (_isOpen)
			{
				return (_access & FileAccess.Write) != 0;
			}
			return false;
		}
	}

	public override long Length
	{
		get
		{
			EnsureNotClosed();
			return Interlocked.Read(ref _length);
		}
	}

	public long Capacity
	{
		get
		{
			EnsureNotClosed();
			return _capacity;
		}
	}

	public override long Position
	{
		get
		{
			if (!CanSeek)
			{
				ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
			}
			return Interlocked.Read(ref _position);
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (!CanSeek)
			{
				ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
			}
			Interlocked.Exchange(ref _position, value);
		}
	}

	[CLSCompliant(false)]
	public unsafe byte* PositionPointer
	{
		get
		{
			if (_buffer != null)
			{
				throw new NotSupportedException(SR.NotSupported_UmsSafeBuffer);
			}
			EnsureNotClosed();
			long num = Interlocked.Read(ref _position);
			if (num > _capacity)
			{
				throw new IndexOutOfRangeException(SR.IndexOutOfRange_UMSPosition);
			}
			return _mem + num;
		}
		set
		{
			if (_buffer != null)
			{
				throw new NotSupportedException(SR.NotSupported_UmsSafeBuffer);
			}
			EnsureNotClosed();
			if (value < _mem)
			{
				throw new IOException(SR.IO_SeekBeforeBegin);
			}
			long num = (long)value - (long)_mem;
			if (num < 0)
			{
				throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_UnmanagedMemStreamLength);
			}
			Interlocked.Exchange(ref _position, num);
		}
	}

	protected UnmanagedMemoryStream()
	{
	}

	public UnmanagedMemoryStream(SafeBuffer buffer, long offset, long length)
	{
		Initialize(buffer, offset, length, FileAccess.Read);
	}

	public UnmanagedMemoryStream(SafeBuffer buffer, long offset, long length, FileAccess access)
	{
		Initialize(buffer, offset, length, access);
	}

	protected unsafe void Initialize(SafeBuffer buffer, long offset, long length, FileAccess access)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (length < 0)
		{
			throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.ByteLength < (ulong)(offset + length))
		{
			throw new ArgumentException(SR.Argument_InvalidSafeBufferOffLen);
		}
		if (access < FileAccess.Read || access > FileAccess.ReadWrite)
		{
			throw new ArgumentOutOfRangeException("access");
		}
		if (_isOpen)
		{
			throw new InvalidOperationException(SR.InvalidOperation_CalledTwice);
		}
		byte* pointer = null;
		try
		{
			buffer.AcquirePointer(ref pointer);
			if (pointer + offset + length < pointer)
			{
				throw new ArgumentException(SR.ArgumentOutOfRange_UnmanagedMemStreamWrapAround);
			}
		}
		finally
		{
			if (pointer != null)
			{
				buffer.ReleasePointer();
			}
		}
		_offset = offset;
		_buffer = buffer;
		_length = length;
		_capacity = length;
		_access = access;
		_isOpen = true;
	}

	[CLSCompliant(false)]
	public unsafe UnmanagedMemoryStream(byte* pointer, long length)
	{
		Initialize(pointer, length, length, FileAccess.Read);
	}

	[CLSCompliant(false)]
	public unsafe UnmanagedMemoryStream(byte* pointer, long length, long capacity, FileAccess access)
	{
		Initialize(pointer, length, capacity, access);
	}

	[CLSCompliant(false)]
	protected unsafe void Initialize(byte* pointer, long length, long capacity, FileAccess access)
	{
		if (pointer == null)
		{
			throw new ArgumentNullException("pointer");
		}
		if (length < 0 || capacity < 0)
		{
			throw new ArgumentOutOfRangeException((length < 0) ? "length" : "capacity", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (length > capacity)
		{
			throw new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_LengthGreaterThanCapacity);
		}
		if ((nuint)((long)pointer + capacity) < (nuint)pointer)
		{
			throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_UnmanagedMemStreamWrapAround);
		}
		if (access < FileAccess.Read || access > FileAccess.ReadWrite)
		{
			throw new ArgumentOutOfRangeException("access", SR.ArgumentOutOfRange_Enum);
		}
		if (_isOpen)
		{
			throw new InvalidOperationException(SR.InvalidOperation_CalledTwice);
		}
		_mem = pointer;
		_offset = 0L;
		_length = length;
		_capacity = capacity;
		_access = access;
		_isOpen = true;
	}

	protected unsafe override void Dispose(bool disposing)
	{
		_isOpen = false;
		_mem = null;
		base.Dispose(disposing);
	}

	private void EnsureNotClosed()
	{
		if (!_isOpen)
		{
			ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
		}
	}

	private void EnsureReadable()
	{
		if (!CanRead)
		{
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
	}

	private void EnsureWriteable()
	{
		if (!CanWrite)
		{
			ThrowHelper.ThrowNotSupportedException_UnwritableStream();
		}
	}

	public override void Flush()
	{
		EnsureNotClosed();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		try
		{
			Flush();
			return Task.CompletedTask;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return ReadCore(new Span<byte>(buffer, offset, count));
	}

	public override int Read(Span<byte> buffer)
	{
		if (GetType() == typeof(UnmanagedMemoryStream))
		{
			return ReadCore(buffer);
		}
		return base.Read(buffer);
	}

	internal unsafe int ReadCore(Span<byte> buffer)
	{
		EnsureNotClosed();
		EnsureReadable();
		long num = Interlocked.Read(ref _position);
		long num2 = Interlocked.Read(ref _length);
		long num3 = Math.Min(num2 - num, buffer.Length);
		if (num3 <= 0)
		{
			return 0;
		}
		int num4 = (int)num3;
		if (num4 < 0)
		{
			return 0;
		}
		if (_buffer != null)
		{
			byte* pointer = null;
			try
			{
				_buffer.AcquirePointer(ref pointer);
				Buffer.Memmove(ref MemoryMarshal.GetReference(buffer), ref (pointer + num)[_offset], (nuint)num4);
			}
			finally
			{
				if (pointer != null)
				{
					_buffer.ReleasePointer();
				}
			}
		}
		else
		{
			Buffer.Memmove(ref MemoryMarshal.GetReference(buffer), ref _mem[num], (nuint)num4);
		}
		Interlocked.Exchange(ref _position, num + num3);
		return num4;
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<int>(cancellationToken);
		}
		try
		{
			int num = Read(buffer, offset, count);
			Task<int> lastReadTask = _lastReadTask;
			return (lastReadTask != null && lastReadTask.Result == num) ? lastReadTask : (_lastReadTask = Task.FromResult(num));
		}
		catch (Exception exception)
		{
			return Task.FromException<int>(exception);
		}
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		try
		{
			ArraySegment<byte> segment;
			return new ValueTask<int>(MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out segment) ? Read(segment.Array, segment.Offset, segment.Count) : Read(buffer.Span));
		}
		catch (Exception exception)
		{
			return ValueTask.FromException<int>(exception);
		}
	}

	public unsafe override int ReadByte()
	{
		EnsureNotClosed();
		EnsureReadable();
		long num = Interlocked.Read(ref _position);
		long num2 = Interlocked.Read(ref _length);
		if (num >= num2)
		{
			return -1;
		}
		Interlocked.Exchange(ref _position, num + 1);
		if (_buffer != null)
		{
			byte* pointer = null;
			try
			{
				_buffer.AcquirePointer(ref pointer);
				return (pointer + num)[_offset];
			}
			finally
			{
				if (pointer != null)
				{
					_buffer.ReleasePointer();
				}
			}
		}
		return _mem[num];
	}

	public override long Seek(long offset, SeekOrigin loc)
	{
		EnsureNotClosed();
		switch (loc)
		{
		case SeekOrigin.Begin:
			if (offset < 0)
			{
				throw new IOException(SR.IO_SeekBeforeBegin);
			}
			Interlocked.Exchange(ref _position, offset);
			break;
		case SeekOrigin.Current:
		{
			long num2 = Interlocked.Read(ref _position);
			if (offset + num2 < 0)
			{
				throw new IOException(SR.IO_SeekBeforeBegin);
			}
			Interlocked.Exchange(ref _position, offset + num2);
			break;
		}
		case SeekOrigin.End:
		{
			long num = Interlocked.Read(ref _length);
			if (num + offset < 0)
			{
				throw new IOException(SR.IO_SeekBeforeBegin);
			}
			Interlocked.Exchange(ref _position, num + offset);
			break;
		}
		default:
			throw new ArgumentException(SR.Argument_InvalidSeekOrigin);
		}
		return Interlocked.Read(ref _position);
	}

	public unsafe override void SetLength(long value)
	{
		if (value < 0)
		{
			throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (_buffer != null)
		{
			throw new NotSupportedException(SR.NotSupported_UmsSafeBuffer);
		}
		EnsureNotClosed();
		EnsureWriteable();
		if (value > _capacity)
		{
			throw new IOException(SR.IO_FixedCapacity);
		}
		long num = Interlocked.Read(ref _position);
		long num2 = Interlocked.Read(ref _length);
		if (value > num2)
		{
			Buffer.ZeroMemory(_mem + num2, (nuint)(value - num2));
		}
		Interlocked.Exchange(ref _length, value);
		if (num > value)
		{
			Interlocked.Exchange(ref _position, value);
		}
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		WriteCore(new ReadOnlySpan<byte>(buffer, offset, count));
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		if (GetType() == typeof(UnmanagedMemoryStream))
		{
			WriteCore(buffer);
		}
		else
		{
			base.Write(buffer);
		}
	}

	internal unsafe void WriteCore(ReadOnlySpan<byte> buffer)
	{
		EnsureNotClosed();
		EnsureWriteable();
		long num = Interlocked.Read(ref _position);
		long num2 = Interlocked.Read(ref _length);
		long num3 = num + buffer.Length;
		if (num3 < 0)
		{
			throw new IOException(SR.IO_StreamTooLong);
		}
		if (num3 > _capacity)
		{
			throw new NotSupportedException(SR.IO_FixedCapacity);
		}
		if (_buffer == null)
		{
			if (num > num2)
			{
				Buffer.ZeroMemory(_mem + num2, (nuint)(num - num2));
			}
			if (num3 > num2)
			{
				Interlocked.Exchange(ref _length, num3);
			}
		}
		if (_buffer != null)
		{
			long num4 = _capacity - num;
			if (num4 < buffer.Length)
			{
				throw new ArgumentException(SR.Arg_BufferTooSmall);
			}
			byte* pointer = null;
			try
			{
				_buffer.AcquirePointer(ref pointer);
				Buffer.Memmove(ref (pointer + num)[_offset], ref MemoryMarshal.GetReference(buffer), (nuint)buffer.Length);
			}
			finally
			{
				if (pointer != null)
				{
					_buffer.ReleasePointer();
				}
			}
		}
		else
		{
			Buffer.Memmove(ref _mem[num], ref MemoryMarshal.GetReference(buffer), (nuint)buffer.Length);
		}
		Interlocked.Exchange(ref _position, num3);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		try
		{
			Write(buffer, offset, count);
			return Task.CompletedTask;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		try
		{
			if (MemoryMarshal.TryGetArray(buffer, out var segment))
			{
				Write(segment.Array, segment.Offset, segment.Count);
			}
			else
			{
				Write(buffer.Span);
			}
			return default(ValueTask);
		}
		catch (Exception exception)
		{
			return ValueTask.FromException(exception);
		}
	}

	public unsafe override void WriteByte(byte value)
	{
		EnsureNotClosed();
		EnsureWriteable();
		long num = Interlocked.Read(ref _position);
		long num2 = Interlocked.Read(ref _length);
		long num3 = num + 1;
		if (num >= num2)
		{
			if (num3 < 0)
			{
				throw new IOException(SR.IO_StreamTooLong);
			}
			if (num3 > _capacity)
			{
				throw new NotSupportedException(SR.IO_FixedCapacity);
			}
			if (_buffer == null)
			{
				if (num > num2)
				{
					Buffer.ZeroMemory(_mem + num2, (nuint)(num - num2));
				}
				Interlocked.Exchange(ref _length, num3);
			}
		}
		if (_buffer != null)
		{
			byte* pointer = null;
			try
			{
				_buffer.AcquirePointer(ref pointer);
				(pointer + num)[_offset] = value;
			}
			finally
			{
				if (pointer != null)
				{
					_buffer.ReleasePointer();
				}
			}
		}
		else
		{
			_mem[num] = value;
		}
		Interlocked.Exchange(ref _position, num3);
	}
}
