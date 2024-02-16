using System.Collections;

namespace System.Xml.Schema;

internal abstract class DatatypeImplementation : XmlSchemaDatatype
{
	private sealed class SchemaDatatypeMap : IComparable
	{
		private readonly string _name;

		private readonly DatatypeImplementation _type;

		private readonly int _parentIndex;

		public string Name => _name;

		public int ParentIndex => _parentIndex;

		internal SchemaDatatypeMap(string name, DatatypeImplementation type)
		{
			_name = name;
			_type = type;
		}

		internal SchemaDatatypeMap(string name, DatatypeImplementation type, int parentIndex)
		{
			_name = name;
			_type = type;
			_parentIndex = parentIndex;
		}

		public static explicit operator DatatypeImplementation(SchemaDatatypeMap sdm)
		{
			return sdm._type;
		}

		public int CompareTo(object obj)
		{
			return string.Compare(_name, (string)obj, StringComparison.Ordinal);
		}
	}

	private XmlSchemaDatatypeVariety _variety;

	private RestrictionFacets _restriction;

	private DatatypeImplementation _baseType;

	private XmlValueConverter _valueConverter;

	private XmlSchemaType _parentSchemaType;

	private static readonly Hashtable s_builtinTypes;

	private static readonly XmlSchemaSimpleType[] s_enumToTypeCode;

	private static XmlSchemaSimpleType s__anySimpleType;

	private static XmlSchemaSimpleType s__anyAtomicType;

	private static XmlSchemaSimpleType s__untypedAtomicType;

	private static XmlSchemaSimpleType s_yearMonthDurationType;

	private static XmlSchemaSimpleType s_dayTimeDurationType;

	private static volatile XmlSchemaSimpleType s_normalizedStringTypeV1Compat;

	private static volatile XmlSchemaSimpleType s_tokenTypeV1Compat;

	internal static XmlQualifiedName QnAnySimpleType;

	internal static XmlQualifiedName QnAnyType;

	internal static FacetsChecker stringFacetsChecker;

	internal static FacetsChecker miscFacetsChecker;

	internal static FacetsChecker numeric2FacetsChecker;

	internal static FacetsChecker binaryFacetsChecker;

	internal static FacetsChecker dateTimeFacetsChecker;

	internal static FacetsChecker durationFacetsChecker;

	internal static FacetsChecker listFacetsChecker;

	internal static FacetsChecker qnameFacetsChecker;

	internal static FacetsChecker unionFacetsChecker;

	private static readonly DatatypeImplementation s_anySimpleType;

	private static readonly DatatypeImplementation s_anyURI;

	private static readonly DatatypeImplementation s_base64Binary;

	private static readonly DatatypeImplementation s_boolean;

	private static readonly DatatypeImplementation s_byte;

	private static readonly DatatypeImplementation s_char;

	private static readonly DatatypeImplementation s_date;

	private static readonly DatatypeImplementation s_dateTime;

	private static readonly DatatypeImplementation s_dateTimeNoTz;

	private static readonly DatatypeImplementation s_dateTimeTz;

	private static readonly DatatypeImplementation s_day;

	private static readonly DatatypeImplementation s_decimal;

	private static readonly DatatypeImplementation s_double;

	private static readonly DatatypeImplementation s_doubleXdr;

	private static readonly DatatypeImplementation s_duration;

	private static readonly DatatypeImplementation s_ENTITY;

	private static readonly DatatypeImplementation s_ENTITIES;

	private static readonly DatatypeImplementation s_ENUMERATION;

	private static readonly DatatypeImplementation s_fixed;

	private static readonly DatatypeImplementation s_float;

	private static readonly DatatypeImplementation s_floatXdr;

	private static readonly DatatypeImplementation s_hexBinary;

	private static readonly DatatypeImplementation s_ID;

	private static readonly DatatypeImplementation s_IDREF;

	private static readonly DatatypeImplementation s_IDREFS;

	private static readonly DatatypeImplementation s_int;

	private static readonly DatatypeImplementation s_integer;

	private static readonly DatatypeImplementation s_language;

	private static readonly DatatypeImplementation s_long;

	private static readonly DatatypeImplementation s_month;

	private static readonly DatatypeImplementation s_monthDay;

	private static readonly DatatypeImplementation s_name;

	private static readonly DatatypeImplementation s_NCName;

	private static readonly DatatypeImplementation s_negativeInteger;

	private static readonly DatatypeImplementation s_NMTOKEN;

	private static readonly DatatypeImplementation s_NMTOKENS;

	private static readonly DatatypeImplementation s_nonNegativeInteger;

	private static readonly DatatypeImplementation s_nonPositiveInteger;

	private static readonly DatatypeImplementation s_normalizedString;

	private static readonly DatatypeImplementation s_NOTATION;

	private static readonly DatatypeImplementation s_positiveInteger;

