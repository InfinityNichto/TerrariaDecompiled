using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

public class MemoryStream : Stream
{
	private byte[] _buffer;

	private readonly int _origin;

	private int _position;

	private int _length;

	private int _capacity;

	private bool _expandable;

	private bool _writable;

	private readonly bool _exposable;

	private bool _isOpen;

	private Task<int> _lastReadTask;

	public override bool CanRead => _isOpen;

	public override bool CanSeek => _isOpen;

	public override bool CanWrite => _writable;

	public virtual int Capacity
	{
		get
		{
			EnsureNotClosed();
			return _capacity - _origin;
		}
		set
		{
			if (value < Length)
			{
				throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_SmallCapacity);
			}
			EnsureNotClosed();
			if (!_expandable && value != Capacity)
			{
				throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);
			}
			if (!_expandable || value == _capacity)
			{
				return;
			}
			if (value > 0)
			{
				byte[] array = new byte[value];
				if (_length > 0)
				{
					Buffer.BlockCopy(_buffer, 0, array, 0, _length);
				}
				_buffer = array;
			}
			else
			{
				_buffer = Array.Empty<byte>();
			}
			_capacity = value;
		}
	}

	public override long Length
	{
		get
		{
			EnsureNotClosed();
			return _length - _origin;
		}
	}

	public override long Position
	{
		get
		{
			EnsureNotClosed();
			return _position - _origin;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			EnsureNotClosed();
			if (value > int.MaxValue)
			{
				throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_StreamLength);
			}
			_position = _origin + (int)value;
		}
	}

	public MemoryStream()
		: this(0)
	{
	}

	public MemoryStream(int capacity)
	{
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", SR.ArgumentOutOfRange_NegativeCapacity);
		}
		_buffer = ((capacity != 0) ? new byte[capacity] : Array.Empty<byte>());
		_capacity = capacity;
		_expandable = true;
		_writable = true;
		_exposable = true;
		_isOpen = true;
	}

	public MemoryStream(byte[] buffer)
		: this(buffer, writable: true)
	{
	}

	public MemoryStream(byte[] buffer, bool writable)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
		}
		_buffer = buffer;
		_length = (_capacity = buffer.Length);
		_writable = writable;
		_isOpen = true;
	}

	public MemoryStream(byte[] buffer, int index, int count)
		: this(buffer, index, count, writable: true, publiclyVisible: false)
	{
	}

	public MemoryStream(byte[] buffer, int index, int count, bool writable)
		: this(buffer, index, count, writable, publiclyVisible: false)
	{
	}

	public MemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		_buffer = buffer;
		_origin = (_position = index);
		_length = (_capacity = index + count);
		_writable = writable;
		_exposable = publiclyVisible;
		_isOpen = true;
	}

	private void EnsureNotClosed()
	{
		if (!_isOpen)
		{
			ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
		}
	}

	private void EnsureWriteable()
	{
		if (!CanWrite)
		{
			ThrowHelper.ThrowNotSupportedException_UnwritableStream();
		}
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing)
			{
				_isOpen = false;
				_writable = false;
				_expandable = false;
				_lastReadTask = null;
			}
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	private bool EnsureCapacity(int value)
	{
		if (value < 0)
		{
			throw new IOException(SR.IO_StreamTooLong);
		}
		if (value > _capacity)
		{
			int num = Math.Max(value, 256);
			if (num < _capacity * 2)
			{
				num = _capacity * 2;
			}
			if ((uint)(_capacity * 2) > Array.MaxLength)
			{
				num = Math.Max(value, Array.MaxLength);
			}
			Capacity = num;
			return true;
		}
		return false;
	}

	public override void Flush()
	{
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

	public virtual byte[] GetBuffer()
	{
		if (!_exposable)
		{
			throw new UnauthorizedAccessException(SR.UnauthorizedAccess_MemStreamBuffer);
		}
		return _buffer;
	}

	public virtual bool TryGetBuffer(out ArraySegment<byte> buffer)
	{
		if (!_exposable)
		{
			buffer = default(ArraySegment<byte>);
			return false;
		}
		buffer = new ArraySegment<byte>(_buffer, _origin, _length - _origin);
		return true;
	}

	internal byte[] InternalGetBuffer()
	{
		return _buffer;
	}

	internal int InternalGetPosition()
	{
		return _position;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ReadOnlySpan<byte> InternalReadSpan(int count)
	{
		EnsureNotClosed();
		int position = _position;
		int num = position + count;
		if ((uint)num > (uint)_length)
		{
			_position = _length;
			ThrowHelper.ThrowEndOfFileException();
		}
		ReadOnlySpan<byte> result = new ReadOnlySpan<byte>(_buffer, position, count);
		_position = num;
		return result;
	}

	internal int InternalEmulateRead(int count)
	{
		EnsureNotClosed();
		int num = _length - _position;
		if (num > count)
		{
			num = count;
		}
		if (num < 0)
		{
			num = 0;
		}
		_position += num;
		return num;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		EnsureNotClosed();
		int num = _length - _position;
		if (num > count)
		{
			num = count;
		}
		if (num <= 0)
		{
			return 0;
		}
		if (num <= 8)
		{
			int num2 = num;
			while (--num2 >= 0)
			{
				buffer[offset + num2] = _buffer[_position + num2];
			}
		}
		else
		{
			Buffer.BlockCopy(_buffer, _position, buffer, offset, num);
		}
		_position += num;
		return num;
	}

	public override int Read(Span<byte> buffer)
	{
		if (GetType() != typeof(MemoryStream))
		{
			return base.Read(buffer);
		}
		EnsureNotClosed();
		int num = Math.Min(_length - _position, buffer.Length);
		if (num <= 0)
		{
			return 0;
		}
		new Span<byte>(_buffer, _position, num).CopyTo(buffer);
		_position += num;
		return num;
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
		catch (OperationCanceledException exception)
		{
			return Task.FromCanceled<int>(exception);
		}
		catch (Exception exception2)
		{
			return Task.FromException<int>(exception2);
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
		catch (OperationCanceledException exception)
		{
			return new ValueTask<int>(Task.FromCanceled<int>(exception));
		}
		catch (Exception exception2)
		{
			return ValueTask.FromException<int>(exception2);
		}
	}

	public override int ReadByte()
	{
		EnsureNotClosed();
		if (_position >= _length)
		{
			return -1;
		}
		return _buffer[_position++];
	}

	public override void CopyTo(Stream destination, int bufferSize)
	{
		if (GetType() != typeof(MemoryStream))
		{
			base.CopyTo(destination, bufferSize);
			return;
		}
		Stream.ValidateCopyToArguments(destination, bufferSize);
		EnsureNotClosed();
		int position = _position;
		int num = InternalEmulateRead(_length - position);
		if (num > 0)
		{
			destination.Write(_buffer, position, num);
		}
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		Stream.ValidateCopyToArguments(destination, bufferSize);
		EnsureNotClosed();
		if (GetType() != typeof(MemoryStream))
		{
			return base.CopyToAsync(destination, bufferSize, cancellationToken);
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		int position = _position;
		int num = InternalEmulateRead(_length - _position);
		if (num == 0)
		{
			return Task.CompletedTask;
		}
		if (!(destination is MemoryStream memoryStream))
		{
			return destination.WriteAsync(_buffer, position, num, cancellationToken);
		}
		try
		{
			memoryStream.Write(_buffer, position, num);
			return Task.CompletedTask;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
	}

	public override long Seek(long offset, SeekOrigin loc)
	{
		EnsureNotClosed();
		if (offset > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("offset", SR.ArgumentOutOfRange_StreamLength);
		}
		switch (loc)
		{
		case SeekOrigin.Begin:
		{
			int num3 = _origin + (int)offset;
			if (offset < 0 || num3 < _origin)
			{
				throw new IOException(SR.IO_SeekBeforeBegin);
			}
			_position = num3;
			break;
		}
		case SeekOrigin.Current:
		{
			int num2 = _position + (int)offset;
			if (_position + offset < _origin || num2 < _origin)
			{
				throw new IOException(SR.IO_SeekBeforeBegin);
			}
			_position = num2;
			break;
		}
		case SeekOrigin.End:
		{
			int num = _length + (int)offset;
			if (_length + offset < _origin || num < _origin)
			{
				throw new IOException(SR.IO_SeekBeforeBegin);
			}
			_position = num;
			break;
		}
		default:
			throw new ArgumentException(SR.Argument_InvalidSeekOrigin);
		}
		return _position;
	}

	public override void SetLength(long value)
	{
		if (value < 0 || value > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_StreamLength);
		}
		EnsureWriteable();
		if (value > int.MaxValue - _origin)
		{
			throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_StreamLength);
		}
		int num = _origin + (int)value;
		if (!EnsureCapacity(num) && num > _length)
		{
			Array.Clear(_buffer, _length, num - _length);
		}
		_length = num;
		if (_position > num)
		{
			_position = num;
		}
	}

	public virtual byte[] ToArray()
	{
		int num = _length - _origin;
		if (num == 0)
		{
			return Array.Empty<byte>();
		}
		byte[] array = GC.AllocateUninitializedArray<byte>(num);
		_buffer.AsSpan(_origin, num).CopyTo(array);
		return array;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		EnsureNotClosed();
		EnsureWriteable();
		int num = _position + count;
		if (num < 0)
		{
			throw new IOException(SR.IO_StreamTooLong);
		}
		if (num > _length)
		{
			bool flag = _position > _length;
			if (num > _capacity && EnsureCapacity(num))
			{
				flag = false;
			}
			if (flag)
			{
				Array.Clear(_buffer, _length, num - _length);
			}
			_length = num;
		}
		if (count <= 8 && buffer != _buffer)
		{
			int num2 = count;
			while (--num2 >= 0)
			{
				_buffer[_position + num2] = buffer[offset + num2];
			}
		}
		else
		{
			Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
		}
		_position = num;
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		if (GetType() != typeof(MemoryStream))
		{
			base.Write(buffer);
			return;
		}
		EnsureNotClosed();
		EnsureWriteable();
		int num = _position + buffer.Length;
		if (num < 0)
		{
			throw new IOException(SR.IO_StreamTooLong);
		}
		if (num > _length)
		{
			bool flag = _position > _length;
			if (num > _capacity && EnsureCapacity(num))
			{
				flag = false;
			}
			if (flag)
			{
				Array.Clear(_buffer, _length, num - _length);
			}
			_length = num;
		}
		buffer.CopyTo(new Span<byte>(_buffer, _position, buffer.Length));
		_position = num;
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
		catch (OperationCanceledException exception)
		{
			return Task.FromCanceled(exception);
		}
		catch (Exception exception2)
		{
			return Task.FromException(exception2);
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
		catch (OperationCanceledException exception)
		{
			return new ValueTask(Task.FromCanceled(exception));
		}
		catch (Exception exception2)
		{
			return ValueTask.FromException(exception2);
		}
	}

	public override void WriteByte(byte value)
	{
		EnsureNotClosed();
		EnsureWriteable();
		if (_position >= _length)
		{
			int num = _position + 1;
			bool flag = _position > _length;
			if (num >= _capacity && EnsureCapacity(num))
			{
				flag = false;
			}
			if (flag)
			{
				Array.Clear(_buffer, _length, _position - _length);
			}
			_length = num;
		}
		_buffer[_position++] = value;
	}

	public virtual void WriteTo(Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream", SR.ArgumentNull_Stream);
		}
		EnsureNotClosed();
		stream.Write(_buffer, _origin, _length - _origin);
	}
}
