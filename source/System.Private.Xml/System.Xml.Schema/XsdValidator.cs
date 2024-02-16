using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace System.Xml.Schema;

internal sealed class XsdValidator : BaseValidator
{
	private int _startIDConstraint = -1;

	private HWStack _validationStack;

	private Hashtable _attPresence;

	private XmlNamespaceManager _nsManager;

	private bool _bManageNamespaces;

	private Hashtable _IDs;

	private IdRefNode _idRefListHead;

	private Parser _inlineSchemaParser;

	private XmlSchemaContentProcessing _processContents;

	private static readonly XmlSchemaDatatype s_dtCDATA = XmlSchemaDatatype.FromXmlTokenizedType(XmlTokenizedType.CDATA);

	private static readonly XmlSchemaDatatype s_dtQName = XmlSchemaDatatype.FromXmlTokenizedTypeXsd(XmlTokenizedType.QName);

	private static readonly XmlSchemaDatatype s_dtStringArray = s_dtCDATA.DeriveByList(null);

	private string _nsXmlNs;

	private string _nsXs;

	private string _nsXsi;

	private string _xsiType;

	private string _xsiNil;

	private string _xsiSchemaLocation;

	private string _xsiNoNamespaceSchemaLocation;

	private string _xsdSchema;

	private bool IsInlineSchemaStarted => _inlineSchemaParser != null;

	private bool HasSchema => schemaInfo.SchemaType != SchemaType.None;

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

	private bool HasIdentityConstraints => _startIDConstraint != -1;

	internal XsdValidator(BaseValidator validator)
		: base(validator)
	{
		Init();
	}

	internal XsdValidator(XmlValidatingReaderImpl reader, XmlSchemaCollection schemaCollection, IValidationEventHandling eventHandling)
		: base(reader, schemaCollection, eventHandling)
	{
		Init();
	}

	[MemberNotNull("_nsManager")]
	[MemberNotNull("_validationStack")]
	[MemberNotNull("_attPresence")]
	[MemberNotNull("_processContents")]
	[MemberNotNull("_nsXmlNs")]
	[MemberNotNull("_nsXs")]
	[MemberNotNull("_nsXsi")]
	[MemberNotNull("_xsiType")]
	[MemberNotNull("_xsiNil")]
	[MemberNotNull("_xsiSchemaLocation")]
	[MemberNotNull("_xsiNoNamespaceSchemaLocation")]
	[MemberNotNull("_xsdSchema")]
	private void Init()
	{
		_nsManager = reader.NamespaceManager;
		if (_nsManager == null)
		{
			_nsManager = new XmlNamespaceManager(base.NameTable);
			_bManageNamespaces = true;
		}
		_validationStack = new HWStack(10);
		textValue = new StringBuilder();
		_attPresence = new Hashtable();
		schemaInfo = new SchemaInfo();
		checkDatatype = false;
		_processContents = XmlSchemaContentProcessing.Strict;
		Push(XmlQualifiedName.Empty);
		_nsXmlNs = base.NameTable.Add("http://www.w3.org/2000/xmlns/");
		_nsXs = base.NameTable.Add("http://www.w3.org/2001/XMLSchema");
		_nsXsi = base.NameTable.Add("http://www.w3.org/2001/XMLSchema-instance");
		_xsiType = base.NameTable.Add("type");
		_xsiNil = base.NameTable.Add("nil");
		_xsiSchemaLocation = base.NameTable.Add("schemaLocation");
		_xsiNoNamespaceSchemaLocation = base.NameTable.Add("noNamespaceSchemaLocation");
		_xsdSchema = base.NameTable.Add("schema");
	}

