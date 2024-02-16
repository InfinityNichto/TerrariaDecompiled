namespace System.Xml.Schema;

internal class BaseProcessor
{
	private readonly XmlNameTable _nameTable;

	private SchemaNames _schemaNames;

	private readonly ValidationEventHandler _eventHandler;

	private readonly XmlSchemaCompilationSettings _compilationSettings;

	private int _errorCount;

	private readonly string _nsXml;

	protected XmlNameTable NameTable => _nameTable;

	protected SchemaNames SchemaNames
	{
		get
		{
			if (_schemaNames == null)
			{
				_schemaNames = new SchemaNames(_nameTable);
			}
			return _schemaNames;
		}
	}

	protected ValidationEventHandler EventHandler => _eventHandler;

	protected XmlSchemaCompilationSettings CompilationSettings => _compilationSettings;

	protected bool HasErrors => _errorCount != 0;

	public BaseProcessor(XmlNameTable nameTable, SchemaNames schemaNames, ValidationEventHandler eventHandler)
		: this(nameTable, schemaNames, eventHandler, new XmlSchemaCompilationSettings())
	{
	}

	public BaseProcessor(XmlNameTable nameTable, SchemaNames schemaNames, ValidationEventHandler eventHandler, XmlSchemaCompilationSettings compilationSettings)
	{
		_nameTable = nameTable;
		_schemaNames = schemaNames;
		_eventHandler = eventHandler;
		_compilationSettings = compilationSettings;
		_nsXml = nameTable.Add("http://www.w3.org/XML/1998/namespace");
	}

	protected void AddToTable(XmlSchemaObjectTable table, XmlQualifiedName qname, XmlSchemaObject item)
	{
		if (qname.Name.Length == 0)
		{
			return;
		}
		XmlSchemaObject xmlSchemaObject = table[qname];
		if (xmlSchemaObject != null)
		{
			if (xmlSchemaObject == item)
			{
				return;
			}
			string code = System.SR.Sch_DupGlobalElement;
			if (item is XmlSchemaAttributeGroup)
			{
				string strA = _nameTable.Add(qname.Namespace);
				if (Ref.Equal(strA, _nsXml))
				{
					XmlSchema buildInSchema = Preprocessor.GetBuildInSchema();
					XmlSchemaObject xmlSchemaObject2 = buildInSchema.AttributeGroups[qname];
					if (xmlSchemaObject == xmlSchemaObject2)
					{
						table.Insert(qname, item);
						return;
					}
					if (item == xmlSchemaObject2)
					{
						return;
					}
				}
				else if (IsValidAttributeGroupRedefine(xmlSchemaObject, item, table))
				{
					return;
				}
				code = System.SR.Sch_DupAttributeGroup;
			}
			else if (item is XmlSchemaAttribute)
			{
				string strA2 = _nameTable.Add(qname.Namespace);
				if (Ref.Equal(strA2, _nsXml))
				{
					XmlSchema buildInSchema2 = Preprocessor.GetBuildInSchema();
					XmlSchemaObject xmlSchemaObject3 = buildInSchema2.Attributes[qname];
					if (xmlSchemaObject == xmlSchemaObject3)
					{
						table.Insert(qname, item);
						return;
					}
					if (item == xmlSchemaObject3)
					{
						return;
					}
				}
				code = System.SR.Sch_DupGlobalAttribute;
			}
			else if (item is XmlSchemaSimpleType)
			{
				if (IsValidTypeRedefine(xmlSchemaObject, item, table))
				{
					return;
				}
				code = System.SR.Sch_DupSimpleType;
			}
			else if (item is XmlSchemaComplexType)
			{
				if (IsValidTypeRedefine(xmlSchemaObject, item, table))
				{
					return;
				}
				code = System.SR.Sch_DupComplexType;
			}
			else if (item is XmlSchemaGroup)
			{
				if (IsValidGroupRedefine(xmlSchemaObject, item, table))
				{
					return;
				}
				code = System.SR.Sch_DupGroup;
			}
			else if (item is XmlSchemaNotation)
			{
				code = System.SR.Sch_DupNotation;
			}
			else if (item is XmlSchemaIdentityConstraint)
			{
				code = System.SR.Sch_DupIdentityConstraint;
			}
			SendValidationEvent(code, qname.ToString(), item);
		}
		else
		{
			table.Add(qname, item);
		}
	}