	private static readonly DatatypeImplementation s_QName;

	private static readonly DatatypeImplementation s_QNameXdr;

	private static readonly DatatypeImplementation s_short;

	private static readonly DatatypeImplementation s_string;

	private static readonly DatatypeImplementation s_time;

	private static readonly DatatypeImplementation s_timeNoTz;

	private static readonly DatatypeImplementation s_timeTz;

	private static readonly DatatypeImplementation s_token;

	private static readonly DatatypeImplementation s_unsignedByte;

	private static readonly DatatypeImplementation s_unsignedInt;

	private static readonly DatatypeImplementation s_unsignedLong;

	private static readonly DatatypeImplementation s_unsignedShort;

	private static readonly DatatypeImplementation s_uuid;

	private static readonly DatatypeImplementation s_year;

	private static readonly DatatypeImplementation s_yearMonth;

	internal static readonly DatatypeImplementation c_normalizedStringV1Compat;

	internal static readonly DatatypeImplementation c_tokenV1Compat;

	private static readonly DatatypeImplementation s_anyAtomicType;

	private static readonly DatatypeImplementation s_dayTimeDuration;

	private static readonly DatatypeImplementation s_untypedAtomicType;

	private static readonly DatatypeImplementation s_yearMonthDuration;

	private static readonly DatatypeImplementation[] s_tokenizedTypes;

	private static readonly DatatypeImplementation[] s_tokenizedTypesXsd;

	private static readonly SchemaDatatypeMap[] s_xdrTypes;

	private static readonly SchemaDatatypeMap[] s_xsdTypes;

	internal static XmlSchemaSimpleType AnySimpleType => s__anySimpleType;

	internal static XmlSchemaSimpleType AnyAtomicType => s__anyAtomicType;

	internal static XmlSchemaSimpleType UntypedAtomicType => s__untypedAtomicType;

	internal override FacetsChecker FacetsChecker => miscFacetsChecker;

	internal override XmlValueConverter ValueConverter
	{
		get
		{
			if (_valueConverter == null)
			{
				_valueConverter = CreateValueConverter(_parentSchemaType);
			}
			return _valueConverter;
		}
	}

	public override XmlTokenizedType TokenizedType => XmlTokenizedType.None;

	public override Type ValueType => typeof(string);

	public override XmlSchemaDatatypeVariety Variety => _variety;

	public override XmlTypeCode TypeCode => XmlTypeCode.None;

	internal override RestrictionFacets Restriction => _restriction;

	internal override bool HasLexicalFacets
	{
		get
		{
			RestrictionFlags restrictionFlags = ((_restriction != null) ? _restriction.Flags : ((RestrictionFlags)0));
			if (restrictionFlags != 0 && (restrictionFlags & (RestrictionFlags.Pattern | RestrictionFlags.WhiteSpace | RestrictionFlags.TotalDigits | RestrictionFlags.FractionDigits)) != 0)
			{
				return true;
			}
			return false;
		}
	}

	internal override bool HasValueFacets
	{
		get
		{
			RestrictionFlags restrictionFlags = ((_restriction != null) ? _restriction.Flags : ((RestrictionFlags)0));
			if (restrictionFlags != 0 && (restrictionFlags & (RestrictionFlags.Length | RestrictionFlags.MinLength | RestrictionFlags.MaxLength | RestrictionFlags.Enumeration | RestrictionFlags.MaxInclusive | RestrictionFlags.MaxExclusive | RestrictionFlags.MinInclusive | RestrictionFlags.MinExclusive | RestrictionFlags.TotalDigits | RestrictionFlags.FractionDigits)) != 0)
			{
				return true;
			}
			return false;
		}
	}

	protected DatatypeImplementation Base => _baseType;

	internal abstract Type ListValueType { get; }

	internal abstract RestrictionFlags ValidRestrictionFlags { get; }

	internal override XmlSchemaWhiteSpace BuiltInWhitespaceFacet => XmlSchemaWhiteSpace.Preserve;

