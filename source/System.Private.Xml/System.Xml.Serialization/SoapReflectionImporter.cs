using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Xml.Schema;

namespace System.Xml.Serialization;

public class SoapReflectionImporter
{
	private readonly TypeScope _typeScope;

	private readonly SoapAttributeOverrides _attributeOverrides;

	private readonly NameTable _types = new NameTable();

	private readonly NameTable _nullables = new NameTable();

	private StructMapping _root;

	private readonly string _defaultNs;

	private readonly ModelScope _modelScope;

	public SoapReflectionImporter()
		: this(null, null)
	{
	}

	public SoapReflectionImporter(string? defaultNamespace)
		: this(null, defaultNamespace)
	{
	}

	public SoapReflectionImporter(SoapAttributeOverrides? attributeOverrides)
		: this(attributeOverrides, null)
	{
	}

	public SoapReflectionImporter(SoapAttributeOverrides? attributeOverrides, string? defaultNamespace)
	{
		if (defaultNamespace == null)
		{
			defaultNamespace = string.Empty;
		}
		if (attributeOverrides == null)
		{
			attributeOverrides = new SoapAttributeOverrides();
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
		object[] customAttributes = provider.GetCustomAttributes(typeof(SoapIncludeAttribute), inherit: false);
		for (int i = 0; i < customAttributes.Length; i++)
		{
			IncludeType(((SoapIncludeAttribute)customAttributes[i]).Type, limiter);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void IncludeType(Type type)
	{
		IncludeType(type, new RecursionLimiter());
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private void IncludeType(Type type, RecursionLimiter limiter)
	{
		ImportTypeMapping(_modelScope.GetTypeModel(type), limiter);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlTypeMapping ImportTypeMapping(Type type)
	{
		return ImportTypeMapping(type, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlTypeMapping ImportTypeMapping(Type type, string? defaultNamespace)
	{
		ElementAccessor elementAccessor = new ElementAccessor();
		elementAccessor.IsSoap = true;
		elementAccessor.Mapping = ImportTypeMapping(_modelScope.GetTypeModel(type), new RecursionLimiter());
		elementAccessor.Name = elementAccessor.Mapping.DefaultElementName;
		elementAccessor.Namespace = ((elementAccessor.Mapping.Namespace == null) ? defaultNamespace : elementAccessor.Mapping.Namespace);
		elementAccessor.Form = XmlSchemaForm.Qualified;
		XmlTypeMapping xmlTypeMapping = new XmlTypeMapping(_typeScope, elementAccessor);
		xmlTypeMapping.SetKeyInternal(XmlMapping.GenerateKey(type, null, defaultNamespace));
		xmlTypeMapping.IsSoap = true;
		xmlTypeMapping.GenerateSerializer = true;
		return xmlTypeMapping;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, XmlReflectionMember[] members)
	{
		return ImportMembersMapping(elementName, ns, members, hasWrapperElement: true, writeAccessors: true, validate: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors)
	{
		return ImportMembersMapping(elementName, ns, members, hasWrapperElement, writeAccessors, validate: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors, bool validate)
	{
		return ImportMembersMapping(elementName, ns, members, hasWrapperElement, writeAccessors, validate, XmlMappingAccess.Read | XmlMappingAccess.Write);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors, bool validate, XmlMappingAccess access)
	{
		ElementAccessor elementAccessor = new ElementAccessor();
		elementAccessor.IsSoap = true;
		elementAccessor.Name = ((elementName == null || elementName.Length == 0) ? elementName : XmlConvert.EncodeLocalName(elementName));
		elementAccessor.Mapping = ImportMembersMapping(members, ns, hasWrapperElement, writeAccessors, validate, new RecursionLimiter());
		elementAccessor.Mapping.TypeName = elementName;
		elementAccessor.Namespace = ((elementAccessor.Mapping.Namespace == null) ? ns : elementAccessor.Mapping.Namespace);
		elementAccessor.Form = XmlSchemaForm.Qualified;
		XmlMembersMapping xmlMembersMapping = new XmlMembersMapping(_typeScope, elementAccessor, access);
		xmlMembersMapping.IsSoap = true;
		xmlMembersMapping.GenerateSerializer = true;
		return xmlMembersMapping;
	}

	private Exception ReflectionException(string context, Exception e)
	{
		return new InvalidOperationException(System.SR.Format(System.SR.XmlReflectionError, context), e);
	}

	private SoapAttributes GetAttributes(Type type)
	{
		SoapAttributes soapAttributes = _attributeOverrides[type];
		if (soapAttributes != null)
		{
			return soapAttributes;
		}
		return new SoapAttributes(type);
	}

	private SoapAttributes GetAttributes(MemberInfo memberInfo)
	{
		SoapAttributes soapAttributes = _attributeOverrides[memberInfo.DeclaringType, memberInfo.Name];
		if (soapAttributes != null)
		{
			return soapAttributes;
		}
		return new SoapAttributes(memberInfo);
	}

	[RequiresUnreferencedCode("calls ImportTypeMapping")]
	private TypeMapping ImportTypeMapping(TypeModel model, RecursionLimiter limiter)
	{
		return ImportTypeMapping(model, string.Empty, limiter);
	}

	[RequiresUnreferencedCode("Calls TypeDesc")]
	private TypeMapping ImportTypeMapping(TypeModel model, string dataType, RecursionLimiter limiter)
	{
		if (dataType.Length > 0)
		{
			if (!model.TypeDesc.IsPrimitive)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidDataTypeUsage, dataType, "SoapElementAttribute.DataType"));
			}
			TypeDesc typeDesc = _typeScope.GetTypeDesc(dataType, "http://www.w3.org/2001/XMLSchema");
			if (typeDesc == null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidXsdDataType, dataType, "SoapElementAttribute.DataType", new XmlQualifiedName(dataType, "http://www.w3.org/2001/XMLSchema").ToString()));
			}
			if (model.TypeDesc.FullName != typeDesc.FullName)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlDataTypeMismatch, dataType, "SoapElementAttribute.DataType", model.TypeDesc.FullName));
			}
		}
		SoapAttributes attributes = GetAttributes(model.Type);
		if (((uint)attributes.GetSoapFlags() & 0xFFFFFFFDu) != 0)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidTypeAttributes, model.Type.FullName));
		}
		switch (model.TypeDesc.Kind)
		{
		case TypeKind.Enum:
			return ImportEnumMapping((EnumModel)model);
		case TypeKind.Primitive:
			return ImportPrimitiveMapping((PrimitiveModel)model, dataType);
		case TypeKind.Array:
		case TypeKind.Collection:
		case TypeKind.Enumerable:
			return ImportArrayLikeMapping((ArrayModel)model, limiter);
		case TypeKind.Root:
		case TypeKind.Struct:
		case TypeKind.Class:
			if (model.TypeDesc.IsOptionalValue)
			{
				TypeDesc baseTypeDesc = model.TypeDesc.BaseTypeDesc;
				SoapAttributes attributes2 = GetAttributes(baseTypeDesc.Type);
				string ns = _defaultNs;
				if (attributes2.SoapType != null && attributes2.SoapType.Namespace != null)
				{
					ns = attributes2.SoapType.Namespace;
				}
				TypeDesc typeDesc2 = (string.IsNullOrEmpty(dataType) ? model.TypeDesc.BaseTypeDesc : _typeScope.GetTypeDesc(dataType, "http://www.w3.org/2001/XMLSchema"));
				string typeName = (string.IsNullOrEmpty(dataType) ? model.TypeDesc.BaseTypeDesc.Name : dataType);
				TypeMapping typeMapping = GetTypeMapping(typeName, ns, typeDesc2);
				if (typeMapping == null)
				{
					typeMapping = ImportTypeMapping(_modelScope.GetTypeModel(baseTypeDesc.Type), dataType, limiter);
				}
				return CreateNullableMapping(typeMapping, model.TypeDesc.Type);
			}
			return ImportStructLikeMapping((StructModel)model, limiter);
		default:
			throw new NotSupportedException(System.SR.Format(System.SR.XmlUnsupportedSoapTypeKind, model.TypeDesc.FullName));
		}
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private StructMapping CreateRootMapping()
	{
		TypeDesc typeDesc = _typeScope.GetTypeDesc(typeof(object));
		StructMapping structMapping = new StructMapping();
		structMapping.IsSoap = true;
		structMapping.TypeDesc = typeDesc;
		structMapping.Members = Array.Empty<MemberMapping>();
		structMapping.IncludeInSchema = false;
		structMapping.TypeName = "anyType";
		structMapping.Namespace = "http://www.w3.org/2001/XMLSchema";
		return structMapping;
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

	private TypeMapping GetTypeMapping(string typeName, string ns, TypeDesc typeDesc)
	{
		TypeMapping typeMapping = (TypeMapping)_types[typeName, ns];
		if (typeMapping == null)
		{
			return null;
		}
		if (typeMapping.TypeDesc != typeDesc)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlTypesDuplicate, typeDesc.FullName, typeMapping.TypeDesc.FullName, typeName, ns));
		}
		return typeMapping;
	}

	private NullableMapping CreateNullableMapping(TypeMapping baseMapping, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
	{
		TypeDesc nullableTypeDesc = baseMapping.TypeDesc.GetNullableTypeDesc(type);
		TypeMapping typeMapping = (TypeMapping)_nullables[baseMapping.TypeName, baseMapping.Namespace];
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
			if (!(baseMapping is PrimitiveMapping))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlTypesDuplicate, nullableTypeDesc.FullName, typeMapping.TypeDesc.FullName, nullableTypeDesc.Name, typeMapping.Namespace));
			}
		}
		nullableMapping = new NullableMapping();
		nullableMapping.BaseMapping = baseMapping;
		nullableMapping.TypeDesc = nullableTypeDesc;
		nullableMapping.TypeName = baseMapping.TypeName;
		nullableMapping.Namespace = baseMapping.Namespace;
		nullableMapping.IncludeInSchema = false;
		_nullables.Add(baseMapping.TypeName, nullableMapping.Namespace, nullableMapping);
		_typeScope.AddTypeMapping(nullableMapping);
		return nullableMapping;
	}

	[RequiresUnreferencedCode("calls GetRootMapping")]
	private StructMapping ImportStructLikeMapping(StructModel model, RecursionLimiter limiter)
	{
		if (model.TypeDesc.Kind == TypeKind.Root)
		{
			return GetRootMapping();
		}
		SoapAttributes attributes = GetAttributes(model.Type);
		string text = _defaultNs;
		if (attributes.SoapType != null && attributes.SoapType.Namespace != null)
		{
			text = attributes.SoapType.Namespace;
		}
		string name = XsdTypeName(model.Type, attributes, model.TypeDesc.Name);
		name = XmlConvert.EncodeLocalName(name);
		StructMapping structMapping = (StructMapping)GetTypeMapping(name, text, model.TypeDesc);
		if (structMapping == null)
		{
			structMapping = new StructMapping();
			structMapping.IsSoap = true;
			structMapping.TypeDesc = model.TypeDesc;
			structMapping.Namespace = text;
			structMapping.TypeName = name;
			if (attributes.SoapType != null)
			{
				structMapping.IncludeInSchema = attributes.SoapType.IncludeInSchema;
			}
			_typeScope.AddTypeMapping(structMapping);
			_types.Add(name, text, structMapping);
			if (limiter.IsExceededLimit)
			{
				limiter.DeferredWorkItems.Add(new ImportStructWorkItem(model, structMapping));
				return structMapping;
			}
			limiter.Depth++;
			InitializeStructMembers(structMapping, model, limiter);
			while (limiter.DeferredWorkItems.Count > 0)
			{
				int index = limiter.DeferredWorkItems.Count - 1;
				ImportStructWorkItem importStructWorkItem = limiter.DeferredWorkItems[index];
				if (InitializeStructMembers(importStructWorkItem.Mapping, importStructWorkItem.Model, limiter))
				{
					limiter.DeferredWorkItems.RemoveAt(index);
				}
			}
			limiter.Depth--;
		}
		return structMapping;
	}

	[RequiresUnreferencedCode("calls GetTypeModel")]
	private bool InitializeStructMembers(StructMapping mapping, StructModel model, RecursionLimiter limiter)
	{
		if (mapping.IsFullyInitialized)
		{
			return true;
		}
		if (model.TypeDesc.BaseTypeDesc != null)
		{
			StructMapping baseMapping = ImportStructLikeMapping((StructModel)_modelScope.GetTypeModel(model.Type.BaseType, directReference: false), limiter);
			int num = limiter.DeferredWorkItems.IndexOf(mapping.BaseMapping);
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
			mapping.BaseMapping = baseMapping;
		}
		List<MemberMapping> list = new List<MemberMapping>();
		MemberInfo[] memberInfos = model.GetMemberInfos();
		foreach (MemberInfo memberInfo in memberInfos)
		{
			if (!(memberInfo is FieldInfo) && !(memberInfo is PropertyInfo))
			{
				continue;
			}
			SoapAttributes attributes = GetAttributes(memberInfo);
			if (attributes.SoapIgnore)
			{
				continue;
			}
			FieldModel fieldModel = model.GetFieldModel(memberInfo);
			if (fieldModel == null)
			{
				continue;
			}
			MemberMapping memberMapping = ImportFieldMapping(fieldModel, attributes, mapping.Namespace, limiter);
			if (memberMapping == null)
			{
				continue;
			}
			if (!memberMapping.TypeDesc.IsPrimitive && !memberMapping.TypeDesc.IsEnum && !memberMapping.TypeDesc.IsOptionalValue)
			{
				if (model.TypeDesc.IsValueType)
				{
					throw new NotSupportedException(System.SR.Format(System.SR.XmlRpcRefsInValueType, model.TypeDesc.FullName));
				}
				if (memberMapping.TypeDesc.IsValueType)
				{
					throw new NotSupportedException(System.SR.Format(System.SR.XmlRpcNestedValueType, memberMapping.TypeDesc.FullName));
				}
			}
			if (mapping.BaseMapping == null || !mapping.BaseMapping.Declares(memberMapping, mapping.TypeName))
			{
				list.Add(memberMapping);
			}
		}
		mapping.Members = list.ToArray();
		if (mapping.BaseMapping == null)
		{
			mapping.BaseMapping = GetRootMapping();
		}
		IncludeTypes(model.Type, limiter);
		return true;
	}

	[RequiresUnreferencedCode("calls IncludeTypes")]
	private ArrayMapping ImportArrayLikeMapping(ArrayModel model, RecursionLimiter limiter)
	{
		ArrayMapping arrayMapping = new ArrayMapping();
		arrayMapping.IsSoap = true;
		TypeMapping typeMapping = ImportTypeMapping(model.Element, limiter);
		if (typeMapping.TypeDesc.IsValueType && !typeMapping.TypeDesc.IsPrimitive && !typeMapping.TypeDesc.IsEnum)
		{
			throw new NotSupportedException(System.SR.Format(System.SR.XmlRpcArrayOfValueTypes, model.TypeDesc.FullName));
		}
		arrayMapping.TypeDesc = model.TypeDesc;
		arrayMapping.Elements = new ElementAccessor[1] { CreateElementAccessor(typeMapping, arrayMapping.Namespace) };
		SetArrayMappingType(arrayMapping);
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
			_types[arrayMapping.TypeName, arrayMapping.Namespace] = arrayMapping;
			return arrayMapping;
		}
		_typeScope.AddTypeMapping(arrayMapping);
		_types.Add(arrayMapping.TypeName, arrayMapping.Namespace, arrayMapping);
		IncludeTypes(model.Type);
		return arrayMapping;
	}

	private void SetArrayMappingType(ArrayMapping mapping)
	{
		bool flag = false;
		TypeMapping typeMapping = ((mapping.Elements.Length != 1) ? null : mapping.Elements[0].Mapping);
		string text;
		string identifier;
		if (typeMapping is EnumMapping)
		{
			text = typeMapping.Namespace;
			identifier = typeMapping.TypeName;
		}
		else if (typeMapping is PrimitiveMapping)
		{
			text = (typeMapping.TypeDesc.IsXsdType ? "http://www.w3.org/2001/XMLSchema" : "http://microsoft.com/wsdl/types/");
			identifier = typeMapping.TypeDesc.DataType.Name;
			flag = true;
		}
		else if (typeMapping is StructMapping)
		{
			if (typeMapping.TypeDesc.IsRoot)
			{
				text = "http://www.w3.org/2001/XMLSchema";
				identifier = "anyType";
				flag = true;
			}
			else
			{
				text = typeMapping.Namespace;
				identifier = typeMapping.TypeName;
			}
		}
		else
		{
			if (!(typeMapping is ArrayMapping))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidSoapArray, mapping.TypeDesc.FullName));
			}
			text = typeMapping.Namespace;
			identifier = typeMapping.TypeName;
		}
		identifier = CodeIdentifier.MakePascal(identifier);
		string text2 = "ArrayOf" + identifier;
		string text3 = (flag ? _defaultNs : text);
		int num = 1;
		TypeMapping typeMapping2 = (TypeMapping)_types[text2, text3];
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
			text2 = identifier + num.ToString(CultureInfo.InvariantCulture);
			typeMapping2 = (TypeMapping)_types[text2, text3];
			num++;
		}
		mapping.Namespace = text3;
		mapping.TypeName = text2;
	}

	private PrimitiveMapping ImportPrimitiveMapping(PrimitiveModel model, string dataType)
	{
		PrimitiveMapping primitiveMapping = new PrimitiveMapping();
		primitiveMapping.IsSoap = true;
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
		return primitiveMapping;
	}

	[RequiresUnreferencedCode("calls XsdTypeName")]
	private EnumMapping ImportEnumMapping(EnumModel model)
	{
		SoapAttributes attributes = GetAttributes(model.Type);
		string text = _defaultNs;
		if (attributes.SoapType != null && attributes.SoapType.Namespace != null)
		{
			text = attributes.SoapType.Namespace;
		}
		string name = XsdTypeName(model.Type, attributes, model.TypeDesc.Name);
		name = XmlConvert.EncodeLocalName(name);
		EnumMapping enumMapping = (EnumMapping)GetTypeMapping(name, text, model.TypeDesc);
		if (enumMapping == null)
		{
			enumMapping = new EnumMapping();
			enumMapping.IsSoap = true;
			enumMapping.TypeDesc = model.TypeDesc;
			enumMapping.TypeName = name;
			enumMapping.Namespace = text;
			enumMapping.IsFlags = model.Type.IsDefined(typeof(FlagsAttribute), inherit: false);
			_typeScope.AddTypeMapping(enumMapping);
			_types.Add(name, text, enumMapping);
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
		}
		return enumMapping;
	}

	private ConstantMapping ImportConstantMapping(ConstantModel model)
	{
		SoapAttributes attributes = GetAttributes(model.FieldInfo);
		if (attributes.SoapIgnore)
		{
			return null;
		}
		if (((uint)attributes.GetSoapFlags() & 0xFFFFFFFEu) != 0)
		{
			throw new InvalidOperationException(System.SR.XmlInvalidEnumAttribute);
		}
		if (attributes.SoapEnum == null)
		{
			attributes.SoapEnum = new SoapEnumAttribute();
		}
		ConstantMapping constantMapping = new ConstantMapping();
		constantMapping.XmlName = ((attributes.SoapEnum.Name.Length == 0) ? model.Name : attributes.SoapEnum.Name);
		constantMapping.Name = model.Name;
		constantMapping.Value = model.Value;
		return constantMapping;
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private MembersMapping ImportMembersMapping(XmlReflectionMember[] xmlReflectionMembers, string ns, bool hasWrapperElement, bool writeAccessors, bool validateWrapperElement, RecursionLimiter limiter)
	{
		MembersMapping membersMapping = new MembersMapping();
		membersMapping.TypeDesc = _typeScope.GetTypeDesc(typeof(object[]));
		MemberMapping[] array = new MemberMapping[xmlReflectionMembers.Length];
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				XmlReflectionMember xmlReflectionMember = xmlReflectionMembers[i];
				MemberMapping memberMapping = ImportMemberMapping(xmlReflectionMember, ns, xmlReflectionMembers, (!hasWrapperElement) ? XmlSchemaForm.Qualified : XmlSchemaForm.Unqualified, limiter);
				if (xmlReflectionMember.IsReturnValue && writeAccessors)
				{
					if (i > 0)
					{
						throw new InvalidOperationException(System.SR.XmlInvalidReturnPosition);
					}
					memberMapping.IsReturnValue = true;
				}
				array[i] = memberMapping;
			}
			catch (Exception ex)
			{
				if (ex is OutOfMemoryException)
				{
					throw;
				}
				throw ReflectionException(xmlReflectionMembers[i].MemberName, ex);
			}
		}
		membersMapping.Members = array;
		membersMapping.HasWrapperElement = hasWrapperElement;
		if (hasWrapperElement)
		{
			membersMapping.ValidateRpcWrapperElement = validateWrapperElement;
		}
		membersMapping.WriteAccessors = writeAccessors;
		membersMapping.IsSoap = true;
		if (hasWrapperElement && !writeAccessors)
		{
			membersMapping.Namespace = ns;
		}
		return membersMapping;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private MemberMapping ImportMemberMapping(XmlReflectionMember xmlReflectionMember, string ns, XmlReflectionMember[] xmlReflectionMembers, XmlSchemaForm form, RecursionLimiter limiter)
	{
		SoapAttributes soapAttributes = xmlReflectionMember.SoapAttributes;
		if (soapAttributes.SoapIgnore)
		{
			return null;
		}
		MemberMapping memberMapping = new MemberMapping();
		memberMapping.IsSoap = true;
		memberMapping.Name = xmlReflectionMember.MemberName;
		bool checkSpecified = XmlReflectionImporter.FindSpecifiedMember(xmlReflectionMember.MemberName, xmlReflectionMembers) != null;
		FieldModel fieldModel = new FieldModel(xmlReflectionMember.MemberName, xmlReflectionMember.MemberType, _typeScope.GetTypeDesc(xmlReflectionMember.MemberType), checkSpecified, checkShouldPersist: false);
		memberMapping.CheckShouldPersist = fieldModel.CheckShouldPersist;
		memberMapping.CheckSpecified = fieldModel.CheckSpecified;
		memberMapping.ReadOnly = fieldModel.ReadOnly;
		ImportAccessorMapping(memberMapping, fieldModel, soapAttributes, ns, form, limiter);
		if (xmlReflectionMember.OverrideIsNullable)
		{
			memberMapping.Elements[0].IsNullable = false;
		}
		return memberMapping;
	}

	[RequiresUnreferencedCode("calls ImportAccessorMapping")]
	private MemberMapping ImportFieldMapping(FieldModel model, SoapAttributes a, string ns, RecursionLimiter limiter)
	{
		if (a.SoapIgnore)
		{
			return null;
		}
		MemberMapping memberMapping = new MemberMapping();
		memberMapping.IsSoap = true;
		memberMapping.Name = model.Name;
		memberMapping.CheckShouldPersist = model.CheckShouldPersist;
		memberMapping.CheckSpecified = model.CheckSpecified;
		memberMapping.MemberInfo = model.MemberInfo;
		memberMapping.CheckSpecifiedMemberInfo = model.CheckSpecifiedMemberInfo;
		memberMapping.CheckShouldPersistMethodInfo = model.CheckShouldPersistMethodInfo;
		memberMapping.ReadOnly = model.ReadOnly;
		ImportAccessorMapping(memberMapping, model, a, ns, XmlSchemaForm.Unqualified, limiter);
		return memberMapping;
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private void ImportAccessorMapping(MemberMapping accessor, FieldModel model, SoapAttributes a, string ns, XmlSchemaForm form, RecursionLimiter limiter)
	{
		Type fieldType = model.FieldType;
		string name = model.Name;
		accessor.TypeDesc = _typeScope.GetTypeDesc(fieldType);
		if (accessor.TypeDesc.IsVoid)
		{
			throw new InvalidOperationException(System.SR.XmlInvalidVoid);
		}
		SoapAttributeFlags soapFlags = a.GetSoapFlags();
		if ((soapFlags & SoapAttributeFlags.Attribute) == SoapAttributeFlags.Attribute)
		{
			if (!accessor.TypeDesc.IsPrimitive && !accessor.TypeDesc.IsEnum)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlIllegalSoapAttribute, name, accessor.TypeDesc.FullName));
			}
			if ((soapFlags & SoapAttributeFlags.Attribute) != soapFlags)
			{
				throw new InvalidOperationException(System.SR.XmlInvalidElementAttribute);
			}
			AttributeAccessor attributeAccessor = new AttributeAccessor();
			attributeAccessor.Name = Accessor.EscapeQName((a.SoapAttribute == null || a.SoapAttribute.AttributeName.Length == 0) ? name : a.SoapAttribute.AttributeName);
			attributeAccessor.Namespace = ((a.SoapAttribute == null || a.SoapAttribute.Namespace == null) ? ns : a.SoapAttribute.Namespace);
			attributeAccessor.Form = XmlSchemaForm.Qualified;
			attributeAccessor.Mapping = ImportTypeMapping(_modelScope.GetTypeModel(fieldType), (a.SoapAttribute == null) ? string.Empty : a.SoapAttribute.DataType, limiter);
			attributeAccessor.Default = GetDefaultValue(model.FieldTypeDesc, a);
			accessor.Attribute = attributeAccessor;
			accessor.Elements = Array.Empty<ElementAccessor>();
		}
		else
		{
			if ((soapFlags & SoapAttributeFlags.Element) != soapFlags)
			{
				throw new InvalidOperationException(System.SR.XmlInvalidElementAttribute);
			}
			ElementAccessor elementAccessor = new ElementAccessor();
			elementAccessor.IsSoap = true;
			elementAccessor.Name = XmlConvert.EncodeLocalName((a.SoapElement == null || a.SoapElement.ElementName.Length == 0) ? name : a.SoapElement.ElementName);
			elementAccessor.Namespace = ns;
			elementAccessor.Form = form;
			elementAccessor.Mapping = ImportTypeMapping(_modelScope.GetTypeModel(fieldType), (a.SoapElement == null) ? string.Empty : a.SoapElement.DataType, limiter);
			if (a.SoapElement != null)
			{
				elementAccessor.IsNullable = a.SoapElement.IsNullable;
			}
			accessor.Elements = new ElementAccessor[1] { elementAccessor };
		}
	}

	private static ElementAccessor CreateElementAccessor(TypeMapping mapping, string ns)
	{
		ElementAccessor elementAccessor = new ElementAccessor();
		elementAccessor.IsSoap = true;
		elementAccessor.Name = mapping.TypeName;
		elementAccessor.Namespace = ns;
		elementAccessor.Mapping = mapping;
		return elementAccessor;
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	private object GetDefaultValue(TypeDesc fieldTypeDesc, SoapAttributes a)
	{
		if (a.SoapDefaultValue == null || a.SoapDefaultValue == DBNull.Value)
		{
			return null;
		}
		if (fieldTypeDesc.Kind != TypeKind.Primitive && fieldTypeDesc.Kind != TypeKind.Enum)
		{
			a.SoapDefaultValue = null;
			return a.SoapDefaultValue;
		}
		if (fieldTypeDesc.Kind == TypeKind.Enum)
		{
			if (fieldTypeDesc != _typeScope.GetTypeDesc(a.SoapDefaultValue.GetType()))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidDefaultEnumValue, a.SoapDefaultValue.GetType().FullName, fieldTypeDesc.FullName));
			}
			string text = Enum.Format(a.SoapDefaultValue.GetType(), a.SoapDefaultValue, "G").Replace(",", " ");
			string text2 = Enum.Format(a.SoapDefaultValue.GetType(), a.SoapDefaultValue, "D");
			if (text == text2)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidDefaultValue, text, a.SoapDefaultValue.GetType().FullName));
			}
			return text;
		}
		return a.SoapDefaultValue;
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
		return XsdTypeName(type, GetAttributes(type), typeDesc.Name);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	internal string XsdTypeName(Type type, SoapAttributes a, string name)
	{
		string text = name;
		if (a.SoapType != null && a.SoapType.TypeName.Length > 0)
		{
			text = a.SoapType.TypeName;
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
}
