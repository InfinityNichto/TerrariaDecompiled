using System.Collections;
using System.Data.Common;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;

namespace System.Data;

internal sealed class SimpleType : ISerializable
{
	private string _baseType;

	private SimpleType _baseSimpleType;

	private XmlQualifiedName _xmlBaseType;

	private string _name = string.Empty;

	private int _length = -1;

	private int _minLength = -1;

	private int _maxLength = -1;

	private string _pattern = string.Empty;

	private string _ns = string.Empty;

	private string _maxExclusive = string.Empty;

	private string _maxInclusive = string.Empty;

	private string _minExclusive = string.Empty;

	private string _minInclusive = string.Empty;

	internal string _enumeration = string.Empty;

	internal string BaseType => _baseType;

	internal XmlQualifiedName XmlBaseType => _xmlBaseType;

	internal string Name => _name;

	internal string Namespace => _ns;

	internal int Length => _length;

	internal int MaxLength
	{
		get
		{
			return _maxLength;
		}
		set
		{
			_maxLength = value;
		}
	}

	internal SimpleType BaseSimpleType => _baseSimpleType;

	public string SimpleTypeQualifiedName
	{
		get
		{
			if (_ns.Length == 0)
			{
				return _name;
			}
			return _ns + ":" + _name;
		}
	}

	internal SimpleType(string baseType)
	{
		_baseType = baseType;
	}

	internal SimpleType(XmlSchemaSimpleType node)
	{
		_name = node.Name;
		_ns = ((node.QualifiedName != null) ? node.QualifiedName.Namespace : "");
		LoadTypeValues(node);
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	internal void LoadTypeValues(XmlSchemaSimpleType node)
	{
		if (node.Content is XmlSchemaSimpleTypeList || node.Content is XmlSchemaSimpleTypeUnion)
		{
			throw ExceptionBuilder.SimpleTypeNotSupported();
		}
		if (node.Content is XmlSchemaSimpleTypeRestriction)
		{
			XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)node.Content;
			if (node.BaseXmlSchemaType is XmlSchemaSimpleType xmlSchemaSimpleType && xmlSchemaSimpleType.QualifiedName.Namespace != "http://www.w3.org/2001/XMLSchema")
			{
				_baseSimpleType = new SimpleType(xmlSchemaSimpleType);
			}
			if (xmlSchemaSimpleTypeRestriction.BaseTypeName.Namespace == "http://www.w3.org/2001/XMLSchema")
			{
				_baseType = xmlSchemaSimpleTypeRestriction.BaseTypeName.Name;
			}
			else
			{
				_baseType = xmlSchemaSimpleTypeRestriction.BaseTypeName.ToString();
			}
			if (_baseSimpleType != null && _baseSimpleType.Name != null && _baseSimpleType.Name.Length > 0)
			{
				_xmlBaseType = _baseSimpleType.XmlBaseType;
			}
			else
			{
				_xmlBaseType = xmlSchemaSimpleTypeRestriction.BaseTypeName;
			}
			if (_baseType == null || _baseType.Length == 0)
			{
				_baseType = xmlSchemaSimpleTypeRestriction.BaseType.Name;
				_xmlBaseType = null;
			}
			if (_baseType == "NOTATION")
			{
				_baseType = "string";
			}
			foreach (XmlSchemaFacet facet in xmlSchemaSimpleTypeRestriction.Facets)
			{
				if (facet is XmlSchemaLengthFacet)
				{
					_length = Convert.ToInt32(facet.Value, null);
				}
				if (facet is XmlSchemaMinLengthFacet)
				{
					_minLength = Convert.ToInt32(facet.Value, null);
				}
				if (facet is XmlSchemaMaxLengthFacet)
				{
					_maxLength = Convert.ToInt32(facet.Value, null);
				}
				if (facet is XmlSchemaPatternFacet)
				{
					_pattern = facet.Value;
				}
				if (facet is XmlSchemaEnumerationFacet)
				{
					_enumeration = ((!string.IsNullOrEmpty(_enumeration)) ? (_enumeration + " " + facet.Value) : facet.Value);
				}
				if (facet is XmlSchemaMinExclusiveFacet)
				{
					_minExclusive = facet.Value;
				}
				if (facet is XmlSchemaMinInclusiveFacet)
				{
					_minInclusive = facet.Value;
				}
				if (facet is XmlSchemaMaxExclusiveFacet)
				{
					_maxExclusive = facet.Value;
				}
				if (facet is XmlSchemaMaxInclusiveFacet)
				{
					_maxInclusive = facet.Value;
				}
			}
		}
		string msdataAttribute = XSDSchema.GetMsdataAttribute(node, "targetNamespace");
		if (msdataAttribute != null)
		{
			_ns = msdataAttribute;
		}
	}

