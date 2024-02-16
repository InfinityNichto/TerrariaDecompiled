using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Xml.Schema;

internal sealed class DtdValidator : BaseValidator
{
	private sealed class NamespaceManager : XmlNamespaceManager
	{
		public override string LookupNamespace(string prefix)
		{
			return prefix;
		}
	}

	private static readonly NamespaceManager s_namespaceManager = new NamespaceManager();

	private HWStack _validationStack;

	private Hashtable _attPresence;

	private XmlQualifiedName _name = XmlQualifiedName.Empty;

	private Hashtable _IDs;

	private IdRefNode _idRefListHead;

	private readonly bool _processIdentityConstraints;

	public override bool PreserveWhitespace
	{
		get
		{
			if (context.ElementDecl == null)
			{
				return false;
			}
			return context.ElementDecl.ContentValidator.PreserveWhitespace;
		}
	}

	internal DtdValidator(XmlValidatingReaderImpl reader, IValidationEventHandling eventHandling, bool processIdentityConstraints)
		: base(reader, null, eventHandling)
	{
		_processIdentityConstraints = processIdentityConstraints;
		Init();
	}

	[MemberNotNull("_validationStack")]
	[MemberNotNull("_name")]
	[MemberNotNull("_attPresence")]
	private void Init()
	{
		_validationStack = new HWStack(10);
		textValue = new StringBuilder();
		_name = XmlQualifiedName.Empty;
		_attPresence = new Hashtable();
		schemaInfo = new SchemaInfo();
		checkDatatype = false;
		Push(_name);
	}

	public override void Validate()
	{
		if (schemaInfo.SchemaType == SchemaType.DTD)
		{
			switch (reader.NodeType)
			{
			case XmlNodeType.Element:
				ValidateElement();
				if (reader.IsEmptyElement)
				{
					goto case XmlNodeType.EndElement;
				}
				break;
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				if (MeetsStandAloneConstraint())
				{
					ValidateWhitespace();
				}
				break;
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.Comment:
				ValidatePIComment();
				break;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
				ValidateText();
				break;
			case XmlNodeType.EntityReference:
				if (!GenEntity(new XmlQualifiedName(reader.LocalName, reader.Prefix)))
				{
					ValidateText();
				}
				break;
			case XmlNodeType.EndElement:
				ValidateEndElement();
				break;
			case XmlNodeType.Attribute:
			case XmlNodeType.Entity:
			case XmlNodeType.Document:
			case XmlNodeType.DocumentType:
			case XmlNodeType.DocumentFragment:
			case XmlNodeType.Notation:
				break;
			}
		}
		else if (reader.Depth == 0 && reader.NodeType == XmlNodeType.Element)
		{
			SendValidationEvent(System.SR.Xml_NoDTDPresent, _name.ToString(), XmlSeverityType.Warning);
		}
	}

	private bool MeetsStandAloneConstraint()
	{
		if (reader.StandAlone && context.ElementDecl != null && context.ElementDecl.IsDeclaredInExternal && context.ElementDecl.ContentValidator.ContentType == XmlSchemaContentType.ElementOnly)
		{
			SendValidationEvent(System.SR.Sch_StandAlone);
			return false;
		}
		return true;
	}

	private void ValidatePIComment()
	{
		if (context.NeedValidateChildren && context.ElementDecl.ContentValidator == ContentValidator.Empty)
		{
			SendValidationEvent(System.SR.Sch_InvalidPIComment);
		}
	}

	private void ValidateElement()
	{
		elementName.Init(reader.LocalName, reader.Prefix);
		if (reader.Depth == 0 && !schemaInfo.DocTypeName.IsEmpty && !schemaInfo.DocTypeName.Equals(elementName))
		{
			SendValidationEvent(System.SR.Sch_RootMatchDocType);
		}
		else
		{
			ValidateChildElement();
		}
		ProcessElement();
	}

	private void ValidateChildElement()
	{
		if (context.NeedValidateChildren)
		{
			int errorCode = 0;
			context.ElementDecl.ContentValidator.ValidateElement(elementName, context, out errorCode);
			if (errorCode < 0)
			{
				XmlSchemaValidator.ElementValidationError(elementName, context, base.EventHandler, reader, reader.BaseURI, base.PositionInfo.LineNumber, base.PositionInfo.LinePosition, null);
			}
		}
	}