	static DatatypeImplementation()
	{
		s_builtinTypes = new Hashtable();
		s_enumToTypeCode = new XmlSchemaSimpleType[55];
		s__anySimpleType = null;
		s__anyAtomicType = null;
		s__untypedAtomicType = null;
		s_yearMonthDurationType = null;
		s_dayTimeDurationType = null;
		QnAnySimpleType = new XmlQualifiedName("anySimpleType", "http://www.w3.org/2001/XMLSchema");
		QnAnyType = new XmlQualifiedName("anyType", "http://www.w3.org/2001/XMLSchema");
		stringFacetsChecker = new StringFacetsChecker();
		miscFacetsChecker = new MiscFacetsChecker();
		numeric2FacetsChecker = new Numeric2FacetsChecker();
		binaryFacetsChecker = new BinaryFacetsChecker();
		dateTimeFacetsChecker = new DateTimeFacetsChecker();
		durationFacetsChecker = new DurationFacetsChecker();
		listFacetsChecker = new ListFacetsChecker();
		qnameFacetsChecker = new QNameFacetsChecker();
		unionFacetsChecker = new UnionFacetsChecker();
		s_anySimpleType = new Datatype_anySimpleType();
		s_anyURI = new Datatype_anyURI();
		s_base64Binary = new Datatype_base64Binary();
		s_boolean = new Datatype_boolean();
		s_byte = new Datatype_byte();
		s_char = new Datatype_char();
		s_date = new Datatype_date();
		s_dateTime = new Datatype_dateTime();
		s_dateTimeNoTz = new Datatype_dateTimeNoTimeZone();
		s_dateTimeTz = new Datatype_dateTimeTimeZone();
		s_day = new Datatype_day();
		s_decimal = new Datatype_decimal();
		s_double = new Datatype_double();
		s_doubleXdr = new Datatype_doubleXdr();
		s_duration = new Datatype_duration();
		s_ENTITY = new Datatype_ENTITY();
		s_ENTITIES = (DatatypeImplementation)s_ENTITY.DeriveByList(1, null);
		s_ENUMERATION = new Datatype_ENUMERATION();
		s_fixed = new Datatype_fixed();
		s_float = new Datatype_float();
		s_floatXdr = new Datatype_floatXdr();
		s_hexBinary = new Datatype_hexBinary();
		s_ID = new Datatype_ID();
		s_IDREF = new Datatype_IDREF();
		s_IDREFS = (DatatypeImplementation)s_IDREF.DeriveByList(1, null);
		s_int = new Datatype_int();
		s_integer = new Datatype_integer();
		s_language = new Datatype_language();
		s_long = new Datatype_long();
		s_month = new Datatype_month();
		s_monthDay = new Datatype_monthDay();
		s_name = new Datatype_Name();
		s_NCName = new Datatype_NCName();
		s_negativeInteger = new Datatype_negativeInteger();
		s_NMTOKEN = new Datatype_NMTOKEN();
		s_NMTOKENS = (DatatypeImplementation)s_NMTOKEN.DeriveByList(1, null);
		s_nonNegativeInteger = new Datatype_nonNegativeInteger();
		s_nonPositiveInteger = new Datatype_nonPositiveInteger();
		s_normalizedString = new Datatype_normalizedString();
		s_NOTATION = new Datatype_NOTATION();
		s_positiveInteger = new Datatype_positiveInteger();
		s_QName = new Datatype_QName();
		s_QNameXdr = new Datatype_QNameXdr();
		s_short = new Datatype_short();
		s_string = new Datatype_string();
		s_time = new Datatype_time();
		s_timeNoTz = new Datatype_timeNoTimeZone();
		s_timeTz = new Datatype_timeTimeZone();
		s_token = new Datatype_token();
		s_unsignedByte = new Datatype_unsignedByte();
		s_unsignedInt = new Datatype_unsignedInt();
		s_unsignedLong = new Datatype_unsignedLong();
		s_unsignedShort = new Datatype_unsignedShort();
		s_uuid = new Datatype_uuid();
		s_year = new Datatype_year();
		s_yearMonth = new Datatype_yearMonth();
		c_normalizedStringV1Compat = new Datatype_normalizedStringV1Compat();
		c_tokenV1Compat = new Datatype_tokenV1Compat();
		s_anyAtomicType = new Datatype_anyAtomicType();
		s_dayTimeDuration = new Datatype_dayTimeDuration();
		s_untypedAtomicType = new Datatype_untypedAtomicType();
		s_yearMonthDuration = new Datatype_yearMonthDuration();
		s_tokenizedTypes = new DatatypeImplementation[13]
		{
			s_string, s_ID, s_IDREF, s_IDREFS, s_ENTITY, s_ENTITIES, s_NMTOKEN, s_NMTOKENS, s_NOTATION, s_ENUMERATION,
			s_QNameXdr, s_NCName, null
		};
		s_tokenizedTypesXsd = new DatatypeImplementation[13]
		{
			s_string, s_ID, s_IDREF, s_IDREFS, s_ENTITY, s_ENTITIES, s_NMTOKEN, s_NMTOKENS, s_NOTATION, s_ENUMERATION,
			s_QName, s_NCName, null
		};
		s_xdrTypes = new SchemaDatatypeMap[38]
		{
			new SchemaDatatypeMap("bin.base64", s_base64Binary),
			new SchemaDatatypeMap("bin.hex", s_hexBinary),
			new SchemaDatatypeMap("boolean", s_boolean),
			new SchemaDatatypeMap("char", s_char),
			new SchemaDatatypeMap("date", s_date),
			new SchemaDatatypeMap("dateTime", s_dateTimeNoTz),
			new SchemaDatatypeMap("dateTime.tz", s_dateTimeTz),
			new SchemaDatatypeMap("decimal", s_decimal),
			new SchemaDatatypeMap("entities", s_ENTITIES),
			new SchemaDatatypeMap("entity", s_ENTITY),
			new SchemaDatatypeMap("enumeration", s_ENUMERATION),
			new SchemaDatatypeMap("fixed.14.4", s_fixed),
			new SchemaDatatypeMap("float", s_doubleXdr),
			new SchemaDatatypeMap("float.ieee.754.32", s_floatXdr),
			new SchemaDatatypeMap("float.ieee.754.64", s_doubleXdr),
			new SchemaDatatypeMap("i1", s_byte),
			new SchemaDatatypeMap("i2", s_short),
			new SchemaDatatypeMap("i4", s_int),
			new SchemaDatatypeMap("i8", s_long),
			new SchemaDatatypeMap("id", s_ID),
			new SchemaDatatypeMap("idref", s_IDREF),
			new SchemaDatatypeMap("idrefs", s_IDREFS),
			new SchemaDatatypeMap("int", s_int),
			new SchemaDatatypeMap("nmtoken", s_NMTOKEN),
			new SchemaDatatypeMap("nmtokens", s_NMTOKENS),
			new SchemaDatatypeMap("notation", s_NOTATION),
			new SchemaDatatypeMap("number", s_doubleXdr),
			new SchemaDatatypeMap("r4", s_floatXdr),
			new SchemaDatatypeMap("r8", s_doubleXdr),
			new SchemaDatatypeMap("string", s_string),
			new SchemaDatatypeMap("time", s_timeNoTz),
			new SchemaDatatypeMap("time.tz", s_timeTz),
			new SchemaDatatypeMap("ui1", s_unsignedByte),
			new SchemaDatatypeMap("ui2", s_unsignedShort),
			new SchemaDatatypeMap("ui4", s_unsignedInt),
			new SchemaDatatypeMap("ui8", s_unsignedLong),
			new SchemaDatatypeMap("uri", s_anyURI),
			new SchemaDatatypeMap("uuid", s_uuid)
		};
		s_xsdTypes = new SchemaDatatypeMap[45]
		{
			new SchemaDatatypeMap("ENTITIES", s_ENTITIES, 11),
			new SchemaDatatypeMap("ENTITY", s_ENTITY, 11),
			new SchemaDatatypeMap("ID", s_ID, 5),
			new SchemaDatatypeMap("IDREF", s_IDREF, 5),
			new SchemaDatatypeMap("IDREFS", s_IDREFS, 11),
			new SchemaDatatypeMap("NCName", s_NCName, 9),
			new SchemaDatatypeMap("NMTOKEN", s_NMTOKEN, 40),
			new SchemaDatatypeMap("NMTOKENS", s_NMTOKENS, 11),
			new SchemaDatatypeMap("NOTATION", s_NOTATION, 11),
			new SchemaDatatypeMap("Name", s_name, 40),
			new SchemaDatatypeMap("QName", s_QName, 11),
			new SchemaDatatypeMap("anySimpleType", s_anySimpleType, -1),
			new SchemaDatatypeMap("anyURI", s_anyURI, 11),
			new SchemaDatatypeMap("base64Binary", s_base64Binary, 11),
			new SchemaDatatypeMap("boolean", s_boolean, 11),
			new SchemaDatatypeMap("byte", s_byte, 37),
			new SchemaDatatypeMap("date", s_date, 11),
			new SchemaDatatypeMap("dateTime", s_dateTime, 11),
			new SchemaDatatypeMap("decimal", s_decimal, 11),
			new SchemaDatatypeMap("double", s_double, 11),
			new SchemaDatatypeMap("duration", s_duration, 11),
			new SchemaDatatypeMap("float", s_float, 11),
			new SchemaDatatypeMap("gDay", s_day, 11),
			new SchemaDatatypeMap("gMonth", s_month, 11),
			new SchemaDatatypeMap("gMonthDay", s_monthDay, 11),
			new SchemaDatatypeMap("gYear", s_year, 11),
			new SchemaDatatypeMap("gYearMonth", s_yearMonth, 11),
			new SchemaDatatypeMap("hexBinary", s_hexBinary, 11),
			new SchemaDatatypeMap("int", s_int, 31),
			new SchemaDatatypeMap("integer", s_integer, 18),
			new SchemaDatatypeMap("language", s_language, 40),
			new SchemaDatatypeMap("long", s_long, 29),
			new SchemaDatatypeMap("negativeInteger", s_negativeInteger, 34),
			new SchemaDatatypeMap("nonNegativeInteger", s_nonNegativeInteger, 29),
			new SchemaDatatypeMap("nonPositiveInteger", s_nonPositiveInteger, 29),
			new SchemaDatatypeMap("normalizedString", s_normalizedString, 38),
			new SchemaDatatypeMap("positiveInteger", s_positiveInteger, 33),
			new SchemaDatatypeMap("short", s_short, 28),
			new SchemaDatatypeMap("string", s_string, 11),
			new SchemaDatatypeMap("time", s_time, 11),
			new SchemaDatatypeMap("token", s_token, 35),
			new SchemaDatatypeMap("unsignedByte", s_unsignedByte, 44),
			new SchemaDatatypeMap("unsignedInt", s_unsignedInt, 43),
			new SchemaDatatypeMap("unsignedLong", s_unsignedLong, 33),
			new SchemaDatatypeMap("unsignedShort", s_unsignedShort, 42)
		};
		CreateBuiltinTypes();
	}

