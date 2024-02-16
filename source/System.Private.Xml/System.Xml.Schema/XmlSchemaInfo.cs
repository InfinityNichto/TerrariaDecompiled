namespace System.Xml.Schema;

public class XmlSchemaInfo : IXmlSchemaInfo
{
	private bool _isDefault;

	private bool _isNil;

	private XmlSchemaElement _schemaElement;

	private XmlSchemaAttribute _schemaAttribute;

	private XmlSchemaType _schemaType;

	private XmlSchemaSimpleType _memberType;

	private XmlSchemaValidity _validity;

	private XmlSchemaContentType _contentType;

	public XmlSchemaValidity Validity
	{
		get
		{
			return _validity;
		}
		set
		{
			_validity = value;
		}
	}

	public bool IsDefault
	{
		get
		{
			return _isDefault;
		}
		set
		{
			_isDefault = value;
		}
	}

	public bool IsNil
	{
		get
		{
			return _isNil;
		}
		set
		{
			_isNil = value;
		}
	}

	public XmlSchemaSimpleType? MemberType
	{
		get
		{
			return _memberType;
		}
		set
		{
			_memberType = value;
		}
	}

	public XmlSchemaType? SchemaType
	{
		get
		{
			return _schemaType;
		}
		set
		{
			_schemaType = value;
			if (_schemaType != null)
			{
				_contentType = _schemaType.SchemaContentType;
			}
			else
			{
				_contentType = XmlSchemaContentType.Empty;
			}
		}
	}

	public XmlSchemaElement? SchemaElement
	{
		get
		{
			return _schemaElement;
		}
		set
		{
			_schemaElement = value;
			if (value != null)
			{
				_schemaAttribute = null;
			}
		}
	}

	public XmlSchemaAttribute? SchemaAttribute
	{
		get
		{
			return _schemaAttribute;
		}
		set
		{
			_schemaAttribute = value;
			if (value != null)
			{
				_schemaElement = null;
			}
		}
	}

	public XmlSchemaContentType ContentType
	{
		get
		{
			return _contentType;
		}
		set
		{
			_contentType = value;
		}
	}

	internal XmlSchemaType? XmlType
	{
		get
		{
			if (_memberType != null)
			{
				return _memberType;
			}
			return _schemaType;
		}
	}

	internal bool HasDefaultValue
	{
		get
		{
			if (_schemaElement != null)
			{
				return _schemaElement.ElementDecl.DefaultValueTyped != null;
			}
			return false;
		}
	}

	internal bool IsUnionType
	{
		get
		{
			if (_schemaType == null || _schemaType.Datatype == null)
			{
				return false;
			}
			return _schemaType.Datatype.Variety == XmlSchemaDatatypeVariety.Union;
		}
	}

	public XmlSchemaInfo()
	{
		Clear();
	}

	internal XmlSchemaInfo(XmlSchemaValidity validity)
		: this()
	{
		_validity = validity;
	}

	internal void Clear()
	{
		_isNil = false;
		_isDefault = false;
		_schemaType = null;
		_schemaElement = null;
		_schemaAttribute = null;
		_memberType = null;
		_validity = XmlSchemaValidity.NotKnown;
		_contentType = XmlSchemaContentType.Empty;
	}
}