	private bool IsValidAttributeGroupRedefine(XmlSchemaObject existingObject, XmlSchemaObject item, XmlSchemaObjectTable table)
	{
		XmlSchemaAttributeGroup xmlSchemaAttributeGroup = item as XmlSchemaAttributeGroup;
		XmlSchemaAttributeGroup xmlSchemaAttributeGroup2 = existingObject as XmlSchemaAttributeGroup;
		if (xmlSchemaAttributeGroup2 == xmlSchemaAttributeGroup.Redefined)
		{
			if (xmlSchemaAttributeGroup2.AttributeUses.Count == 0)
			{
				table.Insert(xmlSchemaAttributeGroup.QualifiedName, xmlSchemaAttributeGroup);
				return true;
			}
		}
		else if (xmlSchemaAttributeGroup2.Redefined == xmlSchemaAttributeGroup)
		{
			return true;
		}
		return false;
	}

	private bool IsValidGroupRedefine(XmlSchemaObject existingObject, XmlSchemaObject item, XmlSchemaObjectTable table)
	{
		XmlSchemaGroup xmlSchemaGroup = item as XmlSchemaGroup;
		XmlSchemaGroup xmlSchemaGroup2 = existingObject as XmlSchemaGroup;
		if (xmlSchemaGroup2 == xmlSchemaGroup.Redefined)
		{
			if (xmlSchemaGroup2.CanonicalParticle == null)
			{
				table.Insert(xmlSchemaGroup.QualifiedName, xmlSchemaGroup);
				return true;
			}
		}
		else if (xmlSchemaGroup2.Redefined == xmlSchemaGroup)
		{
			return true;
		}
		return false;
	}

	private bool IsValidTypeRedefine(XmlSchemaObject existingObject, XmlSchemaObject item, XmlSchemaObjectTable table)
	{
		XmlSchemaType xmlSchemaType = item as XmlSchemaType;
		XmlSchemaType xmlSchemaType2 = existingObject as XmlSchemaType;
		if (xmlSchemaType2 == xmlSchemaType.Redefined)
		{
			if (xmlSchemaType2.ElementDecl == null)
			{
				table.Insert(xmlSchemaType.QualifiedName, xmlSchemaType);
				return true;
			}
		}
		else if (xmlSchemaType2.Redefined == xmlSchemaType)
		{
			return true;
		}
		return false;
	}

	protected void SendValidationEvent(string code, XmlSchemaObject source)
	{
		SendValidationEvent(new XmlSchemaException(code, source), XmlSeverityType.Error);
	}

	protected void SendValidationEvent(string code, string msg, XmlSchemaObject source)
	{
		SendValidationEvent(new XmlSchemaException(code, msg, source), XmlSeverityType.Error);
	}

	protected void SendValidationEvent(string code, string msg1, string msg2, XmlSchemaObject source)
	{
		SendValidationEvent(new XmlSchemaException(code, new string[2] { msg1, msg2 }, source), XmlSeverityType.Error);
	}

	protected void SendValidationEvent(string code, string[] args, Exception innerException, XmlSchemaObject source)
	{
		SendValidationEvent(new XmlSchemaException(code, args, innerException, source.SourceUri, source.LineNumber, source.LinePosition, source), XmlSeverityType.Error);
	}

	protected void SendValidationEvent(string code, string msg1, string msg2, string sourceUri, int lineNumber, int linePosition)
	{
		SendValidationEvent(new XmlSchemaException(code, new string[2] { msg1, msg2 }, sourceUri, lineNumber, linePosition), XmlSeverityType.Error);
	}

	protected void SendValidationEvent(string code, XmlSchemaObject source, XmlSeverityType severity)
	{
		SendValidationEvent(new XmlSchemaException(code, source), severity);
	}

	protected void SendValidationEvent(XmlSchemaException e)
	{
		SendValidationEvent(e, XmlSeverityType.Error);
	}

	protected void SendValidationEvent(string code, string msg, XmlSchemaObject source, XmlSeverityType severity)
	{
		SendValidationEvent(new XmlSchemaException(code, msg, source), severity);
	}

	protected void SendValidationEvent(XmlSchemaException e, XmlSeverityType severity)
	{
		if (severity == XmlSeverityType.Error)
		{
			_errorCount++;
		}
		if (_eventHandler != null)
		{
			_eventHandler(null, new ValidationEventArgs(e, severity));
		}
		else if (severity == XmlSeverityType.Error)
		{
			throw e;
		}
	}

	protected void SendValidationEventNoThrow(XmlSchemaException e, XmlSeverityType severity)
	{
		if (severity == XmlSeverityType.Error)
		{
			_errorCount++;
		}
		if (_eventHandler != null)
		{
			_eventHandler(null, new ValidationEventArgs(e, severity));
		}
	}
}