	internal new static DatatypeImplementation FromXmlTokenizedType(XmlTokenizedType token)
	{
		return s_tokenizedTypes[(int)token];
	}

	internal new static DatatypeImplementation FromXmlTokenizedTypeXsd(XmlTokenizedType token)
	{
		return s_tokenizedTypesXsd[(int)token];
	}

	internal new static DatatypeImplementation FromXdrName(string name)
	{
		int num = Array.BinarySearch(s_xdrTypes, name, null);
		if (num >= 0)
		{
			return (DatatypeImplementation)s_xdrTypes[num];
		}
		return null;
	}

	private static DatatypeImplementation FromTypeName(string name)
	{
		int num = Array.BinarySearch(s_xsdTypes, name, null);
		if (num >= 0)
		{
			return (DatatypeImplementation)s_xsdTypes[num];
		}
		return null;
	}

	internal static XmlSchemaSimpleType StartBuiltinType(XmlQualifiedName qname, XmlSchemaDatatype dataType)
	{
		XmlSchemaSimpleType xmlSchemaSimpleType = new XmlSchemaSimpleType();
		xmlSchemaSimpleType.SetQualifiedName(qname);
		xmlSchemaSimpleType.SetDatatype(dataType);
		xmlSchemaSimpleType.ElementDecl = new SchemaElementDecl(dataType);
		xmlSchemaSimpleType.ElementDecl.SchemaType = xmlSchemaSimpleType;
		return xmlSchemaSimpleType;
	}

