using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration.Assemblies;
using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Reflection.Metadata;

public sealed class MetadataReader
{
	private readonly struct ProjectionInfo
	{
		public readonly string WinRTNamespace;

		public readonly StringHandle.VirtualIndex ClrNamespace;

		public readonly StringHandle.VirtualIndex ClrName;

		public readonly AssemblyReferenceHandle.VirtualIndex AssemblyRef;

		public readonly TypeDefTreatment Treatment;

		public readonly TypeRefSignatureTreatment SignatureTreatment;

		public readonly bool IsIDisposable;

		public ProjectionInfo(string winRtNamespace, StringHandle.VirtualIndex clrNamespace, StringHandle.VirtualIndex clrName, AssemblyReferenceHandle.VirtualIndex clrAssembly, TypeDefTreatment treatment = TypeDefTreatment.RedirectedToClrType, TypeRefSignatureTreatment signatureTreatment = TypeRefSignatureTreatment.None, bool isIDisposable = false)
		{
			WinRTNamespace = winRtNamespace;
			ClrNamespace = clrNamespace;
			ClrName = clrName;
			AssemblyRef = clrAssembly;
			Treatment = treatment;
			SignatureTreatment = signatureTreatment;
			IsIDisposable = isIDisposable;
		}
	}

	internal readonly NamespaceCache NamespaceCache;

	internal readonly MemoryBlock Block;

	internal readonly int WinMDMscorlibRef;

	private readonly object _memoryOwnerObj;

	private readonly MetadataReaderOptions _options;

	private Dictionary<TypeDefinitionHandle, ImmutableArray<TypeDefinitionHandle>> _lazyNestedTypesMap;

	private readonly string _versionString;

	private readonly MetadataKind _metadataKind;

	private readonly MetadataStreamKind _metadataStreamKind;

	private readonly DebugMetadataHeader _debugMetadataHeader;

	internal StringHeap StringHeap;

	internal BlobHeap BlobHeap;

	internal GuidHeap GuidHeap;

	internal UserStringHeap UserStringHeap;

	internal bool IsMinimalDelta;

	private readonly TableMask _sortedTables;

	internal int[] TableRowCounts;

	internal ModuleTableReader ModuleTable;

	internal TypeRefTableReader TypeRefTable;

	internal TypeDefTableReader TypeDefTable;

	internal FieldPtrTableReader FieldPtrTable;

	internal FieldTableReader FieldTable;

	internal MethodPtrTableReader MethodPtrTable;

	internal MethodTableReader MethodDefTable;

	internal ParamPtrTableReader ParamPtrTable;

	internal ParamTableReader ParamTable;

	internal InterfaceImplTableReader InterfaceImplTable;

	internal MemberRefTableReader MemberRefTable;

	internal ConstantTableReader ConstantTable;

	internal CustomAttributeTableReader CustomAttributeTable;

	internal FieldMarshalTableReader FieldMarshalTable;

	internal DeclSecurityTableReader DeclSecurityTable;

	internal ClassLayoutTableReader ClassLayoutTable;

	internal FieldLayoutTableReader FieldLayoutTable;

	internal StandAloneSigTableReader StandAloneSigTable;

	internal EventMapTableReader EventMapTable;

	internal EventPtrTableReader EventPtrTable;

	internal EventTableReader EventTable;

	internal PropertyMapTableReader PropertyMapTable;

	internal PropertyPtrTableReader PropertyPtrTable;

	internal PropertyTableReader PropertyTable;

	internal MethodSemanticsTableReader MethodSemanticsTable;

	internal MethodImplTableReader MethodImplTable;

	internal ModuleRefTableReader ModuleRefTable;

	internal TypeSpecTableReader TypeSpecTable;

	internal ImplMapTableReader ImplMapTable;

	internal FieldRVATableReader FieldRvaTable;

	internal EnCLogTableReader EncLogTable;

	internal EnCMapTableReader EncMapTable;

	internal AssemblyTableReader AssemblyTable;

	internal AssemblyProcessorTableReader AssemblyProcessorTable;

	internal AssemblyOSTableReader AssemblyOSTable;

	internal AssemblyRefTableReader AssemblyRefTable;

	internal AssemblyRefProcessorTableReader AssemblyRefProcessorTable;

	internal AssemblyRefOSTableReader AssemblyRefOSTable;

	internal FileTableReader FileTable;

	internal ExportedTypeTableReader ExportedTypeTable;

	internal ManifestResourceTableReader ManifestResourceTable;

	internal NestedClassTableReader NestedClassTable;

	internal GenericParamTableReader GenericParamTable;

	internal MethodSpecTableReader MethodSpecTable;

	internal GenericParamConstraintTableReader GenericParamConstraintTable;

	internal DocumentTableReader DocumentTable;

	internal MethodDebugInformationTableReader MethodDebugInformationTable;

	internal LocalScopeTableReader LocalScopeTable;

	internal LocalVariableTableReader LocalVariableTable;

	internal LocalConstantTableReader LocalConstantTable;

	internal ImportScopeTableReader ImportScopeTable;

	internal StateMachineMethodTableReader StateMachineMethodTable;

	internal CustomDebugInformationTableReader CustomDebugInformationTable;

	internal const string ClrPrefix = "<CLR>";

	internal static readonly byte[] WinRTPrefix = new byte[7] { 60, 87, 105, 110, 82, 84, 62 };

	private static string[] s_projectedTypeNames;

	private static ProjectionInfo[] s_projectionInfos;

	internal bool UseFieldPtrTable => FieldPtrTable.NumberOfRows > 0;

	internal bool UseMethodPtrTable => MethodPtrTable.NumberOfRows > 0;

	internal bool UseParamPtrTable => ParamPtrTable.NumberOfRows > 0;

	internal bool UseEventPtrTable => EventPtrTable.NumberOfRows > 0;

	internal bool UsePropertyPtrTable => PropertyPtrTable.NumberOfRows > 0;

	public unsafe byte* MetadataPointer => Block.Pointer;

	public int MetadataLength => Block.Length;

	public MetadataReaderOptions Options => _options;

	public string MetadataVersion => _versionString;

	public DebugMetadataHeader? DebugMetadataHeader => _debugMetadataHeader;

	public MetadataKind MetadataKind => _metadataKind;

	public MetadataStringComparer StringComparer => new MetadataStringComparer(this);

	public MetadataStringDecoder UTF8Decoder { get; }

	public bool IsAssembly => AssemblyTable.NumberOfRows == 1;

	public AssemblyReferenceHandleCollection AssemblyReferences => new AssemblyReferenceHandleCollection(this);

	public TypeDefinitionHandleCollection TypeDefinitions => new TypeDefinitionHandleCollection(TypeDefTable.NumberOfRows);

	public TypeReferenceHandleCollection TypeReferences => new TypeReferenceHandleCollection(TypeRefTable.NumberOfRows);

	public CustomAttributeHandleCollection CustomAttributes => new CustomAttributeHandleCollection(this);

	public DeclarativeSecurityAttributeHandleCollection DeclarativeSecurityAttributes => new DeclarativeSecurityAttributeHandleCollection(this);

	public MemberReferenceHandleCollection MemberReferences => new MemberReferenceHandleCollection(MemberRefTable.NumberOfRows);

	public ManifestResourceHandleCollection ManifestResources => new ManifestResourceHandleCollection(ManifestResourceTable.NumberOfRows);

	public AssemblyFileHandleCollection AssemblyFiles => new AssemblyFileHandleCollection(FileTable.NumberOfRows);

	public ExportedTypeHandleCollection ExportedTypes => new ExportedTypeHandleCollection(ExportedTypeTable.NumberOfRows);

	public MethodDefinitionHandleCollection MethodDefinitions => new MethodDefinitionHandleCollection(this);

	public FieldDefinitionHandleCollection FieldDefinitions => new FieldDefinitionHandleCollection(this);

	public EventDefinitionHandleCollection EventDefinitions => new EventDefinitionHandleCollection(this);

	public PropertyDefinitionHandleCollection PropertyDefinitions => new PropertyDefinitionHandleCollection(this);

	public DocumentHandleCollection Documents => new DocumentHandleCollection(this);

	public MethodDebugInformationHandleCollection MethodDebugInformation => new MethodDebugInformationHandleCollection(this);

	public LocalScopeHandleCollection LocalScopes => new LocalScopeHandleCollection(this, 0);

	public LocalVariableHandleCollection LocalVariables => new LocalVariableHandleCollection(this, default(LocalScopeHandle));

	public LocalConstantHandleCollection LocalConstants => new LocalConstantHandleCollection(this, default(LocalScopeHandle));

	public ImportScopeCollection ImportScopes => new ImportScopeCollection(this);

	public CustomDebugInformationHandleCollection CustomDebugInformation => new CustomDebugInformationHandleCollection(this);

	internal AssemblyName GetAssemblyName(StringHandle nameHandle, Version version, StringHandle cultureHandle, BlobHandle publicKeyOrTokenHandle, AssemblyHashAlgorithm assemblyHashAlgorithm, AssemblyFlags flags)
	{
		string @string = GetString(nameHandle);
		string cultureName = ((!cultureHandle.IsNil) ? GetString(cultureHandle) : null);
		byte[] array = ((!publicKeyOrTokenHandle.IsNil) ? GetBlobBytes(publicKeyOrTokenHandle) : null);
		AssemblyName assemblyName = new AssemblyName(@string)
		{
			Version = version,
			CultureName = cultureName,
			HashAlgorithm = (System.Configuration.Assemblies.AssemblyHashAlgorithm)assemblyHashAlgorithm,
			Flags = GetAssemblyNameFlags(flags),
			ContentType = GetContentTypeFromAssemblyFlags(flags)
		};
		if ((flags & AssemblyFlags.PublicKey) != 0)
		{
			assemblyName.SetPublicKey(array);
		}
		else
		{
			assemblyName.SetPublicKeyToken(array);
		}
		return assemblyName;
	}

