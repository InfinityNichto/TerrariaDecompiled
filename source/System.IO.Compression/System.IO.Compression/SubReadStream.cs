using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

internal sealed class SubReadStream : Stream
{
	private readonly long _startInSuperStream;

	private long _positionInSuperStream;

	private readonly long _endInSuperStream;

	private readonly Stream _superStream;

	private bool _canRead;

	private bool _isDisposed;

	public override long Length
	{
		get
		{
			ThrowIfDisposed();
			return _endInSuperStream - _startInSuperStream;
		}
	}

	public override long Position
	{
		get
		{
			ThrowIfDisposed();
			return _positionInSuperStream - _startInSuperStream;
		}
		set
		{
			ThrowIfDisposed();
			throw new NotSupportedException(System.SR.SeekingNotSupported);
		}
	}

	public override bool CanRead
	{
		get
		{
			if (_superStream.CanRead)
			{
				return _canRead;
			}
			return false;
		}
	}

	public override bool CanSeek => false;

	public override bool CanWrite => false;

	public SubReadStream(Stream superStream, long startPosition, long maxLength)
	{
		_startInSuperStream = startPosition;
		_positionInSuperStream = startPosition;
		_endInSuperStream = startPosition + maxLength;
		_superStream = superStream;
		_canRead = true;
		_isDisposed = false;
	}

	private void ThrowIfDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(GetType().ToString(), System.SR.HiddenStreamName);
		}
	}

	private void ThrowIfCantRead()
	{
		if (!CanRead)
		{
			throw new NotSupportedException(System.SR.ReadingNotSupported);
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		int num = count;
		ThrowIfDisposed();
		ThrowIfCantRead();
		if (_superStream.Position != _positionInSuperStream)
		{
			_superStream.Seek(_positionInSuperStream, SeekOrigin.Begin);
		}
		if (_positionInSuperStream + count > _endInSuperStream)
		{
			count = (int)(_endInSuperStream - _positionInSuperStream);
		}
		int num2 = _superStream.Read(buffer, offset, count);
		_positionInSuperStream += num2;
		return num2;
	}

	public override int Read(Span<byte> destination)
	{
		int length = destination.Length;
		int num = destination.Length;
		ThrowIfDisposed();
		ThrowIfCantRead();
		if (_superStream.Position != _positionInSuperStream)
		{
			_superStream.Seek(_positionInSuperStream, SeekOrigin.Begin);
		}
		if (_positionInSuperStream + num > _endInSuperStream)
		{
			num = (int)(_endInSuperStream - _positionInSuperStream);
		}
		int num2 = _superStream.Read(destination.Slice(0, num));
		_positionInSuperStream += num2;
		return num2;
	}

	public override int ReadByte()
	{
		byte reference = 0;
		if (Read(MemoryMarshal.CreateSpan(ref reference, 1)) != 1)
		{
			return -1;
		}
		return reference;
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		ThrowIfCantRead();
		return Core(buffer, cancellationToken);
		async ValueTask<int> Core(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			if (_superStream.Position != _positionInSuperStream)
			{
				_superStream.Seek(_positionInSuperStream, SeekOrigin.Begin);
			}
			if (_positionInSuperStream > _endInSuperStream - buffer.Length)
			{
				buffer = buffer.Slice(0, (int)(_endInSuperStream - _positionInSuperStream));
			}
			int num = await _superStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			_positionInSuperStream += num;
			return num;
		}
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		ThrowIfDisposed();
		throw new NotSupportedException(System.SR.SeekingNotSupported);
	}

	public override void SetLength(long value)
	{
		ThrowIfDisposed();
		throw new NotSupportedException(System.SR.SetLengthRequiresSeekingAndWriting);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		ThrowIfDisposed();
		throw new NotSupportedException(System.SR.WritingNotSupported);
	}

	public override void Flush()
	{
		ThrowIfDisposed();
		throw new NotSupportedException(System.SR.WritingNotSupported);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !_isDisposed)
		{
			_canRead = false;
			_isDisposed = true;
		}
		base.Dispose(disposing);
	}
}
