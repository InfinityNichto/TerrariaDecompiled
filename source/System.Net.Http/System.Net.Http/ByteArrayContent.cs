using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public class ByteArrayContent : HttpContent
{
	private readonly byte[] _content;

	private readonly int _offset;

	private readonly int _count;

	internal override bool AllowDuplex => false;

	public ByteArrayContent(byte[] content)
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		_content = content;
		_count = content.Length;
	}

	public ByteArrayContent(byte[] content, int offset, int count)
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		if (offset < 0 || offset > content.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0 || count > content.Length - offset)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		_content = content;
		_offset = offset;
		_count = count;
	}

	protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		stream.Write(_content, _offset, _count);
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
	{
		return SerializeToStreamAsyncCore(stream, default(CancellationToken));
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		if (!(GetType() == typeof(ByteArrayContent)))
		{
			return base.SerializeToStreamAsync(stream, context, cancellationToken);
		}
		return SerializeToStreamAsyncCore(stream, cancellationToken);
	}

	private protected Task SerializeToStreamAsyncCore(Stream stream, CancellationToken cancellationToken)
	{
		return stream.WriteAsync(_content, _offset, _count, cancellationToken);
	}

	protected internal override bool TryComputeLength(out long length)
	{
		length = _count;
		return true;
	}

	protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
	{
		return CreateMemoryStreamForByteArray();
	}

	protected override Task<Stream> CreateContentReadStreamAsync()
	{
		return Task.FromResult((Stream)CreateMemoryStreamForByteArray());
	}

	internal override Stream TryCreateContentReadStream()
	{
		if (!(GetType() == typeof(ByteArrayContent)))
		{
			return null;
		}
		return CreateMemoryStreamForByteArray();
	}

	internal MemoryStream CreateMemoryStreamForByteArray()
	{
		return new MemoryStream(_content, _offset, _count, writable: false);
	}
}
