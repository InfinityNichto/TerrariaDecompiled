using System.Collections.Immutable;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

public sealed class MetadataSizes
{
	internal const int MaxMetadataVersionByteCount = 254;

	internal readonly int MetadataVersionPaddedLength;

	internal const ulong SortedDebugTables = 55169095435288576uL;

	internal readonly bool IsEncDelta;

	internal readonly bool IsCompressed;

	internal readonly bool BlobReferenceIsSmall;

	internal readonly bool StringReferenceIsSmall;

	internal readonly bool GuidReferenceIsSmall;

	internal readonly bool CustomAttributeTypeCodedIndexIsSmall;

	internal readonly bool DeclSecurityCodedIndexIsSmall;

	internal readonly bool EventDefReferenceIsSmall;

	internal readonly bool FieldDefReferenceIsSmall;

	internal readonly bool GenericParamReferenceIsSmall;

	internal readonly bool HasConstantCodedIndexIsSmall;

	internal readonly bool HasCustomAttributeCodedIndexIsSmall;

	internal readonly bool HasFieldMarshalCodedIndexIsSmall;

	internal readonly bool HasSemanticsCodedIndexIsSmall;

	internal readonly bool ImplementationCodedIndexIsSmall;

	internal readonly bool MemberForwardedCodedIndexIsSmall;

	internal readonly bool MemberRefParentCodedIndexIsSmall;

	internal readonly bool MethodDefReferenceIsSmall;

	internal readonly bool MethodDefOrRefCodedIndexIsSmall;

	internal readonly bool ModuleRefReferenceIsSmall;

	internal readonly bool ParameterReferenceIsSmall;

	internal readonly bool PropertyDefReferenceIsSmall;

	internal readonly bool ResolutionScopeCodedIndexIsSmall;

	internal readonly bool TypeDefReferenceIsSmall;

	internal readonly bool TypeDefOrRefCodedIndexIsSmall;

	internal readonly bool TypeOrMethodDefCodedIndexIsSmall;

	internal readonly bool DocumentReferenceIsSmall;

	internal readonly bool LocalVariableReferenceIsSmall;

	internal readonly bool LocalConstantReferenceIsSmall;

	internal readonly bool ImportScopeReferenceIsSmall;

	internal readonly bool HasCustomDebugInformationCodedIndexIsSmall;

	internal readonly ulong PresentTablesMask;

	internal readonly ulong ExternalTablesMask;

	internal readonly int MetadataStreamStorageSize;

	internal readonly int MetadataTableStreamSize;

	internal readonly int StandalonePdbStreamSize;

	internal const int PdbIdSize = 20;

	public ImmutableArray<int> HeapSizes { get; }

	public ImmutableArray<int> RowCounts { get; }

	public ImmutableArray<int> ExternalRowCounts { get; }

	internal bool IsStandaloneDebugMetadata => StandalonePdbStreamSize > 0;

	internal int MetadataHeaderSize => 16 + MetadataVersionPaddedLength + 2 + 2 + (IsStandaloneDebugMetadata ? 16 : 0) + 76 + (IsEncDelta ? 16 : 0);

	internal int MetadataSize => MetadataHeaderSize + MetadataStreamStorageSize;

