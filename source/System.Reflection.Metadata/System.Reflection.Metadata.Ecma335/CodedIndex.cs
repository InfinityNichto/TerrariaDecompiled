namespace System.Reflection.Metadata.Ecma335;

public static class CodedIndex
{
	private enum HasCustomAttributeTag
	{
		MethodDef = 0,
		Field = 1,
		TypeRef = 2,
		TypeDef = 3,
		Param = 4,
		InterfaceImpl = 5,
		MemberRef = 6,
		Module = 7,
		DeclSecurity = 8,
		Property = 9,
		Event = 10,
		StandAloneSig = 11,
		ModuleRef = 12,
		TypeSpec = 13,
		Assembly = 14,
		AssemblyRef = 15,
		File = 16,
		ExportedType = 17,
		ManifestResource = 18,
		GenericParam = 19,
		GenericParamConstraint = 20,
		MethodSpec = 21,
		BitCount = 5
	}

	private enum HasConstantTag
	{
		Field = 0,
		Param = 1,
		Property = 2,
		BitCount = 2
	}

	private enum CustomAttributeTypeTag
	{
		MethodDef = 2,
		MemberRef = 3,
		BitCount = 3
	}

	private enum HasDeclSecurityTag
	{
		TypeDef = 0,
		MethodDef = 1,
		Assembly = 2,
		BitCount = 2
	}

	private enum HasFieldMarshalTag
	{
		Field = 0,
		Param = 1,
		BitCount = 1
	}

	private enum HasSemanticsTag
	{
		Event = 0,
		Property = 1,
		BitCount = 1
	}

	private enum ImplementationTag
	{
		File = 0,
		AssemblyRef = 1,
		ExportedType = 2,
		BitCount = 2
	}

	private enum MemberForwardedTag
	{
		Field = 0,
		MethodDef = 1,
		BitCount = 1
	}

	private enum MemberRefParentTag
	{
		TypeDef = 0,
		TypeRef = 1,
		ModuleRef = 2,
		MethodDef = 3,
		TypeSpec = 4,
		BitCount = 3
	}

	private enum MethodDefOrRefTag
	{
		MethodDef = 0,
		MemberRef = 1,
		BitCount = 1
	}

	private enum ResolutionScopeTag
	{
		Module = 0,
		ModuleRef = 1,
		AssemblyRef = 2,
		TypeRef = 3,
		BitCount = 2
	}

	private enum TypeDefOrRefOrSpecTag
	{
		TypeDef = 0,
		TypeRef = 1,
		TypeSpec = 2,
		BitCount = 2
	}

	private enum TypeDefOrRefTag
	{
		TypeDef,
		TypeRef,
		BitCount
	}

	private enum TypeOrMethodDefTag
	{
		TypeDef = 0,
		MethodDef = 1,
		BitCount = 1
	}

	private enum HasCustomDebugInformationTag
	{
		MethodDef = 0,
		Field = 1,
		TypeRef = 2,
		TypeDef = 3,
		Param = 4,
		InterfaceImpl = 5,
		MemberRef = 6,
		Module = 7,
		DeclSecurity = 8,
		Property = 9,
		Event = 10,
		StandAloneSig = 11,
		ModuleRef = 12,
		TypeSpec = 13,
		Assembly = 14,
		AssemblyRef = 15,
		File = 16,
		ExportedType = 17,
		ManifestResource = 18,
		GenericParam = 19,
		GenericParamConstraint = 20,
		MethodSpec = 21,
		Document = 22,
		LocalScope = 23,
		LocalVariable = 24,
		LocalConstant = 25,
		ImportScope = 26,
		BitCount = 5
	}

	public static int HasCustomAttribute(EntityHandle handle)
	{
		return (handle.RowId << 5) | (int)ToHasCustomAttributeTag(handle.Kind);
	}

	public static int HasConstant(EntityHandle handle)
	{
		return (handle.RowId << 2) | (int)ToHasConstantTag(handle.Kind);
	}

