using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace System.Xml.Schema;

public class XmlSchemaElement : XmlSchemaParticle
{
	private bool _isAbstract;

	private bool _hasAbstractAttribute;

	private bool _isNillable;

	private bool _hasNillableAttribute;

	private bool _isLocalTypeDerivationChecked;

	private XmlSchemaDerivationMethod _block = XmlSchemaDerivationMethod.None;

	private XmlSchemaDerivationMethod _final = XmlSchemaDerivationMethod.None;

	private XmlSchemaForm _form;

	private string _defaultValue;

	private string _fixedValue;

	private string _name;

	private XmlQualifiedName _refName = XmlQualifiedName.Empty;

	private XmlQualifiedName _substitutionGroup = XmlQualifiedName.Empty;

	private XmlQualifiedName _typeName = XmlQualifiedName.Empty;

	private XmlSchemaType _type;

	private XmlQualifiedName _qualifiedName = XmlQualifiedName.Empty;

	private XmlSchemaType _elementType;

	private XmlSchemaDerivationMethod _blockResolved;

	private XmlSchemaDerivationMethod _finalResolved;

	private XmlSchemaObjectCollection _constraints;

	private SchemaElementDecl _elementDecl;

	[XmlAttribute("abstract")]
	[DefaultValue(false)]
	public bool IsAbstract
	{
		get
		{
			return _isAbstract;
		}
		set
		{
			_isAbstract = value;
			_hasAbstractAttribute = true;
		}
	}

	[XmlAttribute("block")]
	[DefaultValue(XmlSchemaDerivationMethod.None)]
	public XmlSchemaDerivationMethod Block
	{
		get
		{
			return _block;
		}
		set
		{
			_block = value;
		}
	}

	[XmlAttribute("default")]
	[DefaultValue(null)]
	public string? DefaultValue
	{
		get
		{
			return _defaultValue;
		}
		set
		{
			_defaultValue = value;
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

	[XmlAttribute("fixed")]
	[DefaultValue(null)]
	public string? FixedValue
	{
		get
		{
			return _fixedValue;
		}
		set
		{
			_fixedValue = value;
		}
	}

	[XmlAttribute("form")]
	[DefaultValue(XmlSchemaForm.None)]
	public XmlSchemaForm Form
	{
		get
		{
			return _form;
		}
		set
		{
			_form = value;
		}
	}

	[XmlAttribute("name")]
	[DefaultValue("")]
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

	[XmlAttribute("nillable")]
	[DefaultValue(false)]
	public bool IsNillable
	{
		get
		{
			return _isNillable;
		}
		set
		{
			_isNillable = value;
			_hasNillableAttribute = true;
		}
	}

	[XmlIgnore]
	internal bool HasNillableAttribute => _hasNillableAttribute;

	[XmlIgnore]
	internal bool HasAbstractAttribute => _hasAbstractAttribute;

	[XmlAttribute("ref")]
	public XmlQualifiedName RefName
	{
		get
		{
			return _refName;
		}
		[param: AllowNull]
		set
		{
			_refName = ((value == null) ? XmlQualifiedName.Empty : value);
		}
	}

	[XmlAttribute("substitutionGroup")]
	public XmlQualifiedName SubstitutionGroup
	{
		get
		{
			return _substitutionGroup;
		}
		[param: AllowNull]
		set
		{
			_substitutionGroup = ((value == null) ? XmlQualifiedName.Empty : value);
		}
	}

	[XmlAttribute("type")]
	public XmlQualifiedName SchemaTypeName
	{
		get
		{
			return _typeName;
		}
		[param: AllowNull]
		set
		{
			_typeName = ((value == null) ? XmlQualifiedName.Empty : value);
		}
	}

	[XmlElement("complexType", typeof(XmlSchemaComplexType))]
	[XmlElement("simpleType", typeof(XmlSchemaSimpleType))]
	public XmlSchemaType? SchemaType
	{
		get
		{
			return _type;
		}
		set
		{
			_type = value;
		}
	}

	[XmlElement("key", typeof(XmlSchemaKey))]
	[XmlElement("keyref", typeof(XmlSchemaKeyref))]
	[XmlElement("unique", typeof(XmlSchemaUnique))]
	public XmlSchemaObjectCollection Constraints
	{
		get
		{
			if (_constraints == null)
			{
				_constraints = new XmlSchemaObjectCollection();
			}
			return _constraints;
		}
	}

	[XmlIgnore]
	public XmlQualifiedName QualifiedName => _qualifiedName;

	[XmlIgnore]
	[Obsolete("XmlSchemaElement.ElementType has been deprecated. Use the ElementSchemaType property that returns a strongly typed element type instead.")]
	public object? ElementType
	{
		get
		{
			if (_elementType == null)
			{
				return null;
			}
			if (_elementType.QualifiedName.Namespace == "http://www.w3.org/2001/XMLSchema")
			{
				return _elementType.Datatype;
			}
			return _elementType;
		}
	}

	[XmlIgnore]
	public XmlSchemaType? ElementSchemaType => _elementType;

	[XmlIgnore]
	public XmlSchemaDerivationMethod BlockResolved => _blockResolved;

	[XmlIgnore]
	public XmlSchemaDerivationMethod FinalResolved => _finalResolved;

	[XmlIgnore]
	internal bool HasDefault
	{
		get
		{
			if (_defaultValue != null)
			{
				return _defaultValue.Length > 0;
			}
			return false;
		}
	}

	internal bool HasConstraints
	{
		get
		{
			if (_constraints != null)
			{
				return _constraints.Count > 0;
			}
			return false;
		}
	}

	internal bool IsLocalTypeDerivationChecked
	{
		get
		{
			return _isLocalTypeDerivationChecked;
		}
		set
		{
			_isLocalTypeDerivationChecked = value;
		}
	}

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

	[XmlIgnore]
	internal override string NameString => _qualifiedName.ToString();

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
		_qualifiedName = value;
	}

	internal void SetElementType(XmlSchemaType value)
	{
		_elementType = value;
	}

	internal void SetBlockResolved(XmlSchemaDerivationMethod value)
	{
		_blockResolved = value;
	}

	internal void SetFinalResolved(XmlSchemaDerivationMethod value)
	{
		_finalResolved = value;
	}

	internal override XmlSchemaObject Clone()
	{
		return Clone(null);
	}

	internal XmlSchemaObject Clone(XmlSchema parentSchema)
	{
		XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)MemberwiseClone();
		xmlSchemaElement._refName = _refName.Clone();
		xmlSchemaElement._substitutionGroup = _substitutionGroup.Clone();
		xmlSchemaElement._typeName = _typeName.Clone();
		xmlSchemaElement._qualifiedName = _qualifiedName.Clone();
		if (_type is XmlSchemaComplexType xmlSchemaComplexType && xmlSchemaComplexType.QualifiedName.IsEmpty)
		{
			xmlSchemaElement._type = (XmlSchemaType)xmlSchemaComplexType.Clone(parentSchema);
		}
		xmlSchemaElement._constraints = null;
		return xmlSchemaElement;
	}
}
