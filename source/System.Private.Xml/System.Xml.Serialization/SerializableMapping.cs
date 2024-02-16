using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Xml.Schema;

namespace System.Xml.Serialization;

internal sealed class SerializableMapping : SpecialMapping
{
	private XmlSchema _schema;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	private Type _type;

	private bool _needSchema = true;

	private readonly MethodInfo _getSchemaMethod;

	private XmlQualifiedName _xsiType;

	private XmlSchemaType _xsdType;

	private XmlSchemaSet _schemas;

	private bool _any;

	private string _namespaces;

	private SerializableMapping _baseMapping;

	private SerializableMapping _derivedMappings;

	private SerializableMapping _nextDerivedMapping;

	private SerializableMapping _next;

	internal bool IsAny
	{
		get
		{
			if (_any)
			{
				return true;
			}
			if (_getSchemaMethod == null)
			{
				return false;
			}
			if (_needSchema && typeof(XmlSchemaType).IsAssignableFrom(_getSchemaMethod.ReturnType))
			{
				return false;
			}
			RetrieveSerializableSchema();
			return _any;
		}
	}

	internal string NamespaceList
	{
		get
		{
			RetrieveSerializableSchema();
			if (_namespaces == null)
			{
				if (_schemas != null)
				{
					StringBuilder stringBuilder = new StringBuilder();
					foreach (XmlSchema item in _schemas.Schemas())
					{
						if (item.TargetNamespace != null && item.TargetNamespace.Length > 0)
						{
							if (stringBuilder.Length > 0)
							{
								stringBuilder.Append(' ');
							}
							stringBuilder.Append(item.TargetNamespace);
						}
					}
					_namespaces = stringBuilder.ToString();
				}
				else
				{
					_namespaces = string.Empty;
				}
			}
			return _namespaces;
		}
	}

	internal SerializableMapping DerivedMappings => _derivedMappings;

	internal SerializableMapping NextDerivedMapping => _nextDerivedMapping;