	public static int CustomAttributeType(EntityHandle handle)
	{
		return (handle.RowId << 3) | (int)ToCustomAttributeTypeTag(handle.Kind);
	}

	public static int HasDeclSecurity(EntityHandle handle)
	{
		return (handle.RowId << 2) | (int)ToHasDeclSecurityTag(handle.Kind);
	}

	public static int HasFieldMarshal(EntityHandle handle)
	{
		return (handle.RowId << 1) | (int)ToHasFieldMarshalTag(handle.Kind);
	}

	public static int HasSemantics(EntityHandle handle)
	{
		return (handle.RowId << 1) | (int)ToHasSemanticsTag(handle.Kind);
	}

	public static int Implementation(EntityHandle handle)
	{
		return (handle.RowId << 2) | (int)ToImplementationTag(handle.Kind);
	}

	public static int MemberForwarded(EntityHandle handle)
	{
		return (handle.RowId << 1) | (int)ToMemberForwardedTag(handle.Kind);
	}

	public static int MemberRefParent(EntityHandle handle)
	{
		return (handle.RowId << 3) | (int)ToMemberRefParentTag(handle.Kind);
	}

	public static int MethodDefOrRef(EntityHandle handle)
	{
		return (handle.RowId << 1) | (int)ToMethodDefOrRefTag(handle.Kind);
	}

	public static int ResolutionScope(EntityHandle handle)
	{
		return (handle.RowId << 2) | (int)ToResolutionScopeTag(handle.Kind);
	}

	public static int TypeDefOrRef(EntityHandle handle)
	{
		return (handle.RowId << 2) | (int)ToTypeDefOrRefTag(handle.Kind);
	}

	public static int TypeDefOrRefOrSpec(EntityHandle handle)
	{
		return (handle.RowId << 2) | (int)ToTypeDefOrRefOrSpecTag(handle.Kind);
	}

	public static int TypeOrMethodDef(EntityHandle handle)
	{
		return (handle.RowId << 1) | (int)ToTypeOrMethodDefTag(handle.Kind);
	}

	public static int HasCustomDebugInformation(EntityHandle handle)
	{
		return (handle.RowId << 5) | (int)ToHasCustomDebugInformationTag(handle.Kind);
	}

