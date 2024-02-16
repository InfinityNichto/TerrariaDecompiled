using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

public sealed class MetadataBuilder
{
	private struct AssemblyRefTableRow
	{
		public Version Version;

		public BlobHandle PublicKeyToken;

		public StringHandle Name;

		public StringHandle Culture;

		public uint Flags;

		public BlobHandle HashValue;
	}

	private struct ModuleRow
	{
		public ushort Generation;

		public StringHandle Name;

		public GuidHandle ModuleVersionId;

		public GuidHandle EncId;

		public GuidHandle EncBaseId;
	}

	private struct AssemblyRow
	{
		public uint HashAlgorithm;

		public Version Version;

		public ushort Flags;

		public BlobHandle AssemblyKey;

		public StringHandle AssemblyName;

		public StringHandle AssemblyCulture;
	}

	private struct ClassLayoutRow
	{
		public ushort PackingSize;

		public uint ClassSize;

		public int Parent;
	}

	private struct ConstantRow
	{
		public byte Type;

		public int Parent;

		public BlobHandle Value;
	}

	private struct CustomAttributeRow
	{
		public int Parent;

		public int Type;

		public BlobHandle Value;
	}

	private struct DeclSecurityRow
	{
		public ushort Action;

		public int Parent;

		public BlobHandle PermissionSet;
	}

	private struct EncLogRow
	{
		public int Token;

		public byte FuncCode;
	}

	private struct EncMapRow
	{
		public int Token;
	}

	private struct EventRow
	{
		public ushort EventFlags;

		public StringHandle Name;

		public int EventType;
	}

	private struct EventMapRow
	{
		public int Parent;

		public int EventList;
	}

	private struct ExportedTypeRow
	{
		public uint Flags;

		public int TypeDefId;

		public StringHandle TypeName;

		public StringHandle TypeNamespace;

		public int Implementation;
	}

	private struct FieldLayoutRow
	{
		public int Offset;

		public int Field;
	}

	private struct FieldMarshalRow
	{
		public int Parent;

		public BlobHandle NativeType;
	}

	private struct FieldRvaRow
	{
		public int Offset;

		public int Field;
	}

	private struct FieldDefRow
	{
		public ushort Flags;

		public StringHandle Name;

		public BlobHandle Signature;
	}

	private struct FileTableRow
	{
		public uint Flags;

		public StringHandle FileName;

		public BlobHandle HashValue;
	}

	private struct GenericParamConstraintRow
	{
		public int Owner;

		public int Constraint;
	}

	private struct GenericParamRow
	{
		public ushort Number;

		public ushort Flags;

		public int Owner;

		public StringHandle Name;
	}

	private struct ImplMapRow
	{
		public ushort MappingFlags;

		public int MemberForwarded;

		public StringHandle ImportName;

		public int ImportScope;
	}

	private struct InterfaceImplRow
	{
		public int Class;

		public int Interface;
	}

	private struct ManifestResourceRow
	{
		public uint Offset;

		public uint Flags;

		public StringHandle Name;

		public int Implementation;
	}

	private struct MemberRefRow
	{
		public int Class;

		public StringHandle Name;

		public BlobHandle Signature;
	}

	private struct MethodImplRow
	{
		public int Class;

		public int MethodBody;

		public int MethodDecl;
	}

	private struct MethodSemanticsRow
	{
		public ushort Semantic;

		public int Method;

		public int Association;
	}

	private struct MethodSpecRow
	{
		public int Method;

		public BlobHandle Instantiation;
	}

	private struct MethodRow
	{
		public int BodyOffset;

		public ushort ImplFlags;

		public ushort Flags;

		public StringHandle Name;

		public BlobHandle Signature;

		public int ParamList;
	}

	private struct ModuleRefRow
	{
		public StringHandle Name;
	}

	private struct NestedClassRow
	{
		public int NestedClass;

		public int EnclosingClass;
	}

	private struct ParamRow
	{
		public ushort Flags;

		public ushort Sequence;

		public StringHandle Name;
	}

	private struct PropertyMapRow
	{
		public int Parent;

		public int PropertyList;
	}

	private struct PropertyRow
	{
		public ushort PropFlags;

		public StringHandle Name;

		public BlobHandle Type;
	}

	private struct TypeDefRow
	{
		public uint Flags;

		public StringHandle Name;

		public StringHandle Namespace;

		public int Extends;

		public int FieldList;

		public int MethodList;
	}

	private struct TypeRefRow
	{
		public int ResolutionScope;

		public StringHandle Name;

		public StringHandle Namespace;
	}

	private struct TypeSpecRow
	{
		public BlobHandle Signature;
	}

	private struct StandaloneSigRow
	{
		public BlobHandle Signature;
	}

	private struct DocumentRow
	{
		public BlobHandle Name;

		public GuidHandle HashAlgorithm;

		public BlobHandle Hash;

		public GuidHandle Language;
	}

	private struct MethodDebugInformationRow
	{
		public int Document;

		public BlobHandle SequencePoints;
	}

	private struct LocalScopeRow
	{
		public int Method;

		public int ImportScope;

		public int VariableList;

		public int ConstantList;

		public int StartOffset;

		public int Length;
	}

	private struct LocalVariableRow
	{
		public ushort Attributes;

		public ushort Index;

		public StringHandle Name;
	}

	private struct LocalConstantRow
	{
		public StringHandle Name;

		public BlobHandle Signature;
	}

	private struct ImportScopeRow
	{
		public int Parent;

		public BlobHandle Imports;
	}

	private struct StateMachineMethodRow
	{
		public int MoveNextMethod;

		public int KickoffMethod;
	}

	private struct CustomDebugInformationRow
	{
		public int Parent;

		public GuidHandle Kind;

		public BlobHandle Value;
	}

	private sealed class HeapBlobBuilder : BlobBuilder
	{
		private int _capacityExpansion;

		public HeapBlobBuilder(int capacity)
			: base(capacity)
		{
		}

		protected override BlobBuilder AllocateChunk(int minimalSize)
		{
			return new HeapBlobBuilder(Math.Max(Math.Max(minimalSize, base.ChunkCapacity), _capacityExpansion));
		}

		internal void SetCapacity(int capacity)
		{
			_capacityExpansion = Math.Max(0, capacity - base.Count - base.FreeBytes);
		}
	}

	private sealed class SuffixSort : IComparer<KeyValuePair<string, StringHandle>>
	{
		internal static SuffixSort Instance = new SuffixSort();

		public int Compare(KeyValuePair<string, StringHandle> xPair, KeyValuePair<string, StringHandle> yPair)
		{
			string key = xPair.Key;
			string key2 = yPair.Key;
			int num = key.Length - 1;
			int num2 = key2.Length - 1;
			while (num >= 0 && num2 >= 0)
			{
				if (key[num] < key2[num2])
				{
					return -1;
				}
				if (key[num] > key2[num2])
				{
					return 1;
				}
				num--;
				num2--;
			}
			return key2.Length.CompareTo(key.Length);
		}
	}

	private ModuleRow? _moduleRow;

	private AssemblyRow? _assemblyRow;

	private readonly List<ClassLayoutRow> _classLayoutTable = new List<ClassLayoutRow>();

	private readonly List<ConstantRow> _constantTable = new List<ConstantRow>();

	private int _constantTableLastParent;

	private bool _constantTableNeedsSorting;

	private readonly List<CustomAttributeRow> _customAttributeTable = new List<CustomAttributeRow>();

	private int _customAttributeTableLastParent;

	private bool _customAttributeTableNeedsSorting;

	private readonly List<DeclSecurityRow> _declSecurityTable = new List<DeclSecurityRow>();

	private int _declSecurityTableLastParent;

	private bool _declSecurityTableNeedsSorting;

	private readonly List<EncLogRow> _encLogTable = new List<EncLogRow>();

	private readonly List<EncMapRow> _encMapTable = new List<EncMapRow>();

	private readonly List<EventRow> _eventTable = new List<EventRow>();

	private readonly List<EventMapRow> _eventMapTable = new List<EventMapRow>();

	private readonly List<ExportedTypeRow> _exportedTypeTable = new List<ExportedTypeRow>();

	private readonly List<FieldLayoutRow> _fieldLayoutTable = new List<FieldLayoutRow>();

	private readonly List<FieldMarshalRow> _fieldMarshalTable = new List<FieldMarshalRow>();

	private int _fieldMarshalTableLastParent;

	private bool _fieldMarshalTableNeedsSorting;

	private readonly List<FieldRvaRow> _fieldRvaTable = new List<FieldRvaRow>();

	private readonly List<FieldDefRow> _fieldTable = new List<FieldDefRow>();

	private readonly List<FileTableRow> _fileTable = new List<FileTableRow>();

	private readonly List<GenericParamConstraintRow> _genericParamConstraintTable = new List<GenericParamConstraintRow>();

	private readonly List<GenericParamRow> _genericParamTable = new List<GenericParamRow>();

	private readonly List<ImplMapRow> _implMapTable = new List<ImplMapRow>();

	private readonly List<InterfaceImplRow> _interfaceImplTable = new List<InterfaceImplRow>();

	private readonly List<ManifestResourceRow> _manifestResourceTable = new List<ManifestResourceRow>();

	private readonly List<MemberRefRow> _memberRefTable = new List<MemberRefRow>();

	private readonly List<MethodImplRow> _methodImplTable = new List<MethodImplRow>();

	private readonly List<MethodSemanticsRow> _methodSemanticsTable = new List<MethodSemanticsRow>();

	private int _methodSemanticsTableLastAssociation;

	private bool _methodSemanticsTableNeedsSorting;

	private readonly List<MethodSpecRow> _methodSpecTable = new List<MethodSpecRow>();

	private readonly List<MethodRow> _methodDefTable = new List<MethodRow>();

	private readonly List<ModuleRefRow> _moduleRefTable = new List<ModuleRefRow>();

	private readonly List<NestedClassRow> _nestedClassTable = new List<NestedClassRow>();

	private readonly List<ParamRow> _paramTable = new List<ParamRow>();

	private readonly List<PropertyMapRow> _propertyMapTable = new List<PropertyMapRow>();

	private readonly List<PropertyRow> _propertyTable = new List<PropertyRow>();

	private readonly List<TypeDefRow> _typeDefTable = new List<TypeDefRow>();

	private readonly List<TypeRefRow> _typeRefTable = new List<TypeRefRow>();

	private readonly List<TypeSpecRow> _typeSpecTable = new List<TypeSpecRow>();

	private readonly List<AssemblyRefTableRow> _assemblyRefTable = new List<AssemblyRefTableRow>();

	private readonly List<StandaloneSigRow> _standAloneSigTable = new List<StandaloneSigRow>();

	private readonly List<DocumentRow> _documentTable = new List<DocumentRow>();

