using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Xml.Schema;

namespace System.Xml.Serialization;

public class XmlReflectionImporter
{
	private enum ImportContext
	{
		Text,
		Attribute,
		Element
	}

	private readonly TypeScope _typeScope;

	private readonly XmlAttributeOverrides _attributeOverrides;

	private readonly XmlAttributes _defaultAttributes = new XmlAttributes();

	private readonly NameTable _types = new NameTable();

	private readonly NameTable _nullables = new NameTable();

	private readonly NameTable _elements = new NameTable();

	private NameTable _xsdAttributes;

	private Hashtable _specials;

	private readonly Hashtable _anonymous = new Hashtable();

	private NameTable _serializables;

	private StructMapping _root;

	private readonly string _defaultNs;

	private readonly ModelScope _modelScope;

	private int _arrayNestingLevel;

	private XmlArrayItemAttributes _savedArrayItemAttributes;

	private string _savedArrayNamespace;

	private int _choiceNum = 1;

	public XmlReflectionImporter()
		: this(null, null)
	{
	}

	public XmlReflectionImporter(string? defaultNamespace)
		: this(null, defaultNamespace)
	{
	}

	public XmlReflectionImporter(XmlAttributeOverrides? attributeOverrides)
		: this(attributeOverrides, null)
	{
	}

