using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata;

namespace System.Reflection.PortableExecutable;

public sealed class DebugDirectoryBuilder
{
	private struct Entry
	{
		public uint Stamp;

		public uint Version;

		public DebugDirectoryEntryType Type;

		public int DataSize;
	}

	private readonly List<Entry> _entries;

	private readonly BlobBuilder _dataBuilder;

	internal int TableSize => 28 * _entries.Count;

	internal int Size => (TableSize + _dataBuilder?.Count).GetValueOrDefault();

	public DebugDirectoryBuilder()
	{
		_entries = new List<Entry>(3);
		_dataBuilder = new BlobBuilder();
	}

	internal void AddEntry(DebugDirectoryEntryType type, uint version, uint stamp, int dataSize)
	{
		_entries.Add(new Entry
		{
			Stamp = stamp,
			Version = version,
			Type = type,
			DataSize = dataSize
		});
	}

	public void AddEntry(DebugDirectoryEntryType type, uint version, uint stamp)
	{
		AddEntry(type, version, stamp, 0);
	}

	public void AddEntry<TData>(DebugDirectoryEntryType type, uint version, uint stamp, TData data, Action<BlobBuilder, TData> dataSerializer)
	{
		if (dataSerializer == null)
		{
			Throw.ArgumentNull("dataSerializer");
		}
		int count = _dataBuilder.Count;
		dataSerializer(_dataBuilder, data);
		int dataSize = _dataBuilder.Count - count;
		AddEntry(type, version, stamp, dataSize);
	}

	public void AddCodeViewEntry(string pdbPath, BlobContentId pdbContentId, ushort portablePdbVersion)
	{
		AddCodeViewEntry(pdbPath, pdbContentId, portablePdbVersion, 1);
	}

	public void AddCodeViewEntry(string pdbPath, BlobContentId pdbContentId, ushort portablePdbVersion, int age)
	{
		if (pdbPath == null)
		{
			Throw.ArgumentNull("pdbPath");
		}
		if (age < 1)
		{
			Throw.ArgumentOutOfRange("age");
		}
		if (pdbPath.Length == 0 || pdbPath.IndexOf('\0') == 0)
		{
			Throw.InvalidArgument(System.SR.ExpectedNonEmptyString, "pdbPath");
		}
		if (portablePdbVersion > 0 && portablePdbVersion < 256)
		{
			Throw.ArgumentOutOfRange("portablePdbVersion");
		}
		int dataSize = WriteCodeViewData(_dataBuilder, pdbPath, pdbContentId.Guid, age);
		AddEntry(DebugDirectoryEntryType.CodeView, (portablePdbVersion != 0) ? PortablePdbVersions.DebugDirectoryEntryVersion(portablePdbVersion) : 0u, pdbContentId.Stamp, dataSize);
	}

	public void AddReproducibleEntry()
	{
		AddEntry(DebugDirectoryEntryType.Reproducible, 0u, 0u);
	}

	private static int WriteCodeViewData(BlobBuilder builder, string pdbPath, Guid pdbGuid, int age)
	{
		int count = builder.Count;
		builder.WriteByte(82);
		builder.WriteByte(83);
		builder.WriteByte(68);
		builder.WriteByte(83);
		builder.WriteGuid(pdbGuid);
		builder.WriteInt32(age);
		builder.WriteUTF8(pdbPath);
		builder.WriteByte(0);
		return builder.Count - count;
	}

	public void AddPdbChecksumEntry(string algorithmName, ImmutableArray<byte> checksum)
	{
		if (algorithmName == null)
		{
			Throw.ArgumentNull("algorithmName");
		}
		if (algorithmName.Length == 0)
		{
			Throw.ArgumentEmptyString("algorithmName");
		}
		if (checksum.IsDefault)
		{
			Throw.ArgumentNull("checksum");
		}
		if (checksum.Length == 0)
		{
			Throw.ArgumentEmptyArray("checksum");
		}
		int dataSize = WritePdbChecksumData(_dataBuilder, algorithmName, checksum);
		AddEntry(DebugDirectoryEntryType.PdbChecksum, 1u, 0u, dataSize);
	}

	private static int WritePdbChecksumData(BlobBuilder builder, string algorithmName, ImmutableArray<byte> checksum)
	{
		int count = builder.Count;
		builder.WriteUTF8(algorithmName);
		builder.WriteByte(0);
		builder.WriteBytes(checksum);
		return builder.Count - count;
	}

	internal void Serialize(BlobBuilder builder, SectionLocation sectionLocation, int sectionOffset)
	{
		int num = sectionOffset + TableSize;
		foreach (Entry entry in _entries)
		{
			int value;
			int value2;
			if (entry.DataSize > 0)
			{
				value = sectionLocation.RelativeVirtualAddress + num;
				value2 = sectionLocation.PointerToRawData + num;
			}
			else
			{
				value = 0;
				value2 = 0;
			}
			builder.WriteUInt32(0u);
			builder.WriteUInt32(entry.Stamp);
			builder.WriteUInt32(entry.Version);
			builder.WriteInt32((int)entry.Type);
			builder.WriteInt32(entry.DataSize);
			builder.WriteInt32(value);
			builder.WriteInt32(value2);
			num += entry.DataSize;
		}
		builder.LinkSuffix(_dataBuilder);
	}

	public void AddEmbeddedPortablePdbEntry(BlobBuilder debugMetadata, ushort portablePdbVersion)
	{
		if (debugMetadata == null)
		{
			Throw.ArgumentNull("debugMetadata");
		}
		if (portablePdbVersion < 256)
		{
			Throw.ArgumentOutOfRange("portablePdbVersion");
		}
		int dataSize = WriteEmbeddedPortablePdbData(_dataBuilder, debugMetadata);
		AddEntry(DebugDirectoryEntryType.EmbeddedPortablePdb, PortablePdbVersions.DebugDirectoryEmbeddedVersion(portablePdbVersion), 0u, dataSize);
	}

	private static int WriteEmbeddedPortablePdbData(BlobBuilder builder, BlobBuilder debugMetadata)
	{
		int count = builder.Count;
		builder.WriteUInt32(1111773261u);
		builder.WriteInt32(debugMetadata.Count);
		MemoryStream memoryStream = new MemoryStream();
		using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
		{
			foreach (Blob blob in debugMetadata.GetBlobs())
			{
				ArraySegment<byte> bytes = blob.GetBytes();
				deflateStream.Write(bytes.Array, bytes.Offset, bytes.Count);
			}
		}
		builder.WriteBytes(memoryStream.ToArray());
		return builder.Count - count;
	}
}