	private AssemblyNameFlags GetAssemblyNameFlags(AssemblyFlags flags)
	{
		AssemblyNameFlags assemblyNameFlags = AssemblyNameFlags.None;
		if ((flags & AssemblyFlags.PublicKey) != 0)
		{
			assemblyNameFlags |= AssemblyNameFlags.PublicKey;
		}
		if ((flags & AssemblyFlags.Retargetable) != 0)
		{
			assemblyNameFlags |= AssemblyNameFlags.Retargetable;
		}
		if ((flags & AssemblyFlags.EnableJitCompileTracking) != 0)
		{
			assemblyNameFlags |= AssemblyNameFlags.EnableJITcompileTracking;
		}
		if ((flags & AssemblyFlags.DisableJitCompileOptimizer) != 0)
		{
			assemblyNameFlags |= AssemblyNameFlags.EnableJITcompileOptimizer;
		}
		return assemblyNameFlags;
	}

	private AssemblyContentType GetContentTypeFromAssemblyFlags(AssemblyFlags flags)
	{
		return (AssemblyContentType)((int)(flags & AssemblyFlags.ContentTypeMask) >> 9);
	}

	public unsafe MetadataReader(byte* metadata, int length)
		: this(metadata, length, MetadataReaderOptions.Default, null, null)
	{
	}

	public unsafe MetadataReader(byte* metadata, int length, MetadataReaderOptions options)
		: this(metadata, length, options, null, null)
	{
	}

	public unsafe MetadataReader(byte* metadata, int length, MetadataReaderOptions options, MetadataStringDecoder? utf8Decoder)
		: this(metadata, length, options, utf8Decoder, null)
	{
	}