	private static HasCustomAttributeTag ToHasCustomAttributeTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.MethodDefinition:
			return HasCustomAttributeTag.MethodDef;
		case HandleKind.FieldDefinition:
			return HasCustomAttributeTag.Field;
		case HandleKind.TypeReference:
			return HasCustomAttributeTag.TypeRef;
		case HandleKind.TypeDefinition:
			return HasCustomAttributeTag.TypeDef;
		case HandleKind.Parameter:
			return HasCustomAttributeTag.Param;
		case HandleKind.InterfaceImplementation:
			return HasCustomAttributeTag.InterfaceImpl;
		case HandleKind.MemberReference:
			return HasCustomAttributeTag.MemberRef;
		case HandleKind.ModuleDefinition:
			return HasCustomAttributeTag.Module;
		case HandleKind.DeclarativeSecurityAttribute:
			return HasCustomAttributeTag.DeclSecurity;
		case HandleKind.PropertyDefinition:
			return HasCustomAttributeTag.Property;
		case HandleKind.EventDefinition:
			return HasCustomAttributeTag.Event;
		case HandleKind.StandaloneSignature:
			return HasCustomAttributeTag.StandAloneSig;
		case HandleKind.ModuleReference:
			return HasCustomAttributeTag.ModuleRef;
		case HandleKind.TypeSpecification:
			return HasCustomAttributeTag.TypeSpec;
		case HandleKind.AssemblyDefinition:
			return HasCustomAttributeTag.Assembly;
		case HandleKind.AssemblyReference:
			return HasCustomAttributeTag.AssemblyRef;
		case HandleKind.AssemblyFile:
			return HasCustomAttributeTag.File;
		case HandleKind.ExportedType:
			return HasCustomAttributeTag.ExportedType;
		case HandleKind.ManifestResource:
			return HasCustomAttributeTag.ManifestResource;
		case HandleKind.GenericParameter:
			return HasCustomAttributeTag.GenericParam;
		case HandleKind.GenericParameterConstraint:
			return HasCustomAttributeTag.GenericParamConstraint;
		case HandleKind.MethodSpecification:
			return HasCustomAttributeTag.MethodSpec;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return HasCustomAttributeTag.MethodDef;
		}
	}

	private static HasConstantTag ToHasConstantTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.FieldDefinition:
			return HasConstantTag.Field;
		case HandleKind.Parameter:
			return HasConstantTag.Param;
		case HandleKind.PropertyDefinition:
			return HasConstantTag.Property;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return HasConstantTag.Field;
		}
	}

	private static CustomAttributeTypeTag ToCustomAttributeTypeTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.MethodDefinition:
			return CustomAttributeTypeTag.MethodDef;
		case HandleKind.MemberReference:
			return CustomAttributeTypeTag.MemberRef;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return (CustomAttributeTypeTag)0;
		}
	}

	private static HasDeclSecurityTag ToHasDeclSecurityTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.TypeDefinition:
			return HasDeclSecurityTag.TypeDef;
		case HandleKind.MethodDefinition:
			return HasDeclSecurityTag.MethodDef;
		case HandleKind.AssemblyDefinition:
			return HasDeclSecurityTag.Assembly;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return HasDeclSecurityTag.TypeDef;
		}
	}

	private static HasFieldMarshalTag ToHasFieldMarshalTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.FieldDefinition:
			return HasFieldMarshalTag.Field;
		case HandleKind.Parameter:
			return HasFieldMarshalTag.Param;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return HasFieldMarshalTag.Field;
		}
	}

	private static HasSemanticsTag ToHasSemanticsTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.EventDefinition:
			return HasSemanticsTag.Event;
		case HandleKind.PropertyDefinition:
			return HasSemanticsTag.Property;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return HasSemanticsTag.Event;
		}
	}

	private static ImplementationTag ToImplementationTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.AssemblyFile:
			return ImplementationTag.File;
		case HandleKind.AssemblyReference:
			return ImplementationTag.AssemblyRef;
		case HandleKind.ExportedType:
			return ImplementationTag.ExportedType;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return ImplementationTag.File;
		}
	}

	private static MemberForwardedTag ToMemberForwardedTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.FieldDefinition:
			return MemberForwardedTag.Field;
		case HandleKind.MethodDefinition:
			return MemberForwardedTag.MethodDef;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return MemberForwardedTag.Field;
		}
	}

	private static MemberRefParentTag ToMemberRefParentTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.TypeDefinition:
			return MemberRefParentTag.TypeDef;
		case HandleKind.TypeReference:
			return MemberRefParentTag.TypeRef;
		case HandleKind.ModuleReference:
			return MemberRefParentTag.ModuleRef;
		case HandleKind.MethodDefinition:
			return MemberRefParentTag.MethodDef;
		case HandleKind.TypeSpecification:
			return MemberRefParentTag.TypeSpec;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return MemberRefParentTag.TypeDef;
		}
	}

	private static MethodDefOrRefTag ToMethodDefOrRefTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.MethodDefinition:
			return MethodDefOrRefTag.MethodDef;
		case HandleKind.MemberReference:
			return MethodDefOrRefTag.MemberRef;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return MethodDefOrRefTag.MethodDef;
		}
	}

	private static ResolutionScopeTag ToResolutionScopeTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.TypeReference:
			if (kind != HandleKind.TypeReference)
			{
				break;
			}
			return ResolutionScopeTag.TypeRef;
		case HandleKind.ModuleDefinition:
			return ResolutionScopeTag.Module;
		case HandleKind.ModuleReference:
			return ResolutionScopeTag.ModuleRef;
		case HandleKind.AssemblyReference:
			return ResolutionScopeTag.AssemblyRef;
		}
		Throw.InvalidArgument_UnexpectedHandleKind(kind);
		return ResolutionScopeTag.Module;
	}

	private static TypeDefOrRefOrSpecTag ToTypeDefOrRefOrSpecTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.TypeDefinition:
			return TypeDefOrRefOrSpecTag.TypeDef;
		case HandleKind.TypeReference:
			return TypeDefOrRefOrSpecTag.TypeRef;
		case HandleKind.TypeSpecification:
			return TypeDefOrRefOrSpecTag.TypeSpec;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return TypeDefOrRefOrSpecTag.TypeDef;
		}
	}

	private static TypeDefOrRefTag ToTypeDefOrRefTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.TypeDefinition:
			return TypeDefOrRefTag.TypeDef;
		case HandleKind.TypeReference:
			return TypeDefOrRefTag.TypeRef;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return TypeDefOrRefTag.TypeDef;
		}
	}

	private static TypeOrMethodDefTag ToTypeOrMethodDefTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.TypeDefinition:
			return TypeOrMethodDefTag.TypeDef;
		case HandleKind.MethodDefinition:
			return TypeOrMethodDefTag.MethodDef;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return TypeOrMethodDefTag.TypeDef;
		}
	}

	private static HasCustomDebugInformationTag ToHasCustomDebugInformationTag(HandleKind kind)
	{
		switch (kind)
		{
		case HandleKind.MethodDefinition:
			return HasCustomDebugInformationTag.MethodDef;
		case HandleKind.FieldDefinition:
			return HasCustomDebugInformationTag.Field;
		case HandleKind.TypeReference:
			return HasCustomDebugInformationTag.TypeRef;
		case HandleKind.TypeDefinition:
			return HasCustomDebugInformationTag.TypeDef;
		case HandleKind.Parameter:
			return HasCustomDebugInformationTag.Param;
		case HandleKind.InterfaceImplementation:
			return HasCustomDebugInformationTag.InterfaceImpl;
		case HandleKind.MemberReference:
			return HasCustomDebugInformationTag.MemberRef;
		case HandleKind.ModuleDefinition:
			return HasCustomDebugInformationTag.Module;
		case HandleKind.DeclarativeSecurityAttribute:
			return HasCustomDebugInformationTag.DeclSecurity;
		case HandleKind.PropertyDefinition:
			return HasCustomDebugInformationTag.Property;
		case HandleKind.EventDefinition:
			return HasCustomDebugInformationTag.Event;
		case HandleKind.StandaloneSignature:
			return HasCustomDebugInformationTag.StandAloneSig;
		case HandleKind.ModuleReference:
			return HasCustomDebugInformationTag.ModuleRef;
		case HandleKind.TypeSpecification:
			return HasCustomDebugInformationTag.TypeSpec;
		case HandleKind.AssemblyDefinition:
			return HasCustomDebugInformationTag.Assembly;
		case HandleKind.AssemblyReference:
			return HasCustomDebugInformationTag.AssemblyRef;
		case HandleKind.AssemblyFile:
			return HasCustomDebugInformationTag.File;
		case HandleKind.ExportedType:
			return HasCustomDebugInformationTag.ExportedType;
		case HandleKind.ManifestResource:
			return HasCustomDebugInformationTag.ManifestResource;
		case HandleKind.GenericParameter:
			return HasCustomDebugInformationTag.GenericParam;
		case HandleKind.GenericParameterConstraint:
			return HasCustomDebugInformationTag.GenericParamConstraint;
		case HandleKind.MethodSpecification:
			return HasCustomDebugInformationTag.MethodSpec;
		case HandleKind.Document:
			return HasCustomDebugInformationTag.Document;
		case HandleKind.LocalScope:
			return HasCustomDebugInformationTag.LocalScope;
		case HandleKind.LocalVariable:
			return HasCustomDebugInformationTag.LocalVariable;
		case HandleKind.LocalConstant:
			return HasCustomDebugInformationTag.LocalConstant;
		case HandleKind.ImportScope:
			return HasCustomDebugInformationTag.ImportScope;
		default:
			Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return HasCustomDebugInformationTag.MethodDef;
		}
	}
}
