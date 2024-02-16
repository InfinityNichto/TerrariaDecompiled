using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

internal sealed class ReadOnlyMemoryStream : Stream
{
	private ReadOnlyMemory<byte> _content;

	private int _position;

	private bool _isOpen;

	public override bool CanRead => _isOpen;

	public override bool CanSeek => _isOpen;

	public override bool CanWrite => false;

	public override long Length
	{
		get
		{
			EnsureNotClosed();
			return _content.Length;
		}
	}

	public override long Position
	{
		get
		{
			EnsureNotClosed();
			return _position;
		}
		set
		{
			EnsureNotClosed();
			if (value < 0 || value > int.MaxValue)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_position = (int)value;
		}
	}

	public ReadOnlyMemoryStream(ReadOnlyMemory<byte> content)
	{
		_content = content;
		_isOpen = true;
	}

	private void EnsureNotClosed()
	{
		if (!_isOpen)
		{
			throw new ObjectDisposedException(null, System.SR.ObjectDisposed_StreamClosed);
		}
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		EnsureNotClosed();
		long num = origin switch
		{
			SeekOrigin.End => _content.Length + offset, 
			SeekOrigin.Current => _position + offset, 
			SeekOrigin.Begin => offset, 
			_ => throw new ArgumentOutOfRangeException("origin"), 
		};
		if (num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (num < 0)
		{
			throw new IOException(System.SR.IO_SeekBeforeBegin);
		}
		_position = (int)num;
		return _position;
	}

	public override int ReadByte()
	{
		EnsureNotClosed();
		ReadOnlySpan<byte> span = _content.Span;
		if (_position >= span.Length)
		{
			return -1;
		}
		return span[_position++];
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return ReadBuffer(new Span<byte>(buffer, offset, count));
	}

	public override int Read(Span<byte> buffer)
	{
		return ReadBuffer(buffer);
	}

	private int ReadBuffer(Span<byte> buffer)
	{
		EnsureNotClosed();
		int num = _content.Length - _position;
		if (num <= 0 || buffer.Length == 0)
		{
			return 0;
		}
		ReadOnlySpan<byte> readOnlySpan;
		if (num <= buffer.Length)
		{
			readOnlySpan = _content.Span;
			readOnlySpan = readOnlySpan.Slice(_position);
			readOnlySpan.CopyTo(buffer);
			_position = _content.Length;
			return num;
		}
		readOnlySpan = _content.Span;
		readOnlySpan = readOnlySpan.Slice(_position, buffer.Length);
		readOnlySpan.CopyTo(buffer);
		_position += buffer.Length;
		return buffer.Length;
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		EnsureNotClosed();
		if (!cancellationToken.IsCancellationRequested)
		{
			return Task.FromResult(ReadBuffer(new Span<byte>(buffer, offset, count)));
		}
		return Task.FromCanceled<int>(cancellationToken);
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		EnsureNotClosed();
		if (!cancellationToken.IsCancellationRequested)
		{
			return new ValueTask<int>(ReadBuffer(buffer.Span));
		}
		return ValueTask.FromCanceled<int>(cancellationToken);
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		EnsureNotClosed();
		return System.Threading.Tasks.TaskToApm.End<int>(asyncResult);
	}

	public override void CopyTo(Stream destination, int bufferSize)
	{
		Stream.ValidateCopyToArguments(destination, bufferSize);
		EnsureNotClosed();
		if (_content.Length > _position)
		{
			destination.Write(_content.Span.Slice(_position));
		}
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		Stream.ValidateCopyToArguments(destination, bufferSize);
		EnsureNotClosed();
		if (_content.Length <= _position)
		{
			return Task.CompletedTask;
		}
		return destination.WriteAsync(_content.Slice(_position), cancellationToken).AsTask();
	}

	public override void Flush()
	{
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
	}

	protected override void Dispose(bool disposing)
	{
		_isOpen = false;
		_content = default(ReadOnlyMemory<byte>);
		base.Dispose(disposing);
	}
}
