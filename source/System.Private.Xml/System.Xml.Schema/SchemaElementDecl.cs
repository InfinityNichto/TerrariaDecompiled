using System.Collections;
using System.Collections.Generic;

namespace System.Xml.Schema;

internal sealed class SchemaElementDecl : SchemaDeclBase, IDtdAttributeListInfo
{
	private readonly Dictionary<XmlQualifiedName, SchemaAttDef> _attdefs = new Dictionary<XmlQualifiedName, SchemaAttDef>();

	private List<IDtdDefaultAttributeInfo> _defaultAttdefs;

	private bool _isIdDeclared;

	private bool _hasNonCDataAttribute;

	private bool _isAbstract;

	private bool _isNillable;

	private bool _hasRequiredAttribute;

	private bool _isNotationDeclared;

	private readonly Dictionary<XmlQualifiedName, XmlQualifiedName> _prohibitedAttributes = new Dictionary<XmlQualifiedName, XmlQualifiedName>();

	private ContentValidator _contentValidator;

	private XmlSchemaAnyAttribute _anyAttribute;

	private XmlSchemaDerivationMethod _block;

	private CompiledIdentityConstraint[] _constraints;

	private XmlSchemaElement _schemaElement;

	internal static readonly SchemaElementDecl Empty = new SchemaElementDecl();

	string IDtdAttributeListInfo.Prefix => Prefix;

	string IDtdAttributeListInfo.LocalName => Name.Name;

	bool IDtdAttributeListInfo.HasNonCDataAttributes => _hasNonCDataAttribute;

	internal bool IsIdDeclared
	{
		get
		{
			return _isIdDeclared;
		}
		set
		{
			_isIdDeclared = value;
		}
	}

	internal bool HasNonCDataAttribute
	{
		get
		{
			return _hasNonCDataAttribute;
		}
		set
		{
			_hasNonCDataAttribute = value;
		}
	}

	internal bool IsAbstract
	{
		get
		{
			return _isAbstract;
		}
		set
		{
			_isAbstract = value;
		}
	}

	internal bool IsNillable
	{
		get
		{
			return _isNillable;
		}
		set
		{
			_isNillable = value;
		}
	}

	internal XmlSchemaDerivationMethod Block
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

	internal bool IsNotationDeclared
	{
		get
		{
			return _isNotationDeclared;
		}
		set
		{
			_isNotationDeclared = value;
		}
	}

	internal bool HasDefaultAttribute => _defaultAttdefs != null;

	internal bool HasRequiredAttribute => _hasRequiredAttribute;

	internal ContentValidator ContentValidator
	{
		get
		{
			return _contentValidator;
		}
		set
		{
			_contentValidator = value;
		}
	}

	internal XmlSchemaAnyAttribute AnyAttribute
	{
		get
		{
			return _anyAttribute;
		}
		set
		{
			_anyAttribute = value;
		}
	}

	internal CompiledIdentityConstraint[] Constraints
	{
		get
		{
			return _constraints;
		}
		set
		{
			_constraints = value;
		}
	}

	internal XmlSchemaElement SchemaElement
	{
		get
		{
			return _schemaElement;
		}
		set
		{
			_schemaElement = value;
		}
	}

	internal IList<IDtdDefaultAttributeInfo> DefaultAttDefs => _defaultAttdefs;

	internal Dictionary<XmlQualifiedName, SchemaAttDef> AttDefs => _attdefs;

	internal Dictionary<XmlQualifiedName, XmlQualifiedName> ProhibitedAttributes => _prohibitedAttributes;

	internal SchemaElementDecl()
	{
	}

	internal SchemaElementDecl(XmlSchemaDatatype dtype)
	{
		base.Datatype = dtype;
		_contentValidator = ContentValidator.TextOnly;
	}

	internal SchemaElementDecl(XmlQualifiedName name, string prefix)
		: base(name, prefix)
	{
	}

	internal static SchemaElementDecl CreateAnyTypeElementDecl()
	{
		SchemaElementDecl schemaElementDecl = new SchemaElementDecl();
		schemaElementDecl.Datatype = DatatypeImplementation.AnySimpleType.Datatype;
		return schemaElementDecl;
	}

	IDtdAttributeInfo IDtdAttributeListInfo.LookupAttribute(string prefix, string localName)
	{
		XmlQualifiedName key = new XmlQualifiedName(localName, prefix);
		if (_attdefs.TryGetValue(key, out var value))
		{
			return value;
		}
		return null;
	}

	IEnumerable<IDtdDefaultAttributeInfo> IDtdAttributeListInfo.LookupDefaultAttributes()
	{
		return _defaultAttdefs;
	}

	IDtdAttributeInfo IDtdAttributeListInfo.LookupIdAttribute()
	{
		foreach (SchemaAttDef value in _attdefs.Values)
		{
			if (value.TokenizedType == XmlTokenizedType.ID)
			{
				return value;
			}
		}
		return null;
	}

	internal SchemaElementDecl Clone()
	{
		return (SchemaElementDecl)MemberwiseClone();
	}

	internal void AddAttDef(SchemaAttDef attdef)
	{
		_attdefs.Add(attdef.Name, attdef);
		if (attdef.Presence == Use.Required || attdef.Presence == Use.RequiredFixed)
		{
			_hasRequiredAttribute = true;
		}
		if (attdef.Presence == Use.Default || attdef.Presence == Use.Fixed)
		{
			if (_defaultAttdefs == null)
			{
				_defaultAttdefs = new List<IDtdDefaultAttributeInfo>();
			}
			_defaultAttdefs.Add(attdef);
		}
	}

	internal SchemaAttDef GetAttDef(XmlQualifiedName qname)
	{
		if (_attdefs.TryGetValue(qname, out var value))
		{
			return value;
		}
		return null;
	}

	internal void CheckAttributes(Hashtable presence, bool standalone)
	{
		foreach (SchemaAttDef value in _attdefs.Values)
		{
			if (presence[value.Name] == null)
			{
				if (value.Presence == Use.Required)
				{
					throw new XmlSchemaException(System.SR.Sch_MissRequiredAttribute, value.Name.ToString());
				}
				if (standalone && value.IsDeclaredInExternal && (value.Presence == Use.Default || value.Presence == Use.Fixed))
				{
					throw new XmlSchemaException(System.SR.Sch_StandAlone, string.Empty);
				}
			}
		}
	}
}