	internal static void FinishBuiltinType(XmlSchemaSimpleType derivedType, XmlSchemaSimpleType baseType)
	{
		derivedType.SetBaseSchemaType(baseType);
		derivedType.SetDerivedBy(XmlSchemaDerivationMethod.Restriction);
		if (derivedType.Datatype.Variety == XmlSchemaDatatypeVariety.Atomic)
		{
			XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction = new XmlSchemaSimpleTypeRestriction();
			xmlSchemaSimpleTypeRestriction.BaseTypeName = baseType.QualifiedName;
			derivedType.Content = xmlSchemaSimpleTypeRestriction;
		}
		if (derivedType.Datatype.Variety == XmlSchemaDatatypeVariety.List)
		{
			XmlSchemaSimpleTypeList xmlSchemaSimpleTypeList = new XmlSchemaSimpleTypeList();
			derivedType.SetDerivedBy(XmlSchemaDerivationMethod.List);
			switch (derivedType.Datatype.TypeCode)
			{
			case XmlTypeCode.NmToken:
			{
				XmlSchemaSimpleType itemType = (xmlSchemaSimpleTypeList.BaseItemType = s_enumToTypeCode[34]);
				xmlSchemaSimpleTypeList.ItemType = itemType;
				break;
			}
			case XmlTypeCode.Entity:
			{
				XmlSchemaSimpleType itemType = (xmlSchemaSimpleTypeList.BaseItemType = s_enumToTypeCode[39]);
				xmlSchemaSimpleTypeList.ItemType = itemType;
				break;
			}
			case XmlTypeCode.Idref:
			{
				XmlSchemaSimpleType itemType = (xmlSchemaSimpleTypeList.BaseItemType = s_enumToTypeCode[38]);
				xmlSchemaSimpleTypeList.ItemType = itemType;
				break;
			}
			}
			derivedType.Content = xmlSchemaSimpleTypeList;
		}
	}

