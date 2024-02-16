using System.Collections.Generic;

namespace System.Xml.Schema;

internal sealed class SchemaInfo : IDtdInfo
{
	private readonly Dictionary<XmlQualifiedName, SchemaElementDecl> _elementDecls = new Dictionary<XmlQualifiedName, SchemaElementDecl>();

	private readonly Dictionary<XmlQualifiedName, SchemaElementDecl> _undeclaredElementDecls = new Dictionary<XmlQualifiedName, SchemaElementDecl>();

	private Dictionary<XmlQualifiedName, SchemaEntity> _generalEntities;

	private Dictionary<XmlQualifiedName, SchemaEntity> _parameterEntities;

	private XmlQualifiedName _docTypeName = XmlQualifiedName.Empty;

	private string _internalDtdSubset = string.Empty;

	private bool _hasNonCDataAttributes;

	private bool _hasDefaultAttributes;

	private readonly Dictionary<string, bool> _targetNamespaces = new Dictionary<string, bool>();

	private readonly Dictionary<XmlQualifiedName, SchemaAttDef> _attributeDecls = new Dictionary<XmlQualifiedName, SchemaAttDef>();

	private int _errorCount;

	private SchemaType _schemaType;

	private readonly Dictionary<XmlQualifiedName, SchemaElementDecl> _elementDeclsByType = new Dictionary<XmlQualifiedName, SchemaElementDecl>();

	private Dictionary<string, SchemaNotation> _notations;

	public XmlQualifiedName DocTypeName
	{
		get
		{
			return _docTypeName;
		}
		set
		{
			_docTypeName = value;
		}
	}

	internal string InternalDtdSubset
	{
		set
		{
			_internalDtdSubset = value;
		}
	}

	internal Dictionary<XmlQualifiedName, SchemaElementDecl> ElementDecls => _elementDecls;

	internal Dictionary<XmlQualifiedName, SchemaElementDecl> UndeclaredElementDecls => _undeclaredElementDecls;

	internal Dictionary<XmlQualifiedName, SchemaEntity> GeneralEntities
	{
		get
		{
			if (_generalEntities == null)
			{
				_generalEntities = new Dictionary<XmlQualifiedName, SchemaEntity>();
			}
			return _generalEntities;
		}
	}

	internal Dictionary<XmlQualifiedName, SchemaEntity> ParameterEntities
	{
		get
		{
			if (_parameterEntities == null)
			{
				_parameterEntities = new Dictionary<XmlQualifiedName, SchemaEntity>();
			}
			return _parameterEntities;
		}
	}

	internal SchemaType SchemaType
	{
		get
		{
			return _schemaType;
		}
		set
		{
			_schemaType = value;
		}
	}

	internal Dictionary<string, bool> TargetNamespaces => _targetNamespaces;

	internal Dictionary<XmlQualifiedName, SchemaElementDecl> ElementDeclsByType => _elementDeclsByType;

	internal Dictionary<XmlQualifiedName, SchemaAttDef> AttributeDecls => _attributeDecls;

	internal Dictionary<string, SchemaNotation> Notations
	{
		get
		{
			if (_notations == null)
			{
				_notations = new Dictionary<string, SchemaNotation>();
			}
			return _notations;
		}
	}

	internal int ErrorCount
	{
		get
		{
			return _errorCount;
		}
		set
		{
			_errorCount = value;
		}
	}

	bool IDtdInfo.HasDefaultAttributes => _hasDefaultAttributes;

	bool IDtdInfo.HasNonCDataAttributes => _hasNonCDataAttributes;

	XmlQualifiedName IDtdInfo.Name => _docTypeName;

	string IDtdInfo.InternalDtdSubset => _internalDtdSubset;

	internal SchemaInfo()
	{
		_schemaType = SchemaType.None;
	}

	internal SchemaElementDecl GetElementDecl(XmlQualifiedName qname)
	{
		if (_elementDecls.TryGetValue(qname, out var value))
		{
			return value;
		}
		return null;
	}