	private readonly List<MethodDebugInformationRow> _methodDebugInformationTable = new List<MethodDebugInformationRow>();

	private readonly List<LocalScopeRow> _localScopeTable = new List<LocalScopeRow>();

	private readonly List<LocalVariableRow> _localVariableTable = new List<LocalVariableRow>();

	private readonly List<LocalConstantRow> _localConstantTable = new List<LocalConstantRow>();

	private readonly List<ImportScopeRow> _importScopeTable = new List<ImportScopeRow>();

	private readonly List<StateMachineMethodRow> _stateMachineMethodTable = new List<StateMachineMethodRow>();

	private readonly List<CustomDebugInformationRow> _customDebugInformationTable = new List<CustomDebugInformationRow>();

	private readonly Dictionary<string, UserStringHandle> _userStrings = new Dictionary<string, UserStringHandle>(256);

	private readonly HeapBlobBuilder _userStringBuilder = new HeapBlobBuilder(4096);

	private readonly int _userStringHeapStartOffset;

	private readonly Dictionary<string, StringHandle> _strings = new Dictionary<string, StringHandle>(256);

	private readonly int _stringHeapStartOffset;

	private int _stringHeapCapacity = 4096;

	private readonly Dictionary<ImmutableArray<byte>, BlobHandle> _blobs = new Dictionary<ImmutableArray<byte>, BlobHandle>(1024, ByteSequenceComparer.Instance);

	private readonly int _blobHeapStartOffset;

	private int _blobHeapSize;

	private readonly Dictionary<Guid, GuidHandle> _guids = new Dictionary<Guid, GuidHandle>();

	private readonly HeapBlobBuilder _guidBuilder = new HeapBlobBuilder(16);

	internal SerializedMetadata GetSerializedMetadata(ImmutableArray<int> externalRowCounts, int metadataVersionByteCount, bool isStandaloneDebugMetadata)
	{
		HeapBlobBuilder heapBlobBuilder = new HeapBlobBuilder(_stringHeapCapacity);
		ImmutableArray<int> stringMap = SerializeStringHeap(heapBlobBuilder, _strings, _stringHeapStartOffset);
		ImmutableArray<int> heapSizes = ImmutableArray.Create(_userStringBuilder.Count, heapBlobBuilder.Count, _blobHeapSize, _guidBuilder.Count);
		MetadataSizes sizes = new MetadataSizes(GetRowCounts(), externalRowCounts, heapSizes, metadataVersionByteCount, isStandaloneDebugMetadata);
		return new SerializedMetadata(sizes, heapBlobBuilder, stringMap);
	}

	internal static void SerializeMetadataHeader(BlobBuilder builder, string metadataVersion, MetadataSizes sizes)
	{
		int count = builder.Count;
		builder.WriteUInt32(1112167234u);
		builder.WriteUInt16(1);
		builder.WriteUInt16(1);
		builder.WriteUInt32(0u);
		builder.WriteInt32(sizes.MetadataVersionPaddedLength);
		int count2 = builder.Count;
		builder.WriteUTF8(metadataVersion);
		builder.WriteByte(0);
		int count3 = builder.Count;
		for (int i = 0; i < sizes.MetadataVersionPaddedLength - (count3 - count2); i++)
		{
			builder.WriteByte(0);
		}
		builder.WriteUInt16(0);
		builder.WriteUInt16((ushort)(5 + (sizes.IsEncDelta ? 1 : 0) + (sizes.IsStandaloneDebugMetadata ? 1 : 0)));
		int offsetFromStartOfMetadata = sizes.MetadataHeaderSize;
		if (sizes.IsStandaloneDebugMetadata)
		{
			SerializeStreamHeader(ref offsetFromStartOfMetadata, sizes.StandalonePdbStreamSize, "#Pdb", builder);
		}
		SerializeStreamHeader(ref offsetFromStartOfMetadata, sizes.MetadataTableStreamSize, sizes.IsCompressed ? "#~" : "#-", builder);
		SerializeStreamHeader(ref offsetFromStartOfMetadata, sizes.GetAlignedHeapSize(HeapIndex.String), "#Strings", builder);
		SerializeStreamHeader(ref offsetFromStartOfMetadata, sizes.GetAlignedHeapSize(HeapIndex.UserString), "#US", builder);
		SerializeStreamHeader(ref offsetFromStartOfMetadata, sizes.GetAlignedHeapSize(HeapIndex.Guid), "#GUID", builder);
		SerializeStreamHeader(ref offsetFromStartOfMetadata, sizes.GetAlignedHeapSize(HeapIndex.Blob), "#Blob", builder);
		if (sizes.IsEncDelta)
		{
			SerializeStreamHeader(ref offsetFromStartOfMetadata, 0, "#JTD", builder);
		}
		int count4 = builder.Count;
	}

	private static void SerializeStreamHeader(ref int offsetFromStartOfMetadata, int alignedStreamSize, string streamName, BlobBuilder builder)
	{
		int metadataStreamHeaderSize = MetadataSizes.GetMetadataStreamHeaderSize(streamName);
		builder.WriteInt32(offsetFromStartOfMetadata);
		builder.WriteInt32(alignedStreamSize);
		foreach (char c in streamName)
		{
			builder.WriteByte((byte)c);
		}
		for (uint num = (uint)(8 + streamName.Length); num < metadataStreamHeaderSize; num++)
		{
			builder.WriteByte(0);
		}
		offsetFromStartOfMetadata += alignedStreamSize;
	}

	public void SetCapacity(TableIndex table, int rowCount)
	{
		if (rowCount < 0)
		{
			Throw.ArgumentOutOfRange("rowCount");
		}
		switch (table)
		{
		case TableIndex.TypeRef:
			SetTableCapacity(_typeRefTable, rowCount);
			break;
		case TableIndex.TypeDef:
			SetTableCapacity(_typeDefTable, rowCount);
			break;
		case TableIndex.Field:
			SetTableCapacity(_fieldTable, rowCount);
			break;
		case TableIndex.MethodDef:
			SetTableCapacity(_methodDefTable, rowCount);
			break;
		case TableIndex.Param:
			SetTableCapacity(_paramTable, rowCount);
			break;
		case TableIndex.InterfaceImpl:
			SetTableCapacity(_interfaceImplTable, rowCount);
			break;
		case TableIndex.MemberRef:
			SetTableCapacity(_memberRefTable, rowCount);
			break;
		case TableIndex.Constant:
			SetTableCapacity(_constantTable, rowCount);
			break;
		case TableIndex.CustomAttribute:
			SetTableCapacity(_customAttributeTable, rowCount);
			break;
		case TableIndex.FieldMarshal:
			SetTableCapacity(_fieldMarshalTable, rowCount);
			break;
		case TableIndex.DeclSecurity:
			SetTableCapacity(_declSecurityTable, rowCount);
			break;
		case TableIndex.ClassLayout:
			SetTableCapacity(_classLayoutTable, rowCount);
			break;
		case TableIndex.FieldLayout:
			SetTableCapacity(_fieldLayoutTable, rowCount);
			break;
		case TableIndex.StandAloneSig:
			SetTableCapacity(_standAloneSigTable, rowCount);
			break;
		case TableIndex.EventMap:
			SetTableCapacity(_eventMapTable, rowCount);
			break;
		case TableIndex.Event:
			SetTableCapacity(_eventTable, rowCount);
			break;
		case TableIndex.PropertyMap:
			SetTableCapacity(_propertyMapTable, rowCount);
			break;
		case TableIndex.Property:
			SetTableCapacity(_propertyTable, rowCount);
			break;
		case TableIndex.MethodSemantics:
			SetTableCapacity(_methodSemanticsTable, rowCount);
			break;
		case TableIndex.MethodImpl:
			SetTableCapacity(_methodImplTable, rowCount);
			break;
		case TableIndex.ModuleRef:
			SetTableCapacity(_moduleRefTable, rowCount);
			break;
		case TableIndex.TypeSpec:
			SetTableCapacity(_typeSpecTable, rowCount);
			break;
		case TableIndex.ImplMap:
			SetTableCapacity(_implMapTable, rowCount);
			break;
		case TableIndex.FieldRva:
			SetTableCapacity(_fieldRvaTable, rowCount);
			break;
		case TableIndex.EncLog:
			SetTableCapacity(_encLogTable, rowCount);
			break;
		case TableIndex.EncMap:
			SetTableCapacity(_encMapTable, rowCount);
			break;
		case TableIndex.AssemblyRef:
			SetTableCapacity(_assemblyRefTable, rowCount);
			break;
		case TableIndex.File:
			SetTableCapacity(_fileTable, rowCount);
			break;
		case TableIndex.ExportedType:
			SetTableCapacity(_exportedTypeTable, rowCount);
			break;
		case TableIndex.ManifestResource:
			SetTableCapacity(_manifestResourceTable, rowCount);
			break;
		case TableIndex.NestedClass:
			SetTableCapacity(_nestedClassTable, rowCount);
			break;
		case TableIndex.GenericParam:
			SetTableCapacity(_genericParamTable, rowCount);
			break;
		case TableIndex.MethodSpec:
			SetTableCapacity(_methodSpecTable, rowCount);
			break;
		case TableIndex.GenericParamConstraint:
			SetTableCapacity(_genericParamConstraintTable, rowCount);
			break;
		case TableIndex.Document:
			SetTableCapacity(_documentTable, rowCount);
			break;
		case TableIndex.MethodDebugInformation:
			SetTableCapacity(_methodDebugInformationTable, rowCount);
			break;
		case TableIndex.LocalScope:
			SetTableCapacity(_localScopeTable, rowCount);
			break;
		case TableIndex.LocalVariable:
			SetTableCapacity(_localVariableTable, rowCount);
			break;
		case TableIndex.LocalConstant:
			SetTableCapacity(_localConstantTable, rowCount);
			break;
		case TableIndex.ImportScope:
			SetTableCapacity(_importScopeTable, rowCount);
			break;
		case TableIndex.StateMachineMethod:
			SetTableCapacity(_stateMachineMethodTable, rowCount);
			break;
		case TableIndex.CustomDebugInformation:
			SetTableCapacity(_customDebugInformationTable, rowCount);
			break;
		default:
			throw new ArgumentOutOfRangeException("table");
		case TableIndex.Module:
		case TableIndex.FieldPtr:
		case TableIndex.MethodPtr:
		case TableIndex.ParamPtr:
		case TableIndex.EventPtr:
		case TableIndex.PropertyPtr:
		case TableIndex.Assembly:
		case TableIndex.AssemblyProcessor:
		case TableIndex.AssemblyOS:
		case TableIndex.AssemblyRefProcessor:
		case TableIndex.AssemblyRefOS:
			break;
		}
	}