	private void ValidateStartElement()
	{
		if (context.ElementDecl != null)
		{
			base.Reader.SchemaTypeObject = context.ElementDecl.SchemaType;
			if (base.Reader.IsEmptyElement && context.ElementDecl.DefaultValueTyped != null)
			{
				base.Reader.TypedValueObject = context.ElementDecl.DefaultValueTyped;
				context.IsNill = true;
			}
			if (context.ElementDecl.HasRequiredAttribute)
			{
				_attPresence.Clear();
			}
		}
		if (!base.Reader.MoveToFirstAttribute())
		{
			return;
		}
		do
		{
			try
			{
				reader.SchemaTypeObject = null;
				SchemaAttDef attDef = context.ElementDecl.GetAttDef(new XmlQualifiedName(reader.LocalName, reader.Prefix));
				if (attDef != null)
				{
					if (context.ElementDecl != null && context.ElementDecl.HasRequiredAttribute)
					{
						_attPresence.Add(attDef.Name, attDef);
					}
					base.Reader.SchemaTypeObject = attDef.SchemaType;
					if (attDef.Datatype != null && !reader.IsDefault)
					{
						CheckValue(base.Reader.Value, attDef);
					}
				}
				else
				{
					SendValidationEvent(System.SR.Sch_UndeclaredAttribute, reader.Name);
				}
			}
			catch (XmlSchemaException ex)
			{
				ex.SetSource(base.Reader.BaseURI, base.PositionInfo.LineNumber, base.PositionInfo.LinePosition);
				SendValidationEvent(ex);
			}
		}
		while (base.Reader.MoveToNextAttribute());
		base.Reader.MoveToElement();
	}

	private void ValidateEndStartElement()
	{
		if (context.ElementDecl.HasRequiredAttribute)
		{
			try
			{
				context.ElementDecl.CheckAttributes(_attPresence, base.Reader.StandAlone);
			}
			catch (XmlSchemaException ex)
			{
				ex.SetSource(base.Reader.BaseURI, base.PositionInfo.LineNumber, base.PositionInfo.LinePosition);
				SendValidationEvent(ex);
			}
		}
		if (context.ElementDecl.Datatype != null)
		{
			checkDatatype = true;
			hasSibling = false;
			textString = string.Empty;
			textValue.Length = 0;
		}
	}

	private void ProcessElement()
	{
		SchemaElementDecl elementDecl = schemaInfo.GetElementDecl(elementName);
		Push(elementName);
		if (elementDecl != null)
		{
			context.ElementDecl = elementDecl;
			ValidateStartElement();
			ValidateEndStartElement();
			context.NeedValidateChildren = true;
			elementDecl.ContentValidator.InitValidation(context);
		}
		else
		{
			SendValidationEvent(System.SR.Sch_UndeclaredElement, XmlSchemaValidator.QNameString(context.LocalName, context.Namespace));
			context.ElementDecl = null;
		}
	}

	public override void CompleteValidation()
	{
		if (schemaInfo.SchemaType == SchemaType.DTD)
		{
			do
			{
				ValidateEndElement();
			}
			while (Pop());
			CheckForwardRefs();
		}
	}

	private void ValidateEndElement()
	{
		if (context.ElementDecl != null)
		{
			if (context.NeedValidateChildren && !context.ElementDecl.ContentValidator.CompleteValidation(context))
			{
				XmlSchemaValidator.CompleteValidationError(context, base.EventHandler, reader, reader.BaseURI, base.PositionInfo.LineNumber, base.PositionInfo.LinePosition, null);
			}
			if (checkDatatype)
			{
				string value = ((!hasSibling) ? textString : textValue.ToString());
				CheckValue(value, null);
				checkDatatype = false;
				textValue.Length = 0;
				textString = string.Empty;
			}
		}
		Pop();
	}

	private void ProcessTokenizedType(XmlTokenizedType ttype, string name)
	{
		switch (ttype)
		{
		case XmlTokenizedType.ID:
			if (_processIdentityConstraints)
			{
				if (FindId(name) != null)
				{
					SendValidationEvent(System.SR.Sch_DupId, name);
				}
				else
				{
					AddID(name, context.LocalName);
				}
			}
			break;
		case XmlTokenizedType.IDREF:
			if (_processIdentityConstraints)
			{
				object obj = FindId(name);
				if (obj == null)
				{
					_idRefListHead = new IdRefNode(_idRefListHead, name, base.PositionInfo.LineNumber, base.PositionInfo.LinePosition);
				}
			}
			break;
		case XmlTokenizedType.ENTITY:
			BaseValidator.ProcessEntity(schemaInfo, name, this, base.EventHandler, base.Reader.BaseURI, base.PositionInfo.LineNumber, base.PositionInfo.LinePosition);
			break;
		case XmlTokenizedType.IDREFS:
			break;
		}
	}

