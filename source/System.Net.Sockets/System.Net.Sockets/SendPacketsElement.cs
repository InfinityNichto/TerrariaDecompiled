using System.IO;

namespace System.Net.Sockets;

public class SendPacketsElement
{
	public string? FilePath { get; private set; }

	public FileStream? FileStream { get; private set; }

	public byte[]? Buffer { get; private set; }

	public int Count { get; private set; }

	public ReadOnlyMemory<byte>? MemoryBuffer { get; private set; }

	public int Offset => checked((int)OffsetLong);

	public long OffsetLong { get; private set; }

	public bool EndOfPacket { get; private set; }

	public SendPacketsElement(string filepath)
		: this(filepath, 0L, 0, endOfPacket: false)
	{
	}

	public SendPacketsElement(string filepath, int offset, int count)
		: this(filepath, (long)offset, count, endOfPacket: false)
	{
	}

	public SendPacketsElement(string filepath, int offset, int count, bool endOfPacket)
		: this(filepath, (long)offset, count, endOfPacket)
	{
	}

	public SendPacketsElement(string filepath, long offset, int count)
		: this(filepath, offset, count, endOfPacket: false)
	{
	}

	public SendPacketsElement(string filepath, long offset, int count, bool endOfPacket)
	{
		if (filepath == null)
		{
			throw new ArgumentNullException("filepath");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		Initialize(filepath, null, null, null, offset, count, endOfPacket);
	}

	public SendPacketsElement(FileStream fileStream)
		: this(fileStream, 0L, 0, endOfPacket: false)
	{
	}

	public SendPacketsElement(FileStream fileStream, long offset, int count)
		: this(fileStream, offset, count, endOfPacket: false)
	{
	}

	public SendPacketsElement(FileStream fileStream, long offset, int count, bool endOfPacket)
	{
		if (fileStream == null)
		{
			throw new ArgumentNullException("fileStream");
		}
		if (!fileStream.IsAsync)
		{
			throw new ArgumentException(System.SR.net_sockets_sendpackelement_FileStreamMustBeAsync, "fileStream");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		Initialize(null, fileStream, null, null, offset, count, endOfPacket);
	}

	public SendPacketsElement(byte[] buffer)
		: this(buffer, 0, (buffer != null) ? buffer.Length : 0, endOfPacket: false)
	{
	}

	public SendPacketsElement(byte[] buffer, int offset, int count)
		: this(buffer, offset, count, endOfPacket: false)
	{
	}

	public SendPacketsElement(byte[] buffer, int offset, int count, bool endOfPacket)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if ((uint)offset > (uint)buffer.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if ((uint)count > (uint)(buffer.Length - offset))
		{
			throw new ArgumentOutOfRangeException("count");
		}
		Initialize(null, null, buffer, buffer.AsMemory(offset, count), offset, count, endOfPacket);
	}

	public SendPacketsElement(ReadOnlyMemory<byte> buffer)
		: this(buffer, endOfPacket: false)
	{
	}

	public SendPacketsElement(ReadOnlyMemory<byte> buffer, bool endOfPacket)
	{
		Initialize(null, null, null, buffer, 0L, buffer.Length, endOfPacket);
	}

	private void Initialize(string filePath, FileStream fileStream, byte[] buffer, ReadOnlyMemory<byte>? memoryBuffer, long offset, int count, bool endOfPacket)
	{
		FilePath = filePath;
		FileStream = fileStream;
		Buffer = buffer;
		MemoryBuffer = memoryBuffer;
		OffsetLong = offset;
		Count = count;
		EndOfPacket = endOfPacket;
	}
}
