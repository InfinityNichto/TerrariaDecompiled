using System.Reflection.Internal;
using System.Reflection.Metadata;

namespace System.Reflection.PortableExecutable;

internal sealed class ManagedTextSection
{
	public const int ManagedResourcesDataAlignment = 8;

	public const int MappedFieldDataAlignment = 8;

	public Characteristics ImageCharacteristics { get; }

	public Machine Machine { get; }

	public int ILStreamSize { get; }

	public int MetadataSize { get; }

	public int ResourceDataSize { get; }

	public int StrongNameSignatureSize { get; }

	public int DebugDataSize { get; }

	public int MappedFieldDataSize { get; }

	internal bool RequiresStartupStub
	{
		get
		{
			if (Machine != Machine.I386)
			{
				return Machine == Machine.Unknown;
			}
			return true;
		}
	}

	internal bool Requires64bits
	{
		get
		{
			if (Machine != Machine.Amd64 && Machine != Machine.IA64)
			{
				return Machine == Machine.Arm64;
			}
			return true;
		}
	}

	public bool Is32Bit => !Requires64bits;

	private string CorEntryPointName
	{
		get
		{
			if ((ImageCharacteristics & Characteristics.Dll) == 0)
			{
				return "_CorExeMain";
			}
			return "_CorDllMain";
		}
	}

	private int SizeOfImportAddressTable
	{
		get
		{
			if (!RequiresStartupStub)
			{
				return 0;
			}
			if (!Is32Bit)
			{
				return 16;
			}
			return 8;
		}
	}

	private int SizeOfImportTable => 40 + (Is32Bit ? 12 : 16) + 2 + CorEntryPointName.Length + 1;

	private static int SizeOfNameTable => "mscoree.dll".Length + 1 + 2;

	private int SizeOfRuntimeStartupStub
	{
		get
		{
			if (!Is32Bit)
			{
				return 16;
			}
			return 8;
		}
	}

	public int OffsetToILStream => SizeOfImportAddressTable + 72;

	public ManagedTextSection(Characteristics imageCharacteristics, Machine machine, int ilStreamSize, int metadataSize, int resourceDataSize, int strongNameSignatureSize, int debugDataSize, int mappedFieldDataSize)
	{
		MetadataSize = metadataSize;
		ResourceDataSize = resourceDataSize;
		ILStreamSize = ilStreamSize;
		MappedFieldDataSize = mappedFieldDataSize;
		StrongNameSignatureSize = strongNameSignatureSize;
		ImageCharacteristics = imageCharacteristics;
		Machine = machine;
		DebugDataSize = debugDataSize;
	}

	public int CalculateOffsetToMappedFieldDataStream()
	{
		int num = ComputeOffsetToImportTable();
		if (RequiresStartupStub)
		{
			num += SizeOfImportTable + SizeOfNameTable;
			num = BitArithmetic.Align(num, Is32Bit ? 4 : 8);
			num += SizeOfRuntimeStartupStub;
		}
		return num;
	}

	internal int ComputeOffsetToDebugDirectory()
	{
		return ComputeOffsetToMetadata() + MetadataSize + ResourceDataSize + StrongNameSignatureSize;
	}

	private int ComputeOffsetToImportTable()
	{
		return ComputeOffsetToDebugDirectory() + DebugDataSize;
	}

	private int ComputeOffsetToMetadata()
	{
		return OffsetToILStream + BitArithmetic.Align(ILStreamSize, 4);
	}

	public int ComputeSizeOfTextSection()
	{
		return CalculateOffsetToMappedFieldDataStream() + MappedFieldDataSize;
	}

	public int GetEntryPointAddress(int rva)
	{
		if (!RequiresStartupStub)
		{
			return 0;
		}
		return rva + CalculateOffsetToMappedFieldDataStream() - (Is32Bit ? 6 : 10);
	}

	public DirectoryEntry GetImportAddressTableDirectoryEntry(int rva)
	{
		if (!RequiresStartupStub)
		{
			return default(DirectoryEntry);
		}
		return new DirectoryEntry(rva, SizeOfImportAddressTable);
	}

	public DirectoryEntry GetImportTableDirectoryEntry(int rva)
	{
		if (!RequiresStartupStub)
		{
			return default(DirectoryEntry);
		}
		return new DirectoryEntry(rva + ComputeOffsetToImportTable(), (Is32Bit ? 66 : 70) + 13);
	}

	public DirectoryEntry GetCorHeaderDirectoryEntry(int rva)
	{
		return new DirectoryEntry(rva + SizeOfImportAddressTable, 72);
	}

