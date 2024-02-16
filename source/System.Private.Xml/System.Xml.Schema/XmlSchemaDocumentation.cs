using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaDocumentation : XmlSchemaObject
{
	private string _source;

	private string _language;

	private XmlNode[] _markup;

	private static readonly XmlSchemaSimpleType s_languageType = DatatypeImplementation.GetSimpleTypeFromXsdType(new XmlQualifiedName("language", "http://www.w3.org/2001/XMLSchema"));

	[XmlAttribute("source", DataType = "anyURI")]
	public string? Source
	{
		get
		{
			return _source;
		}
		set
		{
			_source = value;
		}
	}

	[XmlAttribute("xml:lang")]
	public string? Language
	{
		get
		{
			return _language;
		}
		[param: DisallowNull]
		set
		{
			_language = (string)s_languageType.Datatype.ParseValue(value, null, null);
		}
	}

	[XmlText]
	[XmlAnyElement]
	public XmlNode?[]? Markup
	{
		get
		{
			return _markup;
		}
		set
		{
			_markup = value;
		}
	}
}
