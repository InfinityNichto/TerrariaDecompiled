namespace System.Reflection.Metadata.Ecma335;

public static class MetadataTokens
{
	public static readonly int TableCount = 64;

	public static readonly int HeapCount = 4;

	public static int GetRowNumber(this MetadataReader reader, EntityHandle handle)
	{
		if (handle.IsVirtual)
		{
			return MapVirtualHandleRowId(reader, handle);
		}
		return handle.RowId;
	}

	public static int GetHeapOffset(this MetadataReader reader, Handle handle)
	{
		if (!handle.IsHeapHandle)
		{
			Throw.HeapHandleRequired();
		}
		if (handle.IsVirtual)
		{
			return MapVirtualHandleRowId(reader, handle);
		}
		return handle.Offset;
	}

	public static int GetToken(this MetadataReader reader, EntityHandle handle)
	{
		if (handle.IsVirtual)
		{
			return (int)handle.Type | MapVirtualHandleRowId(reader, handle);
		}
		return handle.Token;
	}

	public static int GetToken(this MetadataReader reader, Handle handle)
	{
		if (!handle.IsEntityOrUserStringHandle)
		{
			Throw.EntityOrUserStringHandleRequired();
		}
		if (handle.IsVirtual)
		{
			return (int)handle.EntityHandleType | MapVirtualHandleRowId(reader, handle);
		}
		return handle.Token;
	}

	private static int MapVirtualHandleRowId(MetadataReader reader, Handle handle)
	{
		switch (handle.Kind)
		{
		case HandleKind.AssemblyReference:
			return reader.AssemblyRefTable.NumberOfNonVirtualRows + 1 + handle.RowId;
		case HandleKind.Blob:
		case HandleKind.String:
			throw new NotSupportedException(System.SR.CantGetOffsetForVirtualHeapHandle);
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(handle.Kind);
			return 0;
		}
	}

	public static int GetRowNumber(EntityHandle handle)
	{
		if (!handle.IsVirtual)
		{
			return handle.RowId;
		}
		return -1;
	}

	public static int GetHeapOffset(Handle handle)
	{
		if (!handle.IsHeapHandle)
		{
			Throw.HeapHandleRequired();
		}
		if (handle.IsVirtual)
		{
			return -1;
		}
		return handle.Offset;
	}

	public static int GetHeapOffset(BlobHandle handle)
	{
		if (!handle.IsVirtual)
		{
			return handle.GetHeapOffset();
		}
		return -1;
	}

	public static int GetHeapOffset(GuidHandle handle)
	{
		return handle.Index;
	}

	public static int GetHeapOffset(UserStringHandle handle)
	{
		return handle.GetHeapOffset();
	}

	public static int GetHeapOffset(StringHandle handle)
	{
		if (!handle.IsVirtual)
		{
			return handle.GetHeapOffset();
		}
		return -1;
	}

	public static int GetToken(Handle handle)
	{
		if (!handle.IsEntityOrUserStringHandle)
		{
			Throw.EntityOrUserStringHandleRequired();
		}
		if (handle.IsVirtual)
		{
			return 0;
		}
		return handle.Token;
	}

	public static int GetToken(EntityHandle handle)
	{
		if (!handle.IsVirtual)
		{
			return handle.Token;
		}
		return 0;
	}

	public static bool TryGetTableIndex(HandleKind type, out TableIndex index)
	{
		if ((int)type < TableCount && ((1L << (int)type) & 0xFF1FC9FFFFFFFFL) != 0L)
		{
			index = (TableIndex)type;
			return true;
		}
		index = TableIndex.Module;
		return false;
	}

	public static bool TryGetHeapIndex(HandleKind type, out HeapIndex index)
	{
		switch (type)
		{
		case HandleKind.UserString:
			index = HeapIndex.UserString;
			return true;
		case HandleKind.String:
		case HandleKind.NamespaceDefinition:
			index = HeapIndex.String;
			return true;
		case HandleKind.Blob:
			index = HeapIndex.Blob;
			return true;
		case HandleKind.Guid:
			index = HeapIndex.Guid;
			return true;
		default:
			index = HeapIndex.UserString;
			return false;
		}
	}