	internal bool IsPlainString()
	{
		if (XSDSchema.QualifiedName(_baseType) == XSDSchema.QualifiedName("string") && string.IsNullOrEmpty(_name) && _length == -1 && _minLength == -1 && _maxLength == -1 && string.IsNullOrEmpty(_pattern) && string.IsNullOrEmpty(_maxExclusive) && string.IsNullOrEmpty(_maxInclusive) && string.IsNullOrEmpty(_minExclusive) && string.IsNullOrEmpty(_minInclusive))
		{
			return string.IsNullOrEmpty(_enumeration);
		}
		return false;
	}

	internal string QualifiedName(string name)
	{
		if (!name.Contains(':'))
		{
			return "xs:" + name;
		}
		return name;
	}

	internal XmlNode ToNode(XmlDocument dc, Hashtable prefixes, bool inRemoting)
	{
		XmlElement xmlElement = dc.CreateElement("xs", "simpleType", "http://www.w3.org/2001/XMLSchema");
		if (_name != null && _name.Length != 0)
		{
			xmlElement.SetAttribute("name", _name);
			if (inRemoting)
			{
				xmlElement.SetAttribute("targetNamespace", "urn:schemas-microsoft-com:xml-msdata", Namespace);
			}
		}
		XmlElement xmlElement2 = dc.CreateElement("xs", "restriction", "http://www.w3.org/2001/XMLSchema");
		if (!inRemoting)
		{
			if (_baseSimpleType != null)
			{
				if (_baseSimpleType.Namespace != null && _baseSimpleType.Namespace.Length > 0)
				{
					string text = ((prefixes != null) ? ((string)prefixes[_baseSimpleType.Namespace]) : null);
					if (text != null)
					{
						xmlElement2.SetAttribute("base", text + ":" + _baseSimpleType.Name);
					}
					else
					{
						xmlElement2.SetAttribute("base", _baseSimpleType.Name);
					}
				}
				else
				{
					xmlElement2.SetAttribute("base", _baseSimpleType.Name);
				}
			}
			else
			{
				xmlElement2.SetAttribute("base", QualifiedName(_baseType));
			}
		}
		else
		{
			xmlElement2.SetAttribute("base", (_baseSimpleType != null) ? _baseSimpleType.Name : QualifiedName(_baseType));
		}
		if (_length >= 0)
		{
			XmlElement xmlElement3 = dc.CreateElement("xs", "length", "http://www.w3.org/2001/XMLSchema");
			xmlElement3.SetAttribute("value", _length.ToString(CultureInfo.InvariantCulture));
			xmlElement2.AppendChild(xmlElement3);
		}
		if (_maxLength >= 0)
		{
			XmlElement xmlElement3 = dc.CreateElement("xs", "maxLength", "http://www.w3.org/2001/XMLSchema");
			xmlElement3.SetAttribute("value", _maxLength.ToString(CultureInfo.InvariantCulture));
			xmlElement2.AppendChild(xmlElement3);
		}
		xmlElement.AppendChild(xmlElement2);
		return xmlElement;
	}

	internal static SimpleType CreateEnumeratedType(string values)
	{
		SimpleType simpleType = new SimpleType("string");
		simpleType._enumeration = values;
		return simpleType;
	}

	internal static SimpleType CreateByteArrayType(string encoding)
	{
		return new SimpleType("base64Binary");
	}

	internal static SimpleType CreateLimitedStringType(int length)
	{
		SimpleType simpleType = new SimpleType("string");
		simpleType._maxLength = length;
		return simpleType;
	}

	internal static SimpleType CreateSimpleType(StorageType typeCode, Type type)
	{
		if (typeCode == StorageType.Char && type == typeof(char))
		{
			return new SimpleType("string")
			{
				_length = 1
			};
		}
		return null;
	}

	internal string HasConflictingDefinition(SimpleType otherSimpleType)
	{
		if (otherSimpleType == null)
		{
			return "otherSimpleType";
		}
		if (MaxLength != otherSimpleType.MaxLength)
		{
			return "MaxLength";
		}
		if (!string.Equals(BaseType, otherSimpleType.BaseType, StringComparison.Ordinal))
		{
			return "BaseType";
		}
		if (BaseSimpleType != null && otherSimpleType.BaseSimpleType != null && BaseSimpleType.HasConflictingDefinition(otherSimpleType.BaseSimpleType).Length != 0)
		{
			return "BaseSimpleType";
		}
		return string.Empty;
	}

	internal bool CanHaveMaxLength()
	{
		SimpleType simpleType = this;
		while (simpleType.BaseSimpleType != null)
		{
			simpleType = simpleType.BaseSimpleType;
		}
		return string.Equals(simpleType.BaseType, "string", StringComparison.OrdinalIgnoreCase);
	}

	internal void ConvertToAnnonymousSimpleType()
	{
		_name = null;
		_ns = string.Empty;
		SimpleType simpleType = this;
		while (simpleType._baseSimpleType != null)
		{
			simpleType = simpleType._baseSimpleType;
		}
		_baseType = simpleType._baseType;
		_baseSimpleType = simpleType._baseSimpleType;
		_xmlBaseType = simpleType._xmlBaseType;
	}
}
