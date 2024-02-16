using System.Runtime.InteropServices;

namespace System.IO;

internal abstract class ConsoleStream : Stream
{
	private bool _canRead;

	private bool _canWrite;

	public sealed override bool CanRead => _canRead;

	public sealed override bool CanWrite => _canWrite;

	public sealed override bool CanSeek => false;

	public sealed override long Length
	{
		get
		{
			throw Error.GetSeekNotSupported();
		}
	}

	public sealed override long Position
	{
		get
		{
			throw Error.GetSeekNotSupported();
		}
		set
		{
			throw Error.GetSeekNotSupported();
		}
	}

	internal ConsoleStream(FileAccess access)
	{
		_canRead = (access & FileAccess.Read) == FileAccess.Read;
		_canWrite = (access & FileAccess.Write) == FileAccess.Write;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		ValidateWrite(buffer, offset, count);
		Write(new ReadOnlySpan<byte>(buffer, offset, count));
	}

	public override void WriteByte(byte value)
	{
		Write(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		ValidateRead(buffer, offset, count);
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

	protected override void Dispose(bool disposing)
	{
		_canRead = false;
		_canWrite = false;
		base.Dispose(disposing);
	}

	public override void Flush()
	{
		if (!CanWrite)
		{
			throw Error.GetWriteNotSupported();
		}
	}

	public sealed override void SetLength(long value)
	{
		throw Error.GetSeekNotSupported();
	}

	public sealed override long Seek(long offset, SeekOrigin origin)
	{
		throw Error.GetSeekNotSupported();
	}

	protected void ValidateRead(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (!_canRead)
		{
			throw Error.GetReadNotSupported();
		}
	}

	protected void ValidateWrite(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (!_canWrite)
		{
			throw Error.GetWriteNotSupported();
		}
	}
}