	internal MetadataSizes(ImmutableArray<int> rowCounts, ImmutableArray<int> externalRowCounts, ImmutableArray<int> heapSizes, int metadataVersionByteCount, bool isStandaloneDebugMetadata)
	{
		RowCounts = rowCounts;
		ExternalRowCounts = externalRowCounts;
		HeapSizes = heapSizes;
		MetadataVersionPaddedLength = BitArithmetic.Align(metadataVersionByteCount + 1, 4);
		PresentTablesMask = ComputeNonEmptyTableMask(rowCounts);
		ExternalTablesMask = ComputeNonEmptyTableMask(externalRowCounts);
		bool flag = IsPresent(TableIndex.EncLog) || IsPresent(TableIndex.EncMap);
		bool flag2 = !flag;
		IsEncDelta = flag;
		IsCompressed = flag2;
		BlobReferenceIsSmall = flag2 && heapSizes[2] <= 65535;
		StringReferenceIsSmall = flag2 && heapSizes[1] <= 65535;
		GuidReferenceIsSmall = flag2 && heapSizes[3] <= 65535;
		CustomAttributeTypeCodedIndexIsSmall = IsReferenceSmall(3, TableIndex.MethodDef, TableIndex.MemberRef);
		DeclSecurityCodedIndexIsSmall = IsReferenceSmall(2, TableIndex.MethodDef, TableIndex.TypeDef);
		EventDefReferenceIsSmall = IsReferenceSmall(0, TableIndex.Event);
		FieldDefReferenceIsSmall = IsReferenceSmall(0, TableIndex.Field);
		GenericParamReferenceIsSmall = IsReferenceSmall(0, TableIndex.GenericParam);
		HasConstantCodedIndexIsSmall = IsReferenceSmall(2, TableIndex.Field, TableIndex.Param, TableIndex.Property);
		HasCustomAttributeCodedIndexIsSmall = IsReferenceSmall(5, TableIndex.MethodDef, TableIndex.Field, TableIndex.TypeRef, TableIndex.TypeDef, TableIndex.Param, TableIndex.InterfaceImpl, TableIndex.MemberRef, TableIndex.Module, TableIndex.DeclSecurity, TableIndex.Property, TableIndex.Event, TableIndex.StandAloneSig, TableIndex.ModuleRef, TableIndex.TypeSpec, TableIndex.Assembly, TableIndex.AssemblyRef, TableIndex.File, TableIndex.ExportedType, TableIndex.ManifestResource, TableIndex.GenericParam, TableIndex.GenericParamConstraint, TableIndex.MethodSpec);
		HasFieldMarshalCodedIndexIsSmall = IsReferenceSmall(1, TableIndex.Field, TableIndex.Param);
		HasSemanticsCodedIndexIsSmall = IsReferenceSmall(1, TableIndex.Event, TableIndex.Property);
		ImplementationCodedIndexIsSmall = IsReferenceSmall(2, TableIndex.File, TableIndex.AssemblyRef, TableIndex.ExportedType);
		MemberForwardedCodedIndexIsSmall = IsReferenceSmall(1, TableIndex.Field, TableIndex.MethodDef);
		MemberRefParentCodedIndexIsSmall = IsReferenceSmall(3, TableIndex.TypeDef, TableIndex.TypeRef, TableIndex.ModuleRef, TableIndex.MethodDef, TableIndex.TypeSpec);
		MethodDefReferenceIsSmall = IsReferenceSmall(0, TableIndex.MethodDef);
		MethodDefOrRefCodedIndexIsSmall = IsReferenceSmall(1, TableIndex.MethodDef, TableIndex.MemberRef);
		ModuleRefReferenceIsSmall = IsReferenceSmall(0, TableIndex.ModuleRef);
		ParameterReferenceIsSmall = IsReferenceSmall(0, TableIndex.Param);
		PropertyDefReferenceIsSmall = IsReferenceSmall(0, TableIndex.Property);
		ResolutionScopeCodedIndexIsSmall = IsReferenceSmall(2, TableIndex.Module, TableIndex.ModuleRef, TableIndex.AssemblyRef, TableIndex.TypeRef);
		TypeDefReferenceIsSmall = IsReferenceSmall(0, TableIndex.TypeDef);
		TypeDefOrRefCodedIndexIsSmall = IsReferenceSmall(2, TableIndex.TypeDef, TableIndex.TypeRef, TableIndex.TypeSpec);
		TypeOrMethodDefCodedIndexIsSmall = IsReferenceSmall(1, TableIndex.TypeDef, TableIndex.MethodDef);
		DocumentReferenceIsSmall = IsReferenceSmall(0, TableIndex.Document);
		LocalVariableReferenceIsSmall = IsReferenceSmall(0, TableIndex.LocalVariable);
		LocalConstantReferenceIsSmall = IsReferenceSmall(0, TableIndex.LocalConstant);
		ImportScopeReferenceIsSmall = IsReferenceSmall(0, TableIndex.ImportScope);
		HasCustomDebugInformationCodedIndexIsSmall = IsReferenceSmall(5, TableIndex.MethodDef, TableIndex.Field, TableIndex.TypeRef, TableIndex.TypeDef, TableIndex.Param, TableIndex.InterfaceImpl, TableIndex.MemberRef, TableIndex.Module, TableIndex.DeclSecurity, TableIndex.Property, TableIndex.Event, TableIndex.StandAloneSig, TableIndex.ModuleRef, TableIndex.TypeSpec, TableIndex.Assembly, TableIndex.AssemblyRef, TableIndex.File, TableIndex.ExportedType, TableIndex.ManifestResource, TableIndex.GenericParam, TableIndex.GenericParamConstraint, TableIndex.MethodSpec, TableIndex.Document, TableIndex.LocalScope, TableIndex.LocalVariable, TableIndex.LocalConstant, TableIndex.ImportScope);
		int num = CalculateTableStreamHeaderSize();
		byte b = (byte)(BlobReferenceIsSmall ? 2 : 4);
		byte b2 = (byte)(StringReferenceIsSmall ? 2 : 4);
		byte b3 = (byte)(GuidReferenceIsSmall ? 2 : 4);
		byte b4 = (byte)(CustomAttributeTypeCodedIndexIsSmall ? 2 : 4);
		byte b5 = (byte)(DeclSecurityCodedIndexIsSmall ? 2 : 4);
		byte b6 = (byte)(EventDefReferenceIsSmall ? 2 : 4);
		byte b7 = (byte)(FieldDefReferenceIsSmall ? 2 : 4);
		byte b8 = (byte)(GenericParamReferenceIsSmall ? 2 : 4);
		byte b9 = (byte)(HasConstantCodedIndexIsSmall ? 2 : 4);
		byte b10 = (byte)(HasCustomAttributeCodedIndexIsSmall ? 2 : 4);
		byte b11 = (byte)(HasFieldMarshalCodedIndexIsSmall ? 2 : 4);
		byte b12 = (byte)(HasSemanticsCodedIndexIsSmall ? 2 : 4);
		byte b13 = (byte)(ImplementationCodedIndexIsSmall ? 2 : 4);
		byte b14 = (byte)(MemberForwardedCodedIndexIsSmall ? 2 : 4);
		byte b15 = (byte)(MemberRefParentCodedIndexIsSmall ? 2 : 4);
		byte b16 = (byte)(MethodDefReferenceIsSmall ? 2 : 4);
		byte b17 = (byte)(MethodDefOrRefCodedIndexIsSmall ? 2 : 4);
		byte b18 = (byte)(ModuleRefReferenceIsSmall ? 2 : 4);
		byte b19 = (byte)(ParameterReferenceIsSmall ? 2 : 4);
		byte b20 = (byte)(PropertyDefReferenceIsSmall ? 2 : 4);
		byte b21 = (byte)(ResolutionScopeCodedIndexIsSmall ? 2 : 4);
		byte b22 = (byte)(TypeDefReferenceIsSmall ? 2 : 4);
		byte b23 = (byte)(TypeDefOrRefCodedIndexIsSmall ? 2 : 4);
		byte b24 = (byte)(TypeOrMethodDefCodedIndexIsSmall ? 2 : 4);
		byte b25 = (byte)(DocumentReferenceIsSmall ? 2 : 4);
		byte b26 = (byte)(LocalVariableReferenceIsSmall ? 2 : 4);
		byte b27 = (byte)(LocalConstantReferenceIsSmall ? 2 : 4);
		byte b28 = (byte)(ImportScopeReferenceIsSmall ? 2 : 4);
		byte b29 = (byte)(HasCustomDebugInformationCodedIndexIsSmall ? 2 : 4);
		num += GetTableSize(TableIndex.Module, 2 + 3 * b3 + b2);
		num += GetTableSize(TableIndex.TypeRef, b21 + b2 + b2);
		num += GetTableSize(TableIndex.TypeDef, 4 + b2 + b2 + b23 + b7 + b16);
		num += GetTableSize(TableIndex.Field, 2 + b2 + b);
		num += GetTableSize(TableIndex.MethodDef, 8 + b2 + b + b19);
		num += GetTableSize(TableIndex.Param, 4 + b2);
		num += GetTableSize(TableIndex.InterfaceImpl, b22 + b23);
		num += GetTableSize(TableIndex.MemberRef, b15 + b2 + b);
		num += GetTableSize(TableIndex.Constant, 2 + b9 + b);
		num += GetTableSize(TableIndex.CustomAttribute, b10 + b4 + b);
		num += GetTableSize(TableIndex.FieldMarshal, b11 + b);
		num += GetTableSize(TableIndex.DeclSecurity, 2 + b5 + b);
		num += GetTableSize(TableIndex.ClassLayout, 6 + b22);
		num += GetTableSize(TableIndex.FieldLayout, 4 + b7);
		num += GetTableSize(TableIndex.StandAloneSig, b);
		num += GetTableSize(TableIndex.EventMap, b22 + b6);
		num += GetTableSize(TableIndex.Event, 2 + b2 + b23);
		num += GetTableSize(TableIndex.PropertyMap, b22 + b20);
		num += GetTableSize(TableIndex.Property, 2 + b2 + b);
		num += GetTableSize(TableIndex.MethodSemantics, 2 + b16 + b12);
		num += GetTableSize(TableIndex.MethodImpl, b22 + b17 + b17);
		num += GetTableSize(TableIndex.ModuleRef, b2);
		num += GetTableSize(TableIndex.TypeSpec, b);
		num += GetTableSize(TableIndex.ImplMap, 2 + b14 + b2 + b18);
		num += GetTableSize(TableIndex.FieldRva, 4 + b7);
		num += GetTableSize(TableIndex.EncLog, 8);
		num += GetTableSize(TableIndex.EncMap, 4);
		num += GetTableSize(TableIndex.Assembly, 16 + b + b2 + b2);
		num += GetTableSize(TableIndex.AssemblyRef, 12 + b + b2 + b2 + b);
		num += GetTableSize(TableIndex.File, 4 + b2 + b);
		num += GetTableSize(TableIndex.ExportedType, 8 + b2 + b2 + b13);
		num += GetTableSize(TableIndex.ManifestResource, 8 + b2 + b13);
		num += GetTableSize(TableIndex.NestedClass, b22 + b22);
		num += GetTableSize(TableIndex.GenericParam, 4 + b24 + b2);
		num += GetTableSize(TableIndex.MethodSpec, b17 + b);
		num += GetTableSize(TableIndex.GenericParamConstraint, b8 + b23);
		num += GetTableSize(TableIndex.Document, b + b3 + b + b3);
		num += GetTableSize(TableIndex.MethodDebugInformation, b25 + b);
		num += GetTableSize(TableIndex.LocalScope, b16 + b28 + b26 + b27 + 4 + 4);
		num += GetTableSize(TableIndex.LocalVariable, 4 + b2);
		num += GetTableSize(TableIndex.LocalConstant, b2 + b);
		num += GetTableSize(TableIndex.ImportScope, b28 + b);
		num += GetTableSize(TableIndex.StateMachineMethod, b16 + b16);
		num += GetTableSize(TableIndex.CustomDebugInformation, b29 + b3 + b);
		num = (MetadataTableStreamSize = BitArithmetic.Align(num + 1, 4)) + GetAlignedHeapSize(HeapIndex.String) + GetAlignedHeapSize(HeapIndex.UserString) + GetAlignedHeapSize(HeapIndex.Guid) + GetAlignedHeapSize(HeapIndex.Blob);
		StandalonePdbStreamSize = (isStandaloneDebugMetadata ? CalculateStandalonePdbStreamSize() : 0);
		num += StandalonePdbStreamSize;
		MetadataStreamStorageSize = num;
	}