	private static void SetTableCapacity<T>(List<T> table, int rowCount)
	{
		if (rowCount > table.Count)
		{
			table.Capacity = rowCount;
		}
	}

	public int GetRowCount(TableIndex table)
	{
		switch (table)
		{
		case TableIndex.Assembly:
			if (!_assemblyRow.HasValue)
			{
				return 0;
			}
			return 1;
		case TableIndex.AssemblyRef:
			return _assemblyRefTable.Count;
		case TableIndex.ClassLayout:
			return _classLayoutTable.Count;
		case TableIndex.Constant:
			return _constantTable.Count;
		case TableIndex.CustomAttribute:
			return _customAttributeTable.Count;
		case TableIndex.DeclSecurity:
			return _declSecurityTable.Count;
		case TableIndex.EncLog:
			return _encLogTable.Count;
		case TableIndex.EncMap:
			return _encMapTable.Count;
		case TableIndex.EventMap:
			return _eventMapTable.Count;
		case TableIndex.Event:
			return _eventTable.Count;
		case TableIndex.ExportedType:
			return _exportedTypeTable.Count;
		case TableIndex.FieldLayout:
			return _fieldLayoutTable.Count;
		case TableIndex.FieldMarshal:
			return _fieldMarshalTable.Count;
		case TableIndex.FieldRva:
			return _fieldRvaTable.Count;
		case TableIndex.Field:
			return _fieldTable.Count;
		case TableIndex.File:
			return _fileTable.Count;
		case TableIndex.GenericParamConstraint:
			return _genericParamConstraintTable.Count;
		case TableIndex.GenericParam:
			return _genericParamTable.Count;
		case TableIndex.ImplMap:
			return _implMapTable.Count;
		case TableIndex.InterfaceImpl:
			return _interfaceImplTable.Count;
		case TableIndex.ManifestResource:
			return _manifestResourceTable.Count;
		case TableIndex.MemberRef:
			return _memberRefTable.Count;
		case TableIndex.MethodImpl:
			return _methodImplTable.Count;
		case TableIndex.MethodSemantics:
			return _methodSemanticsTable.Count;
		case TableIndex.MethodSpec:
			return _methodSpecTable.Count;
		case TableIndex.MethodDef:
			return _methodDefTable.Count;
		case TableIndex.ModuleRef:
			return _moduleRefTable.Count;
		case TableIndex.Module:
			if (!_moduleRow.HasValue)
			{
				return 0;
			}
			return 1;
		case TableIndex.NestedClass:
			return _nestedClassTable.Count;
		case TableIndex.Param:
			return _paramTable.Count;
		case TableIndex.PropertyMap:
			return _propertyMapTable.Count;
		case TableIndex.Property:
			return _propertyTable.Count;
		case TableIndex.StandAloneSig:
			return _standAloneSigTable.Count;
		case TableIndex.TypeDef:
			return _typeDefTable.Count;
		case TableIndex.TypeRef:
			return _typeRefTable.Count;
		case TableIndex.TypeSpec:
			return _typeSpecTable.Count;
		case TableIndex.Document:
			return _documentTable.Count;
		case TableIndex.MethodDebugInformation:
			return _methodDebugInformationTable.Count;
		case TableIndex.LocalScope:
			return _localScopeTable.Count;
		case TableIndex.LocalVariable:
			return _localVariableTable.Count;
		case TableIndex.LocalConstant:
			return _localConstantTable.Count;
		case TableIndex.StateMachineMethod:
			return _stateMachineMethodTable.Count;
		case TableIndex.ImportScope:
			return _importScopeTable.Count;
		case TableIndex.CustomDebugInformation:
			return _customDebugInformationTable.Count;
		case TableIndex.FieldPtr:
		case TableIndex.MethodPtr:
		case TableIndex.ParamPtr:
		case TableIndex.EventPtr:
		case TableIndex.PropertyPtr:
		case TableIndex.AssemblyProcessor:
		case TableIndex.AssemblyOS:
		case TableIndex.AssemblyRefProcessor:
		case TableIndex.AssemblyRefOS:
			return 0;
		default:
			throw new ArgumentOutOfRangeException("table");
		}
	}

	public ImmutableArray<int> GetRowCounts()
	{
		ImmutableArray<int>.Builder builder = ImmutableArray.CreateBuilder<int>(MetadataTokens.TableCount);
		builder.Count = MetadataTokens.TableCount;
		builder[32] = (_assemblyRow.HasValue ? 1 : 0);
		builder[35] = _assemblyRefTable.Count;
		builder[15] = _classLayoutTable.Count;
		builder[11] = _constantTable.Count;
		builder[12] = _customAttributeTable.Count;
		builder[14] = _declSecurityTable.Count;
		builder[30] = _encLogTable.Count;
		builder[31] = _encMapTable.Count;
		builder[18] = _eventMapTable.Count;
		builder[20] = _eventTable.Count;
		builder[39] = _exportedTypeTable.Count;
		builder[16] = _fieldLayoutTable.Count;
		builder[13] = _fieldMarshalTable.Count;
		builder[29] = _fieldRvaTable.Count;
		builder[4] = _fieldTable.Count;
		builder[38] = _fileTable.Count;
		builder[44] = _genericParamConstraintTable.Count;
		builder[42] = _genericParamTable.Count;
		builder[28] = _implMapTable.Count;
		builder[9] = _interfaceImplTable.Count;
		builder[40] = _manifestResourceTable.Count;
		builder[10] = _memberRefTable.Count;
		builder[25] = _methodImplTable.Count;
		builder[24] = _methodSemanticsTable.Count;
		builder[43] = _methodSpecTable.Count;
		builder[6] = _methodDefTable.Count;
		builder[26] = _moduleRefTable.Count;
		builder[0] = (_moduleRow.HasValue ? 1 : 0);
		builder[41] = _nestedClassTable.Count;
		builder[8] = _paramTable.Count;
		builder[21] = _propertyMapTable.Count;
		builder[23] = _propertyTable.Count;
		builder[17] = _standAloneSigTable.Count;
		builder[2] = _typeDefTable.Count;
		builder[1] = _typeRefTable.Count;
		builder[27] = _typeSpecTable.Count;
		builder[48] = _documentTable.Count;
		builder[49] = _methodDebugInformationTable.Count;
		builder[50] = _localScopeTable.Count;
		builder[51] = _localVariableTable.Count;
		builder[52] = _localConstantTable.Count;
		builder[54] = _stateMachineMethodTable.Count;
		builder[53] = _importScopeTable.Count;
		builder[55] = _customDebugInformationTable.Count;
		return builder.MoveToImmutable();
	}

	public ModuleDefinitionHandle AddModule(int generation, StringHandle moduleName, GuidHandle mvid, GuidHandle encId, GuidHandle encBaseId)
	{
		if ((uint)generation > 65535u)
		{
			Throw.ArgumentOutOfRange("generation");
		}
		if (_moduleRow.HasValue)
		{
			Throw.InvalidOperation(System.SR.ModuleAlreadyAdded);
		}
		_moduleRow = new ModuleRow
		{
			Generation = (ushort)generation,
			Name = moduleName,
			ModuleVersionId = mvid,
			EncId = encId,
			EncBaseId = encBaseId
		};
		return EntityHandle.ModuleDefinition;
	}

	public AssemblyDefinitionHandle AddAssembly(StringHandle name, Version version, StringHandle culture, BlobHandle publicKey, AssemblyFlags flags, AssemblyHashAlgorithm hashAlgorithm)
	{
		if (version == null)
		{
			Throw.ArgumentNull("version");
		}
		if (_assemblyRow.HasValue)
		{
			Throw.InvalidOperation(System.SR.AssemblyAlreadyAdded);
		}
		_assemblyRow = new AssemblyRow
		{
			Flags = (ushort)flags,
			HashAlgorithm = (uint)hashAlgorithm,
			Version = version,
			AssemblyKey = publicKey,
			AssemblyName = name,
			AssemblyCulture = culture
		};
		return EntityHandle.AssemblyDefinition;
	}

	public AssemblyReferenceHandle AddAssemblyReference(StringHandle name, Version version, StringHandle culture, BlobHandle publicKeyOrToken, AssemblyFlags flags, BlobHandle hashValue)
	{
		if (version == null)
		{
			Throw.ArgumentNull("version");
		}
		_assemblyRefTable.Add(new AssemblyRefTableRow
		{
			Name = name,
			Version = version,
			Culture = culture,
			PublicKeyToken = publicKeyOrToken,
			Flags = (uint)flags,
			HashValue = hashValue
		});
		return AssemblyReferenceHandle.FromRowId(_assemblyRefTable.Count);
	}

	public TypeDefinitionHandle AddTypeDefinition(TypeAttributes attributes, StringHandle @namespace, StringHandle name, EntityHandle baseType, FieldDefinitionHandle fieldList, MethodDefinitionHandle methodList)
	{
		_typeDefTable.Add(new TypeDefRow
		{
			Flags = (uint)attributes,
			Name = name,
			Namespace = @namespace,
			Extends = ((!baseType.IsNil) ? CodedIndex.TypeDefOrRefOrSpec(baseType) : 0),
			FieldList = fieldList.RowId,
			MethodList = methodList.RowId
		});
		return TypeDefinitionHandle.FromRowId(_typeDefTable.Count);
	}

	public void AddTypeLayout(TypeDefinitionHandle type, ushort packingSize, uint size)
	{
		_classLayoutTable.Add(new ClassLayoutRow
		{
			Parent = type.RowId,
			PackingSize = packingSize,
			ClassSize = size
		});
	}

	public InterfaceImplementationHandle AddInterfaceImplementation(TypeDefinitionHandle type, EntityHandle implementedInterface)
	{
		_interfaceImplTable.Add(new InterfaceImplRow
		{
			Class = type.RowId,
			Interface = CodedIndex.TypeDefOrRefOrSpec(implementedInterface)
		});
		return InterfaceImplementationHandle.FromRowId(_interfaceImplTable.Count);
	}

	public void AddNestedType(TypeDefinitionHandle type, TypeDefinitionHandle enclosingType)
	{
		_nestedClassTable.Add(new NestedClassRow
		{
			NestedClass = type.RowId,
			EnclosingClass = enclosingType.RowId
		});
	}

	public TypeReferenceHandle AddTypeReference(EntityHandle resolutionScope, StringHandle @namespace, StringHandle name)
	{
		_typeRefTable.Add(new TypeRefRow
		{
			ResolutionScope = ((!resolutionScope.IsNil) ? CodedIndex.ResolutionScope(resolutionScope) : 0),
			Name = name,
			Namespace = @namespace
		});
		return TypeReferenceHandle.FromRowId(_typeRefTable.Count);
	}

