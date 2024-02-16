using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml.Schema;

namespace System.Xml.Serialization;

public class XmlSchemaImporter : SchemaImporter
{
	private sealed class TypeItems
	{
		internal XmlSchemaObjectCollection Attributes = new XmlSchemaObjectCollection();

		internal XmlSchemaAnyAttribute AnyAttribute;

		internal XmlSchemaGroupBase Particle;

		internal XmlQualifiedName baseSimpleType;

		internal bool IsUnbounded;
	}

	internal sealed class ElementComparer : IComparer
	{
		public int Compare(object o1, object o2)
		{
			ElementAccessor elementAccessor = (ElementAccessor)o1;
			ElementAccessor elementAccessor2 = (ElementAccessor)o2;
			return string.Compare(elementAccessor.ToString(string.Empty), elementAccessor2.ToString(string.Empty), StringComparison.Ordinal);
		}
	}

	internal bool GenerateOrder => (base.Options & CodeGenerationOptions.GenerateOrder) != 0;

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSchemaImporter(XmlSchemas schemas)
		: base(schemas, CodeGenerationOptions.GenerateProperties, new ImportContext())
	{
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlSchemaImporter(XmlSchemas schemas, CodeIdentifiers? typeIdentifiers)
		: base(schemas, CodeGenerationOptions.GenerateProperties, new ImportContext(typeIdentifiers, shareTypes: false))
	{
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlTypeMapping ImportDerivedTypeMapping(XmlQualifiedName name, Type? baseType)
	{
		return ImportDerivedTypeMapping(name, baseType, baseTypeCanBeIndirect: false);
	}

	internal TypeMapping GetDefaultMapping(TypeFlags flags)
	{
		PrimitiveMapping primitiveMapping = new PrimitiveMapping();
		primitiveMapping.TypeDesc = base.Scope.GetTypeDesc("string", "http://www.w3.org/2001/XMLSchema", flags);
		primitiveMapping.TypeName = primitiveMapping.TypeDesc.DataType.Name;
		primitiveMapping.Namespace = "http://www.w3.org/2001/XMLSchema";
		return primitiveMapping;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlTypeMapping ImportDerivedTypeMapping(XmlQualifiedName name, Type? baseType, bool baseTypeCanBeIndirect)
	{
		ElementAccessor elementAccessor = ImportElement(name, typeof(TypeMapping), baseType);
		if (elementAccessor.Mapping is StructMapping)
		{
			MakeDerived((StructMapping)elementAccessor.Mapping, baseType, baseTypeCanBeIndirect);
		}
		else if (baseType != null)
		{
			if (!(elementAccessor.Mapping is ArrayMapping))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlBadBaseElement, name.Name, name.Namespace, baseType.FullName));
			}
			elementAccessor.Mapping = ((ArrayMapping)elementAccessor.Mapping).TopLevelMapping;
			MakeDerived((StructMapping)elementAccessor.Mapping, baseType, baseTypeCanBeIndirect);
		}
		return new XmlTypeMapping(base.Scope, elementAccessor);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlTypeMapping ImportSchemaType(XmlQualifiedName typeName)
	{
		return ImportSchemaType(typeName, null, baseTypeCanBeIndirect: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlTypeMapping ImportSchemaType(XmlQualifiedName typeName, Type? baseType)
	{
		return ImportSchemaType(typeName, baseType, baseTypeCanBeIndirect: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlTypeMapping ImportSchemaType(XmlQualifiedName typeName, Type? baseType, bool baseTypeCanBeIndirect)
	{
		TypeMapping typeMapping = ImportType(typeName, typeof(TypeMapping), baseType, TypeFlags.CanBeElementValue, addref: true);
		typeMapping.ReferencedByElement = false;
		ElementAccessor elementAccessor = new ElementAccessor();
		elementAccessor.IsTopLevelInSchema = true;
		elementAccessor.Name = typeName.Name;
		elementAccessor.Namespace = typeName.Namespace;
		elementAccessor.Mapping = typeMapping;
		if (typeMapping is SpecialMapping && ((SpecialMapping)typeMapping).NamedAny)
		{
			elementAccessor.Any = true;
		}
		elementAccessor.IsNullable = typeMapping.TypeDesc.IsNullable;
		elementAccessor.Form = XmlSchemaForm.Qualified;
		if (elementAccessor.Mapping is StructMapping)
		{
			MakeDerived((StructMapping)elementAccessor.Mapping, baseType, baseTypeCanBeIndirect);
		}
		else if (baseType != null)
		{
			if (!(elementAccessor.Mapping is ArrayMapping))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlBadBaseType, typeName.Name, typeName.Namespace, baseType.FullName));
			}
			elementAccessor.Mapping = ((ArrayMapping)elementAccessor.Mapping).TopLevelMapping;
			MakeDerived((StructMapping)elementAccessor.Mapping, baseType, baseTypeCanBeIndirect);
		}
		return new XmlTypeMapping(base.Scope, elementAccessor);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlTypeMapping ImportTypeMapping(XmlQualifiedName name)
	{
		return ImportDerivedTypeMapping(name, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlMembersMapping ImportMembersMapping(XmlQualifiedName name)
	{
		return new XmlMembersMapping(base.Scope, ImportElement(name, typeof(MembersMapping), null), XmlMappingAccess.Read | XmlMappingAccess.Write);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlMembersMapping? ImportAnyType(XmlQualifiedName typeName, string elementName)
	{
		TypeMapping typeMapping = ImportType(typeName, typeof(MembersMapping), null, TypeFlags.CanBeElementValue, addref: true);
		MembersMapping membersMapping = typeMapping as MembersMapping;
		if (membersMapping == null)
		{
			XmlSchemaComplexType xmlSchemaComplexType = new XmlSchemaComplexType();
			XmlSchemaSequence xmlSchemaSequence = (XmlSchemaSequence)(xmlSchemaComplexType.Particle = new XmlSchemaSequence());
			XmlSchemaElement xmlSchemaElement = new XmlSchemaElement();
			xmlSchemaElement.Name = elementName;
			xmlSchemaElement.SchemaTypeName = typeName;
			xmlSchemaSequence.Items.Add(xmlSchemaElement);
			membersMapping = ImportMembersType(xmlSchemaComplexType, typeName.Namespace, elementName);
		}
		if (membersMapping.Members.Length != 1 || !membersMapping.Members[0].Accessor.Any)
		{
			return null;
		}
		membersMapping.Members[0].Name = elementName;
		ElementAccessor elementAccessor = new ElementAccessor();
		elementAccessor.Name = elementName;
		elementAccessor.Namespace = typeName.Namespace;
		elementAccessor.Mapping = membersMapping;
		elementAccessor.Any = true;
		XmlSchemaObject xmlSchemaObject = base.Schemas.SchemaSet.GlobalTypes[typeName];
		if (xmlSchemaObject != null && xmlSchemaObject.Parent is XmlSchema xmlSchema)
		{
			elementAccessor.Form = ((xmlSchema.ElementFormDefault == XmlSchemaForm.None) ? XmlSchemaForm.Unqualified : xmlSchema.ElementFormDefault);
		}
		return new XmlMembersMapping(base.Scope, elementAccessor, XmlMappingAccess.Read | XmlMappingAccess.Write);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlMembersMapping ImportMembersMapping(XmlQualifiedName[] names)
	{
		return ImportMembersMapping(names, null, baseTypeCanBeIndirect: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlMembersMapping ImportMembersMapping(XmlQualifiedName[] names, Type? baseType, bool baseTypeCanBeIndirect)
	{
		CodeIdentifiers codeIdentifiers = new CodeIdentifiers();
		codeIdentifiers.UseCamelCasing = true;
		MemberMapping[] array = new MemberMapping[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			XmlQualifiedName name = names[i];
			ElementAccessor elementAccessor = ImportElement(name, typeof(TypeMapping), baseType);
			if (baseType != null && elementAccessor.Mapping is StructMapping)
			{
				MakeDerived((StructMapping)elementAccessor.Mapping, baseType, baseTypeCanBeIndirect);
			}
			MemberMapping memberMapping = new MemberMapping();
			memberMapping.Name = CodeIdentifier.MakeValid(Accessor.UnescapeName(elementAccessor.Name));
			memberMapping.Name = codeIdentifiers.AddUnique(memberMapping.Name, memberMapping);
			memberMapping.TypeDesc = elementAccessor.Mapping.TypeDesc;
			memberMapping.Elements = new ElementAccessor[1] { elementAccessor };
			array[i] = memberMapping;
		}
		MembersMapping membersMapping = new MembersMapping();
		membersMapping.HasWrapperElement = false;
		membersMapping.TypeDesc = base.Scope.GetTypeDesc(typeof(object[]));
		membersMapping.Members = array;
		ElementAccessor elementAccessor2 = new ElementAccessor();
		elementAccessor2.Mapping = membersMapping;
		return new XmlMembersMapping(base.Scope, elementAccessor2, XmlMappingAccess.Read | XmlMappingAccess.Write);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlMembersMapping ImportMembersMapping(string name, string? ns, SoapSchemaMember[] members)
	{
		XmlSchemaComplexType xmlSchemaComplexType = new XmlSchemaComplexType();
		XmlSchemaSequence xmlSchemaSequence = (XmlSchemaSequence)(xmlSchemaComplexType.Particle = new XmlSchemaSequence());
		foreach (SoapSchemaMember soapSchemaMember in members)
		{
			XmlSchemaElement xmlSchemaElement = new XmlSchemaElement();
			xmlSchemaElement.Name = soapSchemaMember.MemberName;
			xmlSchemaElement.SchemaTypeName = soapSchemaMember.MemberType;
			xmlSchemaSequence.Items.Add(xmlSchemaElement);
		}
		MembersMapping mapping = ImportMembersType(xmlSchemaComplexType, null, name);
		ElementAccessor elementAccessor = new ElementAccessor();
		elementAccessor.Name = Accessor.EscapeName(name);
		elementAccessor.Namespace = ns;
		elementAccessor.Mapping = mapping;
		elementAccessor.IsNullable = false;
		elementAccessor.Form = XmlSchemaForm.Qualified;
		return new XmlMembersMapping(base.Scope, elementAccessor, XmlMappingAccess.Read | XmlMappingAccess.Write);
	}

	[RequiresUnreferencedCode("calls ImportElement")]
	private ElementAccessor ImportElement(XmlQualifiedName name, Type desiredMappingType, Type baseType)
	{
		XmlSchemaElement xmlSchemaElement = FindElement(name);
		ElementAccessor elementAccessor = (ElementAccessor)base.ImportedElements[xmlSchemaElement];
		if (elementAccessor != null)
		{
			return elementAccessor;
		}
		elementAccessor = ImportElement(xmlSchemaElement, string.Empty, desiredMappingType, baseType, name.Namespace, topLevelElement: true);
		ElementAccessor elementAccessor2 = (ElementAccessor)base.ImportedElements[xmlSchemaElement];
		if (elementAccessor2 != null)
		{
			return elementAccessor2;
		}
		base.ImportedElements.Add(xmlSchemaElement, elementAccessor);
		return elementAccessor;
	}

	[RequiresUnreferencedCode("calls ImportElementType")]
	private ElementAccessor ImportElement(XmlSchemaElement element, string identifier, Type desiredMappingType, Type baseType, string ns, bool topLevelElement)
	{
		if (!element.RefName.IsEmpty)
		{
			ElementAccessor elementAccessor = ImportElement(element.RefName, desiredMappingType, baseType);
			if (element.IsMultipleOccurrence && elementAccessor.Mapping is ArrayMapping)
			{
				ElementAccessor elementAccessor2 = elementAccessor.Clone();
				elementAccessor2.IsTopLevelInSchema = false;
				elementAccessor2.Mapping.ReferencedByElement = true;
				return elementAccessor2;
			}
			return elementAccessor;
		}
		if (element.Name.Length == 0)
		{
			XmlQualifiedName parentName = XmlSchemas.GetParentName(element);
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlElementHasNoName, parentName.Name, parentName.Namespace));
		}
		string identifier2 = Accessor.UnescapeName(element.Name);
		identifier = ((identifier.Length != 0) ? (identifier + CodeIdentifier.MakePascal(identifier2)) : CodeIdentifier.MakeValid(identifier2));
		TypeMapping typeMapping = ImportElementType(element, identifier, desiredMappingType, baseType, ns);
		ElementAccessor elementAccessor3 = new ElementAccessor();
		elementAccessor3.IsTopLevelInSchema = element.Parent is XmlSchema;
		elementAccessor3.Name = element.Name;
		elementAccessor3.Namespace = ns;
		elementAccessor3.Mapping = typeMapping;
		elementAccessor3.IsOptional = element.MinOccurs == 0m;
		if (element.DefaultValue != null)
		{
			elementAccessor3.Default = element.DefaultValue;
		}
		else if (element.FixedValue != null)
		{
			elementAccessor3.Default = element.FixedValue;
			elementAccessor3.IsFixed = true;
		}
		if (typeMapping is SpecialMapping && ((SpecialMapping)typeMapping).NamedAny)
		{
			elementAccessor3.Any = true;
		}
		elementAccessor3.IsNullable = element.IsNillable;
		if (topLevelElement)
		{
			elementAccessor3.Form = XmlSchemaForm.Qualified;
		}
		else
		{
			elementAccessor3.Form = ElementForm(ns, element);
		}
		return elementAccessor3;
	}

	[RequiresUnreferencedCode("calls ImportMembersType")]
	private TypeMapping ImportElementType(XmlSchemaElement element, string identifier, Type desiredMappingType, Type baseType, string ns)
	{
		TypeMapping typeMapping;
		if (!element.SchemaTypeName.IsEmpty)
		{
			typeMapping = ImportType(element.SchemaTypeName, desiredMappingType, baseType, TypeFlags.CanBeElementValue, addref: false);
			if (!typeMapping.ReferencedByElement)
			{
				object obj = FindType(element.SchemaTypeName, TypeFlags.CanBeElementValue);
				XmlSchemaObject xmlSchemaObject = element;
				while (xmlSchemaObject.Parent != null && obj != xmlSchemaObject)
				{
					xmlSchemaObject = xmlSchemaObject.Parent;
				}
				typeMapping.ReferencedByElement = obj != xmlSchemaObject;
			}
		}
		else if (element.SchemaType == null)
		{
			typeMapping = ((!element.SubstitutionGroup.IsEmpty) ? ImportElementType(FindElement(element.SubstitutionGroup), identifier, desiredMappingType, baseType, ns) : ((!(desiredMappingType == typeof(MembersMapping))) ? ((TypeMapping)ImportRootMapping()) : ((TypeMapping)ImportMembersType(new XmlSchemaType(), ns, identifier))));
		}
		else
		{
			typeMapping = ((!(element.SchemaType is XmlSchemaComplexType)) ? ImportDataType((XmlSchemaSimpleType)element.SchemaType, ns, identifier, baseType, (TypeFlags)56, isList: false) : ImportType((XmlSchemaComplexType)element.SchemaType, ns, identifier, desiredMappingType, baseType, TypeFlags.CanBeElementValue));
			typeMapping.ReferencedByElement = true;
		}
		if (!desiredMappingType.IsAssignableFrom(typeMapping.GetType()))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlElementImportedTwice, element.Name, ns, typeMapping.GetType().Name, desiredMappingType.Name));
		}
		if (!typeMapping.TypeDesc.IsMappedType)
		{
			RunSchemaExtensions(typeMapping, element.SchemaTypeName, element.SchemaType, element, TypeFlags.CanBeElementValue);
		}
		return typeMapping;
	}

	private void RunSchemaExtensions(TypeMapping mapping, XmlQualifiedName qname, XmlSchemaType type, XmlSchemaObject context, TypeFlags flags)
	{
	}

	private string GenerateUniqueTypeName(string desiredName, string ns)
	{
		int num = 1;
		string text = desiredName;
		while (true)
		{
			XmlQualifiedName name = new XmlQualifiedName(text, ns);
			object obj = base.Schemas.Find(name, typeof(XmlSchemaType));
			if (obj == null)
			{
				break;
			}
			text = desiredName + num.ToString(CultureInfo.InvariantCulture);
			num++;
		}
		text = CodeIdentifier.MakeValid(text);
		return base.TypeIdentifiers.AddUnique(text, text);
	}

	[RequiresUnreferencedCode("calls ImportType")]
	internal override void ImportDerivedTypes(XmlQualifiedName baseName)
	{
		foreach (XmlSchema schema in base.Schemas)
		{
			if (base.Schemas.IsReference(schema) || XmlSchemas.IsDataSet(schema))
			{
				continue;
			}
			XmlSchemas.Preprocess(schema);
			foreach (object value in schema.SchemaTypes.Values)
			{
				if (value is XmlSchemaType)
				{
					XmlSchemaType xmlSchemaType = (XmlSchemaType)value;
					if (xmlSchemaType.DerivedFrom == baseName && base.TypesInUse[xmlSchemaType.Name, schema.TargetNamespace] == null)
					{
						ImportType(xmlSchemaType.QualifiedName, typeof(TypeMapping), null, TypeFlags.CanBeElementValue, addref: false);
					}
				}
			}
		}
	}

	[RequiresUnreferencedCode("calls FindType")]
	private TypeMapping ImportType(XmlQualifiedName name, Type desiredMappingType, Type baseType, TypeFlags flags, bool addref)
	{
		if (name.Name == "anyType" && name.Namespace == "http://www.w3.org/2001/XMLSchema")
		{
			return ImportRootMapping();
		}
		object obj = FindType(name, flags);
		TypeMapping typeMapping = (TypeMapping)base.ImportedMappings[obj];
		if (typeMapping != null && desiredMappingType.IsAssignableFrom(typeMapping.GetType()))
		{
			return typeMapping;
		}
		if (addref)
		{
			AddReference(name, base.TypesInUse, System.SR.XmlCircularTypeReference);
		}
		if (obj is XmlSchemaComplexType)
		{
			typeMapping = ImportType((XmlSchemaComplexType)obj, name.Namespace, name.Name, desiredMappingType, baseType, flags);
		}
		else
		{
			if (!(obj is XmlSchemaSimpleType))
			{
				throw new InvalidOperationException(System.SR.XmlInternalError);
			}
			typeMapping = ImportDataType((XmlSchemaSimpleType)obj, name.Namespace, name.Name, baseType, flags, isList: false);
		}
		if (addref && name.Namespace != "http://www.w3.org/2001/XMLSchema")
		{
			RemoveReference(name, base.TypesInUse);
		}
		return typeMapping;
	}

	[RequiresUnreferencedCode("calls ImportMembersType")]
	private TypeMapping ImportType(XmlSchemaComplexType type, string typeNs, string identifier, Type desiredMappingType, Type baseType, TypeFlags flags)
	{
		if (type.Redefined != null)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.XmlUnsupportedRedefine, type.Name, typeNs));
		}
		if (desiredMappingType == typeof(TypeMapping))
		{
			TypeMapping typeMapping = null;
			if (baseType == null && (typeMapping = ImportArrayMapping(type, identifier, typeNs, repeats: false)) == null)
			{
				typeMapping = ImportAnyMapping(type, identifier, typeNs, repeats: false);
			}
			if (typeMapping == null)
			{
				typeMapping = ImportStructType(type, typeNs, identifier, baseType, arrayLike: false);
				if (typeMapping != null && type.Name != null && type.Name.Length != 0)
				{
					ImportDerivedTypes(new XmlQualifiedName(identifier, typeNs));
				}
			}
			return typeMapping;
		}
		if (desiredMappingType == typeof(MembersMapping))
		{
			return ImportMembersType(type, typeNs, identifier);
		}
		throw new ArgumentException(System.SR.XmlInternalError, "desiredMappingType");
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private MembersMapping ImportMembersType(XmlSchemaType type, string typeNs, string identifier)
	{
		if (!type.DerivedFrom.IsEmpty)
		{
			throw new InvalidOperationException(System.SR.XmlMembersDeriveError);
		}
		CodeIdentifiers codeIdentifiers = new CodeIdentifiers();
		codeIdentifiers.UseCamelCasing = true;
		bool needExplicitOrder = false;
		MemberMapping[] members = ImportTypeMembers(type, typeNs, identifier, codeIdentifiers, new CodeIdentifiers(), new NameTable(), ref needExplicitOrder, order: false, allowUnboundedElements: false);
		MembersMapping membersMapping = new MembersMapping();
		membersMapping.HasWrapperElement = true;
		membersMapping.TypeDesc = base.Scope.GetTypeDesc(typeof(object[]));
		membersMapping.Members = members;
		return membersMapping;
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private StructMapping ImportStructType(XmlSchemaType type, string typeNs, string identifier, Type baseType, bool arrayLike)
	{
		TypeDesc typeDesc = null;
		TypeMapping typeMapping = null;
		bool flag = false;
		if (!type.DerivedFrom.IsEmpty)
		{
			typeMapping = ImportType(type.DerivedFrom, typeof(TypeMapping), null, (TypeFlags)48, addref: false);
			if (typeMapping is StructMapping)
			{
				typeDesc = ((StructMapping)typeMapping).TypeDesc;
			}
			else if (typeMapping is ArrayMapping)
			{
				typeMapping = ((ArrayMapping)typeMapping).TopLevelMapping;
				if (typeMapping != null)
				{
					typeMapping.ReferencedByTopLevelElement = false;
					typeMapping.ReferencedByElement = true;
					typeDesc = typeMapping.TypeDesc;
				}
			}
			else
			{
				typeMapping = null;
			}
		}
		if (typeDesc == null && baseType != null)
		{
			typeDesc = base.Scope.GetTypeDesc(baseType);
		}
		if (typeMapping == null)
		{
			typeMapping = GetRootMapping();
			flag = true;
		}
		Mapping mapping = (Mapping)base.ImportedMappings[type];
		if (mapping != null)
		{
			if (mapping is StructMapping)
			{
				return (StructMapping)mapping;
			}
			if (!arrayLike || !(mapping is ArrayMapping))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlTypeUsedTwice, type.QualifiedName.Name, type.QualifiedName.Namespace));
			}
			ArrayMapping arrayMapping = (ArrayMapping)mapping;
			if (arrayMapping.TopLevelMapping != null)
			{
				return arrayMapping.TopLevelMapping;
			}
		}
		StructMapping structMapping = new StructMapping();
		structMapping.IsReference = base.Schemas.IsReference(type);
		TypeFlags typeFlags = TypeFlags.Reference;
		if (type is XmlSchemaComplexType && ((XmlSchemaComplexType)type).IsAbstract)
		{
			typeFlags |= TypeFlags.Abstract;
		}
		identifier = Accessor.UnescapeName(identifier);
		string text = ((type.Name == null || type.Name.Length == 0) ? GenerateUniqueTypeName(identifier, typeNs) : GenerateUniqueTypeName(identifier));
		structMapping.TypeDesc = new TypeDesc(text, text, TypeKind.Struct, typeDesc, typeFlags);
		structMapping.Namespace = typeNs;
		structMapping.TypeName = ((type.Name == null || type.Name.Length == 0) ? null : identifier);
		structMapping.BaseMapping = (StructMapping)typeMapping;
		if (!arrayLike)
		{
			base.ImportedMappings.Add(type, structMapping);
		}
		CodeIdentifiers codeIdentifiers = new CodeIdentifiers();
		CodeIdentifiers codeIdentifiers2 = structMapping.BaseMapping.Scope.Clone();
		codeIdentifiers.AddReserved(text);
		codeIdentifiers2.AddReserved(text);
		AddReservedIdentifiersForDataBinding(codeIdentifiers);
		if (flag)
		{
			AddReservedIdentifiersForDataBinding(codeIdentifiers2);
		}
		bool needExplicitOrder = false;
		structMapping.Members = ImportTypeMembers(type, typeNs, identifier, codeIdentifiers, codeIdentifiers2, structMapping, ref needExplicitOrder, order: true, allowUnboundedElements: true);
		if (!IsAllGroup(type))
		{
			if (needExplicitOrder && !GenerateOrder)
			{
				structMapping.SetSequence();
			}
			else if (GenerateOrder)
			{
				structMapping.IsSequence = true;
			}
		}
		for (int i = 0; i < structMapping.Members.Length; i++)
		{
			StructMapping declaringMapping;
			MemberMapping memberMapping = ((StructMapping)typeMapping).FindDeclaringMapping(structMapping.Members[i], out declaringMapping, structMapping.TypeName);
			if (memberMapping != null && memberMapping.TypeDesc != structMapping.Members[i].TypeDesc)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalOverride, type.Name, memberMapping.Name, memberMapping.TypeDesc.FullName, structMapping.Members[i].TypeDesc.FullName, declaringMapping.TypeDesc.FullName));
			}
		}
		structMapping.Scope = codeIdentifiers2;
		base.Scope.AddTypeMapping(structMapping);
		return structMapping;
	}

	private bool IsAllGroup(XmlSchemaType type)
	{
		TypeItems typeItems = GetTypeItems(type);
		if (typeItems.Particle != null)
		{
			return typeItems.Particle is XmlSchemaAll;
		}
		return false;
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private StructMapping ImportStructDataType(XmlSchemaSimpleType dataType, string typeNs, string identifier, Type baseType)
	{
		identifier = Accessor.UnescapeName(identifier);
		string text = GenerateUniqueTypeName(identifier);
		StructMapping structMapping = new StructMapping();
		structMapping.IsReference = base.Schemas.IsReference(dataType);
		TypeFlags flags = TypeFlags.Reference;
		TypeDesc typeDesc = base.Scope.GetTypeDesc(baseType);
		structMapping.TypeDesc = new TypeDesc(text, text, TypeKind.Struct, typeDesc, flags);
		structMapping.Namespace = typeNs;
		structMapping.TypeName = identifier;
		CodeIdentifiers codeIdentifiers = new CodeIdentifiers();
		codeIdentifiers.AddReserved(text);
		AddReservedIdentifiersForDataBinding(codeIdentifiers);
		ImportTextMember(codeIdentifiers, new CodeIdentifiers(), null);
		structMapping.Members = (MemberMapping[])codeIdentifiers.ToArray(typeof(MemberMapping));
		structMapping.Scope = codeIdentifiers;
		base.Scope.AddTypeMapping(structMapping);
		return structMapping;
	}

	[RequiresUnreferencedCode("calls FindType")]
	private MemberMapping[] ImportTypeMembers(XmlSchemaType type, string typeNs, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, INameScope elementsScope, ref bool needExplicitOrder, bool order, bool allowUnboundedElements)
	{
		TypeItems typeItems = GetTypeItems(type);
		bool flag = IsMixed(type);
		if (flag)
		{
			XmlSchemaType xmlSchemaType = type;
			while (!xmlSchemaType.DerivedFrom.IsEmpty)
			{
				xmlSchemaType = FindType(xmlSchemaType.DerivedFrom, (TypeFlags)48);
				if (IsMixed(xmlSchemaType))
				{
					flag = false;
					break;
				}
			}
		}
		if (typeItems.Particle != null)
		{
			ImportGroup(typeItems.Particle, identifier, members, membersScope, elementsScope, typeNs, flag, ref needExplicitOrder, order, typeItems.IsUnbounded, allowUnboundedElements);
		}
		for (int i = 0; i < typeItems.Attributes.Count; i++)
		{
			object obj = typeItems.Attributes[i];
			if (obj is XmlSchemaAttribute)
			{
				ImportAttributeMember((XmlSchemaAttribute)obj, identifier, members, membersScope, typeNs);
			}
			else if (obj is XmlSchemaAttributeGroupRef)
			{
				XmlQualifiedName refName = ((XmlSchemaAttributeGroupRef)obj).RefName;
				ImportAttributeGroupMembers(FindAttributeGroup(refName), identifier, members, membersScope, refName.Namespace);
			}
		}
		if (typeItems.AnyAttribute != null)
		{
			ImportAnyAttributeMember(typeItems.AnyAttribute, members, membersScope);
		}
		if (typeItems.baseSimpleType != null || (typeItems.Particle == null && flag))
		{
			ImportTextMember(members, membersScope, flag ? null : typeItems.baseSimpleType);
		}
		ImportXmlnsDeclarationsMember(type, members, membersScope);
		return (MemberMapping[])members.ToArray(typeof(MemberMapping));
	}

	internal static bool IsMixed(XmlSchemaType type)
	{
		if (!(type is XmlSchemaComplexType))
		{
			return false;
		}
		XmlSchemaComplexType xmlSchemaComplexType = (XmlSchemaComplexType)type;
		bool isMixed = xmlSchemaComplexType.IsMixed;
		if (!isMixed && xmlSchemaComplexType.ContentModel != null && xmlSchemaComplexType.ContentModel is XmlSchemaComplexContent)
		{
			isMixed = ((XmlSchemaComplexContent)xmlSchemaComplexType.ContentModel).IsMixed;
		}
		return isMixed;
	}

	private TypeItems GetTypeItems(XmlSchemaType type)
	{
		TypeItems typeItems = new TypeItems();
		if (type is XmlSchemaComplexType)
		{
			XmlSchemaParticle xmlSchemaParticle = null;
			XmlSchemaComplexType xmlSchemaComplexType = (XmlSchemaComplexType)type;
			if (xmlSchemaComplexType.ContentModel != null)
			{
				XmlSchemaContent content = xmlSchemaComplexType.ContentModel.Content;
				if (content is XmlSchemaComplexContentExtension)
				{
					XmlSchemaComplexContentExtension xmlSchemaComplexContentExtension = (XmlSchemaComplexContentExtension)content;
					typeItems.Attributes = xmlSchemaComplexContentExtension.Attributes;
					typeItems.AnyAttribute = xmlSchemaComplexContentExtension.AnyAttribute;
					xmlSchemaParticle = xmlSchemaComplexContentExtension.Particle;
				}
				else if (content is XmlSchemaSimpleContentExtension)
				{
					XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension = (XmlSchemaSimpleContentExtension)content;
					typeItems.Attributes = xmlSchemaSimpleContentExtension.Attributes;
					typeItems.AnyAttribute = xmlSchemaSimpleContentExtension.AnyAttribute;
					typeItems.baseSimpleType = xmlSchemaSimpleContentExtension.BaseTypeName;
				}
			}
			else
			{
				typeItems.Attributes = xmlSchemaComplexType.Attributes;
				typeItems.AnyAttribute = xmlSchemaComplexType.AnyAttribute;
				xmlSchemaParticle = xmlSchemaComplexType.Particle;
			}
			if (xmlSchemaParticle is XmlSchemaGroupRef)
			{
				XmlSchemaGroupRef xmlSchemaGroupRef = (XmlSchemaGroupRef)xmlSchemaParticle;
				typeItems.Particle = FindGroup(xmlSchemaGroupRef.RefName).Particle;
				typeItems.IsUnbounded = xmlSchemaParticle.IsMultipleOccurrence;
			}
			else if (xmlSchemaParticle is XmlSchemaGroupBase)
			{
				typeItems.Particle = (XmlSchemaGroupBase)xmlSchemaParticle;
				typeItems.IsUnbounded = xmlSchemaParticle.IsMultipleOccurrence;
			}
		}
		return typeItems;
	}

	[RequiresUnreferencedCode("calls ImportChoiceGroup")]
	private void ImportGroup(XmlSchemaGroupBase group, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, INameScope elementsScope, string ns, bool mixed, ref bool needExplicitOrder, bool allowDuplicates, bool groupRepeats, bool allowUnboundedElements)
	{
		if (group is XmlSchemaChoice)
		{
			ImportChoiceGroup((XmlSchemaChoice)group, identifier, members, membersScope, elementsScope, ns, groupRepeats, ref needExplicitOrder, allowDuplicates);
		}
		else
		{
			ImportGroupMembers(group, identifier, members, membersScope, elementsScope, ns, groupRepeats, ref mixed, ref needExplicitOrder, allowDuplicates, allowUnboundedElements);
		}
		if (mixed)
		{
			ImportTextMember(members, membersScope, null);
		}
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private MemberMapping ImportChoiceGroup(XmlSchemaGroupBase group, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, INameScope elementsScope, string ns, bool groupRepeats, ref bool needExplicitOrder, bool allowDuplicates)
	{
		NameTable nameTable = new NameTable();
		if (GatherGroupChoices(group, nameTable, identifier, ns, ref needExplicitOrder, allowDuplicates))
		{
			groupRepeats = true;
		}
		MemberMapping memberMapping = new MemberMapping();
		memberMapping.Elements = (ElementAccessor[])nameTable.ToArray(typeof(ElementAccessor));
		Array.Sort(memberMapping.Elements, new ElementComparer());
		AddScopeElements(elementsScope, memberMapping.Elements, ref needExplicitOrder, allowDuplicates);
		bool flag = false;
		bool flag2 = false;
		Hashtable hashtable = new Hashtable(memberMapping.Elements.Length);
		for (int i = 0; i < memberMapping.Elements.Length; i++)
		{
			ElementAccessor elementAccessor = memberMapping.Elements[i];
			string fullName = elementAccessor.Mapping.TypeDesc.FullName;
			object obj = hashtable[fullName];
			if (obj != null)
			{
				flag = true;
				ElementAccessor elementAccessor2 = (ElementAccessor)obj;
				if (!flag2 && elementAccessor2.IsNullable != elementAccessor.IsNullable)
				{
					flag2 = true;
				}
			}
			else
			{
				hashtable.Add(fullName, elementAccessor);
			}
			if (elementAccessor.Mapping is ArrayMapping arrayMapping && IsNeedXmlSerializationAttributes(arrayMapping))
			{
				elementAccessor.Mapping = arrayMapping.TopLevelMapping;
				elementAccessor.Mapping.ReferencedByTopLevelElement = false;
				elementAccessor.Mapping.ReferencedByElement = true;
			}
		}
		if (flag2)
		{
			memberMapping.TypeDesc = base.Scope.GetTypeDesc(typeof(object));
		}
		else
		{
			TypeDesc[] array = new TypeDesc[hashtable.Count];
			IEnumerator enumerator = hashtable.Values.GetEnumerator();
			for (int j = 0; j < array.Length; j++)
			{
				if (!enumerator.MoveNext())
				{
					break;
				}
				array[j] = ((ElementAccessor)enumerator.Current).Mapping.TypeDesc;
			}
			memberMapping.TypeDesc = TypeDesc.FindCommonBaseTypeDesc(array);
			if (memberMapping.TypeDesc == null)
			{
				memberMapping.TypeDesc = base.Scope.GetTypeDesc(typeof(object));
			}
		}
		if (groupRepeats)
		{
			memberMapping.TypeDesc = memberMapping.TypeDesc.CreateArrayTypeDesc();
		}
		if (membersScope != null)
		{
			memberMapping.Name = membersScope.AddUnique(groupRepeats ? "Items" : "Item", memberMapping);
			members?.Add(memberMapping.Name, memberMapping);
		}
		if (flag)
		{
			memberMapping.ChoiceIdentifier = new ChoiceIdentifierAccessor();
			memberMapping.ChoiceIdentifier.MemberName = memberMapping.Name + "ElementName";
			memberMapping.ChoiceIdentifier.Mapping = ImportEnumeratedChoice(memberMapping.Elements, ns, memberMapping.Name + "ChoiceType");
			memberMapping.ChoiceIdentifier.MemberIds = new string[memberMapping.Elements.Length];
			ConstantMapping[] constants = ((EnumMapping)memberMapping.ChoiceIdentifier.Mapping).Constants;
			for (int k = 0; k < memberMapping.Elements.Length; k++)
			{
				memberMapping.ChoiceIdentifier.MemberIds[k] = constants[k].Name;
			}
			MemberMapping memberMapping2 = new MemberMapping();
			memberMapping2.Ignore = true;
			memberMapping2.Name = memberMapping.ChoiceIdentifier.MemberName;
			if (groupRepeats)
			{
				memberMapping2.TypeDesc = memberMapping.ChoiceIdentifier.Mapping.TypeDesc.CreateArrayTypeDesc();
			}
			else
			{
				memberMapping2.TypeDesc = memberMapping.ChoiceIdentifier.Mapping.TypeDesc;
			}
			ElementAccessor elementAccessor3 = new ElementAccessor();
			elementAccessor3.Name = memberMapping2.Name;
			elementAccessor3.Namespace = ns;
			elementAccessor3.Mapping = memberMapping.ChoiceIdentifier.Mapping;
			memberMapping2.Elements = new ElementAccessor[1] { elementAccessor3 };
			if (membersScope != null)
			{
				string text2 = (memberMapping.ChoiceIdentifier.MemberName = membersScope.AddUnique(memberMapping.ChoiceIdentifier.MemberName, memberMapping2));
				string name = (memberMapping2.Name = text2);
				elementAccessor3.Name = name;
				members?.Add(elementAccessor3.Name, memberMapping2);
			}
		}
		return memberMapping;
	}

	private bool IsNeedXmlSerializationAttributes(ArrayMapping arrayMapping)
	{
		if (arrayMapping.Elements.Length != 1)
		{
			return true;
		}
		ElementAccessor elementAccessor = arrayMapping.Elements[0];
		TypeMapping mapping = elementAccessor.Mapping;
		if (elementAccessor.Name != mapping.DefaultElementName)
		{
			return true;
		}
		if (elementAccessor.Form != 0 && elementAccessor.Form != XmlSchemaForm.Qualified)
		{
			return true;
		}
		if (elementAccessor.Mapping.TypeDesc != null)
		{
			if (elementAccessor.IsNullable != elementAccessor.Mapping.TypeDesc.IsNullable)
			{
				return true;
			}
			if (elementAccessor.Mapping.TypeDesc.IsAmbiguousDataType)
			{
				return true;
			}
		}
		return false;
	}

	[RequiresUnreferencedCode("calls GatherGroupChoices")]
	private bool GatherGroupChoices(XmlSchemaGroup group, NameTable choiceElements, string identifier, string ns, ref bool needExplicitOrder, bool allowDuplicates)
	{
		return GatherGroupChoices(group.Particle, choiceElements, identifier, ns, ref needExplicitOrder, allowDuplicates);
	}

	[RequiresUnreferencedCode("Calls ImportAny")]
	private bool GatherGroupChoices(XmlSchemaParticle particle, NameTable choiceElements, string identifier, string ns, ref bool needExplicitOrder, bool allowDuplicates)
	{
		if (particle is XmlSchemaGroupRef)
		{
			XmlSchemaGroupRef xmlSchemaGroupRef = (XmlSchemaGroupRef)particle;
			if (!xmlSchemaGroupRef.RefName.IsEmpty)
			{
				AddReference(xmlSchemaGroupRef.RefName, base.GroupsInUse, System.SR.XmlCircularGroupReference);
				if (GatherGroupChoices(FindGroup(xmlSchemaGroupRef.RefName), choiceElements, identifier, xmlSchemaGroupRef.RefName.Namespace, ref needExplicitOrder, allowDuplicates))
				{
					RemoveReference(xmlSchemaGroupRef.RefName, base.GroupsInUse);
					return true;
				}
				RemoveReference(xmlSchemaGroupRef.RefName, base.GroupsInUse);
			}
		}
		else if (particle is XmlSchemaGroupBase)
		{
			XmlSchemaGroupBase xmlSchemaGroupBase = (XmlSchemaGroupBase)particle;
			bool flag = xmlSchemaGroupBase.IsMultipleOccurrence;
			XmlSchemaAny xmlSchemaAny = null;
			bool duplicateElements = false;
			for (int i = 0; i < xmlSchemaGroupBase.Items.Count; i++)
			{
				object obj = xmlSchemaGroupBase.Items[i];
				if (obj is XmlSchemaGroupBase || obj is XmlSchemaGroupRef)
				{
					if (GatherGroupChoices((XmlSchemaParticle)obj, choiceElements, identifier, ns, ref needExplicitOrder, allowDuplicates))
					{
						flag = true;
					}
				}
				else if (obj is XmlSchemaAny)
				{
					if (GenerateOrder)
					{
						AddScopeElements(choiceElements, ImportAny((XmlSchemaAny)obj, makeElement: true, ns), ref duplicateElements, allowDuplicates);
					}
					else
					{
						xmlSchemaAny = (XmlSchemaAny)obj;
					}
				}
				else
				{
					if (!(obj is XmlSchemaElement))
					{
						continue;
					}
					XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)obj;
					XmlSchemaElement topLevelElement = GetTopLevelElement(xmlSchemaElement);
					if (topLevelElement != null)
					{
						XmlSchemaElement[] equivalentElements = GetEquivalentElements(topLevelElement);
						for (int j = 0; j < equivalentElements.Length; j++)
						{
							if (equivalentElements[j].IsMultipleOccurrence)
							{
								flag = true;
							}
							AddScopeElement(choiceElements, ImportElement(equivalentElements[j], identifier, typeof(TypeMapping), null, equivalentElements[j].QualifiedName.Namespace, topLevelElement: true), ref duplicateElements, allowDuplicates);
						}
					}
					if (xmlSchemaElement.IsMultipleOccurrence)
					{
						flag = true;
					}
					AddScopeElement(choiceElements, ImportElement(xmlSchemaElement, identifier, typeof(TypeMapping), null, xmlSchemaElement.QualifiedName.Namespace, topLevelElement: false), ref duplicateElements, allowDuplicates);
				}
			}
			if (xmlSchemaAny != null)
			{
				AddScopeElements(choiceElements, ImportAny(xmlSchemaAny, makeElement: true, ns), ref duplicateElements, allowDuplicates);
			}
			if (!flag && !(xmlSchemaGroupBase is XmlSchemaChoice) && xmlSchemaGroupBase.Items.Count > 1)
			{
				flag = true;
			}
			return flag;
		}
		return false;
	}

	private void AddScopeElement(INameScope scope, ElementAccessor element, ref bool duplicateElements, bool allowDuplicates)
	{
		if (scope == null)
		{
			return;
		}
		ElementAccessor elementAccessor = (ElementAccessor)scope[element.Name, element.Namespace];
		if (elementAccessor != null)
		{
			if (!allowDuplicates)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlDuplicateElementInScope, element.Name, element.Namespace));
			}
			if (elementAccessor.Mapping.TypeDesc != element.Mapping.TypeDesc)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlDuplicateElementInScope1, element.Name, element.Namespace));
			}
			duplicateElements = true;
		}
		else
		{
			scope[element.Name, element.Namespace] = element;
		}
	}

	private void AddScopeElements(INameScope scope, ElementAccessor[] elements, ref bool duplicateElements, bool allowDuplicates)
	{
		for (int i = 0; i < elements.Length; i++)
		{
			AddScopeElement(scope, elements[i], ref duplicateElements, allowDuplicates);
		}
	}

	[RequiresUnreferencedCode("calls ImportChoiceGroup")]
	private void ImportGroupMembers(XmlSchemaParticle particle, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, INameScope elementsScope, string ns, bool groupRepeats, ref bool mixed, ref bool needExplicitOrder, bool allowDuplicates, bool allowUnboundedElements)
	{
		if (particle is XmlSchemaGroupRef)
		{
			XmlSchemaGroupRef xmlSchemaGroupRef = (XmlSchemaGroupRef)particle;
			if (!xmlSchemaGroupRef.RefName.IsEmpty)
			{
				AddReference(xmlSchemaGroupRef.RefName, base.GroupsInUse, System.SR.XmlCircularGroupReference);
				ImportGroupMembers(FindGroup(xmlSchemaGroupRef.RefName).Particle, identifier, members, membersScope, elementsScope, xmlSchemaGroupRef.RefName.Namespace, groupRepeats | xmlSchemaGroupRef.IsMultipleOccurrence, ref mixed, ref needExplicitOrder, allowDuplicates, allowUnboundedElements);
				RemoveReference(xmlSchemaGroupRef.RefName, base.GroupsInUse);
			}
		}
		else
		{
			if (!(particle is XmlSchemaGroupBase))
			{
				return;
			}
			XmlSchemaGroupBase xmlSchemaGroupBase = (XmlSchemaGroupBase)particle;
			if (xmlSchemaGroupBase.IsMultipleOccurrence)
			{
				groupRepeats = true;
			}
			if (GenerateOrder && groupRepeats && xmlSchemaGroupBase.Items.Count > 1)
			{
				ImportChoiceGroup(xmlSchemaGroupBase, identifier, members, membersScope, elementsScope, ns, groupRepeats, ref needExplicitOrder, allowDuplicates);
				return;
			}
			for (int i = 0; i < xmlSchemaGroupBase.Items.Count; i++)
			{
				object obj = xmlSchemaGroupBase.Items[i];
				if (obj is XmlSchemaChoice)
				{
					ImportChoiceGroup((XmlSchemaGroupBase)obj, identifier, members, membersScope, elementsScope, ns, groupRepeats, ref needExplicitOrder, allowDuplicates);
				}
				else if (obj is XmlSchemaElement)
				{
					ImportElementMember((XmlSchemaElement)obj, identifier, members, membersScope, elementsScope, ns, groupRepeats, ref needExplicitOrder, allowDuplicates, allowUnboundedElements);
				}
				else if (obj is XmlSchemaAny)
				{
					ImportAnyMember((XmlSchemaAny)obj, identifier, members, membersScope, elementsScope, ns, ref mixed, ref needExplicitOrder, allowDuplicates);
				}
				else if (obj is XmlSchemaParticle)
				{
					ImportGroupMembers((XmlSchemaParticle)obj, identifier, members, membersScope, elementsScope, ns, groupRepeats, ref mixed, ref needExplicitOrder, allowDuplicates, allowUnboundedElements: true);
				}
			}
		}
	}

	private XmlSchemaElement GetTopLevelElement(XmlSchemaElement element)
	{
		if (!element.RefName.IsEmpty)
		{
			return FindElement(element.RefName);
		}
		return null;
	}

	private XmlSchemaElement[] GetEquivalentElements(XmlSchemaElement element)
	{
		ArrayList arrayList = new ArrayList();
		foreach (XmlSchema item in base.Schemas.SchemaSet.Schemas())
		{
			for (int i = 0; i < item.Items.Count; i++)
			{
				object obj = item.Items[i];
				if (obj is XmlSchemaElement)
				{
					XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)obj;
					if (!xmlSchemaElement.IsAbstract && xmlSchemaElement.SubstitutionGroup.Namespace == item.TargetNamespace && xmlSchemaElement.SubstitutionGroup.Name == element.Name)
					{
						arrayList.Add(xmlSchemaElement);
					}
				}
			}
		}
		return (XmlSchemaElement[])arrayList.ToArray(typeof(XmlSchemaElement));
	}

	[RequiresUnreferencedCode("calls ImportChoiceGroup")]
	private bool ImportSubstitutionGroupMember(XmlSchemaElement element, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, string ns, bool repeats, ref bool needExplicitOrder, bool allowDuplicates)
	{
		XmlSchemaElement[] equivalentElements = GetEquivalentElements(element);
		if (equivalentElements.Length == 0)
		{
			return false;
		}
		XmlSchemaChoice xmlSchemaChoice = new XmlSchemaChoice();
		for (int i = 0; i < equivalentElements.Length; i++)
		{
			xmlSchemaChoice.Items.Add(equivalentElements[i]);
		}
		if (!element.IsAbstract)
		{
			xmlSchemaChoice.Items.Add(element);
		}
		identifier = ((identifier.Length != 0) ? (identifier + CodeIdentifier.MakePascal(Accessor.UnescapeName(element.Name))) : CodeIdentifier.MakeValid(Accessor.UnescapeName(element.Name)));
		ImportChoiceGroup(xmlSchemaChoice, identifier, members, membersScope, null, ns, repeats, ref needExplicitOrder, allowDuplicates);
		return true;
	}

	[RequiresUnreferencedCode("calls ImportType")]
	private void ImportTextMember(CodeIdentifiers members, CodeIdentifiers membersScope, XmlQualifiedName simpleContentType)
	{
		bool flag = false;
		TypeMapping typeMapping;
		if (simpleContentType != null)
		{
			typeMapping = ImportType(simpleContentType, typeof(TypeMapping), null, (TypeFlags)48, addref: false);
			if (!(typeMapping is PrimitiveMapping) && !typeMapping.TypeDesc.CanBeTextValue)
			{
				return;
			}
		}
		else
		{
			flag = true;
			typeMapping = GetDefaultMapping((TypeFlags)48);
		}
		TextAccessor textAccessor = new TextAccessor();
		textAccessor.Mapping = typeMapping;
		MemberMapping memberMapping = new MemberMapping();
		memberMapping.Elements = Array.Empty<ElementAccessor>();
		memberMapping.Text = textAccessor;
		if (flag)
		{
			memberMapping.TypeDesc = textAccessor.Mapping.TypeDesc.CreateArrayTypeDesc();
			memberMapping.Name = members.MakeRightCase("Text");
		}
		else
		{
			PrimitiveMapping primitiveMapping = (PrimitiveMapping)textAccessor.Mapping;
			if (primitiveMapping.IsList)
			{
				memberMapping.TypeDesc = textAccessor.Mapping.TypeDesc.CreateArrayTypeDesc();
				memberMapping.Name = members.MakeRightCase("Text");
			}
			else
			{
				memberMapping.TypeDesc = textAccessor.Mapping.TypeDesc;
				memberMapping.Name = members.MakeRightCase("Value");
			}
		}
		memberMapping.Name = membersScope.AddUnique(memberMapping.Name, memberMapping);
		members.Add(memberMapping.Name, memberMapping);
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private MemberMapping ImportAnyMember(XmlSchemaAny any, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, INameScope elementsScope, string ns, ref bool mixed, ref bool needExplicitOrder, bool allowDuplicates)
	{
		ElementAccessor[] array = ImportAny(any, !mixed, ns);
		AddScopeElements(elementsScope, array, ref needExplicitOrder, allowDuplicates);
		MemberMapping memberMapping = new MemberMapping();
		memberMapping.Elements = array;
		memberMapping.Name = membersScope.MakeRightCase("Any");
		memberMapping.Name = membersScope.AddUnique(memberMapping.Name, memberMapping);
		members.Add(memberMapping.Name, memberMapping);
		memberMapping.TypeDesc = array[0].Mapping.TypeDesc;
		bool flag = any.IsMultipleOccurrence;
		if (mixed)
		{
			SpecialMapping specialMapping = new SpecialMapping();
			specialMapping.TypeDesc = base.Scope.GetTypeDesc(typeof(XmlNode));
			specialMapping.TypeName = specialMapping.TypeDesc.Name;
			memberMapping.TypeDesc = specialMapping.TypeDesc;
			TextAccessor textAccessor = new TextAccessor();
			textAccessor.Mapping = specialMapping;
			memberMapping.Text = textAccessor;
			flag = true;
			mixed = false;
		}
		if (flag)
		{
			memberMapping.TypeDesc = memberMapping.TypeDesc.CreateArrayTypeDesc();
		}
		return memberMapping;
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private ElementAccessor[] ImportAny(XmlSchemaAny any, bool makeElement, string targetNamespace)
	{
		SpecialMapping specialMapping = new SpecialMapping();
		specialMapping.TypeDesc = base.Scope.GetTypeDesc(makeElement ? typeof(XmlElement) : typeof(XmlNode));
		specialMapping.TypeName = specialMapping.TypeDesc.Name;
		TypeFlags typeFlags = TypeFlags.CanBeElementValue;
		if (makeElement)
		{
			typeFlags |= TypeFlags.CanBeTextValue;
		}
		RunSchemaExtensions(specialMapping, XmlQualifiedName.Empty, null, any, typeFlags);
		if (GenerateOrder && any.Namespace != null)
		{
			NamespaceList namespaceList = new NamespaceList(any.Namespace, targetNamespace);
			if (namespaceList.Type == NamespaceList.ListType.Set)
			{
				ICollection enumerate = namespaceList.Enumerate;
				ElementAccessor[] array = new ElementAccessor[(enumerate.Count == 0) ? 1 : enumerate.Count];
				int num = 0;
				foreach (string item in namespaceList.Enumerate)
				{
					ElementAccessor elementAccessor = new ElementAccessor();
					elementAccessor.Mapping = specialMapping;
					elementAccessor.Any = true;
					elementAccessor.Namespace = item;
					array[num++] = elementAccessor;
				}
				if (num > 0)
				{
					return array;
				}
			}
		}
		ElementAccessor elementAccessor2 = new ElementAccessor();
		elementAccessor2.Mapping = specialMapping;
		elementAccessor2.Any = true;
		return new ElementAccessor[1] { elementAccessor2 };
	}

	[RequiresUnreferencedCode("calls ImportArrayMapping")]
	private ElementAccessor ImportArray(XmlSchemaElement element, string identifier, string ns, bool repeats)
	{
		if (repeats)
		{
			return null;
		}
		if (element.SchemaType == null)
		{
			return null;
		}
		if (element.IsMultipleOccurrence)
		{
			return null;
		}
		XmlSchemaType schemaType = element.SchemaType;
		ArrayMapping arrayMapping = ImportArrayMapping(schemaType, identifier, ns, repeats);
		if (arrayMapping == null)
		{
			return null;
		}
		ElementAccessor elementAccessor = new ElementAccessor();
		elementAccessor.Name = element.Name;
		elementAccessor.Namespace = ns;
		elementAccessor.Mapping = arrayMapping;
		if (arrayMapping.TypeDesc.IsNullable)
		{
			elementAccessor.IsNullable = element.IsNillable;
		}
		elementAccessor.Form = ElementForm(ns, element);
		return elementAccessor;
	}

	[RequiresUnreferencedCode("calls ImportChoiceGroup")]
	private ArrayMapping ImportArrayMapping(XmlSchemaType type, string identifier, string ns, bool repeats)
	{
		if (!(type is XmlSchemaComplexType))
		{
			return null;
		}
		if (!type.DerivedFrom.IsEmpty)
		{
			return null;
		}
		if (IsMixed(type))
		{
			return null;
		}
		Mapping mapping = (Mapping)base.ImportedMappings[type];
		if (mapping != null)
		{
			if (mapping is ArrayMapping)
			{
				return (ArrayMapping)mapping;
			}
			return null;
		}
		TypeItems typeItems = GetTypeItems(type);
		if (typeItems.Attributes != null && typeItems.Attributes.Count > 0)
		{
			return null;
		}
		if (typeItems.AnyAttribute != null)
		{
			return null;
		}
		if (typeItems.Particle == null)
		{
			return null;
		}
		XmlSchemaGroupBase particle = typeItems.Particle;
		ArrayMapping arrayMapping = new ArrayMapping();
		arrayMapping.TypeName = identifier;
		arrayMapping.Namespace = ns;
		if (particle is XmlSchemaChoice)
		{
			XmlSchemaChoice xmlSchemaChoice = (XmlSchemaChoice)particle;
			if (!xmlSchemaChoice.IsMultipleOccurrence)
			{
				return null;
			}
			bool needExplicitOrder = false;
			MemberMapping memberMapping = ImportChoiceGroup(xmlSchemaChoice, identifier, null, null, null, ns, groupRepeats: true, ref needExplicitOrder, allowDuplicates: false);
			if (memberMapping.ChoiceIdentifier != null)
			{
				return null;
			}
			arrayMapping.TypeDesc = memberMapping.TypeDesc;
			arrayMapping.Elements = memberMapping.Elements;
			arrayMapping.TypeName = ((type.Name == null || type.Name.Length == 0) ? ("ArrayOf" + CodeIdentifier.MakePascal(arrayMapping.TypeDesc.Name)) : type.Name);
		}
		else
		{
			if (!(particle is XmlSchemaAll) && !(particle is XmlSchemaSequence))
			{
				return null;
			}
			if (particle.Items.Count != 1 || !(particle.Items[0] is XmlSchemaElement))
			{
				return null;
			}
			XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)particle.Items[0];
			if (!xmlSchemaElement.IsMultipleOccurrence)
			{
				return null;
			}
			if (IsCyclicReferencedType(xmlSchemaElement, new List<string>(1) { identifier }))
			{
				return null;
			}
			ElementAccessor elementAccessor = ImportElement(xmlSchemaElement, identifier, typeof(TypeMapping), null, ns, topLevelElement: false);
			if (elementAccessor.Any)
			{
				return null;
			}
			arrayMapping.Elements = new ElementAccessor[1] { elementAccessor };
			arrayMapping.TypeDesc = elementAccessor.Mapping.TypeDesc.CreateArrayTypeDesc();
			arrayMapping.TypeName = ((type.Name == null || type.Name.Length == 0) ? ("ArrayOf" + CodeIdentifier.MakePascal(elementAccessor.Mapping.TypeDesc.Name)) : type.Name);
		}
		base.ImportedMappings[type] = arrayMapping;
		base.Scope.AddTypeMapping(arrayMapping);
		arrayMapping.TopLevelMapping = ImportStructType(type, ns, identifier, null, arrayLike: true);
		arrayMapping.TopLevelMapping.ReferencedByTopLevelElement = true;
		if (type.Name != null && type.Name.Length != 0)
		{
			ImportDerivedTypes(new XmlQualifiedName(identifier, ns));
		}
		return arrayMapping;
	}

	private bool IsCyclicReferencedType(XmlSchemaElement element, List<string> identifiers)
	{
		if (!element.RefName.IsEmpty)
		{
			XmlSchemaElement xmlSchemaElement = FindElement(element.RefName);
			string text = CodeIdentifier.MakeValid(Accessor.UnescapeName(xmlSchemaElement.Name));
			foreach (string identifier in identifiers)
			{
				if (text == identifier)
				{
					return true;
				}
			}
			identifiers.Add(text);
			XmlSchemaType schemaType = xmlSchemaElement.SchemaType;
			if (schemaType is XmlSchemaComplexType)
			{
				TypeItems typeItems = GetTypeItems(schemaType);
				if ((typeItems.Particle is XmlSchemaSequence || typeItems.Particle is XmlSchemaAll) && typeItems.Particle.Items.Count == 1 && typeItems.Particle.Items[0] is XmlSchemaElement)
				{
					XmlSchemaElement xmlSchemaElement2 = (XmlSchemaElement)typeItems.Particle.Items[0];
					if (xmlSchemaElement2.IsMultipleOccurrence)
					{
						return IsCyclicReferencedType(xmlSchemaElement2, identifiers);
					}
				}
			}
		}
		return false;
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private SpecialMapping ImportAnyMapping(XmlSchemaType type, string identifier, string ns, bool repeats)
	{
		if (type == null)
		{
			return null;
		}
		if (!type.DerivedFrom.IsEmpty)
		{
			return null;
		}
		bool flag = IsMixed(type);
		TypeItems typeItems = GetTypeItems(type);
		if (typeItems.Particle == null)
		{
			return null;
		}
		if (!(typeItems.Particle is XmlSchemaAll) && !(typeItems.Particle is XmlSchemaSequence))
		{
			return null;
		}
		if (typeItems.Attributes != null && typeItems.Attributes.Count > 0)
		{
			return null;
		}
		XmlSchemaGroupBase particle = typeItems.Particle;
		if (particle.Items.Count != 1 || !(particle.Items[0] is XmlSchemaAny))
		{
			return null;
		}
		XmlSchemaAny xmlSchemaAny = (XmlSchemaAny)particle.Items[0];
		SpecialMapping specialMapping = new SpecialMapping();
		if (typeItems.AnyAttribute != null && xmlSchemaAny.IsMultipleOccurrence && flag)
		{
			specialMapping.NamedAny = true;
			specialMapping.TypeDesc = base.Scope.GetTypeDesc(typeof(XmlElement));
		}
		else
		{
			if (typeItems.AnyAttribute != null || xmlSchemaAny.IsMultipleOccurrence)
			{
				return null;
			}
			specialMapping.TypeDesc = base.Scope.GetTypeDesc(flag ? typeof(XmlNode) : typeof(XmlElement));
		}
		TypeFlags typeFlags = TypeFlags.CanBeElementValue;
		if (typeItems.AnyAttribute != null || flag)
		{
			typeFlags |= TypeFlags.CanBeTextValue;
		}
		RunSchemaExtensions(specialMapping, XmlQualifiedName.Empty, null, xmlSchemaAny, typeFlags);
		specialMapping.TypeName = specialMapping.TypeDesc.Name;
		if (repeats)
		{
			specialMapping.TypeDesc = specialMapping.TypeDesc.CreateArrayTypeDesc();
		}
		return specialMapping;
	}

	[RequiresUnreferencedCode("calls ImportSubstitutionGroupMember")]
	private void ImportElementMember(XmlSchemaElement element, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, INameScope elementsScope, string ns, bool repeats, ref bool needExplicitOrder, bool allowDuplicates, bool allowUnboundedElements)
	{
		repeats |= element.IsMultipleOccurrence;
		XmlSchemaElement topLevelElement = GetTopLevelElement(element);
		if (topLevelElement != null && ImportSubstitutionGroupMember(topLevelElement, identifier, members, membersScope, ns, repeats, ref needExplicitOrder, allowDuplicates))
		{
			return;
		}
		ElementAccessor elementAccessor;
		if ((elementAccessor = ImportArray(element, identifier, ns, repeats)) == null)
		{
			elementAccessor = ImportElement(element, identifier, typeof(TypeMapping), null, ns, topLevelElement: false);
		}
		MemberMapping memberMapping = new MemberMapping();
		string identifier2 = CodeIdentifier.MakeValid(Accessor.UnescapeName(elementAccessor.Name));
		memberMapping.Name = membersScope.AddUnique(identifier2, memberMapping);
		if (memberMapping.Name.EndsWith("Specified", StringComparison.Ordinal))
		{
			identifier2 = memberMapping.Name;
			memberMapping.Name = membersScope.AddUnique(memberMapping.Name, memberMapping);
			membersScope.Remove(identifier2);
		}
		members.Add(memberMapping.Name, memberMapping);
		if (elementAccessor.Mapping.IsList)
		{
			elementAccessor.Mapping = GetDefaultMapping((TypeFlags)48);
			memberMapping.TypeDesc = elementAccessor.Mapping.TypeDesc;
		}
		else
		{
			memberMapping.TypeDesc = elementAccessor.Mapping.TypeDesc;
		}
		AddScopeElement(elementsScope, elementAccessor, ref needExplicitOrder, allowDuplicates);
		memberMapping.Elements = new ElementAccessor[1] { elementAccessor };
		if (element.IsMultipleOccurrence || repeats)
		{
			if (!allowUnboundedElements && elementAccessor.Mapping is ArrayMapping)
			{
				elementAccessor.Mapping = ((ArrayMapping)elementAccessor.Mapping).TopLevelMapping;
				elementAccessor.Mapping.ReferencedByTopLevelElement = false;
				elementAccessor.Mapping.ReferencedByElement = true;
			}
			memberMapping.TypeDesc = elementAccessor.Mapping.TypeDesc.CreateArrayTypeDesc();
		}
		if (element.MinOccurs == 0m && memberMapping.TypeDesc.IsValueType && !element.HasDefault && !memberMapping.TypeDesc.HasIsEmpty)
		{
			memberMapping.CheckSpecified = SpecifiedAccessor.ReadWrite;
		}
	}

	[RequiresUnreferencedCode("calls ImportAttribute")]
	private void ImportAttributeMember(XmlSchemaAttribute attribute, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, string ns)
	{
		AttributeAccessor attributeAccessor = ImportAttribute(attribute, identifier, ns, attribute);
		if (attributeAccessor != null)
		{
			MemberMapping memberMapping = new MemberMapping();
			memberMapping.Elements = Array.Empty<ElementAccessor>();
			memberMapping.Attribute = attributeAccessor;
			memberMapping.Name = CodeIdentifier.MakeValid(Accessor.UnescapeName(attributeAccessor.Name));
			memberMapping.Name = membersScope.AddUnique(memberMapping.Name, memberMapping);
			if (memberMapping.Name.EndsWith("Specified", StringComparison.Ordinal))
			{
				string name = memberMapping.Name;
				memberMapping.Name = membersScope.AddUnique(memberMapping.Name, memberMapping);
				membersScope.Remove(name);
			}
			members.Add(memberMapping.Name, memberMapping);
			memberMapping.TypeDesc = (attributeAccessor.IsList ? attributeAccessor.Mapping.TypeDesc.CreateArrayTypeDesc() : attributeAccessor.Mapping.TypeDesc);
			if ((attribute.Use == XmlSchemaUse.Optional || attribute.Use == XmlSchemaUse.None) && memberMapping.TypeDesc.IsValueType && !attribute.HasDefault && !memberMapping.TypeDesc.HasIsEmpty)
			{
				memberMapping.CheckSpecified = SpecifiedAccessor.ReadWrite;
			}
		}
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private void ImportAnyAttributeMember(XmlSchemaAnyAttribute any, CodeIdentifiers members, CodeIdentifiers membersScope)
	{
		SpecialMapping specialMapping = new SpecialMapping();
		specialMapping.TypeDesc = base.Scope.GetTypeDesc(typeof(XmlAttribute));
		specialMapping.TypeName = specialMapping.TypeDesc.Name;
		AttributeAccessor attributeAccessor = new AttributeAccessor();
		attributeAccessor.Any = true;
		attributeAccessor.Mapping = specialMapping;
		MemberMapping memberMapping = new MemberMapping();
		memberMapping.Elements = Array.Empty<ElementAccessor>();
		memberMapping.Attribute = attributeAccessor;
		memberMapping.Name = membersScope.MakeRightCase("AnyAttr");
		memberMapping.Name = membersScope.AddUnique(memberMapping.Name, memberMapping);
		members.Add(memberMapping.Name, memberMapping);
		memberMapping.TypeDesc = attributeAccessor.Mapping.TypeDesc;
		memberMapping.TypeDesc = memberMapping.TypeDesc.CreateArrayTypeDesc();
	}

	private bool KeepXmlnsDeclarations(XmlSchemaType type, out string xmlnsMemberName)
	{
		xmlnsMemberName = null;
		if (type.Annotation == null)
		{
			return false;
		}
		if (type.Annotation.Items == null || type.Annotation.Items.Count == 0)
		{
			return false;
		}
		foreach (XmlSchemaObject item in type.Annotation.Items)
		{
			if (!(item is XmlSchemaAppInfo))
			{
				continue;
			}
			XmlNode[] markup = ((XmlSchemaAppInfo)item).Markup;
			if (markup == null || markup.Length == 0)
			{
				continue;
			}
			XmlNode[] array = markup;
			foreach (XmlNode xmlNode in array)
			{
				if (!(xmlNode is XmlElement))
				{
					continue;
				}
				XmlElement xmlElement = (XmlElement)xmlNode;
				if (xmlElement.Name == "keepNamespaceDeclarations")
				{
					if (xmlElement.LastNode is XmlText)
					{
						xmlnsMemberName = ((XmlText)xmlElement.LastNode).Value.Trim(null);
					}
					return true;
				}
			}
		}
		return false;
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private void ImportXmlnsDeclarationsMember(XmlSchemaType type, CodeIdentifiers members, CodeIdentifiers membersScope)
	{
		if (KeepXmlnsDeclarations(type, out var xmlnsMemberName))
		{
			TypeDesc typeDesc = base.Scope.GetTypeDesc(typeof(XmlSerializerNamespaces));
			StructMapping structMapping = new StructMapping();
			structMapping.TypeDesc = typeDesc;
			structMapping.TypeName = structMapping.TypeDesc.Name;
			structMapping.Members = Array.Empty<MemberMapping>();
			structMapping.IncludeInSchema = false;
			structMapping.ReferencedByTopLevelElement = true;
			ElementAccessor elementAccessor = new ElementAccessor();
			elementAccessor.Mapping = structMapping;
			MemberMapping memberMapping = new MemberMapping();
			memberMapping.Elements = new ElementAccessor[1] { elementAccessor };
			memberMapping.Name = CodeIdentifier.MakeValid((xmlnsMemberName == null) ? "Namespaces" : xmlnsMemberName);
			memberMapping.Name = membersScope.AddUnique(memberMapping.Name, memberMapping);
			members.Add(memberMapping.Name, memberMapping);
			memberMapping.TypeDesc = typeDesc;
			memberMapping.Xmlns = new XmlnsAccessor();
			memberMapping.Ignore = true;
		}
	}

	[RequiresUnreferencedCode("calls ImportAnyAttributeMember")]
	private void ImportAttributeGroupMembers(XmlSchemaAttributeGroup group, string identifier, CodeIdentifiers members, CodeIdentifiers membersScope, string ns)
	{
		for (int i = 0; i < group.Attributes.Count; i++)
		{
			object obj = group.Attributes[i];
			if (obj is XmlSchemaAttributeGroup)
			{
				ImportAttributeGroupMembers((XmlSchemaAttributeGroup)obj, identifier, members, membersScope, ns);
			}
			else if (obj is XmlSchemaAttribute)
			{
				ImportAttributeMember((XmlSchemaAttribute)obj, identifier, members, membersScope, ns);
			}
		}
		if (group.AnyAttribute != null)
		{
			ImportAnyAttributeMember(group.AnyAttribute, members, membersScope);
		}
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private AttributeAccessor ImportSpecialAttribute(XmlQualifiedName name, string identifier)
	{
		PrimitiveMapping primitiveMapping = new PrimitiveMapping();
		primitiveMapping.TypeDesc = base.Scope.GetTypeDesc(typeof(string));
		primitiveMapping.TypeName = primitiveMapping.TypeDesc.DataType.Name;
		AttributeAccessor attributeAccessor = new AttributeAccessor();
		attributeAccessor.Name = name.Name;
		attributeAccessor.Namespace = "http://www.w3.org/XML/1998/namespace";
		attributeAccessor.CheckSpecial();
		attributeAccessor.Mapping = primitiveMapping;
		return attributeAccessor;
	}

	[RequiresUnreferencedCode("calls ImportSpecialAttribute")]
	private AttributeAccessor ImportAttribute(XmlSchemaAttribute attribute, string identifier, string ns, XmlSchemaAttribute defaultValueProvider)
	{
		if (attribute.Use == XmlSchemaUse.Prohibited)
		{
			return null;
		}
		if (!attribute.RefName.IsEmpty)
		{
			if (attribute.RefName.Namespace == "http://www.w3.org/XML/1998/namespace")
			{
				return ImportSpecialAttribute(attribute.RefName, identifier);
			}
			return ImportAttribute(FindAttribute(attribute.RefName), identifier, attribute.RefName.Namespace, defaultValueProvider);
		}
		if (attribute.Name.Length == 0)
		{
			throw new InvalidOperationException(System.SR.XmlAttributeHasNoName);
		}
		identifier = ((identifier.Length != 0) ? (identifier + CodeIdentifier.MakePascal(attribute.Name)) : CodeIdentifier.MakeValid(attribute.Name));
		TypeMapping typeMapping = ((!attribute.SchemaTypeName.IsEmpty) ? ImportType(attribute.SchemaTypeName, typeof(TypeMapping), null, TypeFlags.CanBeAttributeValue, addref: false) : ((attribute.SchemaType == null) ? GetDefaultMapping(TypeFlags.CanBeAttributeValue) : ImportDataType(attribute.SchemaType, ns, identifier, null, TypeFlags.CanBeAttributeValue, isList: false)));
		if (typeMapping != null && !typeMapping.TypeDesc.IsMappedType)
		{
			RunSchemaExtensions(typeMapping, attribute.SchemaTypeName, attribute.SchemaType, attribute, (TypeFlags)56);
		}
		AttributeAccessor attributeAccessor = new AttributeAccessor();
		attributeAccessor.Name = attribute.Name;
		attributeAccessor.Namespace = ns;
		attributeAccessor.Form = AttributeForm(ns, attribute);
		attributeAccessor.CheckSpecial();
		attributeAccessor.Mapping = typeMapping;
		attributeAccessor.IsList = typeMapping.IsList;
		attributeAccessor.IsOptional = attribute.Use != XmlSchemaUse.Required;
		if (defaultValueProvider.DefaultValue != null)
		{
			attributeAccessor.Default = defaultValueProvider.DefaultValue;
		}
		else if (defaultValueProvider.FixedValue != null)
		{
			attributeAccessor.Default = defaultValueProvider.FixedValue;
			attributeAccessor.IsFixed = true;
		}
		else if (attribute != defaultValueProvider)
		{
			if (attribute.DefaultValue != null)
			{
				attributeAccessor.Default = attribute.DefaultValue;
			}
			else if (attribute.FixedValue != null)
			{
				attributeAccessor.Default = attribute.FixedValue;
				attributeAccessor.IsFixed = true;
			}
		}
		return attributeAccessor;
	}

	[RequiresUnreferencedCode("calls ImportStructDataType")]
	private TypeMapping ImportDataType(XmlSchemaSimpleType dataType, string typeNs, string identifier, Type baseType, TypeFlags flags, bool isList)
	{
		if (baseType != null)
		{
			return ImportStructDataType(dataType, typeNs, identifier, baseType);
		}
		TypeMapping typeMapping = ImportNonXsdPrimitiveDataType(dataType, typeNs, flags);
		if (typeMapping != null)
		{
			return typeMapping;
		}
		if (dataType.Content is XmlSchemaSimpleTypeRestriction)
		{
			XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)dataType.Content;
			foreach (XmlSchemaObject facet in xmlSchemaSimpleTypeRestriction.Facets)
			{
				if (facet is XmlSchemaEnumerationFacet)
				{
					return ImportEnumeratedDataType(dataType, typeNs, identifier, flags, isList);
				}
			}
			if (xmlSchemaSimpleTypeRestriction.BaseType != null)
			{
				return ImportDataType(xmlSchemaSimpleTypeRestriction.BaseType, typeNs, identifier, null, flags, isList: false);
			}
			AddReference(xmlSchemaSimpleTypeRestriction.BaseTypeName, base.TypesInUse, System.SR.XmlCircularTypeReference);
			typeMapping = ImportDataType(FindDataType(xmlSchemaSimpleTypeRestriction.BaseTypeName, flags), xmlSchemaSimpleTypeRestriction.BaseTypeName.Namespace, identifier, null, flags, isList: false);
			if (xmlSchemaSimpleTypeRestriction.BaseTypeName.Namespace != "http://www.w3.org/2001/XMLSchema")
			{
				RemoveReference(xmlSchemaSimpleTypeRestriction.BaseTypeName, base.TypesInUse);
			}
			return typeMapping;
		}
		if (dataType.Content is XmlSchemaSimpleTypeList || dataType.Content is XmlSchemaSimpleTypeUnion)
		{
			if (dataType.Content is XmlSchemaSimpleTypeList)
			{
				XmlSchemaSimpleTypeList xmlSchemaSimpleTypeList = (XmlSchemaSimpleTypeList)dataType.Content;
				if (xmlSchemaSimpleTypeList.ItemType != null)
				{
					typeMapping = ImportDataType(xmlSchemaSimpleTypeList.ItemType, typeNs, identifier, null, flags, isList: true);
					if (typeMapping != null)
					{
						typeMapping.TypeName = dataType.Name;
						return typeMapping;
					}
				}
				else if (xmlSchemaSimpleTypeList.ItemTypeName != null && !xmlSchemaSimpleTypeList.ItemTypeName.IsEmpty)
				{
					typeMapping = ImportType(xmlSchemaSimpleTypeList.ItemTypeName, typeof(TypeMapping), null, TypeFlags.CanBeAttributeValue, addref: true);
					if (typeMapping != null && typeMapping is PrimitiveMapping)
					{
						((PrimitiveMapping)typeMapping).IsList = true;
						return typeMapping;
					}
				}
			}
			return GetDefaultMapping(flags);
		}
		return ImportPrimitiveDataType(dataType, flags);
	}

	[RequiresUnreferencedCode("calls FindType")]
	private TypeMapping ImportEnumeratedDataType(XmlSchemaSimpleType dataType, string typeNs, string identifier, TypeFlags flags, bool isList)
	{
		TypeMapping typeMapping = (TypeMapping)base.ImportedMappings[dataType];
		if (typeMapping != null)
		{
			return typeMapping;
		}
		XmlSchemaType xmlSchemaType = dataType;
		while (!xmlSchemaType.DerivedFrom.IsEmpty)
		{
			xmlSchemaType = FindType(xmlSchemaType.DerivedFrom, (TypeFlags)40);
		}
		if (xmlSchemaType is XmlSchemaComplexType)
		{
			return null;
		}
		TypeDesc typeDesc = base.Scope.GetTypeDesc((XmlSchemaSimpleType)xmlSchemaType);
		if (typeDesc != null && typeDesc.FullName != typeof(string).FullName)
		{
			return ImportPrimitiveDataType(dataType, flags);
		}
		identifier = Accessor.UnescapeName(identifier);
		string text = GenerateUniqueTypeName(identifier);
		EnumMapping enumMapping = new EnumMapping();
		enumMapping.IsReference = base.Schemas.IsReference(dataType);
		enumMapping.TypeDesc = new TypeDesc(text, text, TypeKind.Enum, null, TypeFlags.None);
		if (dataType.Name != null && dataType.Name.Length > 0)
		{
			enumMapping.TypeName = identifier;
		}
		enumMapping.Namespace = typeNs;
		enumMapping.IsFlags = isList;
		CodeIdentifiers codeIdentifiers = new CodeIdentifiers();
		XmlSchemaSimpleTypeContent content = dataType.Content;
		if (content is XmlSchemaSimpleTypeRestriction)
		{
			XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)content;
			for (int i = 0; i < xmlSchemaSimpleTypeRestriction.Facets.Count; i++)
			{
				object obj = xmlSchemaSimpleTypeRestriction.Facets[i];
				if (obj is XmlSchemaEnumerationFacet)
				{
					XmlSchemaEnumerationFacet xmlSchemaEnumerationFacet = (XmlSchemaEnumerationFacet)obj;
					if (typeDesc != null && typeDesc.HasCustomFormatter)
					{
						XmlCustomFormatter.ToDefaultValue(xmlSchemaEnumerationFacet.Value, typeDesc.FormatterName);
					}
					ConstantMapping constantMapping = new ConstantMapping();
					string identifier2 = CodeIdentifier.MakeValid(xmlSchemaEnumerationFacet.Value);
					constantMapping.Name = codeIdentifiers.AddUnique(identifier2, constantMapping);
					constantMapping.XmlName = xmlSchemaEnumerationFacet.Value;
					constantMapping.Value = i;
				}
			}
		}
		enumMapping.Constants = (ConstantMapping[])codeIdentifiers.ToArray(typeof(ConstantMapping));
		if (isList && enumMapping.Constants.Length > 63)
		{
			typeMapping = GetDefaultMapping((TypeFlags)56);
			base.ImportedMappings.Add(dataType, typeMapping);
			return typeMapping;
		}
		base.ImportedMappings.Add(dataType, enumMapping);
		base.Scope.AddTypeMapping(enumMapping);
		return enumMapping;
	}

	private EnumMapping ImportEnumeratedChoice(ElementAccessor[] choice, string typeNs, string typeName)
	{
		typeName = GenerateUniqueTypeName(Accessor.UnescapeName(typeName), typeNs);
		EnumMapping enumMapping = new EnumMapping();
		enumMapping.TypeDesc = new TypeDesc(typeName, typeName, TypeKind.Enum, null, TypeFlags.None);
		enumMapping.TypeName = typeName;
		enumMapping.Namespace = typeNs;
		enumMapping.IsFlags = false;
		enumMapping.IncludeInSchema = false;
		if (GenerateOrder)
		{
			Array.Sort(choice, new ElementComparer());
		}
		CodeIdentifiers codeIdentifiers = new CodeIdentifiers();
		for (int i = 0; i < choice.Length; i++)
		{
			ElementAccessor elementAccessor = choice[i];
			ConstantMapping constantMapping = new ConstantMapping();
			string identifier = CodeIdentifier.MakeValid(elementAccessor.Name);
			constantMapping.Name = codeIdentifiers.AddUnique(identifier, constantMapping);
			constantMapping.XmlName = elementAccessor.ToString(typeNs);
			constantMapping.Value = i;
		}
		enumMapping.Constants = (ConstantMapping[])codeIdentifiers.ToArray(typeof(ConstantMapping));
		base.Scope.AddTypeMapping(enumMapping);
		return enumMapping;
	}

	[RequiresUnreferencedCode("calls GetDataTypeSource")]
	private PrimitiveMapping ImportPrimitiveDataType(XmlSchemaSimpleType dataType, TypeFlags flags)
	{
		TypeDesc dataTypeSource = GetDataTypeSource(dataType, flags);
		PrimitiveMapping primitiveMapping = new PrimitiveMapping();
		primitiveMapping.TypeDesc = dataTypeSource;
		primitiveMapping.TypeName = dataTypeSource.DataType.Name;
		primitiveMapping.Namespace = (primitiveMapping.TypeDesc.IsXsdType ? "http://www.w3.org/2001/XMLSchema" : "http://microsoft.com/wsdl/types/");
		return primitiveMapping;
	}

	private PrimitiveMapping ImportNonXsdPrimitiveDataType(XmlSchemaSimpleType dataType, string ns, TypeFlags flags)
	{
		PrimitiveMapping primitiveMapping = null;
		TypeDesc typeDesc = null;
		if (dataType.Name != null && dataType.Name.Length != 0)
		{
			typeDesc = base.Scope.GetTypeDesc(dataType.Name, ns, flags);
			if (typeDesc != null)
			{
				primitiveMapping = new PrimitiveMapping();
				primitiveMapping.TypeDesc = typeDesc;
				primitiveMapping.TypeName = typeDesc.DataType.Name;
				primitiveMapping.Namespace = (primitiveMapping.TypeDesc.IsXsdType ? "http://www.w3.org/2001/XMLSchema" : ns);
			}
		}
		return primitiveMapping;
	}

	private XmlSchemaGroup FindGroup(XmlQualifiedName name)
	{
		XmlSchemaGroup xmlSchemaGroup = (XmlSchemaGroup)base.Schemas.Find(name, typeof(XmlSchemaGroup));
		if (xmlSchemaGroup == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlMissingGroup, name.Name));
		}
		return xmlSchemaGroup;
	}

	private XmlSchemaAttributeGroup FindAttributeGroup(XmlQualifiedName name)
	{
		XmlSchemaAttributeGroup xmlSchemaAttributeGroup = (XmlSchemaAttributeGroup)base.Schemas.Find(name, typeof(XmlSchemaAttributeGroup));
		if (xmlSchemaAttributeGroup == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlMissingAttributeGroup, name.Name));
		}
		return xmlSchemaAttributeGroup;
	}

	internal static XmlQualifiedName BaseTypeName(XmlSchemaSimpleType dataType)
	{
		XmlSchemaSimpleTypeContent content = dataType.Content;
		if (content is XmlSchemaSimpleTypeRestriction)
		{
			return ((XmlSchemaSimpleTypeRestriction)content).BaseTypeName;
		}
		if (content is XmlSchemaSimpleTypeList)
		{
			XmlSchemaSimpleTypeList xmlSchemaSimpleTypeList = (XmlSchemaSimpleTypeList)content;
			if (xmlSchemaSimpleTypeList.ItemTypeName != null && !xmlSchemaSimpleTypeList.ItemTypeName.IsEmpty)
			{
				return xmlSchemaSimpleTypeList.ItemTypeName;
			}
			if (xmlSchemaSimpleTypeList.ItemType != null)
			{
				return BaseTypeName(xmlSchemaSimpleTypeList.ItemType);
			}
		}
		return new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");
	}

	[RequiresUnreferencedCode("calls FindDataType")]
	private TypeDesc GetDataTypeSource(XmlSchemaSimpleType dataType, TypeFlags flags)
	{
		TypeDesc typeDesc = null;
		if (dataType.Name != null && dataType.Name.Length != 0)
		{
			typeDesc = base.Scope.GetTypeDesc(dataType);
			if (typeDesc != null)
			{
				return typeDesc;
			}
		}
		XmlQualifiedName xmlQualifiedName = BaseTypeName(dataType);
		AddReference(xmlQualifiedName, base.TypesInUse, System.SR.XmlCircularTypeReference);
		typeDesc = GetDataTypeSource(FindDataType(xmlQualifiedName, flags), flags);
		if (xmlQualifiedName.Namespace != "http://www.w3.org/2001/XMLSchema")
		{
			RemoveReference(xmlQualifiedName, base.TypesInUse);
		}
		return typeDesc;
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private XmlSchemaSimpleType FindDataType(XmlQualifiedName name, TypeFlags flags)
	{
		if (name == null || name.IsEmpty)
		{
			return (XmlSchemaSimpleType)base.Scope.GetTypeDesc(typeof(string)).DataType;
		}
		TypeDesc typeDesc = base.Scope.GetTypeDesc(name.Name, name.Namespace, flags);
		if (typeDesc != null && typeDesc.DataType is XmlSchemaSimpleType)
		{
			return (XmlSchemaSimpleType)typeDesc.DataType;
		}
		XmlSchemaSimpleType xmlSchemaSimpleType = (XmlSchemaSimpleType)base.Schemas.Find(name, typeof(XmlSchemaSimpleType));
		if (xmlSchemaSimpleType != null)
		{
			return xmlSchemaSimpleType;
		}
		if (name.Namespace == "http://www.w3.org/2001/XMLSchema")
		{
			return (XmlSchemaSimpleType)base.Scope.GetTypeDesc("string", "http://www.w3.org/2001/XMLSchema", flags).DataType;
		}
		if (name.Name == "Array" && name.Namespace == "http://schemas.xmlsoap.org/soap/encoding/")
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidEncoding, name));
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.XmlMissingDataType, name));
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private XmlSchemaType FindType(XmlQualifiedName name, TypeFlags flags)
	{
		if (name == null || name.IsEmpty)
		{
			return base.Scope.GetTypeDesc(typeof(string)).DataType;
		}
		object obj = base.Schemas.Find(name, typeof(XmlSchemaComplexType));
		if (obj != null)
		{
			return (XmlSchemaComplexType)obj;
		}
		return FindDataType(name, flags);
	}

	private XmlSchemaElement FindElement(XmlQualifiedName name)
	{
		XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)base.Schemas.Find(name, typeof(XmlSchemaElement));
		if (xmlSchemaElement == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlMissingElement, name));
		}
		return xmlSchemaElement;
	}

	private XmlSchemaAttribute FindAttribute(XmlQualifiedName name)
	{
		XmlSchemaAttribute xmlSchemaAttribute = (XmlSchemaAttribute)base.Schemas.Find(name, typeof(XmlSchemaAttribute));
		if (xmlSchemaAttribute == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlMissingAttribute, name.Name));
		}
		return xmlSchemaAttribute;
	}

	private XmlSchemaForm ElementForm(string ns, XmlSchemaElement element)
	{
		if (element.Form == XmlSchemaForm.None)
		{
			XmlSchemaObject xmlSchemaObject = element;
			while (xmlSchemaObject.Parent != null)
			{
				xmlSchemaObject = xmlSchemaObject.Parent;
			}
			if (xmlSchemaObject is XmlSchema xmlSchema)
			{
				if (ns == null || ns.Length == 0)
				{
					if (xmlSchema.ElementFormDefault != 0)
					{
						return xmlSchema.ElementFormDefault;
					}
					return XmlSchemaForm.Unqualified;
				}
				XmlSchemas.Preprocess(xmlSchema);
				if (element.QualifiedName.Namespace != null && element.QualifiedName.Namespace.Length != 0)
				{
					return XmlSchemaForm.Qualified;
				}
				return XmlSchemaForm.Unqualified;
			}
			return XmlSchemaForm.Qualified;
		}
		return element.Form;
	}

	private XmlSchemaForm AttributeForm(string ns, XmlSchemaAttribute attribute)
	{
		if (attribute.Form == XmlSchemaForm.None)
		{
			XmlSchemaObject xmlSchemaObject = attribute;
			while (xmlSchemaObject.Parent != null)
			{
				xmlSchemaObject = xmlSchemaObject.Parent;
			}
			if (xmlSchemaObject is XmlSchema xmlSchema)
			{
				if (ns == null || ns.Length == 0)
				{
					if (xmlSchema.AttributeFormDefault != 0)
					{
						return xmlSchema.AttributeFormDefault;
					}
					return XmlSchemaForm.Unqualified;
				}
				XmlSchemas.Preprocess(xmlSchema);
				if (attribute.QualifiedName.Namespace != null && attribute.QualifiedName.Namespace.Length != 0)
				{
					return XmlSchemaForm.Qualified;
				}
				return XmlSchemaForm.Unqualified;
			}
			return XmlSchemaForm.Unqualified;
		}
		return attribute.Form;
	}
}
