using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace System.IO.Compression;

public class ZipArchive : IDisposable
{
	private readonly Stream _archiveStream;

	private ZipArchiveEntry _archiveStreamOwner;

	private BinaryReader _archiveReader;

	private ZipArchiveMode _mode;

	private List<ZipArchiveEntry> _entries;

	private ReadOnlyCollection<ZipArchiveEntry> _entriesCollection;

	private Dictionary<string, ZipArchiveEntry> _entriesDictionary;

	private bool _readEntries;

	private bool _leaveOpen;

	private long _centralDirectoryStart;

	private bool _isDisposed;

	private uint _numberOfThisDisk;

	private long _expectedNumberOfEntries;

	private Stream _backingStream;

	private byte[] _archiveComment;

	private Encoding _entryNameEncoding;

	public ReadOnlyCollection<ZipArchiveEntry> Entries
	{
		get
		{
			if (_mode == ZipArchiveMode.Create)
			{
				throw new NotSupportedException(System.SR.EntriesInCreateMode);
			}
			ThrowIfDisposed();
			EnsureCentralDirectoryRead();
			return _entriesCollection;
		}
	}

	public ZipArchiveMode Mode => _mode;

	internal BinaryReader? ArchiveReader => _archiveReader;

	internal Stream ArchiveStream => _archiveStream;

	internal uint NumberOfThisDisk => _numberOfThisDisk;

	internal Encoding? EntryNameEncoding
	{
		get
		{
			return _entryNameEncoding;
		}
		private set
		{
			if (value != null && (value.Equals(Encoding.BigEndianUnicode) || value.Equals(Encoding.Unicode)))
			{
				throw new ArgumentException(System.SR.EntryNameEncodingNotSupported, "EntryNameEncoding");
			}
			_entryNameEncoding = value;
		}
	}

	public ZipArchive(Stream stream)
		: this(stream, ZipArchiveMode.Read, leaveOpen: false, null)
	{
	}

	public ZipArchive(Stream stream, ZipArchiveMode mode)
		: this(stream, mode, leaveOpen: false, null)
	{
	}