	public XmlReflectionImporter(XmlAttributeOverrides? attributeOverrides, string? defaultNamespace)
	{
		if (defaultNamespace == null)
		{
			defaultNamespace = string.Empty;
		}
		if (attributeOverrides == null)
		{
			attributeOverrides = new XmlAttributeOverrides();
		}
		_attributeOverrides = attributeOverrides;
		_defaultNs = defaultNamespace;
		_typeScope = new TypeScope();
		_modelScope = new ModelScope(_typeScope);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void IncludeTypes(ICustomAttributeProvider provider)
	{
		IncludeTypes(provider, new RecursionLimiter());
	}

	[RequiresUnreferencedCode("calls IncludeType")]
	private void IncludeTypes(ICustomAttributeProvider provider, RecursionLimiter limiter)
	{
		object[] customAttributes = provider.GetCustomAttributes(typeof(XmlIncludeAttribute), inherit: false);
		for (int i = 0; i < customAttributes.Length; i++)
		{
			Type type = ((XmlIncludeAttribute)customAttributes[i]).Type;
			IncludeType(type, limiter);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void IncludeType(Type type)
	{
		IncludeType(type, new RecursionLimiter());
	}

	[RequiresUnreferencedCode("calls ImportTypeMapping")]
	private void IncludeType(Type type, RecursionLimiter limiter)
	{
		int arrayNestingLevel = _arrayNestingLevel;
		XmlArrayItemAttributes savedArrayItemAttributes = _savedArrayItemAttributes;
		string savedArrayNamespace = _savedArrayNamespace;
		_arrayNestingLevel = 0;
		_savedArrayItemAttributes = null;
		_savedArrayNamespace = null;
		TypeMapping typeMapping = ImportTypeMapping(_modelScope.GetTypeModel(type), _defaultNs, ImportContext.Element, string.Empty, null, limiter);
		if (typeMapping.IsAnonymousType && !typeMapping.TypeDesc.IsSpecial)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlAnonymousInclude, type.FullName));
		}
		_arrayNestingLevel = arrayNestingLevel;
		_savedArrayItemAttributes = savedArrayItemAttributes;
		_savedArrayNamespace = savedArrayNamespace;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlTypeMapping ImportTypeMapping(Type type)
	{
		return ImportTypeMapping(type, null, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlTypeMapping ImportTypeMapping(Type type, string? defaultNamespace)
	{
		return ImportTypeMapping(type, null, defaultNamespace);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlTypeMapping ImportTypeMapping(Type type, XmlRootAttribute? root)
	{
		return ImportTypeMapping(type, root, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlTypeMapping ImportTypeMapping(Type type, XmlRootAttribute? root, string? defaultNamespace)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		XmlTypeMapping xmlTypeMapping = new XmlTypeMapping(_typeScope, ImportElement(_modelScope.GetTypeModel(type), root, defaultNamespace, new RecursionLimiter()));
		xmlTypeMapping.SetKeyInternal(XmlMapping.GenerateKey(type, root, defaultNamespace));
		xmlTypeMapping.GenerateSerializer = true;
		return xmlTypeMapping;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, XmlReflectionMember[] members, bool hasWrapperElement)
	{
		return ImportMembersMapping(elementName, ns, members, hasWrapperElement, rpc: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, XmlReflectionMember[] members, bool hasWrapperElement, bool rpc)
	{
		return ImportMembersMapping(elementName, ns, members, hasWrapperElement, rpc, openModel: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, XmlReflectionMember[] members, bool hasWrapperElement, bool rpc, bool openModel)
	{
		return ImportMembersMapping(elementName, ns, members, hasWrapperElement, rpc, openModel, XmlMappingAccess.Read | XmlMappingAccess.Write);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, XmlReflectionMember[] members, bool hasWrapperElement, bool rpc, bool openModel, XmlMappingAccess access)
	{
		ElementAccessor elementAccessor = new ElementAccessor();
		elementAccessor.Name = ((elementName == null || elementName.Length == 0) ? elementName : XmlConvert.EncodeLocalName(elementName));
		elementAccessor.Namespace = ns;
		MembersMapping membersMapping = (MembersMapping)(elementAccessor.Mapping = ImportMembersMapping(members, ns, hasWrapperElement, rpc, openModel, new RecursionLimiter()));
		elementAccessor.Form = XmlSchemaForm.Qualified;
		if (!rpc)
		{
			if (hasWrapperElement)
			{
				elementAccessor = (ElementAccessor)ReconcileAccessor(elementAccessor, _elements);
			}
			else
			{
				MemberMapping[] members2 = membersMapping.Members;
				foreach (MemberMapping memberMapping in members2)
				{
					if (memberMapping.Elements != null && memberMapping.Elements.Length != 0)
					{
						memberMapping.Elements[0] = (ElementAccessor)ReconcileAccessor(memberMapping.Elements[0], _elements);
					}
				}
			}
		}
		XmlMembersMapping xmlMembersMapping = new XmlMembersMapping(_typeScope, elementAccessor, access);
		xmlMembersMapping.GenerateSerializer = true;
		return xmlMembersMapping;
	}

	private XmlAttributes GetAttributes(Type type, bool canBeSimpleType)
	{
		XmlAttributes xmlAttributes = _attributeOverrides[type];
		if (xmlAttributes != null)
		{
			return xmlAttributes;
		}
		if (canBeSimpleType && TypeScope.IsKnownType(type))
		{
			return _defaultAttributes;
		}
		return new XmlAttributes(type);
	}

	private XmlAttributes GetAttributes(MemberInfo memberInfo)
	{
		XmlAttributes xmlAttributes = _attributeOverrides[memberInfo.DeclaringType, memberInfo.Name];
		if (xmlAttributes != null)
		{
			return xmlAttributes;
		}
		return new XmlAttributes(memberInfo);
	}

	[RequiresUnreferencedCode("calls ImportTypeMapping")]
	private ElementAccessor ImportElement(TypeModel model, XmlRootAttribute root, string defaultNamespace, RecursionLimiter limiter)
	{
		XmlAttributes attributes = GetAttributes(model.Type, canBeSimpleType: true);
		if (root == null)
		{
			root = attributes.XmlRoot;
		}
		string text = root?.Namespace;
		if (text == null)
		{
			text = defaultNamespace;
		}
		if (text == null)
		{
			text = _defaultNs;
		}
		_arrayNestingLevel = -1;
		_savedArrayItemAttributes = null;
		_savedArrayNamespace = null;
		ElementAccessor elementAccessor = CreateElementAccessor(ImportTypeMapping(model, text, ImportContext.Element, string.Empty, attributes, limiter), text);
		if (root != null)
		{
			if (root.ElementName.Length > 0)
			{
				elementAccessor.Name = XmlConvert.EncodeLocalName(root.ElementName);
			}
			if (root.GetIsNullableSpecified() && !root.IsNullable && model.TypeDesc.IsOptionalValue)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidNotNullable, model.TypeDesc.BaseTypeDesc.FullName, "XmlRoot"));
			}
			elementAccessor.IsNullable = (root.GetIsNullableSpecified() ? root.IsNullable : (model.TypeDesc.IsNullable || model.TypeDesc.IsOptionalValue));
			CheckNullable(elementAccessor.IsNullable, model.TypeDesc, elementAccessor.Mapping);
		}
		else
		{
			elementAccessor.IsNullable = model.TypeDesc.IsNullable || model.TypeDesc.IsOptionalValue;
		}
		elementAccessor.Form = XmlSchemaForm.Qualified;
		return (ElementAccessor)ReconcileAccessor(elementAccessor, _elements);
	}

	private static string GetMappingName(Mapping mapping)
	{
		if (mapping is MembersMapping)
		{
			return "(method)";
		}
		if (mapping is TypeMapping)
		{
			return ((TypeMapping)mapping).TypeDesc.FullName;
		}
		throw new ArgumentException(System.SR.XmlInternalError, "mapping");
	}

	private ElementAccessor ReconcileLocalAccessor(ElementAccessor accessor, string ns)
	{
		if (accessor.Namespace == ns)
		{
			return accessor;
		}
		return (ElementAccessor)ReconcileAccessor(accessor, _elements);
	}

	private Accessor ReconcileAccessor(Accessor accessor, NameTable accessors)
	{
		if (accessor.Any && accessor.Name.Length == 0)
		{
			return accessor;
		}
		Accessor accessor2 = (Accessor)accessors[accessor.Name, accessor.Namespace];
		if (accessor2 == null)
		{
			accessor.IsTopLevelInSchema = true;
			accessors.Add(accessor.Name, accessor.Namespace, accessor);
			return accessor;
		}
		if (accessor2.Mapping == accessor.Mapping)
		{
			return accessor2;
		}
		if (!(accessor.Mapping is MembersMapping) && !(accessor2.Mapping is MembersMapping) && (accessor.Mapping.TypeDesc == accessor2.Mapping.TypeDesc || (accessor2.Mapping is NullableMapping && accessor.Mapping.TypeDesc == ((NullableMapping)accessor2.Mapping).BaseMapping.TypeDesc) || (accessor.Mapping is NullableMapping && ((NullableMapping)accessor.Mapping).BaseMapping.TypeDesc == accessor2.Mapping.TypeDesc)))
		{
			string text = Convert.ToString(accessor.Default, CultureInfo.InvariantCulture);
			string text2 = Convert.ToString(accessor2.Default, CultureInfo.InvariantCulture);
			if (text == text2)
			{
				return accessor2;
			}
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlCannotReconcileAccessorDefault, accessor.Name, accessor.Namespace, text, text2));
		}
		if (accessor.Mapping is MembersMapping || accessor2.Mapping is MembersMapping)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlMethodTypeNameConflict, accessor.Name, accessor.Namespace));
		}
		if (accessor.Mapping is ArrayMapping)
		{
			if (!(accessor2.Mapping is ArrayMapping))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlCannotReconcileAccessor, accessor.Name, accessor.Namespace, GetMappingName(accessor2.Mapping), GetMappingName(accessor.Mapping)));
			}
			ArrayMapping arrayMapping = (ArrayMapping)accessor.Mapping;
			ArrayMapping arrayMapping2 = (arrayMapping.IsAnonymousType ? null : ((ArrayMapping)_types[accessor2.Mapping.TypeName, accessor2.Mapping.Namespace]));
			ArrayMapping next = arrayMapping2;
			while (arrayMapping2 != null)
			{
				if (arrayMapping2 == accessor.Mapping)
				{
					return accessor2;
				}
				arrayMapping2 = arrayMapping2.Next;
			}
			arrayMapping.Next = next;
			if (!arrayMapping.IsAnonymousType)
			{
				_types[accessor2.Mapping.TypeName, accessor2.Mapping.Namespace] = arrayMapping;
			}
			return accessor2;
		}
		if (accessor is AttributeAccessor)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlCannotReconcileAttributeAccessor, accessor.Name, accessor.Namespace, GetMappingName(accessor2.Mapping), GetMappingName(accessor.Mapping)));
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.XmlCannotReconcileAccessor, accessor.Name, accessor.Namespace, GetMappingName(accessor2.Mapping), GetMappingName(accessor.Mapping)));
	}

	private Exception CreateReflectionException(string context, Exception e)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlReflectionError, context), e);
	}

	private Exception CreateTypeReflectionException(string context, Exception e)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlTypeReflectionError, context), e);
	}

	private Exception CreateMemberReflectionException(FieldModel model, Exception e)
	{
		return new InvalidOperationException(System.SR.Format(model.IsProperty ? System.SR.XmlPropertyReflectionError : System.SR.XmlFieldReflectionError, model.Name), e);
	}

	[RequiresUnreferencedCode("calls ImportTypeMapping")]
	private TypeMapping ImportTypeMapping(TypeModel model, string ns, ImportContext context, string dataType, XmlAttributes a, RecursionLimiter limiter)
	{
		return ImportTypeMapping(model, ns, context, dataType, a, repeats: false, openModel: false, limiter);
	}

	[RequiresUnreferencedCode("calls ImportEnumMapping")]
	private TypeMapping ImportTypeMapping(TypeModel model, string ns, ImportContext context, string dataType, XmlAttributes a, bool repeats, bool openModel, RecursionLimiter limiter)
	{
		try
		{
			if (dataType.Length > 0)
			{
				TypeDesc typeDesc = (TypeScope.IsOptionalValue(model.Type) ? model.TypeDesc.BaseTypeDesc : model.TypeDesc);
				if (!typeDesc.IsPrimitive)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidDataTypeUsage, dataType, "XmlElementAttribute.DataType"));
				}
				TypeDesc typeDesc2 = _typeScope.GetTypeDesc(dataType, "http://www.w3.org/2001/XMLSchema");
				if (typeDesc2 == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidXsdDataType, dataType, "XmlElementAttribute.DataType", new XmlQualifiedName(dataType, "http://www.w3.org/2001/XMLSchema").ToString()));
				}
				if (typeDesc.FullName != typeDesc2.FullName)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlDataTypeMismatch, dataType, "XmlElementAttribute.DataType", typeDesc.FullName));
				}
			}
			if (a == null)
			{
				a = GetAttributes(model.Type, canBeSimpleType: false);
			}
			if (((uint)a.XmlFlags & 0xFFFFFF3Fu) != 0)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidTypeAttributes, model.Type.FullName));
			}
			switch (model.TypeDesc.Kind)
			{
			case TypeKind.Enum:
				return ImportEnumMapping((EnumModel)model, ns, repeats);
			case TypeKind.Primitive:
				if (a.XmlFlags != 0)
				{
					throw InvalidAttributeUseException(model.Type);
				}
				return ImportPrimitiveMapping((PrimitiveModel)model, context, dataType, repeats);
			case TypeKind.Array:
			case TypeKind.Collection:
			case TypeKind.Enumerable:
			{
				if (context != ImportContext.Element)
				{
					throw UnsupportedException(model.TypeDesc, context);
				}
				_arrayNestingLevel++;
				ArrayMapping result = ImportArrayLikeMapping((ArrayModel)model, ns, limiter);
				_arrayNestingLevel--;
				return result;
			}
			case TypeKind.Root:
			case TypeKind.Struct:
			case TypeKind.Class:
				if (context != ImportContext.Element)
				{
					throw UnsupportedException(model.TypeDesc, context);
				}
				if (model.TypeDesc.IsOptionalValue)
				{
					TypeDesc typeDesc3 = (string.IsNullOrEmpty(dataType) ? model.TypeDesc.BaseTypeDesc : _typeScope.GetTypeDesc(dataType, "http://www.w3.org/2001/XMLSchema"));
					string typeName = ((typeDesc3.DataType == null) ? typeDesc3.Name : typeDesc3.DataType.Name);
					TypeMapping typeMapping = GetTypeMapping(typeName, ns, typeDesc3, _types, null);
					if (typeMapping == null)
					{
						typeMapping = ImportTypeMapping(_modelScope.GetTypeModel(model.TypeDesc.BaseTypeDesc.Type), ns, context, dataType, null, repeats, openModel, limiter);
					}
					return CreateNullableMapping(typeMapping, model.TypeDesc.Type);
				}
				return ImportStructLikeMapping((StructModel)model, ns, openModel, a, limiter);
			default:
				if (model.TypeDesc.Kind == TypeKind.Serializable)
				{
					if (((uint)a.XmlFlags & 0xFFFFFFBFu) != 0)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlSerializableAttributes, model.TypeDesc.FullName, "XmlSchemaProviderAttribute"));
					}
				}
				else if (a.XmlFlags != 0)
				{
					throw InvalidAttributeUseException(model.Type);
				}
				if (model.TypeDesc.IsSpecial)
				{
					return ImportSpecialMapping(model.Type, model.TypeDesc, ns, context, limiter);
				}
				throw UnsupportedException(model.TypeDesc, context);
			}
		}
		catch (Exception ex)
		{
			if (ex is OutOfMemoryException)
			{
				throw;
			}
			throw CreateTypeReflectionException(model.TypeDesc.FullName, ex);
		}
	}

	internal static MethodInfo GetMethodFromSchemaProvider(XmlSchemaProviderAttribute provider, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
	{
		if (provider.IsAny)
		{
			return null;
		}
		if (provider.MethodName == null)
		{
			throw new ArgumentNullException("MethodName");
		}
		if (!CSharpHelpers.IsValidLanguageIndependentIdentifier(provider.MethodName))
		{
			throw new ArgumentException(System.SR.Format(System.SR.XmlGetSchemaMethodName, provider.MethodName), "MethodName");
		}
		MethodInfo method = (method = type.GetMethod(provider.MethodName, BindingFlags.Static | BindingFlags.Public, new Type[1] { typeof(XmlSchemaSet) }));
		if (method == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlGetSchemaMethodMissing, provider.MethodName, "XmlSchemaSet", type.FullName));
		}
		if (!typeof(XmlQualifiedName).IsAssignableFrom(method.ReturnType) && !typeof(XmlSchemaType).IsAssignableFrom(method.ReturnType))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlGetSchemaMethodReturnType, type.Name, provider.MethodName, "XmlSchemaProviderAttribute", typeof(XmlQualifiedName).FullName, typeof(XmlSchemaType).FullName));
		}
		return method;
	}

	[RequiresUnreferencedCode("calls IncludeTypes")]
	private SpecialMapping ImportSpecialMapping(Type type, TypeDesc typeDesc, string ns, ImportContext context, RecursionLimiter limiter)
	{
		if (_specials == null)
		{
			_specials = new Hashtable();
		}
		SpecialMapping specialMapping = (SpecialMapping)_specials[type];
		if (specialMapping != null)
		{
			CheckContext(specialMapping.TypeDesc, context);
			return specialMapping;
		}
		if (typeDesc.Kind == TypeKind.Serializable)
		{
			SerializableMapping serializableMapping = null;
			object[] customAttributes = type.GetCustomAttributes(typeof(XmlSchemaProviderAttribute), inherit: false);
			if (customAttributes.Length != 0)
			{
				XmlSchemaProviderAttribute xmlSchemaProviderAttribute = (XmlSchemaProviderAttribute)customAttributes[0];
				MethodInfo methodFromSchemaProvider = GetMethodFromSchemaProvider(xmlSchemaProviderAttribute, type);
				serializableMapping = new SerializableMapping(methodFromSchemaProvider, xmlSchemaProviderAttribute.IsAny, ns);
				XmlQualifiedName xsiType = serializableMapping.XsiType;
				if (xsiType != null && !xsiType.IsEmpty)
				{
					if (_serializables == null)
					{
						_serializables = new NameTable();
					}
					SerializableMapping serializableMapping2 = (SerializableMapping)_serializables[xsiType];
					if (serializableMapping2 != null)
					{
						if (serializableMapping2.Type == null)
						{
							serializableMapping = serializableMapping2;
						}
						else if (serializableMapping2.Type != type)
						{
							SerializableMapping next = serializableMapping2.Next;
							serializableMapping2.Next = serializableMapping;
							serializableMapping.Next = next;
						}
					}
					else
					{
						XmlSchemaType xsdType = serializableMapping.XsdType;
						if (xsdType != null)
						{
							SetBase(serializableMapping, xsdType.DerivedFrom);
						}
						_serializables[xsiType] = serializableMapping;
					}
					serializableMapping.TypeName = xsiType.Name;
					serializableMapping.Namespace = xsiType.Namespace;
				}
				serializableMapping.TypeDesc = typeDesc;
				serializableMapping.Type = type;
				IncludeTypes(type);
			}
			else
			{
				serializableMapping = new SerializableMapping();
				serializableMapping.TypeDesc = typeDesc;
				serializableMapping.Type = type;
			}
			specialMapping = serializableMapping;
		}
		else
		{
			specialMapping = new SpecialMapping();
			specialMapping.TypeDesc = typeDesc;
		}
		CheckContext(typeDesc, context);
		_specials.Add(type, specialMapping);
		_typeScope.AddTypeMapping(specialMapping);
		return specialMapping;
	}

	internal void SetBase(SerializableMapping mapping, XmlQualifiedName baseQname)
	{
		if (!baseQname.IsEmpty && !(baseQname.Namespace == "http://www.w3.org/2001/XMLSchema"))
		{
			XmlSchemaSet schemas = mapping.Schemas;
			ArrayList arrayList = (ArrayList)schemas.Schemas(baseQname.Namespace);
			if (arrayList.Count == 0)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlMissingSchema, baseQname.Namespace));
			}
			if (arrayList.Count > 1)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlGetSchemaInclude, baseQname.Namespace, "IXmlSerializable", "GetSchema"));
			}
			XmlSchema xmlSchema = (XmlSchema)arrayList[0];
			XmlSchemaType xmlSchemaType = (XmlSchemaType)xmlSchema.SchemaTypes[baseQname];
			xmlSchemaType = ((xmlSchemaType.Redefined != null) ? xmlSchemaType.Redefined : xmlSchemaType);
			if (_serializables[baseQname] == null)
			{
				SerializableMapping serializableMapping = new SerializableMapping(baseQname, schemas);
				SetBase(serializableMapping, xmlSchemaType.DerivedFrom);
				_serializables.Add(baseQname, serializableMapping);
			}
			mapping.SetBaseMapping((SerializableMapping)_serializables[baseQname]);
		}
	}

	private static string GetContextName(ImportContext context)
	{
		return context switch
		{
			ImportContext.Element => "element", 
			ImportContext.Attribute => "attribute", 
			ImportContext.Text => "text", 
			_ => throw new ArgumentException(System.SR.XmlInternalError, "context"), 
		};
	}

	private static Exception InvalidAttributeUseException(Type type)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidAttributeUse, type.FullName));
	}

	private static Exception UnsupportedException(TypeDesc typeDesc, ImportContext context)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalTypeContext, typeDesc.FullName, GetContextName(context)));
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private StructMapping CreateRootMapping()
	{
		TypeDesc typeDesc = _typeScope.GetTypeDesc(typeof(object));
		StructMapping structMapping = new StructMapping();
		structMapping.TypeDesc = typeDesc;
		structMapping.TypeName = "anyType";
		structMapping.Namespace = "http://www.w3.org/2001/XMLSchema";
		structMapping.Members = Array.Empty<MemberMapping>();
		structMapping.IncludeInSchema = false;
		return structMapping;
	}

	private NullableMapping CreateNullableMapping(TypeMapping baseMapping, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		TypeDesc nullableTypeDesc = baseMapping.TypeDesc.GetNullableTypeDesc(type);
		TypeMapping typeMapping = (baseMapping.IsAnonymousType ? ((TypeMapping)_anonymous[type]) : ((TypeMapping)_nullables[baseMapping.TypeName, baseMapping.Namespace]));
		NullableMapping nullableMapping;
		if (typeMapping != null)
		{
			if (typeMapping is NullableMapping)
			{
				nullableMapping = (NullableMapping)typeMapping;
				if (nullableMapping.BaseMapping is PrimitiveMapping && baseMapping is PrimitiveMapping)
				{
					return nullableMapping;
				}
				if (nullableMapping.BaseMapping == baseMapping)
				{
					return nullableMapping;
				}
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlTypesDuplicate, nullableTypeDesc.FullName, typeMapping.TypeDesc.FullName, nullableTypeDesc.Name, typeMapping.Namespace));
			}
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlTypesDuplicate, nullableTypeDesc.FullName, typeMapping.TypeDesc.FullName, nullableTypeDesc.Name, typeMapping.Namespace));
		}
		nullableMapping = new NullableMapping();
		nullableMapping.BaseMapping = baseMapping;
		nullableMapping.TypeDesc = nullableTypeDesc;
		nullableMapping.TypeName = baseMapping.TypeName;
		nullableMapping.Namespace = baseMapping.Namespace;
		nullableMapping.IncludeInSchema = baseMapping.IncludeInSchema;
		if (!baseMapping.IsAnonymousType)
		{
			_nullables.Add(baseMapping.TypeName, baseMapping.Namespace, nullableMapping);
		}
		else
		{
			_anonymous[type] = nullableMapping;
		}
		_typeScope.AddTypeMapping(nullableMapping);
		return nullableMapping;
	}

	[RequiresUnreferencedCode("calls CreateRootMapping")]
	private StructMapping GetRootMapping()
	{
		if (_root == null)
		{
			_root = CreateRootMapping();
			_typeScope.AddTypeMapping(_root);
		}
		return _root;
	}

	private TypeMapping GetTypeMapping(string typeName, string ns, TypeDesc typeDesc, NameTable typeLib, Type type)
	{
		TypeMapping typeMapping = ((typeName != null && typeName.Length != 0) ? ((TypeMapping)typeLib[typeName, ns]) : ((type == null) ? null : ((TypeMapping)_anonymous[type])));
		if (typeMapping == null)
		{
			return null;
		}
		if (!typeMapping.IsAnonymousType && typeMapping.TypeDesc != typeDesc)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlTypesDuplicate, typeDesc.FullName, typeMapping.TypeDesc.FullName, typeName, ns));
		}
		return typeMapping;
	}

	[RequiresUnreferencedCode("calls GetRootMapping")]
	private StructMapping ImportStructLikeMapping(StructModel model, string ns, bool openModel, XmlAttributes a, RecursionLimiter limiter)
	{
		if (model.TypeDesc.Kind == TypeKind.Root)
		{
			return GetRootMapping();
		}
		if (a == null)
		{
			a = GetAttributes(model.Type, canBeSimpleType: false);
		}
		string text = ns;
		if (a.XmlType != null && a.XmlType.Namespace != null)
		{
			text = a.XmlType.Namespace;
		}
		else if (a.XmlRoot != null && a.XmlRoot.Namespace != null)
		{
			text = a.XmlRoot.Namespace;
		}
		string name = (IsAnonymousType(a, ns) ? null : XsdTypeName(model.Type, a, model.TypeDesc.Name));
		name = XmlConvert.EncodeLocalName(name);
		StructMapping structMapping = (StructMapping)GetTypeMapping(name, text, model.TypeDesc, _types, model.Type);
		if (structMapping == null)
		{
			structMapping = new StructMapping();
			structMapping.TypeDesc = model.TypeDesc;
			structMapping.Namespace = text;
			structMapping.TypeName = name;
			if (!structMapping.IsAnonymousType)
			{
				_types.Add(name, text, structMapping);
			}
			else
			{
				_anonymous[model.Type] = structMapping;
			}
			if (a.XmlType != null)
			{
				structMapping.IncludeInSchema = a.XmlType.IncludeInSchema;
			}
			if (limiter.IsExceededLimit)
			{
				limiter.DeferredWorkItems.Add(new ImportStructWorkItem(model, structMapping));
				return structMapping;
			}
			limiter.Depth++;
			InitializeStructMembers(structMapping, model, openModel, name, limiter);
			while (limiter.DeferredWorkItems.Count > 0)
			{
				int index = limiter.DeferredWorkItems.Count - 1;
				ImportStructWorkItem importStructWorkItem = limiter.DeferredWorkItems[index];
				if (InitializeStructMembers(importStructWorkItem.Mapping, importStructWorkItem.Model, openModel, name, limiter))
				{
					limiter.DeferredWorkItems.RemoveAt(index);
				}
			}
			limiter.Depth--;
		}
		return structMapping;
	}

	[RequiresUnreferencedCode("calls GetTypeModel")]
	private bool InitializeStructMembers(StructMapping mapping, StructModel model, bool openModel, string typeName, RecursionLimiter limiter)
	{
		if (mapping.IsFullyInitialized)
		{
			return true;
		}
		if (model.TypeDesc.BaseTypeDesc != null)
		{
			TypeModel typeModel = _modelScope.GetTypeModel(model.Type.BaseType, directReference: false);
			if (!(typeModel is StructModel))
			{
				throw new NotSupportedException(System.SR.Format(System.SR.XmlUnsupportedInheritance, model.Type.BaseType.FullName));
			}
			StructMapping structMapping = ImportStructLikeMapping((StructModel)typeModel, mapping.Namespace, openModel, null, limiter);
			int num = limiter.DeferredWorkItems.IndexOf(structMapping);
			if (num >= 0)
			{
				if (!limiter.DeferredWorkItems.Contains(mapping))
				{
					limiter.DeferredWorkItems.Add(new ImportStructWorkItem(model, mapping));
				}
				int num2 = limiter.DeferredWorkItems.Count - 1;
				if (num < num2)
				{
					ImportStructWorkItem value = limiter.DeferredWorkItems[num];
					limiter.DeferredWorkItems[num] = limiter.DeferredWorkItems[num2];
					limiter.DeferredWorkItems[num2] = value;
				}
				return false;
			}
			mapping.BaseMapping = structMapping;
			ICollection values = mapping.BaseMapping.LocalAttributes.Values;
			foreach (AttributeAccessor item in values)
			{
				AddUniqueAccessor(mapping.LocalAttributes, item);
			}
			if (!mapping.BaseMapping.HasExplicitSequence())
			{
				values = mapping.BaseMapping.LocalElements.Values;
				foreach (ElementAccessor item2 in values)
				{
					AddUniqueAccessor(mapping.LocalElements, item2);
				}
			}
		}
		List<MemberMapping> list = new List<MemberMapping>();
		TextAccessor textAccessor = null;
		bool hasElements = false;
		bool flag = false;
		MemberInfo[] memberInfos = model.GetMemberInfos();
		foreach (MemberInfo memberInfo in memberInfos)
		{
			if (!(memberInfo is FieldInfo) && !(memberInfo is PropertyInfo))
			{
				continue;
			}
			XmlAttributes attributes = GetAttributes(memberInfo);
			if (attributes.XmlIgnore)
			{
				continue;
			}
			FieldModel fieldModel = model.GetFieldModel(memberInfo);
			if (fieldModel == null)
			{
				continue;
			}
			try
			{
				MemberMapping memberMapping = ImportFieldMapping(model, fieldModel, attributes, mapping.Namespace, limiter);
				if (memberMapping == null || (mapping.BaseMapping != null && mapping.BaseMapping.Declares(memberMapping, mapping.TypeName)))
				{
					continue;
				}
				flag |= memberMapping.IsSequence;
				AddUniqueAccessor(memberMapping, mapping.LocalElements, mapping.LocalAttributes, flag);
				if (memberMapping.Text != null)
				{
					if (!memberMapping.Text.Mapping.TypeDesc.CanBeTextValue && memberMapping.Text.Mapping.IsList)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalTypedTextAttribute, typeName, memberMapping.Text.Name, memberMapping.Text.Mapping.TypeDesc.FullName));
					}
					if (textAccessor != null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalMultipleText, model.Type.FullName));
					}
					textAccessor = memberMapping.Text;
				}
				if (memberMapping.Xmlns != null)
				{
					if (mapping.XmlnsMember != null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlMultipleXmlns, model.Type.FullName));
					}
					mapping.XmlnsMember = memberMapping;
				}
				if (memberMapping.Elements != null && memberMapping.Elements.Length != 0)
				{
					hasElements = true;
				}
				list.Add(memberMapping);
			}
			catch (Exception ex)
			{
				if (ex is OutOfMemoryException)
				{
					throw;
				}
				throw CreateMemberReflectionException(fieldModel, ex);
			}
		}
		mapping.SetContentModel(textAccessor, hasElements);
		if (flag)
		{
			Hashtable hashtable = new Hashtable();
			for (int j = 0; j < list.Count; j++)
			{
				MemberMapping memberMapping2 = list[j];
				if (memberMapping2.IsParticle)
				{
					if (!memberMapping2.IsSequence)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlSequenceInconsistent, "Order", memberMapping2.Name));
					}
					if (hashtable[memberMapping2.SequenceId] != null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlSequenceUnique, memberMapping2.SequenceId.ToString(CultureInfo.InvariantCulture), "Order", memberMapping2.Name));
					}
					hashtable[memberMapping2.SequenceId] = memberMapping2;
				}
			}
			list.Sort(new MemberMappingComparer());
		}
		mapping.Members = list.ToArray();
		if (mapping.BaseMapping == null)
		{
			mapping.BaseMapping = GetRootMapping();
		}
		if (mapping.XmlnsMember != null && mapping.BaseMapping.HasXmlnsMember)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlMultipleXmlns, model.Type.FullName));
		}
		IncludeTypes(model.Type, limiter);
		_typeScope.AddTypeMapping(mapping);
		if (openModel)
		{
			mapping.IsOpenModel = true;
		}
		return true;
	}

	private static bool IsAnonymousType(XmlAttributes a, string contextNs)
	{
		if (a.XmlType != null && a.XmlType.AnonymousType)
		{
			string @namespace = a.XmlType.Namespace;
			if (!string.IsNullOrEmpty(@namespace))
			{
				return @namespace == contextNs;
			}
			return true;
		}
		return false;
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	internal string XsdTypeName(Type type)
	{
		if (type == typeof(object))
		{
			return "anyType";
		}
		TypeDesc typeDesc = _typeScope.GetTypeDesc(type);
		if (typeDesc.IsPrimitive && typeDesc.DataType != null && typeDesc.DataType.Name != null && typeDesc.DataType.Name.Length > 0)
		{
			return typeDesc.DataType.Name;
		}
		return XsdTypeName(type, GetAttributes(type, canBeSimpleType: false), typeDesc.Name);
	}

	[RequiresUnreferencedCode("Calls XsdTypeName")]
	internal string XsdTypeName(Type type, XmlAttributes a, string name)
	{
		string text = name;
		if (a.XmlType != null && a.XmlType.TypeName.Length > 0)
		{
			text = a.XmlType.TypeName;
		}
		if (type.IsGenericType && text.Contains('{'))
		{
			Type genericTypeDefinition = type.GetGenericTypeDefinition();
			Type[] genericArguments = genericTypeDefinition.GetGenericArguments();
			Type[] genericArguments2 = type.GetGenericArguments();
			for (int i = 0; i < genericArguments.Length; i++)
			{
				string text2 = "{" + genericArguments[i]?.ToString() + "}";
				if (text.Contains(text2))
				{
					text = text.Replace(text2, XsdTypeName(genericArguments2[i]));
					if (!text.Contains('{'))
					{
						break;
					}
				}
			}
		}
		return text;
	}

	private static int CountAtLevel(XmlArrayItemAttributes attributes, int level)
	{
		int num = 0;
		for (int i = 0; i < attributes.Count; i++)
		{
			if (attributes[i].NestingLevel == level)
			{
				num++;
			}
		}
		return num;
	}

	[RequiresUnreferencedCode("calls XsdTypeName")]
	private void SetArrayMappingType(ArrayMapping mapping, string defaultNs, Type type)
	{
		XmlAttributes attributes = GetAttributes(type, canBeSimpleType: false);
		if (IsAnonymousType(attributes, defaultNs))
		{
			mapping.TypeName = null;
			mapping.Namespace = defaultNs;
			return;
		}
		ElementAccessor elementAccessor = null;
		TypeMapping typeMapping;
		if (mapping.Elements.Length == 1)
		{
			elementAccessor = mapping.Elements[0];
			typeMapping = elementAccessor.Mapping;
		}
		else
		{
			typeMapping = null;
		}
		bool flag = true;
		string text;
		string name;
		if (attributes.XmlType != null)
		{
			text = attributes.XmlType.Namespace;
			name = XsdTypeName(type, attributes, attributes.XmlType.TypeName);
			name = XmlConvert.EncodeLocalName(name);
			flag = name == null;
		}
		else if (typeMapping is EnumMapping)
		{
			text = typeMapping.Namespace;
			name = typeMapping.DefaultElementName;
		}
		else if (typeMapping is PrimitiveMapping)
		{
			text = defaultNs;
			name = typeMapping.TypeDesc.DataType.Name;
		}
		else if (typeMapping is StructMapping && typeMapping.TypeDesc.IsRoot)
		{
			text = defaultNs;
			name = "anyType";
		}
		else if (typeMapping != null)
		{
			text = ((typeMapping.Namespace == "http://www.w3.org/2001/XMLSchema") ? defaultNs : typeMapping.Namespace);
			name = typeMapping.DefaultElementName;
		}
		else
		{
			text = defaultNs;
			name = "Choice" + _choiceNum++;
		}
		if (name == null)
		{
			name = "Any";
		}
		if (elementAccessor != null)
		{
			text = elementAccessor.Namespace;
		}
		if (text == null)
		{
			text = defaultNs;
		}
		string text2 = (name = (flag ? ("ArrayOf" + CodeIdentifier.MakePascal(name)) : name));
		int num = 1;
		TypeMapping typeMapping2 = (TypeMapping)_types[text2, text];
		while (typeMapping2 != null)
		{
			if (typeMapping2 is ArrayMapping)
			{
				ArrayMapping arrayMapping = (ArrayMapping)typeMapping2;
				if (AccessorMapping.ElementsMatch(arrayMapping.Elements, mapping.Elements))
				{
					break;
				}
			}
			text2 = name + num.ToString(CultureInfo.InvariantCulture);
			typeMapping2 = (TypeMapping)_types[text2, text];
			num++;
		}
		mapping.TypeName = text2;
		mapping.Namespace = text;
	}

	[RequiresUnreferencedCode("calls SetArrayMappingType")]
	private ArrayMapping ImportArrayLikeMapping(ArrayModel model, string ns, RecursionLimiter limiter)
	{
		ArrayMapping arrayMapping = new ArrayMapping();
		arrayMapping.TypeDesc = model.TypeDesc;
		if (_savedArrayItemAttributes == null)
		{
			_savedArrayItemAttributes = new XmlArrayItemAttributes();
		}
		if (CountAtLevel(_savedArrayItemAttributes, _arrayNestingLevel) == 0)
		{
			_savedArrayItemAttributes.Add(CreateArrayItemAttribute(_typeScope.GetTypeDesc(model.Element.Type), _arrayNestingLevel));
		}
		CreateArrayElementsFromAttributes(arrayMapping, _savedArrayItemAttributes, model.Element.Type, (_savedArrayNamespace == null) ? ns : _savedArrayNamespace, limiter);
		SetArrayMappingType(arrayMapping, ns, model.Type);
		for (int i = 0; i < arrayMapping.Elements.Length; i++)
		{
			arrayMapping.Elements[i] = ReconcileLocalAccessor(arrayMapping.Elements[i], arrayMapping.Namespace);
		}
		IncludeTypes(model.Type);
		ArrayMapping arrayMapping2 = (ArrayMapping)_types[arrayMapping.TypeName, arrayMapping.Namespace];
		if (arrayMapping2 != null)
		{
			ArrayMapping next = arrayMapping2;
			while (arrayMapping2 != null)
			{
				if (arrayMapping2.TypeDesc == model.TypeDesc)
				{
					return arrayMapping2;
				}
				arrayMapping2 = arrayMapping2.Next;
			}
			arrayMapping.Next = next;
			if (!arrayMapping.IsAnonymousType)
			{
				_types[arrayMapping.TypeName, arrayMapping.Namespace] = arrayMapping;
			}
			else
			{
				_anonymous[model.Type] = arrayMapping;
			}
			return arrayMapping;
		}
		_typeScope.AddTypeMapping(arrayMapping);
		if (!arrayMapping.IsAnonymousType)
		{
			_types.Add(arrayMapping.TypeName, arrayMapping.Namespace, arrayMapping);
		}
		else
		{
			_anonymous[model.Type] = arrayMapping;
		}
		return arrayMapping;
	}

	private void CheckContext(TypeDesc typeDesc, ImportContext context)
	{
		switch (context)
		{
		case ImportContext.Element:
			if (typeDesc.CanBeElementValue)
			{
				return;
			}
			break;
		case ImportContext.Attribute:
			if (typeDesc.CanBeAttributeValue)
			{
				return;
			}
			break;
		case ImportContext.Text:
			if (typeDesc.CanBeTextValue || typeDesc.IsEnum || typeDesc.IsPrimitive)
			{
				return;
			}
			break;
		default:
			throw new ArgumentException(System.SR.XmlInternalError, "context");
		}
		throw UnsupportedException(typeDesc, context);
	}

	private PrimitiveMapping ImportPrimitiveMapping(PrimitiveModel model, ImportContext context, string dataType, bool repeats)
	{
		PrimitiveMapping primitiveMapping = new PrimitiveMapping();
		if (dataType.Length > 0)
		{
			primitiveMapping.TypeDesc = _typeScope.GetTypeDesc(dataType, "http://www.w3.org/2001/XMLSchema");
			if (primitiveMapping.TypeDesc == null)
			{
				primitiveMapping.TypeDesc = _typeScope.GetTypeDesc(dataType, "http://microsoft.com/wsdl/types/");
				if (primitiveMapping.TypeDesc == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlUdeclaredXsdType, dataType));
				}
			}
		}
		else
		{
			primitiveMapping.TypeDesc = model.TypeDesc;
		}
		primitiveMapping.TypeName = primitiveMapping.TypeDesc.DataType.Name;
		primitiveMapping.Namespace = (primitiveMapping.TypeDesc.IsXsdType ? "http://www.w3.org/2001/XMLSchema" : "http://microsoft.com/wsdl/types/");
		primitiveMapping.IsList = repeats;
		CheckContext(primitiveMapping.TypeDesc, context);
		return primitiveMapping;
	}

	[RequiresUnreferencedCode("calls XsdTypeName")]
	private EnumMapping ImportEnumMapping(EnumModel model, string ns, bool repeats)
	{
		XmlAttributes attributes = GetAttributes(model.Type, canBeSimpleType: false);
		string text = ns;
		if (attributes.XmlType != null && attributes.XmlType.Namespace != null)
		{
			text = attributes.XmlType.Namespace;
		}
		string name = (IsAnonymousType(attributes, ns) ? null : XsdTypeName(model.Type, attributes, model.TypeDesc.Name));
		name = XmlConvert.EncodeLocalName(name);
		EnumMapping enumMapping = (EnumMapping)GetTypeMapping(name, text, model.TypeDesc, _types, model.Type);
		if (enumMapping == null)
		{
			enumMapping = new EnumMapping();
			enumMapping.TypeDesc = model.TypeDesc;
			enumMapping.TypeName = name;
			enumMapping.Namespace = text;
			enumMapping.IsFlags = model.Type.IsDefined(typeof(FlagsAttribute), inherit: false);
			if (enumMapping.IsFlags && repeats)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalAttributeFlagsArray, model.TypeDesc.FullName));
			}
			enumMapping.IsList = repeats;
			enumMapping.IncludeInSchema = attributes.XmlType == null || attributes.XmlType.IncludeInSchema;
			if (!enumMapping.IsAnonymousType)
			{
				_types.Add(name, text, enumMapping);
			}
			else
			{
				_anonymous[model.Type] = enumMapping;
			}
			List<ConstantMapping> list = new List<ConstantMapping>();
			for (int i = 0; i < model.Constants.Length; i++)
			{
				ConstantMapping constantMapping = ImportConstantMapping(model.Constants[i]);
				if (constantMapping != null)
				{
					list.Add(constantMapping);
				}
			}
			if (list.Count == 0)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlNoSerializableMembers, model.TypeDesc.FullName));
			}
			enumMapping.Constants = list.ToArray();
			_typeScope.AddTypeMapping(enumMapping);
		}
		return enumMapping;
	}

	private ConstantMapping ImportConstantMapping(ConstantModel model)
	{
		XmlAttributes attributes = GetAttributes(model.FieldInfo);
		if (attributes.XmlIgnore)
		{
			return null;
		}
		if (((uint)attributes.XmlFlags & 0xFFFFFFFEu) != 0)
		{
			throw new InvalidOperationException(System.SR.XmlInvalidConstantAttribute);
		}
		if (attributes.XmlEnum == null)
		{
			attributes.XmlEnum = new XmlEnumAttribute();
		}
		ConstantMapping constantMapping = new ConstantMapping();
		constantMapping.XmlName = ((attributes.XmlEnum.Name == null) ? model.Name : attributes.XmlEnum.Name);
		constantMapping.Name = model.Name;
		constantMapping.Value = model.Value;
		return constantMapping;
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private MembersMapping ImportMembersMapping(XmlReflectionMember[] xmlReflectionMembers, string ns, bool hasWrapperElement, bool rpc, bool openModel, RecursionLimiter limiter)
	{
		MembersMapping membersMapping = new MembersMapping();
		membersMapping.TypeDesc = _typeScope.GetTypeDesc(typeof(object[]));
		MemberMapping[] array = new MemberMapping[xmlReflectionMembers.Length];
		NameTable elements = new NameTable();
		NameTable attributes = new NameTable();
		TextAccessor textAccessor = null;
		bool flag = false;
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				MemberMapping memberMapping = ImportMemberMapping(xmlReflectionMembers[i], ns, xmlReflectionMembers, rpc, openModel, limiter);
				if (!hasWrapperElement && memberMapping.Attribute != null)
				{
					if (rpc)
					{
						throw new InvalidOperationException(System.SR.XmlRpcLitAttributeAttributes);
					}
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidAttributeType, "XmlAttribute"));
				}
				if (rpc && xmlReflectionMembers[i].IsReturnValue)
				{
					if (i > 0)
					{
						throw new InvalidOperationException(System.SR.XmlInvalidReturnPosition);
					}
					memberMapping.IsReturnValue = true;
				}
				array[i] = memberMapping;
				flag |= memberMapping.IsSequence;
				if (!xmlReflectionMembers[i].XmlAttributes.XmlIgnore)
				{
					AddUniqueAccessor(memberMapping, elements, attributes, flag);
				}
				array[i] = memberMapping;
				if (memberMapping.Text != null)
				{
					if (textAccessor != null)
					{
						throw new InvalidOperationException(System.SR.XmlIllegalMultipleTextMembers);
					}
					textAccessor = memberMapping.Text;
				}
				if (memberMapping.Xmlns != null)
				{
					if (membersMapping.XmlnsMember != null)
					{
						throw new InvalidOperationException(System.SR.XmlMultipleXmlnsMembers);
					}
					membersMapping.XmlnsMember = memberMapping;
				}
			}
			catch (Exception ex)
			{
				if (ex is OutOfMemoryException)
				{
					throw;
				}
				throw CreateReflectionException(xmlReflectionMembers[i].MemberName, ex);
			}
		}
		if (flag)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlSequenceMembers, "Order"));
		}
		membersMapping.Members = array;
		membersMapping.HasWrapperElement = hasWrapperElement;
		return membersMapping;
	}

	[RequiresUnreferencedCode("Calls TypeScope.GetTypeDesc(Type) and XmlReflectionImporter.ImportAccessorMapping both of which RequireUnreferencedCode")]
	private MemberMapping ImportMemberMapping(XmlReflectionMember xmlReflectionMember, string ns, XmlReflectionMember[] xmlReflectionMembers, bool rpc, bool openModel, RecursionLimiter limiter)
	{
		XmlSchemaForm form = ((!rpc) ? XmlSchemaForm.Qualified : XmlSchemaForm.Unqualified);
		XmlAttributes xmlAttributes = xmlReflectionMember.XmlAttributes;
		TypeDesc typeDesc = _typeScope.GetTypeDesc(xmlReflectionMember.MemberType);
		if (xmlAttributes.XmlFlags == (XmlAttributeFlags)0)
		{
			if (typeDesc.IsArrayLike)
			{
				XmlArrayAttribute xmlArrayAttribute = CreateArrayAttribute(typeDesc);
				xmlArrayAttribute.ElementName = xmlReflectionMember.MemberName;
				xmlArrayAttribute.Namespace = (rpc ? null : ns);
				xmlArrayAttribute.Form = form;
				xmlAttributes.XmlArray = xmlArrayAttribute;
			}
			else
			{
				XmlElementAttribute xmlElementAttribute = CreateElementAttribute(typeDesc);
				if (typeDesc.IsStructLike)
				{
					XmlAttributes xmlAttributes2 = new XmlAttributes(xmlReflectionMember.MemberType);
					if (xmlAttributes2.XmlRoot != null)
					{
						if (xmlAttributes2.XmlRoot.ElementName.Length > 0)
						{
							xmlElementAttribute.ElementName = xmlAttributes2.XmlRoot.ElementName;
						}
						if (rpc)
						{
							xmlElementAttribute.Namespace = null;
							if (xmlAttributes2.XmlRoot.GetIsNullableSpecified())
							{
								xmlElementAttribute.IsNullable = xmlAttributes2.XmlRoot.IsNullable;
							}
						}
						else
						{
							xmlElementAttribute.Namespace = xmlAttributes2.XmlRoot.Namespace;
							xmlElementAttribute.IsNullable = xmlAttributes2.XmlRoot.IsNullable;
						}
					}
				}
				if (xmlElementAttribute.ElementName.Length == 0)
				{
					xmlElementAttribute.ElementName = xmlReflectionMember.MemberName;
				}
				if (xmlElementAttribute.Namespace == null && !rpc)
				{
					xmlElementAttribute.Namespace = ns;
				}
				xmlElementAttribute.Form = form;
				xmlAttributes.XmlElements.Add(xmlElementAttribute);
			}
		}
		else if (xmlAttributes.XmlRoot != null)
		{
			CheckNullable(xmlAttributes.XmlRoot.IsNullable, typeDesc, null);
		}
		MemberMapping memberMapping = new MemberMapping();
		memberMapping.Name = xmlReflectionMember.MemberName;
		bool checkSpecified = FindSpecifiedMember(xmlReflectionMember.MemberName, xmlReflectionMembers) != null;
		FieldModel fieldModel = new FieldModel(xmlReflectionMember.MemberName, xmlReflectionMember.MemberType, _typeScope.GetTypeDesc(xmlReflectionMember.MemberType), checkSpecified, checkShouldPersist: false);
		memberMapping.CheckShouldPersist = fieldModel.CheckShouldPersist;
		memberMapping.CheckSpecified = fieldModel.CheckSpecified;
		memberMapping.ReadOnly = fieldModel.ReadOnly;
		Type choiceIdentifierType = null;
		if (xmlAttributes.XmlChoiceIdentifier != null)
		{
			choiceIdentifierType = GetChoiceIdentifierType(xmlAttributes.XmlChoiceIdentifier, xmlReflectionMembers, typeDesc.IsArrayLike, fieldModel.Name);
		}
		ImportAccessorMapping(memberMapping, fieldModel, xmlAttributes, ns, choiceIdentifierType, rpc, openModel, limiter);
		if (xmlReflectionMember.OverrideIsNullable && memberMapping.Elements.Length != 0)
		{
			memberMapping.Elements[0].IsNullable = false;
		}
		return memberMapping;
	}

	internal static XmlReflectionMember FindSpecifiedMember(string memberName, XmlReflectionMember[] reflectionMembers)
	{
		for (int i = 0; i < reflectionMembers.Length; i++)
		{
			if (string.Equals(reflectionMembers[i].MemberName, memberName + "Specified", StringComparison.Ordinal))
			{
				return reflectionMembers[i];
			}
		}
		return null;
	}

	[RequiresUnreferencedCode("calls ImportAccessorMapping")]
	private MemberMapping ImportFieldMapping(StructModel parent, FieldModel model, XmlAttributes a, string ns, RecursionLimiter limiter)
	{
		MemberMapping memberMapping = new MemberMapping();
		memberMapping.Name = model.Name;
		memberMapping.CheckShouldPersist = model.CheckShouldPersist;
		memberMapping.CheckSpecified = model.CheckSpecified;
		memberMapping.MemberInfo = model.MemberInfo;
		memberMapping.CheckSpecifiedMemberInfo = model.CheckSpecifiedMemberInfo;
		memberMapping.CheckShouldPersistMethodInfo = model.CheckShouldPersistMethodInfo;
		memberMapping.ReadOnly = model.ReadOnly;
		Type choiceIdentifierType = null;
		if (a.XmlChoiceIdentifier != null)
		{
			choiceIdentifierType = GetChoiceIdentifierType(a.XmlChoiceIdentifier, parent, model.FieldTypeDesc.IsArrayLike, model.Name);
		}
		ImportAccessorMapping(memberMapping, model, a, ns, choiceIdentifierType, rpc: false, openModel: false, limiter);
		return memberMapping;
	}

	private Type CheckChoiceIdentifierType(Type type, bool isArrayLike, string identifierName, string memberName)
	{
		if (type.IsArray)
		{
			if (!isArrayLike)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceIdentifierType, identifierName, memberName, type.GetElementType().FullName));
			}
			type = type.GetElementType();
		}
		else if (isArrayLike)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceIdentifierArrayType, identifierName, memberName, type.FullName));
		}
		if (!type.IsEnum)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceIdentifierTypeEnum, identifierName));
		}
		return type;
	}

	private Type GetChoiceIdentifierType(XmlChoiceIdentifierAttribute choice, XmlReflectionMember[] xmlReflectionMembers, bool isArrayLike, string accessorName)
	{
		for (int i = 0; i < xmlReflectionMembers.Length; i++)
		{
			if (choice.MemberName == xmlReflectionMembers[i].MemberName)
			{
				return CheckChoiceIdentifierType(xmlReflectionMembers[i].MemberType, isArrayLike, choice.MemberName, accessorName);
			}
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceIdentiferMemberMissing, choice.MemberName, accessorName));
	}

	[RequiresUnreferencedCode("calls GetFieldModel")]
	private Type GetChoiceIdentifierType(XmlChoiceIdentifierAttribute choice, StructModel structModel, bool isArrayLike, string accessorName)
	{
		MemberInfo[] array = structModel.Type.GetMember(choice.MemberName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		if (array == null || array.Length == 0)
		{
			PropertyInfo property = structModel.Type.GetProperty(choice.MemberName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			if (property == null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceIdentiferMemberMissing, choice.MemberName, accessorName));
			}
			array = new MemberInfo[1] { property };
		}
		else if (array.Length > 1)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceIdentiferAmbiguous, choice.MemberName));
		}
		FieldModel fieldModel = structModel.GetFieldModel(array[0]);
		if (fieldModel == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceIdentiferMemberMissing, choice.MemberName, accessorName));
		}
		choice.SetMemberInfo(fieldModel.MemberInfo);
		Type fieldType = fieldModel.FieldType;
		return CheckChoiceIdentifierType(fieldType, isArrayLike, choice.MemberName, accessorName);
	}

	[RequiresUnreferencedCode("calls ImportTypeMapping")]
	private void CreateArrayElementsFromAttributes(ArrayMapping arrayMapping, XmlArrayItemAttributes attributes, Type arrayElementType, string arrayElementNs, RecursionLimiter limiter)
	{
		NameTable nameTable = new NameTable();
		int num = 0;
		while (attributes != null && num < attributes.Count)
		{
			XmlArrayItemAttribute xmlArrayItemAttribute = attributes[num];
			if (xmlArrayItemAttribute.NestingLevel == _arrayNestingLevel)
			{
				Type type = ((xmlArrayItemAttribute.Type != null) ? xmlArrayItemAttribute.Type : arrayElementType);
				TypeDesc typeDesc = _typeScope.GetTypeDesc(type);
				ElementAccessor elementAccessor = new ElementAccessor();
				elementAccessor.Namespace = ((xmlArrayItemAttribute.Namespace == null) ? arrayElementNs : xmlArrayItemAttribute.Namespace);
				elementAccessor.Mapping = ImportTypeMapping(_modelScope.GetTypeModel(type), elementAccessor.Namespace, ImportContext.Element, xmlArrayItemAttribute.DataType, null, limiter);
				elementAccessor.Name = ((xmlArrayItemAttribute.ElementName.Length == 0) ? elementAccessor.Mapping.DefaultElementName : XmlConvert.EncodeLocalName(xmlArrayItemAttribute.ElementName));
				elementAccessor.IsNullable = (xmlArrayItemAttribute.GetIsNullableSpecified() ? xmlArrayItemAttribute.IsNullable : (typeDesc.IsNullable || typeDesc.IsOptionalValue));
				elementAccessor.Form = ((xmlArrayItemAttribute.Form == XmlSchemaForm.None) ? XmlSchemaForm.Qualified : xmlArrayItemAttribute.Form);
				CheckForm(elementAccessor.Form, arrayElementNs != elementAccessor.Namespace);
				CheckNullable(elementAccessor.IsNullable, typeDesc, elementAccessor.Mapping);
				AddUniqueAccessor(nameTable, elementAccessor);
			}
			num++;
		}
		arrayMapping.Elements = (ElementAccessor[])nameTable.ToArray(typeof(ElementAccessor));
	}

	[RequiresUnreferencedCode("calls GetArrayElementType")]
	private void ImportAccessorMapping(MemberMapping accessor, FieldModel model, XmlAttributes a, string ns, Type choiceIdentifierType, bool rpc, bool openModel, RecursionLimiter limiter)
	{
		XmlSchemaForm xmlSchemaForm = XmlSchemaForm.Qualified;
		int arrayNestingLevel = _arrayNestingLevel;
		int num = -1;
		XmlArrayItemAttributes savedArrayItemAttributes = _savedArrayItemAttributes;
		string savedArrayNamespace = _savedArrayNamespace;
		_arrayNestingLevel = 0;
		_savedArrayItemAttributes = null;
		_savedArrayNamespace = null;
		Type fieldType = model.FieldType;
		string name = model.Name;
		ArrayList arrayList = new ArrayList();
		NameTable nameTable = new NameTable();
		accessor.TypeDesc = _typeScope.GetTypeDesc(fieldType);
		XmlAttributeFlags xmlFlags = a.XmlFlags;
		accessor.Ignore = a.XmlIgnore;
		if (rpc)
		{
			CheckTopLevelAttributes(a, name);
		}
		else
		{
			CheckAmbiguousChoice(a, fieldType, name);
		}
		XmlAttributeFlags xmlAttributeFlags = (XmlAttributeFlags)1300;
		XmlAttributeFlags xmlAttributeFlags2 = (XmlAttributeFlags)544;
		XmlAttributeFlags xmlAttributeFlags3 = (XmlAttributeFlags)10;
		if ((xmlFlags & xmlAttributeFlags3) != 0 && fieldType == typeof(byte[]))
		{
			accessor.TypeDesc = _typeScope.GetArrayTypeDesc(fieldType);
		}
		if (a.XmlChoiceIdentifier != null)
		{
			accessor.ChoiceIdentifier = new ChoiceIdentifierAccessor();
			accessor.ChoiceIdentifier.MemberName = a.XmlChoiceIdentifier.MemberName;
			accessor.ChoiceIdentifier.MemberInfo = a.XmlChoiceIdentifier.GetMemberInfo();
			accessor.ChoiceIdentifier.Mapping = ImportTypeMapping(_modelScope.GetTypeModel(choiceIdentifierType), ns, ImportContext.Element, string.Empty, null, limiter);
			CheckChoiceIdentifierMapping((EnumMapping)accessor.ChoiceIdentifier.Mapping);
		}
		if (accessor.TypeDesc.IsArrayLike)
		{
			Type arrayElementType = TypeScope.GetArrayElementType(fieldType, model.FieldTypeDesc.FullName + "." + model.Name);
			if ((xmlFlags & xmlAttributeFlags2) != 0)
			{
				if ((xmlFlags & xmlAttributeFlags2) != xmlFlags)
				{
					throw new InvalidOperationException(System.SR.XmlIllegalAttributesArrayAttribute);
				}
				if (a.XmlAttribute != null && !accessor.TypeDesc.ArrayElementTypeDesc.IsPrimitive && !accessor.TypeDesc.ArrayElementTypeDesc.IsEnum)
				{
					if (accessor.TypeDesc.ArrayElementTypeDesc.Kind == TypeKind.Serializable)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalAttrOrTextInterface, name, accessor.TypeDesc.ArrayElementTypeDesc.FullName, "IXmlSerializable"));
					}
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalAttrOrText, name, accessor.TypeDesc.ArrayElementTypeDesc.FullName));
				}
				bool flag = a.XmlAttribute != null && (accessor.TypeDesc.ArrayElementTypeDesc.IsPrimitive || accessor.TypeDesc.ArrayElementTypeDesc.IsEnum);
				if (a.XmlAnyAttribute != null)
				{
					a.XmlAttribute = new XmlAttributeAttribute();
				}
				AttributeAccessor attributeAccessor = new AttributeAccessor();
				Type type = ((a.XmlAttribute.Type == null) ? arrayElementType : a.XmlAttribute.Type);
				TypeDesc typeDesc = _typeScope.GetTypeDesc(type);
				attributeAccessor.Name = Accessor.EscapeQName((a.XmlAttribute.AttributeName.Length == 0) ? name : a.XmlAttribute.AttributeName);
				attributeAccessor.Namespace = ((a.XmlAttribute.Namespace == null) ? ns : a.XmlAttribute.Namespace);
				attributeAccessor.Form = a.XmlAttribute.Form;
				if (attributeAccessor.Form == XmlSchemaForm.None && ns != attributeAccessor.Namespace)
				{
					attributeAccessor.Form = XmlSchemaForm.Qualified;
				}
				attributeAccessor.CheckSpecial();
				CheckForm(attributeAccessor.Form, ns != attributeAccessor.Namespace);
				attributeAccessor.Mapping = ImportTypeMapping(_modelScope.GetTypeModel(type), ns, ImportContext.Attribute, a.XmlAttribute.DataType, null, flag, openModel: false, limiter);
				attributeAccessor.IsList = flag;
				attributeAccessor.Default = GetDefaultValue(model.FieldTypeDesc, model.FieldType, a);
				attributeAccessor.Any = a.XmlAnyAttribute != null;
				if (attributeAccessor.Form == XmlSchemaForm.Qualified && attributeAccessor.Namespace != ns)
				{
					if (_xsdAttributes == null)
					{
						_xsdAttributes = new NameTable();
					}
					attributeAccessor = (AttributeAccessor)ReconcileAccessor(attributeAccessor, _xsdAttributes);
				}
				accessor.Attribute = attributeAccessor;
			}
			else if ((xmlFlags & xmlAttributeFlags) != 0)
			{
				if ((xmlFlags & xmlAttributeFlags) != xmlFlags)
				{
					throw new InvalidOperationException(System.SR.XmlIllegalElementsArrayAttribute);
				}
				if (a.XmlText != null)
				{
					TextAccessor textAccessor = new TextAccessor();
					Type type2 = ((a.XmlText.Type == null) ? arrayElementType : a.XmlText.Type);
					TypeDesc typeDesc2 = _typeScope.GetTypeDesc(type2);
					textAccessor.Name = name;
					textAccessor.Mapping = ImportTypeMapping(_modelScope.GetTypeModel(type2), ns, ImportContext.Text, a.XmlText.DataType, null, repeats: true, openModel: false, limiter);
					if (!(textAccessor.Mapping is SpecialMapping) && typeDesc2 != _typeScope.GetTypeDesc(typeof(string)))
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalArrayTextAttribute, name));
					}
					accessor.Text = textAccessor;
				}
				if (a.XmlText == null && a.XmlElements.Count == 0 && a.XmlAnyElements.Count == 0)
				{
					a.XmlElements.Add(CreateElementAttribute(accessor.TypeDesc));
				}
				for (int i = 0; i < a.XmlElements.Count; i++)
				{
					XmlElementAttribute xmlElementAttribute = a.XmlElements[i];
					Type type3 = ((xmlElementAttribute.Type == null) ? arrayElementType : xmlElementAttribute.Type);
					TypeDesc typeDesc3 = _typeScope.GetTypeDesc(type3);
					TypeModel typeModel = _modelScope.GetTypeModel(type3);
					ElementAccessor elementAccessor = new ElementAccessor();
					elementAccessor.Namespace = (rpc ? null : ((xmlElementAttribute.Namespace == null) ? ns : xmlElementAttribute.Namespace));
					elementAccessor.Mapping = ImportTypeMapping(typeModel, rpc ? ns : elementAccessor.Namespace, ImportContext.Element, xmlElementAttribute.DataType, null, limiter);
					if (a.XmlElements.Count == 1)
					{
						elementAccessor.Name = XmlConvert.EncodeLocalName((xmlElementAttribute.ElementName.Length == 0) ? name : xmlElementAttribute.ElementName);
					}
					else
					{
						elementAccessor.Name = ((xmlElementAttribute.ElementName.Length == 0) ? elementAccessor.Mapping.DefaultElementName : XmlConvert.EncodeLocalName(xmlElementAttribute.ElementName));
					}
					elementAccessor.Default = GetDefaultValue(model.FieldTypeDesc, model.FieldType, a);
					if (xmlElementAttribute.GetIsNullableSpecified() && !xmlElementAttribute.IsNullable && typeModel.TypeDesc.IsOptionalValue)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidNotNullable, typeModel.TypeDesc.BaseTypeDesc.FullName, "XmlElement"));
					}
					elementAccessor.IsNullable = (xmlElementAttribute.GetIsNullableSpecified() ? xmlElementAttribute.IsNullable : typeModel.TypeDesc.IsOptionalValue);
					elementAccessor.Form = (rpc ? XmlSchemaForm.Unqualified : ((xmlElementAttribute.Form == XmlSchemaForm.None) ? xmlSchemaForm : xmlElementAttribute.Form));
					CheckNullable(elementAccessor.IsNullable, typeDesc3, elementAccessor.Mapping);
					if (!rpc)
					{
						CheckForm(elementAccessor.Form, ns != elementAccessor.Namespace);
						elementAccessor = ReconcileLocalAccessor(elementAccessor, ns);
					}
					if (xmlElementAttribute.Order != -1)
					{
						if (xmlElementAttribute.Order != num && num != -1)
						{
							throw new InvalidOperationException(System.SR.Format(System.SR.XmlSequenceMatch, "Order"));
						}
						num = xmlElementAttribute.Order;
					}
					AddUniqueAccessor(nameTable, elementAccessor);
					arrayList.Add(elementAccessor);
				}
				NameTable nameTable2 = new NameTable();
				for (int j = 0; j < a.XmlAnyElements.Count; j++)
				{
					XmlAnyElementAttribute xmlAnyElementAttribute = a.XmlAnyElements[j];
					Type type4 = (typeof(IXmlSerializable).IsAssignableFrom(arrayElementType) ? arrayElementType : (typeof(XmlNode).IsAssignableFrom(arrayElementType) ? arrayElementType : typeof(XmlElement)));
					if (!arrayElementType.IsAssignableFrom(type4))
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalAnyElement, arrayElementType.FullName));
					}
					string name2 = ((xmlAnyElementAttribute.Name.Length == 0) ? xmlAnyElementAttribute.Name : XmlConvert.EncodeLocalName(xmlAnyElementAttribute.Name));
					string text = (xmlAnyElementAttribute.GetNamespaceSpecified() ? xmlAnyElementAttribute.Namespace : null);
					if (nameTable2[name2, text] != null)
					{
						continue;
					}
					nameTable2[name2, text] = xmlAnyElementAttribute;
					if (nameTable[name2, (text == null) ? ns : text] != null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlAnyElementDuplicate, name, xmlAnyElementAttribute.Name, (xmlAnyElementAttribute.Namespace == null) ? "null" : xmlAnyElementAttribute.Namespace));
					}
					ElementAccessor elementAccessor2 = new ElementAccessor();
					elementAccessor2.Name = name2;
					elementAccessor2.Namespace = ((text == null) ? ns : text);
					elementAccessor2.Any = true;
					elementAccessor2.AnyNamespaces = text;
					TypeDesc typeDesc4 = _typeScope.GetTypeDesc(type4);
					TypeModel typeModel2 = _modelScope.GetTypeModel(type4);
					if (elementAccessor2.Name.Length > 0)
					{
						typeModel2.TypeDesc.IsMixed = true;
					}
					elementAccessor2.Mapping = ImportTypeMapping(typeModel2, elementAccessor2.Namespace, ImportContext.Element, string.Empty, null, limiter);
					elementAccessor2.Default = GetDefaultValue(model.FieldTypeDesc, model.FieldType, a);
					elementAccessor2.IsNullable = false;
					elementAccessor2.Form = xmlSchemaForm;
					CheckNullable(elementAccessor2.IsNullable, typeDesc4, elementAccessor2.Mapping);
					if (!rpc)
					{
						CheckForm(elementAccessor2.Form, ns != elementAccessor2.Namespace);
						elementAccessor2 = ReconcileLocalAccessor(elementAccessor2, ns);
					}
					nameTable.Add(elementAccessor2.Name, elementAccessor2.Namespace, elementAccessor2);
					arrayList.Add(elementAccessor2);
					if (xmlAnyElementAttribute.Order != -1)
					{
						if (xmlAnyElementAttribute.Order != num && num != -1)
						{
							throw new InvalidOperationException(System.SR.Format(System.SR.XmlSequenceMatch, "Order"));
						}
						num = xmlAnyElementAttribute.Order;
					}
				}
			}
			else
			{
				if ((xmlFlags & xmlAttributeFlags3) != 0 && (xmlFlags & xmlAttributeFlags3) != xmlFlags)
				{
					throw new InvalidOperationException(System.SR.XmlIllegalArrayArrayAttribute);
				}
				TypeDesc typeDesc5 = _typeScope.GetTypeDesc(arrayElementType);
				if (a.XmlArray == null)
				{
					a.XmlArray = CreateArrayAttribute(accessor.TypeDesc);
				}
				if (CountAtLevel(a.XmlArrayItems, _arrayNestingLevel) == 0)
				{
					a.XmlArrayItems.Add(CreateArrayItemAttribute(typeDesc5, _arrayNestingLevel));
				}
				ElementAccessor elementAccessor3 = new ElementAccessor();
				elementAccessor3.Name = XmlConvert.EncodeLocalName((a.XmlArray.ElementName.Length == 0) ? name : a.XmlArray.ElementName);
				elementAccessor3.Namespace = (rpc ? null : ((a.XmlArray.Namespace == null) ? ns : a.XmlArray.Namespace));
				_savedArrayItemAttributes = a.XmlArrayItems;
				_savedArrayNamespace = elementAccessor3.Namespace;
				ArrayMapping mapping = ImportArrayLikeMapping(_modelScope.GetArrayModel(fieldType), ns, limiter);
				elementAccessor3.Mapping = mapping;
				elementAccessor3.IsNullable = a.XmlArray.IsNullable;
				elementAccessor3.Form = (rpc ? XmlSchemaForm.Unqualified : ((a.XmlArray.Form == XmlSchemaForm.None) ? xmlSchemaForm : a.XmlArray.Form));
				num = a.XmlArray.Order;
				CheckNullable(elementAccessor3.IsNullable, accessor.TypeDesc, elementAccessor3.Mapping);
				if (!rpc)
				{
					CheckForm(elementAccessor3.Form, ns != elementAccessor3.Namespace);
					elementAccessor3 = ReconcileLocalAccessor(elementAccessor3, ns);
				}
				_savedArrayItemAttributes = null;
				_savedArrayNamespace = null;
				AddUniqueAccessor(nameTable, elementAccessor3);
				arrayList.Add(elementAccessor3);
			}
		}
		else if (!accessor.TypeDesc.IsVoid)
		{
			XmlAttributeFlags xmlAttributeFlags4 = (XmlAttributeFlags)3380;
			if ((xmlFlags & xmlAttributeFlags4) != xmlFlags)
			{
				throw new InvalidOperationException(System.SR.XmlIllegalAttribute);
			}
			if (accessor.TypeDesc.IsPrimitive || accessor.TypeDesc.IsEnum)
			{
				if (a.XmlAnyElements.Count > 0)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalAnyElement, accessor.TypeDesc.FullName));
				}
				if (a.XmlAttribute != null)
				{
					if (a.XmlElements.Count > 0)
					{
						throw new InvalidOperationException(System.SR.XmlIllegalAttribute);
					}
					if (a.XmlAttribute.Type != null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalType, "XmlAttribute"));
					}
					AttributeAccessor attributeAccessor2 = new AttributeAccessor();
					attributeAccessor2.Name = Accessor.EscapeQName((a.XmlAttribute.AttributeName.Length == 0) ? name : a.XmlAttribute.AttributeName);
					attributeAccessor2.Namespace = ((a.XmlAttribute.Namespace == null) ? ns : a.XmlAttribute.Namespace);
					attributeAccessor2.Form = a.XmlAttribute.Form;
					if (attributeAccessor2.Form == XmlSchemaForm.None && ns != attributeAccessor2.Namespace)
					{
						attributeAccessor2.Form = XmlSchemaForm.Qualified;
					}
					attributeAccessor2.CheckSpecial();
					CheckForm(attributeAccessor2.Form, ns != attributeAccessor2.Namespace);
					attributeAccessor2.Mapping = ImportTypeMapping(_modelScope.GetTypeModel(fieldType), ns, ImportContext.Attribute, a.XmlAttribute.DataType, null, limiter);
					attributeAccessor2.Default = GetDefaultValue(model.FieldTypeDesc, model.FieldType, a);
					attributeAccessor2.Any = a.XmlAnyAttribute != null;
					if (attributeAccessor2.Form == XmlSchemaForm.Qualified && attributeAccessor2.Namespace != ns)
					{
						if (_xsdAttributes == null)
						{
							_xsdAttributes = new NameTable();
						}
						attributeAccessor2 = (AttributeAccessor)ReconcileAccessor(attributeAccessor2, _xsdAttributes);
					}
					accessor.Attribute = attributeAccessor2;
				}
				else
				{
					if (a.XmlText != null)
					{
						if (a.XmlText.Type != null && a.XmlText.Type != fieldType)
						{
							throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalType, "XmlText"));
						}
						TextAccessor textAccessor2 = new TextAccessor();
						textAccessor2.Name = name;
						textAccessor2.Mapping = ImportTypeMapping(_modelScope.GetTypeModel(fieldType), ns, ImportContext.Text, a.XmlText.DataType, null, limiter);
						accessor.Text = textAccessor2;
					}
					else if (a.XmlElements.Count == 0)
					{
						a.XmlElements.Add(CreateElementAttribute(accessor.TypeDesc));
					}
					for (int k = 0; k < a.XmlElements.Count; k++)
					{
						XmlElementAttribute xmlElementAttribute2 = a.XmlElements[k];
						if (xmlElementAttribute2.Type != null && _typeScope.GetTypeDesc(xmlElementAttribute2.Type) != accessor.TypeDesc)
						{
							throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalType, "XmlElement"));
						}
						ElementAccessor elementAccessor4 = new ElementAccessor();
						elementAccessor4.Name = XmlConvert.EncodeLocalName((xmlElementAttribute2.ElementName.Length == 0) ? name : xmlElementAttribute2.ElementName);
						elementAccessor4.Namespace = (rpc ? null : ((xmlElementAttribute2.Namespace == null) ? ns : xmlElementAttribute2.Namespace));
						TypeModel typeModel3 = _modelScope.GetTypeModel(fieldType);
						elementAccessor4.Mapping = ImportTypeMapping(typeModel3, rpc ? ns : elementAccessor4.Namespace, ImportContext.Element, xmlElementAttribute2.DataType, null, limiter);
						if (elementAccessor4.Mapping.TypeDesc.Kind == TypeKind.Node)
						{
							elementAccessor4.Any = true;
						}
						elementAccessor4.Default = GetDefaultValue(model.FieldTypeDesc, model.FieldType, a);
						if (xmlElementAttribute2.GetIsNullableSpecified() && !xmlElementAttribute2.IsNullable && typeModel3.TypeDesc.IsOptionalValue)
						{
							throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidNotNullable, typeModel3.TypeDesc.BaseTypeDesc.FullName, "XmlElement"));
						}
						elementAccessor4.IsNullable = (xmlElementAttribute2.GetIsNullableSpecified() ? xmlElementAttribute2.IsNullable : typeModel3.TypeDesc.IsOptionalValue);
						elementAccessor4.Form = (rpc ? XmlSchemaForm.Unqualified : ((xmlElementAttribute2.Form == XmlSchemaForm.None) ? xmlSchemaForm : xmlElementAttribute2.Form));
						CheckNullable(elementAccessor4.IsNullable, accessor.TypeDesc, elementAccessor4.Mapping);
						if (!rpc)
						{
							CheckForm(elementAccessor4.Form, ns != elementAccessor4.Namespace);
							elementAccessor4 = ReconcileLocalAccessor(elementAccessor4, ns);
						}
						if (xmlElementAttribute2.Order != -1)
						{
							if (xmlElementAttribute2.Order != num && num != -1)
							{
								throw new InvalidOperationException(System.SR.Format(System.SR.XmlSequenceMatch, "Order"));
							}
							num = xmlElementAttribute2.Order;
						}
						AddUniqueAccessor(nameTable, elementAccessor4);
						arrayList.Add(elementAccessor4);
					}
				}
			}
			else if (a.Xmlns)
			{
				if (xmlFlags != XmlAttributeFlags.XmlnsDeclarations)
				{
					throw new InvalidOperationException(System.SR.XmlSoleXmlnsAttribute);
				}
				if (fieldType != typeof(XmlSerializerNamespaces))
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlXmlnsInvalidType, name, fieldType.FullName, typeof(XmlSerializerNamespaces).FullName));
				}
				accessor.Xmlns = new XmlnsAccessor();
				accessor.Ignore = true;
			}
			else
			{
				if (a.XmlAttribute != null || a.XmlText != null)
				{
					if (accessor.TypeDesc.Kind == TypeKind.Serializable)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalAttrOrTextInterface, name, accessor.TypeDesc.FullName, "IXmlSerializable"));
					}
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalAttrOrText, name, accessor.TypeDesc));
				}
				if (a.XmlElements.Count == 0 && a.XmlAnyElements.Count == 0)
				{
					a.XmlElements.Add(CreateElementAttribute(accessor.TypeDesc));
				}
				for (int l = 0; l < a.XmlElements.Count; l++)
				{
					XmlElementAttribute xmlElementAttribute3 = a.XmlElements[l];
					Type type5 = ((xmlElementAttribute3.Type == null) ? fieldType : xmlElementAttribute3.Type);
					TypeDesc typeDesc6 = _typeScope.GetTypeDesc(type5);
					ElementAccessor elementAccessor5 = new ElementAccessor();
					TypeModel typeModel4 = _modelScope.GetTypeModel(type5);
					elementAccessor5.Namespace = (rpc ? null : ((xmlElementAttribute3.Namespace == null) ? ns : xmlElementAttribute3.Namespace));
					elementAccessor5.Mapping = ImportTypeMapping(typeModel4, rpc ? ns : elementAccessor5.Namespace, ImportContext.Element, xmlElementAttribute3.DataType, null, repeats: false, openModel, limiter);
					if (a.XmlElements.Count == 1)
					{
						elementAccessor5.Name = XmlConvert.EncodeLocalName((xmlElementAttribute3.ElementName.Length == 0) ? name : xmlElementAttribute3.ElementName);
					}
					else
					{
						elementAccessor5.Name = ((xmlElementAttribute3.ElementName.Length == 0) ? elementAccessor5.Mapping.DefaultElementName : XmlConvert.EncodeLocalName(xmlElementAttribute3.ElementName));
					}
					elementAccessor5.Default = GetDefaultValue(model.FieldTypeDesc, model.FieldType, a);
					if (xmlElementAttribute3.GetIsNullableSpecified() && !xmlElementAttribute3.IsNullable && typeModel4.TypeDesc.IsOptionalValue)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidNotNullable, typeModel4.TypeDesc.BaseTypeDesc.FullName, "XmlElement"));
					}
					elementAccessor5.IsNullable = (xmlElementAttribute3.GetIsNullableSpecified() ? xmlElementAttribute3.IsNullable : typeModel4.TypeDesc.IsOptionalValue);
					elementAccessor5.Form = (rpc ? XmlSchemaForm.Unqualified : ((xmlElementAttribute3.Form == XmlSchemaForm.None) ? xmlSchemaForm : xmlElementAttribute3.Form));
					CheckNullable(elementAccessor5.IsNullable, typeDesc6, elementAccessor5.Mapping);
					if (!rpc)
					{
						CheckForm(elementAccessor5.Form, ns != elementAccessor5.Namespace);
						elementAccessor5 = ReconcileLocalAccessor(elementAccessor5, ns);
					}
					if (xmlElementAttribute3.Order != -1)
					{
						if (xmlElementAttribute3.Order != num && num != -1)
						{
							throw new InvalidOperationException(System.SR.Format(System.SR.XmlSequenceMatch, "Order"));
						}
						num = xmlElementAttribute3.Order;
					}
					AddUniqueAccessor(nameTable, elementAccessor5);
					arrayList.Add(elementAccessor5);
				}
				NameTable nameTable3 = new NameTable();
				for (int m = 0; m < a.XmlAnyElements.Count; m++)
				{
					XmlAnyElementAttribute xmlAnyElementAttribute2 = a.XmlAnyElements[m];
					Type type6 = (typeof(IXmlSerializable).IsAssignableFrom(fieldType) ? fieldType : (typeof(XmlNode).IsAssignableFrom(fieldType) ? fieldType : typeof(XmlElement)));
					if (!fieldType.IsAssignableFrom(type6))
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalAnyElement, fieldType.FullName));
					}
					string name3 = ((xmlAnyElementAttribute2.Name.Length == 0) ? xmlAnyElementAttribute2.Name : XmlConvert.EncodeLocalName(xmlAnyElementAttribute2.Name));
					string text2 = (xmlAnyElementAttribute2.GetNamespaceSpecified() ? xmlAnyElementAttribute2.Namespace : null);
					if (nameTable3[name3, text2] != null)
					{
						continue;
					}
					nameTable3[name3, text2] = xmlAnyElementAttribute2;
					if (nameTable[name3, (text2 == null) ? ns : text2] != null)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlAnyElementDuplicate, name, xmlAnyElementAttribute2.Name, (xmlAnyElementAttribute2.Namespace == null) ? "null" : xmlAnyElementAttribute2.Namespace));
					}
					ElementAccessor elementAccessor6 = new ElementAccessor();
					elementAccessor6.Name = name3;
					elementAccessor6.Namespace = ((text2 == null) ? ns : text2);
					elementAccessor6.Any = true;
					elementAccessor6.AnyNamespaces = text2;
					TypeDesc typeDesc7 = _typeScope.GetTypeDesc(type6);
					TypeModel typeModel5 = _modelScope.GetTypeModel(type6);
					if (elementAccessor6.Name.Length > 0)
					{
						typeModel5.TypeDesc.IsMixed = true;
					}
					elementAccessor6.Mapping = ImportTypeMapping(typeModel5, elementAccessor6.Namespace, ImportContext.Element, string.Empty, null, repeats: false, openModel, limiter);
					elementAccessor6.Default = GetDefaultValue(model.FieldTypeDesc, model.FieldType, a);
					elementAccessor6.IsNullable = false;
					elementAccessor6.Form = xmlSchemaForm;
					CheckNullable(elementAccessor6.IsNullable, typeDesc7, elementAccessor6.Mapping);
					if (!rpc)
					{
						CheckForm(elementAccessor6.Form, ns != elementAccessor6.Namespace);
						elementAccessor6 = ReconcileLocalAccessor(elementAccessor6, ns);
					}
					if (xmlAnyElementAttribute2.Order != -1)
					{
						if (xmlAnyElementAttribute2.Order != num && num != -1)
						{
							throw new InvalidOperationException(System.SR.Format(System.SR.XmlSequenceMatch, "Order"));
						}
						num = xmlAnyElementAttribute2.Order;
					}
					nameTable.Add(elementAccessor6.Name, elementAccessor6.Namespace, elementAccessor6);
					arrayList.Add(elementAccessor6);
				}
			}
		}
		accessor.Elements = (ElementAccessor[])arrayList.ToArray(typeof(ElementAccessor));
		accessor.SequenceId = num;
		if (rpc)
		{
			if (accessor.TypeDesc.IsArrayLike && accessor.Elements.Length != 0 && !(accessor.Elements[0].Mapping is ArrayMapping))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlRpcLitArrayElement, accessor.Elements[0].Name));
			}
			if (accessor.Xmlns != null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlRpcLitXmlns, accessor.Name));
			}
		}
		if (accessor.ChoiceIdentifier != null)
		{
			accessor.ChoiceIdentifier.MemberIds = new string[accessor.Elements.Length];
			for (int n = 0; n < accessor.Elements.Length; n++)
			{
				bool flag2 = false;
				ElementAccessor elementAccessor7 = accessor.Elements[n];
				EnumMapping enumMapping = (EnumMapping)accessor.ChoiceIdentifier.Mapping;
				for (int num2 = 0; num2 < enumMapping.Constants.Length; num2++)
				{
					string xmlName = enumMapping.Constants[num2].XmlName;
					if (elementAccessor7.Any && elementAccessor7.Name.Length == 0)
					{
						string text3 = ((elementAccessor7.AnyNamespaces == null) ? "##any" : elementAccessor7.AnyNamespaces);
						if (xmlName.Substring(0, xmlName.Length - 1) == text3)
						{
							accessor.ChoiceIdentifier.MemberIds[n] = enumMapping.Constants[num2].Name;
							flag2 = true;
							break;
						}
						continue;
					}
					int num3 = xmlName.LastIndexOf(':');
					string text4 = ((num3 < 0) ? enumMapping.Namespace : xmlName.Substring(0, num3));
					string text5 = ((num3 < 0) ? xmlName : xmlName.Substring(num3 + 1));
					if (elementAccessor7.Name == text5 && ((elementAccessor7.Form == XmlSchemaForm.Unqualified && string.IsNullOrEmpty(text4)) || elementAccessor7.Namespace == text4))
					{
						accessor.ChoiceIdentifier.MemberIds[n] = enumMapping.Constants[num2].Name;
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					if (elementAccessor7.Any && elementAccessor7.Name.Length == 0)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceMissingAnyValue, accessor.ChoiceIdentifier.Mapping.TypeDesc.FullName));
					}
					string text6 = ((elementAccessor7.Namespace != null && elementAccessor7.Namespace.Length > 0) ? (elementAccessor7.Namespace + ":" + elementAccessor7.Name) : elementAccessor7.Name);
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceMissingValue, accessor.ChoiceIdentifier.Mapping.TypeDesc.FullName, text6, elementAccessor7.Name, elementAccessor7.Namespace));
				}
			}
		}
		_arrayNestingLevel = arrayNestingLevel;
		_savedArrayItemAttributes = savedArrayItemAttributes;
		_savedArrayNamespace = savedArrayNamespace;
	}

	private void CheckTopLevelAttributes(XmlAttributes a, string accessorName)
	{
		XmlAttributeFlags xmlFlags = a.XmlFlags;
		if ((xmlFlags & (XmlAttributeFlags)544) != 0)
		{
			throw new InvalidOperationException(System.SR.XmlRpcLitAttributeAttributes);
		}
		if ((xmlFlags & (XmlAttributeFlags)1284) != 0)
		{
			throw new InvalidOperationException(System.SR.XmlRpcLitAttributes);
		}
		if (a.XmlElements != null && a.XmlElements.Count > 0)
		{
			if (a.XmlElements.Count > 1)
			{
				throw new InvalidOperationException(System.SR.XmlRpcLitElements);
			}
			XmlElementAttribute xmlElementAttribute = a.XmlElements[0];
			if (xmlElementAttribute.Namespace != null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlRpcLitElementNamespace, "Namespace", xmlElementAttribute.Namespace));
			}
			if (xmlElementAttribute.IsNullable)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlRpcLitElementNullable, "IsNullable", "true"));
			}
		}
		if (a.XmlArray != null && a.XmlArray.Namespace != null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlRpcLitElementNamespace, "Namespace", a.XmlArray.Namespace));
		}
	}

	private void CheckAmbiguousChoice(XmlAttributes a, Type accessorType, string accessorName)
	{
		Hashtable hashtable = new Hashtable();
		XmlElementAttributes xmlElements = a.XmlElements;
		if (xmlElements != null && xmlElements.Count >= 2 && a.XmlChoiceIdentifier == null)
		{
			for (int i = 0; i < xmlElements.Count; i++)
			{
				Type key = ((xmlElements[i].Type == null) ? accessorType : xmlElements[i].Type);
				if (hashtable.Contains(key))
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceIdentiferMissing, "XmlChoiceIdentifierAttribute", accessorName));
				}
				hashtable.Add(key, false);
			}
		}
		if (hashtable.Contains(typeof(XmlElement)) && a.XmlAnyElements.Count > 0)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceIdentiferMissing, "XmlChoiceIdentifierAttribute", accessorName));
		}
		XmlArrayItemAttributes xmlArrayItems = a.XmlArrayItems;
		if (xmlArrayItems == null || xmlArrayItems.Count < 2)
		{
			return;
		}
		NameTable nameTable = new NameTable();
		for (int j = 0; j < xmlArrayItems.Count; j++)
		{
			Type type = ((xmlArrayItems[j].Type == null) ? accessorType : xmlArrayItems[j].Type);
			string ns = xmlArrayItems[j].NestingLevel.ToString(CultureInfo.InvariantCulture);
			XmlArrayItemAttribute xmlArrayItemAttribute = (XmlArrayItemAttribute)nameTable[type.FullName, ns];
			if (xmlArrayItemAttribute != null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlArrayItemAmbiguousTypes, accessorName, xmlArrayItemAttribute.ElementName, xmlArrayItems[j].ElementName, "XmlElementAttribute", "XmlChoiceIdentifierAttribute", accessorName));
			}
			nameTable[type.FullName, ns] = xmlArrayItems[j];
		}
	}

	private void CheckChoiceIdentifierMapping(EnumMapping choiceMapping)
	{
		NameTable nameTable = new NameTable();
		for (int i = 0; i < choiceMapping.Constants.Length; i++)
		{
			string xmlName = choiceMapping.Constants[i].XmlName;
			int num = xmlName.LastIndexOf(':');
			string name = ((num < 0) ? xmlName : xmlName.Substring(num + 1));
			string ns = ((num < 0) ? "" : xmlName.Substring(0, num));
			if (nameTable[name, ns] != null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceIdDuplicate, choiceMapping.TypeName, xmlName));
			}
			nameTable.Add(name, ns, choiceMapping.Constants[i]);
		}
	}

	private object GetDefaultValue(TypeDesc fieldTypeDesc, Type t, XmlAttributes a)
	{
		if (a.XmlDefaultValue == null || a.XmlDefaultValue == DBNull.Value)
		{
			return null;
		}
		if (fieldTypeDesc.Kind != TypeKind.Primitive && fieldTypeDesc.Kind != TypeKind.Enum)
		{
			a.XmlDefaultValue = null;
			return a.XmlDefaultValue;
		}
		if (fieldTypeDesc.Kind == TypeKind.Enum)
		{
			string text = Enum.Format(t, a.XmlDefaultValue, "G").Replace(",", " ");
			string text2 = Enum.Format(t, a.XmlDefaultValue, "D");
			if (text == text2)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidDefaultValue, text, a.XmlDefaultValue.GetType().FullName));
			}
			return text;
		}
		return a.XmlDefaultValue;
	}

	private static XmlArrayItemAttribute CreateArrayItemAttribute(TypeDesc typeDesc, int nestingLevel)
	{
		XmlArrayItemAttribute xmlArrayItemAttribute = new XmlArrayItemAttribute();
		xmlArrayItemAttribute.NestingLevel = nestingLevel;
		return xmlArrayItemAttribute;
	}

	private static XmlArrayAttribute CreateArrayAttribute(TypeDesc typeDesc)
	{
		return new XmlArrayAttribute();
	}

	private static XmlElementAttribute CreateElementAttribute(TypeDesc typeDesc)
	{
		XmlElementAttribute xmlElementAttribute = new XmlElementAttribute();
		xmlElementAttribute.IsNullable = typeDesc.IsOptionalValue;
		return xmlElementAttribute;
	}

	private static void AddUniqueAccessor(INameScope scope, Accessor accessor)
	{
		Accessor accessor2 = (Accessor)scope[accessor.Name, accessor.Namespace];
		if (accessor2 != null)
		{
			if (accessor is ElementAccessor)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlDuplicateElementName, accessor2.Name, accessor2.Namespace));
			}
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlDuplicateAttributeName, accessor2.Name, accessor2.Namespace));
		}
		scope[accessor.Name, accessor.Namespace] = accessor;
	}

	private static void AddUniqueAccessor(MemberMapping member, INameScope elements, INameScope attributes, bool isSequence)
	{
		if (member.Attribute != null)
		{
			AddUniqueAccessor(attributes, member.Attribute);
		}
		else if (!isSequence && member.Elements != null && member.Elements.Length != 0)
		{
			for (int i = 0; i < member.Elements.Length; i++)
			{
				AddUniqueAccessor(elements, member.Elements[i]);
			}
		}
	}

	private static void CheckForm(XmlSchemaForm form, bool isQualified)
	{
		if (isQualified && form == XmlSchemaForm.Unqualified)
		{
			throw new InvalidOperationException(System.SR.XmlInvalidFormUnqualified);
		}
	}

	private static void CheckNullable(bool isNullable, TypeDesc typeDesc, TypeMapping mapping)
	{
		if (mapping is NullableMapping || mapping is SerializableMapping || !isNullable || typeDesc.IsNullable)
		{
			return;
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidIsNullable, typeDesc.FullName));
	}

	private static ElementAccessor CreateElementAccessor(TypeMapping mapping, string ns)
	{
		ElementAccessor elementAccessor = new ElementAccessor();
		bool flag = mapping.TypeDesc.Kind == TypeKind.Node;
		if (!flag && mapping is SerializableMapping)
		{
			flag = ((SerializableMapping)mapping).IsAny;
		}
		if (flag)
		{
			elementAccessor.Any = true;
		}
		else
		{
			elementAccessor.Name = mapping.DefaultElementName;
			elementAccessor.Namespace = ns;
		}
		elementAccessor.Mapping = mapping;
		return elementAccessor;
	}

	[RequiresUnreferencedCode("Calls TypeScope.GetTypeDesc(Type) which has RequiresUnreferencedCode")]
	internal static XmlTypeMapping GetTopLevelMapping(Type type, string defaultNamespace)
	{
		defaultNamespace = defaultNamespace ?? string.Empty;
		XmlAttributes xmlAttributes = new XmlAttributes(type);
		TypeDesc typeDesc = new TypeScope().GetTypeDesc(type);
		ElementAccessor elementAccessor = new ElementAccessor();
		if (typeDesc.Kind == TypeKind.Node)
		{
			elementAccessor.Any = true;
		}
		else
		{
			string @namespace = ((xmlAttributes.XmlRoot == null) ? defaultNamespace : xmlAttributes.XmlRoot.Namespace);
			string text = string.Empty;
			if (xmlAttributes.XmlType != null)
			{
				text = xmlAttributes.XmlType.TypeName;
			}
			if (text.Length == 0)
			{
				text = type.Name;
			}
			elementAccessor.Name = XmlConvert.EncodeLocalName(text);
			elementAccessor.Namespace = @namespace;
		}
		XmlTypeMapping xmlTypeMapping = new XmlTypeMapping(null, elementAccessor);
		xmlTypeMapping.SetKeyInternal(XmlMapping.GenerateKey(type, xmlAttributes.XmlRoot, defaultNamespace));
		return xmlTypeMapping;
	}
}
