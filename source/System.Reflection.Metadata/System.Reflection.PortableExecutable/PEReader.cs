using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Reflection.Internal;
using System.Reflection.Metadata;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace System.Reflection.PortableExecutable;

public sealed class PEReader : IDisposable
{
	private MemoryBlockProvider _peImage;

	private PEHeaders _lazyPEHeaders;

	private AbstractMemoryBlock _lazyMetadataBlock;

	private AbstractMemoryBlock _lazyImageBlock;

	private AbstractMemoryBlock[] _lazyPESectionBlocks;

	public bool IsLoadedImage { get; }

	public PEHeaders PEHeaders
	{
		get
		{
			if (_lazyPEHeaders == null)
			{
				InitializePEHeaders();
			}
			return _lazyPEHeaders;
		}
	}

	public bool IsEntireImageAvailable
	{
		get
		{
			if (_lazyImageBlock == null)
			{
				return _peImage != null;
			}
			return true;
		}
	}

	public bool HasMetadata => PEHeaders.MetadataSize > 0;

	public unsafe PEReader(byte* peImage, int size)
		: this(peImage, size, isLoadedImage: false)
	{
	}

	public unsafe PEReader(byte* peImage, int size, bool isLoadedImage)
	{
		if (peImage == null)
		{
			throw new ArgumentNullException("peImage");
		}
		if (size < 0)
		{
			throw new ArgumentOutOfRangeException("size");
		}
		_peImage = new ExternalMemoryBlockProvider(peImage, size);
		IsLoadedImage = isLoadedImage;
	}

	public PEReader(Stream peStream)
		: this(peStream, PEStreamOptions.Default)
	{
	}

	public PEReader(Stream peStream, PEStreamOptions options)
		: this(peStream, options, 0)
	{
	}

	public unsafe PEReader(Stream peStream, PEStreamOptions options, int size)
	{
		if (peStream == null)
		{
			throw new ArgumentNullException("peStream");
		}
		if (!peStream.CanRead || !peStream.CanSeek)
		{
			throw new ArgumentException(System.SR.StreamMustSupportReadAndSeek, "peStream");
		}
		if (!options.IsValid())
		{
			throw new ArgumentOutOfRangeException("options");
		}
		IsLoadedImage = (options & PEStreamOptions.IsLoadedImage) != 0;
		long position = peStream.Position;
		int andValidateSize = StreamExtensions.GetAndValidateSize(peStream, size, "peStream");
		bool flag = true;
		try
		{
			bool isFileStream = FileStreamReadLightUp.IsFileStream(peStream);
			if ((options & (PEStreamOptions.PrefetchMetadata | PEStreamOptions.PrefetchEntireImage)) == 0)
			{
				_peImage = new StreamMemoryBlockProvider(peStream, position, andValidateSize, isFileStream, (options & PEStreamOptions.LeaveOpen) != 0);
				flag = false;
			}
			else if ((options & PEStreamOptions.PrefetchEntireImage) != 0)
			{
				NativeHeapMemoryBlock nativeHeapMemoryBlock = (NativeHeapMemoryBlock)(_lazyImageBlock = StreamMemoryBlockProvider.ReadMemoryBlockNoLock(peStream, isFileStream, position, andValidateSize));
				_peImage = new ExternalMemoryBlockProvider(nativeHeapMemoryBlock.Pointer, nativeHeapMemoryBlock.Size);
				if ((options & PEStreamOptions.PrefetchMetadata) != 0)
				{
					InitializePEHeaders();
				}
			}
			else
			{
				_lazyPEHeaders = new PEHeaders(peStream);
				_lazyMetadataBlock = StreamMemoryBlockProvider.ReadMemoryBlockNoLock(peStream, isFileStream, _lazyPEHeaders.MetadataStartOffset, _lazyPEHeaders.MetadataSize);
			}
		}
		finally
		{
			if (flag && (options & PEStreamOptions.LeaveOpen) == 0)
			{
				peStream.Dispose();
			}
		}
	}

	public PEReader(ImmutableArray<byte> peImage)
	{
		if (peImage.IsDefault)
		{
			throw new ArgumentNullException("peImage");
		}
		_peImage = new ByteArrayMemoryProvider(peImage);
	}

