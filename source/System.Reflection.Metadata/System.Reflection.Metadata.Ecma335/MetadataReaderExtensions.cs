using System.Collections.Generic;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

public static class MetadataReaderExtensions
{
	public static int GetTableRowCount(this MetadataReader reader, TableIndex tableIndex)
	{
		if (reader == null)
		{
			Throw.ArgumentNull("reader");
		}
		if ((int)tableIndex >= MetadataTokens.TableCount)
		{
			Throw.TableIndexOutOfRange();
		}
		return reader.TableRowCounts[(uint)tableIndex];
	}

	public static int GetTableRowSize(this MetadataReader reader, TableIndex tableIndex)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		return tableIndex switch
		{
			TableIndex.Module => reader.ModuleTable.RowSize, 
			TableIndex.TypeRef => reader.TypeRefTable.RowSize, 
			TableIndex.TypeDef => reader.TypeDefTable.RowSize, 
			TableIndex.FieldPtr => reader.FieldPtrTable.RowSize, 
			TableIndex.Field => reader.FieldTable.RowSize, 
			TableIndex.MethodPtr => reader.MethodPtrTable.RowSize, 
			TableIndex.MethodDef => reader.MethodDefTable.RowSize, 
			TableIndex.ParamPtr => reader.ParamPtrTable.RowSize, 
			TableIndex.Param => reader.ParamTable.RowSize, 
			TableIndex.InterfaceImpl => reader.InterfaceImplTable.RowSize, 
			TableIndex.MemberRef => reader.MemberRefTable.RowSize, 
			TableIndex.Constant => reader.ConstantTable.RowSize, 
			TableIndex.CustomAttribute => reader.CustomAttributeTable.RowSize, 
			TableIndex.FieldMarshal => reader.FieldMarshalTable.RowSize, 
			TableIndex.DeclSecurity => reader.DeclSecurityTable.RowSize, 
			TableIndex.ClassLayout => reader.ClassLayoutTable.RowSize, 
			TableIndex.FieldLayout => reader.FieldLayoutTable.RowSize, 
			TableIndex.StandAloneSig => reader.StandAloneSigTable.RowSize, 
			TableIndex.EventMap => reader.EventMapTable.RowSize, 
			TableIndex.EventPtr => reader.EventPtrTable.RowSize, 
			TableIndex.Event => reader.EventTable.RowSize, 
			TableIndex.PropertyMap => reader.PropertyMapTable.RowSize, 
			TableIndex.PropertyPtr => reader.PropertyPtrTable.RowSize, 
			TableIndex.Property => reader.PropertyTable.RowSize, 
			TableIndex.MethodSemantics => reader.MethodSemanticsTable.RowSize, 
			TableIndex.MethodImpl => reader.MethodImplTable.RowSize, 
			TableIndex.ModuleRef => reader.ModuleRefTable.RowSize, 
			TableIndex.TypeSpec => reader.TypeSpecTable.RowSize, 
			TableIndex.ImplMap => reader.ImplMapTable.RowSize, 
			TableIndex.FieldRva => reader.FieldRvaTable.RowSize, 
			TableIndex.EncLog => reader.EncLogTable.RowSize, 
			TableIndex.EncMap => reader.EncMapTable.RowSize, 
			TableIndex.Assembly => reader.AssemblyTable.RowSize, 
			TableIndex.AssemblyProcessor => reader.AssemblyProcessorTable.RowSize, 
			TableIndex.AssemblyOS => reader.AssemblyOSTable.RowSize, 
			TableIndex.AssemblyRef => reader.AssemblyRefTable.RowSize, 
			TableIndex.AssemblyRefProcessor => reader.AssemblyRefProcessorTable.RowSize, 
			TableIndex.AssemblyRefOS => reader.AssemblyRefOSTable.RowSize, 
			TableIndex.File => reader.FileTable.RowSize, 
			TableIndex.ExportedType => reader.ExportedTypeTable.RowSize, 
			TableIndex.ManifestResource => reader.ManifestResourceTable.RowSize, 
			TableIndex.NestedClass => reader.NestedClassTable.RowSize, 
			TableIndex.GenericParam => reader.GenericParamTable.RowSize, 
			TableIndex.MethodSpec => reader.MethodSpecTable.RowSize, 
			TableIndex.GenericParamConstraint => reader.GenericParamConstraintTable.RowSize, 
			TableIndex.Document => reader.DocumentTable.RowSize, 
			TableIndex.MethodDebugInformation => reader.MethodDebugInformationTable.RowSize, 
			TableIndex.LocalScope => reader.LocalScopeTable.RowSize, 
			TableIndex.LocalVariable => reader.LocalVariableTable.RowSize, 
			TableIndex.LocalConstant => reader.LocalConstantTable.RowSize, 
			TableIndex.ImportScope => reader.ImportScopeTable.RowSize, 
			TableIndex.StateMachineMethod => reader.StateMachineMethodTable.RowSize, 
			TableIndex.CustomDebugInformation => reader.CustomDebugInformationTable.RowSize, 
			_ => throw new ArgumentOutOfRangeException("tableIndex"), 
		};
	}

	public unsafe static int GetTableMetadataOffset(this MetadataReader reader, TableIndex tableIndex)
	{
		if (reader == null)
		{
			Throw.ArgumentNull("reader");
		}
		return (int)(reader.GetTableMetadataBlock(tableIndex).Pointer - reader.Block.Pointer);
	}

	private static MemoryBlock GetTableMetadataBlock(this MetadataReader reader, TableIndex tableIndex)
	{
		return tableIndex switch
		{
			TableIndex.Module => reader.ModuleTable.Block, 
			TableIndex.TypeRef => reader.TypeRefTable.Block, 
			TableIndex.TypeDef => reader.TypeDefTable.Block, 
			TableIndex.FieldPtr => reader.FieldPtrTable.Block, 
			TableIndex.Field => reader.FieldTable.Block, 
			TableIndex.MethodPtr => reader.MethodPtrTable.Block, 
			TableIndex.MethodDef => reader.MethodDefTable.Block, 
			TableIndex.ParamPtr => reader.ParamPtrTable.Block, 
			TableIndex.Param => reader.ParamTable.Block, 
			TableIndex.InterfaceImpl => reader.InterfaceImplTable.Block, 
			TableIndex.MemberRef => reader.MemberRefTable.Block, 
			TableIndex.Constant => reader.ConstantTable.Block, 
			TableIndex.CustomAttribute => reader.CustomAttributeTable.Block, 
			TableIndex.FieldMarshal => reader.FieldMarshalTable.Block, 
			TableIndex.DeclSecurity => reader.DeclSecurityTable.Block, 
			TableIndex.ClassLayout => reader.ClassLayoutTable.Block, 
			TableIndex.FieldLayout => reader.FieldLayoutTable.Block, 
			TableIndex.StandAloneSig => reader.StandAloneSigTable.Block, 
			TableIndex.EventMap => reader.EventMapTable.Block, 
			TableIndex.EventPtr => reader.EventPtrTable.Block, 
			TableIndex.Event => reader.EventTable.Block, 
			TableIndex.PropertyMap => reader.PropertyMapTable.Block, 
			TableIndex.PropertyPtr => reader.PropertyPtrTable.Block, 
			TableIndex.Property => reader.PropertyTable.Block, 
			TableIndex.MethodSemantics => reader.MethodSemanticsTable.Block, 
			TableIndex.MethodImpl => reader.MethodImplTable.Block, 
			TableIndex.ModuleRef => reader.ModuleRefTable.Block, 
			TableIndex.TypeSpec => reader.TypeSpecTable.Block, 
			TableIndex.ImplMap => reader.ImplMapTable.Block, 
			TableIndex.FieldRva => reader.FieldRvaTable.Block, 
			TableIndex.EncLog => reader.EncLogTable.Block, 
			TableIndex.EncMap => reader.EncMapTable.Block, 
			TableIndex.Assembly => reader.AssemblyTable.Block, 
			TableIndex.AssemblyProcessor => reader.AssemblyProcessorTable.Block, 
			TableIndex.AssemblyOS => reader.AssemblyOSTable.Block, 
			TableIndex.AssemblyRef => reader.AssemblyRefTable.Block, 
			TableIndex.AssemblyRefProcessor => reader.AssemblyRefProcessorTable.Block, 
			TableIndex.AssemblyRefOS => reader.AssemblyRefOSTable.Block, 
			TableIndex.File => reader.FileTable.Block, 
			TableIndex.ExportedType => reader.ExportedTypeTable.Block, 
			TableIndex.ManifestResource => reader.ManifestResourceTable.Block, 
			TableIndex.NestedClass => reader.NestedClassTable.Block, 
			TableIndex.GenericParam => reader.GenericParamTable.Block, 
			TableIndex.MethodSpec => reader.MethodSpecTable.Block, 
			TableIndex.GenericParamConstraint => reader.GenericParamConstraintTable.Block, 
			TableIndex.Document => reader.DocumentTable.Block, 
			TableIndex.MethodDebugInformation => reader.MethodDebugInformationTable.Block, 
			TableIndex.LocalScope => reader.LocalScopeTable.Block, 
			TableIndex.LocalVariable => reader.LocalVariableTable.Block, 
			TableIndex.LocalConstant => reader.LocalConstantTable.Block, 
			TableIndex.ImportScope => reader.ImportScopeTable.Block, 
			TableIndex.StateMachineMethod => reader.StateMachineMethodTable.Block, 
			TableIndex.CustomDebugInformation => reader.CustomDebugInformationTable.Block, 
			_ => throw new ArgumentOutOfRangeException("tableIndex"), 
		};
	}

	public static int GetHeapSize(this MetadataReader reader, HeapIndex heapIndex)
	{
		if (reader == null)
		{
			Throw.ArgumentNull("reader");
		}
		return reader.GetMetadataBlock(heapIndex).Length;
	}

	public unsafe static int GetHeapMetadataOffset(this MetadataReader reader, HeapIndex heapIndex)
	{
		if (reader == null)
		{
			Throw.ArgumentNull("reader");
		}
		return (int)(reader.GetMetadataBlock(heapIndex).Pointer - reader.Block.Pointer);
	}

	private static MemoryBlock GetMetadataBlock(this MetadataReader reader, HeapIndex heapIndex)
	{
		return heapIndex switch
		{
			HeapIndex.UserString => reader.UserStringHeap.Block, 
			HeapIndex.String => reader.StringHeap.Block, 
			HeapIndex.Blob => reader.BlobHeap.Block, 
			HeapIndex.Guid => reader.GuidHeap.Block, 
			_ => throw new ArgumentOutOfRangeException("heapIndex"), 
		};
	}

	public static UserStringHandle GetNextHandle(this MetadataReader reader, UserStringHandle handle)
	{
		if (reader == null)
		{
			Throw.ArgumentNull("reader");
		}
		return reader.UserStringHeap.GetNextHandle(handle);
	}

	public static BlobHandle GetNextHandle(this MetadataReader reader, BlobHandle handle)
	{
		if (reader == null)
		{
			Throw.ArgumentNull("reader");
		}
		return reader.BlobHeap.GetNextHandle(handle);
	}

	public static StringHandle GetNextHandle(this MetadataReader reader, StringHandle handle)
	{
		if (reader == null)
		{
			Throw.ArgumentNull("reader");
		}
		return reader.StringHeap.GetNextHandle(handle);
	}

	public static IEnumerable<EditAndContinueLogEntry> GetEditAndContinueLogEntries(this MetadataReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		for (int rid = 1; rid <= reader.EncLogTable.NumberOfRows; rid++)
		{
			yield return new EditAndContinueLogEntry(new EntityHandle(reader.EncLogTable.GetToken(rid)), reader.EncLogTable.GetFuncCode(rid));
		}
	}

	public static IEnumerable<EntityHandle> GetEditAndContinueMapEntries(this MetadataReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		for (int rid = 1; rid <= reader.EncMapTable.NumberOfRows; rid++)
		{
			yield return new EntityHandle(reader.EncMapTable.GetToken(rid));
		}
	}

	public static IEnumerable<TypeDefinitionHandle> GetTypesWithProperties(this MetadataReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		for (int rid = 1; rid <= reader.PropertyMapTable.NumberOfRows; rid++)
		{
			yield return reader.PropertyMapTable.GetParentType(rid);
		}
	}

	public static IEnumerable<TypeDefinitionHandle> GetTypesWithEvents(this MetadataReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		for (int rid = 1; rid <= reader.EventMapTable.NumberOfRows; rid++)
		{
			yield return reader.EventMapTable.GetParentType(rid);
		}
	}

	public static SignatureTypeKind ResolveSignatureTypeKind(this MetadataReader reader, EntityHandle typeHandle, byte rawTypeKind)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		SignatureTypeKind signatureTypeKind = (SignatureTypeKind)rawTypeKind;
		switch (signatureTypeKind)
		{
		case SignatureTypeKind.Unknown:
			return SignatureTypeKind.Unknown;
		default:
			throw new ArgumentOutOfRangeException("rawTypeKind");
		case SignatureTypeKind.ValueType:
		case SignatureTypeKind.Class:
			switch (typeHandle.Kind)
			{
			case HandleKind.TypeDefinition:
				return signatureTypeKind;
			case HandleKind.TypeReference:
			{
				TypeRefSignatureTreatment signatureTreatment = reader.GetTypeReference((TypeReferenceHandle)typeHandle).SignatureTreatment;
				return signatureTreatment switch
				{
					TypeRefSignatureTreatment.ProjectedToClass => SignatureTypeKind.Class, 
					TypeRefSignatureTreatment.ProjectedToValueType => SignatureTypeKind.ValueType, 
					TypeRefSignatureTreatment.None => signatureTypeKind, 
					_ => throw ExceptionUtilities.UnexpectedValue(signatureTreatment), 
				};
			}
			case HandleKind.TypeSpecification:
				return SignatureTypeKind.Unknown;
			default:
				throw new ArgumentOutOfRangeException("typeHandle", System.SR.Format(System.SR.UnexpectedHandleKind, typeHandle.Kind));
			}
		}
	}
}
