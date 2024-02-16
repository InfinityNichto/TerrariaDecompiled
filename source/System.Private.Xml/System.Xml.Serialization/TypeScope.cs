using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Xml.Schema;

namespace System.Xml.Serialization;

internal sealed class TypeScope
{
	private readonly Hashtable _typeDescs = new Hashtable();

	private readonly Hashtable _arrayTypeDescs = new Hashtable();

	private readonly ArrayList _typeMappings = new ArrayList();

	private static readonly Hashtable s_primitiveTypes;

	private static readonly Hashtable s_primitiveDataTypes;

	private static readonly NameTable s_primitiveNames;

	private static readonly string[] s_unsupportedTypes;

	internal ICollection Types => _typeDescs.Keys;

	internal ICollection TypeMappings => _typeMappings;

	internal static Hashtable PrimtiveTypes => s_primitiveTypes;

	static TypeScope()
	{
		s_primitiveTypes = new Hashtable();
		s_primitiveDataTypes = new Hashtable();
		s_primitiveNames = new NameTable();
		s_unsupportedTypes = new string[20]
		{
			"anyURI", "duration", "ENTITY", "ENTITIES", "gDay", "gMonth", "gMonthDay", "gYear", "gYearMonth", "ID",
			"IDREF", "IDREFS", "integer", "language", "negativeInteger", "nonNegativeInteger", "nonPositiveInteger", "NOTATION", "positiveInteger", "token"
		};
		AddPrimitive(typeof(string), "string", "String", (TypeFlags)2106);
		AddPrimitive(typeof(int), "int", "Int32", (TypeFlags)4136);
		AddPrimitive(typeof(bool), "boolean", "Boolean", (TypeFlags)4136);
		AddPrimitive(typeof(short), "short", "Int16", (TypeFlags)4136);
		AddPrimitive(typeof(long), "long", "Int64", (TypeFlags)4136);
		AddPrimitive(typeof(float), "float", "Single", (TypeFlags)4136);
		AddPrimitive(typeof(double), "double", "Double", (TypeFlags)4136);
		AddPrimitive(typeof(decimal), "decimal", "Decimal", (TypeFlags)4136);
		AddPrimitive(typeof(DateTime), "dateTime", "DateTime", (TypeFlags)4200);
		AddPrimitive(typeof(XmlQualifiedName), "QName", "XmlQualifiedName", (TypeFlags)5226);
		AddPrimitive(typeof(byte), "unsignedByte", "Byte", (TypeFlags)4136);
		AddPrimitive(typeof(sbyte), "byte", "SByte", (TypeFlags)4136);
		AddPrimitive(typeof(ushort), "unsignedShort", "UInt16", (TypeFlags)4136);
		AddPrimitive(typeof(uint), "unsignedInt", "UInt32", (TypeFlags)4136);
		AddPrimitive(typeof(ulong), "unsignedLong", "UInt64", (TypeFlags)4136);
		AddPrimitive(typeof(DateTime), "date", "Date", (TypeFlags)4328);
		AddPrimitive(typeof(DateTime), "time", "Time", (TypeFlags)4328);
		AddPrimitive(typeof(string), "Name", "XmlName", (TypeFlags)234);
		AddPrimitive(typeof(string), "NCName", "XmlNCName", (TypeFlags)234);
		AddPrimitive(typeof(string), "NMTOKEN", "XmlNmToken", (TypeFlags)234);
		AddPrimitive(typeof(string), "NMTOKENS", "XmlNmTokens", (TypeFlags)234);
		AddPrimitive(typeof(byte[]), "base64Binary", "ByteArrayBase64", (TypeFlags)6890);
		AddPrimitive(typeof(byte[]), "hexBinary", "ByteArrayHex", (TypeFlags)6890);
		XmlSchemaPatternFacet xmlSchemaPatternFacet = new XmlSchemaPatternFacet
		{
			Value = "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}"
		};
		AddNonXsdPrimitive(typeof(Guid), "guid", "http://microsoft.com/wsdl/types/", "Guid", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), new XmlSchemaFacet[1] { xmlSchemaPatternFacet }, (TypeFlags)4648);
		AddNonXsdPrimitive(typeof(char), "char", "http://microsoft.com/wsdl/types/", "Char", new XmlQualifiedName("unsignedShort", "http://www.w3.org/2001/XMLSchema"), Array.Empty<XmlSchemaFacet>(), (TypeFlags)616);
		AddNonXsdPrimitive(typeof(TimeSpan), "TimeSpan", "http://microsoft.com/wsdl/types/", "TimeSpan", new XmlQualifiedName("duration", "http://www.w3.org/2001/XMLSchema"), Array.Empty<XmlSchemaFacet>(), (TypeFlags)4136);
		AddNonXsdPrimitive(typeof(DateTimeOffset), "dateTimeOffset", "http://microsoft.com/wsdl/types/", "DateTimeOffset", new XmlQualifiedName("dateTime", "http://www.w3.org/2001/XMLSchema"), Array.Empty<XmlSchemaFacet>(), (TypeFlags)4136);
		AddSoapEncodedTypes("http://schemas.xmlsoap.org/soap/encoding/");
		AddPrimitive(typeof(string), "normalizedString", "String", (TypeFlags)2234);
		for (int i = 0; i < s_unsupportedTypes.Length; i++)
		{
			AddPrimitive(typeof(string), s_unsupportedTypes[i], "String", (TypeFlags)32954);
		}
	}

	internal static bool IsKnownType(Type type)
	{
		if (type == typeof(object))
		{
			return true;
		}
		if (type.IsEnum)
		{
			return false;
		}
		switch (Type.GetTypeCode(type))
		{
		case TypeCode.String:
			return true;
		case TypeCode.Int32:
			return true;
		case TypeCode.Boolean:
			return true;
		case TypeCode.Int16:
			return true;
		case TypeCode.Int64:
			return true;
		case TypeCode.Single:
			return true;
		case TypeCode.Double:
			return true;
		case TypeCode.Decimal:
			return true;
		case TypeCode.DateTime:
			return true;
		case TypeCode.Byte:
			return true;
		case TypeCode.SByte:
			return true;
		case TypeCode.UInt16:
			return true;
		case TypeCode.UInt32:
			return true;
		case TypeCode.UInt64:
			return true;
		case TypeCode.Char:
			return true;
		default:
			if (type == typeof(XmlQualifiedName))
			{
				return true;
			}
			if (type == typeof(byte[]))
			{
				return true;
			}
			if (type == typeof(Guid))
			{
				return true;
			}
			if (type == typeof(TimeSpan))
			{
				return true;
			}
			if (type == typeof(DateTimeOffset))
			{
				return true;
			}
			if (type == typeof(XmlNode[]))
			{
				return true;
			}
			return false;
		}
	}

	private static void AddSoapEncodedTypes(string ns)
	{
		AddSoapEncodedPrimitive(typeof(string), "normalizedString", ns, "String", new XmlQualifiedName("normalizedString", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)2218);
		for (int i = 0; i < s_unsupportedTypes.Length; i++)
		{
			AddSoapEncodedPrimitive(typeof(string), s_unsupportedTypes[i], ns, "String", new XmlQualifiedName(s_unsupportedTypes[i], "http://www.w3.org/2001/XMLSchema"), (TypeFlags)32938);
		}
		AddSoapEncodedPrimitive(typeof(string), "string", ns, "String", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)58);
		AddSoapEncodedPrimitive(typeof(int), "int", ns, "Int32", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4136);
		AddSoapEncodedPrimitive(typeof(bool), "boolean", ns, "Boolean", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4136);
		AddSoapEncodedPrimitive(typeof(short), "short", ns, "Int16", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4136);
		AddSoapEncodedPrimitive(typeof(long), "long", ns, "Int64", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4136);
		AddSoapEncodedPrimitive(typeof(float), "float", ns, "Single", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4136);
		AddSoapEncodedPrimitive(typeof(double), "double", ns, "Double", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4136);
		AddSoapEncodedPrimitive(typeof(decimal), "decimal", ns, "Decimal", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4136);
		AddSoapEncodedPrimitive(typeof(DateTime), "dateTime", ns, "DateTime", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4200);
		AddSoapEncodedPrimitive(typeof(XmlQualifiedName), "QName", ns, "XmlQualifiedName", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)5226);
		AddSoapEncodedPrimitive(typeof(byte), "unsignedByte", ns, "Byte", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4136);
		AddSoapEncodedPrimitive(typeof(sbyte), "byte", ns, "SByte", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4136);
		AddSoapEncodedPrimitive(typeof(ushort), "unsignedShort", ns, "UInt16", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4136);
		AddSoapEncodedPrimitive(typeof(uint), "unsignedInt", ns, "UInt32", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4136);
		AddSoapEncodedPrimitive(typeof(ulong), "unsignedLong", ns, "UInt64", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4136);
		AddSoapEncodedPrimitive(typeof(DateTime), "date", ns, "Date", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4328);
		AddSoapEncodedPrimitive(typeof(DateTime), "time", ns, "Time", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4328);
		AddSoapEncodedPrimitive(typeof(string), "Name", ns, "XmlName", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)234);
		AddSoapEncodedPrimitive(typeof(string), "NCName", ns, "XmlNCName", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)234);
		AddSoapEncodedPrimitive(typeof(string), "NMTOKEN", ns, "XmlNmToken", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)234);
		AddSoapEncodedPrimitive(typeof(string), "NMTOKENS", ns, "XmlNmTokens", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)234);
		AddSoapEncodedPrimitive(typeof(byte[]), "base64Binary", ns, "ByteArrayBase64", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4842);
		AddSoapEncodedPrimitive(typeof(byte[]), "hexBinary", ns, "ByteArrayHex", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)4842);
		AddSoapEncodedPrimitive(typeof(string), "arrayCoordinate", ns, "String", new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)40);
		AddSoapEncodedPrimitive(typeof(byte[]), "base64", ns, "ByteArrayBase64", new XmlQualifiedName("base64Binary", "http://www.w3.org/2001/XMLSchema"), (TypeFlags)554);
	}

	private static void AddPrimitive([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string dataTypeName, string formatterName, TypeFlags flags)
	{
		XmlSchemaSimpleType xmlSchemaSimpleType = new XmlSchemaSimpleType();
		xmlSchemaSimpleType.Name = dataTypeName;
		TypeDesc value = new TypeDesc(type, isXsdType: true, xmlSchemaSimpleType, formatterName, flags);
		if (s_primitiveTypes[type] == null)
		{
			s_primitiveTypes.Add(type, value);
		}
		s_primitiveDataTypes.Add(xmlSchemaSimpleType, value);
		s_primitiveNames.Add(dataTypeName, "http://www.w3.org/2001/XMLSchema", value);
	}

	private static void AddNonXsdPrimitive([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string dataTypeName, string ns, string formatterName, XmlQualifiedName baseTypeName, XmlSchemaFacet[] facets, TypeFlags flags)
	{
		XmlSchemaSimpleType xmlSchemaSimpleType = new XmlSchemaSimpleType();
		xmlSchemaSimpleType.Name = dataTypeName;
		XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction = new XmlSchemaSimpleTypeRestriction();
		xmlSchemaSimpleTypeRestriction.BaseTypeName = baseTypeName;
		foreach (XmlSchemaFacet item in facets)
		{
			xmlSchemaSimpleTypeRestriction.Facets.Add(item);
		}
		xmlSchemaSimpleType.Content = xmlSchemaSimpleTypeRestriction;
		TypeDesc value = new TypeDesc(type, isXsdType: false, xmlSchemaSimpleType, formatterName, flags);
		if (s_primitiveTypes[type] == null)
		{
			s_primitiveTypes.Add(type, value);
		}
		s_primitiveDataTypes.Add(xmlSchemaSimpleType, value);
		s_primitiveNames.Add(dataTypeName, ns, value);
	}

	private static void AddSoapEncodedPrimitive([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, string dataTypeName, string ns, string formatterName, XmlQualifiedName baseTypeName, TypeFlags flags)
	{
		AddNonXsdPrimitive(type, dataTypeName, ns, formatterName, baseTypeName, Array.Empty<XmlSchemaFacet>(), flags);
	}

	internal TypeDesc GetTypeDesc(string name, string ns)
	{
		return GetTypeDesc(name, ns, (TypeFlags)56);
	}

	internal TypeDesc GetTypeDesc(string name, string ns, TypeFlags flags)
	{
		TypeDesc typeDesc = (TypeDesc)s_primitiveNames[name, ns];
		if (typeDesc != null && (typeDesc.Flags & flags) != 0)
		{
			return typeDesc;
		}
		return null;
	}

	internal TypeDesc GetTypeDesc(XmlSchemaSimpleType dataType)
	{
		return (TypeDesc)s_primitiveDataTypes[dataType];
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	internal TypeDesc GetTypeDesc(Type type)
	{
		return GetTypeDesc(type, null, directReference: true, throwOnError: true);
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	internal TypeDesc GetTypeDesc(Type type, MemberInfo source, bool directReference)
	{
		return GetTypeDesc(type, source, directReference, throwOnError: true);
	}

	[RequiresUnreferencedCode("calls ImportTypeDesc")]
	internal TypeDesc GetTypeDesc(Type type, MemberInfo source, bool directReference, bool throwOnError)
	{
		if (type.ContainsGenericParameters)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlUnsupportedOpenGenericType, type));
		}
		TypeDesc typeDesc = (TypeDesc)s_primitiveTypes[type];
		if (typeDesc == null)
		{
			typeDesc = (TypeDesc)_typeDescs[type];
			if (typeDesc == null)
			{
				typeDesc = ImportTypeDesc(type, source, directReference);
			}
		}
		if (throwOnError)
		{
			typeDesc.CheckSupported();
		}
		return typeDesc;
	}

	[RequiresUnreferencedCode("calls ImportTypeDesc")]
	internal TypeDesc GetArrayTypeDesc(Type type)
	{
		TypeDesc typeDesc = (TypeDesc)_arrayTypeDescs[type];
		if (typeDesc == null)
		{
			typeDesc = GetTypeDesc(type);
			if (!typeDesc.IsArrayLike)
			{
				typeDesc = ImportTypeDesc(type, null, directReference: false);
			}
			typeDesc.CheckSupported();
			_arrayTypeDescs.Add(type, typeDesc);
		}
		return typeDesc;
	}

	internal TypeMapping GetTypeMappingFromTypeDesc(TypeDesc typeDesc)
	{
		foreach (TypeMapping typeMapping in TypeMappings)
		{
			if (typeMapping.TypeDesc == typeDesc)
			{
				return typeMapping;
			}
		}
		return null;
	}

	internal Type GetTypeFromTypeDesc(TypeDesc typeDesc)
	{
		if (typeDesc.Type != null)
		{
			return typeDesc.Type;
		}
		foreach (DictionaryEntry typeDesc2 in _typeDescs)
		{
			if (typeDesc2.Value == typeDesc)
			{
				return typeDesc2.Key as Type;
			}
		}
		return null;
	}

	[RequiresUnreferencedCode("calls GetEnumeratorElementType")]
	private TypeDesc ImportTypeDesc(Type type, MemberInfo memberInfo, bool directReference)
	{
		TypeDesc typeDesc = null;
		Type type2 = null;
		Type type3 = null;
		TypeFlags flags = TypeFlags.None;
		Exception exception = null;
		if (!type.IsVisible)
		{
			flags |= TypeFlags.Unsupported;
			exception = new InvalidOperationException(System.SR.Format(System.SR.XmlTypeInaccessible, type.FullName));
		}
		else if (directReference && type.IsAbstract && type.IsSealed)
		{
			flags |= TypeFlags.Unsupported;
			exception = new InvalidOperationException(System.SR.Format(System.SR.XmlTypeStatic, type.FullName));
		}
		if (DynamicAssemblies.IsTypeDynamic(type))
		{
			flags |= TypeFlags.UseReflection;
		}
		if (!type.IsValueType)
		{
			flags |= TypeFlags.Reference;
		}
		TypeKind typeKind;
		if (type == typeof(object))
		{
			typeKind = TypeKind.Root;
			flags |= TypeFlags.HasDefaultConstructor;
		}
		else if (type == typeof(ValueType))
		{
			typeKind = TypeKind.Enum;
			flags |= TypeFlags.Unsupported;
			if (exception == null)
			{
				exception = new NotSupportedException(System.SR.Format(System.SR.XmlSerializerUnsupportedType, type.FullName));
			}
		}
		else if (type == typeof(void))
		{
			typeKind = TypeKind.Void;
		}
		else if (typeof(IXmlSerializable).IsAssignableFrom(type))
		{
			typeKind = TypeKind.Serializable;
			flags |= (TypeFlags)36;
			flags |= GetConstructorFlags(type, ref exception);
		}
		else if (type.IsArray)
		{
			typeKind = TypeKind.Array;
			if (type.GetArrayRank() > 1)
			{
				flags |= TypeFlags.Unsupported;
				if (exception == null)
				{
					exception = new NotSupportedException(System.SR.Format(System.SR.XmlUnsupportedRank, type.FullName));
				}
			}
			type2 = type.GetElementType();
			flags |= TypeFlags.HasDefaultConstructor;
		}
		else if (typeof(ICollection).IsAssignableFrom(type) && !IsArraySegment(type))
		{
			typeKind = TypeKind.Collection;
			type2 = GetCollectionElementType(type, (memberInfo == null) ? null : (memberInfo.DeclaringType.FullName + "." + memberInfo.Name));
			flags |= GetConstructorFlags(type, ref exception);
		}
		else if (type == typeof(XmlQualifiedName))
		{
			typeKind = TypeKind.Primitive;
		}
		else if (type.IsPrimitive)
		{
			typeKind = TypeKind.Primitive;
			flags |= TypeFlags.Unsupported;
			if (exception == null)
			{
				exception = new NotSupportedException(System.SR.Format(System.SR.XmlSerializerUnsupportedType, type.FullName));
			}
		}
		else if (type.IsEnum)
		{
			typeKind = TypeKind.Enum;
		}
		else if (type.IsValueType)
		{
			typeKind = TypeKind.Struct;
			if (IsOptionalValue(type))
			{
				type3 = type.GetGenericArguments()[0];
				flags |= TypeFlags.OptionalValue;
			}
			else
			{
				type3 = type.BaseType;
			}
			if (type.IsAbstract)
			{
				flags |= TypeFlags.Abstract;
			}
		}
		else if (type.IsClass)
		{
			if (type == typeof(XmlAttribute))
			{
				typeKind = TypeKind.Attribute;
				flags |= (TypeFlags)12;
			}
			else if (typeof(XmlNode).IsAssignableFrom(type))
			{
				typeKind = TypeKind.Node;
				type3 = type.BaseType;
				flags |= (TypeFlags)52;
				if (typeof(XmlText).IsAssignableFrom(type))
				{
					flags &= (TypeFlags)(-33);
				}
				else if (typeof(XmlElement).IsAssignableFrom(type))
				{
					flags &= (TypeFlags)(-17);
				}
				else if (type.IsAssignableFrom(typeof(XmlAttribute)))
				{
					flags |= TypeFlags.CanBeAttributeValue;
				}
			}
			else
			{
				typeKind = TypeKind.Class;
				type3 = type.BaseType;
				if (type.IsAbstract)
				{
					flags |= TypeFlags.Abstract;
				}
			}
		}
		else if (type.IsInterface)
		{
			typeKind = TypeKind.Void;
			flags |= TypeFlags.Unsupported;
			if (exception == null)
			{
				exception = ((!(memberInfo == null)) ? new NotSupportedException(System.SR.Format(System.SR.XmlUnsupportedInterfaceDetails, memberInfo.DeclaringType.FullName + "." + memberInfo.Name, type.FullName)) : new NotSupportedException(System.SR.Format(System.SR.XmlUnsupportedInterface, type.FullName)));
			}
		}
		else
		{
			typeKind = TypeKind.Void;
			flags |= TypeFlags.Unsupported;
			if (exception == null)
			{
				exception = new NotSupportedException(System.SR.Format(System.SR.XmlSerializerUnsupportedType, type.FullName));
			}
		}
		if (typeKind == TypeKind.Class && !type.IsAbstract)
		{
			flags |= GetConstructorFlags(type, ref exception);
		}
		if ((typeKind == TypeKind.Struct || typeKind == TypeKind.Class) && typeof(IEnumerable).IsAssignableFrom(type) && !IsArraySegment(type))
		{
			type2 = GetEnumeratorElementType(type, ref flags);
			typeKind = TypeKind.Enumerable;
			flags |= GetConstructorFlags(type, ref exception);
		}
		typeDesc = new TypeDesc(type, CodeIdentifier.MakeValid(TypeName(type)), type.ToString(), typeKind, null, flags, null);
		typeDesc.Exception = exception;
		if (directReference && (typeDesc.IsClass || typeKind == TypeKind.Serializable))
		{
			typeDesc.CheckNeedConstructor();
		}
		if (typeDesc.IsUnsupported)
		{
			return typeDesc;
		}
		_typeDescs.Add(type, typeDesc);
		if (type2 != null)
		{
			TypeDesc typeDesc2 = GetTypeDesc(type2, memberInfo, directReference: true, throwOnError: false);
			if (directReference && (typeDesc2.IsCollection || typeDesc2.IsEnumerable) && !typeDesc2.IsPrimitive)
			{
				typeDesc2.CheckNeedConstructor();
			}
			typeDesc.ArrayElementTypeDesc = typeDesc2;
		}
		if (type3 != null && type3 != typeof(object) && type3 != typeof(ValueType))
		{
			typeDesc.BaseTypeDesc = GetTypeDesc(type3, memberInfo, directReference: false, throwOnError: false);
		}
		if (type.IsNestedPublic)
		{
			Type declaringType = type.DeclaringType;
			while (declaringType != null && !declaringType.ContainsGenericParameters && (!declaringType.IsAbstract || !declaringType.IsSealed))
			{
				GetTypeDesc(declaringType, null, directReference: false);
				declaringType = declaringType.DeclaringType;
			}
		}
		return typeDesc;
	}

	private static bool IsArraySegment(Type t)
	{
		if (t.IsGenericType)
		{
			return t.GetGenericTypeDefinition() == typeof(ArraySegment<>);
		}
		return false;
	}

	internal static bool IsOptionalValue(Type type)
	{
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>).GetGenericTypeDefinition())
		{
			return true;
		}
		return false;
	}

	internal static string TypeName(Type t)
	{
		if (t.IsArray)
		{
			return "ArrayOf" + TypeName(t.GetElementType());
		}
		if (t.IsGenericType)
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			string text = t.Name;
			int num = text.IndexOf('`');
			if (num >= 0)
			{
				text = text.Substring(0, num);
			}
			stringBuilder.Append(text);
			stringBuilder.Append("Of");
			Type[] genericArguments = t.GetGenericArguments();
			for (int i = 0; i < genericArguments.Length; i++)
			{
				stringBuilder.Append(TypeName(genericArguments[i]));
				stringBuilder2.Append(genericArguments[i].Namespace);
			}
			return stringBuilder.ToString();
		}
		return t.Name;
	}

	[RequiresUnreferencedCode("calls GetEnumeratorElementType")]
	internal static Type GetArrayElementType(Type type, string memberInfo)
	{
		if (type.IsArray)
		{
			return type.GetElementType();
		}
		if (IsArraySegment(type))
		{
			return null;
		}
		if (typeof(ICollection).IsAssignableFrom(type))
		{
			return GetCollectionElementType(type, memberInfo);
		}
		if (typeof(IEnumerable).IsAssignableFrom(type))
		{
			TypeFlags flags = TypeFlags.None;
			return GetEnumeratorElementType(type, ref flags);
		}
		return null;
	}

	internal static MemberMapping[] GetAllMembers(StructMapping mapping)
	{
		if (mapping.BaseMapping == null)
		{
			return mapping.Members;
		}
		List<MemberMapping> list = new List<MemberMapping>();
		GetAllMembers(mapping, list);
		return list.ToArray();
	}

	internal static void GetAllMembers(StructMapping mapping, List<MemberMapping> list)
	{
		if (mapping.BaseMapping != null)
		{
			GetAllMembers(mapping.BaseMapping, list);
		}
		for (int i = 0; i < mapping.Members.Length; i++)
		{
			list.Add(mapping.Members[i]);
		}
	}

	internal static MemberMapping[] GetAllMembers(StructMapping mapping, Dictionary<string, MemberInfo> memberInfos)
	{
		MemberMapping[] allMembers = GetAllMembers(mapping);
		PopulateMemberInfos(mapping, allMembers, memberInfos);
		return allMembers;
	}

	internal static MemberMapping[] GetSettableMembers(StructMapping structMapping)
	{
		List<MemberMapping> list = new List<MemberMapping>();
		GetSettableMembers(structMapping, list);
		return list.ToArray();
	}

	private static void GetSettableMembers(StructMapping mapping, List<MemberMapping> list)
	{
		if (mapping.BaseMapping != null)
		{
			GetSettableMembers(mapping.BaseMapping, list);
		}
		if (mapping.Members == null)
		{
			return;
		}
		MemberMapping[] members = mapping.Members;
		foreach (MemberMapping memberMapping in members)
		{
			MemberInfo memberInfo = memberMapping.MemberInfo;
			PropertyInfo propertyInfo = memberInfo as PropertyInfo;
			if (propertyInfo != null && !CanWriteProperty(propertyInfo, memberMapping.TypeDesc))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlReadOnlyPropertyError, propertyInfo.DeclaringType, propertyInfo.Name));
			}
			list.Add(memberMapping);
		}
	}

	private static bool CanWriteProperty(PropertyInfo propertyInfo, TypeDesc typeDesc)
	{
		if (typeDesc.Kind == TypeKind.Collection || typeDesc.Kind == TypeKind.Enumerable)
		{
			return true;
		}
		if (propertyInfo.SetMethod != null)
		{
			return propertyInfo.SetMethod.IsPublic;
		}
		return false;
	}

	internal static MemberMapping[] GetSettableMembers(StructMapping mapping, Dictionary<string, MemberInfo> memberInfos)
	{
		MemberMapping[] settableMembers = GetSettableMembers(mapping);
		PopulateMemberInfos(mapping, settableMembers, memberInfos);
		return settableMembers;
	}

	private static void PopulateMemberInfos(StructMapping structMapping, MemberMapping[] mappings, Dictionary<string, MemberInfo> memberInfos)
	{
		memberInfos.Clear();
		for (int i = 0; i < mappings.Length; i++)
		{
			memberInfos[mappings[i].Name] = mappings[i].MemberInfo;
			if (mappings[i].ChoiceIdentifier != null)
			{
				memberInfos[mappings[i].ChoiceIdentifier.MemberName] = mappings[i].ChoiceIdentifier.MemberInfo;
			}
			if (mappings[i].CheckSpecifiedMemberInfo != null)
			{
				memberInfos[mappings[i].Name + "Specified"] = mappings[i].CheckSpecifiedMemberInfo;
			}
		}
		Dictionary<string, MemberInfo> dictionary = null;
		MemberInfo replacedInfo = null;
		foreach (KeyValuePair<string, MemberInfo> memberInfo in memberInfos)
		{
			if (ShouldBeReplaced(memberInfo.Value, structMapping.TypeDesc.Type, out replacedInfo))
			{
				if (dictionary == null)
				{
					dictionary = new Dictionary<string, MemberInfo>();
				}
				dictionary.Add(memberInfo.Key, replacedInfo);
			}
		}
		if (dictionary == null)
		{
			return;
		}
		foreach (KeyValuePair<string, MemberInfo> item in dictionary)
		{
			memberInfos[item.Key] = item.Value;
		}
		for (int j = 0; j < mappings.Length; j++)
		{
			if (dictionary.TryGetValue(mappings[j].Name, out var value))
			{
				MemberMapping memberMapping = mappings[j].Clone();
				memberMapping.MemberInfo = value;
				mappings[j] = memberMapping;
			}
		}
	}

	private static bool ShouldBeReplaced(MemberInfo memberInfoToBeReplaced, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type derivedType, out MemberInfo replacedInfo)
	{
		replacedInfo = memberInfoToBeReplaced;
		Type type = derivedType;
		Type declaringType = memberInfoToBeReplaced.DeclaringType;
		if (declaringType.IsAssignableFrom(type))
		{
			while (type != declaringType)
			{
				PropertyInfo[] properties = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (PropertyInfo propertyInfo in properties)
				{
					if (!(propertyInfo.Name == memberInfoToBeReplaced.Name))
					{
						continue;
					}
					replacedInfo = propertyInfo;
					if (replacedInfo != memberInfoToBeReplaced)
					{
						if (propertyInfo.GetMethod != null && !propertyInfo.GetMethod.IsPublic && memberInfoToBeReplaced is PropertyInfo && ((PropertyInfo)memberInfoToBeReplaced).GetMethod.IsPublic)
						{
							break;
						}
						return true;
					}
				}
				FieldInfo[] fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (FieldInfo fieldInfo in fields)
				{
					if (fieldInfo.Name == memberInfoToBeReplaced.Name)
					{
						replacedInfo = fieldInfo;
						if (replacedInfo != memberInfoToBeReplaced)
						{
							return true;
						}
					}
				}
				type = type.BaseType;
			}
		}
		return false;
	}

	private static TypeFlags GetConstructorFlags([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type, ref Exception exception)
	{
		ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		if (constructor != null)
		{
			TypeFlags typeFlags = TypeFlags.HasDefaultConstructor;
			if (!constructor.IsPublic)
			{
				typeFlags |= TypeFlags.CtorInaccessible;
			}
			else
			{
				object[] customAttributes = constructor.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false);
				if (customAttributes != null && customAttributes.Length != 0)
				{
					ObsoleteAttribute obsoleteAttribute = (ObsoleteAttribute)customAttributes[0];
					if (obsoleteAttribute.IsError)
					{
						typeFlags |= TypeFlags.CtorInaccessible;
					}
				}
			}
			return typeFlags;
		}
		return TypeFlags.None;
	}

	[RequiresUnreferencedCode("Needs to mark members on the return type of the GetEnumerator method")]
	private static Type GetEnumeratorElementType(Type type, ref TypeFlags flags)
	{
		if (typeof(IEnumerable).IsAssignableFrom(type))
		{
			MethodInfo methodInfo = type.GetMethod("GetEnumerator", Type.EmptyTypes);
			if (methodInfo == null || !typeof(IEnumerator).IsAssignableFrom(methodInfo.ReturnType))
			{
				methodInfo = null;
				MemberInfo[] member = type.GetMember("System.Collections.Generic.IEnumerable<*", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (MemberInfo memberInfo in member)
				{
					methodInfo = memberInfo as MethodInfo;
					if (methodInfo != null && typeof(IEnumerator).IsAssignableFrom(methodInfo.ReturnType))
					{
						flags |= TypeFlags.GenericInterface;
						break;
					}
					methodInfo = null;
				}
				if (methodInfo == null)
				{
					flags |= TypeFlags.UsePrivateImplementation;
					methodInfo = type.GetMethod("System.Collections.IEnumerable.GetEnumerator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				}
			}
			if (methodInfo == null || !typeof(IEnumerator).IsAssignableFrom(methodInfo.ReturnType))
			{
				return null;
			}
			XmlAttributes xmlAttributes = new XmlAttributes(methodInfo);
			if (xmlAttributes.XmlIgnore)
			{
				return null;
			}
			PropertyInfo property = methodInfo.ReturnType.GetProperty("Current");
			Type type2 = ((property == null) ? typeof(object) : property.PropertyType);
			MethodInfo method = type.GetMethod("Add", new Type[1] { type2 });
			if (method == null && type2 != typeof(object))
			{
				type2 = typeof(object);
				method = type.GetMethod("Add", new Type[1] { type2 });
			}
			if (method == null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlNoAddMethod, type.FullName, type2, "IEnumerable"));
			}
			return type2;
		}
		return null;
	}

	internal static PropertyInfo GetDefaultIndexer([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents)] Type type, string memberInfo)
	{
		if (typeof(IDictionary).IsAssignableFrom(type))
		{
			if (memberInfo == null)
			{
				throw new NotSupportedException(System.SR.Format(System.SR.XmlUnsupportedIDictionary, type.FullName));
			}
			throw new NotSupportedException(System.SR.Format(System.SR.XmlUnsupportedIDictionaryDetails, memberInfo, type.FullName));
		}
		MemberInfo[] defaultMembers = type.GetDefaultMembers();
		PropertyInfo propertyInfo = null;
		if (defaultMembers != null && defaultMembers.Length != 0)
		{
			Type type2 = type;
			while (type2 != null)
			{
				for (int i = 0; i < defaultMembers.Length; i++)
				{
					if (!(defaultMembers[i] is PropertyInfo))
					{
						continue;
					}
					PropertyInfo propertyInfo2 = (PropertyInfo)defaultMembers[i];
					if (!(propertyInfo2.DeclaringType != type2) && propertyInfo2.CanRead)
					{
						MethodInfo getMethod = propertyInfo2.GetMethod;
						ParameterInfo[] parameters = getMethod.GetParameters();
						if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
						{
							propertyInfo = propertyInfo2;
							break;
						}
					}
				}
				if (propertyInfo != null)
				{
					break;
				}
				type2 = type2.BaseType;
			}
		}
		if (propertyInfo == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlNoDefaultAccessors, type.FullName));
		}
		MethodInfo method = type.GetMethod("Add", new Type[1] { propertyInfo.PropertyType });
		if (method == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlNoAddMethod, type.FullName, propertyInfo.PropertyType, "ICollection"));
		}
		return propertyInfo;
	}

	private static Type GetCollectionElementType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents)] Type type, string memberInfo)
	{
		return GetDefaultIndexer(type, memberInfo).PropertyType;
	}

	internal static XmlQualifiedName ParseWsdlArrayType(string type, out string dims, XmlSchemaObject parent)
	{
		int num = type.LastIndexOf(':');
		string text = ((num > 0) ? type.Substring(0, num) : "");
		int num2 = type.IndexOf('[', num + 1);
		if (num2 <= num)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidArrayTypeSyntax, type));
		}
		string name = type.Substring(num + 1, num2 - num - 1);
		dims = type.Substring(num2);
		while (parent != null)
		{
			if (parent.Namespaces != null && parent.Namespaces.TryLookupNamespace(text, out var ns) && ns != null)
			{
				text = ns;
				break;
			}
			parent = parent.Parent;
		}
		return new XmlQualifiedName(name, text);
	}

	internal void AddTypeMapping(TypeMapping typeMapping)
	{
		_typeMappings.Add(typeMapping);
	}
}