	public void Dispose()
	{
		_lazyPEHeaders = null;
		_peImage?.Dispose();
		_peImage = null;
		_lazyImageBlock?.Dispose();
		_lazyImageBlock = null;
		_lazyMetadataBlock?.Dispose();
		_lazyMetadataBlock = null;
		AbstractMemoryBlock[] lazyPESectionBlocks = _lazyPESectionBlocks;
		if (lazyPESectionBlocks != null)
		{
			AbstractMemoryBlock[] array = lazyPESectionBlocks;
			for (int i = 0; i < array.Length; i++)
			{
				array[i]?.Dispose();
			}
			_lazyPESectionBlocks = null;
		}
	}

	private MemoryBlockProvider GetPEImage()
	{
		MemoryBlockProvider peImage = _peImage;
		if (peImage == null)
		{
			if (_lazyPEHeaders == null)
			{
				Throw.PEReaderDisposed();
			}
			Throw.InvalidOperation_PEImageNotAvailable();
		}
		return peImage;
	}

	private void InitializePEHeaders()
	{
		StreamConstraints constraints;
		Stream stream = GetPEImage().GetStream(out constraints);
		PEHeaders value;
		if (constraints.GuardOpt != null)
		{
			lock (constraints.GuardOpt)
			{
				value = ReadPEHeadersNoLock(stream, constraints.ImageStart, constraints.ImageSize, IsLoadedImage);
			}
		}
		else
		{
			value = ReadPEHeadersNoLock(stream, constraints.ImageStart, constraints.ImageSize, IsLoadedImage);
		}
		Interlocked.CompareExchange(ref _lazyPEHeaders, value, null);
	}

	private static PEHeaders ReadPEHeadersNoLock(Stream stream, long imageStartPosition, int imageSize, bool isLoadedImage)
	{
		stream.Seek(imageStartPosition, SeekOrigin.Begin);
		return new PEHeaders(stream, imageSize, isLoadedImage);
	}

	private AbstractMemoryBlock GetEntireImageBlock()
	{
		if (_lazyImageBlock == null)
		{
			AbstractMemoryBlock memoryBlock = GetPEImage().GetMemoryBlock();
			if (Interlocked.CompareExchange(ref _lazyImageBlock, memoryBlock, null) != null)
			{
				memoryBlock.Dispose();
			}
		}
		return _lazyImageBlock;
	}

	private AbstractMemoryBlock GetMetadataBlock()
	{
		if (!HasMetadata)
		{
			throw new InvalidOperationException(System.SR.PEImageDoesNotHaveMetadata);
		}
		if (_lazyMetadataBlock == null)
		{
			AbstractMemoryBlock memoryBlock = GetPEImage().GetMemoryBlock(PEHeaders.MetadataStartOffset, PEHeaders.MetadataSize);
			if (Interlocked.CompareExchange(ref _lazyMetadataBlock, memoryBlock, null) != null)
			{
				memoryBlock.Dispose();
			}
		}
		return _lazyMetadataBlock;
	}

	private AbstractMemoryBlock GetPESectionBlock(int index)
	{
		MemoryBlockProvider pEImage = GetPEImage();
		if (_lazyPESectionBlocks == null)
		{
			Interlocked.CompareExchange(ref _lazyPESectionBlocks, new AbstractMemoryBlock[PEHeaders.SectionHeaders.Length], null);
		}
		AbstractMemoryBlock memoryBlock;
		if (IsLoadedImage)
		{
			memoryBlock = pEImage.GetMemoryBlock(PEHeaders.SectionHeaders[index].VirtualAddress, PEHeaders.SectionHeaders[index].VirtualSize);
		}
		else
		{
			int size = Math.Min(PEHeaders.SectionHeaders[index].VirtualSize, PEHeaders.SectionHeaders[index].SizeOfRawData);
			memoryBlock = pEImage.GetMemoryBlock(PEHeaders.SectionHeaders[index].PointerToRawData, size);
		}
		if (Interlocked.CompareExchange(ref _lazyPESectionBlocks[index], memoryBlock, null) != null)
		{
			memoryBlock.Dispose();
		}
		return _lazyPESectionBlocks[index];
	}

	public PEMemoryBlock GetEntireImage()
	{
		return new PEMemoryBlock(GetEntireImageBlock());
	}

	public PEMemoryBlock GetMetadata()
	{
		return new PEMemoryBlock(GetMetadataBlock());
	}