	internal bool IsPresent(TableIndex table)
	{
		return (PresentTablesMask & (ulong)(1L << (int)table)) != 0;
	}

	internal static int GetMetadataStreamHeaderSize(string streamName)
	{
		return 8 + BitArithmetic.Align(streamName.Length + 1, 4);
	}

	public int GetAlignedHeapSize(HeapIndex index)
	{
		if (index < HeapIndex.UserString || (int)index > HeapSizes.Length)
		{
			Throw.ArgumentOutOfRange("index");
		}
		return BitArithmetic.Align(HeapSizes[(int)index], 4);
	}

	internal int CalculateTableStreamHeaderSize()
	{
		int num = 24;
		for (int i = 0; i < RowCounts.Length; i++)
		{
			if (((ulong)(1L << i) & PresentTablesMask) != 0L)
			{
				num += 4;
			}
		}
		return num;
	}

	internal int CalculateStandalonePdbStreamSize()
	{
		return 32 + BitArithmetic.CountBits(ExternalTablesMask) * 4;
	}

	private static ulong ComputeNonEmptyTableMask(ImmutableArray<int> rowCounts)
	{
		ulong num = 0uL;
		for (int i = 0; i < rowCounts.Length; i++)
		{
			if (rowCounts[i] > 0)
			{
				num |= (ulong)(1L << i);
			}
		}
		return num;
	}

	private int GetTableSize(TableIndex index, int rowSize)
	{
		return RowCounts[(int)index] * rowSize;
	}

	private bool IsReferenceSmall(int tagBitSize, params TableIndex[] tables)
	{
		if (IsCompressed)
		{
			return ReferenceFits(16 - tagBitSize, tables);
		}
		return false;
	}

	private bool ReferenceFits(int bitCount, TableIndex[] tables)
	{
		int num = (1 << bitCount) - 1;
		foreach (TableIndex index in tables)
		{
			if (RowCounts[(int)index] + ExternalRowCounts[(int)index] > num)
			{
				return false;
			}
		}
		return true;
	}
}
