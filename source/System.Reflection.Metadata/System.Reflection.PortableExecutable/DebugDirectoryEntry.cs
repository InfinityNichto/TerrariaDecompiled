namespace System.Reflection.PortableExecutable;

public readonly struct DebugDirectoryEntry
{
	internal const int Size = 28;

	public uint Stamp { get; }

	public ushort MajorVersion { get; }

	public ushort MinorVersion { get; }

	public DebugDirectoryEntryType Type { get; }

	public int DataSize { get; }

	public int DataRelativeVirtualAddress { get; }

	public int DataPointer { get; }

	public bool IsPortableCodeView => MinorVersion == 20557;

	public DebugDirectoryEntry(uint stamp, ushort majorVersion, ushort minorVersion, DebugDirectoryEntryType type, int dataSize, int dataRelativeVirtualAddress, int dataPointer)
	{
		Stamp = stamp;
		MajorVersion = majorVersion;
		MinorVersion = minorVersion;
		Type = type;
		DataSize = dataSize;
		DataRelativeVirtualAddress = dataRelativeVirtualAddress;
		DataPointer = dataPointer;
	}
}
