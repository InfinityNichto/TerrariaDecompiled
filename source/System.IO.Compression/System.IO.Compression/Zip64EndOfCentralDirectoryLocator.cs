namespace System.IO.Compression;

internal struct Zip64EndOfCentralDirectoryLocator
{
	public uint NumberOfDiskWithZip64EOCD;

	public ulong OffsetOfZip64EOCD;

	public uint TotalNumberOfDisks;

	public static bool TryReadBlock(BinaryReader reader, out Zip64EndOfCentralDirectoryLocator zip64EOCDLocator)
	{
		zip64EOCDLocator = default(Zip64EndOfCentralDirectoryLocator);
		if (reader.ReadUInt32() != 117853008)
		{
			return false;
		}
		zip64EOCDLocator.NumberOfDiskWithZip64EOCD = reader.ReadUInt32();
		zip64EOCDLocator.OffsetOfZip64EOCD = reader.ReadUInt64();
		zip64EOCDLocator.TotalNumberOfDisks = reader.ReadUInt32();
		return true;
	}

	public static void WriteBlock(Stream stream, long zip64EOCDRecordStart)
	{
		BinaryWriter binaryWriter = new BinaryWriter(stream);
		binaryWriter.Write(117853008u);
		binaryWriter.Write(0u);
		binaryWriter.Write(zip64EOCDRecordStart);
		binaryWriter.Write(1u);
	}
}