	internal static void CreateBuiltinTypes()
	{
		SchemaDatatypeMap schemaDatatypeMap = s_xsdTypes[11];
		XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(schemaDatatypeMap.Name, "http://www.w3.org/2001/XMLSchema");
		DatatypeImplementation datatypeImplementation = FromTypeName(xmlQualifiedName.Name);
		s__anySimpleType = StartBuiltinType(xmlQualifiedName, datatypeImplementation);
		datatypeImplementation._parentSchemaType = s__anySimpleType;
		s_builtinTypes.Add(xmlQualifiedName, s__anySimpleType);
		for (int i = 0; i < s_xsdTypes.Length; i++)
		{
			if (i != 11)
			{
				schemaDatatypeMap = s_xsdTypes[i];
				xmlQualifiedName = new XmlQualifiedName(schemaDatatypeMap.Name, "http://www.w3.org/2001/XMLSchema");
				datatypeImplementation = FromTypeName(xmlQualifiedName.Name);
				XmlSchemaSimpleType xmlSchemaSimpleType = (XmlSchemaSimpleType)(datatypeImplementation._parentSchemaType = StartBuiltinType(xmlQualifiedName, datatypeImplementation));
				s_builtinTypes.Add(xmlQualifiedName, xmlSchemaSimpleType);
				if (datatypeImplementation._variety == XmlSchemaDatatypeVariety.Atomic)
				{
					s_enumToTypeCode[(int)datatypeImplementation.TypeCode] = xmlSchemaSimpleType;
				}
			}
		}
		for (int j = 0; j < s_xsdTypes.Length; j++)
		{
			if (j != 11)
			{
				schemaDatatypeMap = s_xsdTypes[j];
				XmlSchemaSimpleType derivedType = (XmlSchemaSimpleType)s_builtinTypes[new XmlQualifiedName(schemaDatatypeMap.Name, "http://www.w3.org/2001/XMLSchema")];
				if (schemaDatatypeMap.ParentIndex == 11)
				{
					FinishBuiltinType(derivedType, s__anySimpleType);
					continue;
				}
				XmlSchemaSimpleType baseType = (XmlSchemaSimpleType)s_builtinTypes[new XmlQualifiedName(s_xsdTypes[schemaDatatypeMap.ParentIndex].Name, "http://www.w3.org/2001/XMLSchema")];
				FinishBuiltinType(derivedType, baseType);
			}
		}
		xmlQualifiedName = new XmlQualifiedName("anyAtomicType", "http://www.w3.org/2003/11/xpath-datatypes");
		s__anyAtomicType = StartBuiltinType(xmlQualifiedName, s_anyAtomicType);
		s_anyAtomicType._parentSchemaType = s__anyAtomicType;
		FinishBuiltinType(s__anyAtomicType, s__anySimpleType);
		s_builtinTypes.Add(xmlQualifiedName, s__anyAtomicType);
		s_enumToTypeCode[10] = s__anyAtomicType;
		xmlQualifiedName = new XmlQualifiedName("untypedAtomic", "http://www.w3.org/2003/11/xpath-datatypes");
		s__untypedAtomicType = StartBuiltinType(xmlQualifiedName, s_untypedAtomicType);
		s_untypedAtomicType._parentSchemaType = s__untypedAtomicType;
		FinishBuiltinType(s__untypedAtomicType, s__anyAtomicType);
		s_builtinTypes.Add(xmlQualifiedName, s__untypedAtomicType);
		s_enumToTypeCode[11] = s__untypedAtomicType;
		xmlQualifiedName = new XmlQualifiedName("yearMonthDuration", "http://www.w3.org/2003/11/xpath-datatypes");
		s_yearMonthDurationType = StartBuiltinType(xmlQualifiedName, s_yearMonthDuration);
		s_yearMonthDuration._parentSchemaType = s_yearMonthDurationType;
		FinishBuiltinType(s_yearMonthDurationType, s_enumToTypeCode[17]);
		s_builtinTypes.Add(xmlQualifiedName, s_yearMonthDurationType);
		s_enumToTypeCode[53] = s_yearMonthDurationType;
		xmlQualifiedName = new XmlQualifiedName("dayTimeDuration", "http://www.w3.org/2003/11/xpath-datatypes");
		s_dayTimeDurationType = StartBuiltinType(xmlQualifiedName, s_dayTimeDuration);
		s_dayTimeDuration._parentSchemaType = s_dayTimeDurationType;
		FinishBuiltinType(s_dayTimeDurationType, s_enumToTypeCode[17]);
		s_builtinTypes.Add(xmlQualifiedName, s_dayTimeDurationType);
		s_enumToTypeCode[54] = s_dayTimeDurationType;
	}

	internal static XmlSchemaSimpleType GetSimpleTypeFromTypeCode(XmlTypeCode typeCode)
	{
		return s_enumToTypeCode[(int)typeCode];
	}

	internal static XmlSchemaSimpleType GetSimpleTypeFromXsdType(XmlQualifiedName qname)
	{
		return (XmlSchemaSimpleType)s_builtinTypes[qname];
	}