	internal SchemaElementDecl GetTypeDecl(XmlQualifiedName qname)
	{
		if (_elementDeclsByType.TryGetValue(qname, out var value))
		{
			return value;
		}
		return null;
	}

	internal XmlSchemaElement GetElement(XmlQualifiedName qname)
	{
		return GetElementDecl(qname)?.SchemaElement;
	}

	internal bool HasSchema(string ns)
	{
		return _targetNamespaces.ContainsKey(ns);
	}

	internal bool Contains(string ns)
	{
		return _targetNamespaces.ContainsKey(ns);
	}

	internal SchemaAttDef GetAttributeXdr(SchemaElementDecl ed, XmlQualifiedName qname)
	{
		SchemaAttDef value = null;
		if (ed != null)
		{
			value = ed.GetAttDef(qname);
			if (value == null)
			{
				if (!ed.ContentValidator.IsOpen || qname.Namespace.Length == 0)
				{
					throw new XmlSchemaException(System.SR.Sch_UndeclaredAttribute, qname.ToString());
				}
				if (!_attributeDecls.TryGetValue(qname, out value) && _targetNamespaces.ContainsKey(qname.Namespace))
				{
					throw new XmlSchemaException(System.SR.Sch_UndeclaredAttribute, qname.ToString());
				}
			}
		}
		return value;
	}

	internal SchemaAttDef GetAttributeXsd(SchemaElementDecl ed, XmlQualifiedName qname, XmlSchemaObject partialValidationType, out AttributeMatchState attributeMatchState)
	{
		SchemaAttDef value = null;
		attributeMatchState = AttributeMatchState.UndeclaredAttribute;
		if (ed != null)
		{
			value = ed.GetAttDef(qname);
			if (value != null)
			{
				attributeMatchState = AttributeMatchState.AttributeFound;
				return value;
			}
			XmlSchemaAnyAttribute anyAttribute = ed.AnyAttribute;
			if (anyAttribute != null)
			{
				if (!anyAttribute.NamespaceList.Allows(qname))
				{
					attributeMatchState = AttributeMatchState.ProhibitedAnyAttribute;
				}
				else if (anyAttribute.ProcessContentsCorrect != XmlSchemaContentProcessing.Skip)
				{
					if (_attributeDecls.TryGetValue(qname, out value))
					{
						if (value.Datatype.TypeCode == XmlTypeCode.Id)
						{
							attributeMatchState = AttributeMatchState.AnyIdAttributeFound;
						}
						else
						{
							attributeMatchState = AttributeMatchState.AttributeFound;
						}
					}
					else if (anyAttribute.ProcessContentsCorrect == XmlSchemaContentProcessing.Lax)
					{
						attributeMatchState = AttributeMatchState.AnyAttributeLax;
					}
				}
				else
				{
					attributeMatchState = AttributeMatchState.AnyAttributeSkip;
				}
			}
			else if (ed.ProhibitedAttributes.ContainsKey(qname))
			{
				attributeMatchState = AttributeMatchState.ProhibitedAttribute;
			}
		}
		else if (partialValidationType != null)
		{
			if (partialValidationType is XmlSchemaAttribute xmlSchemaAttribute)
			{
				if (qname.Equals(xmlSchemaAttribute.QualifiedName))
				{
					value = xmlSchemaAttribute.AttDef;
					attributeMatchState = AttributeMatchState.AttributeFound;
				}
				else
				{
					attributeMatchState = AttributeMatchState.AttributeNameMismatch;
				}
			}
			else
			{
				attributeMatchState = AttributeMatchState.ValidateAttributeInvalidCall;
			}
		}
		else if (_attributeDecls.TryGetValue(qname, out value))
		{
			attributeMatchState = AttributeMatchState.AttributeFound;
		}
		else
		{
			attributeMatchState = AttributeMatchState.UndeclaredElementAndAttribute;
		}
		return value;
	}

