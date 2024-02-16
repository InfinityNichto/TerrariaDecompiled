using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.IO.Compression;

[StructLayout(LayoutKind.Sequential, Size = 1)]
internal readonly struct ZipLocalFileHeader
{
	public static List<ZipGenericExtraField> GetExtraFields(BinaryReader reader)
	{
		reader.BaseStream.Seek(26L, SeekOrigin.Current);
		ushort num = reader.ReadUInt16();
		ushort num2 = reader.ReadUInt16();
		reader.BaseStream.Seek(num, SeekOrigin.Current);
		List<ZipGenericExtraField> list;
		using (Stream extraFieldData = new SubReadStream(reader.BaseStream, reader.BaseStream.Position, num2))
		{
			list = ZipGenericExtraField.ParseExtraField(extraFieldData);
		}
		Zip64ExtraField.RemoveZip64Blocks(list);
		return list;
	}

	public static bool TrySkipBlock(BinaryReader reader)
	{
		if (reader.ReadUInt32() != 67324752)
		{
			return false;
		}
		if (reader.BaseStream.Length < reader.BaseStream.Position + 22)
		{
			return false;
		}
		reader.BaseStream.Seek(22L, SeekOrigin.Current);
		ushort num = reader.ReadUInt16();
		ushort num2 = reader.ReadUInt16();
		if (reader.BaseStream.Length < reader.BaseStream.Position + num + num2)
		{
			return false;
		}
		reader.BaseStream.Seek(num + num2, SeekOrigin.Current);
		return true;
	}
}