	internal static XmlSchemaSimpleType GetNormalizedStringTypeV1Compat()
	{
		if (s_normalizedStringTypeV1Compat == null)
		{
			XmlSchemaSimpleType simpleTypeFromTypeCode = GetSimpleTypeFromTypeCode(XmlTypeCode.NormalizedString);
			XmlSchemaSimpleType xmlSchemaSimpleType = simpleTypeFromTypeCode.Clone() as XmlSchemaSimpleType;
			xmlSchemaSimpleType.SetDatatype(c_normalizedStringV1Compat);
			xmlSchemaSimpleType.ElementDecl = new SchemaElementDecl(c_normalizedStringV1Compat);
			xmlSchemaSimpleType.ElementDecl.SchemaType = xmlSchemaSimpleType;
			s_normalizedStringTypeV1Compat = xmlSchemaSimpleType;
		}
		return s_normalizedStringTypeV1Compat;
	}

	internal static XmlSchemaSimpleType GetTokenTypeV1Compat()
	{
		if (s_tokenTypeV1Compat == null)
		{
			XmlSchemaSimpleType simpleTypeFromTypeCode = GetSimpleTypeFromTypeCode(XmlTypeCode.Token);
			XmlSchemaSimpleType xmlSchemaSimpleType = simpleTypeFromTypeCode.Clone() as XmlSchemaSimpleType;
			xmlSchemaSimpleType.SetDatatype(c_tokenV1Compat);
			xmlSchemaSimpleType.ElementDecl = new SchemaElementDecl(c_tokenV1Compat);
			xmlSchemaSimpleType.ElementDecl.SchemaType = xmlSchemaSimpleType;
			s_tokenTypeV1Compat = xmlSchemaSimpleType;
		}
		return s_tokenTypeV1Compat;
	}

	internal static XmlSchemaSimpleType[] GetBuiltInTypes()
	{
		return s_enumToTypeCode;
	}

	internal static XmlTypeCode GetPrimitiveTypeCode(XmlTypeCode typeCode)
	{
		XmlSchemaSimpleType xmlSchemaSimpleType = s_enumToTypeCode[(int)typeCode];
		while (xmlSchemaSimpleType.BaseXmlSchemaType != AnySimpleType)
		{
			xmlSchemaSimpleType = xmlSchemaSimpleType.BaseXmlSchemaType as XmlSchemaSimpleType;
		}
		return xmlSchemaSimpleType.TypeCode;
	}

	internal override XmlSchemaDatatype DeriveByRestriction(XmlSchemaObjectCollection facets, XmlNameTable nameTable, XmlSchemaType schemaType)
	{
		DatatypeImplementation datatypeImplementation = (DatatypeImplementation)MemberwiseClone();
		datatypeImplementation._restriction = FacetsChecker.ConstructRestriction(this, facets, nameTable);
		datatypeImplementation._baseType = this;
		datatypeImplementation._parentSchemaType = schemaType;
		datatypeImplementation._valueConverter = null;
		return datatypeImplementation;
	}

	internal override XmlSchemaDatatype DeriveByList(XmlSchemaType schemaType)
	{
		return DeriveByList(0, schemaType);
	}

	internal XmlSchemaDatatype DeriveByList(int minSize, XmlSchemaType schemaType)
	{
		if (_variety == XmlSchemaDatatypeVariety.List)
		{
			throw new XmlSchemaException(System.SR.Sch_ListFromNonatomic, string.Empty);
		}
		if (_variety == XmlSchemaDatatypeVariety.Union && !((Datatype_union)this).HasAtomicMembers())
		{
			throw new XmlSchemaException(System.SR.Sch_ListFromNonatomic, string.Empty);
		}
		DatatypeImplementation datatypeImplementation = new Datatype_List(this, minSize);
		datatypeImplementation._variety = XmlSchemaDatatypeVariety.List;
		datatypeImplementation._restriction = null;
		datatypeImplementation._baseType = s_anySimpleType;
		datatypeImplementation._parentSchemaType = schemaType;
		return datatypeImplementation;
	}

	internal new static DatatypeImplementation DeriveByUnion(XmlSchemaSimpleType[] types, XmlSchemaType schemaType)
	{
		DatatypeImplementation datatypeImplementation = new Datatype_union(types);
		datatypeImplementation._baseType = s_anySimpleType;
		datatypeImplementation._variety = XmlSchemaDatatypeVariety.Union;
		datatypeImplementation._parentSchemaType = schemaType;
		return datatypeImplementation;
	}

	internal override void VerifySchemaValid(XmlSchemaObjectTable notations, XmlSchemaObject caller)
	{
	}

