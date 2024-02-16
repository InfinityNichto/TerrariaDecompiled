using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net;

internal abstract class DelegatedStream : Stream
{
	private readonly Stream _stream;

	protected Stream BaseStream => _stream;

	public override bool CanRead => _stream.CanRead;

	public override bool CanSeek => _stream.CanSeek;

	public override bool CanWrite => _stream.CanWrite;

	public override long Length
	{
		get
		{
			if (!CanSeek)
			{
				throw new NotSupportedException(System.SR.SeekNotSupported);
			}
			return _stream.Length;
		}
	}

	public override long Position
	{
		get
		{
			if (!CanSeek)
			{
				throw new NotSupportedException(System.SR.SeekNotSupported);
			}
			return _stream.Position;
		}
		set
		{
			if (!CanSeek)
			{
				throw new NotSupportedException(System.SR.SeekNotSupported);
			}
			_stream.Position = value;
		}
	}

	protected DelegatedStream(Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		_stream = stream;
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		if (!CanRead)
		{
			throw new NotSupportedException(System.SR.ReadNotSupported);
		}
		return _stream.BeginRead(buffer, offset, count, callback, state);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		if (!CanWrite)
		{
			throw new NotSupportedException(System.SR.WriteNotSupported);
		}
		return _stream.BeginWrite(buffer, offset, count, callback, state);
	}

	public override void Close()
	{
		_stream.Close();
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		if (!CanRead)
		{
			throw new NotSupportedException(System.SR.ReadNotSupported);
		}
		return _stream.EndRead(asyncResult);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		if (!CanWrite)
		{
			throw new NotSupportedException(System.SR.WriteNotSupported);
		}
		_stream.EndWrite(asyncResult);
	}

	public override void Flush()
	{
		_stream.Flush();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		return _stream.FlushAsync(cancellationToken);
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		if (!CanRead)
		{
			throw new NotSupportedException(System.SR.ReadNotSupported);
		}
		return _stream.Read(buffer, offset, count);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (!CanRead)
		{
			throw new NotSupportedException(System.SR.ReadNotSupported);
		}
		return _stream.ReadAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!CanRead)
		{
			throw new NotSupportedException(System.SR.ReadNotSupported);
		}
		return _stream.ReadAsync(buffer, cancellationToken);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		if (!CanSeek)
		{
			throw new NotSupportedException(System.SR.SeekNotSupported);
		}
		return _stream.Seek(offset, origin);
	}

	public override void SetLength(long value)
	{
		if (!CanSeek)
		{
			throw new NotSupportedException(System.SR.SeekNotSupported);
		}
		_stream.SetLength(value);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		if (!CanWrite)
		{
			throw new NotSupportedException(System.SR.WriteNotSupported);
		}
		_stream.Write(buffer, offset, count);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (!CanWrite)
		{
			throw new NotSupportedException(System.SR.WriteNotSupported);
		}
		return _stream.WriteAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!CanWrite)
		{
			throw new NotSupportedException(System.SR.WriteNotSupported);
		}
		return _stream.WriteAsync(buffer, cancellationToken);
	}
}