	private void CheckValue(string value, SchemaAttDef attdef)
	{
		try
		{
			reader.TypedValueObject = null;
			bool flag = attdef != null;
			XmlSchemaDatatype xmlSchemaDatatype = (flag ? attdef.Datatype : context.ElementDecl.Datatype);
			if (xmlSchemaDatatype == null)
			{
				return;
			}
			if (xmlSchemaDatatype.TokenizedType != 0)
			{
				value = value.Trim();
			}
			object obj = xmlSchemaDatatype.ParseValue(value, base.NameTable, s_namespaceManager);
			reader.TypedValueObject = obj;
			XmlTokenizedType tokenizedType = xmlSchemaDatatype.TokenizedType;
			if (tokenizedType == XmlTokenizedType.ENTITY || tokenizedType == XmlTokenizedType.ID || tokenizedType == XmlTokenizedType.IDREF)
			{
				if (xmlSchemaDatatype.Variety == XmlSchemaDatatypeVariety.List)
				{
					string[] array = (string[])obj;
					for (int i = 0; i < array.Length; i++)
					{
						ProcessTokenizedType(xmlSchemaDatatype.TokenizedType, array[i]);
					}
				}
				else
				{
					ProcessTokenizedType(xmlSchemaDatatype.TokenizedType, (string)obj);
				}
			}
			SchemaDeclBase schemaDeclBase = (flag ? ((SchemaDeclBase)attdef) : ((SchemaDeclBase)context.ElementDecl));
			if (schemaDeclBase.Values != null && !schemaDeclBase.CheckEnumeration(obj))
			{
				if (xmlSchemaDatatype.TokenizedType == XmlTokenizedType.NOTATION)
				{
					SendValidationEvent(System.SR.Sch_NotationValue, obj.ToString());
				}
				else
				{
					SendValidationEvent(System.SR.Sch_EnumerationValue, obj.ToString());
				}
			}
			if (!schemaDeclBase.CheckValue(obj))
			{
				if (flag)
				{
					SendValidationEvent(System.SR.Sch_FixedAttributeValue, attdef.Name.ToString());
				}
				else
				{
					SendValidationEvent(System.SR.Sch_FixedElementValue, XmlSchemaValidator.QNameString(context.LocalName, context.Namespace));
				}
			}
		}
		catch (XmlSchemaException)
		{
			if (attdef != null)
			{
				SendValidationEvent(System.SR.Sch_AttributeValueDataType, attdef.Name.ToString());
			}
			else
			{
				SendValidationEvent(System.SR.Sch_ElementValueDataType, XmlSchemaValidator.QNameString(context.LocalName, context.Namespace));
			}
		}
	}

	internal void AddID(string name, object node)
	{
		if (_IDs == null)
		{
			_IDs = new Hashtable();
		}
		_IDs.Add(name, node);
	}

	public override object FindId(string name)
	{
		if (_IDs != null)
		{
			return _IDs[name];
		}
		return null;
	}

	private bool GenEntity(XmlQualifiedName qname)
	{
		string name = qname.Name;
		if (name[0] == '#')
		{
			return false;
		}
		if (SchemaEntity.IsPredefinedEntity(name))
		{
			return false;
		}
		SchemaEntity entity = GetEntity(qname, fParameterEntity: false);
		if (entity == null)
		{
			throw new XmlException(System.SR.Xml_UndeclaredEntity, name);
		}
		if (!entity.NData.IsEmpty)
		{
			throw new XmlException(System.SR.Xml_UnparsedEntityRef, name);
		}
		if (reader.StandAlone && entity.DeclaredInExternal)
		{
			SendValidationEvent(System.SR.Sch_StandAlone);
		}
		return true;
	}

