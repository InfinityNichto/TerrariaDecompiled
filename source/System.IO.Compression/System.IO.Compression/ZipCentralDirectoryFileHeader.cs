using System.Collections.Generic;

namespace System.IO.Compression;

internal struct ZipCentralDirectoryFileHeader
{
	public byte VersionMadeByCompatibility;

	public byte VersionMadeBySpecification;

	public ushort VersionNeededToExtract;

	public ushort GeneralPurposeBitFlag;

	public ushort CompressionMethod;

	public uint LastModified;

	public uint Crc32;

	public long CompressedSize;

	public long UncompressedSize;

	public ushort FilenameLength;

	public ushort ExtraFieldLength;

	public ushort FileCommentLength;

	public int DiskNumberStart;

	public ushort InternalFileAttributes;

	public uint ExternalFileAttributes;

	public long RelativeOffsetOfLocalHeader;

	public byte[] Filename;

	public byte[] FileComment;

	public List<ZipGenericExtraField> ExtraFields;

	public static bool TryReadBlock(BinaryReader reader, bool saveExtraFieldsAndComments, out ZipCentralDirectoryFileHeader header)
	{
		header = default(ZipCentralDirectoryFileHeader);
		if (reader.ReadUInt32() != 33639248)
		{
			return false;
		}
		header.VersionMadeBySpecification = reader.ReadByte();
		header.VersionMadeByCompatibility = reader.ReadByte();
		header.VersionNeededToExtract = reader.ReadUInt16();
		header.GeneralPurposeBitFlag = reader.ReadUInt16();
		header.CompressionMethod = reader.ReadUInt16();
		header.LastModified = reader.ReadUInt32();
		header.Crc32 = reader.ReadUInt32();
		uint num = reader.ReadUInt32();
		uint num2 = reader.ReadUInt32();
		header.FilenameLength = reader.ReadUInt16();
		header.ExtraFieldLength = reader.ReadUInt16();
		header.FileCommentLength = reader.ReadUInt16();
		ushort num3 = reader.ReadUInt16();
		header.InternalFileAttributes = reader.ReadUInt16();
		header.ExternalFileAttributes = reader.ReadUInt32();
		uint num4 = reader.ReadUInt32();
		header.Filename = reader.ReadBytes(header.FilenameLength);
		bool readUncompressedSize = num2 == uint.MaxValue;
		bool readCompressedSize = num == uint.MaxValue;
		bool readLocalHeaderOffset = num4 == uint.MaxValue;
		bool readStartDiskNumber = num3 == ushort.MaxValue;
		long position = reader.BaseStream.Position + header.ExtraFieldLength;
		Zip64ExtraField zip64ExtraField;
		using (Stream stream = new SubReadStream(reader.BaseStream, reader.BaseStream.Position, header.ExtraFieldLength))
		{
			if (saveExtraFieldsAndComments)
			{
				header.ExtraFields = ZipGenericExtraField.ParseExtraField(stream);
				zip64ExtraField = Zip64ExtraField.GetAndRemoveZip64Block(header.ExtraFields, readUncompressedSize, readCompressedSize, readLocalHeaderOffset, readStartDiskNumber);
			}
			else
			{
				header.ExtraFields = null;
				zip64ExtraField = Zip64ExtraField.GetJustZip64Block(stream, readUncompressedSize, readCompressedSize, readLocalHeaderOffset, readStartDiskNumber);
			}
		}
		reader.BaseStream.AdvanceToPosition(position);
		if (saveExtraFieldsAndComments)
		{
			header.FileComment = reader.ReadBytes(header.FileCommentLength);
		}
		else
		{
			reader.BaseStream.Position += header.FileCommentLength;
			header.FileComment = null;
		}
		header.UncompressedSize = ((!zip64ExtraField.UncompressedSize.HasValue) ? num2 : zip64ExtraField.UncompressedSize.Value);
		header.CompressedSize = ((!zip64ExtraField.CompressedSize.HasValue) ? num : zip64ExtraField.CompressedSize.Value);
		header.RelativeOffsetOfLocalHeader = ((!zip64ExtraField.LocalHeaderOffset.HasValue) ? num4 : zip64ExtraField.LocalHeaderOffset.Value);
		header.DiskNumberStart = ((!zip64ExtraField.StartDiskNumber.HasValue) ? num3 : zip64ExtraField.StartDiskNumber.Value);
		return true;
	}
}
