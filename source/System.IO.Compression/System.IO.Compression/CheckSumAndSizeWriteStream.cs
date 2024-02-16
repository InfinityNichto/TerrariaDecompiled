using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

internal sealed class CheckSumAndSizeWriteStream : Stream
{
	private readonly Stream _baseStream;

	private readonly Stream _baseBaseStream;

	private long _position;

	private uint _checksum;

	private readonly bool _leaveOpenOnClose;

	private readonly bool _canWrite;

	private bool _isDisposed;

	private bool _everWritten;

	private long _initialPosition;

	private readonly ZipArchiveEntry _zipArchiveEntry;

	private readonly EventHandler _onClose;

	private readonly Action<long, long, uint, Stream, ZipArchiveEntry, EventHandler> _saveCrcAndSizes;

	public override long Length
	{
		get
		{
			ThrowIfDisposed();
			throw new NotSupportedException(System.SR.SeekingNotSupported);
		}
	}

	public override long Position
	{
		get
		{
			ThrowIfDisposed();
			return _position;
		}
		set
		{
			ThrowIfDisposed();
			throw new NotSupportedException(System.SR.SeekingNotSupported);
		}
	}

	public override bool CanRead => false;

	public override bool CanSeek => false;

	public override bool CanWrite => _canWrite;

	public CheckSumAndSizeWriteStream(Stream baseStream, Stream baseBaseStream, bool leaveOpenOnClose, ZipArchiveEntry entry, EventHandler onClose, Action<long, long, uint, Stream, ZipArchiveEntry, EventHandler> saveCrcAndSizes)
	{
		_baseStream = baseStream;
		_baseBaseStream = baseBaseStream;
		_position = 0L;
		_checksum = 0u;
		_leaveOpenOnClose = leaveOpenOnClose;
		_canWrite = true;
		_isDisposed = false;
		_initialPosition = 0L;
		_zipArchiveEntry = entry;
		_onClose = onClose;
		_saveCrcAndSizes = saveCrcAndSizes;
	}

	private void ThrowIfDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(GetType().ToString(), System.SR.HiddenStreamName);
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		ThrowIfDisposed();
		throw new NotSupportedException(System.SR.ReadingNotSupported);
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
		Stream.ValidateBufferArguments(buffer, offset, count);
		ThrowIfDisposed();
		if (count != 0)
		{
			if (!_everWritten)
			{
				_initialPosition = _baseBaseStream.Position;
				_everWritten = true;
			}
			_checksum = Crc32Helper.UpdateCrc32(_checksum, buffer, offset, count);
			_baseStream.Write(buffer, offset, count);
			_position += count;
		}
	}

	public override void Write(ReadOnlySpan<byte> source)
	{
		ThrowIfDisposed();
		if (source.Length != 0)
		{
			if (!_everWritten)
			{
				_initialPosition = _baseBaseStream.Position;
				_everWritten = true;
			}
			_checksum = Crc32Helper.UpdateCrc32(_checksum, source);
			_baseStream.Write(source);
			_position += source.Length;
		}
	}

	public override void WriteByte(byte value)
	{
		Write(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		if (buffer.IsEmpty)
		{
			return default(ValueTask);
		}
		return Core(buffer, cancellationToken);
		async ValueTask Core(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!_everWritten)
			{
				_initialPosition = _baseBaseStream.Position;
				_everWritten = true;
			}
			_checksum = Crc32Helper.UpdateCrc32(_checksum, buffer.Span);
			await _baseStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			_position += buffer.Length;
		}
	}

	public override void Flush()
	{
		ThrowIfDisposed();
		_baseStream.Flush();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		return _baseStream.FlushAsync(cancellationToken);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !_isDisposed)
		{
			if (!_everWritten)
			{
				_initialPosition = _baseBaseStream.Position;
			}
			if (!_leaveOpenOnClose)
			{
				_baseStream.Dispose();
			}
			_saveCrcAndSizes?.Invoke(_initialPosition, Position, _checksum, _baseBaseStream, _zipArchiveEntry, _onClose);
			_isDisposed = true;
		}
		base.Dispose(disposing);
	}
}
