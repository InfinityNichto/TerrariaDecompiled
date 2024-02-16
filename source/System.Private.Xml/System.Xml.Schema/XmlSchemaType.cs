using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaType : XmlSchemaAnnotated
{
	private string _name;

	private XmlSchemaDerivationMethod _final = XmlSchemaDerivationMethod.None;

	private XmlSchemaDerivationMethod _derivedBy;

	private XmlSchemaType _baseSchemaType;

	private XmlSchemaDatatype _datatype;

	private XmlSchemaDerivationMethod _finalResolved;

	private volatile SchemaElementDecl _elementDecl;

	private volatile XmlQualifiedName _qname = XmlQualifiedName.Empty;

	private XmlSchemaType _redefined;

	private XmlSchemaContentType _contentType;

	[XmlAttribute("name")]
	public string? Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	[XmlAttribute("final")]
	[DefaultValue(XmlSchemaDerivationMethod.None)]
	public XmlSchemaDerivationMethod Final
	{
		get
		{
			return _final;
		}
		set
		{
			_final = value;
		}
	}

	[XmlIgnore]
	public XmlQualifiedName QualifiedName => _qname;

	[XmlIgnore]
	public XmlSchemaDerivationMethod FinalResolved => _finalResolved;

	[XmlIgnore]
	[Obsolete("XmlSchemaType.BaseSchemaType has been deprecated. Use the BaseXmlSchemaType property that returns a strongly typed base schema type instead.")]
	public object? BaseSchemaType
	{
		get
		{
			if (_baseSchemaType == null)
			{
				return null;
			}
			if (_baseSchemaType.QualifiedName.Namespace == "http://www.w3.org/2001/XMLSchema")
			{
				return _baseSchemaType.Datatype;
			}
			return _baseSchemaType;
		}
	}

	[XmlIgnore]
	public XmlSchemaType? BaseXmlSchemaType => _baseSchemaType;

	[XmlIgnore]
	public XmlSchemaDerivationMethod DerivedBy => _derivedBy;

	[XmlIgnore]
	public XmlSchemaDatatype? Datatype => _datatype;

	[XmlIgnore]
	public virtual bool IsMixed
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	[XmlIgnore]
	public XmlTypeCode TypeCode
	{
		get
		{
			if (this == XmlSchemaComplexType.AnyType)
			{
				return XmlTypeCode.Item;
			}
			if (_datatype == null)
			{
				return XmlTypeCode.None;
			}
			return _datatype.TypeCode;
		}
	}

	[XmlIgnore]
	internal XmlValueConverter ValueConverter
	{
		get
		{
			if (_datatype == null)
			{
				return XmlUntypedConverter.Untyped;
			}
			return _datatype.ValueConverter;
		}
	}

	internal XmlSchemaContentType SchemaContentType => _contentType;

	internal SchemaElementDecl? ElementDecl
	{
		get
		{
			return _elementDecl;
		}
		set
		{
			_elementDecl = value;
		}
	}

	[XmlIgnore]
	internal XmlSchemaType? Redefined
	{
		get
		{
			return _redefined;
		}
		set
		{
			_redefined = value;
		}
	}

	internal virtual XmlQualifiedName DerivedFrom => XmlQualifiedName.Empty;

	[XmlIgnore]
	internal override string? NameAttribute
	{
		get
		{
			return Name;
		}
		set
		{
			Name = value;
		}
	}

	public static XmlSchemaSimpleType? GetBuiltInSimpleType(XmlQualifiedName qualifiedName)
	{
		if (qualifiedName == null)
		{
			throw new ArgumentNullException("qualifiedName");
		}
		return DatatypeImplementation.GetSimpleTypeFromXsdType(qualifiedName);
	}

	public static XmlSchemaSimpleType GetBuiltInSimpleType(XmlTypeCode typeCode)
	{
		return DatatypeImplementation.GetSimpleTypeFromTypeCode(typeCode);
	}

	public static XmlSchemaComplexType? GetBuiltInComplexType(XmlTypeCode typeCode)
	{
		if (typeCode == XmlTypeCode.Item)
		{
			return XmlSchemaComplexType.AnyType;
		}
		return null;
	}

	public static XmlSchemaComplexType? GetBuiltInComplexType(XmlQualifiedName qualifiedName)
	{
		if (qualifiedName == null)
		{
			throw new ArgumentNullException("qualifiedName");
		}
		if (qualifiedName.Equals(XmlSchemaComplexType.AnyType.QualifiedName))
		{
			return XmlSchemaComplexType.AnyType;
		}
		if (qualifiedName.Equals(XmlSchemaComplexType.UntypedAnyType.QualifiedName))
		{
			return XmlSchemaComplexType.UntypedAnyType;
		}
		return null;
	}

	[return: NotNullIfNotNull("schemaSet")]
	internal XmlReader Validate(XmlReader reader, XmlResolver resolver, XmlSchemaSet schemaSet, ValidationEventHandler valEventHandler)
	{
		if (schemaSet != null)
		{
			XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
			xmlReaderSettings.ValidationType = ValidationType.Schema;
			xmlReaderSettings.Schemas = schemaSet;
			xmlReaderSettings.ValidationEventHandler += valEventHandler;
			return new XsdValidatingReader(reader, resolver, xmlReaderSettings, this);
		}
		return null;
	}

	internal void SetQualifiedName(XmlQualifiedName value)
	{
		_qname = value;
	}

	internal void SetFinalResolved(XmlSchemaDerivationMethod value)
	{
		_finalResolved = value;
	}

	internal void SetBaseSchemaType(XmlSchemaType value)
	{
		_baseSchemaType = value;
	}

	internal void SetDerivedBy(XmlSchemaDerivationMethod value)
	{
		_derivedBy = value;
	}

	internal void SetDatatype(XmlSchemaDatatype value)
	{
		_datatype = value;
	}

	internal void SetContentType(XmlSchemaContentType value)
	{
		_contentType = value;
	}

	public static bool IsDerivedFrom([NotNullWhen(true)] XmlSchemaType? derivedType, [NotNullWhen(true)] XmlSchemaType? baseType, XmlSchemaDerivationMethod except)
	{
		if (derivedType == null || baseType == null)
		{
			return false;
		}
		if (derivedType == baseType)
		{
			return true;
		}
		if (baseType == XmlSchemaComplexType.AnyType)
		{
			return true;
		}
		do
		{
			XmlSchemaSimpleType xmlSchemaSimpleType = derivedType as XmlSchemaSimpleType;
			if (baseType is XmlSchemaSimpleType xmlSchemaSimpleType2 && xmlSchemaSimpleType != null)
			{
				if (xmlSchemaSimpleType2 == DatatypeImplementation.AnySimpleType)
				{
					return true;
				}
				if ((except & derivedType.DerivedBy) != 0 || !xmlSchemaSimpleType.Datatype.IsDerivedFrom(xmlSchemaSimpleType2.Datatype))
				{
					return false;
				}
				return true;
			}
			if ((except & derivedType.DerivedBy) != 0)
			{
				return false;
			}
			derivedType = derivedType.BaseXmlSchemaType;
			if (derivedType == baseType)
			{
				return true;
			}
		}
		while (derivedType != null);
		return false;
	}

	internal static bool IsDerivedFromDatatype(XmlSchemaDatatype derivedDataType, XmlSchemaDatatype baseDataType, XmlSchemaDerivationMethod except)
	{
		if (DatatypeImplementation.AnySimpleType.Datatype == baseDataType)
		{
			return true;
		}
		return derivedDataType.IsDerivedFrom(baseDataType);
	}
}