	public static Handle Handle(int token)
	{
		if (!TokenTypeIds.IsEntityOrUserStringToken((uint)token))
		{
			Throw.InvalidToken();
		}
		return System.Reflection.Metadata.Handle.FromVToken((uint)token);
	}

	public static EntityHandle EntityHandle(int token)
	{
		if (!TokenTypeIds.IsEntityToken((uint)token))
		{
			Throw.InvalidToken();
		}
		return new EntityHandle((uint)token);
	}

	public static EntityHandle EntityHandle(TableIndex tableIndex, int rowNumber)
	{
		return Handle(tableIndex, rowNumber);
	}

	public static EntityHandle Handle(TableIndex tableIndex, int rowNumber)
	{
		int vToken = (int)((uint)tableIndex << 24) | rowNumber;
		if (!TokenTypeIds.IsEntityOrUserStringToken((uint)vToken))
		{
			Throw.TableIndexOutOfRange();
		}
		return new EntityHandle((uint)vToken);
	}

	private static int ToRowId(int rowNumber)
	{
		return rowNumber & 0xFFFFFF;
	}

	public static MethodDefinitionHandle MethodDefinitionHandle(int rowNumber)
	{
		return System.Reflection.Metadata.MethodDefinitionHandle.FromRowId(ToRowId(rowNumber));
	}

	public static MethodImplementationHandle MethodImplementationHandle(int rowNumber)
	{
		return System.Reflection.Metadata.MethodImplementationHandle.FromRowId(ToRowId(rowNumber));
	}

	public static MethodSpecificationHandle MethodSpecificationHandle(int rowNumber)
	{
		return System.Reflection.Metadata.MethodSpecificationHandle.FromRowId(ToRowId(rowNumber));
	}

	public static TypeDefinitionHandle TypeDefinitionHandle(int rowNumber)
	{
		return System.Reflection.Metadata.TypeDefinitionHandle.FromRowId(ToRowId(rowNumber));
	}

	public static ExportedTypeHandle ExportedTypeHandle(int rowNumber)
	{
		return System.Reflection.Metadata.ExportedTypeHandle.FromRowId(ToRowId(rowNumber));
	}

	public static TypeReferenceHandle TypeReferenceHandle(int rowNumber)
	{
		return System.Reflection.Metadata.TypeReferenceHandle.FromRowId(ToRowId(rowNumber));
	}

	public static TypeSpecificationHandle TypeSpecificationHandle(int rowNumber)
	{
		return System.Reflection.Metadata.TypeSpecificationHandle.FromRowId(ToRowId(rowNumber));
	}

	public static InterfaceImplementationHandle InterfaceImplementationHandle(int rowNumber)
	{
		return System.Reflection.Metadata.InterfaceImplementationHandle.FromRowId(ToRowId(rowNumber));
	}

	public static MemberReferenceHandle MemberReferenceHandle(int rowNumber)
	{
		return System.Reflection.Metadata.MemberReferenceHandle.FromRowId(ToRowId(rowNumber));
	}

	public static FieldDefinitionHandle FieldDefinitionHandle(int rowNumber)
	{
		return System.Reflection.Metadata.FieldDefinitionHandle.FromRowId(ToRowId(rowNumber));
	}

	public static EventDefinitionHandle EventDefinitionHandle(int rowNumber)
	{
		return System.Reflection.Metadata.EventDefinitionHandle.FromRowId(ToRowId(rowNumber));
	}

	public static PropertyDefinitionHandle PropertyDefinitionHandle(int rowNumber)
	{
		return System.Reflection.Metadata.PropertyDefinitionHandle.FromRowId(ToRowId(rowNumber));
	}

	public static StandaloneSignatureHandle StandaloneSignatureHandle(int rowNumber)
	{
		return System.Reflection.Metadata.StandaloneSignatureHandle.FromRowId(ToRowId(rowNumber));
	}

	public static ParameterHandle ParameterHandle(int rowNumber)
	{
		return System.Reflection.Metadata.ParameterHandle.FromRowId(ToRowId(rowNumber));
	}