	internal unsafe MetadataReader(byte* metadata, int length, MetadataReaderOptions options, MetadataStringDecoder? utf8Decoder, object? memoryOwner)
	{
		if (length < 0)
		{
			Throw.ArgumentOutOfRange("length");
		}
		if (metadata == null)
		{
			Throw.ArgumentNull("metadata");
		}
		if (utf8Decoder == null)
		{
			utf8Decoder = MetadataStringDecoder.DefaultUTF8;
		}
		if (!(utf8Decoder.Encoding is UTF8Encoding))
		{
			Throw.InvalidArgument(System.SR.MetadataStringDecoderEncodingMustBeUtf8, "utf8Decoder");
		}
		Block = new MemoryBlock(metadata, length);
		_memoryOwnerObj = memoryOwner;
		_options = options;
		UTF8Decoder = utf8Decoder;
		BlobReader memReader = new BlobReader(Block);
		ReadMetadataHeader(ref memReader, out _versionString);
		_metadataKind = GetMetadataKind(_versionString);
		StreamHeader[] streamHeaders = ReadStreamHeaders(ref memReader);
		InitializeStreamReaders(in Block, streamHeaders, out _metadataStreamKind, out var metadataTableStream, out var standalonePdbStream);
		int[] externalTableRowCounts;
		if (standalonePdbStream.Length > 0)
		{
			int pdbStreamOffset = (int)(standalonePdbStream.Pointer - metadata);
			ReadStandalonePortablePdbStream(standalonePdbStream, pdbStreamOffset, out _debugMetadataHeader, out externalTableRowCounts);
		}
		else
		{
			externalTableRowCounts = null;
		}
		BlobReader reader = new BlobReader(metadataTableStream);
		ReadMetadataTableHeader(ref reader, out var heapSizes, out var metadataTableRowCounts, out _sortedTables);
		InitializeTableReaders(reader.GetMemoryBlockAt(0, reader.RemainingBytes), heapSizes, metadataTableRowCounts, externalTableRowCounts);
		if (standalonePdbStream.Length == 0 && ModuleTable.NumberOfRows < 1)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.ModuleTableInvalidNumberOfRows, ModuleTable.NumberOfRows));
		}
		NamespaceCache = new NamespaceCache(this);
		if (_metadataKind != 0)
		{
			WinMDMscorlibRef = FindMscorlibAssemblyRefNoProjection();
		}
	}

	private void ReadMetadataHeader(ref BlobReader memReader, out string versionString)
	{
		if (memReader.RemainingBytes < 16)
		{
			throw new BadImageFormatException(System.SR.MetadataHeaderTooSmall);
		}
		uint num = memReader.ReadUInt32();
		if (num != 1112167234)
		{
			throw new BadImageFormatException(System.SR.MetadataSignature);
		}
		memReader.ReadUInt16();
		memReader.ReadUInt16();
		memReader.ReadUInt32();
		int num2 = memReader.ReadInt32();
		if (memReader.RemainingBytes < num2)
		{
			throw new BadImageFormatException(System.SR.NotEnoughSpaceForVersionString);
		}
		versionString = memReader.GetMemoryBlockAt(0, num2).PeekUtf8NullTerminated(0, null, UTF8Decoder, out var _);
		memReader.Offset += num2;
	}

	private MetadataKind GetMetadataKind(string versionString)
	{
		if ((_options & MetadataReaderOptions.Default) == 0)
		{
			return MetadataKind.Ecma335;
		}
		if (!versionString.Contains("WindowsRuntime"))
		{
			return MetadataKind.Ecma335;
		}
		if (versionString.Contains("CLR"))
		{
			return MetadataKind.ManagedWindowsMetadata;
		}
		return MetadataKind.WindowsMetadata;
	}

	private StreamHeader[] ReadStreamHeaders(ref BlobReader memReader)
	{
		memReader.ReadUInt16();
		int num = memReader.ReadInt16();
		StreamHeader[] array = new StreamHeader[num];
		for (int i = 0; i < array.Length; i++)
		{
			if (memReader.RemainingBytes < 8)
			{
				throw new BadImageFormatException(System.SR.StreamHeaderTooSmall);
			}
			array[i].Offset = memReader.ReadUInt32();
			array[i].Size = memReader.ReadInt32();
			array[i].Name = memReader.ReadUtf8NullTerminated();
			if (!memReader.TryAlign(4) || memReader.RemainingBytes == 0)
			{
				throw new BadImageFormatException(System.SR.NotEnoughSpaceForStreamHeaderName);
			}
		}
		return array;
	}

	private void InitializeStreamReaders(in MemoryBlock metadataRoot, StreamHeader[] streamHeaders, out MetadataStreamKind metadataStreamKind, out MemoryBlock metadataTableStream, out MemoryBlock standalonePdbStream)
	{
		metadataTableStream = default(MemoryBlock);
		standalonePdbStream = default(MemoryBlock);
		metadataStreamKind = MetadataStreamKind.Illegal;
		for (int i = 0; i < streamHeaders.Length; i++)
		{
			StreamHeader streamHeader = streamHeaders[i];
			switch (streamHeader.Name)
			{
			case "#Strings":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForStringStream);
				}
				StringHeap = new StringHeap(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size), _metadataKind);
				break;
			case "#Blob":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForBlobStream);
				}
				BlobHeap = new BlobHeap(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size), _metadataKind);
				break;
			case "#GUID":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForGUIDStream);
				}
				GuidHeap = new GuidHeap(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size));
				break;
			case "#US":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForBlobStream);
				}
				UserStringHeap = new UserStringHeap(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size));
				break;
			case "#~":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForMetadataStream);
				}
				metadataStreamKind = MetadataStreamKind.Compressed;
				metadataTableStream = metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size);
				break;
			case "#-":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForMetadataStream);
				}
				metadataStreamKind = MetadataStreamKind.Uncompressed;
				metadataTableStream = metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size);
				break;
			case "#JTD":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForMetadataStream);
				}
				IsMinimalDelta = true;
				break;
			case "#Pdb":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForMetadataStream);
				}
				standalonePdbStream = metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size);
				break;
			}
		}
		if (IsMinimalDelta && metadataStreamKind != MetadataStreamKind.Uncompressed)
		{
			throw new BadImageFormatException(System.SR.InvalidMetadataStreamFormat);
		}
	}

	private void ReadMetadataTableHeader(ref BlobReader reader, out HeapSizes heapSizes, out int[] metadataTableRowCounts, out TableMask sortedTables)
	{
		if (reader.RemainingBytes < 24)
		{
			throw new BadImageFormatException(System.SR.MetadataTableHeaderTooSmall);
		}
		reader.ReadUInt32();
		reader.ReadByte();
		reader.ReadByte();
		heapSizes = (HeapSizes)reader.ReadByte();
		reader.ReadByte();
		ulong num = reader.ReadUInt64();
		sortedTables = (TableMask)reader.ReadUInt64();
		ulong num2 = 71811071505072127uL;
		if ((num & ~num2) != 0L)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.UnknownTables, num));
		}
		if (_metadataStreamKind == MetadataStreamKind.Compressed && (num & 0x804800A8u) != 0L)
		{
			throw new BadImageFormatException(System.SR.IllegalTablesInCompressedMetadataStream);
		}
		metadataTableRowCounts = ReadMetadataTableRowCounts(ref reader, num);
		if ((heapSizes & HeapSizes.ExtraData) == HeapSizes.ExtraData)
		{
			reader.ReadUInt32();
		}
	}

	private static int[] ReadMetadataTableRowCounts(ref BlobReader memReader, ulong presentTableMask)
	{
		ulong num = 1uL;
		int[] array = new int[MetadataTokens.TableCount];
		for (int i = 0; i < array.Length; i++)
		{
			if ((presentTableMask & num) != 0L)
			{
				if (memReader.RemainingBytes < 4)
				{
					throw new BadImageFormatException(System.SR.TableRowCountSpaceTooSmall);
				}
				uint num2 = memReader.ReadUInt32();
				if (num2 > 16777215)
				{
					throw new BadImageFormatException(System.SR.Format(System.SR.InvalidRowCount, num2));
				}
				array[i] = (int)num2;
			}
			num <<= 1;
		}
		return array;
	}

	internal static void ReadStandalonePortablePdbStream(MemoryBlock pdbStreamBlock, int pdbStreamOffset, out DebugMetadataHeader debugMetadataHeader, out int[] externalTableRowCounts)
	{
		BlobReader memReader = new BlobReader(pdbStreamBlock);
		byte[] array = memReader.ReadBytes(20);
		uint num = memReader.ReadUInt32();
		int num2 = (int)(num & 0xFFFFFF);
		if (num != 0 && ((num & 0x7F000000) != 100663296 || num2 == 0))
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.InvalidEntryPointToken, num));
		}
		ulong num3 = memReader.ReadUInt64();
		if ((num3 & 0xFFFFE036C04800A8uL) != 0L)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.UnknownTables, num3));
		}
		externalTableRowCounts = ReadMetadataTableRowCounts(ref memReader, num3);
		debugMetadataHeader = new DebugMetadataHeader(ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref array), MethodDefinitionHandle.FromRowId(num2), pdbStreamOffset);
	}

	private int GetReferenceSize(int[] rowCounts, TableIndex index)
	{
		if ((long)rowCounts[(uint)index] >= 65536L || IsMinimalDelta)
		{
			return 4;
		}
		return 2;
	}

	private void InitializeTableReaders(MemoryBlock metadataTablesMemoryBlock, HeapSizes heapSizes, int[] rowCounts, int[] externalRowCountsOpt)
	{
		TableRowCounts = rowCounts;
		int fieldRefSize = ((GetReferenceSize(rowCounts, TableIndex.FieldPtr) > 2) ? 4 : GetReferenceSize(rowCounts, TableIndex.Field));
		int methodRefSize = ((GetReferenceSize(rowCounts, TableIndex.MethodPtr) > 2) ? 4 : GetReferenceSize(rowCounts, TableIndex.MethodDef));
		int paramRefSize = ((GetReferenceSize(rowCounts, TableIndex.ParamPtr) > 2) ? 4 : GetReferenceSize(rowCounts, TableIndex.Param));
		int eventRefSize = ((GetReferenceSize(rowCounts, TableIndex.EventPtr) > 2) ? 4 : GetReferenceSize(rowCounts, TableIndex.Event));
		int propertyRefSize = ((GetReferenceSize(rowCounts, TableIndex.PropertyPtr) > 2) ? 4 : GetReferenceSize(rowCounts, TableIndex.Property));
		int typeDefOrRefRefSize = ComputeCodedTokenSize(16384, rowCounts, TableMask.TypeRef | TableMask.TypeDef | TableMask.TypeSpec);
		int hasConstantRefSize = ComputeCodedTokenSize(16384, rowCounts, TableMask.Field | TableMask.Param | TableMask.Property);
		int hasCustomAttributeRefSize = ComputeCodedTokenSize(2048, rowCounts, TableMask.Module | TableMask.TypeRef | TableMask.TypeDef | TableMask.Field | TableMask.MethodDef | TableMask.Param | TableMask.InterfaceImpl | TableMask.MemberRef | TableMask.DeclSecurity | TableMask.StandAloneSig | TableMask.Event | TableMask.Property | TableMask.ModuleRef | TableMask.TypeSpec | TableMask.Assembly | TableMask.AssemblyRef | TableMask.File | TableMask.ExportedType | TableMask.ManifestResource | TableMask.GenericParam | TableMask.MethodSpec | TableMask.GenericParamConstraint);
		int hasFieldMarshalRefSize = ComputeCodedTokenSize(32768, rowCounts, TableMask.Field | TableMask.Param);
		int hasDeclSecurityRefSize = ComputeCodedTokenSize(16384, rowCounts, TableMask.TypeDef | TableMask.MethodDef | TableMask.Assembly);
		int memberRefParentRefSize = ComputeCodedTokenSize(8192, rowCounts, TableMask.TypeRef | TableMask.TypeDef | TableMask.MethodDef | TableMask.ModuleRef | TableMask.TypeSpec);
		int hasSemanticRefSize = ComputeCodedTokenSize(32768, rowCounts, TableMask.Event | TableMask.Property);
		int methodDefOrRefRefSize = ComputeCodedTokenSize(32768, rowCounts, TableMask.MethodDef | TableMask.MemberRef);
		int memberForwardedRefSize = ComputeCodedTokenSize(32768, rowCounts, TableMask.Field | TableMask.MethodDef);
		int implementationRefSize = ComputeCodedTokenSize(16384, rowCounts, TableMask.AssemblyRef | TableMask.File | TableMask.ExportedType);
		int customAttributeTypeRefSize = ComputeCodedTokenSize(8192, rowCounts, TableMask.MethodDef | TableMask.MemberRef);
		int resolutionScopeRefSize = ComputeCodedTokenSize(16384, rowCounts, TableMask.Module | TableMask.TypeRef | TableMask.ModuleRef | TableMask.AssemblyRef);
		int typeOrMethodDefRefSize = ComputeCodedTokenSize(32768, rowCounts, TableMask.TypeDef | TableMask.MethodDef);
		int stringHeapRefSize = (((heapSizes & HeapSizes.StringHeapLarge) == HeapSizes.StringHeapLarge) ? 4 : 2);
		int guidHeapRefSize = (((heapSizes & HeapSizes.GuidHeapLarge) == HeapSizes.GuidHeapLarge) ? 4 : 2);
		int blobHeapRefSize = (((heapSizes & HeapSizes.BlobHeapLarge) == HeapSizes.BlobHeapLarge) ? 4 : 2);
		int num = 0;
		ModuleTable = new ModuleTableReader(rowCounts[0], stringHeapRefSize, guidHeapRefSize, metadataTablesMemoryBlock, num);
		num += ModuleTable.Block.Length;
		TypeRefTable = new TypeRefTableReader(rowCounts[1], resolutionScopeRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += TypeRefTable.Block.Length;
		TypeDefTable = new TypeDefTableReader(rowCounts[2], fieldRefSize, methodRefSize, typeDefOrRefRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += TypeDefTable.Block.Length;
		FieldPtrTable = new FieldPtrTableReader(rowCounts[3], GetReferenceSize(rowCounts, TableIndex.Field), metadataTablesMemoryBlock, num);
		num += FieldPtrTable.Block.Length;
		FieldTable = new FieldTableReader(rowCounts[4], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += FieldTable.Block.Length;
		MethodPtrTable = new MethodPtrTableReader(rowCounts[5], GetReferenceSize(rowCounts, TableIndex.MethodDef), metadataTablesMemoryBlock, num);
		num += MethodPtrTable.Block.Length;
		MethodDefTable = new MethodTableReader(rowCounts[6], paramRefSize, stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += MethodDefTable.Block.Length;
		ParamPtrTable = new ParamPtrTableReader(rowCounts[7], GetReferenceSize(rowCounts, TableIndex.Param), metadataTablesMemoryBlock, num);
		num += ParamPtrTable.Block.Length;
		ParamTable = new ParamTableReader(rowCounts[8], stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += ParamTable.Block.Length;
		InterfaceImplTable = new InterfaceImplTableReader(rowCounts[9], IsDeclaredSorted(TableMask.InterfaceImpl), GetReferenceSize(rowCounts, TableIndex.TypeDef), typeDefOrRefRefSize, metadataTablesMemoryBlock, num);
		num += InterfaceImplTable.Block.Length;
		MemberRefTable = new MemberRefTableReader(rowCounts[10], memberRefParentRefSize, stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += MemberRefTable.Block.Length;
		ConstantTable = new ConstantTableReader(rowCounts[11], IsDeclaredSorted(TableMask.Constant), hasConstantRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += ConstantTable.Block.Length;
		CustomAttributeTable = new CustomAttributeTableReader(rowCounts[12], IsDeclaredSorted(TableMask.CustomAttribute), hasCustomAttributeRefSize, customAttributeTypeRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += CustomAttributeTable.Block.Length;
		FieldMarshalTable = new FieldMarshalTableReader(rowCounts[13], IsDeclaredSorted(TableMask.FieldMarshal), hasFieldMarshalRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += FieldMarshalTable.Block.Length;
		DeclSecurityTable = new DeclSecurityTableReader(rowCounts[14], IsDeclaredSorted(TableMask.DeclSecurity), hasDeclSecurityRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += DeclSecurityTable.Block.Length;
		ClassLayoutTable = new ClassLayoutTableReader(rowCounts[15], IsDeclaredSorted(TableMask.ClassLayout), GetReferenceSize(rowCounts, TableIndex.TypeDef), metadataTablesMemoryBlock, num);
		num += ClassLayoutTable.Block.Length;
		FieldLayoutTable = new FieldLayoutTableReader(rowCounts[16], IsDeclaredSorted(TableMask.FieldLayout), GetReferenceSize(rowCounts, TableIndex.Field), metadataTablesMemoryBlock, num);
		num += FieldLayoutTable.Block.Length;
		StandAloneSigTable = new StandAloneSigTableReader(rowCounts[17], blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += StandAloneSigTable.Block.Length;
		EventMapTable = new EventMapTableReader(rowCounts[18], GetReferenceSize(rowCounts, TableIndex.TypeDef), eventRefSize, metadataTablesMemoryBlock, num);
		num += EventMapTable.Block.Length;
		EventPtrTable = new EventPtrTableReader(rowCounts[19], GetReferenceSize(rowCounts, TableIndex.Event), metadataTablesMemoryBlock, num);
		num += EventPtrTable.Block.Length;
		EventTable = new EventTableReader(rowCounts[20], typeDefOrRefRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += EventTable.Block.Length;
		PropertyMapTable = new PropertyMapTableReader(rowCounts[21], GetReferenceSize(rowCounts, TableIndex.TypeDef), propertyRefSize, metadataTablesMemoryBlock, num);
		num += PropertyMapTable.Block.Length;
		PropertyPtrTable = new PropertyPtrTableReader(rowCounts[22], GetReferenceSize(rowCounts, TableIndex.Property), metadataTablesMemoryBlock, num);
		num += PropertyPtrTable.Block.Length;
		PropertyTable = new PropertyTableReader(rowCounts[23], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += PropertyTable.Block.Length;
		MethodSemanticsTable = new MethodSemanticsTableReader(rowCounts[24], IsDeclaredSorted(TableMask.MethodSemantics), GetReferenceSize(rowCounts, TableIndex.MethodDef), hasSemanticRefSize, metadataTablesMemoryBlock, num);
		num += MethodSemanticsTable.Block.Length;
		MethodImplTable = new MethodImplTableReader(rowCounts[25], IsDeclaredSorted(TableMask.MethodImpl), GetReferenceSize(rowCounts, TableIndex.TypeDef), methodDefOrRefRefSize, metadataTablesMemoryBlock, num);
		num += MethodImplTable.Block.Length;
		ModuleRefTable = new ModuleRefTableReader(rowCounts[26], stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += ModuleRefTable.Block.Length;
		TypeSpecTable = new TypeSpecTableReader(rowCounts[27], blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += TypeSpecTable.Block.Length;
		ImplMapTable = new ImplMapTableReader(rowCounts[28], IsDeclaredSorted(TableMask.ImplMap), GetReferenceSize(rowCounts, TableIndex.ModuleRef), memberForwardedRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += ImplMapTable.Block.Length;
		FieldRvaTable = new FieldRVATableReader(rowCounts[29], IsDeclaredSorted(TableMask.FieldRva), GetReferenceSize(rowCounts, TableIndex.Field), metadataTablesMemoryBlock, num);
		num += FieldRvaTable.Block.Length;
		EncLogTable = new EnCLogTableReader(rowCounts[30], metadataTablesMemoryBlock, num, _metadataStreamKind);
		num += EncLogTable.Block.Length;
		EncMapTable = new EnCMapTableReader(rowCounts[31], metadataTablesMemoryBlock, num);
		num += EncMapTable.Block.Length;
		AssemblyTable = new AssemblyTableReader(rowCounts[32], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += AssemblyTable.Block.Length;
		AssemblyProcessorTable = new AssemblyProcessorTableReader(rowCounts[33], metadataTablesMemoryBlock, num);
		num += AssemblyProcessorTable.Block.Length;
		AssemblyOSTable = new AssemblyOSTableReader(rowCounts[34], metadataTablesMemoryBlock, num);
		num += AssemblyOSTable.Block.Length;
		AssemblyRefTable = new AssemblyRefTableReader(rowCounts[35], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num, _metadataKind);
		num += AssemblyRefTable.Block.Length;
		AssemblyRefProcessorTable = new AssemblyRefProcessorTableReader(rowCounts[36], GetReferenceSize(rowCounts, TableIndex.AssemblyRef), metadataTablesMemoryBlock, num);
		num += AssemblyRefProcessorTable.Block.Length;
		AssemblyRefOSTable = new AssemblyRefOSTableReader(rowCounts[37], GetReferenceSize(rowCounts, TableIndex.AssemblyRef), metadataTablesMemoryBlock, num);
		num += AssemblyRefOSTable.Block.Length;
		FileTable = new FileTableReader(rowCounts[38], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += FileTable.Block.Length;
		ExportedTypeTable = new ExportedTypeTableReader(rowCounts[39], implementationRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += ExportedTypeTable.Block.Length;
		ManifestResourceTable = new ManifestResourceTableReader(rowCounts[40], implementationRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += ManifestResourceTable.Block.Length;
		NestedClassTable = new NestedClassTableReader(rowCounts[41], IsDeclaredSorted(TableMask.NestedClass), GetReferenceSize(rowCounts, TableIndex.TypeDef), metadataTablesMemoryBlock, num);
		num += NestedClassTable.Block.Length;
		GenericParamTable = new GenericParamTableReader(rowCounts[42], IsDeclaredSorted(TableMask.GenericParam), typeOrMethodDefRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += GenericParamTable.Block.Length;
		MethodSpecTable = new MethodSpecTableReader(rowCounts[43], methodDefOrRefRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += MethodSpecTable.Block.Length;
		GenericParamConstraintTable = new GenericParamConstraintTableReader(rowCounts[44], IsDeclaredSorted(TableMask.GenericParamConstraint), GetReferenceSize(rowCounts, TableIndex.GenericParam), typeDefOrRefRefSize, metadataTablesMemoryBlock, num);
		num += GenericParamConstraintTable.Block.Length;
		int[] rowCounts2 = ((externalRowCountsOpt != null) ? CombineRowCounts(rowCounts, externalRowCountsOpt, TableIndex.Document) : rowCounts);
		int referenceSize = GetReferenceSize(rowCounts2, TableIndex.MethodDef);
		int hasCustomDebugInformationRefSize = ComputeCodedTokenSize(2048, rowCounts2, TableMask.Module | TableMask.TypeRef | TableMask.TypeDef | TableMask.Field | TableMask.MethodDef | TableMask.Param | TableMask.InterfaceImpl | TableMask.MemberRef | TableMask.DeclSecurity | TableMask.StandAloneSig | TableMask.Event | TableMask.Property | TableMask.ModuleRef | TableMask.TypeSpec | TableMask.Assembly | TableMask.AssemblyRef | TableMask.File | TableMask.ExportedType | TableMask.ManifestResource | TableMask.GenericParam | TableMask.MethodSpec | TableMask.GenericParamConstraint | TableMask.Document | TableMask.LocalScope | TableMask.LocalVariable | TableMask.LocalConstant | TableMask.ImportScope);
		DocumentTable = new DocumentTableReader(rowCounts[48], guidHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += DocumentTable.Block.Length;
		MethodDebugInformationTable = new MethodDebugInformationTableReader(rowCounts[49], GetReferenceSize(rowCounts, TableIndex.Document), blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += MethodDebugInformationTable.Block.Length;
		LocalScopeTable = new LocalScopeTableReader(rowCounts[50], IsDeclaredSorted(TableMask.LocalScope), referenceSize, GetReferenceSize(rowCounts, TableIndex.ImportScope), GetReferenceSize(rowCounts, TableIndex.LocalVariable), GetReferenceSize(rowCounts, TableIndex.LocalConstant), metadataTablesMemoryBlock, num);
		num += LocalScopeTable.Block.Length;
		LocalVariableTable = new LocalVariableTableReader(rowCounts[51], stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += LocalVariableTable.Block.Length;
		LocalConstantTable = new LocalConstantTableReader(rowCounts[52], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += LocalConstantTable.Block.Length;
		ImportScopeTable = new ImportScopeTableReader(rowCounts[53], GetReferenceSize(rowCounts, TableIndex.ImportScope), blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += ImportScopeTable.Block.Length;
		StateMachineMethodTable = new StateMachineMethodTableReader(rowCounts[54], IsDeclaredSorted(TableMask.StateMachineMethod), referenceSize, metadataTablesMemoryBlock, num);
		num += StateMachineMethodTable.Block.Length;
		CustomDebugInformationTable = new CustomDebugInformationTableReader(rowCounts[55], IsDeclaredSorted(TableMask.CustomDebugInformation), hasCustomDebugInformationRefSize, guidHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += CustomDebugInformationTable.Block.Length;
		if (num > metadataTablesMemoryBlock.Length)
		{
			throw new BadImageFormatException(System.SR.MetadataTablesTooSmall);
		}
	}

	private static int[] CombineRowCounts(int[] local, int[] external, TableIndex firstLocalTableIndex)
	{
		int[] array = new int[local.Length];
		for (int i = 0; i < (int)firstLocalTableIndex; i++)
		{
			array[i] = external[i];
		}
		for (int j = (int)firstLocalTableIndex; j < array.Length; j++)
		{
			array[j] = local[j];
		}
		return array;
	}

	private int ComputeCodedTokenSize(int largeRowSize, int[] rowCounts, TableMask tablesReferenced)
	{
		if (IsMinimalDelta)
		{
			return 4;
		}
		bool flag = true;
		ulong num = (ulong)tablesReferenced;
		for (int i = 0; i < MetadataTokens.TableCount; i++)
		{
			if ((num & 1) != 0L)
			{
				flag = flag && rowCounts[i] < largeRowSize;
			}
			num >>= 1;
		}
		if (!flag)
		{
			return 4;
		}
		return 2;
	}

	private bool IsDeclaredSorted(TableMask index)
	{
		return (_sortedTables & index) != 0;
	}

	internal void GetFieldRange(TypeDefinitionHandle typeDef, out int firstFieldRowId, out int lastFieldRowId)
	{
		int rowId = typeDef.RowId;
		firstFieldRowId = TypeDefTable.GetFieldStart(rowId);
		if (firstFieldRowId == 0)
		{
			firstFieldRowId = 1;
			lastFieldRowId = 0;
		}
		else if (rowId == TypeDefTable.NumberOfRows)
		{
			lastFieldRowId = (UseFieldPtrTable ? FieldPtrTable.NumberOfRows : FieldTable.NumberOfRows);
		}
		else
		{
			lastFieldRowId = TypeDefTable.GetFieldStart(rowId + 1) - 1;
		}
	}

	internal void GetMethodRange(TypeDefinitionHandle typeDef, out int firstMethodRowId, out int lastMethodRowId)
	{
		int rowId = typeDef.RowId;
		firstMethodRowId = TypeDefTable.GetMethodStart(rowId);
		if (firstMethodRowId == 0)
		{
			firstMethodRowId = 1;
			lastMethodRowId = 0;
		}
		else if (rowId == TypeDefTable.NumberOfRows)
		{
			lastMethodRowId = (UseMethodPtrTable ? MethodPtrTable.NumberOfRows : MethodDefTable.NumberOfRows);
		}
		else
		{
			lastMethodRowId = TypeDefTable.GetMethodStart(rowId + 1) - 1;
		}
	}

	internal void GetEventRange(TypeDefinitionHandle typeDef, out int firstEventRowId, out int lastEventRowId)
	{
		int num = EventMapTable.FindEventMapRowIdFor(typeDef);
		if (num == 0)
		{
			firstEventRowId = 1;
			lastEventRowId = 0;
			return;
		}
		firstEventRowId = EventMapTable.GetEventListStartFor(num);
		if (num == EventMapTable.NumberOfRows)
		{
			lastEventRowId = (UseEventPtrTable ? EventPtrTable.NumberOfRows : EventTable.NumberOfRows);
		}
		else
		{
			lastEventRowId = EventMapTable.GetEventListStartFor(num + 1) - 1;
		}
	}

	internal void GetPropertyRange(TypeDefinitionHandle typeDef, out int firstPropertyRowId, out int lastPropertyRowId)
	{
		int num = PropertyMapTable.FindPropertyMapRowIdFor(typeDef);
		if (num == 0)
		{
			firstPropertyRowId = 1;
			lastPropertyRowId = 0;
			return;
		}
		firstPropertyRowId = PropertyMapTable.GetPropertyListStartFor(num);
		if (num == PropertyMapTable.NumberOfRows)
		{
			lastPropertyRowId = (UsePropertyPtrTable ? PropertyPtrTable.NumberOfRows : PropertyTable.NumberOfRows);
		}
		else
		{
			lastPropertyRowId = PropertyMapTable.GetPropertyListStartFor(num + 1) - 1;
		}
	}

	internal void GetParameterRange(MethodDefinitionHandle methodDef, out int firstParamRowId, out int lastParamRowId)
	{
		int rowId = methodDef.RowId;
		firstParamRowId = MethodDefTable.GetParamStart(rowId);
		if (firstParamRowId == 0)
		{
			firstParamRowId = 1;
			lastParamRowId = 0;
		}
		else if (rowId == MethodDefTable.NumberOfRows)
		{
			lastParamRowId = (UseParamPtrTable ? ParamPtrTable.NumberOfRows : ParamTable.NumberOfRows);
		}
		else
		{
			lastParamRowId = MethodDefTable.GetParamStart(rowId + 1) - 1;
		}
	}

	internal void GetLocalVariableRange(LocalScopeHandle scope, out int firstVariableRowId, out int lastVariableRowId)
	{
		int rowId = scope.RowId;
		firstVariableRowId = LocalScopeTable.GetVariableStart(rowId);
		if (firstVariableRowId == 0)
		{
			firstVariableRowId = 1;
			lastVariableRowId = 0;
		}
		else if (rowId == LocalScopeTable.NumberOfRows)
		{
			lastVariableRowId = LocalVariableTable.NumberOfRows;
		}
		else
		{
			lastVariableRowId = LocalScopeTable.GetVariableStart(rowId + 1) - 1;
		}
	}

	internal void GetLocalConstantRange(LocalScopeHandle scope, out int firstConstantRowId, out int lastConstantRowId)
	{
		int rowId = scope.RowId;
		firstConstantRowId = LocalScopeTable.GetConstantStart(rowId);
		if (firstConstantRowId == 0)
		{
			firstConstantRowId = 1;
			lastConstantRowId = 0;
		}
		else if (rowId == LocalScopeTable.NumberOfRows)
		{
			lastConstantRowId = LocalConstantTable.NumberOfRows;
		}
		else
		{
			lastConstantRowId = LocalScopeTable.GetConstantStart(rowId + 1) - 1;
		}
	}

	public AssemblyDefinition GetAssemblyDefinition()
	{
		if (!IsAssembly)
		{
			throw new InvalidOperationException(System.SR.MetadataImageDoesNotRepresentAnAssembly);
		}
		return new AssemblyDefinition(this);
	}

	public string GetString(StringHandle handle)
	{
		return StringHeap.GetString(handle, UTF8Decoder);
	}

	public string GetString(NamespaceDefinitionHandle handle)
	{
		if (handle.HasFullName)
		{
			return StringHeap.GetString(handle.GetFullName(), UTF8Decoder);
		}
		return NamespaceCache.GetFullName(handle);
	}

	public byte[] GetBlobBytes(BlobHandle handle)
	{
		return BlobHeap.GetBytes(handle);
	}

	public ImmutableArray<byte> GetBlobContent(BlobHandle handle)
	{
		byte[] array = GetBlobBytes(handle);
		return ImmutableByteArrayInterop.DangerousCreateFromUnderlyingArray(ref array);
	}

	public BlobReader GetBlobReader(BlobHandle handle)
	{
		return BlobHeap.GetBlobReader(handle);
	}

	public BlobReader GetBlobReader(StringHandle handle)
	{
		return StringHeap.GetBlobReader(handle);
	}

	public string GetUserString(UserStringHandle handle)
	{
		return UserStringHeap.GetString(handle);
	}

	public Guid GetGuid(GuidHandle handle)
	{
		return GuidHeap.GetGuid(handle);
	}

	public ModuleDefinition GetModuleDefinition()
	{
		if (_debugMetadataHeader != null)
		{
			throw new InvalidOperationException(System.SR.StandaloneDebugMetadataImageDoesNotContainModuleTable);
		}
		return new ModuleDefinition(this);
	}

	public AssemblyReference GetAssemblyReference(AssemblyReferenceHandle handle)
	{
		return new AssemblyReference(this, handle.Value);
	}

	public TypeDefinition GetTypeDefinition(TypeDefinitionHandle handle)
	{
		return new TypeDefinition(this, GetTypeDefTreatmentAndRowId(handle));
	}

	public NamespaceDefinition GetNamespaceDefinitionRoot()
	{
		NamespaceData rootNamespace = NamespaceCache.GetRootNamespace();
		return new NamespaceDefinition(rootNamespace);
	}

	public NamespaceDefinition GetNamespaceDefinition(NamespaceDefinitionHandle handle)
	{
		NamespaceData namespaceData = NamespaceCache.GetNamespaceData(handle);
		return new NamespaceDefinition(namespaceData);
	}

	private uint GetTypeDefTreatmentAndRowId(TypeDefinitionHandle handle)
	{
		if (_metadataKind == MetadataKind.Ecma335)
		{
			return (uint)handle.RowId;
		}
		return CalculateTypeDefTreatmentAndRowId(handle);
	}

	public TypeReference GetTypeReference(TypeReferenceHandle handle)
	{
		return new TypeReference(this, GetTypeRefTreatmentAndRowId(handle));
	}

	private uint GetTypeRefTreatmentAndRowId(TypeReferenceHandle handle)
	{
		if (_metadataKind == MetadataKind.Ecma335)
		{
			return (uint)handle.RowId;
		}
		return CalculateTypeRefTreatmentAndRowId(handle);
	}

	public ExportedType GetExportedType(ExportedTypeHandle handle)
	{
		return new ExportedType(this, handle.RowId);
	}

	public CustomAttributeHandleCollection GetCustomAttributes(EntityHandle handle)
	{
		return new CustomAttributeHandleCollection(this, handle);
	}

	public CustomAttribute GetCustomAttribute(CustomAttributeHandle handle)
	{
		return new CustomAttribute(this, GetCustomAttributeTreatmentAndRowId(handle));
	}

	private uint GetCustomAttributeTreatmentAndRowId(CustomAttributeHandle handle)
	{
		if (_metadataKind == MetadataKind.Ecma335)
		{
			return (uint)handle.RowId;
		}
		return TreatmentAndRowId(1, handle.RowId);
	}

	public DeclarativeSecurityAttribute GetDeclarativeSecurityAttribute(DeclarativeSecurityAttributeHandle handle)
	{
		return new DeclarativeSecurityAttribute(this, handle.RowId);
	}

	public Constant GetConstant(ConstantHandle handle)
	{
		return new Constant(this, handle.RowId);
	}

	public MethodDefinition GetMethodDefinition(MethodDefinitionHandle handle)
	{
		return new MethodDefinition(this, GetMethodDefTreatmentAndRowId(handle));
	}

	private uint GetMethodDefTreatmentAndRowId(MethodDefinitionHandle handle)
	{
		if (_metadataKind == MetadataKind.Ecma335)
		{
			return (uint)handle.RowId;
		}
		return CalculateMethodDefTreatmentAndRowId(handle);
	}

	public FieldDefinition GetFieldDefinition(FieldDefinitionHandle handle)
	{
		return new FieldDefinition(this, GetFieldDefTreatmentAndRowId(handle));
	}

	private uint GetFieldDefTreatmentAndRowId(FieldDefinitionHandle handle)
	{
		if (_metadataKind == MetadataKind.Ecma335)
		{
			return (uint)handle.RowId;
		}
		return CalculateFieldDefTreatmentAndRowId(handle);
	}

	public PropertyDefinition GetPropertyDefinition(PropertyDefinitionHandle handle)
	{
		return new PropertyDefinition(this, handle);
	}

	public EventDefinition GetEventDefinition(EventDefinitionHandle handle)
	{
		return new EventDefinition(this, handle);
	}

	public MethodImplementation GetMethodImplementation(MethodImplementationHandle handle)
	{
		return new MethodImplementation(this, handle);
	}

	public MemberReference GetMemberReference(MemberReferenceHandle handle)
	{
		return new MemberReference(this, GetMemberRefTreatmentAndRowId(handle));
	}

	private uint GetMemberRefTreatmentAndRowId(MemberReferenceHandle handle)
	{
		if (_metadataKind == MetadataKind.Ecma335)
		{
			return (uint)handle.RowId;
		}
		return CalculateMemberRefTreatmentAndRowId(handle);
	}

	public MethodSpecification GetMethodSpecification(MethodSpecificationHandle handle)
	{
		return new MethodSpecification(this, handle);
	}

	public Parameter GetParameter(ParameterHandle handle)
	{
		return new Parameter(this, handle);
	}

	public GenericParameter GetGenericParameter(GenericParameterHandle handle)
	{
		return new GenericParameter(this, handle);
	}

	public GenericParameterConstraint GetGenericParameterConstraint(GenericParameterConstraintHandle handle)
	{
		return new GenericParameterConstraint(this, handle);
	}

	public ManifestResource GetManifestResource(ManifestResourceHandle handle)
	{
		return new ManifestResource(this, handle);
	}

	public AssemblyFile GetAssemblyFile(AssemblyFileHandle handle)
	{
		return new AssemblyFile(this, handle);
	}

	public StandaloneSignature GetStandaloneSignature(StandaloneSignatureHandle handle)
	{
		return new StandaloneSignature(this, handle);
	}

	public TypeSpecification GetTypeSpecification(TypeSpecificationHandle handle)
	{
		return new TypeSpecification(this, handle);
	}

	public ModuleReference GetModuleReference(ModuleReferenceHandle handle)
	{
		return new ModuleReference(this, handle);
	}

	public InterfaceImplementation GetInterfaceImplementation(InterfaceImplementationHandle handle)
	{
		return new InterfaceImplementation(this, handle);
	}

	internal TypeDefinitionHandle GetDeclaringType(MethodDefinitionHandle methodDef)
	{
		int methodDefOrPtrRowId = ((!UseMethodPtrTable) ? methodDef.RowId : MethodPtrTable.GetRowIdForMethodDefRow(methodDef.RowId));
		return TypeDefTable.FindTypeContainingMethod(methodDefOrPtrRowId, MethodDefTable.NumberOfRows);
	}

	internal TypeDefinitionHandle GetDeclaringType(FieldDefinitionHandle fieldDef)
	{
		int fieldDefOrPtrRowId = ((!UseFieldPtrTable) ? fieldDef.RowId : FieldPtrTable.GetRowIdForFieldDefRow(fieldDef.RowId));
		return TypeDefTable.FindTypeContainingField(fieldDefOrPtrRowId, FieldTable.NumberOfRows);
	}

	public string GetString(DocumentNameBlobHandle handle)
	{
		return BlobHeap.GetDocumentName(handle);
	}

	public Document GetDocument(DocumentHandle handle)
	{
		return new Document(this, handle);
	}

	public MethodDebugInformation GetMethodDebugInformation(MethodDebugInformationHandle handle)
	{
		return new MethodDebugInformation(this, handle);
	}

	public MethodDebugInformation GetMethodDebugInformation(MethodDefinitionHandle handle)
	{
		return new MethodDebugInformation(this, MethodDebugInformationHandle.FromRowId(handle.RowId));
	}

	public LocalScope GetLocalScope(LocalScopeHandle handle)
	{
		return new LocalScope(this, handle);
	}

	public LocalVariable GetLocalVariable(LocalVariableHandle handle)
	{
		return new LocalVariable(this, handle);
	}

	public LocalConstant GetLocalConstant(LocalConstantHandle handle)
	{
		return new LocalConstant(this, handle);
	}

	public ImportScope GetImportScope(ImportScopeHandle handle)
	{
		return new ImportScope(this, handle);
	}

	public CustomDebugInformation GetCustomDebugInformation(CustomDebugInformationHandle handle)
	{
		return new CustomDebugInformation(this, handle);
	}

	public CustomDebugInformationHandleCollection GetCustomDebugInformation(EntityHandle handle)
	{
		return new CustomDebugInformationHandleCollection(this, handle);
	}

	public LocalScopeHandleCollection GetLocalScopes(MethodDefinitionHandle handle)
	{
		return new LocalScopeHandleCollection(this, handle.RowId);
	}

	public LocalScopeHandleCollection GetLocalScopes(MethodDebugInformationHandle handle)
	{
		return new LocalScopeHandleCollection(this, handle.RowId);
	}

	private void InitializeNestedTypesMap()
	{
		Dictionary<TypeDefinitionHandle, ImmutableArray<TypeDefinitionHandle>.Builder> dictionary = new Dictionary<TypeDefinitionHandle, ImmutableArray<TypeDefinitionHandle>.Builder>();
		int numberOfRows = NestedClassTable.NumberOfRows;
		ImmutableArray<TypeDefinitionHandle>.Builder value = null;
		TypeDefinitionHandle typeDefinitionHandle = default(TypeDefinitionHandle);
		for (int i = 1; i <= numberOfRows; i++)
		{
			TypeDefinitionHandle enclosingClass = NestedClassTable.GetEnclosingClass(i);
			if (enclosingClass != typeDefinitionHandle)
			{
				if (!dictionary.TryGetValue(enclosingClass, out value))
				{
					value = ImmutableArray.CreateBuilder<TypeDefinitionHandle>();
					dictionary.Add(enclosingClass, value);
				}
				typeDefinitionHandle = enclosingClass;
			}
			value.Add(NestedClassTable.GetNestedClass(i));
		}
		Dictionary<TypeDefinitionHandle, ImmutableArray<TypeDefinitionHandle>> dictionary2 = new Dictionary<TypeDefinitionHandle, ImmutableArray<TypeDefinitionHandle>>();
		foreach (KeyValuePair<TypeDefinitionHandle, ImmutableArray<TypeDefinitionHandle>.Builder> item in dictionary)
		{
			dictionary2.Add(item.Key, item.Value.ToImmutable());
		}
		_lazyNestedTypesMap = dictionary2;
	}

	internal ImmutableArray<TypeDefinitionHandle> GetNestedTypes(TypeDefinitionHandle typeDef)
	{
		if (_lazyNestedTypesMap == null)
		{
			InitializeNestedTypesMap();
		}
		if (_lazyNestedTypesMap.TryGetValue(typeDef, out var value))
		{
			return value;
		}
		return ImmutableArray<TypeDefinitionHandle>.Empty;
	}

	private TypeDefTreatment GetWellKnownTypeDefinitionTreatment(TypeDefinitionHandle typeDef)
	{
		InitializeProjectedTypes();
		StringHandle name = TypeDefTable.GetName(typeDef);
		int num = StringHeap.BinarySearchRaw(s_projectedTypeNames, name);
		if (num < 0)
		{
			return TypeDefTreatment.None;
		}
		StringHandle @namespace = TypeDefTable.GetNamespace(typeDef);
		if (StringHeap.EqualsRaw(@namespace, StringHeap.GetVirtualString(s_projectionInfos[num].ClrNamespace)))
		{
			return s_projectionInfos[num].Treatment;
		}
		if (StringHeap.EqualsRaw(@namespace, s_projectionInfos[num].WinRTNamespace))
		{
			return s_projectionInfos[num].Treatment | TypeDefTreatment.MarkInternalFlag;
		}
		return TypeDefTreatment.None;
	}

	private int GetProjectionIndexForTypeReference(TypeReferenceHandle typeRef, out bool isIDisposable)
	{
		InitializeProjectedTypes();
		int num = StringHeap.BinarySearchRaw(s_projectedTypeNames, TypeRefTable.GetName(typeRef));
		if (num >= 0 && StringHeap.EqualsRaw(TypeRefTable.GetNamespace(typeRef), s_projectionInfos[num].WinRTNamespace))
		{
			isIDisposable = s_projectionInfos[num].IsIDisposable;
			return num;
		}
		isIDisposable = false;
		return -1;
	}

	internal static AssemblyReferenceHandle GetProjectedAssemblyRef(int projectionIndex)
	{
		return AssemblyReferenceHandle.FromVirtualIndex(s_projectionInfos[projectionIndex].AssemblyRef);
	}

	internal static StringHandle GetProjectedName(int projectionIndex)
	{
		return StringHandle.FromVirtualIndex(s_projectionInfos[projectionIndex].ClrName);
	}

	internal static StringHandle GetProjectedNamespace(int projectionIndex)
	{
		return StringHandle.FromVirtualIndex(s_projectionInfos[projectionIndex].ClrNamespace);
	}

	internal static TypeRefSignatureTreatment GetProjectedSignatureTreatment(int projectionIndex)
	{
		return s_projectionInfos[projectionIndex].SignatureTreatment;
	}

	private static void InitializeProjectedTypes()
	{
		if (s_projectedTypeNames == null || s_projectionInfos == null)
		{
			AssemblyReferenceHandle.VirtualIndex clrAssembly = AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime;
			AssemblyReferenceHandle.VirtualIndex clrAssembly2 = AssemblyReferenceHandle.VirtualIndex.System_Runtime;
			AssemblyReferenceHandle.VirtualIndex clrAssembly3 = AssemblyReferenceHandle.VirtualIndex.System_ObjectModel;
			AssemblyReferenceHandle.VirtualIndex clrAssembly4 = AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime_UI_Xaml;
			AssemblyReferenceHandle.VirtualIndex clrAssembly5 = AssemblyReferenceHandle.VirtualIndex.System_Runtime_InteropServices_WindowsRuntime;
			AssemblyReferenceHandle.VirtualIndex clrAssembly6 = AssemblyReferenceHandle.VirtualIndex.System_Numerics_Vectors;
			string[] array = new string[50];
			ProjectionInfo[] array2 = new ProjectionInfo[50];
			int num = 0;
			int num2 = 0;
			array[num++] = "AttributeTargets";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Metadata", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.AttributeTargets, clrAssembly2);
			array[num++] = "AttributeUsageAttribute";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Metadata", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.AttributeUsageAttribute, clrAssembly2, TypeDefTreatment.RedirectedToClrAttribute);
			array[num++] = "Color";
			array2[num2++] = new ProjectionInfo("Windows.UI", StringHandle.VirtualIndex.Windows_UI, StringHandle.VirtualIndex.Color, clrAssembly);
			array[num++] = "CornerRadius";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.CornerRadius, clrAssembly4);
			array[num++] = "DateTime";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.DateTimeOffset, clrAssembly2);
			array[num++] = "Duration";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.Duration, clrAssembly4);
			array[num++] = "DurationType";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.DurationType, clrAssembly4);
			array[num++] = "EventHandler`1";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.EventHandler1, clrAssembly2);
			array[num++] = "EventRegistrationToken";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System_Runtime_InteropServices_WindowsRuntime, StringHandle.VirtualIndex.EventRegistrationToken, clrAssembly5);
			array[num++] = "GeneratorPosition";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Controls.Primitives", StringHandle.VirtualIndex.Windows_UI_Xaml_Controls_Primitives, StringHandle.VirtualIndex.GeneratorPosition, clrAssembly4);
			array[num++] = "GridLength";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.GridLength, clrAssembly4);
			array[num++] = "GridUnitType";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.GridUnitType, clrAssembly4);
			array[num++] = "HResult";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.Exception, clrAssembly2, TypeDefTreatment.RedirectedToClrType, TypeRefSignatureTreatment.ProjectedToClass);
			array[num++] = "IBindableIterable";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections, StringHandle.VirtualIndex.IEnumerable, clrAssembly2);
			array[num++] = "IBindableVector";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections, StringHandle.VirtualIndex.IList, clrAssembly2);
			array[num++] = "IClosable";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.IDisposable, clrAssembly2, TypeDefTreatment.RedirectedToClrType, TypeRefSignatureTreatment.None, isIDisposable: true);
			array[num++] = "ICommand";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Input", StringHandle.VirtualIndex.System_Windows_Input, StringHandle.VirtualIndex.ICommand, clrAssembly3);
			array[num++] = "IIterable`1";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IEnumerable1, clrAssembly2);
			array[num++] = "IKeyValuePair`2";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.KeyValuePair2, clrAssembly2, TypeDefTreatment.RedirectedToClrType, TypeRefSignatureTreatment.ProjectedToValueType);
			array[num++] = "IMapView`2";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IReadOnlyDictionary2, clrAssembly2);
			array[num++] = "IMap`2";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IDictionary2, clrAssembly2);
			array[num++] = "INotifyCollectionChanged";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections_Specialized, StringHandle.VirtualIndex.INotifyCollectionChanged, clrAssembly3);
			array[num++] = "INotifyPropertyChanged";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Data", StringHandle.VirtualIndex.System_ComponentModel, StringHandle.VirtualIndex.INotifyPropertyChanged, clrAssembly3);
			array[num++] = "IReference`1";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.Nullable1, clrAssembly2, TypeDefTreatment.RedirectedToClrType, TypeRefSignatureTreatment.ProjectedToValueType);
			array[num++] = "IVectorView`1";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IReadOnlyList1, clrAssembly2);
			array[num++] = "IVector`1";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IList1, clrAssembly2);
			array[num++] = "KeyTime";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Media.Animation", StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Animation, StringHandle.VirtualIndex.KeyTime, clrAssembly4);
			array[num++] = "Matrix";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Media", StringHandle.VirtualIndex.Windows_UI_Xaml_Media, StringHandle.VirtualIndex.Matrix, clrAssembly4);
			array[num++] = "Matrix3D";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Media.Media3D", StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Media3D, StringHandle.VirtualIndex.Matrix3D, clrAssembly4);
			array[num++] = "Matrix3x2";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Matrix3x2, clrAssembly6);
			array[num++] = "Matrix4x4";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Matrix4x4, clrAssembly6);
			array[num++] = "NotifyCollectionChangedAction";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections_Specialized, StringHandle.VirtualIndex.NotifyCollectionChangedAction, clrAssembly3);
			array[num++] = "NotifyCollectionChangedEventArgs";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections_Specialized, StringHandle.VirtualIndex.NotifyCollectionChangedEventArgs, clrAssembly3);
			array[num++] = "NotifyCollectionChangedEventHandler";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections_Specialized, StringHandle.VirtualIndex.NotifyCollectionChangedEventHandler, clrAssembly3);
			array[num++] = "Plane";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Plane, clrAssembly6);
			array[num++] = "Point";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.Windows_Foundation, StringHandle.VirtualIndex.Point, clrAssembly);
			array[num++] = "PropertyChangedEventArgs";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Data", StringHandle.VirtualIndex.System_ComponentModel, StringHandle.VirtualIndex.PropertyChangedEventArgs, clrAssembly3);
			array[num++] = "PropertyChangedEventHandler";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Data", StringHandle.VirtualIndex.System_ComponentModel, StringHandle.VirtualIndex.PropertyChangedEventHandler, clrAssembly3);
			array[num++] = "Quaternion";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Quaternion, clrAssembly6);
			array[num++] = "Rect";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.Windows_Foundation, StringHandle.VirtualIndex.Rect, clrAssembly);
			array[num++] = "RepeatBehavior";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Media.Animation", StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Animation, StringHandle.VirtualIndex.RepeatBehavior, clrAssembly4);
			array[num++] = "RepeatBehaviorType";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Media.Animation", StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Animation, StringHandle.VirtualIndex.RepeatBehaviorType, clrAssembly4);
			array[num++] = "Size";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.Windows_Foundation, StringHandle.VirtualIndex.Size, clrAssembly);
			array[num++] = "Thickness";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.Thickness, clrAssembly4);
			array[num++] = "TimeSpan";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.TimeSpan, clrAssembly2);
			array[num++] = "TypeName";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.Type, clrAssembly2, TypeDefTreatment.RedirectedToClrType, TypeRefSignatureTreatment.ProjectedToClass);
			array[num++] = "Uri";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.Uri, clrAssembly2);
			array[num++] = "Vector2";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Vector2, clrAssembly6);
			array[num++] = "Vector3";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Vector3, clrAssembly6);
			array[num++] = "Vector4";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Vector4, clrAssembly6);
			s_projectedTypeNames = array;
			s_projectionInfos = array2;
		}
	}

	internal static string[] GetProjectedTypeNames()
	{
		InitializeProjectedTypes();
		return s_projectedTypeNames;
	}

	private static uint TreatmentAndRowId(byte treatment, int rowId)
	{
		return (uint)((treatment << 24) | rowId);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal uint CalculateTypeDefTreatmentAndRowId(TypeDefinitionHandle handle)
	{
		TypeAttributes flags = TypeDefTable.GetFlags(handle);
		EntityHandle extends = TypeDefTable.GetExtends(handle);
		TypeDefTreatment typeDefTreatment;
		if ((flags & TypeAttributes.WindowsRuntime) == 0)
		{
			typeDefTreatment = ((_metadataKind == MetadataKind.ManagedWindowsMetadata && IsClrImplementationType(handle)) ? TypeDefTreatment.UnmangleWinRTName : TypeDefTreatment.None);
		}
		else
		{
			if (_metadataKind != MetadataKind.WindowsMetadata)
			{
				typeDefTreatment = ((_metadataKind == MetadataKind.ManagedWindowsMetadata && NeedsWinRTPrefix(flags, extends)) ? TypeDefTreatment.PrefixWinRTName : TypeDefTreatment.None);
			}
			else
			{
				typeDefTreatment = GetWellKnownTypeDefinitionTreatment(handle);
				if (typeDefTreatment != 0)
				{
					return TreatmentAndRowId((byte)typeDefTreatment, handle.RowId);
				}
				typeDefTreatment = ((extends.Kind != HandleKind.TypeReference || !IsSystemAttribute((TypeReferenceHandle)extends)) ? TypeDefTreatment.NormalNonAttribute : TypeDefTreatment.NormalAttribute);
			}
			if ((typeDefTreatment == TypeDefTreatment.PrefixWinRTName || typeDefTreatment == TypeDefTreatment.NormalNonAttribute) && (flags & TypeAttributes.ClassSemanticsMask) == 0 && HasAttribute(handle, "Windows.UI.Xaml", "TreatAsAbstractComposableClassAttribute"))
			{
				typeDefTreatment |= TypeDefTreatment.MarkAbstractFlag;
			}
		}
		return TreatmentAndRowId((byte)typeDefTreatment, handle.RowId);
	}

	private bool IsClrImplementationType(TypeDefinitionHandle typeDef)
	{
		TypeAttributes flags = TypeDefTable.GetFlags(typeDef);
		if ((flags & (TypeAttributes.VisibilityMask | TypeAttributes.SpecialName)) != TypeAttributes.SpecialName)
		{
			return false;
		}
		return StringHeap.StartsWithRaw(TypeDefTable.GetName(typeDef), "<CLR>");
	}

	internal uint CalculateTypeRefTreatmentAndRowId(TypeReferenceHandle handle)
	{
		bool isIDisposable;
		int projectionIndexForTypeReference = GetProjectionIndexForTypeReference(handle, out isIDisposable);
		if (projectionIndexForTypeReference >= 0)
		{
			return TreatmentAndRowId(3, projectionIndexForTypeReference);
		}
		return TreatmentAndRowId((byte)GetSpecialTypeRefTreatment(handle), handle.RowId);
	}

	private TypeRefTreatment GetSpecialTypeRefTreatment(TypeReferenceHandle handle)
	{
		if (StringHeap.EqualsRaw(TypeRefTable.GetNamespace(handle), "System"))
		{
			StringHandle name = TypeRefTable.GetName(handle);
			if (StringHeap.EqualsRaw(name, "MulticastDelegate"))
			{
				return TypeRefTreatment.SystemDelegate;
			}
			if (StringHeap.EqualsRaw(name, "Attribute"))
			{
				return TypeRefTreatment.SystemAttribute;
			}
		}
		return TypeRefTreatment.None;
	}

	private bool IsSystemAttribute(TypeReferenceHandle handle)
	{
		if (StringHeap.EqualsRaw(TypeRefTable.GetNamespace(handle), "System"))
		{
			return StringHeap.EqualsRaw(TypeRefTable.GetName(handle), "Attribute");
		}
		return false;
	}

	private bool NeedsWinRTPrefix(TypeAttributes flags, EntityHandle extends)
	{
		if ((flags & (TypeAttributes.VisibilityMask | TypeAttributes.ClassSemanticsMask)) != TypeAttributes.Public)
		{
			return false;
		}
		if (extends.Kind != HandleKind.TypeReference)
		{
			return false;
		}
		TypeReferenceHandle handle = (TypeReferenceHandle)extends;
		if (StringHeap.EqualsRaw(TypeRefTable.GetNamespace(handle), "System"))
		{
			StringHandle name = TypeRefTable.GetName(handle);
			if (StringHeap.EqualsRaw(name, "MulticastDelegate") || StringHeap.EqualsRaw(name, "ValueType") || StringHeap.EqualsRaw(name, "Attribute"))
			{
				return false;
			}
		}
		return true;
	}

	private uint CalculateMethodDefTreatmentAndRowId(MethodDefinitionHandle methodDef)
	{
		MethodDefTreatment methodDefTreatment = MethodDefTreatment.Implementation;
		TypeDefinitionHandle declaringType = GetDeclaringType(methodDef);
		TypeAttributes flags = TypeDefTable.GetFlags(declaringType);
		if ((flags & TypeAttributes.WindowsRuntime) != 0)
		{
			if (IsClrImplementationType(declaringType))
			{
				methodDefTreatment = MethodDefTreatment.Implementation;
			}
			else if (flags.IsNested())
			{
				methodDefTreatment = MethodDefTreatment.Implementation;
			}
			else if ((flags & TypeAttributes.ClassSemanticsMask) != 0)
			{
				methodDefTreatment = MethodDefTreatment.InterfaceMethod;
			}
			else if (_metadataKind == MetadataKind.ManagedWindowsMetadata && (flags & TypeAttributes.Public) == 0)
			{
				methodDefTreatment = MethodDefTreatment.Implementation;
			}
			else
			{
				methodDefTreatment = MethodDefTreatment.Other;
				EntityHandle extends = TypeDefTable.GetExtends(declaringType);
				if (extends.Kind == HandleKind.TypeReference)
				{
					switch (GetSpecialTypeRefTreatment((TypeReferenceHandle)extends))
					{
					case TypeRefTreatment.SystemAttribute:
						methodDefTreatment = MethodDefTreatment.AttributeMethod;
						break;
					case TypeRefTreatment.SystemDelegate:
						methodDefTreatment = MethodDefTreatment.DelegateMethod | MethodDefTreatment.MarkPublicFlag;
						break;
					}
				}
			}
		}
		if (methodDefTreatment == MethodDefTreatment.Other)
		{
			bool flag = false;
			bool flag2 = false;
			bool isIDisposable = false;
			foreach (MethodImplementationHandle item in new MethodImplementationHandleCollection(this, declaringType))
			{
				MethodImplementation methodImplementation = GetMethodImplementation(item);
				if (!(methodImplementation.MethodBody == methodDef))
				{
					continue;
				}
				EntityHandle methodDeclaration = methodImplementation.MethodDeclaration;
				if (methodDeclaration.Kind == HandleKind.MemberReference && ImplementsRedirectedInterface((MemberReferenceHandle)methodDeclaration, out isIDisposable))
				{
					flag = true;
					if (isIDisposable)
					{
						break;
					}
				}
				else
				{
					flag2 = true;
				}
			}
			if (isIDisposable)
			{
				methodDefTreatment = MethodDefTreatment.DisposeMethod;
			}
			else if (flag && !flag2)
			{
				methodDefTreatment = MethodDefTreatment.HiddenInterfaceImplementation;
			}
		}
		if (methodDefTreatment == MethodDefTreatment.Other)
		{
			methodDefTreatment |= GetMethodTreatmentFromCustomAttributes(methodDef);
		}
		return TreatmentAndRowId((byte)methodDefTreatment, methodDef.RowId);
	}

	private MethodDefTreatment GetMethodTreatmentFromCustomAttributes(MethodDefinitionHandle methodDef)
	{
		MethodDefTreatment methodDefTreatment = MethodDefTreatment.None;
		foreach (CustomAttributeHandle customAttribute in GetCustomAttributes(methodDef))
		{
			if (GetAttributeTypeNameRaw(customAttribute, out var namespaceName, out var typeName) && StringHeap.EqualsRaw(namespaceName, "Windows.UI.Xaml"))
			{
				if (StringHeap.EqualsRaw(typeName, "TreatAsPublicMethodAttribute"))
				{
					methodDefTreatment |= MethodDefTreatment.MarkPublicFlag;
				}
				if (StringHeap.EqualsRaw(typeName, "TreatAsAbstractMethodAttribute"))
				{
					methodDefTreatment |= MethodDefTreatment.MarkAbstractFlag;
				}
			}
		}
		return methodDefTreatment;
	}

	private uint CalculateFieldDefTreatmentAndRowId(FieldDefinitionHandle handle)
	{
		FieldAttributes flags = FieldTable.GetFlags(handle);
		FieldDefTreatment treatment = FieldDefTreatment.None;
		if ((flags & FieldAttributes.RTSpecialName) != 0 && StringHeap.EqualsRaw(FieldTable.GetName(handle), "value__"))
		{
			TypeDefinitionHandle declaringType = GetDeclaringType(handle);
			EntityHandle extends = TypeDefTable.GetExtends(declaringType);
			if (extends.Kind == HandleKind.TypeReference)
			{
				TypeReferenceHandle handle2 = (TypeReferenceHandle)extends;
				if (StringHeap.EqualsRaw(TypeRefTable.GetName(handle2), "Enum") && StringHeap.EqualsRaw(TypeRefTable.GetNamespace(handle2), "System"))
				{
					treatment = FieldDefTreatment.EnumValue;
				}
			}
		}
		return TreatmentAndRowId((byte)treatment, handle.RowId);
	}

	private uint CalculateMemberRefTreatmentAndRowId(MemberReferenceHandle handle)
	{
		bool isIDisposable;
		MemberRefTreatment treatment = ((ImplementsRedirectedInterface(handle, out isIDisposable) && isIDisposable) ? MemberRefTreatment.Dispose : MemberRefTreatment.None);
		return TreatmentAndRowId((byte)treatment, handle.RowId);
	}

	private bool ImplementsRedirectedInterface(MemberReferenceHandle memberRef, out bool isIDisposable)
	{
		isIDisposable = false;
		EntityHandle @class = MemberRefTable.GetClass(memberRef);
		TypeReferenceHandle typeRef;
		if (@class.Kind == HandleKind.TypeReference)
		{
			typeRef = (TypeReferenceHandle)@class;
		}
		else
		{
			if (@class.Kind != HandleKind.TypeSpecification)
			{
				return false;
			}
			BlobHandle signature = TypeSpecTable.GetSignature((TypeSpecificationHandle)@class);
			BlobReader blobReader = new BlobReader(BlobHeap.GetMemoryBlock(signature));
			if (blobReader.Length < 2 || blobReader.ReadByte() != 21 || blobReader.ReadByte() != 18)
			{
				return false;
			}
			EntityHandle entityHandle = blobReader.ReadTypeHandle();
			if (entityHandle.Kind != HandleKind.TypeReference)
			{
				return false;
			}
			typeRef = (TypeReferenceHandle)entityHandle;
		}
		return GetProjectionIndexForTypeReference(typeRef, out isIDisposable) >= 0;
	}

	private int FindMscorlibAssemblyRefNoProjection()
	{
		for (int i = 1; i <= AssemblyRefTable.NumberOfNonVirtualRows; i++)
		{
			if (StringHeap.EqualsRaw(AssemblyRefTable.GetName(i), "mscorlib"))
			{
				return i;
			}
		}
		throw new BadImageFormatException(System.SR.WinMDMissingMscorlibRef);
	}

	internal CustomAttributeValueTreatment CalculateCustomAttributeValueTreatment(CustomAttributeHandle handle)
	{
		EntityHandle parent = CustomAttributeTable.GetParent(handle);
		if (!IsWindowsAttributeUsageAttribute(parent, handle))
		{
			return CustomAttributeValueTreatment.None;
		}
		TypeDefinitionHandle typeDefinitionHandle = (TypeDefinitionHandle)parent;
		if (StringHeap.EqualsRaw(TypeDefTable.GetNamespace(typeDefinitionHandle), "Windows.Foundation.Metadata"))
		{
			if (StringHeap.EqualsRaw(TypeDefTable.GetName(typeDefinitionHandle), "VersionAttribute"))
			{
				return CustomAttributeValueTreatment.AttributeUsageVersionAttribute;
			}
			if (StringHeap.EqualsRaw(TypeDefTable.GetName(typeDefinitionHandle), "DeprecatedAttribute"))
			{
				return CustomAttributeValueTreatment.AttributeUsageDeprecatedAttribute;
			}
		}
		if (!HasAttribute(typeDefinitionHandle, "Windows.Foundation.Metadata", "AllowMultipleAttribute"))
		{
			return CustomAttributeValueTreatment.AttributeUsageAllowSingle;
		}
		return CustomAttributeValueTreatment.AttributeUsageAllowMultiple;
	}

	private bool IsWindowsAttributeUsageAttribute(EntityHandle targetType, CustomAttributeHandle attributeHandle)
	{
		if (targetType.Kind != HandleKind.TypeDefinition)
		{
			return false;
		}
		EntityHandle constructor = CustomAttributeTable.GetConstructor(attributeHandle);
		if (constructor.Kind != HandleKind.MemberReference)
		{
			return false;
		}
		EntityHandle @class = MemberRefTable.GetClass((MemberReferenceHandle)constructor);
		if (@class.Kind != HandleKind.TypeReference)
		{
			return false;
		}
		TypeReferenceHandle handle = (TypeReferenceHandle)@class;
		if (StringHeap.EqualsRaw(TypeRefTable.GetName(handle), "AttributeUsageAttribute"))
		{
			return StringHeap.EqualsRaw(TypeRefTable.GetNamespace(handle), "Windows.Foundation.Metadata");
		}
		return false;
	}

	private bool HasAttribute(EntityHandle token, string asciiNamespaceName, string asciiTypeName)
	{
		foreach (CustomAttributeHandle customAttribute in GetCustomAttributes(token))
		{
			if (GetAttributeTypeNameRaw(customAttribute, out var namespaceName, out var typeName) && StringHeap.EqualsRaw(typeName, asciiTypeName) && StringHeap.EqualsRaw(namespaceName, asciiNamespaceName))
			{
				return true;
			}
		}
		return false;
	}

	private bool GetAttributeTypeNameRaw(CustomAttributeHandle caHandle, out StringHandle namespaceName, out StringHandle typeName)
	{
		namespaceName = (typeName = default(StringHandle));
		EntityHandle attributeTypeRaw = GetAttributeTypeRaw(caHandle);
		if (attributeTypeRaw.IsNil)
		{
			return false;
		}
		if (attributeTypeRaw.Kind == HandleKind.TypeReference)
		{
			TypeReferenceHandle handle = (TypeReferenceHandle)attributeTypeRaw;
			EntityHandle resolutionScope = TypeRefTable.GetResolutionScope(handle);
			if (!resolutionScope.IsNil && resolutionScope.Kind == HandleKind.TypeReference)
			{
				return false;
			}
			typeName = TypeRefTable.GetName(handle);
			namespaceName = TypeRefTable.GetNamespace(handle);
		}
		else
		{
			if (attributeTypeRaw.Kind != HandleKind.TypeDefinition)
			{
				return false;
			}
			TypeDefinitionHandle handle2 = (TypeDefinitionHandle)attributeTypeRaw;
			if (TypeDefTable.GetFlags(handle2).IsNested())
			{
				return false;
			}
			typeName = TypeDefTable.GetName(handle2);
			namespaceName = TypeDefTable.GetNamespace(handle2);
		}
		return true;
	}

	private EntityHandle GetAttributeTypeRaw(CustomAttributeHandle handle)
	{
		EntityHandle constructor = CustomAttributeTable.GetConstructor(handle);
		if (constructor.Kind == HandleKind.MethodDefinition)
		{
			return GetDeclaringType((MethodDefinitionHandle)constructor);
		}
		if (constructor.Kind == HandleKind.MemberReference)
		{
			EntityHandle @class = MemberRefTable.GetClass((MemberReferenceHandle)constructor);
			HandleKind kind = @class.Kind;
			if (kind == HandleKind.TypeReference || kind == HandleKind.TypeDefinition)
			{
				return @class;
			}
		}
		return default(EntityHandle);
	}
}