	public PEMemoryBlock GetSectionData(int relativeVirtualAddress)
	{
		if (relativeVirtualAddress < 0)
		{
			Throw.ArgumentOutOfRange("relativeVirtualAddress");
		}
		int containingSectionIndex = PEHeaders.GetContainingSectionIndex(relativeVirtualAddress);
		if (containingSectionIndex < 0)
		{
			return default(PEMemoryBlock);
		}
		AbstractMemoryBlock pESectionBlock = GetPESectionBlock(containingSectionIndex);
		int num = relativeVirtualAddress - PEHeaders.SectionHeaders[containingSectionIndex].VirtualAddress;
		if (num > pESectionBlock.Size)
		{
			return default(PEMemoryBlock);
		}
		return new PEMemoryBlock(pESectionBlock, num);
	}

	public PEMemoryBlock GetSectionData(string sectionName)
	{
		if (sectionName == null)
		{
			Throw.ArgumentNull("sectionName");
		}
		int num = PEHeaders.IndexOfSection(sectionName);
		if (num < 0)
		{
			return default(PEMemoryBlock);
		}
		return new PEMemoryBlock(GetPESectionBlock(num));
	}

	public ImmutableArray<DebugDirectoryEntry> ReadDebugDirectory()
	{
		DirectoryEntry debugTableDirectory = PEHeaders.PEHeader.DebugTableDirectory;
		if (debugTableDirectory.Size == 0)
		{
			return ImmutableArray<DebugDirectoryEntry>.Empty;
		}
		if (!PEHeaders.TryGetDirectoryOffset(debugTableDirectory, out var offset))
		{
			throw new BadImageFormatException(System.SR.InvalidDirectoryRVA);
		}
		if (debugTableDirectory.Size % 28 != 0)
		{
			throw new BadImageFormatException(System.SR.InvalidDirectorySize);
		}
		using AbstractMemoryBlock abstractMemoryBlock = GetPEImage().GetMemoryBlock(offset, debugTableDirectory.Size);
		return ReadDebugDirectoryEntries(abstractMemoryBlock.GetReader());
	}

	internal static ImmutableArray<DebugDirectoryEntry> ReadDebugDirectoryEntries(BlobReader reader)
	{
		int num = reader.Length / 28;
		ImmutableArray<DebugDirectoryEntry>.Builder builder = ImmutableArray.CreateBuilder<DebugDirectoryEntry>(num);
		for (int i = 0; i < num; i++)
		{
			if (reader.ReadInt32() != 0)
			{
				throw new BadImageFormatException(System.SR.InvalidDebugDirectoryEntryCharacteristics);
			}
			uint stamp = reader.ReadUInt32();
			ushort majorVersion = reader.ReadUInt16();
			ushort minorVersion = reader.ReadUInt16();
			DebugDirectoryEntryType type = (DebugDirectoryEntryType)reader.ReadInt32();
			int dataSize = reader.ReadInt32();
			int dataRelativeVirtualAddress = reader.ReadInt32();
			int dataPointer = reader.ReadInt32();
			builder.Add(new DebugDirectoryEntry(stamp, majorVersion, minorVersion, type, dataSize, dataRelativeVirtualAddress, dataPointer));
		}
		return builder.MoveToImmutable();
	}

	private AbstractMemoryBlock GetDebugDirectoryEntryDataBlock(DebugDirectoryEntry entry)
	{
		int start = (IsLoadedImage ? entry.DataRelativeVirtualAddress : entry.DataPointer);
		return GetPEImage().GetMemoryBlock(start, entry.DataSize);
	}

	public CodeViewDebugDirectoryData ReadCodeViewDebugDirectoryData(DebugDirectoryEntry entry)
	{
		if (entry.Type != DebugDirectoryEntryType.CodeView)
		{
			Throw.InvalidArgument(System.SR.Format(System.SR.UnexpectedDebugDirectoryType, "CodeView"), "entry");
		}
		using AbstractMemoryBlock block = GetDebugDirectoryEntryDataBlock(entry);
		return DecodeCodeViewDebugDirectoryData(block);
	}

	internal static CodeViewDebugDirectoryData DecodeCodeViewDebugDirectoryData(AbstractMemoryBlock block)
	{
		BlobReader reader = block.GetReader();
		if (reader.ReadByte() != 82 || reader.ReadByte() != 83 || reader.ReadByte() != 68 || reader.ReadByte() != 83)
		{
			throw new BadImageFormatException(System.SR.UnexpectedCodeViewDataSignature);
		}
		Guid guid = reader.ReadGuid();
		int age = reader.ReadInt32();
		string path = reader.ReadUtf8NullTerminated();
		return new CodeViewDebugDirectoryData(guid, age, path);
	}

