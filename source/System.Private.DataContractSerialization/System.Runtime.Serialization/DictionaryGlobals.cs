using System.Xml;

namespace System.Runtime.Serialization;

internal static class DictionaryGlobals
{
	private static readonly XmlDictionary s_dictionary = new XmlDictionary(61);

	public static readonly XmlDictionaryString SchemaInstanceNamespace = s_dictionary.Add("http://www.w3.org/2001/XMLSchema-instance");

	public static readonly XmlDictionaryString SerializationNamespace = s_dictionary.Add("http://schemas.microsoft.com/2003/10/Serialization/");

	public static readonly XmlDictionaryString SchemaNamespace = s_dictionary.Add("http://www.w3.org/2001/XMLSchema");

	public static readonly XmlDictionaryString XsiTypeLocalName = s_dictionary.Add("type");

	public static readonly XmlDictionaryString XsiNilLocalName = s_dictionary.Add("nil");

	public static readonly XmlDictionaryString IdLocalName = s_dictionary.Add("Id");

	public static readonly XmlDictionaryString RefLocalName = s_dictionary.Add("Ref");

	public static readonly XmlDictionaryString ArraySizeLocalName = s_dictionary.Add("Size");

	public static readonly XmlDictionaryString EmptyString = s_dictionary.Add(string.Empty);

	public static readonly XmlDictionaryString ISerializableFactoryTypeLocalName = s_dictionary.Add("FactoryType");

	public static readonly XmlDictionaryString XmlnsNamespace = s_dictionary.Add("http://www.w3.org/2000/xmlns/");

	public static readonly XmlDictionaryString CharLocalName = s_dictionary.Add("char");

	public static readonly XmlDictionaryString BooleanLocalName = s_dictionary.Add("boolean");

	public static readonly XmlDictionaryString SignedByteLocalName = s_dictionary.Add("byte");

	public static readonly XmlDictionaryString UnsignedByteLocalName = s_dictionary.Add("unsignedByte");

	public static readonly XmlDictionaryString ShortLocalName = s_dictionary.Add("short");

	public static readonly XmlDictionaryString UnsignedShortLocalName = s_dictionary.Add("unsignedShort");

	public static readonly XmlDictionaryString IntLocalName = s_dictionary.Add("int");

	public static readonly XmlDictionaryString UnsignedIntLocalName = s_dictionary.Add("unsignedInt");

	public static readonly XmlDictionaryString LongLocalName = s_dictionary.Add("long");

	public static readonly XmlDictionaryString UnsignedLongLocalName = s_dictionary.Add("unsignedLong");

	public static readonly XmlDictionaryString FloatLocalName = s_dictionary.Add("float");

	public static readonly XmlDictionaryString DoubleLocalName = s_dictionary.Add("double");

	public static readonly XmlDictionaryString DecimalLocalName = s_dictionary.Add("decimal");

	public static readonly XmlDictionaryString DateTimeLocalName = s_dictionary.Add("dateTime");

	public static readonly XmlDictionaryString StringLocalName = s_dictionary.Add("string");

	public static readonly XmlDictionaryString ByteArrayLocalName = s_dictionary.Add("base64Binary");

	public static readonly XmlDictionaryString ObjectLocalName = s_dictionary.Add("anyType");

	public static readonly XmlDictionaryString TimeSpanLocalName = s_dictionary.Add("duration");

	public static readonly XmlDictionaryString GuidLocalName = s_dictionary.Add("guid");

	public static readonly XmlDictionaryString UriLocalName = s_dictionary.Add("anyURI");

	public static readonly XmlDictionaryString QNameLocalName = s_dictionary.Add("QName");

	public static readonly XmlDictionaryString ClrTypeLocalName = s_dictionary.Add("Type");

	public static readonly XmlDictionaryString ClrAssemblyLocalName = s_dictionary.Add("Assembly");

	public static readonly XmlDictionaryString Space = s_dictionary.Add(" ");

	public static readonly XmlDictionaryString timeLocalName = s_dictionary.Add("time");

	public static readonly XmlDictionaryString dateLocalName = s_dictionary.Add("date");

	public static readonly XmlDictionaryString hexBinaryLocalName = s_dictionary.Add("hexBinary");

	public static readonly XmlDictionaryString gYearMonthLocalName = s_dictionary.Add("gYearMonth");

	public static readonly XmlDictionaryString gYearLocalName = s_dictionary.Add("gYear");

	public static readonly XmlDictionaryString gMonthDayLocalName = s_dictionary.Add("gMonthDay");

	public static readonly XmlDictionaryString gDayLocalName = s_dictionary.Add("gDay");

	public static readonly XmlDictionaryString gMonthLocalName = s_dictionary.Add("gMonth");

	public static readonly XmlDictionaryString integerLocalName = s_dictionary.Add("integer");

	public static readonly XmlDictionaryString positiveIntegerLocalName = s_dictionary.Add("positiveInteger");

	public static readonly XmlDictionaryString negativeIntegerLocalName = s_dictionary.Add("negativeInteger");

	public static readonly XmlDictionaryString nonPositiveIntegerLocalName = s_dictionary.Add("nonPositiveInteger");

	public static readonly XmlDictionaryString nonNegativeIntegerLocalName = s_dictionary.Add("nonNegativeInteger");

	public static readonly XmlDictionaryString normalizedStringLocalName = s_dictionary.Add("normalizedString");

	public static readonly XmlDictionaryString tokenLocalName = s_dictionary.Add("token");

	public static readonly XmlDictionaryString languageLocalName = s_dictionary.Add("language");

	public static readonly XmlDictionaryString NameLocalName = s_dictionary.Add("Name");

	public static readonly XmlDictionaryString NCNameLocalName = s_dictionary.Add("NCName");

	public static readonly XmlDictionaryString XSDIDLocalName = s_dictionary.Add("ID");

	public static readonly XmlDictionaryString IDREFLocalName = s_dictionary.Add("IDREF");

	public static readonly XmlDictionaryString IDREFSLocalName = s_dictionary.Add("IDREFS");

	public static readonly XmlDictionaryString ENTITYLocalName = s_dictionary.Add("ENTITY");

	public static readonly XmlDictionaryString ENTITIESLocalName = s_dictionary.Add("ENTITIES");

	public static readonly XmlDictionaryString NMTOKENLocalName = s_dictionary.Add("NMTOKEN");

	public static readonly XmlDictionaryString NMTOKENSLocalName = s_dictionary.Add("NMTOKENS");

	public static readonly XmlDictionaryString AsmxTypesNamespace = s_dictionary.Add("http://microsoft.com/wsdl/types/");
}
