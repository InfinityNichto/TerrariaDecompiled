using System.Collections.Immutable;
using System.IO;
using System.Reflection.Internal;
using System.Threading;

namespace System.Reflection.Metadata;

public sealed class MetadataReaderProvider : IDisposable
{
	private MemoryBlockProvider _blockProviderOpt;

	private AbstractMemoryBlock _lazyMetadataBlock;

	private MetadataReader _lazyMetadataReader;

	private readonly object _metadataReaderGuard = new object();

	internal MetadataReaderProvider(AbstractMemoryBlock metadataBlock)
	{
		_lazyMetadataBlock = metadataBlock;
	}

	private MetadataReaderProvider(MemoryBlockProvider blockProvider)
	{
		_blockProviderOpt = blockProvider;
	}

	public unsafe static MetadataReaderProvider FromPortablePdbImage(byte* start, int size)
	{
		return FromMetadataImage(start, size);
	}

	public unsafe static MetadataReaderProvider FromMetadataImage(byte* start, int size)
	{
		if (start == null)
		{
			throw new ArgumentNullException("start");
		}
		if (size < 0)
		{
			throw new ArgumentOutOfRangeException("size");
		}
		return new MetadataReaderProvider(new ExternalMemoryBlockProvider(start, size));
	}

	public static MetadataReaderProvider FromPortablePdbImage(ImmutableArray<byte> image)
	{
		return FromMetadataImage(image);
	}

	public static MetadataReaderProvider FromMetadataImage(ImmutableArray<byte> image)
	{
		if (image.IsDefault)
		{
			throw new ArgumentNullException("image");
		}
		return new MetadataReaderProvider(new ByteArrayMemoryProvider(image));
	}

	public static MetadataReaderProvider FromPortablePdbStream(Stream stream, MetadataStreamOptions options = MetadataStreamOptions.Default, int size = 0)
	{
		return FromMetadataStream(stream, options, size);
	}

	public static MetadataReaderProvider FromMetadataStream(Stream stream, MetadataStreamOptions options = MetadataStreamOptions.Default, int size = 0)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (!stream.CanRead || !stream.CanSeek)
		{
			throw new ArgumentException(System.SR.StreamMustSupportReadAndSeek, "stream");
		}
		if (!options.IsValid())
		{
			throw new ArgumentOutOfRangeException("options");
		}
		long position = stream.Position;
		int andValidateSize = StreamExtensions.GetAndValidateSize(stream, size, "stream");
		bool flag = true;
		MetadataReaderProvider result;
		try
		{
			bool isFileStream = FileStreamReadLightUp.IsFileStream(stream);
			if ((options & MetadataStreamOptions.PrefetchMetadata) == 0)
			{
				result = new MetadataReaderProvider(new StreamMemoryBlockProvider(stream, position, andValidateSize, isFileStream, (options & MetadataStreamOptions.LeaveOpen) != 0));
				flag = false;
			}
			else
			{
				result = new MetadataReaderProvider(StreamMemoryBlockProvider.ReadMemoryBlockNoLock(stream, isFileStream, position, andValidateSize));
			}
		}
		finally
		{
			if (flag && (options & MetadataStreamOptions.LeaveOpen) == 0)
			{
				stream.Dispose();
			}
		}
		return result;
	}

	public void Dispose()
	{
		_blockProviderOpt?.Dispose();
		_blockProviderOpt = null;
		_lazyMetadataBlock?.Dispose();
		_lazyMetadataBlock = null;
		_lazyMetadataReader = null;
	}

	public unsafe MetadataReader GetMetadataReader(MetadataReaderOptions options = MetadataReaderOptions.Default, MetadataStringDecoder? utf8Decoder = null)
	{
		MetadataReader lazyMetadataReader = _lazyMetadataReader;
		if (CanReuseReader(lazyMetadataReader, options, utf8Decoder))
		{
			return lazyMetadataReader;
		}
		lock (_metadataReaderGuard)
		{
			lazyMetadataReader = _lazyMetadataReader;
			if (CanReuseReader(lazyMetadataReader, options, utf8Decoder))
			{
				return lazyMetadataReader;
			}
			AbstractMemoryBlock metadataBlock = GetMetadataBlock();
			return _lazyMetadataReader = new MetadataReader(metadataBlock.Pointer, metadataBlock.Size, options, utf8Decoder, this);
		}
	}

	private static bool CanReuseReader(MetadataReader reader, MetadataReaderOptions options, MetadataStringDecoder utf8DecoderOpt)
	{
		if (reader != null && reader.Options == options)
		{
			return reader.UTF8Decoder == (utf8DecoderOpt ?? MetadataStringDecoder.DefaultUTF8);
		}
		return false;
	}

	internal AbstractMemoryBlock GetMetadataBlock()
	{
		if (_lazyMetadataBlock == null)
		{
			if (_blockProviderOpt == null)
			{
				throw new ObjectDisposedException("MetadataReaderProvider");
			}
			AbstractMemoryBlock memoryBlock = _blockProviderOpt.GetMemoryBlock(0, _blockProviderOpt.Size);
			if (Interlocked.CompareExchange(ref _lazyMetadataBlock, memoryBlock, null) != null)
			{
				memoryBlock.Dispose();
			}
		}
		return _lazyMetadataBlock;
	}
}