	private SchemaEntity GetEntity(XmlQualifiedName qname, bool fParameterEntity)
	{
		SchemaEntity value;
		if (fParameterEntity)
		{
			if (schemaInfo.ParameterEntities.TryGetValue(qname, out value))
			{
				return value;
			}
		}
		else if (schemaInfo.GeneralEntities.TryGetValue(qname, out value))
		{
			return value;
		}
		return null;
	}

	private void CheckForwardRefs()
	{
		IdRefNode idRefNode = _idRefListHead;
		while (idRefNode != null)
		{
			if (FindId(idRefNode.Id) == null)
			{
				SendValidationEvent(new XmlSchemaException(System.SR.Sch_UndeclaredId, idRefNode.Id, reader.BaseURI, idRefNode.LineNo, idRefNode.LinePos));
			}
			IdRefNode next = idRefNode.Next;
			idRefNode.Next = null;
			idRefNode = next;
		}
		_idRefListHead = null;
	}

	private void Push(XmlQualifiedName elementName)
	{
		context = (ValidationState)_validationStack.Push();
		if (context == null)
		{
			context = new ValidationState();
			_validationStack.AddToTop(context);
		}
		context.LocalName = elementName.Name;
		context.Namespace = elementName.Namespace;
		context.HasMatched = false;
		context.IsNill = false;
		context.NeedValidateChildren = false;
	}

	private bool Pop()
	{
		if (_validationStack.Length > 1)
		{
			_validationStack.Pop();
			context = (ValidationState)_validationStack.Peek();
			return true;
		}
		return false;
	}

	public static void SetDefaultTypedValue(SchemaAttDef attdef, IDtdParserAdapter readerAdapter)
	{
		try
		{
			string text = attdef.DefaultValueExpanded;
			XmlSchemaDatatype datatype = attdef.Datatype;
			if (datatype != null)
			{
				if (datatype.TokenizedType != 0)
				{
					text = text.Trim();
				}
				attdef.DefaultValueTyped = datatype.ParseValue(text, readerAdapter.NameTable, readerAdapter.NamespaceResolver);
			}
		}
		catch (Exception)
		{
			IValidationEventHandling validationEventHandling = ((IDtdParserAdapterWithValidation)readerAdapter).ValidationEventHandling;
			if (validationEventHandling != null)
			{
				XmlSchemaException exception = new XmlSchemaException(System.SR.Sch_AttributeDefaultDataType, attdef.Name.ToString());
				validationEventHandling.SendEvent(exception, XmlSeverityType.Error);
			}
		}
	}

	public static void CheckDefaultValue(SchemaAttDef attdef, SchemaInfo sinfo, IValidationEventHandling eventHandling, string baseUriStr)
	{
		try
		{
			if (baseUriStr == null)
			{
				baseUriStr = string.Empty;
			}
			XmlSchemaDatatype datatype = attdef.Datatype;
			if (datatype == null)
			{
				return;
			}
			object defaultValueTyped = attdef.DefaultValueTyped;
			switch (datatype.TokenizedType)
			{
			case XmlTokenizedType.ENTITY:
				if (datatype.Variety == XmlSchemaDatatypeVariety.List)
				{
					string[] array = (string[])defaultValueTyped;
					for (int i = 0; i < array.Length; i++)
					{
						BaseValidator.ProcessEntity(sinfo, array[i], eventHandling, baseUriStr, attdef.ValueLineNumber, attdef.ValueLinePosition);
					}
				}
				else
				{
					BaseValidator.ProcessEntity(sinfo, (string)defaultValueTyped, eventHandling, baseUriStr, attdef.ValueLineNumber, attdef.ValueLinePosition);
				}
				break;
			case XmlTokenizedType.ENUMERATION:
				if (!attdef.CheckEnumeration(defaultValueTyped) && eventHandling != null)
				{
					XmlSchemaException exception = new XmlSchemaException(System.SR.Sch_EnumerationValue, defaultValueTyped.ToString(), baseUriStr, attdef.ValueLineNumber, attdef.ValueLinePosition);
					eventHandling.SendEvent(exception, XmlSeverityType.Error);
				}
				break;
			}
		}
		catch (Exception)
		{
			if (eventHandling != null)
			{
				XmlSchemaException exception2 = new XmlSchemaException(System.SR.Sch_AttributeDefaultDataType, attdef.Name.ToString());
				eventHandling.SendEvent(exception2, XmlSeverityType.Error);
			}
		}
	}
}