	public PdbChecksumDebugDirectoryData ReadPdbChecksumDebugDirectoryData(DebugDirectoryEntry entry)
	{
		if (entry.Type != DebugDirectoryEntryType.PdbChecksum)
		{
			Throw.InvalidArgument(System.SR.Format(System.SR.UnexpectedDebugDirectoryType, "PdbChecksum"), "entry");
		}
		using AbstractMemoryBlock block = GetDebugDirectoryEntryDataBlock(entry);
		return DecodePdbChecksumDebugDirectoryData(block);
	}

	internal static PdbChecksumDebugDirectoryData DecodePdbChecksumDebugDirectoryData(AbstractMemoryBlock block)
	{
		BlobReader reader = block.GetReader();
		string text = reader.ReadUtf8NullTerminated();
		byte[] array = reader.ReadBytes(reader.RemainingBytes);
		if (text.Length == 0 || array.Length == 0)
		{
			throw new BadImageFormatException(System.SR.InvalidPdbChecksumDataFormat);
		}
		return new PdbChecksumDebugDirectoryData(text, ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref array));
	}

	public bool TryOpenAssociatedPortablePdb(string peImagePath, Func<string, Stream?> pdbFileStreamProvider, out MetadataReaderProvider? pdbReaderProvider, out string? pdbPath)
	{
		if (peImagePath == null)
		{
			Throw.ArgumentNull("peImagePath");
		}
		if (pdbFileStreamProvider == null)
		{
			Throw.ArgumentNull("pdbFileStreamProvider");
		}
		pdbReaderProvider = null;
		pdbPath = null;
		string directoryName;
		try
		{
			directoryName = Path.GetDirectoryName(peImagePath);
		}
		catch (Exception ex)
		{
			throw new ArgumentException(ex.Message, "peImagePath");
		}
		Exception errorToReport = null;
		ImmutableArray<DebugDirectoryEntry> collection = ReadDebugDirectory();
		DebugDirectoryEntry codeViewEntry = collection.FirstOrDefault((DebugDirectoryEntry e) => e.IsPortableCodeView);
		if (codeViewEntry.DataSize != 0 && TryOpenCodeViewPortablePdb(codeViewEntry, directoryName, pdbFileStreamProvider, out pdbReaderProvider, out pdbPath, ref errorToReport))
		{
			return true;
		}
		DebugDirectoryEntry embeddedPdbEntry = collection.FirstOrDefault((DebugDirectoryEntry e) => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
		if (embeddedPdbEntry.DataSize != 0)
		{
			bool openedEmbeddedPdb = false;
			pdbReaderProvider = null;
			TryOpenEmbeddedPortablePdb(embeddedPdbEntry, ref openedEmbeddedPdb, ref pdbReaderProvider, ref errorToReport);
			if (openedEmbeddedPdb)
			{
				return true;
			}
		}
		if (errorToReport != null)
		{
			ExceptionDispatchInfo.Capture(errorToReport).Throw();
		}
		return false;
	}

	private bool TryOpenCodeViewPortablePdb(DebugDirectoryEntry codeViewEntry, string peImageDirectory, Func<string, Stream> pdbFileStreamProvider, out MetadataReaderProvider provider, out string pdbPath, ref Exception errorToReport)
	{
		pdbPath = null;
		provider = null;
		CodeViewDebugDirectoryData codeViewDebugDirectoryData;
		try
		{
			codeViewDebugDirectoryData = ReadCodeViewDebugDirectoryData(codeViewEntry);
		}
		catch (Exception ex) when (ex is BadImageFormatException || ex is IOException)
		{
			_ = errorToReport;
			if (ex == null)
			{
			}
			errorToReport = ex;
			return false;
		}
		BlobContentId id = new BlobContentId(codeViewDebugDirectoryData.Guid, codeViewEntry.Stamp);
		string text = PathUtilities.CombinePathWithRelativePath(peImageDirectory, PathUtilities.GetFileName(codeViewDebugDirectoryData.Path));
		if (TryOpenPortablePdbFile(text, id, pdbFileStreamProvider, out provider, ref errorToReport))
		{
			pdbPath = text;
			return true;
		}
		return false;
	}

	private bool TryOpenPortablePdbFile(string path, BlobContentId id, Func<string, Stream> pdbFileStreamProvider, out MetadataReaderProvider provider, ref Exception errorToReport)
	{
		provider = null;
		MetadataReaderProvider metadataReaderProvider = null;
		try
		{
			Stream stream;
			try
			{
				stream = pdbFileStreamProvider(path);
			}
			catch (FileNotFoundException)
			{
				stream = null;
			}
			if (stream == null)
			{
				return false;
			}
			if (!stream.CanRead || !stream.CanSeek)
			{
				throw new InvalidOperationException(System.SR.StreamMustSupportReadAndSeek);
			}
			metadataReaderProvider = MetadataReaderProvider.FromPortablePdbStream(stream);
			if (new BlobContentId(metadataReaderProvider.GetMetadataReader().DebugMetadataHeader.Id) != id)
			{
				return false;
			}
			provider = metadataReaderProvider;
			return true;
		}
		catch (Exception ex2) when (ex2 is BadImageFormatException || ex2 is IOException)
		{
			_ = errorToReport;
			if (ex2 == null)
			{
			}
			errorToReport = ex2;
			return false;
		}
		finally
		{
			if (provider == null)
			{
				metadataReaderProvider?.Dispose();
			}
		}
	}

	private void TryOpenEmbeddedPortablePdb(DebugDirectoryEntry embeddedPdbEntry, ref bool openedEmbeddedPdb, ref MetadataReaderProvider provider, ref Exception errorToReport)
	{
		provider = null;
		MetadataReaderProvider metadataReaderProvider = null;
		try
		{
			metadataReaderProvider = ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbEntry);
			metadataReaderProvider.GetMetadataReader();
			provider = metadataReaderProvider;
			openedEmbeddedPdb = true;
		}
		catch (Exception ex) when (ex is BadImageFormatException || ex is IOException)
		{
			_ = errorToReport;
			if (ex == null)
			{
			}
			errorToReport = ex;
			openedEmbeddedPdb = false;
		}
		finally
		{
			if (provider == null)
			{
				metadataReaderProvider?.Dispose();
			}
		}
	}

	public MetadataReaderProvider ReadEmbeddedPortablePdbDebugDirectoryData(DebugDirectoryEntry entry)
	{
		if (entry.Type != DebugDirectoryEntryType.EmbeddedPortablePdb)
		{
			Throw.InvalidArgument(System.SR.Format(System.SR.UnexpectedDebugDirectoryType, "EmbeddedPortablePdb"), "entry");
		}
		ValidateEmbeddedPortablePdbVersion(entry);
		using AbstractMemoryBlock block = GetDebugDirectoryEntryDataBlock(entry);
		return new MetadataReaderProvider(DecodeEmbeddedPortablePdbDebugDirectoryData(block));
	}

	internal static void ValidateEmbeddedPortablePdbVersion(DebugDirectoryEntry entry)
	{
		ushort majorVersion = entry.MajorVersion;
		if (majorVersion < 256)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.UnsupportedFormatVersion, PortablePdbVersions.Format(majorVersion)));
		}
		ushort minorVersion = entry.MinorVersion;
		if (minorVersion != 256)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.UnsupportedFormatVersion, PortablePdbVersions.Format(minorVersion)));
		}
	}

	internal unsafe static NativeHeapMemoryBlock DecodeEmbeddedPortablePdbDebugDirectoryData(AbstractMemoryBlock block)
	{
		BlobReader reader = block.GetReader();
		if (reader.ReadUInt32() != 1111773261)
		{
			throw new BadImageFormatException(System.SR.UnexpectedEmbeddedPortablePdbDataSignature);
		}
		int num = reader.ReadInt32();
		NativeHeapMemoryBlock nativeHeapMemoryBlock;
		try
		{
			nativeHeapMemoryBlock = new NativeHeapMemoryBlock(num);
		}
		catch
		{
			throw new BadImageFormatException(System.SR.DataTooBig);
		}
		bool flag = false;
		try
		{
			ReadOnlyUnmanagedMemoryStream stream = new ReadOnlyUnmanagedMemoryStream(reader.CurrentPointer, reader.RemainingBytes);
			DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: true);
			if (num > 0)
			{
				int num2;
				try
				{
					num2 = deflateStream.TryReadAll(new Span<byte>(nativeHeapMemoryBlock.Pointer, nativeHeapMemoryBlock.Size));
				}
				catch (Exception ex)
				{
					throw new BadImageFormatException(ex.Message, ex.InnerException);
				}
				if (num2 != nativeHeapMemoryBlock.Size)
				{
					throw new BadImageFormatException(System.SR.SizeMismatch);
				}
			}
			if (deflateStream.ReadByte() != -1)
			{
				throw new BadImageFormatException(System.SR.SizeMismatch);
			}
			flag = true;
		}
		finally
		{
			if (!flag)
			{
				nativeHeapMemoryBlock.Dispose();
			}
		}
		return nativeHeapMemoryBlock;
	}
}
