using System.Collections;

namespace System.Xml.Schema;

public sealed class XmlSchemaInference
{
	public enum InferenceOption
	{
		Restricted,
		Relaxed
	}

	internal static XmlQualifiedName ST_boolean = new XmlQualifiedName("boolean", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_byte = new XmlQualifiedName("byte", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_unsignedByte = new XmlQualifiedName("unsignedByte", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_short = new XmlQualifiedName("short", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_unsignedShort = new XmlQualifiedName("unsignedShort", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_int = new XmlQualifiedName("int", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_unsignedInt = new XmlQualifiedName("unsignedInt", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_long = new XmlQualifiedName("long", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_unsignedLong = new XmlQualifiedName("unsignedLong", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_integer = new XmlQualifiedName("integer", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_decimal = new XmlQualifiedName("decimal", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_float = new XmlQualifiedName("float", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_double = new XmlQualifiedName("double", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_duration = new XmlQualifiedName("duration", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_dateTime = new XmlQualifiedName("dateTime", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_time = new XmlQualifiedName("time", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_date = new XmlQualifiedName("date", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_gYearMonth = new XmlQualifiedName("gYearMonth", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_string = new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName ST_anySimpleType = new XmlQualifiedName("anySimpleType", "http://www.w3.org/2001/XMLSchema");

	internal static XmlQualifiedName[] SimpleTypes = new XmlQualifiedName[19]
	{
		ST_boolean, ST_byte, ST_unsignedByte, ST_short, ST_unsignedShort, ST_int, ST_unsignedInt, ST_long, ST_unsignedLong, ST_integer,
		ST_decimal, ST_float, ST_double, ST_duration, ST_dateTime, ST_time, ST_date, ST_gYearMonth, ST_string
	};

	private XmlSchema _rootSchema;

	private XmlSchemaSet _schemaSet;

	private XmlReader _xtr;

	private readonly NameTable _nametable;

	private string _targetNamespace;

	private readonly XmlNamespaceManager _namespaceManager;

	private readonly ArrayList _schemaList;

	private InferenceOption _occurrence;

	private InferenceOption _typeInference;

	public InferenceOption Occurrence
	{
		get
		{
			return _occurrence;
		}
		set
		{
			_occurrence = value;
		}
	}

	public InferenceOption TypeInference
	{
		get
		{
			return _typeInference;
		}
		set
		{
			_typeInference = value;
		}
	}

	public XmlSchemaInference()
	{
		_nametable = new NameTable();
		_namespaceManager = new XmlNamespaceManager(_nametable);
		_namespaceManager.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
		_schemaList = new ArrayList();
	}

	public XmlSchemaSet InferSchema(XmlReader instanceDocument)
	{
		return InferSchema1(instanceDocument, new XmlSchemaSet(_nametable));
	}

	public XmlSchemaSet InferSchema(XmlReader instanceDocument, XmlSchemaSet schemas)
	{
		if (schemas == null)
		{
			schemas = new XmlSchemaSet(_nametable);
		}
		return InferSchema1(instanceDocument, schemas);
	}

	internal XmlSchemaSet InferSchema1(XmlReader instanceDocument, XmlSchemaSet schemas)
	{
		if (instanceDocument == null)
		{
			throw new ArgumentNullException("instanceDocument");
		}
		_rootSchema = null;
		_xtr = instanceDocument;
		schemas.Compile();
		_schemaSet = schemas;
		while (_xtr.NodeType != XmlNodeType.Element && _xtr.Read())
		{
		}
		if (_xtr.NodeType == XmlNodeType.Element)
		{
			_targetNamespace = _xtr.NamespaceURI;
			if (_xtr.NamespaceURI == "http://www.w3.org/2001/XMLSchema")
			{
				throw new XmlSchemaInferenceException(System.SR.SchInf_schema, 0, 0);
			}
			XmlSchemaElement xse = null;
			foreach (XmlSchemaElement value in schemas.GlobalElements.Values)
			{
				if (value.Name == _xtr.LocalName && value.QualifiedName.Namespace == _xtr.NamespaceURI)
				{
					_rootSchema = value.Parent as XmlSchema;
					xse = value;
					break;
				}
			}
			if (_rootSchema == null)
			{
				xse = AddElement(_xtr.LocalName, _xtr.Prefix, _xtr.NamespaceURI, null, null, -1);
			}
			else
			{
				InferElement(xse, bCreatingNewType: false, _rootSchema);
			}
			foreach (string item in _namespaceManager)
			{
				if (!item.Equals("xml") && !item.Equals("xmlns"))
				{
					string text2 = _namespaceManager.LookupNamespace(_nametable.Get(item));
					if (text2.Length != 0)
					{
						_rootSchema.Namespaces.Add(item, text2);
					}
				}
			}
			schemas.Reprocess(_rootSchema);
			schemas.Compile();
			return schemas;
		}
		throw new XmlSchemaInferenceException(System.SR.SchInf_NoElement, 0, 0);
	}

	private XmlSchemaAttribute AddAttribute(string localName, string prefix, string childURI, string attrValue, bool bCreatingNewType, XmlSchema parentSchema, XmlSchemaObjectCollection addLocation, XmlSchemaObjectTable compiledAttributes)
	{
		if (childURI == "http://www.w3.org/2001/XMLSchema")
		{
			throw new XmlSchemaInferenceException(System.SR.SchInf_schema, 0, 0);
		}
		XmlSchemaAttribute xmlSchemaAttribute = null;
		int iTypeFlags = -1;
		XmlSchemaAttribute xmlSchemaAttribute2 = null;
		XmlSchema xmlSchema = null;
		bool flag = true;
		ICollection attributes;
		ICollection collection;
		if (compiledAttributes.Count > 0)
		{
			attributes = compiledAttributes.Values;
			collection = addLocation;
		}
		else
		{
			attributes = addLocation;
			collection = null;
		}
		if (childURI == "http://www.w3.org/XML/1998/namespace")
		{
			XmlSchemaAttribute xmlSchemaAttribute3 = null;
			xmlSchemaAttribute3 = FindAttributeRef(attributes, localName, childURI);
			if (xmlSchemaAttribute3 == null && collection != null)
			{
				xmlSchemaAttribute3 = FindAttributeRef(collection, localName, childURI);
			}
			if (xmlSchemaAttribute3 == null)
			{
				xmlSchemaAttribute3 = new XmlSchemaAttribute();
				xmlSchemaAttribute3.RefName = new XmlQualifiedName(localName, childURI);
				if (bCreatingNewType && Occurrence == InferenceOption.Restricted)
				{
					xmlSchemaAttribute3.Use = XmlSchemaUse.Required;
				}
				else
				{
					xmlSchemaAttribute3.Use = XmlSchemaUse.Optional;
				}
				addLocation.Add(xmlSchemaAttribute3);
			}
			xmlSchemaAttribute2 = xmlSchemaAttribute3;
		}
		else
		{
			if (childURI.Length == 0)
			{
				xmlSchema = parentSchema;
				flag = false;
			}
			else if (childURI != null && !_schemaSet.Contains(childURI))
			{
				xmlSchema = new XmlSchema();
				xmlSchema.AttributeFormDefault = XmlSchemaForm.Unqualified;
				xmlSchema.ElementFormDefault = XmlSchemaForm.Qualified;
				if (childURI.Length != 0)
				{
					xmlSchema.TargetNamespace = childURI;
				}
				_schemaSet.Add(xmlSchema);
				if (prefix.Length != 0 && !string.Equals(prefix, "xml", StringComparison.OrdinalIgnoreCase))
				{
					_namespaceManager.AddNamespace(prefix, childURI);
				}
			}
			else if (_schemaSet.Schemas(childURI) is ArrayList { Count: >0 } arrayList)
			{
				xmlSchema = arrayList[0] as XmlSchema;
			}
			if (childURI.Length != 0)
			{
				XmlSchemaAttribute xmlSchemaAttribute4 = null;
				xmlSchemaAttribute4 = FindAttributeRef(attributes, localName, childURI);
				if (xmlSchemaAttribute4 == null && collection != null)
				{
					xmlSchemaAttribute4 = FindAttributeRef(collection, localName, childURI);
				}
				if (xmlSchemaAttribute4 == null)
				{
					xmlSchemaAttribute4 = new XmlSchemaAttribute();
					xmlSchemaAttribute4.RefName = new XmlQualifiedName(localName, childURI);
					if (bCreatingNewType && Occurrence == InferenceOption.Restricted)
					{
						xmlSchemaAttribute4.Use = XmlSchemaUse.Required;
					}
					else
					{
						xmlSchemaAttribute4.Use = XmlSchemaUse.Optional;
					}
					addLocation.Add(xmlSchemaAttribute4);
				}
				xmlSchemaAttribute2 = xmlSchemaAttribute4;
				xmlSchemaAttribute = FindAttribute(xmlSchema.Items, localName);
				if (xmlSchemaAttribute == null)
				{
					xmlSchemaAttribute = new XmlSchemaAttribute();
					xmlSchemaAttribute.Name = localName;
					xmlSchemaAttribute.SchemaTypeName = RefineSimpleType(attrValue, ref iTypeFlags);
					xmlSchemaAttribute.LineNumber = iTypeFlags;
					xmlSchema.Items.Add(xmlSchemaAttribute);
				}
				else
				{
					if (xmlSchemaAttribute.Parent == null)
					{
						iTypeFlags = xmlSchemaAttribute.LineNumber;
					}
					else
					{
						iTypeFlags = GetSchemaType(xmlSchemaAttribute.SchemaTypeName);
						xmlSchemaAttribute.Parent = null;
					}
					xmlSchemaAttribute.SchemaTypeName = RefineSimpleType(attrValue, ref iTypeFlags);
					xmlSchemaAttribute.LineNumber = iTypeFlags;
				}
			}
			else
			{
				xmlSchemaAttribute = FindAttribute(attributes, localName);
				if (xmlSchemaAttribute == null && collection != null)
				{
					xmlSchemaAttribute = FindAttribute(collection, localName);
				}
				if (xmlSchemaAttribute == null)
				{
					xmlSchemaAttribute = new XmlSchemaAttribute();
					xmlSchemaAttribute.Name = localName;
					xmlSchemaAttribute.SchemaTypeName = RefineSimpleType(attrValue, ref iTypeFlags);
					xmlSchemaAttribute.LineNumber = iTypeFlags;
					if (bCreatingNewType && Occurrence == InferenceOption.Restricted)
					{
						xmlSchemaAttribute.Use = XmlSchemaUse.Required;
					}
					else
					{
						xmlSchemaAttribute.Use = XmlSchemaUse.Optional;
					}
					addLocation.Add(xmlSchemaAttribute);
					if (xmlSchema.AttributeFormDefault != XmlSchemaForm.Unqualified)
					{
						xmlSchemaAttribute.Form = XmlSchemaForm.Unqualified;
					}
				}
				else
				{
					if (xmlSchemaAttribute.Parent == null)
					{
						iTypeFlags = xmlSchemaAttribute.LineNumber;
					}
					else
					{
						iTypeFlags = GetSchemaType(xmlSchemaAttribute.SchemaTypeName);
						xmlSchemaAttribute.Parent = null;
					}
					xmlSchemaAttribute.SchemaTypeName = RefineSimpleType(attrValue, ref iTypeFlags);
					xmlSchemaAttribute.LineNumber = iTypeFlags;
				}
				xmlSchemaAttribute2 = xmlSchemaAttribute;
			}
		}
		string @namespace = null;
		if (flag && childURI != parentSchema.TargetNamespace)
		{
			for (int i = 0; i < parentSchema.Includes.Count; i++)
			{
				if (parentSchema.Includes[i] is XmlSchemaImport xmlSchemaImport && xmlSchemaImport.Namespace == childURI)
				{
					flag = false;
				}
			}
			if (flag)
			{
				XmlSchemaImport xmlSchemaImport2 = new XmlSchemaImport();
				xmlSchemaImport2.Schema = xmlSchema;
				if (childURI.Length != 0)
				{
					@namespace = childURI;
				}
				xmlSchemaImport2.Namespace = @namespace;
				parentSchema.Includes.Add(xmlSchemaImport2);
			}
		}
		return xmlSchemaAttribute2;
	}

	private XmlSchema CreateXmlSchema(string targetNS)
	{
		XmlSchema xmlSchema = new XmlSchema();
		xmlSchema.AttributeFormDefault = XmlSchemaForm.Unqualified;
		xmlSchema.ElementFormDefault = XmlSchemaForm.Qualified;
		xmlSchema.TargetNamespace = targetNS;
		_schemaSet.Add(xmlSchema);
		return xmlSchema;
	}

	private XmlSchemaElement AddElement(string localName, string prefix, string childURI, XmlSchema parentSchema, XmlSchemaObjectCollection addLocation, int positionWithinCollection)
	{
		if (childURI == "http://www.w3.org/2001/XMLSchema")
		{
			throw new XmlSchemaInferenceException(System.SR.SchInf_schema, 0, 0);
		}
		XmlSchemaElement xmlSchemaElement = null;
		XmlSchemaElement xmlSchemaElement2 = xmlSchemaElement;
		XmlSchema parentSchema2 = null;
		bool bCreatingNewType = true;
		if (childURI == string.Empty)
		{
			childURI = null;
		}
		if (parentSchema != null && childURI == parentSchema.TargetNamespace)
		{
			xmlSchemaElement = new XmlSchemaElement();
			xmlSchemaElement.Name = localName;
			parentSchema2 = parentSchema;
			if (parentSchema2.ElementFormDefault != XmlSchemaForm.Qualified && addLocation != null)
			{
				xmlSchemaElement.Form = XmlSchemaForm.Qualified;
			}
		}
		else if (_schemaSet.Contains(childURI))
		{
			xmlSchemaElement = FindGlobalElement(childURI, localName, out parentSchema2);
			if (xmlSchemaElement == null)
			{
				if (_schemaSet.Schemas(childURI) is ArrayList { Count: >0 } arrayList)
				{
					parentSchema2 = arrayList[0] as XmlSchema;
				}
				xmlSchemaElement = new XmlSchemaElement();
				xmlSchemaElement.Name = localName;
				parentSchema2.Items.Add(xmlSchemaElement);
			}
			else
			{
				bCreatingNewType = false;
			}
		}
		else
		{
			parentSchema2 = CreateXmlSchema(childURI);
			if (prefix.Length != 0)
			{
				_namespaceManager.AddNamespace(prefix, childURI);
			}
			xmlSchemaElement = new XmlSchemaElement();
			xmlSchemaElement.Name = localName;
			parentSchema2.Items.Add(xmlSchemaElement);
		}
		if (parentSchema == null)
		{
			parentSchema = parentSchema2;
			_rootSchema = parentSchema;
		}
		if (childURI != parentSchema.TargetNamespace)
		{
			bool flag = true;
			for (int i = 0; i < parentSchema.Includes.Count; i++)
			{
				if (parentSchema.Includes[i] is XmlSchemaImport xmlSchemaImport && xmlSchemaImport.Namespace == childURI)
				{
					flag = false;
				}
			}
			if (flag)
			{
				XmlSchemaImport xmlSchemaImport2 = new XmlSchemaImport();
				xmlSchemaImport2.Schema = parentSchema2;
				xmlSchemaImport2.Namespace = childURI;
				parentSchema.Includes.Add(xmlSchemaImport2);
			}
		}
		xmlSchemaElement2 = xmlSchemaElement;
		if (addLocation != null)
		{
			if (childURI == parentSchema.TargetNamespace)
			{
				if (Occurrence == InferenceOption.Relaxed)
				{
					xmlSchemaElement.MinOccurs = 0m;
				}
				if (positionWithinCollection == -1)
				{
					positionWithinCollection = addLocation.Add(xmlSchemaElement);
				}
				else
				{
					addLocation.Insert(positionWithinCollection, xmlSchemaElement);
				}
			}
			else
			{
				XmlSchemaElement xmlSchemaElement3 = new XmlSchemaElement();
				xmlSchemaElement3.RefName = new XmlQualifiedName(localName, childURI);
				if (Occurrence == InferenceOption.Relaxed)
				{
					xmlSchemaElement3.MinOccurs = 0m;
				}
				if (positionWithinCollection == -1)
				{
					positionWithinCollection = addLocation.Add(xmlSchemaElement3);
				}
				else
				{
					addLocation.Insert(positionWithinCollection, xmlSchemaElement3);
				}
				xmlSchemaElement2 = xmlSchemaElement3;
			}
		}
		InferElement(xmlSchemaElement, bCreatingNewType, parentSchema2);
		return xmlSchemaElement2;
	}

	internal void InferElement(XmlSchemaElement xse, bool bCreatingNewType, XmlSchema parentSchema)
	{
		bool isEmptyElement = _xtr.IsEmptyElement;
		int lastUsedSeqItem = -1;
		Hashtable hashtable = new Hashtable();
		XmlSchemaType effectiveSchemaType = GetEffectiveSchemaType(xse, bCreatingNewType);
		XmlSchemaComplexType xmlSchemaComplexType = effectiveSchemaType as XmlSchemaComplexType;
		if (_xtr.MoveToFirstAttribute())
		{
			ProcessAttributes(ref xse, effectiveSchemaType, bCreatingNewType, parentSchema);
		}
		else if (!bCreatingNewType && xmlSchemaComplexType != null)
		{
			MakeExistingAttributesOptional(xmlSchemaComplexType, null);
		}
		if (xmlSchemaComplexType == null || xmlSchemaComplexType == XmlSchemaComplexType.AnyType)
		{
			xmlSchemaComplexType = xse.SchemaType as XmlSchemaComplexType;
		}
		if (isEmptyElement)
		{
			if (!bCreatingNewType)
			{
				if (xmlSchemaComplexType != null)
				{
					if (xmlSchemaComplexType.Particle != null)
					{
						xmlSchemaComplexType.Particle.MinOccurs = 0m;
					}
					else if (xmlSchemaComplexType.ContentModel != null)
					{
						XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension = CheckSimpleContentExtension(xmlSchemaComplexType);
						xmlSchemaSimpleContentExtension.BaseTypeName = ST_string;
						xmlSchemaSimpleContentExtension.LineNumber = 262144;
					}
				}
				else if (!xse.SchemaTypeName.IsEmpty)
				{
					xse.LineNumber = 262144;
					xse.SchemaTypeName = ST_string;
				}
			}
			else
			{
				xse.LineNumber = 262144;
			}
			return;
		}
		bool flag = false;
		do
		{
			_xtr.Read();
			if (_xtr.NodeType == XmlNodeType.Whitespace)
			{
				flag = true;
			}
			if (_xtr.NodeType == XmlNodeType.EntityReference)
			{
				throw new XmlSchemaInferenceException(System.SR.SchInf_entity, 0, 0);
			}
		}
		while (!_xtr.EOF && _xtr.NodeType != XmlNodeType.EndElement && _xtr.NodeType != XmlNodeType.CDATA && _xtr.NodeType != XmlNodeType.Element && _xtr.NodeType != XmlNodeType.Text);
		if (_xtr.NodeType == XmlNodeType.EndElement)
		{
			if (flag)
			{
				if (xmlSchemaComplexType != null)
				{
					if (xmlSchemaComplexType.ContentModel != null)
					{
						XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension2 = CheckSimpleContentExtension(xmlSchemaComplexType);
						xmlSchemaSimpleContentExtension2.BaseTypeName = ST_string;
						xmlSchemaSimpleContentExtension2.LineNumber = 262144;
					}
					else if (bCreatingNewType)
					{
						XmlSchemaSimpleContent xmlSchemaSimpleContent = (XmlSchemaSimpleContent)(xmlSchemaComplexType.ContentModel = new XmlSchemaSimpleContent());
						XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension3 = (XmlSchemaSimpleContentExtension)(xmlSchemaSimpleContent.Content = new XmlSchemaSimpleContentExtension());
						MoveAttributes(xmlSchemaComplexType, xmlSchemaSimpleContentExtension3, bCreatingNewType);
						xmlSchemaSimpleContentExtension3.BaseTypeName = ST_string;
						xmlSchemaSimpleContentExtension3.LineNumber = 262144;
					}
					else
					{
						xmlSchemaComplexType.IsMixed = true;
					}
				}
				else
				{
					xse.SchemaTypeName = ST_string;
					xse.LineNumber = 262144;
				}
			}
			if (bCreatingNewType)
			{
				xse.LineNumber = 262144;
			}
			else if (xmlSchemaComplexType != null)
			{
				if (xmlSchemaComplexType.Particle != null)
				{
					xmlSchemaComplexType.Particle.MinOccurs = 0m;
				}
				else if (xmlSchemaComplexType.ContentModel != null)
				{
					XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension4 = CheckSimpleContentExtension(xmlSchemaComplexType);
					xmlSchemaSimpleContentExtension4.BaseTypeName = ST_string;
					xmlSchemaSimpleContentExtension4.LineNumber = 262144;
				}
			}
			else if (!xse.SchemaTypeName.IsEmpty)
			{
				xse.LineNumber = 262144;
				xse.SchemaTypeName = ST_string;
			}
			return;
		}
		int num = 0;
		bool flag2 = false;
		while (!_xtr.EOF && _xtr.NodeType != XmlNodeType.EndElement)
		{
			bool flag3 = false;
			num++;
			if (_xtr.NodeType == XmlNodeType.Text || _xtr.NodeType == XmlNodeType.CDATA)
			{
				if (xmlSchemaComplexType != null)
				{
					if (xmlSchemaComplexType.Particle != null)
					{
						xmlSchemaComplexType.IsMixed = true;
						if (num == 1)
						{
							do
							{
								_xtr.Read();
							}
							while (!_xtr.EOF && (_xtr.NodeType == XmlNodeType.CDATA || _xtr.NodeType == XmlNodeType.Text || _xtr.NodeType == XmlNodeType.Comment || _xtr.NodeType == XmlNodeType.ProcessingInstruction || _xtr.NodeType == XmlNodeType.Whitespace || _xtr.NodeType == XmlNodeType.SignificantWhitespace || _xtr.NodeType == XmlNodeType.XmlDeclaration));
							flag3 = true;
							if (_xtr.NodeType == XmlNodeType.EndElement)
							{
								xmlSchemaComplexType.Particle.MinOccurs = 0m;
							}
						}
					}
					else if (xmlSchemaComplexType.ContentModel != null)
					{
						XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension5 = CheckSimpleContentExtension(xmlSchemaComplexType);
						if (_xtr.NodeType == XmlNodeType.Text && num == 1)
						{
							int num2 = -1;
							if (xse.Parent == null)
							{
								num2 = xmlSchemaSimpleContentExtension5.LineNumber;
							}
							else
							{
								num2 = GetSchemaType(xmlSchemaSimpleContentExtension5.BaseTypeName);
								xse.Parent = null;
							}
							xmlSchemaSimpleContentExtension5.BaseTypeName = RefineSimpleType(_xtr.Value, ref num2);
							xmlSchemaSimpleContentExtension5.LineNumber = num2;
						}
						else
						{
							xmlSchemaSimpleContentExtension5.BaseTypeName = ST_string;
							xmlSchemaSimpleContentExtension5.LineNumber = 262144;
						}
					}
					else
					{
						XmlSchemaSimpleContent xmlSchemaSimpleContent2 = (XmlSchemaSimpleContent)(xmlSchemaComplexType.ContentModel = new XmlSchemaSimpleContent());
						XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension6 = (XmlSchemaSimpleContentExtension)(xmlSchemaSimpleContent2.Content = new XmlSchemaSimpleContentExtension());
						MoveAttributes(xmlSchemaComplexType, xmlSchemaSimpleContentExtension6, bCreatingNewType);
						if (_xtr.NodeType == XmlNodeType.Text)
						{
							int iTypeFlags = (bCreatingNewType ? (-1) : 262144);
							xmlSchemaSimpleContentExtension6.BaseTypeName = RefineSimpleType(_xtr.Value, ref iTypeFlags);
							xmlSchemaSimpleContentExtension6.LineNumber = iTypeFlags;
						}
						else
						{
							xmlSchemaSimpleContentExtension6.BaseTypeName = ST_string;
							xmlSchemaSimpleContentExtension6.LineNumber = 262144;
						}
					}
				}
				else if (num > 1)
				{
					xse.SchemaTypeName = ST_string;
					xse.LineNumber = 262144;
				}
				else
				{
					int iTypeFlags2 = -1;
					if (bCreatingNewType)
					{
						if (_xtr.NodeType == XmlNodeType.Text)
						{
							xse.SchemaTypeName = RefineSimpleType(_xtr.Value, ref iTypeFlags2);
							xse.LineNumber = iTypeFlags2;
						}
						else
						{
							xse.SchemaTypeName = ST_string;
							xse.LineNumber = 262144;
						}
					}
					else if (_xtr.NodeType == XmlNodeType.Text)
					{
						if (xse.Parent == null)
						{
							iTypeFlags2 = xse.LineNumber;
						}
						else
						{
							iTypeFlags2 = GetSchemaType(xse.SchemaTypeName);
							if (iTypeFlags2 == -1 && xse.LineNumber == 262144)
							{
								iTypeFlags2 = 262144;
							}
							xse.Parent = null;
						}
						xse.SchemaTypeName = RefineSimpleType(_xtr.Value, ref iTypeFlags2);
						xse.LineNumber = iTypeFlags2;
					}
					else
					{
						xse.SchemaTypeName = ST_string;
						xse.LineNumber = 262144;
					}
				}
			}
			else if (_xtr.NodeType == XmlNodeType.Element)
			{
				XmlQualifiedName key = new XmlQualifiedName(_xtr.LocalName, _xtr.NamespaceURI);
				bool setMaxoccurs = false;
				if (hashtable.Contains(key))
				{
					setMaxoccurs = true;
				}
				else
				{
					hashtable.Add(key, null);
				}
				if (xmlSchemaComplexType == null)
				{
					xmlSchemaComplexType = (XmlSchemaComplexType)(xse.SchemaType = new XmlSchemaComplexType());
					if (!xse.SchemaTypeName.IsEmpty)
					{
						xmlSchemaComplexType.IsMixed = true;
						xse.SchemaTypeName = XmlQualifiedName.Empty;
					}
				}
				if (xmlSchemaComplexType.ContentModel != null)
				{
					XmlSchemaSimpleContentExtension scExtension = CheckSimpleContentExtension(xmlSchemaComplexType);
					MoveAttributes(scExtension, xmlSchemaComplexType);
					xmlSchemaComplexType.ContentModel = null;
					xmlSchemaComplexType.IsMixed = true;
					if (xmlSchemaComplexType.Particle != null)
					{
						throw new XmlSchemaInferenceException(System.SR.SchInf_particle, 0, 0);
					}
					xmlSchemaComplexType.Particle = new XmlSchemaSequence();
					flag2 = true;
					AddElement(_xtr.LocalName, _xtr.Prefix, _xtr.NamespaceURI, parentSchema, ((XmlSchemaSequence)xmlSchemaComplexType.Particle).Items, -1);
					lastUsedSeqItem = 0;
					if (!bCreatingNewType)
					{
						xmlSchemaComplexType.Particle.MinOccurs = 0m;
					}
				}
				else if (xmlSchemaComplexType.Particle == null)
				{
					xmlSchemaComplexType.Particle = new XmlSchemaSequence();
					flag2 = true;
					AddElement(_xtr.LocalName, _xtr.Prefix, _xtr.NamespaceURI, parentSchema, ((XmlSchemaSequence)xmlSchemaComplexType.Particle).Items, -1);
					if (!bCreatingNewType)
					{
						((XmlSchemaSequence)xmlSchemaComplexType.Particle).MinOccurs = 0m;
					}
					lastUsedSeqItem = 0;
				}
				else
				{
					bool bParticleChanged = false;
					FindMatchingElement(bCreatingNewType || flag2, _xtr, xmlSchemaComplexType, ref lastUsedSeqItem, ref bParticleChanged, parentSchema, setMaxoccurs);
				}
			}
			else if (_xtr.NodeType == XmlNodeType.Text)
			{
				if (xmlSchemaComplexType == null)
				{
					throw new XmlSchemaInferenceException(System.SR.SchInf_ct, 0, 0);
				}
				xmlSchemaComplexType.IsMixed = true;
			}
			do
			{
				if (_xtr.NodeType == XmlNodeType.EntityReference)
				{
					throw new XmlSchemaInferenceException(System.SR.SchInf_entity, 0, 0);
				}
				if (!flag3)
				{
					_xtr.Read();
				}
				else
				{
					flag3 = false;
				}
			}
			while (!_xtr.EOF && _xtr.NodeType != XmlNodeType.EndElement && _xtr.NodeType != XmlNodeType.CDATA && _xtr.NodeType != XmlNodeType.Element && _xtr.NodeType != XmlNodeType.Text);
		}
		if (lastUsedSeqItem == -1)
		{
			return;
		}
		while (++lastUsedSeqItem < ((XmlSchemaSequence)xmlSchemaComplexType.Particle).Items.Count)
		{
			if (((XmlSchemaSequence)xmlSchemaComplexType.Particle).Items[lastUsedSeqItem].GetType() != typeof(XmlSchemaElement))
			{
				throw new XmlSchemaInferenceException(System.SR.SchInf_seq, 0, 0);
			}
			XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)((XmlSchemaSequence)xmlSchemaComplexType.Particle).Items[lastUsedSeqItem];
			xmlSchemaElement.MinOccurs = 0m;
		}
	}

	private XmlSchemaSimpleContentExtension CheckSimpleContentExtension(XmlSchemaComplexType ct)
	{
		if (!(ct.ContentModel is XmlSchemaSimpleContent xmlSchemaSimpleContent))
		{
			throw new XmlSchemaInferenceException(System.SR.SchInf_simplecontent, 0, 0);
		}
		if (!(xmlSchemaSimpleContent.Content is XmlSchemaSimpleContentExtension result))
		{
			throw new XmlSchemaInferenceException(System.SR.SchInf_extension, 0, 0);
		}
		return result;
	}

	private XmlSchemaType GetEffectiveSchemaType(XmlSchemaElement elem, bool bCreatingNewType)
	{
		XmlSchemaType xmlSchemaType = null;
		if (!bCreatingNewType && elem.ElementSchemaType != null)
		{
			xmlSchemaType = elem.ElementSchemaType;
		}
		else if (elem.SchemaType != null)
		{
			xmlSchemaType = elem.SchemaType;
		}
		else if (elem.SchemaTypeName != XmlQualifiedName.Empty)
		{
			xmlSchemaType = _schemaSet.GlobalTypes[elem.SchemaTypeName] as XmlSchemaType;
			if (xmlSchemaType == null)
			{
				xmlSchemaType = XmlSchemaType.GetBuiltInSimpleType(elem.SchemaTypeName);
			}
			if (xmlSchemaType == null)
			{
				xmlSchemaType = XmlSchemaType.GetBuiltInComplexType(elem.SchemaTypeName);
			}
		}
		return xmlSchemaType;
	}

	internal XmlSchemaElement FindMatchingElement(bool bCreatingNewType, XmlReader xtr, XmlSchemaComplexType ct, ref int lastUsedSeqItem, ref bool bParticleChanged, XmlSchema parentSchema, bool setMaxoccurs)
	{
		if (xtr.NamespaceURI == "http://www.w3.org/2001/XMLSchema")
		{
			throw new XmlSchemaInferenceException(System.SR.SchInf_schema, 0, 0);
		}
		bool flag = lastUsedSeqItem == -1;
		XmlSchemaObjectCollection xmlSchemaObjectCollection = new XmlSchemaObjectCollection();
		if (ct.Particle.GetType() == typeof(XmlSchemaSequence))
		{
			string text = xtr.NamespaceURI;
			if (text.Length == 0)
			{
				text = null;
			}
			XmlSchemaSequence xmlSchemaSequence = (XmlSchemaSequence)ct.Particle;
			if (xmlSchemaSequence.Items.Count < 1 && !bCreatingNewType)
			{
				lastUsedSeqItem = 0;
				XmlSchemaElement xmlSchemaElement = AddElement(xtr.LocalName, xtr.Prefix, xtr.NamespaceURI, parentSchema, xmlSchemaSequence.Items, -1);
				xmlSchemaElement.MinOccurs = 0m;
				return xmlSchemaElement;
			}
			if (xmlSchemaSequence.Items[0].GetType() == typeof(XmlSchemaChoice))
			{
				XmlSchemaChoice xmlSchemaChoice = (XmlSchemaChoice)xmlSchemaSequence.Items[0];
				for (int i = 0; i < xmlSchemaChoice.Items.Count; i++)
				{
					if (!(xmlSchemaChoice.Items[i] is XmlSchemaElement xmlSchemaElement2))
					{
						throw new XmlSchemaInferenceException(System.SR.SchInf_UnknownParticle, 0, 0);
					}
					if (xmlSchemaElement2.Name == xtr.LocalName && parentSchema.TargetNamespace == text)
					{
						InferElement(xmlSchemaElement2, bCreatingNewType: false, parentSchema);
						SetMinMaxOccurs(xmlSchemaElement2, setMaxoccurs);
						return xmlSchemaElement2;
					}
					if (xmlSchemaElement2.RefName.Name == xtr.LocalName && xmlSchemaElement2.RefName.Namespace == xtr.NamespaceURI)
					{
						XmlSchemaElement xmlSchemaElement3 = FindGlobalElement(text, xtr.LocalName, out parentSchema);
						InferElement(xmlSchemaElement3, bCreatingNewType: false, parentSchema);
						SetMinMaxOccurs(xmlSchemaElement2, setMaxoccurs);
						return xmlSchemaElement3;
					}
				}
				return AddElement(xtr.LocalName, xtr.Prefix, xtr.NamespaceURI, parentSchema, xmlSchemaChoice.Items, -1);
			}
			int num = 0;
			if (lastUsedSeqItem >= 0)
			{
				num = lastUsedSeqItem;
			}
			XmlSchemaParticle xmlSchemaParticle = xmlSchemaSequence.Items[num] as XmlSchemaParticle;
			if (!(xmlSchemaParticle is XmlSchemaElement xmlSchemaElement4))
			{
				throw new XmlSchemaInferenceException(System.SR.SchInf_UnknownParticle, 0, 0);
			}
			if (xmlSchemaElement4.Name == xtr.LocalName && parentSchema.TargetNamespace == text)
			{
				if (!flag)
				{
					xmlSchemaElement4.MaxOccurs = decimal.MaxValue;
				}
				lastUsedSeqItem = num;
				InferElement(xmlSchemaElement4, bCreatingNewType: false, parentSchema);
				SetMinMaxOccurs(xmlSchemaElement4, setMaxOccurs: false);
				return xmlSchemaElement4;
			}
			if (xmlSchemaElement4.RefName.Name == xtr.LocalName && xmlSchemaElement4.RefName.Namespace == xtr.NamespaceURI)
			{
				if (!flag)
				{
					xmlSchemaElement4.MaxOccurs = decimal.MaxValue;
				}
				lastUsedSeqItem = num;
				XmlSchemaElement xse = FindGlobalElement(text, xtr.LocalName, out parentSchema);
				InferElement(xse, bCreatingNewType: false, parentSchema);
				SetMinMaxOccurs(xmlSchemaElement4, setMaxOccurs: false);
				return xmlSchemaElement4;
			}
			if (flag && xmlSchemaElement4.MinOccurs != 0m)
			{
				xmlSchemaObjectCollection.Add(xmlSchemaElement4);
			}
			for (num++; num < xmlSchemaSequence.Items.Count; num++)
			{
				xmlSchemaParticle = xmlSchemaSequence.Items[num] as XmlSchemaParticle;
				if (!(xmlSchemaParticle is XmlSchemaElement xmlSchemaElement5))
				{
					throw new XmlSchemaInferenceException(System.SR.SchInf_UnknownParticle, 0, 0);
				}
				if (xmlSchemaElement5.Name == xtr.LocalName && parentSchema.TargetNamespace == text)
				{
					lastUsedSeqItem = num;
					for (int j = 0; j < xmlSchemaObjectCollection.Count; j++)
					{
						((XmlSchemaElement)xmlSchemaObjectCollection[j]).MinOccurs = 0m;
					}
					InferElement(xmlSchemaElement5, bCreatingNewType: false, parentSchema);
					SetMinMaxOccurs(xmlSchemaElement5, setMaxoccurs);
					return xmlSchemaElement5;
				}
				if (xmlSchemaElement5.RefName.Name == xtr.LocalName && xmlSchemaElement5.RefName.Namespace == xtr.NamespaceURI)
				{
					lastUsedSeqItem = num;
					for (int k = 0; k < xmlSchemaObjectCollection.Count; k++)
					{
						((XmlSchemaElement)xmlSchemaObjectCollection[k]).MinOccurs = 0m;
					}
					XmlSchemaElement xmlSchemaElement6 = FindGlobalElement(text, xtr.LocalName, out parentSchema);
					InferElement(xmlSchemaElement6, bCreatingNewType: false, parentSchema);
					SetMinMaxOccurs(xmlSchemaElement5, setMaxoccurs);
					return xmlSchemaElement6;
				}
				xmlSchemaObjectCollection.Add(xmlSchemaElement5);
			}
			XmlSchemaElement xmlSchemaElement7 = null;
			XmlSchemaElement xse2 = null;
			if (parentSchema.TargetNamespace == text)
			{
				xmlSchemaElement7 = FindElement(xmlSchemaSequence.Items, xtr.LocalName);
				xse2 = xmlSchemaElement7;
			}
			else
			{
				xmlSchemaElement7 = FindElementRef(xmlSchemaSequence.Items, xtr.LocalName, xtr.NamespaceURI);
				if (xmlSchemaElement7 != null)
				{
					xse2 = FindGlobalElement(text, xtr.LocalName, out parentSchema);
				}
			}
			if (xmlSchemaElement7 != null)
			{
				XmlSchemaChoice xmlSchemaChoice2 = new XmlSchemaChoice();
				xmlSchemaChoice2.MaxOccurs = decimal.MaxValue;
				SetMinMaxOccurs(xmlSchemaElement7, setMaxoccurs);
				InferElement(xse2, bCreatingNewType: false, parentSchema);
				for (int l = 0; l < xmlSchemaSequence.Items.Count; l++)
				{
					xmlSchemaChoice2.Items.Add(CreateNewElementforChoice((XmlSchemaElement)xmlSchemaSequence.Items[l]));
				}
				xmlSchemaSequence.Items.Clear();
				xmlSchemaSequence.Items.Add(xmlSchemaChoice2);
				return xmlSchemaElement7;
			}
			xmlSchemaElement7 = AddElement(xtr.LocalName, xtr.Prefix, xtr.NamespaceURI, parentSchema, xmlSchemaSequence.Items, ++lastUsedSeqItem);
			if (!bCreatingNewType)
			{
				xmlSchemaElement7.MinOccurs = 0m;
			}
			return xmlSchemaElement7;
		}
		throw new XmlSchemaInferenceException(System.SR.SchInf_noseq, 0, 0);
	}

	internal void ProcessAttributes(ref XmlSchemaElement xse, XmlSchemaType effectiveSchemaType, bool bCreatingNewType, XmlSchema parentSchema)
	{
		XmlSchemaObjectCollection xmlSchemaObjectCollection = new XmlSchemaObjectCollection();
		XmlSchemaComplexType xmlSchemaComplexType = effectiveSchemaType as XmlSchemaComplexType;
		do
		{
			if (_xtr.NamespaceURI == "http://www.w3.org/2001/XMLSchema")
			{
				throw new XmlSchemaInferenceException(System.SR.SchInf_schema, 0, 0);
			}
			if (_xtr.NamespaceURI == "http://www.w3.org/2000/xmlns/")
			{
				if (_xtr.Prefix == "xmlns")
				{
					_namespaceManager.AddNamespace(_xtr.LocalName, _xtr.Value);
				}
				continue;
			}
			if (_xtr.NamespaceURI == "http://www.w3.org/2001/XMLSchema-instance")
			{
				string localName = _xtr.LocalName;
				if (localName == "nil")
				{
					xse.IsNillable = true;
				}
				else if (localName != "type" && localName != "schemaLocation" && localName != "noNamespaceSchemaLocation")
				{
					throw new XmlSchemaInferenceException(System.SR.Sch_NotXsiAttribute, localName);
				}
				continue;
			}
			if (xmlSchemaComplexType == null || xmlSchemaComplexType == XmlSchemaComplexType.AnyType)
			{
				xmlSchemaComplexType = new XmlSchemaComplexType();
				xse.SchemaType = xmlSchemaComplexType;
			}
			XmlSchemaAttribute xmlSchemaAttribute = null;
			if (effectiveSchemaType != null && effectiveSchemaType.Datatype != null && !xse.SchemaTypeName.IsEmpty)
			{
				XmlSchemaSimpleContent xmlSchemaSimpleContent = (XmlSchemaSimpleContent)(xmlSchemaComplexType.ContentModel = new XmlSchemaSimpleContent());
				XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension = (XmlSchemaSimpleContentExtension)(xmlSchemaSimpleContent.Content = new XmlSchemaSimpleContentExtension());
				xmlSchemaSimpleContentExtension.BaseTypeName = xse.SchemaTypeName;
				xmlSchemaSimpleContentExtension.LineNumber = xse.LineNumber;
				xse.LineNumber = 0;
				xse.SchemaTypeName = XmlQualifiedName.Empty;
			}
			if (xmlSchemaComplexType.ContentModel != null)
			{
				XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension2 = CheckSimpleContentExtension(xmlSchemaComplexType);
				xmlSchemaAttribute = AddAttribute(_xtr.LocalName, _xtr.Prefix, _xtr.NamespaceURI, _xtr.Value, bCreatingNewType, parentSchema, xmlSchemaSimpleContentExtension2.Attributes, xmlSchemaComplexType.AttributeUses);
			}
			else
			{
				xmlSchemaAttribute = AddAttribute(_xtr.LocalName, _xtr.Prefix, _xtr.NamespaceURI, _xtr.Value, bCreatingNewType, parentSchema, xmlSchemaComplexType.Attributes, xmlSchemaComplexType.AttributeUses);
			}
			if (xmlSchemaAttribute != null)
			{
				xmlSchemaObjectCollection.Add(xmlSchemaAttribute);
			}
		}
		while (_xtr.MoveToNextAttribute());
		if (!bCreatingNewType && xmlSchemaComplexType != null)
		{
			MakeExistingAttributesOptional(xmlSchemaComplexType, xmlSchemaObjectCollection);
		}
	}

	private void MoveAttributes(XmlSchemaSimpleContentExtension scExtension, XmlSchemaComplexType ct)
	{
		for (int i = 0; i < scExtension.Attributes.Count; i++)
		{
			ct.Attributes.Add(scExtension.Attributes[i]);
		}
	}

	private void MoveAttributes(XmlSchemaComplexType ct, XmlSchemaSimpleContentExtension simpleContentExtension, bool bCreatingNewType)
	{
		ICollection collection = ((bCreatingNewType || ct.AttributeUses.Count <= 0) ? ct.Attributes : ct.AttributeUses.Values);
		foreach (XmlSchemaAttribute item in collection)
		{
			simpleContentExtension.Attributes.Add(item);
		}
		ct.Attributes.Clear();
	}

	internal XmlSchemaAttribute FindAttribute(ICollection attributes, string attrName)
	{
		foreach (XmlSchemaObject attribute in attributes)
		{
			if (attribute is XmlSchemaAttribute xmlSchemaAttribute && xmlSchemaAttribute.Name == attrName)
			{
				return xmlSchemaAttribute;
			}
		}
		return null;
	}

	internal XmlSchemaElement FindGlobalElement(string namespaceURI, string localName, out XmlSchema parentSchema)
	{
		ICollection collection = _schemaSet.Schemas(namespaceURI);
		XmlSchemaElement xmlSchemaElement = null;
		parentSchema = null;
		foreach (XmlSchema item in collection)
		{
			xmlSchemaElement = FindElement(item.Items, localName);
			if (xmlSchemaElement != null)
			{
				parentSchema = item;
				return xmlSchemaElement;
			}
		}
		return null;
	}

	internal XmlSchemaElement FindElement(XmlSchemaObjectCollection elements, string elementName)
	{
		for (int i = 0; i < elements.Count; i++)
		{
			if (elements[i] is XmlSchemaElement xmlSchemaElement && xmlSchemaElement.RefName != null && xmlSchemaElement.Name == elementName)
			{
				return xmlSchemaElement;
			}
		}
		return null;
	}

	internal XmlSchemaAttribute FindAttributeRef(ICollection attributes, string attributeName, string nsURI)
	{
		foreach (XmlSchemaObject attribute in attributes)
		{
			if (attribute is XmlSchemaAttribute xmlSchemaAttribute && xmlSchemaAttribute.RefName.Name == attributeName && xmlSchemaAttribute.RefName.Namespace == nsURI)
			{
				return xmlSchemaAttribute;
			}
		}
		return null;
	}

	internal XmlSchemaElement FindElementRef(XmlSchemaObjectCollection elements, string elementName, string nsURI)
	{
		for (int i = 0; i < elements.Count; i++)
		{
			if (elements[i] is XmlSchemaElement xmlSchemaElement && xmlSchemaElement.RefName != null && xmlSchemaElement.RefName.Name == elementName && xmlSchemaElement.RefName.Namespace == nsURI)
			{
				return xmlSchemaElement;
			}
		}
		return null;
	}

	internal void MakeExistingAttributesOptional(XmlSchemaComplexType ct, XmlSchemaObjectCollection attributesInInstance)
	{
		if (ct == null)
		{
			throw new XmlSchemaInferenceException(System.SR.SchInf_noct, 0, 0);
		}
		if (ct.ContentModel != null)
		{
			XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension = CheckSimpleContentExtension(ct);
			SwitchUseToOptional(xmlSchemaSimpleContentExtension.Attributes, attributesInInstance);
		}
		else
		{
			SwitchUseToOptional(ct.Attributes, attributesInInstance);
		}
	}

	private void SwitchUseToOptional(XmlSchemaObjectCollection attributes, XmlSchemaObjectCollection attributesInInstance)
	{
		for (int i = 0; i < attributes.Count; i++)
		{
			if (!(attributes[i] is XmlSchemaAttribute xmlSchemaAttribute))
			{
				continue;
			}
			if (attributesInInstance != null)
			{
				if (xmlSchemaAttribute.RefName.Name.Length == 0)
				{
					if (FindAttribute(attributesInInstance, xmlSchemaAttribute.Name) == null)
					{
						xmlSchemaAttribute.Use = XmlSchemaUse.Optional;
					}
				}
				else if (FindAttributeRef(attributesInInstance, xmlSchemaAttribute.RefName.Name, xmlSchemaAttribute.RefName.Namespace) == null)
				{
					xmlSchemaAttribute.Use = XmlSchemaUse.Optional;
				}
			}
			else
			{
				xmlSchemaAttribute.Use = XmlSchemaUse.Optional;
			}
		}
	}

	internal XmlQualifiedName RefineSimpleType(string s, ref int iTypeFlags)
	{
		bool bNeedsRangeCheck = false;
		s = s.Trim();
		if (iTypeFlags == 262144 || _typeInference == InferenceOption.Relaxed)
		{
			return ST_string;
		}
		iTypeFlags &= InferSimpleType(s, ref bNeedsRangeCheck);
		if (iTypeFlags == 262144)
		{
			return ST_string;
		}
		if (bNeedsRangeCheck)
		{
			if (((uint)iTypeFlags & 2u) != 0)
			{
				try
				{
					XmlConvert.ToSByte(s);
					if (((uint)iTypeFlags & 4u) != 0)
					{
						return ST_unsignedByte;
					}
					return ST_byte;
				}
				catch (FormatException)
				{
				}
				catch (OverflowException)
				{
				}
				iTypeFlags &= -3;
			}
			if (((uint)iTypeFlags & 4u) != 0)
			{
				try
				{
					XmlConvert.ToByte(s);
					return ST_unsignedByte;
				}
				catch (FormatException)
				{
				}
				catch (OverflowException)
				{
				}
				iTypeFlags &= -5;
			}
			if (((uint)iTypeFlags & 8u) != 0)
			{
				try
				{
					XmlConvert.ToInt16(s);
					if (((uint)iTypeFlags & 0x10u) != 0)
					{
						return ST_unsignedShort;
					}
					return ST_short;
				}
				catch (FormatException)
				{
				}
				catch (OverflowException)
				{
				}
				iTypeFlags &= -9;
			}
			if (((uint)iTypeFlags & 0x10u) != 0)
			{
				try
				{
					XmlConvert.ToUInt16(s);
					return ST_unsignedShort;
				}
				catch (FormatException)
				{
				}
				catch (OverflowException)
				{
				}
				iTypeFlags &= -17;
			}
			if (((uint)iTypeFlags & 0x20u) != 0)
			{
				try
				{
					XmlConvert.ToInt32(s);
					if (((uint)iTypeFlags & 0x40u) != 0)
					{
						return ST_unsignedInt;
					}
					return ST_int;
				}
				catch (FormatException)
				{
				}
				catch (OverflowException)
				{
				}
				iTypeFlags &= -33;
			}
			if (((uint)iTypeFlags & 0x40u) != 0)
			{
				try
				{
					XmlConvert.ToUInt32(s);
					return ST_unsignedInt;
				}
				catch (FormatException)
				{
				}
				catch (OverflowException)
				{
				}
				iTypeFlags &= -65;
			}
			if (((uint)iTypeFlags & 0x80u) != 0)
			{
				try
				{
					XmlConvert.ToInt64(s);
					if (((uint)iTypeFlags & 0x100u) != 0)
					{
						return ST_unsignedLong;
					}
					return ST_long;
				}
				catch (FormatException)
				{
				}
				catch (OverflowException)
				{
				}
				iTypeFlags &= -129;
			}
			if (((uint)iTypeFlags & 0x100u) != 0)
			{
				try
				{
					XmlConvert.ToUInt64(s);
					return ST_unsignedLong;
				}
				catch (FormatException)
				{
				}
				catch (OverflowException)
				{
				}
				iTypeFlags &= -257;
			}
			if (((uint)iTypeFlags & 0x1000u) != 0)
			{
				try
				{
					double num = XmlConvert.ToDouble(s);
					if (((uint)iTypeFlags & 0x200u) != 0)
					{
						return ST_integer;
					}
					if (((uint)iTypeFlags & 0x400u) != 0)
					{
						return ST_decimal;
					}
					if (((uint)iTypeFlags & 0x800u) != 0)
					{
						try
						{
							float num2 = XmlConvert.ToSingle(s);
							if (string.Equals(XmlConvert.ToString(num2), XmlConvert.ToString(num), StringComparison.OrdinalIgnoreCase))
							{
								return ST_float;
							}
						}
						catch (FormatException)
						{
						}
						catch (OverflowException)
						{
						}
					}
					iTypeFlags &= -2049;
					return ST_double;
				}
				catch (FormatException)
				{
				}
				catch (OverflowException)
				{
				}
				iTypeFlags &= -4097;
			}
			if (((uint)iTypeFlags & 0x800u) != 0)
			{
				try
				{
					XmlConvert.ToSingle(s);
					if (((uint)iTypeFlags & 0x200u) != 0)
					{
						return ST_integer;
					}
					if (((uint)iTypeFlags & 0x400u) != 0)
					{
						return ST_decimal;
					}
					return ST_float;
				}
				catch (FormatException)
				{
				}
				catch (OverflowException)
				{
				}
				iTypeFlags &= -2049;
			}
			if (((uint)iTypeFlags & 0x200u) != 0)
			{
				return ST_integer;
			}
			if (((uint)iTypeFlags & 0x400u) != 0)
			{
				return ST_decimal;
			}
			if (iTypeFlags == 393216)
			{
				try
				{
					XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.RoundtripKind);
					return ST_gYearMonth;
				}
				catch (FormatException)
				{
				}
				catch (OverflowException)
				{
				}
				iTypeFlags = 262144;
				return ST_string;
			}
			if (iTypeFlags == 270336)
			{
				try
				{
					XmlConvert.ToTimeSpan(s);
					return ST_duration;
				}
				catch (FormatException)
				{
				}
				catch (OverflowException)
				{
				}
				iTypeFlags = 262144;
				return ST_string;
			}
			if (iTypeFlags == 262145)
			{
				return ST_boolean;
			}
		}
		return iTypeFlags switch
		{
			262144 => ST_string, 
			1 => ST_boolean, 
			2 => ST_byte, 
			4 => ST_unsignedByte, 
			8 => ST_short, 
			16 => ST_unsignedShort, 
			32 => ST_int, 
			64 => ST_unsignedInt, 
			128 => ST_long, 
			256 => ST_unsignedLong, 
			512 => ST_integer, 
			1024 => ST_decimal, 
			2048 => ST_float, 
			4096 => ST_double, 
			8192 => ST_duration, 
			16384 => ST_dateTime, 
			32768 => ST_time, 
			65536 => ST_date, 
			131072 => ST_gYearMonth, 
			262145 => ST_boolean, 
			278528 => ST_dateTime, 
			327680 => ST_date, 
			294912 => ST_time, 
			268288 => ST_float, 
			266240 => ST_double, 
			_ => ST_string, 
		};
	}

	internal static int InferSimpleType(string s, ref bool bNeedsRangeCheck)
	{
		bool flag = false;
		bool flag2 = false;
		bool bDate = false;
		bool bTime = false;
		bool flag3 = false;
		if (s.Length == 0)
		{
			return 262144;
		}
		int num = 0;
		char c8;
		switch (s[num])
		{
		case 'f':
		case 't':
			if (s == "true")
			{
				return 262145;
			}
			if (s == "false")
			{
				return 262145;
			}
			return 262144;
		case 'N':
			if (s == "NaN")
			{
				return 268288;
			}
			return 262144;
		case 'I':
			if (s.AsSpan(num).SequenceEqual("INF"))
			{
				return 268288;
			}
			return 262144;
		case '.':
		{
			bNeedsRangeCheck = true;
			num++;
			if (num == s.Length)
			{
				if (num == 1 || (num == 2 && (flag2 || flag)))
				{
					return 262144;
				}
				return 269312;
			}
			char c21 = s[num];
			if (c21 != 'E' && c21 != 'e')
			{
				if (s[num] < '0' || s[num] > '9')
				{
					return 262144;
				}
				while (true)
				{
					num++;
					if (num == s.Length)
					{
						return 269312;
					}
					char c22 = s[num];
					if (c22 == 'E' || c22 == 'e')
					{
						break;
					}
					if (s[num] < '0' || s[num] > '9')
					{
						return 262144;
					}
				}
			}
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			char c23 = s[num];
			if (c23 != '+' && c23 != '-')
			{
				if (s[num] < '0' || s[num] > '9')
				{
					return 262144;
				}
			}
			else
			{
				num++;
				if (num == s.Length)
				{
					return 262144;
				}
				if (s[num] < '0' || s[num] > '9')
				{
					return 262144;
				}
			}
			do
			{
				num++;
				if (num == s.Length)
				{
					return 268288;
				}
			}
			while (s[num] >= '0' && s[num] <= '9');
			return 262144;
		}
		case '-':
		{
			flag = true;
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			char c7 = s[num];
			if (c7 == '.')
			{
				goto case '.';
			}
			if (c7 == 'I')
			{
				goto case 'I';
			}
			if (c7 == 'P')
			{
				goto case 'P';
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			goto case '0';
		}
		case '+':
		{
			flag2 = true;
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			char c = s[num];
			if (c == '.')
			{
				goto case '.';
			}
			if (c == 'P')
			{
				goto case 'P';
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			goto case '0';
		}
		case 'P':
		{
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			char c10 = s[num];
			if (c10 != 'T')
			{
				if (s[num] < '0' || s[num] > '9')
				{
					return 262144;
				}
				while (true)
				{
					num++;
					if (num == s.Length)
					{
						return 262144;
					}
					char c11 = s[num];
					if (c11 != 'D')
					{
						if (c11 == 'M')
						{
							goto IL_0444;
						}
						if (c11 != 'Y')
						{
							if (s[num] < '0' || s[num] > '9')
							{
								return 262144;
							}
							continue;
						}
						num++;
						if (num == s.Length)
						{
							bNeedsRangeCheck = true;
							return 270336;
						}
						char c12 = s[num];
						if (c12 == 'T')
						{
							break;
						}
						if (s[num] < '0' || s[num] > '9')
						{
							return 262144;
						}
						while (true)
						{
							num++;
							if (num == s.Length)
							{
								return 262144;
							}
							char c13 = s[num];
							if (c13 == 'D')
							{
								break;
							}
							if (c13 != 'M')
							{
								if (s[num] < '0' || s[num] > '9')
								{
									return 262144;
								}
								continue;
							}
							goto IL_0444;
						}
					}
					goto IL_04d2;
					IL_04d2:
					num++;
					if (num == s.Length)
					{
						bNeedsRangeCheck = true;
						return 270336;
					}
					char c14 = s[num];
					if (c14 == 'T')
					{
						break;
					}
					return 262144;
					IL_0444:
					num++;
					if (num == s.Length)
					{
						bNeedsRangeCheck = true;
						return 270336;
					}
					char c15 = s[num];
					if (c15 == 'T')
					{
						break;
					}
					if (s[num] < '0' || s[num] > '9')
					{
						return 262144;
					}
					while (true)
					{
						num++;
						if (num == s.Length)
						{
							return 262144;
						}
						char c16 = s[num];
						if (c16 == 'D')
						{
							break;
						}
						if (s[num] < '0' || s[num] > '9')
						{
							return 262144;
						}
					}
					goto IL_04d2;
				}
			}
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			while (true)
			{
				num++;
				if (num == s.Length)
				{
					return 262144;
				}
				char c17 = s[num];
				if ((uint)c17 <= 72u)
				{
					if (c17 != '.')
					{
						if (c17 != 'H')
						{
							goto IL_057e;
						}
						num++;
						if (num == s.Length)
						{
							bNeedsRangeCheck = true;
							return 270336;
						}
						if (s[num] < '0' || s[num] > '9')
						{
							return 262144;
						}
						while (true)
						{
							num++;
							if (num == s.Length)
							{
								return 262144;
							}
							char c18 = s[num];
							if (c18 == '.')
							{
								break;
							}
							if (c18 != 'M')
							{
								if (c18 == 'S')
								{
									goto end_IL_0535;
								}
								if (s[num] < '0' || s[num] > '9')
								{
									return 262144;
								}
								continue;
							}
							goto IL_0629;
						}
					}
					goto IL_06ad;
				}
				if (c17 != 'M')
				{
					if (c17 == 'S')
					{
						break;
					}
					goto IL_057e;
				}
				goto IL_0629;
				IL_057e:
				if (s[num] < '0' || s[num] > '9')
				{
					return 262144;
				}
				continue;
				IL_06ad:
				num++;
				if (num == s.Length)
				{
					bNeedsRangeCheck = true;
					return 270336;
				}
				if (s[num] < '0' || s[num] > '9')
				{
					return 262144;
				}
				while (true)
				{
					num++;
					if (num == s.Length)
					{
						return 262144;
					}
					char c19 = s[num];
					if (c19 == 'S')
					{
						break;
					}
					if (s[num] < '0' || s[num] > '9')
					{
						return 262144;
					}
				}
				break;
				IL_0629:
				num++;
				if (num == s.Length)
				{
					bNeedsRangeCheck = true;
					return 270336;
				}
				if (s[num] < '0' || s[num] > '9')
				{
					return 262144;
				}
				while (true)
				{
					num++;
					if (num == s.Length)
					{
						return 262144;
					}
					char c20 = s[num];
					if (c20 == '.')
					{
						break;
					}
					if (c20 == 'S')
					{
						goto end_IL_0535;
					}
					if (s[num] < '0' || s[num] > '9')
					{
						return 262144;
					}
				}
				goto IL_06ad;
				continue;
				end_IL_0535:
				break;
			}
			num++;
			if (num == s.Length)
			{
				bNeedsRangeCheck = true;
				return 270336;
			}
			return 262144;
		}
		case '0':
		case '1':
		case '2':
		case '3':
		case '4':
		case '5':
		case '6':
		case '7':
		case '8':
		case '9':
		{
			num++;
			if (num == s.Length)
			{
				bNeedsRangeCheck = true;
				if (flag || flag2)
				{
					return 269994;
				}
				if (s == "0" || s == "1")
				{
					return 270335;
				}
				return 270334;
			}
			char c2 = s[num];
			if (c2 != '.')
			{
				if (c2 == 'E' || c2 == 'e')
				{
					bNeedsRangeCheck = true;
					return 268288;
				}
				if (s[num] < '0' || s[num] > '9')
				{
					return 262144;
				}
				num++;
				if (num == s.Length)
				{
					bNeedsRangeCheck = true;
					if (flag || flag2)
					{
						return 269994;
					}
					return 270334;
				}
				char c3 = s[num];
				if ((uint)c3 <= 58u)
				{
					if (c3 == '.')
					{
						goto case '.';
					}
					if (c3 == ':')
					{
						bTime = true;
						goto IL_0ca9;
					}
				}
				else if (c3 == 'E' || c3 == 'e')
				{
					bNeedsRangeCheck = true;
					return 268288;
				}
				if (s[num] < '0' || s[num] > '9')
				{
					return 262144;
				}
				num++;
				if (num == s.Length)
				{
					bNeedsRangeCheck = true;
					if (flag || flag2)
					{
						return 269994;
					}
					return 270334;
				}
				char c4 = s[num];
				if (c4 != '.')
				{
					if (c4 == 'E' || c4 == 'e')
					{
						bNeedsRangeCheck = true;
						return 268288;
					}
					if (s[num] < '0' || s[num] > '9')
					{
						return 262144;
					}
					while (true)
					{
						num++;
						if (num == s.Length)
						{
							break;
						}
						char c5 = s[num];
						if ((uint)c5 <= 46u)
						{
							if (c5 == '-')
							{
								goto IL_090f;
							}
							if (c5 == '.')
							{
								goto case '.';
							}
						}
						else if (c5 == 'E' || c5 == 'e')
						{
							bNeedsRangeCheck = true;
							return 268288;
						}
						if (s[num] < '0' || s[num] > '9')
						{
							return 262144;
						}
					}
					bNeedsRangeCheck = true;
					if (flag || flag2)
					{
						return 269994;
					}
					return 270334;
				}
			}
			goto case '.';
		}
		default:
			{
				return 262144;
			}
			IL_0ac9:
			num++;
			if (num == s.Length)
			{
				if (flag3)
				{
					bNeedsRangeCheck = true;
					return 393216;
				}
				return DateTime(s, bDate, bTime);
			}
			return 262144;
			IL_0ca9:
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] != ':')
			{
				return 262144;
			}
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			num++;
			if (num == s.Length)
			{
				return DateTime(s, bDate, bTime);
			}
			switch (s[num])
			{
			case 'Z':
			case 'z':
				break;
			case '+':
			case '-':
				goto IL_0af5;
			default:
				return 262144;
			case '.':
				goto IL_0df6;
			}
			goto IL_0ac9;
			IL_0df6:
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			while (true)
			{
				num++;
				if (num == s.Length)
				{
					return DateTime(s, bDate, bTime);
				}
				char c6 = s[num];
				if ((uint)c6 <= 45u)
				{
					if (c6 == '+' || c6 == '-')
					{
						break;
					}
				}
				else if (c6 == 'Z' || c6 == 'z')
				{
					goto IL_0ac9;
				}
				if (s[num] < '0' || s[num] > '9')
				{
					return 262144;
				}
			}
			goto IL_0af5;
			IL_0af5:
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] != ':')
			{
				return 262144;
			}
			goto IL_0b85;
			IL_090f:
			bDate = true;
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			num++;
			if (num == s.Length)
			{
				bNeedsRangeCheck = true;
				return 393216;
			}
			c8 = s[num];
			if ((uint)c8 <= 45u)
			{
				if (c8 == '+')
				{
					flag3 = true;
					goto IL_0af5;
				}
				if (c8 == '-')
				{
					num++;
					if (num == s.Length)
					{
						return 262144;
					}
					if (s[num] < '0' || s[num] > '9')
					{
						return 262144;
					}
					num++;
					if (num == s.Length)
					{
						return 262144;
					}
					if (s[num] < '0' || s[num] > '9')
					{
						return 262144;
					}
					num++;
					if (num == s.Length)
					{
						return DateTime(s, bDate, bTime);
					}
					char c9 = s[num];
					if ((uint)c9 <= 58u)
					{
						if (c9 == '+' || c9 == '-')
						{
							goto IL_0af5;
						}
						if (c9 == ':')
						{
							flag3 = true;
							goto IL_0b85;
						}
					}
					else
					{
						if (c9 == 'T')
						{
							bTime = true;
							num++;
							if (num == s.Length)
							{
								return 262144;
							}
							if (s[num] < '0' || s[num] > '9')
							{
								return 262144;
							}
							num++;
							if (num == s.Length)
							{
								return 262144;
							}
							if (s[num] < '0' || s[num] > '9')
							{
								return 262144;
							}
							num++;
							if (num == s.Length)
							{
								return 262144;
							}
							if (s[num] != ':')
							{
								return 262144;
							}
							goto IL_0ca9;
						}
						if (c9 == 'Z' || c9 == 'z')
						{
							goto IL_0ac9;
						}
					}
					return 262144;
				}
			}
			else if (c8 == 'Z' || c8 == 'z')
			{
				flag3 = true;
				goto IL_0ac9;
			}
			return 262144;
			IL_0b85:
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			num++;
			if (num == s.Length)
			{
				return 262144;
			}
			if (s[num] < '0' || s[num] > '9')
			{
				return 262144;
			}
			num++;
			if (num == s.Length)
			{
				if (flag3)
				{
					bNeedsRangeCheck = true;
					return 393216;
				}
				return DateTime(s, bDate, bTime);
			}
			return 262144;
		}
	}

	internal static int DateTime(string s, bool bDate, bool bTime)
	{
		try
		{
			XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.RoundtripKind);
		}
		catch (FormatException)
		{
			return 262144;
		}
		if (bDate && bTime)
		{
			return 278528;
		}
		if (bDate)
		{
			return 327680;
		}
		if (bTime)
		{
			return 294912;
		}
		return 262144;
	}

	private XmlSchemaElement CreateNewElementforChoice(XmlSchemaElement copyElement)
	{
		XmlSchemaElement xmlSchemaElement = new XmlSchemaElement();
		xmlSchemaElement.Annotation = copyElement.Annotation;
		xmlSchemaElement.Block = copyElement.Block;
		xmlSchemaElement.DefaultValue = copyElement.DefaultValue;
		xmlSchemaElement.Final = copyElement.Final;
		xmlSchemaElement.FixedValue = copyElement.FixedValue;
		xmlSchemaElement.Form = copyElement.Form;
		xmlSchemaElement.Id = copyElement.Id;
		if (copyElement.IsNillable)
		{
			xmlSchemaElement.IsNillable = copyElement.IsNillable;
		}
		xmlSchemaElement.LineNumber = copyElement.LineNumber;
		xmlSchemaElement.LinePosition = copyElement.LinePosition;
		xmlSchemaElement.Name = copyElement.Name;
		xmlSchemaElement.Namespaces = copyElement.Namespaces;
		xmlSchemaElement.RefName = copyElement.RefName;
		xmlSchemaElement.SchemaType = copyElement.SchemaType;
		xmlSchemaElement.SchemaTypeName = copyElement.SchemaTypeName;
		xmlSchemaElement.SourceUri = copyElement.SourceUri;
		xmlSchemaElement.SubstitutionGroup = copyElement.SubstitutionGroup;
		xmlSchemaElement.UnhandledAttributes = copyElement.UnhandledAttributes;
		if (copyElement.MinOccurs != 1m && Occurrence == InferenceOption.Relaxed)
		{
			xmlSchemaElement.MinOccurs = copyElement.MinOccurs;
		}
		if (copyElement.MaxOccurs != 1m)
		{
			xmlSchemaElement.MaxOccurs = copyElement.MaxOccurs;
		}
		return xmlSchemaElement;
	}

	private static int GetSchemaType(XmlQualifiedName qname)
	{
		if (qname == SimpleTypes[0])
		{
			return 262145;
		}
		if (qname == SimpleTypes[1])
		{
			return 269994;
		}
		if (qname == SimpleTypes[2])
		{
			return 270334;
		}
		if (qname == SimpleTypes[3])
		{
			return 269992;
		}
		if (qname == SimpleTypes[4])
		{
			return 270328;
		}
		if (qname == SimpleTypes[5])
		{
			return 269984;
		}
		if (qname == SimpleTypes[6])
		{
			return 270304;
		}
		if (qname == SimpleTypes[7])
		{
			return 269952;
		}
		if (qname == SimpleTypes[8])
		{
			return 270208;
		}
		if (qname == SimpleTypes[9])
		{
			return 269824;
		}
		if (qname == SimpleTypes[10])
		{
			return 269312;
		}
		if (qname == SimpleTypes[11])
		{
			return 268288;
		}
		if (qname == SimpleTypes[12])
		{
			return 266240;
		}
		if (qname == SimpleTypes[13])
		{
			return 270336;
		}
		if (qname == SimpleTypes[14])
		{
			return 278528;
		}
		if (qname == SimpleTypes[15])
		{
			return 294912;
		}
		if (qname == SimpleTypes[16])
		{
			return 65536;
		}
		if (qname == SimpleTypes[17])
		{
			return 131072;
		}
		if (qname == SimpleTypes[18])
		{
			return 262144;
		}
		if (qname == null || qname.IsEmpty)
		{
			return -1;
		}
		throw new XmlSchemaInferenceException(System.SR.SchInf_schematype, 0, 0);
	}

	internal void SetMinMaxOccurs(XmlSchemaElement el, bool setMaxOccurs)
	{
		if (Occurrence == InferenceOption.Relaxed)
		{
			if (setMaxOccurs || el.MaxOccurs > 1m)
			{
				el.MaxOccurs = decimal.MaxValue;
			}
			el.MinOccurs = 0m;
		}
		else if (el.MinOccurs > 1m)
		{
			el.MinOccurs = 1m;
		}
	}
}