	public static GenericParameterHandle GenericParameterHandle(int rowNumber)
	{
		return System.Reflection.Metadata.GenericParameterHandle.FromRowId(ToRowId(rowNumber));
	}

	public static GenericParameterConstraintHandle GenericParameterConstraintHandle(int rowNumber)
	{
		return System.Reflection.Metadata.GenericParameterConstraintHandle.FromRowId(ToRowId(rowNumber));
	}

	public static ModuleReferenceHandle ModuleReferenceHandle(int rowNumber)
	{
		return System.Reflection.Metadata.ModuleReferenceHandle.FromRowId(ToRowId(rowNumber));
	}

	public static AssemblyReferenceHandle AssemblyReferenceHandle(int rowNumber)
	{
		return System.Reflection.Metadata.AssemblyReferenceHandle.FromRowId(ToRowId(rowNumber));
	}

	public static CustomAttributeHandle CustomAttributeHandle(int rowNumber)
	{
		return System.Reflection.Metadata.CustomAttributeHandle.FromRowId(ToRowId(rowNumber));
	}

	public static DeclarativeSecurityAttributeHandle DeclarativeSecurityAttributeHandle(int rowNumber)
	{
		return System.Reflection.Metadata.DeclarativeSecurityAttributeHandle.FromRowId(ToRowId(rowNumber));
	}

	public static ConstantHandle ConstantHandle(int rowNumber)
	{
		return System.Reflection.Metadata.ConstantHandle.FromRowId(ToRowId(rowNumber));
	}

	public static ManifestResourceHandle ManifestResourceHandle(int rowNumber)
	{
		return System.Reflection.Metadata.ManifestResourceHandle.FromRowId(ToRowId(rowNumber));
	}

	public static AssemblyFileHandle AssemblyFileHandle(int rowNumber)
	{
		return System.Reflection.Metadata.AssemblyFileHandle.FromRowId(ToRowId(rowNumber));
	}

	public static DocumentHandle DocumentHandle(int rowNumber)
	{
		return System.Reflection.Metadata.DocumentHandle.FromRowId(ToRowId(rowNumber));
	}

	public static MethodDebugInformationHandle MethodDebugInformationHandle(int rowNumber)
	{
		return System.Reflection.Metadata.MethodDebugInformationHandle.FromRowId(ToRowId(rowNumber));
	}

	public static LocalScopeHandle LocalScopeHandle(int rowNumber)
	{
		return System.Reflection.Metadata.LocalScopeHandle.FromRowId(ToRowId(rowNumber));
	}

	public static LocalVariableHandle LocalVariableHandle(int rowNumber)
	{
		return System.Reflection.Metadata.LocalVariableHandle.FromRowId(ToRowId(rowNumber));
	}

	public static LocalConstantHandle LocalConstantHandle(int rowNumber)
	{
		return System.Reflection.Metadata.LocalConstantHandle.FromRowId(ToRowId(rowNumber));
	}

	public static ImportScopeHandle ImportScopeHandle(int rowNumber)
	{
		return System.Reflection.Metadata.ImportScopeHandle.FromRowId(ToRowId(rowNumber));
	}

	public static CustomDebugInformationHandle CustomDebugInformationHandle(int rowNumber)
	{
		return System.Reflection.Metadata.CustomDebugInformationHandle.FromRowId(ToRowId(rowNumber));
	}

	public static UserStringHandle UserStringHandle(int offset)
	{
		return System.Reflection.Metadata.UserStringHandle.FromOffset(offset & 0xFFFFFF);
	}

	public static StringHandle StringHandle(int offset)
	{
		return System.Reflection.Metadata.StringHandle.FromOffset(offset);
	}

	public static BlobHandle BlobHandle(int offset)
	{
		return System.Reflection.Metadata.BlobHandle.FromOffset(offset);
	}

	public static GuidHandle GuidHandle(int offset)
	{
		return System.Reflection.Metadata.GuidHandle.FromIndex(offset);
	}

	public static DocumentNameBlobHandle DocumentNameBlobHandle(int offset)
	{
		return System.Reflection.Metadata.DocumentNameBlobHandle.FromOffset(offset);
	}
}