	internal SerializableMapping Next
	{
		get
		{
			return _next;
		}
		set
		{
			_next = value;
		}
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	internal Type Type
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

	internal XmlSchemaSet Schemas
	{
		get
		{
			RetrieveSerializableSchema();
			return _schemas;
		}
	}

	internal XmlSchema Schema
	{
		get
		{
			RetrieveSerializableSchema();
			return _schema;
		}
	}

	internal XmlQualifiedName XsiType
	{
		get
		{
			if (!_needSchema)
			{
				return _xsiType;
			}
			if (_getSchemaMethod == null)
			{
				return null;
			}
			if (typeof(XmlSchemaType).IsAssignableFrom(_getSchemaMethod.ReturnType))
			{
				return null;
			}
			RetrieveSerializableSchema();
			return _xsiType;
		}
	}

	internal XmlSchemaType XsdType
	{
		get
		{
			RetrieveSerializableSchema();
			return _xsdType;
		}
	}

	internal SerializableMapping()
	{
	}

	internal SerializableMapping(MethodInfo getSchemaMethod, bool any, string ns)
	{
		_getSchemaMethod = getSchemaMethod;
		_any = any;
		base.Namespace = ns;
		_needSchema = getSchemaMethod != null;
	}

	internal SerializableMapping(XmlQualifiedName xsiType, XmlSchemaSet schemas)
	{
		_xsiType = xsiType;
		_schemas = schemas;
		base.TypeName = xsiType.Name;
		base.Namespace = xsiType.Namespace;
		_needSchema = false;
	}

	internal void SetBaseMapping(SerializableMapping mapping)
	{
		_baseMapping = mapping;
		if (_baseMapping != null)
		{
			_nextDerivedMapping = _baseMapping._derivedMappings;
			_baseMapping._derivedMappings = this;
			if (this == _nextDerivedMapping)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlCircularDerivation, base.TypeDesc.FullName));
			}
		}
	}

	internal static void ValidationCallbackWithErrorCode(object sender, ValidationEventArgs args)
	{
		if (args.Severity == XmlSeverityType.Error)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlSerializableSchemaError, "IXmlSerializable", args.Message));
		}
	}

	internal void CheckDuplicateElement(XmlSchemaElement element, string elementNs)
	{
		if (element == null || element.Parent == null || !(element.Parent is XmlSchema))
		{
			return;
		}
		XmlSchemaObjectTable xmlSchemaObjectTable = null;
		if (Schema != null && Schema.TargetNamespace == elementNs)
		{
			XmlSchemas.Preprocess(Schema);
			xmlSchemaObjectTable = Schema.Elements;
		}
		else
		{
			if (Schemas == null)
			{
				return;
			}
			xmlSchemaObjectTable = Schemas.GlobalElements;
		}
		foreach (XmlSchemaElement value in xmlSchemaObjectTable.Values)
		{
			if (value.Name == element.Name && value.QualifiedName.Namespace == elementNs)
			{
				if (Match(value, element))
				{
					break;
				}
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlSerializableRootDupName, _getSchemaMethod.DeclaringType.FullName, value.Name, elementNs));
			}
		}
	}

	private bool Match(XmlSchemaElement e1, XmlSchemaElement e2)
	{
		if (e1.IsNillable != e2.IsNillable)
		{
			return false;
		}
		if (e1.RefName != e2.RefName)
		{
			return false;
		}
		if (e1.SchemaType != e2.SchemaType)
		{
			return false;
		}
		if (e1.SchemaTypeName != e2.SchemaTypeName)
		{
			return false;
		}
		if (e1.MinOccurs != e2.MinOccurs)
		{
			return false;
		}
		if (e1.MaxOccurs != e2.MaxOccurs)
		{
			return false;
		}
		if (e1.IsAbstract != e2.IsAbstract)
		{
			return false;
		}
		if (e1.DefaultValue != e2.DefaultValue)
		{
			return false;
		}
		if (e1.SubstitutionGroup != e2.SubstitutionGroup)
		{
			return false;
		}
		return true;
	}

	private void RetrieveSerializableSchema()
	{
		if (!_needSchema)
		{
			return;
		}
		_needSchema = false;
		if (_getSchemaMethod != null)
		{
			if (_schemas == null)
			{
				_schemas = new XmlSchemaSet();
			}
			object obj = _getSchemaMethod.Invoke(null, new object[1] { _schemas });
			_xsiType = XmlQualifiedName.Empty;
			if (obj != null)
			{
				if (typeof(XmlSchemaType).IsAssignableFrom(_getSchemaMethod.ReturnType))
				{
					_xsdType = (XmlSchemaType)obj;
					_xsiType = _xsdType.QualifiedName;
				}
				else
				{
					if (!typeof(XmlQualifiedName).IsAssignableFrom(_getSchemaMethod.ReturnType))
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlGetSchemaMethodReturnType, _type.Name, _getSchemaMethod.Name, "XmlSchemaProviderAttribute", typeof(XmlQualifiedName).FullName));
					}
					_xsiType = (XmlQualifiedName)obj;
					if (_xsiType.IsEmpty)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlGetSchemaEmptyTypeName, _type.FullName, _getSchemaMethod.Name));
					}
				}
			}
			else
			{
				_any = true;
			}
			_schemas.ValidationEventHandler += ValidationCallbackWithErrorCode;
			_schemas.Compile();
			if (!_xsiType.IsEmpty && _xsiType.Namespace != "http://www.w3.org/2001/XMLSchema")
			{
				ArrayList arrayList = (ArrayList)_schemas.Schemas(_xsiType.Namespace);
				if (arrayList.Count == 0)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlMissingSchema, _xsiType.Namespace));
				}
				if (arrayList.Count > 1)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlGetSchemaInclude, _xsiType.Namespace, _getSchemaMethod.DeclaringType.FullName, _getSchemaMethod.Name));
				}
				XmlSchema xmlSchema = (XmlSchema)arrayList[0];
				if (xmlSchema == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlMissingSchema, _xsiType.Namespace));
				}
				_xsdType = (XmlSchemaType)xmlSchema.SchemaTypes[_xsiType];
				if (_xsdType == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlGetSchemaTypeMissing, _getSchemaMethod.DeclaringType.FullName, _getSchemaMethod.Name, _xsiType.Name, _xsiType.Namespace));
				}
				_xsdType = ((_xsdType.Redefined != null) ? _xsdType.Redefined : _xsdType);
			}
		}
		else
		{
			IXmlSerializable xmlSerializable = (IXmlSerializable)Activator.CreateInstance(_type);
			_schema = xmlSerializable.GetSchema();
			if (_schema != null && (_schema.Id == null || _schema.Id.Length == 0))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlSerializableNameMissing1, _type.FullName));
			}
		}
	}
}