	public ZipArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen)
		: this(stream, mode, leaveOpen, null)
	{
	}

	public ZipArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen, Encoding? entryNameEncoding)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		EntryNameEncoding = entryNameEncoding;
		Stream stream2 = null;
		try
		{
			_backingStream = null;
			switch (mode)
			{
			case ZipArchiveMode.Create:
				if (!stream.CanWrite)
				{
					throw new ArgumentException(System.SR.CreateModeCapabilities);
				}
				break;
			case ZipArchiveMode.Read:
				if (!stream.CanRead)
				{
					throw new ArgumentException(System.SR.ReadModeCapabilities);
				}
				if (!stream.CanSeek)
				{
					_backingStream = stream;
					stream2 = (stream = new MemoryStream());
					_backingStream.CopyTo(stream);
					stream.Seek(0L, SeekOrigin.Begin);
				}
				break;
			case ZipArchiveMode.Update:
				if (!stream.CanRead || !stream.CanWrite || !stream.CanSeek)
				{
					throw new ArgumentException(System.SR.UpdateModeCapabilities);
				}
				break;
			default:
				throw new ArgumentOutOfRangeException("mode");
			}
			_mode = mode;
			if (mode == ZipArchiveMode.Create && !stream.CanSeek)
			{
				_archiveStream = new PositionPreservingWriteOnlyStreamWrapper(stream);
			}
			else
			{
				_archiveStream = stream;
			}
			_archiveStreamOwner = null;
			if (mode == ZipArchiveMode.Create)
			{
				_archiveReader = null;
			}
			else
			{
				_archiveReader = new BinaryReader(_archiveStream);
			}
			_entries = new List<ZipArchiveEntry>();
			_entriesCollection = new ReadOnlyCollection<ZipArchiveEntry>(_entries);
			_entriesDictionary = new Dictionary<string, ZipArchiveEntry>();
			_readEntries = false;
			_leaveOpen = leaveOpen;
			_centralDirectoryStart = 0L;
			_isDisposed = false;
			_numberOfThisDisk = 0u;
			_archiveComment = null;
			switch (mode)
			{
			case ZipArchiveMode.Create:
				_readEntries = true;
				return;
			case ZipArchiveMode.Read:
				ReadEndOfCentralDirectory();
				return;
			}
			if (_archiveStream.Length == 0L)
			{
				_readEntries = true;
				return;
			}
			ReadEndOfCentralDirectory();
			EnsureCentralDirectoryRead();
			foreach (ZipArchiveEntry entry in _entries)
			{
				entry.ThrowIfNotOpenable(needToUncompress: false, needToLoadIntoMemory: true);
			}
		}
		catch
		{
			stream2?.Dispose();
			throw;
		}
	}

	public ZipArchiveEntry CreateEntry(string entryName)
	{
		return DoCreateEntry(entryName, null);
	}

	public ZipArchiveEntry CreateEntry(string entryName, CompressionLevel compressionLevel)
	{
		return DoCreateEntry(entryName, compressionLevel);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing || _isDisposed)
		{
			return;
		}
		try
		{
			ZipArchiveMode mode = _mode;
			if (mode != 0)
			{
				_ = mode - 1;
				_ = 1;
				WriteFile();
			}
		}
		finally
		{
			CloseStreams();
			_isDisposed = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public ZipArchiveEntry? GetEntry(string entryName)
	{
		if (entryName == null)
		{
			throw new ArgumentNullException("entryName");
		}
		if (_mode == ZipArchiveMode.Create)
		{
			throw new NotSupportedException(System.SR.EntriesInCreateMode);
		}
		EnsureCentralDirectoryRead();
		_entriesDictionary.TryGetValue(entryName, out var value);
		return value;
	}

	private ZipArchiveEntry DoCreateEntry(string entryName, CompressionLevel? compressionLevel)
	{
		if (entryName == null)
		{
			throw new ArgumentNullException("entryName");
		}
		if (string.IsNullOrEmpty(entryName))
		{
			throw new ArgumentException(System.SR.CannotBeEmpty, "entryName");
		}
		if (_mode == ZipArchiveMode.Read)
		{
			throw new NotSupportedException(System.SR.CreateInReadMode);
		}
		ThrowIfDisposed();
		ZipArchiveEntry zipArchiveEntry = (compressionLevel.HasValue ? new ZipArchiveEntry(this, entryName, compressionLevel.Value) : new ZipArchiveEntry(this, entryName));
		AddEntry(zipArchiveEntry);
		return zipArchiveEntry;
	}

	internal void AcquireArchiveStream(ZipArchiveEntry entry)
	{
		if (_archiveStreamOwner != null)
		{
			if (_archiveStreamOwner.EverOpenedForWrite)
			{
				throw new IOException(System.SR.CreateModeCreateEntryWhileOpen);
			}
			_archiveStreamOwner.WriteAndFinishLocalEntry();
		}
		_archiveStreamOwner = entry;
	}

	private void AddEntry(ZipArchiveEntry entry)
	{
		_entries.Add(entry);
		string fullName = entry.FullName;
		if (!_entriesDictionary.ContainsKey(fullName))
		{
			_entriesDictionary.Add(fullName, entry);
		}
	}

	internal void ReleaseArchiveStream(ZipArchiveEntry entry)
	{
		_archiveStreamOwner = null;
	}

	internal void RemoveEntry(ZipArchiveEntry entry)
	{
		_entries.Remove(entry);
		_entriesDictionary.Remove(entry.FullName);
	}

	internal void ThrowIfDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(GetType().ToString());
		}
	}

	private void CloseStreams()
	{
		if (!_leaveOpen)
		{
			_archiveStream.Dispose();
			_backingStream?.Dispose();
			_archiveReader?.Dispose();
		}
		else if (_backingStream != null)
		{
			_archiveStream.Dispose();
		}
	}

	private void EnsureCentralDirectoryRead()
	{
		if (!_readEntries)
		{
			ReadCentralDirectory();
			_readEntries = true;
		}
	}

	private void ReadCentralDirectory()
	{
		try
		{
			_archiveStream.Seek(_centralDirectoryStart, SeekOrigin.Begin);
			long num = 0L;
			bool saveExtraFieldsAndComments = Mode == ZipArchiveMode.Update;
			ZipCentralDirectoryFileHeader header;
			while (ZipCentralDirectoryFileHeader.TryReadBlock(_archiveReader, saveExtraFieldsAndComments, out header))
			{
				AddEntry(new ZipArchiveEntry(this, header));
				num++;
			}
			if (num != _expectedNumberOfEntries)
			{
				throw new InvalidDataException(System.SR.NumEntriesWrong);
			}
		}
		catch (EndOfStreamException p)
		{
			throw new InvalidDataException(System.SR.Format(System.SR.CentralDirectoryInvalid, p));
		}
	}

	private void ReadEndOfCentralDirectory()
	{
		try
		{
			_archiveStream.Seek(-18L, SeekOrigin.End);
			if (!ZipHelper.SeekBackwardsToSignature(_archiveStream, 101010256u, 65539))
			{
				throw new InvalidDataException(System.SR.EOCDNotFound);
			}
			long position = _archiveStream.Position;
			ZipEndOfCentralDirectoryBlock eocdBlock;
			bool flag = ZipEndOfCentralDirectoryBlock.TryReadBlock(_archiveReader, out eocdBlock);
			if (eocdBlock.NumberOfThisDisk != eocdBlock.NumberOfTheDiskWithTheStartOfTheCentralDirectory)
			{
				throw new InvalidDataException(System.SR.SplitSpanned);
			}
			_numberOfThisDisk = eocdBlock.NumberOfThisDisk;
			_centralDirectoryStart = eocdBlock.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;
			if (eocdBlock.NumberOfEntriesInTheCentralDirectory != eocdBlock.NumberOfEntriesInTheCentralDirectoryOnThisDisk)
			{
				throw new InvalidDataException(System.SR.SplitSpanned);
			}
			_expectedNumberOfEntries = eocdBlock.NumberOfEntriesInTheCentralDirectory;
			if (_mode == ZipArchiveMode.Update)
			{
				_archiveComment = eocdBlock.ArchiveComment;
			}
			TryReadZip64EndOfCentralDirectory(eocdBlock, position);
			if (_centralDirectoryStart > _archiveStream.Length)
			{
				throw new InvalidDataException(System.SR.FieldTooBigOffsetToCD);
			}
		}
		catch (EndOfStreamException innerException)
		{
			throw new InvalidDataException(System.SR.CDCorrupt, innerException);
		}
		catch (IOException innerException2)
		{
			throw new InvalidDataException(System.SR.CDCorrupt, innerException2);
		}
	}

	private void TryReadZip64EndOfCentralDirectory(ZipEndOfCentralDirectoryBlock eocd, long eocdStart)
	{
		if (eocd.NumberOfThisDisk != ushort.MaxValue && eocd.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber != uint.MaxValue && eocd.NumberOfEntriesInTheCentralDirectory != ushort.MaxValue)
		{
			return;
		}
		_archiveStream.Seek(eocdStart - 16, SeekOrigin.Begin);
		if (ZipHelper.SeekBackwardsToSignature(_archiveStream, 117853008u, 4))
		{
			Zip64EndOfCentralDirectoryLocator zip64EOCDLocator;
			bool flag = Zip64EndOfCentralDirectoryLocator.TryReadBlock(_archiveReader, out zip64EOCDLocator);
			if (zip64EOCDLocator.OffsetOfZip64EOCD > long.MaxValue)
			{
				throw new InvalidDataException(System.SR.FieldTooBigOffsetToZip64EOCD);
			}
			long offsetOfZip64EOCD = (long)zip64EOCDLocator.OffsetOfZip64EOCD;
			_archiveStream.Seek(offsetOfZip64EOCD, SeekOrigin.Begin);
			if (!Zip64EndOfCentralDirectoryRecord.TryReadBlock(_archiveReader, out var zip64EOCDRecord))
			{
				throw new InvalidDataException(System.SR.Zip64EOCDNotWhereExpected);
			}
			_numberOfThisDisk = zip64EOCDRecord.NumberOfThisDisk;
			if (zip64EOCDRecord.NumberOfEntriesTotal > long.MaxValue)
			{
				throw new InvalidDataException(System.SR.FieldTooBigNumEntries);
			}
			if (zip64EOCDRecord.OffsetOfCentralDirectory > long.MaxValue)
			{
				throw new InvalidDataException(System.SR.FieldTooBigOffsetToCD);
			}
			if (zip64EOCDRecord.NumberOfEntriesTotal != zip64EOCDRecord.NumberOfEntriesOnThisDisk)
			{
				throw new InvalidDataException(System.SR.SplitSpanned);
			}
			_expectedNumberOfEntries = (long)zip64EOCDRecord.NumberOfEntriesTotal;
			_centralDirectoryStart = (long)zip64EOCDRecord.OffsetOfCentralDirectory;
		}
	}

	private void WriteFile()
	{
		if (_mode == ZipArchiveMode.Update)
		{
			List<ZipArchiveEntry> list = new List<ZipArchiveEntry>();
			foreach (ZipArchiveEntry entry in _entries)
			{
				if (!entry.LoadLocalHeaderExtraFieldAndCompressedBytesIfNeeded())
				{
					list.Add(entry);
				}
			}
			foreach (ZipArchiveEntry item in list)
			{
				item.Delete();
			}
			_archiveStream.Seek(0L, SeekOrigin.Begin);
			_archiveStream.SetLength(0L);
		}
		foreach (ZipArchiveEntry entry2 in _entries)
		{
			entry2.WriteAndFinishLocalEntry();
		}
		long position = _archiveStream.Position;
		foreach (ZipArchiveEntry entry3 in _entries)
		{
			entry3.WriteCentralDirectoryFileHeader();
		}
		long sizeOfCentralDirectory = _archiveStream.Position - position;
		WriteArchiveEpilogue(position, sizeOfCentralDirectory);
	}

	private void WriteArchiveEpilogue(long startOfCentralDirectory, long sizeOfCentralDirectory)
	{
		if (startOfCentralDirectory >= uint.MaxValue || sizeOfCentralDirectory >= uint.MaxValue || _entries.Count >= 65535)
		{
			long position = _archiveStream.Position;
			Zip64EndOfCentralDirectoryRecord.WriteBlock(_archiveStream, _entries.Count, startOfCentralDirectory, sizeOfCentralDirectory);
			Zip64EndOfCentralDirectoryLocator.WriteBlock(_archiveStream, position);
		}
		ZipEndOfCentralDirectoryBlock.WriteBlock(_archiveStream, _entries.Count, startOfCentralDirectory, sizeOfCentralDirectory, _archiveComment);
	}
}