	public TypeSpecificationHandle AddTypeSpecification(BlobHandle signature)
	{
		_typeSpecTable.Add(new TypeSpecRow
		{
			Signature = signature
		});
		return TypeSpecificationHandle.FromRowId(_typeSpecTable.Count);
	}

	public StandaloneSignatureHandle AddStandaloneSignature(BlobHandle signature)
	{
		_standAloneSigTable.Add(new StandaloneSigRow
		{
			Signature = signature
		});
		return StandaloneSignatureHandle.FromRowId(_standAloneSigTable.Count);
	}

	public PropertyDefinitionHandle AddProperty(PropertyAttributes attributes, StringHandle name, BlobHandle signature)
	{
		_propertyTable.Add(new PropertyRow
		{
			PropFlags = (ushort)attributes,
			Name = name,
			Type = signature
		});
		return PropertyDefinitionHandle.FromRowId(_propertyTable.Count);
	}

	public void AddPropertyMap(TypeDefinitionHandle declaringType, PropertyDefinitionHandle propertyList)
	{
		_propertyMapTable.Add(new PropertyMapRow
		{
			Parent = declaringType.RowId,
			PropertyList = propertyList.RowId
		});
	}

	public EventDefinitionHandle AddEvent(EventAttributes attributes, StringHandle name, EntityHandle type)
	{
		_eventTable.Add(new EventRow
		{
			EventFlags = (ushort)attributes,
			Name = name,
			EventType = CodedIndex.TypeDefOrRefOrSpec(type)
		});
		return EventDefinitionHandle.FromRowId(_eventTable.Count);
	}

	public void AddEventMap(TypeDefinitionHandle declaringType, EventDefinitionHandle eventList)
	{
		_eventMapTable.Add(new EventMapRow
		{
			Parent = declaringType.RowId,
			EventList = eventList.RowId
		});
	}

	public ConstantHandle AddConstant(EntityHandle parent, object? value)
	{
		int num = CodedIndex.HasConstant(parent);
		_constantTableNeedsSorting |= num < _constantTableLastParent;
		_constantTableLastParent = num;
		_constantTable.Add(new ConstantRow
		{
			Type = (byte)MetadataWriterUtilities.GetConstantTypeCode(value),
			Parent = num,
			Value = GetOrAddConstantBlob(value)
		});
		return ConstantHandle.FromRowId(_constantTable.Count);
	}

	public void AddMethodSemantics(EntityHandle association, MethodSemanticsAttributes semantics, MethodDefinitionHandle methodDefinition)
	{
		int num = CodedIndex.HasSemantics(association);
		_methodSemanticsTableNeedsSorting |= num < _methodSemanticsTableLastAssociation;
		_methodSemanticsTableLastAssociation = num;
		_methodSemanticsTable.Add(new MethodSemanticsRow
		{
			Association = num,
			Method = methodDefinition.RowId,
			Semantic = (ushort)semantics
		});
	}

	public CustomAttributeHandle AddCustomAttribute(EntityHandle parent, EntityHandle constructor, BlobHandle value)
	{
		int num = CodedIndex.HasCustomAttribute(parent);
		_customAttributeTableNeedsSorting |= num < _customAttributeTableLastParent;
		_customAttributeTableLastParent = num;
		_customAttributeTable.Add(new CustomAttributeRow
		{
			Parent = num,
			Type = CodedIndex.CustomAttributeType(constructor),
			Value = value
		});
		return CustomAttributeHandle.FromRowId(_customAttributeTable.Count);
	}

	public MethodSpecificationHandle AddMethodSpecification(EntityHandle method, BlobHandle instantiation)
	{
		_methodSpecTable.Add(new MethodSpecRow
		{
			Method = CodedIndex.MethodDefOrRef(method),
			Instantiation = instantiation
		});
		return MethodSpecificationHandle.FromRowId(_methodSpecTable.Count);
	}

	public ModuleReferenceHandle AddModuleReference(StringHandle moduleName)
	{
		_moduleRefTable.Add(new ModuleRefRow
		{
			Name = moduleName
		});
		return ModuleReferenceHandle.FromRowId(_moduleRefTable.Count);
	}

	public ParameterHandle AddParameter(ParameterAttributes attributes, StringHandle name, int sequenceNumber)
	{
		if ((uint)sequenceNumber > 65535u)
		{
			Throw.ArgumentOutOfRange("sequenceNumber");
		}
		_paramTable.Add(new ParamRow
		{
			Flags = (ushort)attributes,
			Name = name,
			Sequence = (ushort)sequenceNumber
		});
		return ParameterHandle.FromRowId(_paramTable.Count);
	}

	public GenericParameterHandle AddGenericParameter(EntityHandle parent, GenericParameterAttributes attributes, StringHandle name, int index)
	{
		if ((uint)index > 65535u)
		{
			Throw.ArgumentOutOfRange("index");
		}
		_genericParamTable.Add(new GenericParamRow
		{
			Flags = (ushort)attributes,
			Name = name,
			Number = (ushort)index,
			Owner = CodedIndex.TypeOrMethodDef(parent)
		});
		return GenericParameterHandle.FromRowId(_genericParamTable.Count);
	}

	public GenericParameterConstraintHandle AddGenericParameterConstraint(GenericParameterHandle genericParameter, EntityHandle constraint)
	{
		_genericParamConstraintTable.Add(new GenericParamConstraintRow
		{
			Owner = genericParameter.RowId,
			Constraint = CodedIndex.TypeDefOrRefOrSpec(constraint)
		});
		return GenericParameterConstraintHandle.FromRowId(_genericParamConstraintTable.Count);
	}

	public FieldDefinitionHandle AddFieldDefinition(FieldAttributes attributes, StringHandle name, BlobHandle signature)
	{
		_fieldTable.Add(new FieldDefRow
		{
			Flags = (ushort)attributes,
			Name = name,
			Signature = signature
		});
		return FieldDefinitionHandle.FromRowId(_fieldTable.Count);
	}

	public void AddFieldLayout(FieldDefinitionHandle field, int offset)
	{
		_fieldLayoutTable.Add(new FieldLayoutRow
		{
			Field = field.RowId,
			Offset = offset
		});
	}

	public void AddMarshallingDescriptor(EntityHandle parent, BlobHandle descriptor)
	{
		int num = CodedIndex.HasFieldMarshal(parent);
		_fieldMarshalTableNeedsSorting |= num < _fieldMarshalTableLastParent;
		_fieldMarshalTableLastParent = num;
		_fieldMarshalTable.Add(new FieldMarshalRow
		{
			Parent = num,
			NativeType = descriptor
		});
	}

	public void AddFieldRelativeVirtualAddress(FieldDefinitionHandle field, int offset)
	{
		if (offset < 0)
		{
			Throw.ArgumentOutOfRange("offset");
		}
		_fieldRvaTable.Add(new FieldRvaRow
		{
			Field = field.RowId,
			Offset = offset
		});
	}

	public MethodDefinitionHandle AddMethodDefinition(MethodAttributes attributes, MethodImplAttributes implAttributes, StringHandle name, BlobHandle signature, int bodyOffset, ParameterHandle parameterList)
	{
		if (bodyOffset < -1)
		{
			Throw.ArgumentOutOfRange("bodyOffset");
		}
		_methodDefTable.Add(new MethodRow
		{
			Flags = (ushort)attributes,
			ImplFlags = (ushort)implAttributes,
			Name = name,
			Signature = signature,
			BodyOffset = bodyOffset,
			ParamList = parameterList.RowId
		});
		return MethodDefinitionHandle.FromRowId(_methodDefTable.Count);
	}

	public void AddMethodImport(MethodDefinitionHandle method, MethodImportAttributes attributes, StringHandle name, ModuleReferenceHandle module)
	{
		_implMapTable.Add(new ImplMapRow
		{
			MemberForwarded = CodedIndex.MemberForwarded(method),
			ImportName = name,
			ImportScope = module.RowId,
			MappingFlags = (ushort)attributes
		});
	}

	public MethodImplementationHandle AddMethodImplementation(TypeDefinitionHandle type, EntityHandle methodBody, EntityHandle methodDeclaration)
	{
		_methodImplTable.Add(new MethodImplRow
		{
			Class = type.RowId,
			MethodBody = CodedIndex.MethodDefOrRef(methodBody),
			MethodDecl = CodedIndex.MethodDefOrRef(methodDeclaration)
		});
		return MethodImplementationHandle.FromRowId(_methodImplTable.Count);
	}

	public MemberReferenceHandle AddMemberReference(EntityHandle parent, StringHandle name, BlobHandle signature)
	{
		_memberRefTable.Add(new MemberRefRow
		{
			Class = CodedIndex.MemberRefParent(parent),
			Name = name,
			Signature = signature
		});
		return MemberReferenceHandle.FromRowId(_memberRefTable.Count);
	}

	public ManifestResourceHandle AddManifestResource(ManifestResourceAttributes attributes, StringHandle name, EntityHandle implementation, uint offset)
	{
		_manifestResourceTable.Add(new ManifestResourceRow
		{
			Flags = (uint)attributes,
			Name = name,
			Implementation = ((!implementation.IsNil) ? CodedIndex.Implementation(implementation) : 0),
			Offset = offset
		});
		return ManifestResourceHandle.FromRowId(_manifestResourceTable.Count);
	}

	public AssemblyFileHandle AddAssemblyFile(StringHandle name, BlobHandle hashValue, bool containsMetadata)
	{
		_fileTable.Add(new FileTableRow
		{
			FileName = name,
			Flags = ((!containsMetadata) ? 1u : 0u),
			HashValue = hashValue
		});
		return AssemblyFileHandle.FromRowId(_fileTable.Count);
	}

	public ExportedTypeHandle AddExportedType(TypeAttributes attributes, StringHandle @namespace, StringHandle name, EntityHandle implementation, int typeDefinitionId)
	{
		_exportedTypeTable.Add(new ExportedTypeRow
		{
			Flags = (uint)attributes,
			Implementation = CodedIndex.Implementation(implementation),
			TypeNamespace = @namespace,
			TypeName = name,
			TypeDefId = typeDefinitionId
		});
		return ExportedTypeHandle.FromRowId(_exportedTypeTable.Count);
	}

	public DeclarativeSecurityAttributeHandle AddDeclarativeSecurityAttribute(EntityHandle parent, DeclarativeSecurityAction action, BlobHandle permissionSet)
	{
		int num = CodedIndex.HasDeclSecurity(parent);
		_declSecurityTableNeedsSorting |= num < _declSecurityTableLastParent;
		_declSecurityTableLastParent = num;
		_declSecurityTable.Add(new DeclSecurityRow
		{
			Parent = num,
			Action = (ushort)action,
			PermissionSet = permissionSet
		});
		return DeclarativeSecurityAttributeHandle.FromRowId(_declSecurityTable.Count);
	}