	public override void Validate()
	{
		if (IsInlineSchemaStarted)
		{
			ProcessInlineSchema();
			return;
		}
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
			ValidateWhitespace();
			break;
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
		case XmlNodeType.SignificantWhitespace:
			ValidateText();
			break;
		case XmlNodeType.EndElement:
			ValidateEndElement();
			break;
		}
	}

	public override void CompleteValidation()
	{
		CheckForwardRefs();
	}

	private void ProcessInlineSchema()
	{
		if (_inlineSchemaParser.ParseReaderNode())
		{
			return;
		}
		_inlineSchemaParser.FinishParsing();
		XmlSchema xmlSchema = _inlineSchemaParser.XmlSchema;
		string text = null;
		if (xmlSchema != null && xmlSchema.ErrorCount == 0)
		{
			try
			{
				SchemaInfo schemaInfo = new SchemaInfo();
				schemaInfo.SchemaType = SchemaType.XSD;
				text = ((xmlSchema.TargetNamespace == null) ? string.Empty : xmlSchema.TargetNamespace);
				if (!base.SchemaInfo.TargetNamespaces.ContainsKey(text) && base.SchemaCollection.Add(text, schemaInfo, xmlSchema, compile: true) != null)
				{
					base.SchemaInfo.Add(schemaInfo, base.EventHandler);
				}
			}
			catch (XmlSchemaException ex)
			{
				SendValidationEvent(System.SR.Sch_CannotLoadSchema, new string[2]
				{
					base.BaseUri.AbsoluteUri,
					ex.Message
				}, XmlSeverityType.Error);
			}
		}
		_inlineSchemaParser = null;
	}

	private void ValidateElement()
	{
		elementName.Init(reader.LocalName, reader.NamespaceURI);
		object particle = ValidateChildElement();
		if (IsXSDRoot(elementName.Name, elementName.Namespace) && reader.Depth > 0)
		{
			_inlineSchemaParser = new Parser(SchemaType.XSD, base.NameTable, base.SchemaNames, base.EventHandler);
			_inlineSchemaParser.StartParsing(reader, null);
			ProcessInlineSchema();
		}
		else
		{
			ProcessElement(particle);
		}
	}

	private object ValidateChildElement()
	{
		object obj = null;
		int errorCode = 0;
		if (context.NeedValidateChildren)
		{
			if (context.IsNill)
			{
				SendValidationEvent(System.SR.Sch_ContentInNill, elementName.ToString());
				return null;
			}
			obj = context.ElementDecl.ContentValidator.ValidateElement(elementName, context, out errorCode);
			if (obj == null)
			{
				_processContents = (context.ProcessContents = XmlSchemaContentProcessing.Skip);
				if (errorCode == -2)
				{
					SendValidationEvent(System.SR.Sch_AllElement, elementName.ToString());
				}
				XmlSchemaValidator.ElementValidationError(elementName, context, base.EventHandler, reader, reader.BaseURI, base.PositionInfo.LineNumber, base.PositionInfo.LinePosition, null);
			}
		}
		return obj;
	}

	private void ProcessElement(object particle)
	{
		SchemaElementDecl schemaElementDecl = FastGetElementDecl(particle);
		Push(elementName);
		if (_bManageNamespaces)
		{
			_nsManager.PushScope();
		}
		ProcessXsiAttributes(out var xsiType, out var xsiNil);
		if (_processContents != XmlSchemaContentProcessing.Skip)
		{
			if (schemaElementDecl == null || !xsiType.IsEmpty || xsiNil != null)
			{
				schemaElementDecl = ThoroughGetElementDecl(schemaElementDecl, xsiType, xsiNil);
			}
			if (schemaElementDecl == null)
			{
				if (HasSchema && _processContents == XmlSchemaContentProcessing.Strict)
				{
					SendValidationEvent(System.SR.Sch_UndeclaredElement, XmlSchemaValidator.QNameString(context.LocalName, context.Namespace));
				}
				else
				{
					SendValidationEvent(System.SR.Sch_NoElementSchemaFound, XmlSchemaValidator.QNameString(context.LocalName, context.Namespace), XmlSeverityType.Warning);
				}
			}
		}
		context.ElementDecl = schemaElementDecl;
		ValidateStartElementIdentityConstraints();
		ValidateStartElement();
		if (context.ElementDecl != null)
		{
			ValidateEndStartElement();
			context.NeedValidateChildren = _processContents != XmlSchemaContentProcessing.Skip;
			context.ElementDecl.ContentValidator.InitValidation(context);
		}
	}

	private void ProcessXsiAttributes(out XmlQualifiedName xsiType, out string xsiNil)
	{
		string[] array = null;
		string text = null;
		xsiType = XmlQualifiedName.Empty;
		xsiNil = null;
		if (reader.Depth == 0)
		{
			LoadSchema(string.Empty, null);
			foreach (string value in _nsManager.GetNamespacesInScope(XmlNamespaceScope.ExcludeXml).Values)
			{
				LoadSchema(value, null);
			}
		}
		if (reader.MoveToFirstAttribute())
		{
			do
			{
				string namespaceURI = reader.NamespaceURI;
				string localName = reader.LocalName;
				if (Ref.Equal(namespaceURI, _nsXmlNs))
				{
					LoadSchema(reader.Value, null);
					if (_bManageNamespaces)
					{
						_nsManager.AddNamespace((reader.Prefix.Length == 0) ? string.Empty : reader.LocalName, reader.Value);
					}
				}
				else if (Ref.Equal(namespaceURI, _nsXsi))
				{
					if (Ref.Equal(localName, _xsiSchemaLocation))
					{
						array = (string[])s_dtStringArray.ParseValue(reader.Value, base.NameTable, _nsManager);
					}
					else if (Ref.Equal(localName, _xsiNoNamespaceSchemaLocation))
					{
						text = reader.Value;
					}
					else if (Ref.Equal(localName, _xsiType))
					{
						xsiType = (XmlQualifiedName)s_dtQName.ParseValue(reader.Value, base.NameTable, _nsManager);
					}
					else if (Ref.Equal(localName, _xsiNil))
					{
						xsiNil = reader.Value;
					}
				}
			}
			while (reader.MoveToNextAttribute());
			reader.MoveToElement();
		}
		if (text != null)
		{
			LoadSchema(string.Empty, text);
		}
		if (array != null)
		{
			for (int i = 0; i < array.Length - 1; i += 2)
			{
				LoadSchema(array[i], array[i + 1]);
			}
		}
	}

	private void ValidateEndElement()
	{
		if (_bManageNamespaces)
		{
			_nsManager.PopScope();
		}
		if (context.ElementDecl != null)
		{
			if (!context.IsNill)
			{
				if (context.NeedValidateChildren && !context.ElementDecl.ContentValidator.CompleteValidation(context))
				{
					XmlSchemaValidator.CompleteValidationError(context, base.EventHandler, reader, reader.BaseURI, base.PositionInfo.LineNumber, base.PositionInfo.LinePosition, null);
				}
				if (checkDatatype && !context.IsNill)
				{
					string text = ((!hasSibling) ? textString : textValue.ToString());
					if (text.Length != 0 || context.ElementDecl.DefaultValueTyped == null)
					{
						CheckValue(text, null);
						checkDatatype = false;
					}
				}
			}
			if (HasIdentityConstraints)
			{
				EndElementIdentityConstraints();
			}
		}
		Pop();
	}

	private SchemaElementDecl FastGetElementDecl(object particle)
	{
		SchemaElementDecl result = null;
		if (particle != null)
		{
			if (particle is XmlSchemaElement xmlSchemaElement)
			{
				result = xmlSchemaElement.ElementDecl;
			}
			else
			{
				XmlSchemaAny xmlSchemaAny = (XmlSchemaAny)particle;
				_processContents = xmlSchemaAny.ProcessContentsCorrect;
			}
		}
		return result;
	}

	private SchemaElementDecl ThoroughGetElementDecl(SchemaElementDecl elementDecl, XmlQualifiedName xsiType, string xsiNil)
	{
		if (elementDecl == null)
		{
			elementDecl = schemaInfo.GetElementDecl(elementName);
		}
		if (elementDecl != null)
		{
			if (xsiType.IsEmpty)
			{
				if (elementDecl.IsAbstract)
				{
					SendValidationEvent(System.SR.Sch_AbstractElement, XmlSchemaValidator.QNameString(context.LocalName, context.Namespace));
					elementDecl = null;
				}
			}
			else if (xsiNil != null && xsiNil.Equals("true"))
			{
				SendValidationEvent(System.SR.Sch_XsiNilAndType);
			}
			else
			{
				if (!schemaInfo.ElementDeclsByType.TryGetValue(xsiType, out var value) && xsiType.Namespace == _nsXs)
				{
					XmlSchemaSimpleType simpleTypeFromXsdType = DatatypeImplementation.GetSimpleTypeFromXsdType(new XmlQualifiedName(xsiType.Name, _nsXs));
					if (simpleTypeFromXsdType != null)
					{
						value = simpleTypeFromXsdType.ElementDecl;
					}
				}
				if (value == null)
				{
					SendValidationEvent(System.SR.Sch_XsiTypeNotFound, xsiType.ToString());
					elementDecl = null;
				}
				else if (!XmlSchemaType.IsDerivedFrom(value.SchemaType, elementDecl.SchemaType, elementDecl.Block))
				{
					SendValidationEvent(System.SR.Sch_XsiTypeBlockedEx, new string[2]
					{
						xsiType.ToString(),
						XmlSchemaValidator.QNameString(context.LocalName, context.Namespace)
					});
					elementDecl = null;
				}
				else
				{
					elementDecl = value;
				}
			}
			if (elementDecl != null && elementDecl.IsNillable)
			{
				if (xsiNil != null)
				{
					context.IsNill = XmlConvert.ToBoolean(xsiNil);
					if (context.IsNill && elementDecl.DefaultValueTyped != null)
					{
						SendValidationEvent(System.SR.Sch_XsiNilAndFixed);
					}
				}
			}
			else if (xsiNil != null)
			{
				SendValidationEvent(System.SR.Sch_InvalidXsiNill);
			}
		}
		return elementDecl;
	}

	private void ValidateStartElement()
	{
		if (context.ElementDecl != null)
		{
			if (context.ElementDecl.IsAbstract)
			{
				SendValidationEvent(System.SR.Sch_AbstractElement, XmlSchemaValidator.QNameString(context.LocalName, context.Namespace));
			}
			reader.SchemaTypeObject = context.ElementDecl.SchemaType;
			if (reader.IsEmptyElement && !context.IsNill && context.ElementDecl.DefaultValueTyped != null)
			{
				reader.TypedValueObject = UnWrapUnion(context.ElementDecl.DefaultValueTyped);
				context.IsNill = true;
			}
			else
			{
				reader.TypedValueObject = null;
			}
			if (context.ElementDecl.HasRequiredAttribute || HasIdentityConstraints)
			{
				_attPresence.Clear();
			}
		}
		if (!reader.MoveToFirstAttribute())
		{
			return;
		}
		do
		{
			if ((object)reader.NamespaceURI == _nsXmlNs || (object)reader.NamespaceURI == _nsXsi)
			{
				continue;
			}
			try
			{
				reader.SchemaTypeObject = null;
				XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(reader.LocalName, reader.NamespaceURI);
				bool skip = _processContents == XmlSchemaContentProcessing.Skip;
				SchemaAttDef attributeXsd = schemaInfo.GetAttributeXsd(context.ElementDecl, xmlQualifiedName, ref skip);
				if (attributeXsd != null)
				{
					if (context.ElementDecl != null && (context.ElementDecl.HasRequiredAttribute || _startIDConstraint != -1))
					{
						_attPresence.Add(attributeXsd.Name, attributeXsd);
					}
					reader.SchemaTypeObject = attributeXsd.SchemaType;
					if (attributeXsd.Datatype != null)
					{
						CheckValue(reader.Value, attributeXsd);
					}
					if (HasIdentityConstraints)
					{
						AttributeIdentityConstraints(reader.LocalName, reader.NamespaceURI, reader.TypedValueObject, reader.Value, attributeXsd);
					}
				}
				else if (!skip)
				{
					if (context.ElementDecl == null && _processContents == XmlSchemaContentProcessing.Strict && xmlQualifiedName.Namespace.Length != 0 && schemaInfo.Contains(xmlQualifiedName.Namespace))
					{
						SendValidationEvent(System.SR.Sch_UndeclaredAttribute, xmlQualifiedName.ToString());
					}
					else
					{
						SendValidationEvent(System.SR.Sch_NoAttributeSchemaFound, xmlQualifiedName.ToString(), XmlSeverityType.Warning);
					}
				}
			}
			catch (XmlSchemaException ex)
			{
				ex.SetSource(reader.BaseURI, base.PositionInfo.LineNumber, base.PositionInfo.LinePosition);
				SendValidationEvent(ex);
			}
		}
		while (reader.MoveToNextAttribute());
		reader.MoveToElement();
	}

	private void ValidateEndStartElement()
	{
		if (context.ElementDecl.HasDefaultAttribute)
		{
			for (int i = 0; i < context.ElementDecl.DefaultAttDefs.Count; i++)
			{
				SchemaAttDef schemaAttDef = (SchemaAttDef)context.ElementDecl.DefaultAttDefs[i];
				reader.AddDefaultAttribute(schemaAttDef);
				if (HasIdentityConstraints && !_attPresence.Contains(schemaAttDef.Name))
				{
					AttributeIdentityConstraints(schemaAttDef.Name.Name, schemaAttDef.Name.Namespace, UnWrapUnion(schemaAttDef.DefaultValueTyped), schemaAttDef.DefaultValueRaw, schemaAttDef);
				}
			}
		}
		if (context.ElementDecl.HasRequiredAttribute)
		{
			try
			{
				context.ElementDecl.CheckAttributes(_attPresence, reader.StandAlone);
			}
			catch (XmlSchemaException ex)
			{
				ex.SetSource(reader.BaseURI, base.PositionInfo.LineNumber, base.PositionInfo.LinePosition);
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

	private void LoadSchemaFromLocation(string uri, string url)
	{
		XmlReader xmlReader = null;
		SchemaInfo schemaInfo = null;
		try
		{
			Uri uri2 = base.XmlResolver.ResolveUri(base.BaseUri, url);
			Stream input = (Stream)base.XmlResolver.GetEntity(uri2, null, null);
			xmlReader = new XmlTextReader(uri2.ToString(), input, base.NameTable);
			Parser parser = new Parser(SchemaType.XSD, base.NameTable, base.SchemaNames, base.EventHandler);
			parser.XmlResolver = base.XmlResolver;
			SchemaType schemaType = parser.Parse(xmlReader, uri);
			schemaInfo = new SchemaInfo();
			schemaInfo.SchemaType = schemaType;
			if (schemaType == SchemaType.XSD)
			{
				if (base.SchemaCollection.EventHandler == null)
				{
					base.SchemaCollection.EventHandler = base.EventHandler;
				}
				base.SchemaCollection.Add(uri, schemaInfo, parser.XmlSchema, compile: true);
			}
			base.SchemaInfo.Add(schemaInfo, base.EventHandler);
			while (xmlReader.Read())
			{
			}
		}
		catch (XmlSchemaException ex)
		{
			schemaInfo = null;
			SendValidationEvent(System.SR.Sch_CannotLoadSchema, new string[2] { uri, ex.Message }, XmlSeverityType.Error);
		}
		catch (Exception ex2)
		{
			schemaInfo = null;
			SendValidationEvent(System.SR.Sch_CannotLoadSchema, new string[2] { uri, ex2.Message }, XmlSeverityType.Warning);
		}
		finally
		{
			xmlReader?.Close();
		}
	}

	private void LoadSchema(string uri, string url)
	{
		if (base.XmlResolver == null || (base.SchemaInfo.TargetNamespaces.ContainsKey(uri) && _nsManager.LookupPrefix(uri) != null))
		{
			return;
		}
		SchemaInfo schemaInfo = null;
		if (base.SchemaCollection != null)
		{
			schemaInfo = base.SchemaCollection.GetSchemaInfo(uri);
		}
		if (schemaInfo != null)
		{
			if (schemaInfo.SchemaType != SchemaType.XSD)
			{
				throw new XmlException(System.SR.Xml_MultipleValidaitonTypes, string.Empty, base.PositionInfo.LineNumber, base.PositionInfo.LinePosition);
			}
			base.SchemaInfo.Add(schemaInfo, base.EventHandler);
		}
		else if (url != null)
		{
			LoadSchemaFromLocation(uri, url);
		}
	}

	private void ProcessTokenizedType(XmlTokenizedType ttype, string name)
	{
		switch (ttype)
		{
		case XmlTokenizedType.ID:
			if (FindId(name) != null)
			{
				SendValidationEvent(System.SR.Sch_DupId, name);
			}
			else
			{
				AddID(name, context.LocalName);
			}
			break;
		case XmlTokenizedType.IDREF:
		{
			object obj = FindId(name);
			if (obj == null)
			{
				_idRefListHead = new IdRefNode(_idRefListHead, name, base.PositionInfo.LineNumber, base.PositionInfo.LinePosition);
			}
			break;
		}
		case XmlTokenizedType.ENTITY:
			BaseValidator.ProcessEntity(schemaInfo, name, this, base.EventHandler, reader.BaseURI, base.PositionInfo.LineNumber, base.PositionInfo.LinePosition);
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
			object obj = xmlSchemaDatatype.ParseValue(value, base.NameTable, _nsManager, createAtomicValue: true);
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
			if (xmlSchemaDatatype.Variety == XmlSchemaDatatypeVariety.Union)
			{
				obj = UnWrapUnion(obj);
			}
			reader.TypedValueObject = obj;
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

	public bool IsXSDRoot(string localName, string ns)
	{
		if (Ref.Equal(ns, _nsXs))
		{
			return Ref.Equal(localName, _xsdSchema);
		}
		return false;
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
		context.ProcessContents = _processContents;
		context.NeedValidateChildren = false;
		context.Constr = null;
	}

	private void Pop()
	{
		if (_validationStack.Length > 1)
		{
			_validationStack.Pop();
			if (_startIDConstraint == _validationStack.Length)
			{
				_startIDConstraint = -1;
			}
			context = (ValidationState)_validationStack.Peek();
			_processContents = context.ProcessContents;
		}
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

	private void ValidateStartElementIdentityConstraints()
	{
		if (context.ElementDecl != null)
		{
			if (context.ElementDecl.Constraints != null)
			{
				AddIdentityConstraints();
			}
			if (HasIdentityConstraints)
			{
				ElementIdentityConstraints();
			}
		}
	}

	private void AddIdentityConstraints()
	{
		context.Constr = new ConstraintStruct[context.ElementDecl.Constraints.Length];
		int num = 0;
		for (int i = 0; i < context.ElementDecl.Constraints.Length; i++)
		{
			context.Constr[num++] = new ConstraintStruct(context.ElementDecl.Constraints[i]);
		}
		for (int j = 0; j < context.Constr.Length; j++)
		{
			if (context.Constr[j].constraint.Role != CompiledIdentityConstraint.ConstraintRole.Keyref)
			{
				continue;
			}
			bool flag = false;
			for (int num2 = _validationStack.Length - 1; num2 >= ((_startIDConstraint >= 0) ? _startIDConstraint : (_validationStack.Length - 1)); num2--)
			{
				if (((ValidationState)_validationStack[num2]).Constr != null)
				{
					ConstraintStruct[] constr = ((ValidationState)_validationStack[num2]).Constr;
					for (int k = 0; k < constr.Length; k++)
					{
						if (constr[k].constraint.name == context.Constr[j].constraint.refer)
						{
							flag = true;
							if (constr[k].keyrefTable == null)
							{
								constr[k].keyrefTable = new Hashtable();
							}
							context.Constr[j].qualifiedTable = constr[k].keyrefTable;
							break;
						}
					}
					if (flag)
					{
						break;
					}
				}
			}
			if (!flag)
			{
				SendValidationEvent(System.SR.Sch_RefNotInScope, XmlSchemaValidator.QNameString(context.LocalName, context.Namespace));
			}
		}
		if (_startIDConstraint == -1)
		{
			_startIDConstraint = _validationStack.Length - 1;
		}
	}

	private void ElementIdentityConstraints()
	{
		for (int i = _startIDConstraint; i < _validationStack.Length; i++)
		{
			if (((ValidationState)_validationStack[i]).Constr == null)
			{
				continue;
			}
			ConstraintStruct[] constr = ((ValidationState)_validationStack[i]).Constr;
			for (int j = 0; j < constr.Length; j++)
			{
				if (constr[j].axisSelector.MoveToStartElement(reader.LocalName, reader.NamespaceURI))
				{
					constr[j].axisSelector.PushKS(base.PositionInfo.LineNumber, base.PositionInfo.LinePosition);
				}
				for (int k = 0; k < constr[j].axisFields.Count; k++)
				{
					LocatedActiveAxis locatedActiveAxis = (LocatedActiveAxis)constr[j].axisFields[k];
					if (locatedActiveAxis.MoveToStartElement(reader.LocalName, reader.NamespaceURI) && context.ElementDecl != null)
					{
						if (context.ElementDecl.Datatype == null)
						{
							SendValidationEvent(System.SR.Sch_FieldSimpleTypeExpected, reader.LocalName);
						}
						else
						{
							locatedActiveAxis.isMatched = true;
						}
					}
				}
			}
		}
	}

	private void AttributeIdentityConstraints(string name, string ns, object obj, string sobj, SchemaAttDef attdef)
	{
		for (int i = _startIDConstraint; i < _validationStack.Length; i++)
		{
			if (((ValidationState)_validationStack[i]).Constr == null)
			{
				continue;
			}
			ConstraintStruct[] constr = ((ValidationState)_validationStack[i]).Constr;
			for (int j = 0; j < constr.Length; j++)
			{
				for (int k = 0; k < constr[j].axisFields.Count; k++)
				{
					LocatedActiveAxis locatedActiveAxis = (LocatedActiveAxis)constr[j].axisFields[k];
					if (locatedActiveAxis.MoveToAttribute(name, ns))
					{
						if (locatedActiveAxis.Ks[locatedActiveAxis.Column] != null)
						{
							SendValidationEvent(System.SR.Sch_FieldSingleValueExpected, name);
						}
						else if (attdef != null && attdef.Datatype != null)
						{
							locatedActiveAxis.Ks[locatedActiveAxis.Column] = new TypedObject(obj, sobj, attdef.Datatype);
						}
					}
				}
			}
		}
	}

	private object UnWrapUnion(object typedValue)
	{
		if (typedValue is XsdSimpleValue xsdSimpleValue)
		{
			typedValue = xsdSimpleValue.TypedValue;
		}
		return typedValue;
	}

	private void EndElementIdentityConstraints()
	{
		for (int num = _validationStack.Length - 1; num >= _startIDConstraint; num--)
		{
			if (((ValidationState)_validationStack[num]).Constr != null)
			{
				ConstraintStruct[] constr = ((ValidationState)_validationStack[num]).Constr;
				for (int i = 0; i < constr.Length; i++)
				{
					for (int j = 0; j < constr[i].axisFields.Count; j++)
					{
						LocatedActiveAxis locatedActiveAxis = (LocatedActiveAxis)constr[i].axisFields[j];
						if (locatedActiveAxis.isMatched)
						{
							locatedActiveAxis.isMatched = false;
							if (locatedActiveAxis.Ks[locatedActiveAxis.Column] != null)
							{
								SendValidationEvent(System.SR.Sch_FieldSingleValueExpected, reader.LocalName);
							}
							else
							{
								string text = ((!hasSibling) ? textString : textValue.ToString());
								if (reader.TypedValueObject != null && text.Length != 0)
								{
									locatedActiveAxis.Ks[locatedActiveAxis.Column] = new TypedObject(reader.TypedValueObject, text, context.ElementDecl.Datatype);
								}
							}
						}
						locatedActiveAxis.EndElement(reader.LocalName, reader.NamespaceURI);
					}
					if (!constr[i].axisSelector.EndElement(reader.LocalName, reader.NamespaceURI))
					{
						continue;
					}
					KeySequence keySequence = constr[i].axisSelector.PopKS();
					switch (constr[i].constraint.Role)
					{
					case CompiledIdentityConstraint.ConstraintRole.Key:
						if (!keySequence.IsQualified())
						{
							SendValidationEvent(new XmlSchemaException(System.SR.Sch_MissingKey, constr[i].constraint.name.ToString(), reader.BaseURI, keySequence.PosLine, keySequence.PosCol));
						}
						else if (constr[i].qualifiedTable.Contains(keySequence))
						{
							SendValidationEvent(new XmlSchemaException(System.SR.Sch_DuplicateKey, new string[2]
							{
								keySequence.ToString(),
								constr[i].constraint.name.ToString()
							}, reader.BaseURI, keySequence.PosLine, keySequence.PosCol));
						}
						else
						{
							constr[i].qualifiedTable.Add(keySequence, keySequence);
						}
						break;
					case CompiledIdentityConstraint.ConstraintRole.Unique:
						if (keySequence.IsQualified())
						{
							if (constr[i].qualifiedTable.Contains(keySequence))
							{
								SendValidationEvent(new XmlSchemaException(System.SR.Sch_DuplicateKey, new string[2]
								{
									keySequence.ToString(),
									constr[i].constraint.name.ToString()
								}, reader.BaseURI, keySequence.PosLine, keySequence.PosCol));
							}
							else
							{
								constr[i].qualifiedTable.Add(keySequence, keySequence);
							}
						}
						break;
					case CompiledIdentityConstraint.ConstraintRole.Keyref:
						if (constr[i].qualifiedTable != null && keySequence.IsQualified() && !constr[i].qualifiedTable.Contains(keySequence))
						{
							constr[i].qualifiedTable.Add(keySequence, keySequence);
						}
						break;
					}
				}
			}
		}
		ConstraintStruct[] constr2 = ((ValidationState)_validationStack[_validationStack.Length - 1]).Constr;
		if (constr2 == null)
		{
			return;
		}
		for (int k = 0; k < constr2.Length; k++)
		{
			if (constr2[k].constraint.Role == CompiledIdentityConstraint.ConstraintRole.Keyref || constr2[k].keyrefTable == null)
			{
				continue;
			}
			foreach (KeySequence key in constr2[k].keyrefTable.Keys)
			{
				if (!constr2[k].qualifiedTable.Contains(key))
				{
					SendValidationEvent(new XmlSchemaException(System.SR.Sch_UnresolvedKeyref, new string[2]
					{
						key.ToString(),
						constr2[k].constraint.name.ToString()
					}, reader.BaseURI, key.PosLine, key.PosCol));
				}
			}
		}
	}
}
