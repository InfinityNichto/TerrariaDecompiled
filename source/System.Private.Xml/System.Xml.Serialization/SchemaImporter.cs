using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

public abstract class SchemaImporter
{
	private XmlSchemas _schemas;

	private StructMapping _root;

	private readonly CodeGenerationOptions _options;

	private TypeScope _scope;

	private ImportContext _context;

	private bool _rootImported;

	private NameTable _typesInUse;

	private NameTable _groupsInUse;

	internal ImportContext Context
	{
		get
		{
			if (_context == null)
			{
				_context = new ImportContext();
			}
			return _context;
		}
	}

	internal Hashtable ImportedElements => Context.Elements;

	internal Hashtable ImportedMappings => Context.Mappings;

	internal CodeIdentifiers TypeIdentifiers => Context.TypeIdentifiers;

	internal XmlSchemas Schemas
	{
		get
		{
			if (_schemas == null)
			{
				_schemas = new XmlSchemas();
			}
			return _schemas;
		}
	}

	internal TypeScope Scope
	{
		get
		{
			if (_scope == null)
			{
				_scope = new TypeScope();
			}
			return _scope;
		}
	}

	internal NameTable GroupsInUse
	{
		get
		{
			if (_groupsInUse == null)
			{
				_groupsInUse = new NameTable();
			}
			return _groupsInUse;
		}
	}

	internal NameTable TypesInUse
	{
		get
		{
			if (_typesInUse == null)
			{
				_typesInUse = new NameTable();
			}
			return _typesInUse;
		}
	}

	internal CodeGenerationOptions Options => _options;

	[RequiresUnreferencedCode("calls SetCache")]
	internal SchemaImporter(XmlSchemas schemas, CodeGenerationOptions options, ImportContext context)
	{
		if (!schemas.Contains("http://www.w3.org/2001/XMLSchema"))
		{
			schemas.AddReference(XmlSchemas.XsdSchema);
			schemas.SchemaSet.Add(XmlSchemas.XsdSchema);
		}
		if (!schemas.Contains("http://www.w3.org/XML/1998/namespace"))
		{
			schemas.AddReference(XmlSchemas.XmlSchema);
			schemas.SchemaSet.Add(XmlSchemas.XmlSchema);
		}
		_schemas = schemas;
		_options = options;
		_context = context;
		Schemas.SetCache(Context.Cache, Context.ShareTypes);
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	internal void MakeDerived(StructMapping structMapping, Type baseType, bool baseTypeCanBeIndirect)
	{
		structMapping.ReferencedByTopLevelElement = true;
		if (!(baseType != null))
		{
			return;
		}
		TypeDesc typeDesc = Scope.GetTypeDesc(baseType);
		if (typeDesc == null)
		{
			return;
		}
		TypeDesc typeDesc2 = structMapping.TypeDesc;
		if (baseTypeCanBeIndirect)
		{
			while (typeDesc2.BaseTypeDesc != null && typeDesc2.BaseTypeDesc != typeDesc)
			{
				typeDesc2 = typeDesc2.BaseTypeDesc;
			}
		}
		if (typeDesc2.BaseTypeDesc != null && typeDesc2.BaseTypeDesc != typeDesc)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidBaseType, structMapping.TypeDesc.FullName, baseType.FullName, typeDesc2.BaseTypeDesc.FullName));
		}
		typeDesc2.BaseTypeDesc = typeDesc;
	}

	internal string GenerateUniqueTypeName(string typeName)
	{
		typeName = CodeIdentifier.MakeValid(typeName);
		return TypeIdentifiers.AddUnique(typeName, typeName);
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private StructMapping CreateRootMapping()
	{
		TypeDesc typeDesc = Scope.GetTypeDesc(typeof(object));
		StructMapping structMapping = new StructMapping();
		structMapping.TypeDesc = typeDesc;
		structMapping.Members = Array.Empty<MemberMapping>();
		structMapping.IncludeInSchema = false;
		structMapping.TypeName = "anyType";
		structMapping.Namespace = "http://www.w3.org/2001/XMLSchema";
		return structMapping;
	}

	[RequiresUnreferencedCode("calls CreateRootMapping")]
	internal StructMapping GetRootMapping()
	{
		if (_root == null)
		{
			_root = CreateRootMapping();
		}
		return _root;
	}

	[RequiresUnreferencedCode("calls GetRootMapping")]
	internal StructMapping ImportRootMapping()
	{
		if (!_rootImported)
		{
			_rootImported = true;
			ImportDerivedTypes(XmlQualifiedName.Empty);
		}
		return GetRootMapping();
	}

	[RequiresUnreferencedCode("calls ImportType")]
	internal abstract void ImportDerivedTypes(XmlQualifiedName baseName);

	internal void AddReference(XmlQualifiedName name, NameTable references, string error)
	{
		if (!(name.Namespace == "http://www.w3.org/2001/XMLSchema"))
		{
			if (references[name] != null)
			{
				throw new InvalidOperationException(string.Format(error, name.Name, name.Namespace));
			}
			references[name] = name;
		}
	}

	internal void RemoveReference(XmlQualifiedName name, NameTable references)
	{
		references[name] = null;
	}

	internal void AddReservedIdentifiersForDataBinding(CodeIdentifiers scope)
	{
		if ((_options & CodeGenerationOptions.EnableDataBinding) != 0)
		{
			scope.AddReserved("PropertyChanged");
			scope.AddReserved("RaisePropertyChanged");
		}
	}
}