	public override bool IsDerivedFrom(XmlSchemaDatatype datatype)
	{
		if (datatype == null)
		{
			return false;
		}
		for (DatatypeImplementation datatypeImplementation = this; datatypeImplementation != null; datatypeImplementation = datatypeImplementation._baseType)
		{
			if (datatypeImplementation == datatype)
			{
				return true;
			}
		}
		if (((DatatypeImplementation)datatype)._baseType == null)
		{
			Type type = GetType();
			Type type2 = datatype.GetType();
			if (!(type2 == type))
			{
				return type.IsSubclassOf(type2);
			}
			return true;
		}
		if (datatype.Variety == XmlSchemaDatatypeVariety.Union && !datatype.HasLexicalFacets && !datatype.HasValueFacets && _variety != XmlSchemaDatatypeVariety.Union)
		{
			return ((Datatype_union)datatype).IsUnionBaseOf(this);
		}
		if ((_variety == XmlSchemaDatatypeVariety.Union || _variety == XmlSchemaDatatypeVariety.List) && _restriction == null)
		{
			return datatype == s__anySimpleType.Datatype;
		}
		return false;
	}

	internal override bool IsEqual(object o1, object o2)
	{
		return Compare(o1, o2) == 0;
	}

	internal override bool IsComparable(XmlSchemaDatatype dtype)
	{
		XmlTypeCode typeCode = TypeCode;
		XmlTypeCode typeCode2 = dtype.TypeCode;
		if (typeCode == typeCode2)
		{
			return true;
		}
		if (GetPrimitiveTypeCode(typeCode) == GetPrimitiveTypeCode(typeCode2))
		{
			return true;
		}
		if (IsDerivedFrom(dtype) || dtype.IsDerivedFrom(this))
		{
			return true;
		}
		return false;
	}

	internal abstract XmlValueConverter CreateValueConverter(XmlSchemaType schemaType);

	public override object ParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr)
	{
		object typedValue;
		Exception ex = TryParseValue(s, nameTable, nsmgr, out typedValue);
		if (ex != null)
		{
			throw new XmlSchemaException(System.SR.Sch_InvalidValueDetailed, new string[3]
			{
				s,
				GetTypeName(),
				ex.Message
			}, ex, null, 0, 0, null);
		}
		if (Variety == XmlSchemaDatatypeVariety.Union)
		{
			XsdSimpleValue xsdSimpleValue = typedValue as XsdSimpleValue;
			return xsdSimpleValue.TypedValue;
		}
		return typedValue;
	}

	internal override object ParseValue(string s, XmlNameTable nameTable, IXmlNamespaceResolver nsmgr, bool createAtomicValue)
	{
		if (createAtomicValue)
		{
			object typedValue;
			Exception ex = TryParseValue(s, nameTable, nsmgr, out typedValue);
			if (ex != null)
			{
				throw new XmlSchemaException(System.SR.Sch_InvalidValueDetailed, new string[3]
				{
					s,
					GetTypeName(),
					ex.Message
				}, ex, null, 0, 0, null);
			}
			return typedValue;
		}
		return ParseValue(s, nameTable, nsmgr);
	}

	internal override Exception TryParseValue(object value, XmlNameTable nameTable, IXmlNamespaceResolver namespaceResolver, out object typedValue)
	{
		Exception ex = null;
		typedValue = null;
		if (value == null)
		{
			return new ArgumentNullException("value");
		}
		if (value is string s)
		{
			return TryParseValue(s, nameTable, namespaceResolver, out typedValue);
		}
		try
		{
			object obj = value;
			if (value.GetType() != ValueType)
			{
				obj = ValueConverter.ChangeType(value, ValueType, namespaceResolver);
			}
			if (!HasLexicalFacets)
			{
				goto IL_008d;
			}
			string parseString = (string)ValueConverter.ChangeType(value, typeof(string), namespaceResolver);
			ex = FacetsChecker.CheckLexicalFacets(ref parseString, this);
			if (ex == null)
			{
				goto IL_008d;
			}
			goto end_IL_002b;
			IL_008d:
			if (!HasValueFacets)
			{
				goto IL_00a8;
			}
			ex = FacetsChecker.CheckValueFacets(obj, this);
			if (ex == null)
			{
				goto IL_00a8;
			}
			goto end_IL_002b;
			IL_00a8:
			typedValue = obj;
			return null;
			end_IL_002b:;
		}
		catch (FormatException ex2)
		{
			ex = ex2;
		}
		catch (InvalidCastException ex3)
		{
			ex = ex3;
		}
		catch (OverflowException ex4)
		{
			ex = ex4;
		}
		catch (ArgumentException ex5)
		{
			ex = ex5;
		}
		return ex;
	}

	internal string GetTypeName()
	{
		XmlSchemaType parentSchemaType = _parentSchemaType;
		if (parentSchemaType == null || parentSchemaType.QualifiedName.IsEmpty)
		{
			return base.TypeCodeString;
		}
		return parentSchemaType.QualifiedName.ToString();
	}

	protected int Compare(byte[] value1, byte[] value2)
	{
		int num = value1.Length;
		if (num != value2.Length)
		{
			return -1;
		}
		for (int i = 0; i < num; i++)
		{
			if (value1[i] != value2[i])
			{
				return -1;
			}
		}
		return 0;
	}
}