	public void AddEncLogEntry(EntityHandle entity, EditAndContinueOperation code)
	{
		_encLogTable.Add(new EncLogRow
		{
			Token = entity.Token,
			FuncCode = (byte)code
		});
	}

	public void AddEncMapEntry(EntityHandle entity)
	{
		_encMapTable.Add(new EncMapRow
		{
			Token = entity.Token
		});
	}

	public DocumentHandle AddDocument(BlobHandle name, GuidHandle hashAlgorithm, BlobHandle hash, GuidHandle language)
	{
		_documentTable.Add(new DocumentRow
		{
			Name = name,
			HashAlgorithm = hashAlgorithm,
			Hash = hash,
			Language = language
		});
		return DocumentHandle.FromRowId(_documentTable.Count);
	}

	public MethodDebugInformationHandle AddMethodDebugInformation(DocumentHandle document, BlobHandle sequencePoints)
	{
		_methodDebugInformationTable.Add(new MethodDebugInformationRow
		{
			Document = document.RowId,
			SequencePoints = sequencePoints
		});
		return MethodDebugInformationHandle.FromRowId(_methodDebugInformationTable.Count);
	}

	public LocalScopeHandle AddLocalScope(MethodDefinitionHandle method, ImportScopeHandle importScope, LocalVariableHandle variableList, LocalConstantHandle constantList, int startOffset, int length)
	{
		_localScopeTable.Add(new LocalScopeRow
		{
			Method = method.RowId,
			ImportScope = importScope.RowId,
			VariableList = variableList.RowId,
			ConstantList = constantList.RowId,
			StartOffset = startOffset,
			Length = length
		});
		return LocalScopeHandle.FromRowId(_localScopeTable.Count);
	}

	public LocalVariableHandle AddLocalVariable(LocalVariableAttributes attributes, int index, StringHandle name)
	{
		if ((uint)index > 65535u)
		{
			Throw.ArgumentOutOfRange("index");
		}
		_localVariableTable.Add(new LocalVariableRow
		{
			Attributes = (ushort)attributes,
			Index = (ushort)index,
			Name = name
		});
		return LocalVariableHandle.FromRowId(_localVariableTable.Count);
	}

	public LocalConstantHandle AddLocalConstant(StringHandle name, BlobHandle signature)
	{
		_localConstantTable.Add(new LocalConstantRow
		{
			Name = name,
			Signature = signature
		});
		return LocalConstantHandle.FromRowId(_localConstantTable.Count);
	}

	public ImportScopeHandle AddImportScope(ImportScopeHandle parentScope, BlobHandle imports)
	{
		_importScopeTable.Add(new ImportScopeRow
		{
			Parent = parentScope.RowId,
			Imports = imports
		});
		return ImportScopeHandle.FromRowId(_importScopeTable.Count);
	}

	public void AddStateMachineMethod(MethodDefinitionHandle moveNextMethod, MethodDefinitionHandle kickoffMethod)
	{
		_stateMachineMethodTable.Add(new StateMachineMethodRow
		{
			MoveNextMethod = moveNextMethod.RowId,
			KickoffMethod = kickoffMethod.RowId
		});
	}

	public CustomDebugInformationHandle AddCustomDebugInformation(EntityHandle parent, GuidHandle kind, BlobHandle value)
	{
		_customDebugInformationTable.Add(new CustomDebugInformationRow
		{
			Parent = CodedIndex.HasCustomDebugInformation(parent),
			Kind = kind,
			Value = value
		});
		return CustomDebugInformationHandle.FromRowId(_customDebugInformationTable.Count);
	}

	internal void ValidateOrder()
	{
		ValidateClassLayoutTable();
		ValidateFieldLayoutTable();
		ValidateFieldRvaTable();
		ValidateGenericParamTable();
		ValidateGenericParamConstaintTable();
		ValidateImplMapTable();
		ValidateInterfaceImplTable();
		ValidateMethodImplTable();
		ValidateNestedClassTable();
		ValidateLocalScopeTable();
		ValidateStateMachineMethodTable();
	}

	private void ValidateClassLayoutTable()
	{
		for (int i = 1; i < _classLayoutTable.Count; i++)
		{
			if (_classLayoutTable[i - 1].Parent >= _classLayoutTable[i].Parent)
			{
				Throw.InvalidOperation_TableNotSorted(TableIndex.ClassLayout);
			}
		}
	}

	private void ValidateFieldLayoutTable()
	{
		for (int i = 1; i < _fieldLayoutTable.Count; i++)
		{
			if (_fieldLayoutTable[i - 1].Field >= _fieldLayoutTable[i].Field)
			{
				Throw.InvalidOperation_TableNotSorted(TableIndex.FieldLayout);
			}
		}
	}

	private void ValidateFieldRvaTable()
	{
		for (int i = 1; i < _fieldRvaTable.Count; i++)
		{
			if (_fieldRvaTable[i - 1].Field >= _fieldRvaTable[i].Field)
			{
				Throw.InvalidOperation_TableNotSorted(TableIndex.FieldRva);
			}
		}
	}

	private void ValidateGenericParamTable()
	{
		if (_genericParamTable.Count == 0)
		{
			return;
		}
		GenericParamRow genericParamRow = _genericParamTable[0];
		int num = 1;
		while (num < _genericParamTable.Count)
		{
			GenericParamRow genericParamRow2 = _genericParamTable[num];
			if (genericParamRow2.Owner <= genericParamRow.Owner && (genericParamRow.Owner != genericParamRow2.Owner || genericParamRow2.Number <= genericParamRow.Number))
			{
				Throw.InvalidOperation_TableNotSorted(TableIndex.GenericParam);
			}
			num++;
			genericParamRow = genericParamRow2;
		}
	}

	private void ValidateGenericParamConstaintTable()
	{
		for (int i = 1; i < _genericParamConstraintTable.Count; i++)
		{
			if (_genericParamConstraintTable[i - 1].Owner > _genericParamConstraintTable[i].Owner)
			{
				Throw.InvalidOperation_TableNotSorted(TableIndex.GenericParamConstraint);
			}
		}
	}

	private void ValidateImplMapTable()
	{
		for (int i = 1; i < _implMapTable.Count; i++)
		{
			if (_implMapTable[i - 1].MemberForwarded >= _implMapTable[i].MemberForwarded)
			{
				Throw.InvalidOperation_TableNotSorted(TableIndex.ImplMap);
			}
		}
	}

	private void ValidateInterfaceImplTable()
	{
		if (_interfaceImplTable.Count == 0)
		{
			return;
		}
		InterfaceImplRow interfaceImplRow = _interfaceImplTable[0];
		int num = 1;
		while (num < _interfaceImplTable.Count)
		{
			InterfaceImplRow interfaceImplRow2 = _interfaceImplTable[num];
			if (interfaceImplRow2.Class <= interfaceImplRow.Class && (interfaceImplRow.Class != interfaceImplRow2.Class || interfaceImplRow2.Interface <= interfaceImplRow.Interface))
			{
				Throw.InvalidOperation_TableNotSorted(TableIndex.InterfaceImpl);
			}
			num++;
			interfaceImplRow = interfaceImplRow2;
		}
	}

	private void ValidateMethodImplTable()
	{
		for (int i = 1; i < _methodImplTable.Count; i++)
		{
			if (_methodImplTable[i - 1].Class > _methodImplTable[i].Class)
			{
				Throw.InvalidOperation_TableNotSorted(TableIndex.MethodImpl);
			}
		}
	}

	private void ValidateNestedClassTable()
	{
		for (int i = 1; i < _nestedClassTable.Count; i++)
		{
			if (_nestedClassTable[i - 1].NestedClass >= _nestedClassTable[i].NestedClass)
			{
				Throw.InvalidOperation_TableNotSorted(TableIndex.NestedClass);
			}
		}
	}

	private void ValidateLocalScopeTable()
	{
		if (_localScopeTable.Count == 0)
		{
			return;
		}
		LocalScopeRow localScopeRow = _localScopeTable[0];
		int num = 1;
		while (num < _localScopeTable.Count)
		{
			LocalScopeRow localScopeRow2 = _localScopeTable[num];
			if (localScopeRow2.Method <= localScopeRow.Method && (localScopeRow2.Method != localScopeRow.Method || (localScopeRow2.StartOffset <= localScopeRow.StartOffset && (localScopeRow2.StartOffset != localScopeRow.StartOffset || localScopeRow.Length < localScopeRow2.Length))))
			{
				Throw.InvalidOperation_TableNotSorted(TableIndex.LocalScope);
			}
			num++;
			localScopeRow = localScopeRow2;
		}
	}

	private void ValidateStateMachineMethodTable()
	{
		for (int i = 1; i < _stateMachineMethodTable.Count; i++)
		{
			if (_stateMachineMethodTable[i - 1].MoveNextMethod >= _stateMachineMethodTable[i].MoveNextMethod)
			{
				Throw.InvalidOperation_TableNotSorted(TableIndex.StateMachineMethod);
			}
		}
	}

