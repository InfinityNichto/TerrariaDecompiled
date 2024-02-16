namespace System.IO.Compression;

internal struct ZipEndOfCentralDirectoryBlock
{
	public uint Signature;

	public ushort NumberOfThisDisk;

	public ushort NumberOfTheDiskWithTheStartOfTheCentralDirectory;

	public ushort NumberOfEntriesInTheCentralDirectoryOnThisDisk;

	public ushort NumberOfEntriesInTheCentralDirectory;

	public uint SizeOfCentralDirectory;

	public uint OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber;

	public byte[] ArchiveComment;

	public static void WriteBlock(Stream stream, long numberOfEntries, long startOfCentralDirectory, long sizeOfCentralDirectory, byte[] archiveComment)
	{
		BinaryWriter binaryWriter = new BinaryWriter(stream);
		ushort value = ((numberOfEntries > 65535) ? ushort.MaxValue : ((ushort)numberOfEntries));
		uint value2 = (uint)((startOfCentralDirectory > uint.MaxValue) ? uint.MaxValue : startOfCentralDirectory);
		uint value3 = (uint)((sizeOfCentralDirectory > uint.MaxValue) ? uint.MaxValue : sizeOfCentralDirectory);
		binaryWriter.Write(101010256u);
		binaryWriter.Write((ushort)0);
		binaryWriter.Write((ushort)0);
		binaryWriter.Write(value);
		binaryWriter.Write(value);
		binaryWriter.Write(value3);
		binaryWriter.Write(value2);
		binaryWriter.Write((ushort)((archiveComment != null) ? ((ushort)archiveComment.Length) : 0));
		if (archiveComment != null)
		{
			binaryWriter.Write(archiveComment);
		}
	}

	public static bool TryReadBlock(BinaryReader reader, out ZipEndOfCentralDirectoryBlock eocdBlock)
	{
		eocdBlock = default(ZipEndOfCentralDirectoryBlock);
		if (reader.ReadUInt32() != 101010256)
		{
			return false;
		}
		eocdBlock.Signature = 101010256u;
		eocdBlock.NumberOfThisDisk = reader.ReadUInt16();
		eocdBlock.NumberOfTheDiskWithTheStartOfTheCentralDirectory = reader.ReadUInt16();
		eocdBlock.NumberOfEntriesInTheCentralDirectoryOnThisDisk = reader.ReadUInt16();
		eocdBlock.NumberOfEntriesInTheCentralDirectory = reader.ReadUInt16();
		eocdBlock.SizeOfCentralDirectory = reader.ReadUInt32();
		eocdBlock.OffsetOfStartOfCentralDirectoryWithRespectToTheStartingDiskNumber = reader.ReadUInt32();
		ushort count = reader.ReadUInt16();
		eocdBlock.ArchiveComment = reader.ReadBytes(count);
		return true;
	}
}