	internal SchemaAttDef GetAttributeXsd(SchemaElementDecl ed, XmlQualifiedName qname, ref bool skip)
	{
		AttributeMatchState attributeMatchState;
		SchemaAttDef attributeXsd = GetAttributeXsd(ed, qname, null, out attributeMatchState);
		switch (attributeMatchState)
		{
		case AttributeMatchState.UndeclaredAttribute:
			throw new XmlSchemaException(System.SR.Sch_UndeclaredAttribute, qname.ToString());
		case AttributeMatchState.ProhibitedAnyAttribute:
		case AttributeMatchState.ProhibitedAttribute:
			throw new XmlSchemaException(System.SR.Sch_ProhibitedAttribute, qname.ToString());
		case AttributeMatchState.AnyAttributeSkip:
			skip = true;
			break;
		}
		return attributeXsd;
	}

	internal void Add(SchemaInfo sinfo, ValidationEventHandler eventhandler)
	{
		if (_schemaType == SchemaType.None)
		{
			_schemaType = sinfo.SchemaType;
		}
		else if (_schemaType != sinfo.SchemaType)
		{
			eventhandler?.Invoke(this, new ValidationEventArgs(new XmlSchemaException(System.SR.Sch_MixSchemaTypes, string.Empty)));
			return;
		}
		foreach (string key in sinfo.TargetNamespaces.Keys)
		{
			if (!_targetNamespaces.ContainsKey(key))
			{
				_targetNamespaces.Add(key, value: true);
			}
		}
		foreach (KeyValuePair<XmlQualifiedName, SchemaElementDecl> elementDecl in sinfo._elementDecls)
		{
			if (!_elementDecls.ContainsKey(elementDecl.Key))
			{
				_elementDecls.Add(elementDecl.Key, elementDecl.Value);
			}
		}
		foreach (KeyValuePair<XmlQualifiedName, SchemaElementDecl> item in sinfo._elementDeclsByType)
		{
			if (!_elementDeclsByType.ContainsKey(item.Key))
			{
				_elementDeclsByType.Add(item.Key, item.Value);
			}
		}
		foreach (SchemaAttDef value in sinfo.AttributeDecls.Values)
		{
			if (!_attributeDecls.ContainsKey(value.Name))
			{
				_attributeDecls.Add(value.Name, value);
			}
		}
		foreach (SchemaNotation value2 in sinfo.Notations.Values)
		{
			if (!Notations.ContainsKey(value2.Name.Name))
			{
				Notations.Add(value2.Name.Name, value2);
			}
		}
	}

	internal void Finish()
	{
		Dictionary<XmlQualifiedName, SchemaElementDecl> dictionary = _elementDecls;
		for (int i = 0; i < 2; i++)
		{
			foreach (SchemaElementDecl value in dictionary.Values)
			{
				if (value.HasNonCDataAttribute)
				{
					_hasNonCDataAttributes = true;
				}
				if (value.DefaultAttDefs != null)
				{
					_hasDefaultAttributes = true;
				}
			}
			dictionary = _undeclaredElementDecls;
		}
	}

	IDtdAttributeListInfo IDtdInfo.LookupAttributeList(string prefix, string localName)
	{
		XmlQualifiedName key = new XmlQualifiedName(prefix, localName);
		if (!_elementDecls.TryGetValue(key, out var value))
		{
			_undeclaredElementDecls.TryGetValue(key, out value);
		}
		return value;
	}

	IEnumerable<IDtdAttributeListInfo> IDtdInfo.GetAttributeLists()
	{
		foreach (SchemaElementDecl value in _elementDecls.Values)
		{
			yield return value;
		}
	}

	IDtdEntityInfo IDtdInfo.LookupEntity(string name)
	{
		if (_generalEntities == null)
		{
			return null;
		}
		XmlQualifiedName key = new XmlQualifiedName(name);
		if (_generalEntities.TryGetValue(key, out var value))
		{
			return value;
		}
		return null;
	}
}