	internal void SerializeMetadataTables(BlobBuilder writer, MetadataSizes metadataSizes, ImmutableArray<int> stringMap, int methodBodyStreamRva, int mappedFieldDataStreamRva)
	{
		int count = writer.Count;
		SerializeTablesHeader(writer, metadataSizes);
		if (metadataSizes.IsPresent(TableIndex.Module))
		{
			SerializeModuleTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.TypeRef))
		{
			SerializeTypeRefTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.TypeDef))
		{
			SerializeTypeDefTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.Field))
		{
			SerializeFieldTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.MethodDef))
		{
			SerializeMethodDefTable(writer, stringMap, metadataSizes, methodBodyStreamRva);
		}
		if (metadataSizes.IsPresent(TableIndex.Param))
		{
			SerializeParamTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.InterfaceImpl))
		{
			SerializeInterfaceImplTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.MemberRef))
		{
			SerializeMemberRefTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.Constant))
		{
			SerializeConstantTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.CustomAttribute))
		{
			SerializeCustomAttributeTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.FieldMarshal))
		{
			SerializeFieldMarshalTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.DeclSecurity))
		{
			SerializeDeclSecurityTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.ClassLayout))
		{
			SerializeClassLayoutTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.FieldLayout))
		{
			SerializeFieldLayoutTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.StandAloneSig))
		{
			SerializeStandAloneSigTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.EventMap))
		{
			SerializeEventMapTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.Event))
		{
			SerializeEventTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.PropertyMap))
		{
			SerializePropertyMapTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.Property))
		{
			SerializePropertyTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.MethodSemantics))
		{
			SerializeMethodSemanticsTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.MethodImpl))
		{
			SerializeMethodImplTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.ModuleRef))
		{
			SerializeModuleRefTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.TypeSpec))
		{
			SerializeTypeSpecTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.ImplMap))
		{
			SerializeImplMapTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.FieldRva))
		{
			SerializeFieldRvaTable(writer, metadataSizes, mappedFieldDataStreamRva);
		}
		if (metadataSizes.IsPresent(TableIndex.EncLog))
		{
			SerializeEncLogTable(writer);
		}
		if (metadataSizes.IsPresent(TableIndex.EncMap))
		{
			SerializeEncMapTable(writer);
		}
		if (metadataSizes.IsPresent(TableIndex.Assembly))
		{
			SerializeAssemblyTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.AssemblyRef))
		{
			SerializeAssemblyRefTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.File))
		{
			SerializeFileTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.ExportedType))
		{
			SerializeExportedTypeTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.ManifestResource))
		{
			SerializeManifestResourceTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.NestedClass))
		{
			SerializeNestedClassTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.GenericParam))
		{
			SerializeGenericParamTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.MethodSpec))
		{
			SerializeMethodSpecTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.GenericParamConstraint))
		{
			SerializeGenericParamConstraintTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.Document))
		{
			SerializeDocumentTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.MethodDebugInformation))
		{
			SerializeMethodDebugInformationTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.LocalScope))
		{
			SerializeLocalScopeTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.LocalVariable))
		{
			SerializeLocalVariableTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.LocalConstant))
		{
			SerializeLocalConstantTable(writer, stringMap, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.ImportScope))
		{
			SerializeImportScopeTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.StateMachineMethod))
		{
			SerializeStateMachineMethodTable(writer, metadataSizes);
		}
		if (metadataSizes.IsPresent(TableIndex.CustomDebugInformation))
		{
			SerializeCustomDebugInformationTable(writer, metadataSizes);
		}
		writer.WriteByte(0);
		writer.Align(4);
		int count2 = writer.Count;
	}

	private void SerializeTablesHeader(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		int count = writer.Count;
		HeapSizeFlag heapSizeFlag = (HeapSizeFlag)0;
		if (!metadataSizes.StringReferenceIsSmall)
		{
			heapSizeFlag |= HeapSizeFlag.StringHeapLarge;
		}
		if (!metadataSizes.GuidReferenceIsSmall)
		{
			heapSizeFlag |= HeapSizeFlag.GuidHeapLarge;
		}
		if (!metadataSizes.BlobReferenceIsSmall)
		{
			heapSizeFlag |= HeapSizeFlag.BlobHeapLarge;
		}
		if (metadataSizes.IsEncDelta)
		{
			heapSizeFlag |= (HeapSizeFlag)160;
		}
		ulong num = metadataSizes.PresentTablesMask & 0xC4000000000000uL;
		ulong value = num | (metadataSizes.IsStandaloneDebugMetadata ? 0 : 24190111578624uL);
		writer.WriteUInt32(0u);
		writer.WriteByte(2);
		writer.WriteByte(0);
		writer.WriteByte((byte)heapSizeFlag);
		writer.WriteByte(1);
		writer.WriteUInt64(metadataSizes.PresentTablesMask);
		writer.WriteUInt64(value);
		MetadataWriterUtilities.SerializeRowCounts(writer, metadataSizes.RowCounts);
		int count2 = writer.Count;
	}

	internal void SerializeModuleTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		if (_moduleRow.HasValue)
		{
			writer.WriteUInt16(_moduleRow.Value.Generation);
			writer.WriteReference(SerializeHandle(stringMap, _moduleRow.Value.Name), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(SerializeHandle(_moduleRow.Value.ModuleVersionId), metadataSizes.GuidReferenceIsSmall);
			writer.WriteReference(SerializeHandle(_moduleRow.Value.EncId), metadataSizes.GuidReferenceIsSmall);
			writer.WriteReference(SerializeHandle(_moduleRow.Value.EncBaseId), metadataSizes.GuidReferenceIsSmall);
		}
	}

	private void SerializeEncLogTable(BlobBuilder writer)
	{
		foreach (EncLogRow item in _encLogTable)
		{
			writer.WriteInt32(item.Token);
			writer.WriteUInt32(item.FuncCode);
		}
	}

	private void SerializeEncMapTable(BlobBuilder writer)
	{
		foreach (EncMapRow item in _encMapTable)
		{
			writer.WriteInt32(item.Token);
		}
	}

	private void SerializeTypeRefTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (TypeRefRow item in _typeRefTable)
		{
			writer.WriteReference(item.ResolutionScope, metadataSizes.ResolutionScopeCodedIndexIsSmall);
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(SerializeHandle(stringMap, item.Namespace), metadataSizes.StringReferenceIsSmall);
		}
	}

	private void SerializeTypeDefTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (TypeDefRow item in _typeDefTable)
		{
			writer.WriteUInt32(item.Flags);
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(SerializeHandle(stringMap, item.Namespace), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(item.Extends, metadataSizes.TypeDefOrRefCodedIndexIsSmall);
			writer.WriteReference(item.FieldList, metadataSizes.FieldDefReferenceIsSmall);
			writer.WriteReference(item.MethodList, metadataSizes.MethodDefReferenceIsSmall);
		}
	}

	private void SerializeFieldTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (FieldDefRow item in _fieldTable)
		{
			writer.WriteUInt16(item.Flags);
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(SerializeHandle(item.Signature), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeMethodDefTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes, int methodBodyStreamRva)
	{
		foreach (MethodRow item in _methodDefTable)
		{
			if (item.BodyOffset == -1)
			{
				writer.WriteUInt32(0u);
			}
			else
			{
				writer.WriteInt32(methodBodyStreamRva + item.BodyOffset);
			}
			writer.WriteUInt16(item.ImplFlags);
			writer.WriteUInt16(item.Flags);
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(SerializeHandle(item.Signature), metadataSizes.BlobReferenceIsSmall);
			writer.WriteReference(item.ParamList, metadataSizes.ParameterReferenceIsSmall);
		}
	}

	private void SerializeParamTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (ParamRow item in _paramTable)
		{
			writer.WriteUInt16(item.Flags);
			writer.WriteUInt16(item.Sequence);
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
		}
	}

	private void SerializeInterfaceImplTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (InterfaceImplRow item in _interfaceImplTable)
		{
			writer.WriteReference(item.Class, metadataSizes.TypeDefReferenceIsSmall);
			writer.WriteReference(item.Interface, metadataSizes.TypeDefOrRefCodedIndexIsSmall);
		}
	}

	private void SerializeMemberRefTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (MemberRefRow item in _memberRefTable)
		{
			writer.WriteReference(item.Class, metadataSizes.MemberRefParentCodedIndexIsSmall);
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(SerializeHandle(item.Signature), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeConstantTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		IEnumerable<ConstantRow> enumerable;
		if (!_constantTableNeedsSorting)
		{
			IEnumerable<ConstantRow> constantTable = _constantTable;
			enumerable = constantTable;
		}
		else
		{
			enumerable = _constantTable.OrderBy((ConstantRow x, ConstantRow y) => x.Parent - y.Parent);
		}
		IEnumerable<ConstantRow> enumerable2 = enumerable;
		foreach (ConstantRow item in enumerable2)
		{
			writer.WriteByte(item.Type);
			writer.WriteByte(0);
			writer.WriteReference(item.Parent, metadataSizes.HasConstantCodedIndexIsSmall);
			writer.WriteReference(SerializeHandle(item.Value), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeCustomAttributeTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		IEnumerable<CustomAttributeRow> enumerable;
		if (!_customAttributeTableNeedsSorting)
		{
			IEnumerable<CustomAttributeRow> customAttributeTable = _customAttributeTable;
			enumerable = customAttributeTable;
		}
		else
		{
			enumerable = _customAttributeTable.OrderBy((CustomAttributeRow x, CustomAttributeRow y) => x.Parent - y.Parent);
		}
		IEnumerable<CustomAttributeRow> enumerable2 = enumerable;
		foreach (CustomAttributeRow item in enumerable2)
		{
			writer.WriteReference(item.Parent, metadataSizes.HasCustomAttributeCodedIndexIsSmall);
			writer.WriteReference(item.Type, metadataSizes.CustomAttributeTypeCodedIndexIsSmall);
			writer.WriteReference(SerializeHandle(item.Value), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeFieldMarshalTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		IEnumerable<FieldMarshalRow> enumerable;
		if (!_fieldMarshalTableNeedsSorting)
		{
			IEnumerable<FieldMarshalRow> fieldMarshalTable = _fieldMarshalTable;
			enumerable = fieldMarshalTable;
		}
		else
		{
			enumerable = _fieldMarshalTable.OrderBy((FieldMarshalRow x, FieldMarshalRow y) => x.Parent - y.Parent);
		}
		IEnumerable<FieldMarshalRow> enumerable2 = enumerable;
		foreach (FieldMarshalRow item in enumerable2)
		{
			writer.WriteReference(item.Parent, metadataSizes.HasFieldMarshalCodedIndexIsSmall);
			writer.WriteReference(SerializeHandle(item.NativeType), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeDeclSecurityTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		IEnumerable<DeclSecurityRow> enumerable;
		if (!_declSecurityTableNeedsSorting)
		{
			IEnumerable<DeclSecurityRow> declSecurityTable = _declSecurityTable;
			enumerable = declSecurityTable;
		}
		else
		{
			enumerable = _declSecurityTable.OrderBy((DeclSecurityRow x, DeclSecurityRow y) => x.Parent - y.Parent);
		}
		IEnumerable<DeclSecurityRow> enumerable2 = enumerable;
		foreach (DeclSecurityRow item in enumerable2)
		{
			writer.WriteUInt16(item.Action);
			writer.WriteReference(item.Parent, metadataSizes.DeclSecurityCodedIndexIsSmall);
			writer.WriteReference(SerializeHandle(item.PermissionSet), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeClassLayoutTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (ClassLayoutRow item in _classLayoutTable)
		{
			writer.WriteUInt16(item.PackingSize);
			writer.WriteUInt32(item.ClassSize);
			writer.WriteReference(item.Parent, metadataSizes.TypeDefReferenceIsSmall);
		}
	}

	private void SerializeFieldLayoutTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (FieldLayoutRow item in _fieldLayoutTable)
		{
			writer.WriteInt32(item.Offset);
			writer.WriteReference(item.Field, metadataSizes.FieldDefReferenceIsSmall);
		}
	}

	private void SerializeStandAloneSigTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (StandaloneSigRow item in _standAloneSigTable)
		{
			writer.WriteReference(SerializeHandle(item.Signature), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeEventMapTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (EventMapRow item in _eventMapTable)
		{
			writer.WriteReference(item.Parent, metadataSizes.TypeDefReferenceIsSmall);
			writer.WriteReference(item.EventList, metadataSizes.EventDefReferenceIsSmall);
		}
	}

	private void SerializeEventTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (EventRow item in _eventTable)
		{
			writer.WriteUInt16(item.EventFlags);
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(item.EventType, metadataSizes.TypeDefOrRefCodedIndexIsSmall);
		}
	}

	private void SerializePropertyMapTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (PropertyMapRow item in _propertyMapTable)
		{
			writer.WriteReference(item.Parent, metadataSizes.TypeDefReferenceIsSmall);
			writer.WriteReference(item.PropertyList, metadataSizes.PropertyDefReferenceIsSmall);
		}
	}

	private void SerializePropertyTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (PropertyRow item in _propertyTable)
		{
			writer.WriteUInt16(item.PropFlags);
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(SerializeHandle(item.Type), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeMethodSemanticsTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		IEnumerable<MethodSemanticsRow> enumerable;
		if (!_methodSemanticsTableNeedsSorting)
		{
			IEnumerable<MethodSemanticsRow> methodSemanticsTable = _methodSemanticsTable;
			enumerable = methodSemanticsTable;
		}
		else
		{
			enumerable = _methodSemanticsTable.OrderBy((MethodSemanticsRow x, MethodSemanticsRow y) => x.Association - y.Association);
		}
		IEnumerable<MethodSemanticsRow> enumerable2 = enumerable;
		foreach (MethodSemanticsRow item in enumerable2)
		{
			writer.WriteUInt16(item.Semantic);
			writer.WriteReference(item.Method, metadataSizes.MethodDefReferenceIsSmall);
			writer.WriteReference(item.Association, metadataSizes.HasSemanticsCodedIndexIsSmall);
		}
	}

	private void SerializeMethodImplTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (MethodImplRow item in _methodImplTable)
		{
			writer.WriteReference(item.Class, metadataSizes.TypeDefReferenceIsSmall);
			writer.WriteReference(item.MethodBody, metadataSizes.MethodDefOrRefCodedIndexIsSmall);
			writer.WriteReference(item.MethodDecl, metadataSizes.MethodDefOrRefCodedIndexIsSmall);
		}
	}

	private void SerializeModuleRefTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (ModuleRefRow item in _moduleRefTable)
		{
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
		}
	}

	private void SerializeTypeSpecTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (TypeSpecRow item in _typeSpecTable)
		{
			writer.WriteReference(SerializeHandle(item.Signature), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeImplMapTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (ImplMapRow item in _implMapTable)
		{
			writer.WriteUInt16(item.MappingFlags);
			writer.WriteReference(item.MemberForwarded, metadataSizes.MemberForwardedCodedIndexIsSmall);
			writer.WriteReference(SerializeHandle(stringMap, item.ImportName), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(item.ImportScope, metadataSizes.ModuleRefReferenceIsSmall);
		}
	}

	private void SerializeFieldRvaTable(BlobBuilder writer, MetadataSizes metadataSizes, int mappedFieldDataStreamRva)
	{
		foreach (FieldRvaRow item in _fieldRvaTable)
		{
			writer.WriteInt32(mappedFieldDataStreamRva + item.Offset);
			writer.WriteReference(item.Field, metadataSizes.FieldDefReferenceIsSmall);
		}
	}

	private void SerializeAssemblyTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		if (_assemblyRow.HasValue)
		{
			Version version = _assemblyRow.Value.Version;
			writer.WriteUInt32(_assemblyRow.Value.HashAlgorithm);
			writer.WriteUInt16((ushort)version.Major);
			writer.WriteUInt16((ushort)version.Minor);
			writer.WriteUInt16((ushort)version.Build);
			writer.WriteUInt16((ushort)version.Revision);
			writer.WriteUInt32(_assemblyRow.Value.Flags);
			writer.WriteReference(SerializeHandle(_assemblyRow.Value.AssemblyKey), metadataSizes.BlobReferenceIsSmall);
			writer.WriteReference(SerializeHandle(stringMap, _assemblyRow.Value.AssemblyName), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(SerializeHandle(stringMap, _assemblyRow.Value.AssemblyCulture), metadataSizes.StringReferenceIsSmall);
		}
	}

	private void SerializeAssemblyRefTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (AssemblyRefTableRow item in _assemblyRefTable)
		{
			writer.WriteUInt16((ushort)item.Version.Major);
			writer.WriteUInt16((ushort)item.Version.Minor);
			writer.WriteUInt16((ushort)item.Version.Build);
			writer.WriteUInt16((ushort)item.Version.Revision);
			writer.WriteUInt32(item.Flags);
			writer.WriteReference(SerializeHandle(item.PublicKeyToken), metadataSizes.BlobReferenceIsSmall);
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(SerializeHandle(stringMap, item.Culture), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(SerializeHandle(item.HashValue), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeFileTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (FileTableRow item in _fileTable)
		{
			writer.WriteUInt32(item.Flags);
			writer.WriteReference(SerializeHandle(stringMap, item.FileName), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(SerializeHandle(item.HashValue), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeExportedTypeTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (ExportedTypeRow item in _exportedTypeTable)
		{
			writer.WriteUInt32(item.Flags);
			writer.WriteInt32(item.TypeDefId);
			writer.WriteReference(SerializeHandle(stringMap, item.TypeName), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(SerializeHandle(stringMap, item.TypeNamespace), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(item.Implementation, metadataSizes.ImplementationCodedIndexIsSmall);
		}
	}

	private void SerializeManifestResourceTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (ManifestResourceRow item in _manifestResourceTable)
		{
			writer.WriteUInt32(item.Offset);
			writer.WriteUInt32(item.Flags);
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(item.Implementation, metadataSizes.ImplementationCodedIndexIsSmall);
		}
	}

	private void SerializeNestedClassTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (NestedClassRow item in _nestedClassTable)
		{
			writer.WriteReference(item.NestedClass, metadataSizes.TypeDefReferenceIsSmall);
			writer.WriteReference(item.EnclosingClass, metadataSizes.TypeDefReferenceIsSmall);
		}
	}

	private void SerializeGenericParamTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (GenericParamRow item in _genericParamTable)
		{
			writer.WriteUInt16(item.Number);
			writer.WriteUInt16(item.Flags);
			writer.WriteReference(item.Owner, metadataSizes.TypeOrMethodDefCodedIndexIsSmall);
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
		}
	}

	private void SerializeGenericParamConstraintTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (GenericParamConstraintRow item in _genericParamConstraintTable)
		{
			writer.WriteReference(item.Owner, metadataSizes.GenericParamReferenceIsSmall);
			writer.WriteReference(item.Constraint, metadataSizes.TypeDefOrRefCodedIndexIsSmall);
		}
	}

	private void SerializeMethodSpecTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (MethodSpecRow item in _methodSpecTable)
		{
			writer.WriteReference(item.Method, metadataSizes.MethodDefOrRefCodedIndexIsSmall);
			writer.WriteReference(SerializeHandle(item.Instantiation), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeDocumentTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (DocumentRow item in _documentTable)
		{
			writer.WriteReference(SerializeHandle(item.Name), metadataSizes.BlobReferenceIsSmall);
			writer.WriteReference(SerializeHandle(item.HashAlgorithm), metadataSizes.GuidReferenceIsSmall);
			writer.WriteReference(SerializeHandle(item.Hash), metadataSizes.BlobReferenceIsSmall);
			writer.WriteReference(SerializeHandle(item.Language), metadataSizes.GuidReferenceIsSmall);
		}
	}

	private void SerializeMethodDebugInformationTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (MethodDebugInformationRow item in _methodDebugInformationTable)
		{
			writer.WriteReference(item.Document, metadataSizes.DocumentReferenceIsSmall);
			writer.WriteReference(SerializeHandle(item.SequencePoints), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeLocalScopeTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (LocalScopeRow item in _localScopeTable)
		{
			writer.WriteReference(item.Method, metadataSizes.MethodDefReferenceIsSmall);
			writer.WriteReference(item.ImportScope, metadataSizes.ImportScopeReferenceIsSmall);
			writer.WriteReference(item.VariableList, metadataSizes.LocalVariableReferenceIsSmall);
			writer.WriteReference(item.ConstantList, metadataSizes.LocalConstantReferenceIsSmall);
			writer.WriteInt32(item.StartOffset);
			writer.WriteInt32(item.Length);
		}
	}

	private void SerializeLocalVariableTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (LocalVariableRow item in _localVariableTable)
		{
			writer.WriteUInt16(item.Attributes);
			writer.WriteUInt16(item.Index);
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
		}
	}

	private void SerializeLocalConstantTable(BlobBuilder writer, ImmutableArray<int> stringMap, MetadataSizes metadataSizes)
	{
		foreach (LocalConstantRow item in _localConstantTable)
		{
			writer.WriteReference(SerializeHandle(stringMap, item.Name), metadataSizes.StringReferenceIsSmall);
			writer.WriteReference(SerializeHandle(item.Signature), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeImportScopeTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (ImportScopeRow item in _importScopeTable)
		{
			writer.WriteReference(item.Parent, metadataSizes.ImportScopeReferenceIsSmall);
			writer.WriteReference(SerializeHandle(item.Imports), metadataSizes.BlobReferenceIsSmall);
		}
	}

	private void SerializeStateMachineMethodTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (StateMachineMethodRow item in _stateMachineMethodTable)
		{
			writer.WriteReference(item.MoveNextMethod, metadataSizes.MethodDefReferenceIsSmall);
			writer.WriteReference(item.KickoffMethod, metadataSizes.MethodDefReferenceIsSmall);
		}
	}

	private void SerializeCustomDebugInformationTable(BlobBuilder writer, MetadataSizes metadataSizes)
	{
		foreach (CustomDebugInformationRow item in _customDebugInformationTable.OrderBy(delegate(CustomDebugInformationRow x, CustomDebugInformationRow y)
		{
			int num = x.Parent - y.Parent;
			return (num == 0) ? (x.Kind.Index - y.Kind.Index) : num;
		}))
		{
			writer.WriteReference(item.Parent, metadataSizes.HasCustomDebugInformationCodedIndexIsSmall);
			writer.WriteReference(SerializeHandle(item.Kind), metadataSizes.GuidReferenceIsSmall);
			writer.WriteReference(SerializeHandle(item.Value), metadataSizes.BlobReferenceIsSmall);
		}
	}

	public MetadataBuilder(int userStringHeapStartOffset = 0, int stringHeapStartOffset = 0, int blobHeapStartOffset = 0, int guidHeapStartOffset = 0)
	{
		if (userStringHeapStartOffset >= 16777215)
		{
			Throw.HeapSizeLimitExceeded(HeapIndex.UserString);
		}
		if (userStringHeapStartOffset < 0)
		{
			Throw.ArgumentOutOfRange("userStringHeapStartOffset");
		}
		if (stringHeapStartOffset < 0)
		{
			Throw.ArgumentOutOfRange("stringHeapStartOffset");
		}
		if (blobHeapStartOffset < 0)
		{
			Throw.ArgumentOutOfRange("blobHeapStartOffset");
		}
		if (guidHeapStartOffset < 0)
		{
			Throw.ArgumentOutOfRange("guidHeapStartOffset");
		}
		if (guidHeapStartOffset % 16 != 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.ValueMustBeMultiple, 16), "guidHeapStartOffset");
		}
		_userStringBuilder.WriteByte(0);
		_blobs.Add(ImmutableArray<byte>.Empty, default(BlobHandle));
		_blobHeapSize = 1;
		_userStringHeapStartOffset = userStringHeapStartOffset;
		_stringHeapStartOffset = stringHeapStartOffset;
		_blobHeapStartOffset = blobHeapStartOffset;
		_guidBuilder.WriteBytes(0, guidHeapStartOffset);
	}

	public void SetCapacity(HeapIndex heap, int byteCount)
	{
		if (byteCount < 0)
		{
			Throw.ArgumentOutOfRange("byteCount");
		}
		switch (heap)
		{
		case HeapIndex.Guid:
			_guidBuilder.SetCapacity(byteCount);
			break;
		case HeapIndex.String:
			_stringHeapCapacity = byteCount;
			break;
		case HeapIndex.UserString:
			_userStringBuilder.SetCapacity(byteCount);
			break;
		default:
			Throw.ArgumentOutOfRange("heap");
			break;
		case HeapIndex.Blob:
			break;
		}
	}

	internal int SerializeHandle(ImmutableArray<int> map, StringHandle handle)
	{
		return map[handle.GetWriterVirtualIndex()];
	}

	internal int SerializeHandle(BlobHandle handle)
	{
		return handle.GetHeapOffset();
	}

	internal int SerializeHandle(GuidHandle handle)
	{
		return handle.Index;
	}

	internal int SerializeHandle(UserStringHandle handle)
	{
		return handle.GetHeapOffset();
	}

	public BlobHandle GetOrAddBlob(BlobBuilder value)
	{
		if (value == null)
		{
			Throw.ArgumentNull("value");
		}
		return GetOrAddBlob(value.ToImmutableArray());
	}

	public BlobHandle GetOrAddBlob(byte[] value)
	{
		if (value == null)
		{
			Throw.ArgumentNull("value");
		}
		return GetOrAddBlob(ImmutableArray.Create(value));
	}

	public BlobHandle GetOrAddBlob(ImmutableArray<byte> value)
	{
		if (value.IsDefault)
		{
			Throw.ArgumentNull("value");
		}
		if (!_blobs.TryGetValue(value, out var value2))
		{
			value2 = BlobHandle.FromOffset(_blobHeapStartOffset + _blobHeapSize);
			_blobs.Add(value, value2);
			_blobHeapSize += BlobWriterImpl.GetCompressedIntegerSize(value.Length) + value.Length;
		}
		return value2;
	}

	public BlobHandle GetOrAddConstantBlob(object? value)
	{
		if (value is string value2)
		{
			return GetOrAddBlobUTF16(value2);
		}
		PooledBlobBuilder instance = PooledBlobBuilder.GetInstance();
		instance.WriteConstant(value);
		BlobHandle orAddBlob = GetOrAddBlob(instance);
		instance.Free();
		return orAddBlob;
	}

	public BlobHandle GetOrAddBlobUTF16(string value)
	{
		PooledBlobBuilder instance = PooledBlobBuilder.GetInstance();
		instance.WriteUTF16(value);
		BlobHandle orAddBlob = GetOrAddBlob(instance);
		instance.Free();
		return orAddBlob;
	}

	public BlobHandle GetOrAddBlobUTF8(string value, bool allowUnpairedSurrogates = true)
	{
		PooledBlobBuilder instance = PooledBlobBuilder.GetInstance();
		instance.WriteUTF8(value, allowUnpairedSurrogates);
		BlobHandle orAddBlob = GetOrAddBlob(instance);
		instance.Free();
		return orAddBlob;
	}

	public BlobHandle GetOrAddDocumentName(string value)
	{
		if (value == null)
		{
			Throw.ArgumentNull("value");
		}
		char c = ChooseSeparator(value);
		PooledBlobBuilder instance = PooledBlobBuilder.GetInstance();
		instance.WriteByte((byte)c);
		PooledBlobBuilder instance2 = PooledBlobBuilder.GetInstance();
		int num = 0;
		while (true)
		{
			int num2 = value.IndexOf(c, num);
			instance2.WriteUTF8(value, num, ((num2 >= 0) ? num2 : value.Length) - num, allowUnpairedSurrogates: true, prependSize: false);
			instance.WriteCompressedInteger(GetOrAddBlob(instance2).GetHeapOffset());
			if (num2 == -1)
			{
				break;
			}
			if (num2 == value.Length - 1)
			{
				instance.WriteByte(0);
				break;
			}
			instance2.Clear();
			num = num2 + 1;
		}
		instance2.Free();
		BlobHandle orAddBlob = GetOrAddBlob(instance);
		instance.Free();
		return orAddBlob;
	}

	private static char ChooseSeparator(string str)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < str.Length; i++)
		{
			switch (str[i])
			{
			case '/':
				num++;
				break;
			case '\\':
				num2++;
				break;
			}
		}
		if (num < num2)
		{
			return '\\';
		}
		return '/';
	}

	public GuidHandle GetOrAddGuid(Guid guid)
	{
		if (guid == Guid.Empty)
		{
			return default(GuidHandle);
		}
		if (_guids.TryGetValue(guid, out var value))
		{
			return value;
		}
		value = GetNewGuidHandle();
		_guids.Add(guid, value);
		_guidBuilder.WriteGuid(guid);
		return value;
	}

	public ReservedBlob<GuidHandle> ReserveGuid()
	{
		GuidHandle newGuidHandle = GetNewGuidHandle();
		Blob content = _guidBuilder.ReserveBytes(16);
		return new ReservedBlob<GuidHandle>(newGuidHandle, content);
	}

	private GuidHandle GetNewGuidHandle()
	{
		return GuidHandle.FromIndex((_guidBuilder.Count >> 4) + 1);
	}

	public StringHandle GetOrAddString(string value)
	{
		if (value == null)
		{
			Throw.ArgumentNull("value");
		}
		StringHandle value2;
		if (value.Length == 0)
		{
			value2 = default(StringHandle);
		}
		else if (!_strings.TryGetValue(value, out value2))
		{
			value2 = StringHandle.FromWriterVirtualIndex(_strings.Count + 1);
			_strings.Add(value, value2);
		}
		return value2;
	}

	public ReservedBlob<UserStringHandle> ReserveUserString(int length)
	{
		if (length < 0)
		{
			Throw.ArgumentOutOfRange("length");
		}
		UserStringHandle newUserStringHandle = GetNewUserStringHandle();
		int userStringByteLength = BlobUtilities.GetUserStringByteLength(length);
		Blob content = _userStringBuilder.ReserveBytes(BlobWriterImpl.GetCompressedIntegerSize(userStringByteLength) + userStringByteLength);
		return new ReservedBlob<UserStringHandle>(newUserStringHandle, content);
	}

	public UserStringHandle GetOrAddUserString(string value)
	{
		if (value == null)
		{
			Throw.ArgumentNull("value");
		}
		if (!_userStrings.TryGetValue(value, out var value2))
		{
			value2 = GetNewUserStringHandle();
			_userStrings.Add(value, value2);
			_userStringBuilder.WriteUserString(value);
		}
		return value2;
	}

	private UserStringHandle GetNewUserStringHandle()
	{
		int num = _userStringHeapStartOffset + _userStringBuilder.Count;
		if (num >= 16777216)
		{
			Throw.HeapSizeLimitExceeded(HeapIndex.UserString);
		}
		return UserStringHandle.FromOffset(num);
	}

	private static ImmutableArray<int> SerializeStringHeap(BlobBuilder heapBuilder, Dictionary<string, StringHandle> strings, int stringHeapStartOffset)
	{
		List<KeyValuePair<string, StringHandle>> list = new List<KeyValuePair<string, StringHandle>>(strings);
		list.Sort(SuffixSort.Instance);
		int num = list.Count + 1;
		ImmutableArray<int>.Builder builder = ImmutableArray.CreateBuilder<int>(num);
		builder.Count = num;
		builder[0] = 0;
		heapBuilder.WriteByte(0);
		string text = string.Empty;
		foreach (KeyValuePair<string, StringHandle> item in list)
		{
			int num2 = stringHeapStartOffset + heapBuilder.Count;
			if (text.EndsWith(item.Key, StringComparison.Ordinal) && !BlobUtilities.IsLowSurrogateChar(item.Key[0]))
			{
				builder[item.Value.GetWriterVirtualIndex()] = num2 - (BlobUtilities.GetUTF8ByteCount(item.Key) + 1);
			}
			else
			{
				builder[item.Value.GetWriterVirtualIndex()] = num2;
				heapBuilder.WriteUTF8(item.Key, allowUnpairedSurrogates: false);
				heapBuilder.WriteByte(0);
			}
			text = item.Key;
		}
		return builder.MoveToImmutable();
	}

	internal void WriteHeapsTo(BlobBuilder builder, BlobBuilder stringHeap)
	{
		WriteAligned(stringHeap, builder);
		WriteAligned(_userStringBuilder, builder);
		WriteAligned(_guidBuilder, builder);
		WriteAlignedBlobHeap(builder);
	}

	private void WriteAlignedBlobHeap(BlobBuilder builder)
	{
		int num = BitArithmetic.Align(_blobHeapSize, 4) - _blobHeapSize;
		BlobWriter blobWriter = new BlobWriter(builder.ReserveBytes(_blobHeapSize + num));
		int blobHeapStartOffset = _blobHeapStartOffset;
		foreach (KeyValuePair<ImmutableArray<byte>, BlobHandle> blob in _blobs)
		{
			int heapOffset = blob.Value.GetHeapOffset();
			ImmutableArray<byte> key = blob.Key;
			blobWriter.Offset = ((heapOffset != 0) ? (heapOffset - blobHeapStartOffset) : 0);
			blobWriter.WriteCompressedInteger(key.Length);
			blobWriter.WriteBytes(key);
		}
		blobWriter.Offset = _blobHeapSize;
		blobWriter.WriteBytes(0, num);
	}

	private static void WriteAligned(BlobBuilder source, BlobBuilder target)
	{
		int count = source.Count;
		target.LinkSuffix(source);
		target.WriteBytes(0, BitArithmetic.Align(count, 4) - count);
	}
}
