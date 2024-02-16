using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

internal sealed class DeflateManagedStream : Stream
{
	private Stream _stream;

	private InflaterManaged _inflater;

	private readonly byte[] _buffer;

	private int _asyncOperations;

	public override bool CanRead
	{
		get
		{
			if (_stream == null)
			{
				return false;
			}
			return _stream.CanRead;
		}
	}

	public override bool CanWrite => false;

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

	internal DeflateManagedStream(Stream stream, ZipArchiveEntry.CompressionMethodValues method, long uncompressedSize = -1L)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (!stream.CanRead)
		{
			throw new ArgumentException(System.SR.NotSupported_UnreadableStream, "stream");
		}
		if (!stream.CanRead)
		{
			throw new ArgumentException(System.SR.NotSupported_UnreadableStream, "stream");
		}
		_inflater = new InflaterManaged(method == ZipArchiveEntry.CompressionMethodValues.Deflate64, uncompressedSize);
		_stream = stream;
		_buffer = new byte[8192];
	}

	public override void Flush()
	{
		EnsureNotDisposed();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		EnsureNotDisposed();
		if (!cancellationToken.IsCancellationRequested)
		{
			return Task.CompletedTask;
		}
		return Task.FromCanceled(cancellationToken);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException(System.SR.NotSupported);
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException(System.SR.NotSupported);
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		return Read(new Span<byte>(buffer, offset, count));
	}

	public override int Read(Span<byte> buffer)
	{
		EnsureNotDisposed();
		int length = buffer.Length;
		while (true)
		{
			int start = _inflater.Inflate(buffer);
			buffer = buffer.Slice(start);
			if (buffer.Length == 0 || _inflater.Finished())
			{
				break;
			}
			int num = _stream.Read(_buffer, 0, _buffer.Length);
			if (num <= 0)
			{
				break;
			}
			if (num > _buffer.Length)
			{
				throw new InvalidDataException(System.SR.GenericInvalidData);
			}
			_inflater.SetInput(_buffer, 0, num);
		}
		return length - buffer.Length;
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

	private void EnsureNotDisposed()
	{
		if (_stream == null)
		{
			ThrowStreamClosedException();
		}
	}

	private static void ThrowStreamClosedException()
	{
		throw new ObjectDisposedException("DeflateStream", System.SR.ObjectDisposed_StreamClosed);
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState)
	{
		return System.Threading.Tasks.TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return System.Threading.Tasks.TaskToApm.End<int>(asyncResult);
	}

	private ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		Interlocked.Increment(ref _asyncOperations);
		bool flag = false;
		try
		{
			int num = _inflater.Inflate(buffer.Span);
			if (num != 0)
			{
				return ValueTask.FromResult(num);
			}
			if (_inflater.Finished())
			{
				return ValueTask.FromResult(0);
			}
			ValueTask<int> readTask = _stream.ReadAsync(_buffer.AsMemory(), cancellationToken);
			flag = true;
			return ReadAsyncCore(readTask, buffer, cancellationToken);
		}
		finally
		{
			if (!flag)
			{
				Interlocked.Decrement(ref _asyncOperations);
			}
		}
	}

	private async ValueTask<int> ReadAsyncCore(ValueTask<int> readTask, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		try
		{
			int num;
			while (true)
			{
				num = await readTask.ConfigureAwait(continueOnCapturedContext: false);
				EnsureNotDisposed();
				if (num <= 0)
				{
					return 0;
				}
				if (num > _buffer.Length)
				{
					throw new InvalidDataException(System.SR.GenericInvalidData);
				}
				cancellationToken.ThrowIfCancellationRequested();
				_inflater.SetInput(_buffer, 0, num);
				num = _inflater.Inflate(buffer.Span);
				if (num != 0 || _inflater.Finished())
				{
					break;
				}
				readTask = _stream.ReadAsync(_buffer.AsMemory(), cancellationToken);
			}
			return num;
		}
		finally
		{
			Interlocked.Decrement(ref _asyncOperations);
		}
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (_asyncOperations != 0)
		{
			throw new InvalidOperationException(System.SR.InvalidBeginCall);
		}
		Stream.ValidateBufferArguments(buffer, offset, count);
		EnsureNotDisposed();
		return ReadAsyncInternal(buffer.AsMemory(offset, count), cancellationToken).AsTask();
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_asyncOperations != 0)
		{
			throw new InvalidOperationException(System.SR.InvalidBeginCall);
		}
		EnsureNotDisposed();
		return ReadAsyncInternal(buffer, cancellationToken);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new InvalidOperationException(System.SR.CannotWriteToDeflateStream);
	}

	private void PurgeBuffers(bool disposing)
	{
		if (disposing && _stream != null)
		{
			Flush();
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
				if (disposing && _stream != null)
				{
					_stream.Dispose();
				}
			}
			finally
			{
				_stream = null;
				try
				{
					_inflater?.Dispose();
				}
				finally
				{
					_inflater = null;
					base.Dispose(disposing);
				}
			}
		}
	}
}