	public void Serialize(BlobBuilder builder, int relativeVirtualAddess, int entryPointTokenOrRelativeVirtualAddress, CorFlags corFlags, ulong baseAddress, BlobBuilder metadataBuilder, BlobBuilder ilBuilder, BlobBuilder? mappedFieldDataBuilderOpt, BlobBuilder? resourceBuilderOpt, BlobBuilder? debugDataBuilderOpt, out Blob strongNameSignature)
	{
		int relativeVirtualAddress = GetImportTableDirectoryEntry(relativeVirtualAddess).RelativeVirtualAddress;
		int relativeVirtualAddress2 = GetImportAddressTableDirectoryEntry(relativeVirtualAddess).RelativeVirtualAddress;
		if (RequiresStartupStub)
		{
			WriteImportAddressTable(builder, relativeVirtualAddress);
		}
		WriteCorHeader(builder, relativeVirtualAddess, entryPointTokenOrRelativeVirtualAddress, corFlags);
		ilBuilder.Align(4);
		builder.LinkSuffix(ilBuilder);
		builder.LinkSuffix(metadataBuilder);
		if (resourceBuilderOpt != null)
		{
			builder.LinkSuffix(resourceBuilderOpt);
		}
		strongNameSignature = builder.ReserveBytes(StrongNameSignatureSize);
		new BlobWriter(strongNameSignature).WriteBytes(0, StrongNameSignatureSize);
		if (debugDataBuilderOpt != null)
		{
			builder.LinkSuffix(debugDataBuilderOpt);
		}
		if (RequiresStartupStub)
		{
			WriteImportTable(builder, relativeVirtualAddress, relativeVirtualAddress2);
			WriteNameTable(builder);
			WriteRuntimeStartupStub(builder, relativeVirtualAddress2, baseAddress);
		}
		if (mappedFieldDataBuilderOpt != null)
		{
			builder.LinkSuffix(mappedFieldDataBuilderOpt);
		}
	}

	private void WriteImportAddressTable(BlobBuilder builder, int importTableRva)
	{
		int count = builder.Count;
		int num = importTableRva + 40;
		int num2 = num + (Is32Bit ? 12 : 16);
		if (Is32Bit)
		{
			builder.WriteUInt32((uint)num2);
			builder.WriteUInt32(0u);
		}
		else
		{
			builder.WriteUInt64((uint)num2);
			builder.WriteUInt64(0uL);
		}
	}

	private void WriteImportTable(BlobBuilder builder, int importTableRva, int importAddressTableRva)
	{
		int count = builder.Count;
		int num = importTableRva + 40;
		int num2 = num + (Is32Bit ? 12 : 16);
		int value = num2 + 12 + 2;
		builder.WriteUInt32((uint)num);
		builder.WriteUInt32(0u);
		builder.WriteUInt32(0u);
		builder.WriteUInt32((uint)value);
		builder.WriteUInt32((uint)importAddressTableRva);
		builder.WriteBytes(0, 20);
		if (Is32Bit)
		{
			builder.WriteUInt32((uint)num2);
			builder.WriteUInt32(0u);
			builder.WriteUInt32(0u);
		}
		else
		{
			builder.WriteUInt64((uint)num2);
			builder.WriteUInt64(0uL);
		}
		builder.WriteUInt16(0);
		string corEntryPointName = CorEntryPointName;
		foreach (char c in corEntryPointName)
		{
			builder.WriteByte((byte)c);
		}
		builder.WriteByte(0);
	}

	private static void WriteNameTable(BlobBuilder builder)
	{
		int count = builder.Count;
		string text = "mscoree.dll";
		foreach (char c in text)
		{
			builder.WriteByte((byte)c);
		}
		builder.WriteByte(0);
		builder.WriteUInt16(0);
	}

	private void WriteCorHeader(BlobBuilder builder, int textSectionRva, int entryPointTokenOrRva, CorFlags corFlags)
	{
		int num = textSectionRva + ComputeOffsetToMetadata();
		int num2 = num + MetadataSize;
		int num3 = num2 + ResourceDataSize;
		int count = builder.Count;
		builder.WriteUInt32(72u);
		builder.WriteUInt16(2);
		builder.WriteUInt16(5);
		builder.WriteUInt32((uint)num);
		builder.WriteUInt32((uint)MetadataSize);
		builder.WriteUInt32((uint)corFlags);
		builder.WriteUInt32((uint)entryPointTokenOrRva);
		builder.WriteUInt32((ResourceDataSize != 0) ? ((uint)num2) : 0u);
		builder.WriteUInt32((uint)ResourceDataSize);
		builder.WriteUInt32((StrongNameSignatureSize != 0) ? ((uint)num3) : 0u);
		builder.WriteUInt32((uint)StrongNameSignatureSize);
		builder.WriteUInt32(0u);
		builder.WriteUInt32(0u);
		builder.WriteUInt32(0u);
		builder.WriteUInt32(0u);
		builder.WriteUInt32(0u);
		builder.WriteUInt32(0u);
		builder.WriteUInt32(0u);
		builder.WriteUInt32(0u);
	}

	private void WriteRuntimeStartupStub(BlobBuilder sectionBuilder, int importAddressTableRva, ulong baseAddress)
	{
		if (Is32Bit)
		{
			sectionBuilder.Align(4);
			sectionBuilder.WriteUInt16(0);
			sectionBuilder.WriteByte(byte.MaxValue);
			sectionBuilder.WriteByte(37);
			sectionBuilder.WriteUInt32((uint)(importAddressTableRva + (int)baseAddress));
		}
		else
		{
			sectionBuilder.Align(8);
			sectionBuilder.WriteUInt32(0u);
			sectionBuilder.WriteUInt16(0);
			sectionBuilder.WriteByte(byte.MaxValue);
			sectionBuilder.WriteByte(37);
			sectionBuilder.WriteUInt64((ulong)importAddressTableRva + baseAddress);
		}
	}
}
